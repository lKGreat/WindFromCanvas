using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 图结束节点（占位节点，用于布局计算）
    /// </summary>
    public class GraphEndNode : BaseCanvasNode
    {
        public override SizeF Size => LayoutConstants.NodeSize.GRAPH_END_WIDGET;
        public bool ShowWidget { get; set; }

        public GraphEndNode(string id) : base(id)
        {
            Selectable = false;
            Draggable = false;
            ShowWidget = false;
        }

        public override void Draw(Graphics g, float zoom)
        {
            // 图结束节点通常不绘制，仅用于布局计算
            if (ShowWidget)
            {
                // TODO: 绘制结束组件（如果需要）
            }
        }
    }
}
