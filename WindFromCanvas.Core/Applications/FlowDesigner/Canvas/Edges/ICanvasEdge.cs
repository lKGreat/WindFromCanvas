using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges
{
    /// <summary>
    /// 画布边缘接口
    /// </summary>
    public interface ICanvasEdge
    {
        /// <summary>
        /// 边缘ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 源节点ID
        /// </summary>
        string SourceId { get; }

        /// <summary>
        /// 目标节点ID
        /// </summary>
        string TargetId { get; }

        /// <summary>
        /// 绘制边缘
        /// </summary>
        void Draw(Graphics g, float zoom, PointF sourcePos, PointF targetPos);
    }
}
