using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 添加动作操作（支持撤销/重做）
    /// </summary>
    public class AddActionOperation : IFlowOperation
    {
        private readonly AddActionRequest _request;
        private FlowAction _addedAction;
        private FlowAction _previousNextAction;
        private string _parentStepName;

        public AddActionOperation(AddActionRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public FlowVersion Execute(FlowVersion version)
        {
            if (version == null || _request.Action == null)
                return version;

            _addedAction = _request.Action;
            _parentStepName = _request.ParentStepName;

            // 执行FlowOperationExecutor的实际添加逻辑
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_ACTION,
                Request = _request
            };

            return executor.Execute(version, operation);
        }

        public FlowVersion Undo(FlowVersion version)
        {
            if (version == null || _addedAction == null)
                return version;

            // 删除添加的动作
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.DELETE_ACTION,
                Request = new DeleteActionRequest
                {
                    StepName = _addedAction.Name
                }
            };

            return executor.Execute(version, operation);
        }

        public bool CanMerge(IFlowOperation other)
        {
            // 添加操作不支持合并
            return false;
        }

        public IFlowOperation Merge(IFlowOperation other)
        {
            throw new NotSupportedException("AddActionOperation does not support merging");
        }
    }
}
