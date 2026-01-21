using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges
{
    /// <summary>
    /// 路由开始边缘（匹配 Activepieces RouterStartEdge）
    /// </summary>
    public class RouterStartEdge : ICanvasEdge
    {
        public string Id { get; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public bool IsBranchEmpty { get; set; }
        public string Label { get; set; }
        public bool DrawHorizontalLine { get; set; }
        public bool DrawStartingVerticalLine { get; set; }
        public int BranchIndex { get; set; }

        public RouterStartEdge(string id)
        {
            Id = id;
        }

        public void Draw(Graphics g, float zoom, PointF sourcePos, PointF targetPos)
        {
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), LayoutConstants.LINE_WIDTH / zoom))
            {
                // 绘制起始垂直线
                if (DrawStartingVerticalLine)
                {
                    g.DrawLine(pen, sourcePos.X, sourcePos.Y, sourcePos.X, targetPos.Y);
                }

                // 绘制水平线
                if (DrawHorizontalLine)
                {
                    g.DrawLine(pen, sourcePos.X, targetPos.Y, targetPos.X, targetPos.Y);
                }

                // 绘制到目标节点的线
                g.DrawLine(pen, targetPos.X, targetPos.Y, targetPos.X, targetPos.Y);
            }

            // 绘制分支标签
            if (!string.IsNullOrEmpty(Label))
            {
                DrawLabel(g, sourcePos, targetPos, Label, zoom);
            }
        }

        private void DrawLabel(Graphics g, PointF source, PointF target, string label, float zoom)
        {
            var labelX = (source.X + target.X) / 2;
            var labelY = target.Y - LayoutConstants.LABEL_HEIGHT / zoom / 2;

            using (var font = new Font("Microsoft YaHei", 10f / zoom))
            using (var brush = new SolidBrush(Color.Black))
            {
                var size = g.MeasureString(label, font);
                var rect = new RectangleF(
                    labelX - size.Width / 2,
                    labelY - size.Height / 2,
                    size.Width,
                    size.Height
                );

                // 绘制标签背景
                using (var bgBrush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bgBrush, rect);
                }

                // 绘制标签文本
                g.DrawString(label, font, brush, rect);
            }
        }
    }
}
