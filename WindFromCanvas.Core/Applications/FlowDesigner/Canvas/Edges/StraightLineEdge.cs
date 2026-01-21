using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges
{
    /// <summary>
    /// 直线边缘（匹配 Activepieces StraightLineEdge）
    /// </summary>
    public class StraightLineEdge : ICanvasEdge
    {
        public string Id { get; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public bool DrawArrowHead { get; set; }
        public bool HideAddButton { get; set; }
        public string ParentStepName { get; set; }

        public StraightLineEdge(string id)
        {
            Id = id;
            DrawArrowHead = true;
            HideAddButton = false;
        }

        public void Draw(Graphics g, float zoom, PointF sourcePos, PointF targetPos)
        {
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), Layout.LayoutConstants.LINE_WIDTH / zoom))
            {
                // 绘制直线
                g.DrawLine(pen, sourcePos.X, sourcePos.Y, targetPos.X, targetPos.Y);

                // 绘制箭头
                if (DrawArrowHead)
                {
                    DrawArrowHeadShape(g, sourcePos, targetPos, zoom);
                }
            }

            // 绘制添加按钮（如果不需要隐藏）
            if (!HideAddButton)
            {
                DrawAddButton(g, sourcePos, targetPos, zoom);
            }
        }

        /// <summary>
        /// 绘制箭头形状
        /// </summary>
        private void DrawArrowHeadShape(Graphics g, PointF source, PointF target, float zoom)
        {
            var angle = Math.Atan2(target.Y - source.Y, target.X - source.X);
            var arrowLength = 8f / zoom;
            var arrowWidth = 6f / zoom;

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

        /// <summary>
        /// 绘制添加按钮
        /// </summary>
        private void DrawAddButton(Graphics g, PointF source, PointF target, float zoom)
        {
            var midX = (source.X + target.X) / 2;
            var midY = (source.Y + target.Y) / 2;
            var buttonSize = Layout.LayoutConstants.NodeSize.ADD_BUTTON.Width / zoom;
            var buttonRect = new RectangleF(
                midX - buttonSize / 2,
                midY - buttonSize / 2,
                buttonSize,
                buttonSize
            );

            // 绘制圆形背景
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, buttonRect);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1f / zoom))
            {
                g.DrawEllipse(pen, buttonRect);
            }

            // 绘制加号
            var lineLength = buttonSize * 0.4f;
            var lineWidth = 2f / zoom;
            using (var pen = new Pen(Color.FromArgb(100, 100, 100), lineWidth))
            {
                g.DrawLine(pen, midX - lineLength / 2, midY, midX + lineLength / 2, midY);
                g.DrawLine(pen, midX, midY - lineLength / 2, midX, midY + lineLength / 2);
            }
        }
    }
}
