using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
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
            // TODO: 实现添加动作逻辑
            return version;
        }

        private FlowVersion ExecuteDeleteAction(FlowVersion version, FlowOperationRequest operation)
        {
            // TODO: 实现删除动作逻辑
            return version;
        }

        private FlowVersion ExecuteMoveAction(FlowVersion version, FlowOperationRequest operation)
        {
            // TODO: 实现移动动作逻辑
            return version;
        }

        private FlowVersion ExecuteUpdateAction(FlowVersion version, FlowOperationRequest operation)
        {
            // TODO: 实现更新动作逻辑
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
}
