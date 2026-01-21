using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 循环返回节点（用于布局计算）
    /// </summary>
    public class LoopReturnNode : BaseCanvasNode
    {
        public override SizeF Size => LayoutConstants.NodeSize.LOOP_RETURN_NODE;

        public LoopReturnNode(string id) : base(id)
        {
            Selectable = false;
            Draggable = false;
        }

        public override void Draw(Graphics g, float zoom)
        {
            // 循环返回节点通常不绘制，仅用于布局计算
        }
    }
}
