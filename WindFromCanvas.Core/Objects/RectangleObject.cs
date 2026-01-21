using System;
using System.Drawing;

namespace WindFromCanvas.Core.Objects
{
    public class RectangleObject : CanvasObject
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public Color FillColor { get; set; } = Color.Black;
        public Color StrokeColor { get; set; } = Color.Black;
        public float StrokeWidth { get; set; } = 1f;
        public bool IsFilled { get; set; } = true;
        public bool IsStroked { get; set; } = true;

        public override void Draw(Graphics g)
        {
            if (!Visible) return;

            var rect = new RectangleF(X, Y, Width, Height);

            if (IsFilled)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.FillRectangle(brush, rect);
                }
            }

            if (IsStroked && StrokeWidth > 0)
            {
                using (var pen = new Pen(StrokeColor, StrokeWidth))
                {
                    g.DrawRectangle(pen, Rectangle.Round(rect));
                }
            }
        }

        public override bool HitTest(PointF point)
        {
            if (!Visible) return false;
            return point.X >= X && point.X <= X + Width &&
                   point.Y >= Y && point.Y <= Y + Height;
        }

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }
    }
}
