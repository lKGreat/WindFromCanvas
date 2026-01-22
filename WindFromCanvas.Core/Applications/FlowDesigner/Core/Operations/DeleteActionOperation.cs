using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 删除动作操作（支持撤销/重做）
    /// </summary>
    public class DeleteActionOperation : IFlowOperation
    {
        private readonly DeleteActionRequest _request;
        private FlowAction _deletedAction;
        private string _parentStepName;
        private StepLocationRelativeToParent _location;
        private int? _branchIndex;

        public DeleteActionOperation(DeleteActionRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public FlowVersion Execute(FlowVersion version)
        {
            if (version == null)
                return version;

            // 保存删除前的信息（用于撤销）
            var trigger = version.Trigger;
            var step = Utils.FlowStructureUtil.GetStep(_request.StepName, trigger);
            if (step is FlowAction action)
            {
                _deletedAction = action;
                // TODO: 保存父步骤和位置信息
            }

            // 执行删除
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.DELETE_ACTION,
                Request = _request
            };

            return executor.Execute(version, operation);
        }

        public FlowVersion Undo(FlowVersion version)
        {
            if (version == null || _deletedAction == null)
                return version;

            // 重新添加删除的动作
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_ACTION,
                Request = new AddActionRequest
                {
                    Action = _deletedAction,
                    ParentStepName = _parentStepName,
                    StepLocationRelativeToParent = _location,
                    BranchIndex = _branchIndex
                }
            };

            return executor.Execute(version, operation);
        }

        public bool CanMerge(IFlowOperation other)
        {
            // 删除操作不支持合并
            return false;
        }

        public IFlowOperation Merge(IFlowOperation other)
        {
            throw new NotSupportedException("DeleteActionOperation does not support merging");
        }
    }
}
