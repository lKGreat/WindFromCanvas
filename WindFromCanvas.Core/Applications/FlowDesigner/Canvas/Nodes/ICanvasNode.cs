using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 画布节点接口
    /// </summary>
    public interface ICanvasNode
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 节点位置
        /// </summary>
        PointF Position { get; set; }

        /// <summary>
        /// 节点大小
        /// </summary>
        SizeF Size { get; }

        /// <summary>
        /// 节点边界矩形
        /// </summary>
        RectangleF Bounds { get; }

        /// <summary>
        /// 是否可选中
        /// </summary>
        bool Selectable { get; set; }

        /// <summary>
        /// 是否可拖拽
        /// </summary>
        bool Draggable { get; set; }

        /// <summary>
        /// 是否被选中
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// 绘制节点
        /// </summary>
        void Draw(Graphics g, float zoom);

        /// <summary>
        /// 检查点是否在节点内
        /// </summary>
        bool Contains(PointF point);
    }
}
