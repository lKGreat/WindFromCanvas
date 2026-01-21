using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Commands
{
    /// <summary>
    /// 添加节点命令
    /// </summary>
    public class AddNodeCommand : ICommand
    {
        private FlowDesignerCanvas _canvas;
        private FlowNode _node;
        private bool _executed;

        public string Description => $"添加节点: {_node?.Data?.DisplayName ?? "未知"}";

        public AddNodeCommand(FlowDesignerCanvas canvas, FlowNode node)
        {
            _canvas = canvas;
            _node = node;
            _executed = false;
        }

        public void Execute()
        {
            if (_executed || _node == null) return;

            _canvas.AddNodeInternal(_node);
            if (_node.Data != null)
            {
                _canvas.Document.Nodes.Add(_node.Data);
            }
            _executed = true;
        }

        public void Undo()
        {
            if (!_executed || _node == null) return;

            _canvas.RemoveNode(_node);
            if (_node.Data != null)
            {
                _canvas.Document.Nodes.Remove(_node.Data);
            }
            _executed = false;
        }
    }
}
