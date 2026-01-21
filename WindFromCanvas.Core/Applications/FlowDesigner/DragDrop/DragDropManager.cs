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

        public event EventHandler<DragContext> DragStarted;
        public event EventHandler<DragContext> DragUpdated;
        public event EventHandler<DragContext> DragEnded;

        public DragDropManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _collisionDetector = new CollisionDetector();
            _dropTargets = new List<IDropTarget>();
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
        /// 取消拖拽
        /// </summary>
        public void CancelDrag()
        {
            _currentDrag = null;
            _stateStore.EndDrag();
        }

        /// <summary>
        /// 执行放置操作
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
