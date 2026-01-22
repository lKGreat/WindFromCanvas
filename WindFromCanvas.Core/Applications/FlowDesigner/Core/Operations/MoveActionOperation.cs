using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 移动动作操作（支持撤销/重做）
    /// </summary>
    public class MoveActionOperation : IFlowOperation
    {
        private readonly MoveActionRequest _request;
        private string _originalParentStepName;
        private StepLocationRelativeToParent _originalLocation;
        private int? _originalBranchIndex;

        public MoveActionOperation(MoveActionRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public FlowVersion Execute(FlowVersion version)
        {
            if (version == null)
                return version;

            // 保存原始位置信息（用于撤销）
            // TODO: 从version中获取原始位置

            // 执行移动
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.MOVE_ACTION,
                Request = _request
            };

            return executor.Execute(version, operation);
        }

        public FlowVersion Undo(FlowVersion version)
        {
            if (version == null)
                return version;

            // 移动回原始位置
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.MOVE_ACTION,
                Request = new MoveActionRequest
                {
                    StepName = _request.StepName,
                    NewParentStepName = _originalParentStepName,
                    StepLocationRelativeToParent = _originalLocation,
                    BranchIndex = _originalBranchIndex
                }
            };

            return executor.Execute(version, operation);
        }

        public bool CanMerge(IFlowOperation other)
        {
            // 移动操作不支持合并
            return false;
        }

        public IFlowOperation Merge(IFlowOperation other)
        {
            throw new NotSupportedException("MoveActionOperation does not support merging");
        }
    }
}
