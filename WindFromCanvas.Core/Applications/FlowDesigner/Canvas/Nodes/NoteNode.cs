using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 备注节点
    /// </summary>
    public class NoteNode : BaseCanvasNode
    {
        public string Content { get; set; }
        public NoteColorVariant Color { get; set; }
        private SizeF _size;

        public override SizeF Size => _size;

        public NoteNode(string id) : base(id)
        {
            Selectable = true;
            Draggable = true;
            _size = new SizeF(200, 150);
            Color = NoteColorVariant.Blue;
        }

        public void SetSize(SizeF size)
        {
            _size = size;
        }

        public override void Draw(Graphics g, float zoom)
        {
            var bounds = Bounds;
            var color = NoteData.GetColor(Color); // NoteData.GetColor 返回 System.Drawing.Color

            // 绘制背景
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(240, color.R, color.G, color.B)))
            {
                var path = CreateRoundedRectangle(bounds, 4f / zoom);
                g.FillPath(brush, path);
            }

            // 绘制边框
            using (var pen = new Pen(color, 2f / zoom))
            {
                var path = CreateRoundedRectangle(bounds, 4f / zoom);
                g.DrawPath(pen, path);
            }

            // 绘制文本
            if (!string.IsNullOrEmpty(Content))
            {
                var textRect = new RectangleF(
                    bounds.X + 8f / zoom,
                    bounds.Y + 8f / zoom,
                    bounds.Width - 16f / zoom,
                    bounds.Height - 16f / zoom
                );

                using (var font = new Font("Microsoft YaHei", 11f / zoom))
                using (var brush = new SolidBrush(System.Drawing.Color.Black))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near
                    };
                    g.DrawString(Content, font, brush, textRect, format);
                }
            }

            // 绘制选中边框
            DrawSelectionBorder(g, bounds, zoom);
        }

        private GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
