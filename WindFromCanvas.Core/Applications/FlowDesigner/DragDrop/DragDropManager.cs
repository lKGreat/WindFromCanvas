using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 拖拽管理器（匹配 Activepieces DndContext）
    /// </summary>
    public class DragDropManager
    {
        private readonly BuilderStateStore _stateStore;
        private readonly CollisionDetector _collisionDetector;
        private DragContext _currentDrag;
        private List<IDropTarget> _dropTargets;
        private DragOverlay _dragOverlay;

        public event EventHandler<DragContext> DragStarted;
        public event EventHandler<DragContext> DragUpdated;
        public event EventHandler<DragContext> DragEnded;

        /// <summary>
        /// 拖拽覆盖层（用于渲染）
        /// </summary>
        public DragOverlay DragOverlay => _dragOverlay;

        public DragDropManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _collisionDetector = new CollisionDetector();
            _dropTargets = new List<IDropTarget>();
            _dragOverlay = new DragOverlay();
        }

        /// <summary>
        /// 注册放置目标
        /// </summary>
        public void RegisterDropTarget(IDropTarget target)
        {
            if (!_dropTargets.Contains(target))
            {
                _dropTargets.Add(target);
            }
        }

        /// <summary>
        /// 取消注册放置目标
        /// </summary>
        public void UnregisterDropTarget(IDropTarget target)
        {
            _dropTargets.Remove(target);
        }

        /// <summary>
        /// 获取所有注册的放置目标（用于清理）
        /// </summary>
        public List<IDropTarget> GetDropTargets()
        {
            return new List<IDropTarget>(_dropTargets);
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void StartDrag(IDraggable item, PointF position)
        {
            _currentDrag = new DragContext
            {
                Item = item,
                StartPosition = position,
                CurrentPosition = position
            };

            _stateStore.StartDrag(item.Id, position);
            DragStarted?.Invoke(this, _currentDrag);
        }

        /// <summary>
        /// 更新拖拽
        /// </summary>
        public void UpdateDrag(PointF position, RectangleF dragRect)
        {
            if (_currentDrag == null) return;

            _currentDrag.CurrentPosition = position;
            var collision = _collisionDetector.DetectCollision(_currentDrag, _dropTargets, dragRect);
            _currentDrag.HoveredTarget = collision;

            _stateStore.UpdateDrag(position, collision?.Id);
            DragUpdated?.Invoke(this, _currentDrag);
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag()
        {
            if (_currentDrag == null) return;

            if (_currentDrag.HoveredTarget != null)
            {
                ExecuteDrop(_currentDrag);
            }

            _stateStore.EndDrag();
            DragEnded?.Invoke(this, _currentDrag);
            _currentDrag = null;
        }

        /// <summary>
        /// 3.2.6 取消拖拽（Escape键）
        /// </summary>
        public void CancelDrag()
        {
            if (_currentDrag != null)
            {
                _dragOverlay?.CancelDrag();
                _currentDrag = null;
                _stateStore.EndDrag();
            }
        }

        /// <summary>
        /// 3.2.4 执行放置操作（完善具体逻辑）
        /// </summary>
        private void ExecuteDrop(DragContext context)
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;
            if (context.HoveredTarget == null) return;
            if (!context.HoveredTarget.CanAccept(context.Item)) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;
            var dropTarget = context.HoveredTarget as DropTarget;
            
            if (dropTarget == null) return;

            // 获取被拖拽的步骤
            var draggedStep = FlowStructureUtil.GetStep(context.Item.Id, trigger);
            if (draggedStep == null) return;

            // 3.2.5 防止循环嵌套验证
            if (!ValidateDropTarget(draggedStep, dropTarget, trigger))
            {
                // 不能放置到此目标
                return;
            }

            // 如果是动作，执行移动操作
            if (draggedStep is FlowAction draggedAction)
            {
                // 检查是否是移动到新位置（而不是原地）
                var currentParent = FindParentStep(draggedAction, trigger);
                var isNewLocation = currentParent != dropTarget.ParentStepName || 
                                   GetCurrentLocation(draggedAction, trigger) != dropTarget.Location;

                if (isNewLocation)
                {
                    // 移动动作到新位置
                    var moveRequest = new MoveActionRequest
                    {
                        StepName = context.Item.Id,
                        NewParentStepName = dropTarget.ParentStepName,
                        StepLocationRelativeToParent = dropTarget.Location,
                        BranchIndex = dropTarget.BranchIndex
                    };

                    var operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.MOVE_ACTION,
                        Request = moveRequest
                    };

                    _stateStore.ApplyOperation(operation);
                }
            }
        }

        /// <summary>
        /// 3.2.5 验证放置目标（防止循环嵌套）
        /// 检查是否会创建循环依赖或非法嵌套
        /// </summary>
        private bool ValidateDropTarget(IStep draggedStep, DropTarget dropTarget, IStep root)
        {
            if (draggedStep == null || dropTarget == null)
                return false;

            // 1. 不能将节点拖到自己内部
            if (draggedStep.Name == dropTarget.ParentStepName)
                return false;

            // 2. 不能将父节点拖到子节点内部（防止循环嵌套）
            var allChildSteps = FlowStructureUtil.GetAllChildSteps(draggedStep);
            foreach (var childStep in allChildSteps)
            {
                if (childStep.Name == dropTarget.ParentStepName)
                {
                    return false; // 会创建循环嵌套
                }
            }

            // 3. 检查目标位置是否有效
            var targetParent = FlowStructureUtil.GetStep(dropTarget.ParentStepName, root);
            if (targetParent == null)
                return false;

            // 4. 如果目标是循环内部，确保只有单个步骤可以进入
            if (dropTarget.Location == StepLocationRelativeToParent.INSIDE_LOOP)
            {
                if (targetParent is LoopOnItemsAction loop)
                {
                    // 循环内部可以放置
                    return true;
                }
                return false;
            }

            // 5. 如果目标是分支内部，确保分支索引有效
            if (dropTarget.Location == StepLocationRelativeToParent.INSIDE_BRANCH)
            {
                if (targetParent is RouterAction router)
                {
                    return dropTarget.BranchIndex.HasValue && 
                           dropTarget.BranchIndex.Value >= 0 && 
                           dropTarget.BranchIndex.Value < router.Children.Count;
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 查找步骤的父步骤
        /// </summary>
        private string FindParentStep(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                // 检查是否是 nextAction
                FlowAction nextAction = null;
                if (step is FlowAction parentAction)
                {
                    nextAction = parentAction.NextAction;
                }
                else if (step is FlowTrigger trigger)
                {
                    nextAction = trigger.NextAction;
                }

                if (nextAction != null && nextAction.Name == action.Name)
                {
                    return step.Name;
                }

                // 检查是否在循环内
                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null && loop.FirstLoopAction.Name == action.Name)
                {
                    return step.Name;
                }

                // 检查是否在路由分支内
                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null && router.Children[i].Name == action.Name)
                        {
                            return step.Name;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取步骤的当前位置
        /// </summary>
        private StepLocationRelativeToParent GetCurrentLocation(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                // 检查是否在循环内
                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null)
                {
                    var childSteps = FlowStructureUtil.GetAllChildSteps(loop);
                    if (childSteps.Any(s => s.Name == action.Name))
                    {
                        return StepLocationRelativeToParent.INSIDE_LOOP;
                    }
                }

                // 检查是否在路由分支内
                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null)
                        {
                            var childSteps = FlowStructureUtil.GetAllChildSteps(router.Children[i]);
                            if (childSteps.Any(s => s.Name == action.Name))
                            {
                                return StepLocationRelativeToParent.INSIDE_BRANCH;
                            }
                        }
                    }
                }
            }

            return StepLocationRelativeToParent.AFTER;
        }
    }
}
