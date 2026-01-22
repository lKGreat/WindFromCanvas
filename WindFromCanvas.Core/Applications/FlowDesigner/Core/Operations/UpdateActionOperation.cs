using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 更新动作操作（支持撤销/重做和命令合并）
    /// </summary>
    public class UpdateActionOperation : IFlowOperation
    {
        private readonly UpdateActionRequest _request;
        private FlowAction _originalAction;
        private DateTime _timestamp;

        public UpdateActionOperation(UpdateActionRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _timestamp = DateTime.Now;
        }

        public FlowVersion Execute(FlowVersion version)
        {
            if (version == null)
                return version;

            // 保存原始动作（用于撤销）
            var trigger = version.Trigger;
            var step = Utils.FlowStructureUtil.GetStep(_request.StepName, trigger);
            if (step is FlowAction action)
            {
                // 深拷贝原始动作
                _originalAction = CloneAction(action);
            }

            // 执行更新
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.UPDATE_ACTION,
                Request = _request
            };

            return executor.Execute(version, operation);
        }

        public FlowVersion Undo(FlowVersion version)
        {
            if (version == null || _originalAction == null)
                return version;

            // 恢复到原始动作
            var executor = new FlowOperationExecutor();
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.UPDATE_ACTION,
                Request = new UpdateActionRequest
                {
                    StepName = _request.StepName,
                    UpdatedAction = _originalAction
                }
            };

            return executor.Execute(version, operation);
        }

        /// <summary>
        /// 4.1.5 检查是否可以合并（同一个动作的连续更新）
        /// </summary>
        public bool CanMerge(IFlowOperation other)
        {
            if (!(other is UpdateActionOperation otherUpdate))
                return false;

            // 只有相同动作的更新才能合并
            if (_request.StepName != otherUpdate._request.StepName)
                return false;

            // 时间间隔小于1秒才合并（防抖）
            var timeDiff = (otherUpdate._timestamp - _timestamp).TotalMilliseconds;
            return timeDiff < 1000;
        }

        /// <summary>
        /// 4.1.5 合并操作（保留最新的更新，但保持原始状态）
        /// </summary>
        public IFlowOperation Merge(IFlowOperation other)
        {
            if (!(other is UpdateActionOperation otherUpdate))
                throw new InvalidOperationException("Cannot merge with different operation type");

            // 创建新的合并操作，保留原始状态但应用最新的更新
            var mergedOperation = new UpdateActionOperation(_request)
            {
                _originalAction = this._originalAction, // 保留最早的原始状态
                _timestamp = otherUpdate._timestamp      // 使用最新的时间戳
            };

            // 更新请求为最新的
            var mergedRequest = otherUpdate._request;
            return new UpdateActionOperation(mergedRequest)
            {
                _originalAction = this._originalAction,
                _timestamp = otherUpdate._timestamp
            };
        }

        /// <summary>
        /// 深拷贝动作
        /// </summary>
        private FlowAction CloneAction(FlowAction action)
        {
            // 简单实现，实际应该使用深拷贝
            // TODO: 实现完整的深拷贝逻辑
            return action;
        }
    }
}
