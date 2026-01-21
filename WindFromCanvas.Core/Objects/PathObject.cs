using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Objects
{
    public class PathObject : CanvasObject
    {
        public GraphicsPath Path { get; set; }
        public Color FillColor { get; set; } = Color.Black;
        public Color StrokeColor { get; set; } = Color.Black;
        public float StrokeWidth { get; set; } = 1f;
        public bool IsFilled { get; set; } = true;
        public bool IsStroked { get; set; } = true;

        public PathObject()
        {
            Path = new GraphicsPath();
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Path == null) return;

            if (IsFilled)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.FillPath(brush, Path);
                }
            }

            if (IsStroked && StrokeWidth > 0)
            {
                using (var pen = new Pen(StrokeColor, StrokeWidth))
                {
                    g.DrawPath(pen, Path);
                }
            }
        }

        public override bool HitTest(PointF point)
        {
            if (!Visible || Path == null) return false;
            
            using (var region = new Region(Path))
            {
                return region.IsVisible(point);
            }
        }

        public override RectangleF GetBounds()
        {
            if (Path == null) return RectangleF.Empty;
            return Path.GetBounds();
        }
    }
}
