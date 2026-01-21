using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges
{
    /// <summary>
    /// 循环开始边缘（匹配 Activepieces LoopStartEdge）
    /// </summary>
    public class LoopStartEdge : ICanvasEdge
    {
        public string Id { get; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public bool IsLoopEmpty { get; set; }

        public LoopStartEdge(string id)
        {
            Id = id;
        }

        public void Draw(Graphics g, float zoom, PointF sourcePos, PointF targetPos)
        {
            // 绘制从循环节点到第一个子节点的弧线
            var arcLength = LayoutConstants.ARC_LENGTH / zoom;
            var midX = (sourcePos.X + targetPos.X) / 2;
            var controlY = sourcePos.Y + arcLength * 2;

            using (var path = new GraphicsPath())
            {
                path.AddBezier(
                    sourcePos.X, sourcePos.Y,
                    sourcePos.X, sourcePos.Y + arcLength,
                    midX, controlY,
                    targetPos.X, targetPos.Y
                );

                using (var pen = new Pen(Color.FromArgb(200, 200, 200), LayoutConstants.LINE_WIDTH / zoom))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
    }
}
