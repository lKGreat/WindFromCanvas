using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Commands
{
    /// <summary>
    /// 流程操作命令（包装 FlowOperation 以支持撤销/重做）
    /// </summary>
    public class FlowOperationCommand : ICommand
    {
        private readonly BuilderStateStore _stateStore;
        private readonly FlowOperationRequest _operation;
        private FlowVersion _previousVersion;
        private FlowVersion _newVersion;

        public string Description { get; private set; }

        public FlowOperationCommand(BuilderStateStore stateStore, FlowOperationRequest operation)
        {
            _stateStore = stateStore;
            _operation = operation;
            Description = GetOperationDescription(operation);
        }

        public void Execute()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;

            // 保存当前版本
            _previousVersion = CloneFlowVersion(_stateStore.Flow.FlowVersion);

            // 执行操作
            var executor = new FlowOperationExecutor();
            _newVersion = executor.Execute(_stateStore.Flow.FlowVersion, _operation);

            // 更新状态
            _stateStore.Flow.FlowVersion = _newVersion;

            // 通知监听器
            foreach (var listener in _stateStore.Flow.OperationListeners)
            {
                listener(_newVersion, _operation);
            }

            // 触发属性变化事件
            _stateStore.OnPropertyChanged(nameof(BuilderStateStore.Flow));
        }

        public void Undo()
        {
            if (_previousVersion == null) return;

            // 恢复之前的版本（不记录命令，避免循环）
            var executor = new FlowOperationExecutor();
            _stateStore.Flow.FlowVersion = CloneFlowVersion(_previousVersion);
            
            // 通知监听器
            foreach (var listener in _stateStore.Flow.OperationListeners)
            {
                listener(_stateStore.Flow.FlowVersion, _operation);
            }
            
            // 触发属性变化事件
            _stateStore.OnPropertyChanged(nameof(BuilderStateStore.Flow));
        }

        private FlowVersion CloneFlowVersion(FlowVersion version)
        {
            var serializer = new FlowSerializer();
            var json = serializer.Serialize(version);
            return serializer.Deserialize(json);
        }

        private string GetOperationDescription(FlowOperationRequest operation)
        {
            switch (operation.Type)
            {
                case Core.Enums.FlowOperationType.ADD_ACTION:
                    return "添加动作";
                case Core.Enums.FlowOperationType.DELETE_ACTION:
                    return "删除动作";
                case Core.Enums.FlowOperationType.MOVE_ACTION:
                    return "移动动作";
                case Core.Enums.FlowOperationType.UPDATE_ACTION:
                    return "更新动作";
                case Core.Enums.FlowOperationType.UPDATE_TRIGGER:
                    return "更新触发器";
                case Core.Enums.FlowOperationType.CHANGE_NAME:
                    return "更改名称";
                default:
                    return "操作";
            }
        }
    }
}
