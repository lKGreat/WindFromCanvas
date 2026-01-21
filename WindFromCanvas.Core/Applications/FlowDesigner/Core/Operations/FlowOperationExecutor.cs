using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

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
                case FlowOperationType.DUPLICATE_ACTION:
                    return ExecuteDuplicateAction(clonedVersion, operation);
                case FlowOperationType.ADD_BRANCH:
                    return ExecuteAddBranch(clonedVersion, operation);
                case FlowOperationType.DELETE_BRANCH:
                    return ExecuteDeleteBranch(clonedVersion, operation);
                case FlowOperationType.DUPLICATE_BRANCH:
                    return ExecuteDuplicateBranch(clonedVersion, operation);
                case FlowOperationType.MOVE_BRANCH:
                    return ExecuteMoveBranch(clonedVersion, operation);
                case FlowOperationType.SET_SKIP_ACTION:
                    return ExecuteSetSkipAction(clonedVersion, operation);
                case FlowOperationType.ADD_NOTE:
                    return ExecuteAddNote(clonedVersion, operation);
                case FlowOperationType.UPDATE_NOTE:
                    return ExecuteUpdateNote(clonedVersion, operation);
                case FlowOperationType.DELETE_NOTE:
                    return ExecuteDeleteNote(clonedVersion, operation);
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
            if (!(operation.Request is UpdateTriggerRequest request))
            {
                throw new ArgumentException("Invalid request type for UPDATE_TRIGGER");
            }

            // 更新触发器
            version.Trigger = request.Trigger;
            
            // 如果触发器有 NextAction，保持连接
            if (request.Trigger.NextAction != null)
            {
                version.Trigger.NextAction = request.Trigger.NextAction;
            }

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

        private FlowVersion ExecuteDuplicateAction(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is DuplicateStepRequest request))
            {
                throw new ArgumentException("Invalid request type for DUPLICATE_ACTION");
            }

            var stepToDuplicate = FlowStructureUtil.GetActionOrThrow(request.StepName, version.Trigger);
            
            // 深拷贝动作（使用 JSON 序列化）
            var serializer = new Serialization.FlowSerializer();
            var json = serializer.Serialize<FlowAction>(stepToDuplicate);
            var duplicatedAction = serializer.Deserialize<FlowAction>(json);
            
            // 生成新名称
            duplicatedAction.Name = FlowStructureUtil.FindUnusedName(version.Trigger);
            
            // 找到原动作的父步骤和位置
            var parentStepName = FindParentStepName(stepToDuplicate, version.Trigger);
            var location = GetCurrentLocation(stepToDuplicate, version.Trigger);
            int? branchIndex = null;
            
            if (location == StepLocationRelativeToParent.INSIDE_BRANCH)
            {
                branchIndex = GetBranchIndex(stepToDuplicate, version.Trigger);
            }

            // 添加到相同位置之后
            var addRequest = new AddActionRequest
            {
                Action = duplicatedAction,
                ParentStepName = parentStepName,
                StepLocationRelativeToParent = location,
                BranchIndex = branchIndex
            };
            
            return ExecuteAddAction(version, new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_ACTION,
                Request = addRequest
            });
        }

        private FlowVersion ExecuteAddBranch(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is AddBranchRequest request))
            {
                throw new ArgumentException("Invalid request type for ADD_BRANCH");
            }

            var routerStep = FlowStructureUtil.GetActionOrThrow(request.StepName, version.Trigger);
            if (!(routerStep is RouterAction router))
            {
                throw new ArgumentException($"Step '{request.StepName}' is not a router action");
            }

            // 创建新分支
            var newBranch = new RouterBranch
            {
                BranchName = request.BranchName,
                BranchType = BranchExecutionType.CONDITION,
                Conditions = request.Conditions ?? new List<List<BranchCondition>>()
            };

            // 添加到指定位置
            if (request.BranchIndex >= 0 && request.BranchIndex <= router.Settings.Branches.Count)
            {
                router.Settings.Branches.Insert(request.BranchIndex, newBranch);
                router.Children.Insert(request.BranchIndex, null);
            }
            else
            {
                router.Settings.Branches.Add(newBranch);
                router.Children.Add(null);
            }

            return version;
        }

        private FlowVersion ExecuteDeleteBranch(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is DeleteBranchRequest request))
            {
                throw new ArgumentException("Invalid request type for DELETE_BRANCH");
            }

            var routerStep = FlowStructureUtil.GetActionOrThrow(request.StepName, version.Trigger);
            if (!(routerStep is RouterAction router))
            {
                throw new ArgumentException($"Step '{request.StepName}' is not a router action");
            }

            if (request.BranchIndex < 0 || request.BranchIndex >= router.Settings.Branches.Count)
            {
                throw new ArgumentException($"Invalid branch index: {request.BranchIndex}");
            }

            // 删除分支
            router.Settings.Branches.RemoveAt(request.BranchIndex);
            router.Children.RemoveAt(request.BranchIndex);

            return version;
        }

        private FlowVersion ExecuteDuplicateBranch(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is DuplicateBranchRequest request))
            {
                throw new ArgumentException("Invalid request type for DUPLICATE_BRANCH");
            }

            var routerStep = FlowStructureUtil.GetActionOrThrow(request.StepName, version.Trigger);
            if (!(routerStep is RouterAction router))
            {
                throw new ArgumentException($"Step '{request.StepName}' is not a router action");
            }

            if (request.BranchIndex < 0 || request.BranchIndex >= router.Settings.Branches.Count)
            {
                throw new ArgumentException($"Invalid branch index: {request.BranchIndex}");
            }

            // 深拷贝分支（使用 JSON 序列化）
            var serializer = new Serialization.FlowSerializer();
            var branchJson = serializer.Serialize<RouterBranch>(router.Settings.Branches[request.BranchIndex]);
            var duplicatedBranch = serializer.Deserialize<RouterBranch>(branchJson);
            
            // 生成新分支名称
            var baseName = duplicatedBranch.BranchName;
            var counter = 1;
            while (router.Settings.Branches.Any(b => b.BranchName == $"{baseName} {counter}"))
            {
                counter++;
            }
            duplicatedBranch.BranchName = $"{baseName} {counter}";

            // 深拷贝子动作（如果有）
            FlowAction duplicatedChild = null;
            if (router.Children[request.BranchIndex] != null)
            {
                var childJson = serializer.Serialize<FlowAction>(router.Children[request.BranchIndex]);
                duplicatedChild = serializer.Deserialize<FlowAction>(childJson);
                duplicatedChild.Name = FlowStructureUtil.FindUnusedName(version.Trigger);
            }

            // 添加到指定位置之后
            var insertIndex = request.BranchIndex + 1;
            router.Settings.Branches.Insert(insertIndex, duplicatedBranch);
            router.Children.Insert(insertIndex, duplicatedChild);

            return version;
        }

        private FlowVersion ExecuteMoveBranch(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is MoveBranchRequest request))
            {
                throw new ArgumentException("Invalid request type for MOVE_BRANCH");
            }

            var routerStep = FlowStructureUtil.GetActionOrThrow(request.StepName, version.Trigger);
            if (!(routerStep is RouterAction router))
            {
                throw new ArgumentException($"Step '{request.StepName}' is not a router action");
            }

            if (request.SourceBranchIndex < 0 || request.SourceBranchIndex >= router.Settings.Branches.Count ||
                request.TargetBranchIndex < 0 || request.TargetBranchIndex >= router.Settings.Branches.Count)
            {
                throw new ArgumentException("Invalid branch index");
            }

            // 移动分支
            var branch = router.Settings.Branches[request.SourceBranchIndex];
            var child = router.Children[request.SourceBranchIndex];

            router.Settings.Branches.RemoveAt(request.SourceBranchIndex);
            router.Children.RemoveAt(request.SourceBranchIndex);

            router.Settings.Branches.Insert(request.TargetBranchIndex, branch);
            router.Children.Insert(request.TargetBranchIndex, child);

            return version;
        }

        private FlowVersion ExecuteSetSkipAction(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is SkipActionRequest request))
            {
                throw new ArgumentException("Invalid request type for SET_SKIP_ACTION");
            }

            foreach (var stepName in request.Names)
            {
                var step = FlowStructureUtil.GetStep(stepName, version.Trigger);
                if (step is FlowAction action)
                {
                    action.Skip = request.Skip;
                }
            }

            return version;
        }

        private FlowVersion ExecuteAddNote(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is AddNoteRequest request))
            {
                throw new ArgumentException("Invalid request type for ADD_NOTE");
            }

            if (version.Notes == null)
            {
                version.Notes = new List<Note>();
            }

            var note = new Note
            {
                Id = request.Id ?? Guid.NewGuid().ToString(),
                Content = request.Content,
                Position = request.Position,
                Size = request.Size,
                Color = request.Color
            };

            version.Notes.Add(note);
            return version;
        }

        private FlowVersion ExecuteUpdateNote(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is UpdateNoteRequest request))
            {
                throw new ArgumentException("Invalid request type for UPDATE_NOTE");
            }

            if (version.Notes == null)
            {
                throw new InvalidOperationException("No notes found");
            }

            var note = version.Notes.FirstOrDefault(n => n.Id == request.Id);
            if (note == null)
            {
                throw new InvalidOperationException($"Note '{request.Id}' not found");
            }

            note.Content = request.Content;
            note.Position = request.Position;
            note.Size = request.Size;
            note.Color = request.Color;
            note.UpdatedAt = DateTime.Now;

            return version;
        }

        private FlowVersion ExecuteDeleteNote(FlowVersion version, FlowOperationRequest operation)
        {
            if (!(operation.Request is DeleteNoteRequest request))
            {
                throw new ArgumentException("Invalid request type for DELETE_NOTE");
            }

            if (version.Notes == null)
            {
                return version;
            }

            version.Notes.RemoveAll(n => n.Id == request.Id);
            return version;
        }

        // 辅助方法
        private string FindParentStepName(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
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

                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null && loop.FirstLoopAction.Name == action.Name)
                {
                    return step.Name;
                }

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

        private StepLocationRelativeToParent GetCurrentLocation(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null)
                {
                    var childSteps = FlowStructureUtil.GetAllChildSteps(loop);
                    if (childSteps.Any(s => s.Name == action.Name))
                    {
                        return StepLocationRelativeToParent.INSIDE_LOOP;
                    }
                }

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

        private int? GetBranchIndex(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null && router.Children[i].Name == action.Name)
                        {
                            return i;
                        }
                    }
                }
            }

            return null;
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

    /// <summary>
    /// 更新触发器请求
    /// </summary>
    public class UpdateTriggerRequest
    {
        public FlowTrigger Trigger { get; set; }
    }

    /// <summary>
    /// 复制步骤请求
    /// </summary>
    public class DuplicateStepRequest
    {
        public string StepName { get; set; }
    }

    /// <summary>
    /// 添加分支请求
    /// </summary>
    public class AddBranchRequest
    {
        public string StepName { get; set; }
        public int BranchIndex { get; set; }
        public string BranchName { get; set; }
        public List<List<BranchCondition>> Conditions { get; set; }
    }

    /// <summary>
    /// 删除分支请求
    /// </summary>
    public class DeleteBranchRequest
    {
        public string StepName { get; set; }
        public int BranchIndex { get; set; }
    }

    /// <summary>
    /// 复制分支请求
    /// </summary>
    public class DuplicateBranchRequest
    {
        public string StepName { get; set; }
        public int BranchIndex { get; set; }
    }

    /// <summary>
    /// 移动分支请求
    /// </summary>
    public class MoveBranchRequest
    {
        public string StepName { get; set; }
        public int SourceBranchIndex { get; set; }
        public int TargetBranchIndex { get; set; }
    }

    /// <summary>
    /// 跳过动作请求
    /// </summary>
    public class SkipActionRequest
    {
        public List<string> Names { get; set; }
        public bool Skip { get; set; }
    }

    /// <summary>
    /// 添加备注请求
    /// </summary>
    public class AddNoteRequest
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public System.Drawing.PointF Position { get; set; }
        public System.Drawing.SizeF Size { get; set; }
        public NoteColorVariant Color { get; set; }
    }

    /// <summary>
    /// 更新备注请求
    /// </summary>
    public class UpdateNoteRequest
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public System.Drawing.PointF Position { get; set; }
        public System.Drawing.SizeF Size { get; set; }
        public NoteColorVariant Color { get; set; }
    }

    /// <summary>
    /// 删除备注请求
    /// </summary>
    public class DeleteNoteRequest
    {
        public string Id { get; set; }
    }
}
