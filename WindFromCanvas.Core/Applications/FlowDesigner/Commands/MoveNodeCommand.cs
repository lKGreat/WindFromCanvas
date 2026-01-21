using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Commands
{
    /// <summary>
    /// 移动节点命令
    /// </summary>
    public class MoveNodeCommand : ICommand
    {
        private FlowDesignerCanvas _canvas;
        private FlowNode _node;
        private PointF _oldPosition;
        private PointF _newPosition;
        private bool _executed;

        public string Description => $"移动节点: {_node?.Data?.DisplayName ?? "未知"}";

        public MoveNodeCommand(FlowDesignerCanvas canvas, FlowNode node, PointF oldPosition, PointF newPosition)
        {
            _canvas = canvas;
            _node = node;
            _oldPosition = oldPosition;
            _newPosition = newPosition;
            _executed = false;
        }

        public void Execute()
        {
            if (_executed || _node == null) return;

            _node.X = _newPosition.X;
            _node.Y = _newPosition.Y;
            _node.UpdatePosition();
            _canvas.UpdateConnectionsForNode(_node);
            _canvas.Invalidate();
            _executed = true;
        }

        public void Undo()
        {
            if (!_executed || _node == null) return;

            _node.X = _oldPosition.X;
            _node.Y = _oldPosition.Y;
            _node.UpdatePosition();
            _canvas.UpdateConnectionsForNode(_node);
            _canvas.Invalidate();
            _executed = false;
        }
    }
}
