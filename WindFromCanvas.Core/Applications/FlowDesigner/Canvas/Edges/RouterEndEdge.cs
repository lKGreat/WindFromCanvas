using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges
{
    /// <summary>
    /// 路由结束边缘（匹配 Activepieces RouterEndEdge）
    /// </summary>
    public class RouterEndEdge : ICanvasEdge
    {
        public string Id { get; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public bool DrawHorizontalLine { get; set; }
        public bool DrawEndingVerticalLine { get; set; }
        public float VerticalSpaceBetweenLastNodeInBranchAndEndLine { get; set; }
        public string RouterOrBranchStepName { get; set; }
        public bool IsNextStepEmpty { get; set; }

        public RouterEndEdge(string id)
        {
            Id = id;
        }

        public void Draw(Graphics g, float zoom, PointF sourcePos, PointF targetPos)
        {
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), LayoutConstants.LINE_WIDTH / zoom))
            {
                // 绘制从分支节点到结束线的垂直线
                var endY = sourcePos.Y + VerticalSpaceBetweenLastNodeInBranchAndEndLine / zoom;
                g.DrawLine(pen, sourcePos.X, sourcePos.Y, sourcePos.X, endY);

                // 绘制结束垂直线
                if (DrawEndingVerticalLine)
                {
                    g.DrawLine(pen, targetPos.X, endY, targetPos.X, targetPos.Y);
                }

                // 绘制水平汇合线
                if (DrawHorizontalLine)
                {
                    g.DrawLine(pen, sourcePos.X, endY, targetPos.X, endY);
                }
            }

            // 如果需要，绘制箭头
            if (!IsNextStepEmpty)
            {
                DrawArrowHead(g, new PointF(targetPos.X, targetPos.Y - 10), targetPos, zoom);
            }
        }

        private void DrawArrowHead(Graphics g, PointF source, PointF target, float zoom)
        {
            var angle = Math.Atan2(target.Y - source.Y, target.X - source.X);
            var arrowLength = 8f / zoom;

            var arrowPoint1 = new PointF(
                (float)(target.X - arrowLength * Math.Cos(angle - Math.PI / 6)),
                (float)(target.Y - arrowLength * Math.Sin(angle - Math.PI / 6))
            );
            var arrowPoint2 = new PointF(
                (float)(target.X - arrowLength * Math.Cos(angle + Math.PI / 6)),
                (float)(target.Y - arrowLength * Math.Sin(angle + Math.PI / 6))
            );

            using (var brush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                var points = new[] { target, arrowPoint1, arrowPoint2 };
                g.FillPolygon(brush, points);
            }
        }
    }
}
