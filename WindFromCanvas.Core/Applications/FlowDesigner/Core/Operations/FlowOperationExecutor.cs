using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 流程操作执行器（匹配 Activepieces flowOperations.apply）
    /// </summary>
    public class FlowOperationExecutor
    {
        /// <summary>
        /// 执行操作
        /// </summary>
        public FlowVersion Execute(FlowVersion flowVersion, FlowOperationRequest operation)
        {
            // 深拷贝流程版本
            var clonedVersion = CloneFlowVersion(flowVersion);

            switch (operation.Type)
            {
                case FlowOperationType.ADD_ACTION:
                    return ExecuteAddAction(clonedVersion, operation);
                case FlowOperationType.DELETE_ACTION:
                    return ExecuteDeleteAction(clonedVersion, operation);
                case FlowOperationType.MOVE_ACTION:
                    return ExecuteMoveAction(clonedVersion, operation);
                case FlowOperationType.UPDATE_ACTION:
                    return ExecuteUpdateAction(clonedVersion, operation);
                case FlowOperationType.UPDATE_TRIGGER:
                    return ExecuteUpdateTrigger(clonedVersion, operation);
                case FlowOperationType.CHANGE_NAME:
                    return ExecuteChangeName(clonedVersion, operation);
                // TODO: 实现其他操作类型
                default:
                    throw new NotSupportedException($"Operation type {operation.Type} is not supported");
            }
        }

        private FlowVersion CloneFlowVersion(FlowVersion version)
        {
            // 简单的序列化/反序列化克隆
            var serializer = new Serialization.FlowSerializer();
            var json = serializer.Serialize(version);
            return serializer.Deserialize(json);
        }

        private FlowVersion ExecuteAddAction(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is AddActionRequest request))
            {
                throw new ArgumentException("Invalid request type for ADD_ACTION");
            }

            var action = request.Action;
            if (action == null)
            {
                throw new ArgumentException("Action cannot be null");
            }

            // 生成唯一名称
            if (string.IsNullOrEmpty(action.Name))
            {
                action.Name = FlowStructureUtil.FindUnusedName(version.Trigger);
            }

            // 根据插入位置添加动作
            if (request.StepLocationRelativeToParent == StepLocationRelativeToParent.AFTER)
            {
                // 添加到指定步骤之后
                var parentStep = FlowStructureUtil.GetStepOrThrow(request.ParentStepName, version.Trigger);
                if (parentStep is FlowAction parentAction)
                {
                    action.NextAction = parentAction.NextAction;
                    parentAction.NextAction = action;
                }
                else if (parentStep is FlowTrigger parentTrigger)
                {
                    action.NextAction = parentTrigger.NextAction;
                    parentTrigger.NextAction = action;
                }
            }
            else if (request.StepLocationRelativeToParent == StepLocationRelativeToParent.INSIDE_LOOP)
            {
                // 添加到循环内
                var loopStep = FlowStructureUtil.GetActionOrThrow(request.ParentStepName, version.Trigger);
                if (!(loopStep is LoopOnItemsAction loop))
                {
                    throw new ArgumentException($"Step '{request.ParentStepName}' is not a loop action");
                }
                action.NextAction = loop.FirstLoopAction;
                loop.FirstLoopAction = action;
            }
            else if (request.StepLocationRelativeToParent == StepLocationRelativeToParent.INSIDE_BRANCH)
            {
                // 添加到路由分支内
                if (!request.BranchIndex.HasValue)
                {
                    throw new ArgumentException("BranchIndex is required for INSIDE_BRANCH");
                }
                var routerStep = FlowStructureUtil.GetActionOrThrow(request.ParentStepName, version.Trigger);
                if (!(routerStep is RouterAction router))
                {
                    throw new ArgumentException($"Step '{request.ParentStepName}' is not a router action");
                }
                var branchIndex = request.BranchIndex.Value;
                if (branchIndex < 0 || branchIndex >= router.Children.Count)
                {
                    throw new ArgumentException($"Invalid branch index: {branchIndex}");
                }
                action.NextAction = router.Children[branchIndex];
                router.Children[branchIndex] = action;
            }

            return version;
        }

        private FlowVersion ExecuteDeleteAction(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is DeleteActionRequest request))
            {
                throw new ArgumentException("Invalid request type for DELETE_ACTION");
            }

            var stepToDelete = FlowStructureUtil.GetStepOrThrow(request.StepName, version.Trigger);
            
            // 不能删除触发器
            if (stepToDelete is FlowTrigger)
            {
                throw new InvalidOperationException("Cannot delete trigger");
            }

            // 查找父步骤并删除连接
            var allSteps = FlowStructureUtil.GetAllSteps(version.Trigger);
            foreach (var step in allSteps)
            {
                // 检查是否是 nextAction
                FlowAction nextAction = null;
                if (step is FlowAction action)
                {
                    nextAction = action.NextAction;
                }
                else if (step is FlowTrigger trigger)
                {
                    nextAction = trigger.NextAction;
                }

                if (nextAction != null && nextAction.Name == request.StepName)
                {
                    // 找到父步骤，更新 nextAction
                    if (step is FlowAction parentAction)
                    {
                        parentAction.NextAction = nextAction.NextAction;
                    }
                    else if (step is FlowTrigger parentTrigger)
                    {
                        parentTrigger.NextAction = nextAction.NextAction;
                    }
                    return version;
                }

                // 检查是否在循环内
                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null && loop.FirstLoopAction.Name == request.StepName)
                {
                    loop.FirstLoopAction = ((FlowAction)stepToDelete).NextAction;
                    return version;
                }

                // 检查是否在路由分支内
                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null && router.Children[i].Name == request.StepName)
                        {
                            router.Children[i] = ((FlowAction)stepToDelete).NextAction;
                            return version;
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Could not find parent of step '{request.StepName}'");
        }

        private FlowVersion ExecuteMoveAction(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is MoveActionRequest request))
            {
                throw new ArgumentException("Invalid request type for MOVE_ACTION");
            }

            // 先删除动作（但保留引用）
            var stepToMove = FlowStructureUtil.GetActionOrThrow(request.StepName, version.Trigger);
            var nextAction = stepToMove.NextAction;

            // 删除动作
            var deleteRequest = new FlowOperationRequest
            {
                Type = FlowOperationType.DELETE_ACTION,
                Request = new DeleteActionRequest { StepName = request.StepName }
            };
            version = ExecuteDeleteAction(version, deleteRequest);

            // 在新位置添加动作
            stepToMove.NextAction = nextAction; // 恢复原来的 nextAction
            var addRequest = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_ACTION,
                Request = new AddActionRequest
                {
                    Action = stepToMove,
                    ParentStepName = request.NewParentStepName,
                    StepLocationRelativeToParent = request.StepLocationRelativeToParent,
                    BranchIndex = request.BranchIndex
                }
            };
            return ExecuteAddAction(version, addRequest);
        }

        private FlowVersion ExecuteUpdateAction(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is UpdateActionRequest request))
            {
                throw new ArgumentException("Invalid request type for UPDATE_ACTION");
            }

            var step = FlowStructureUtil.GetStepOrThrow(request.StepName, version.Trigger);
            if (!(step is FlowAction))
            {
                throw new ArgumentException($"Step '{request.StepName}' is not an action");
            }

            if (request.UpdatedAction != null)
            {
                // 更新动作属性
                var action = (FlowAction)step;
                action.DisplayName = request.UpdatedAction.DisplayName;
                action.Skip = request.UpdatedAction.Skip;
                action.Valid = request.UpdatedAction.Valid;
                
                // 更新类型特定的属性
                if (request.UpdatedAction is CodeAction codeAction && action is CodeAction existingCode)
                {
                    existingCode.Settings = codeAction.Settings;
                }
                else if (request.UpdatedAction is PieceAction pieceAction && action is PieceAction existingPiece)
                {
                    existingPiece.Settings = pieceAction.Settings;
                }
                else if (request.UpdatedAction is LoopOnItemsAction loopAction && action is LoopOnItemsAction existingLoop)
                {
                    existingLoop.Settings = loopAction.Settings;
                    existingLoop.FirstLoopAction = loopAction.FirstLoopAction;
                }
                else if (request.UpdatedAction is RouterAction routerAction && action is RouterAction existingRouter)
                {
                    existingRouter.Settings = routerAction.Settings;
                    existingRouter.Children = routerAction.Children;
                }
            }

            return version;
        }

        private FlowVersion ExecuteUpdateTrigger(FlowVersion version, FlowOperationRequest operation)
        {
            // TODO: 实现更新触发器逻辑
            return version;
        }

        private FlowVersion ExecuteChangeName(FlowVersion version, FlowOperationRequest operation)
        {
            if (operation.Request is ChangeNameRequest request)
            {
                version.DisplayName = request.DisplayName;
            }
            return version;
        }
    }

    /// <summary>
    /// 更改名称请求
    /// </summary>
    public class ChangeNameRequest
    {
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// 添加动作请求
    /// </summary>
    public class AddActionRequest
    {
        public FlowAction Action { get; set; }
        public string ParentStepName { get; set; }
        public StepLocationRelativeToParent StepLocationRelativeToParent { get; set; }
        public int? BranchIndex { get; set; }
    }

    /// <summary>
    /// 删除动作请求
    /// </summary>
    public class DeleteActionRequest
    {
        public string StepName { get; set; }
    }

    /// <summary>
    /// 移动动作请求
    /// </summary>
    public class MoveActionRequest
    {
        public string StepName { get; set; }
        public string NewParentStepName { get; set; }
        public StepLocationRelativeToParent StepLocationRelativeToParent { get; set; }
        public int? BranchIndex { get; set; }
    }

    /// <summary>
    /// 更新动作请求
    /// </summary>
    public class UpdateActionRequest
    {
        public string StepName { get; set; }
        public FlowAction UpdatedAction { get; set; }
    }
}
