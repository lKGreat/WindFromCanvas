using System;
using System.Drawing;

namespace WindFromCanvas.Core.Objects
{
    public class EllipseObject : CanvasObject
    {
        public float RadiusX { get; set; }
        public float RadiusY { get; set; }
        public Color FillColor { get; set; } = Color.Black;
        public Color StrokeColor { get; set; } = Color.Black;
        public float StrokeWidth { get; set; } = 1f;
        public bool IsFilled { get; set; } = true;
        public bool IsStroked { get; set; } = true;

        public override void Draw(Graphics g)
        {
            if (!Visible) return;

            var rect = new RectangleF(X - RadiusX, Y - RadiusY, RadiusX * 2, RadiusY * 2);

            if (IsFilled)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.FillEllipse(brush, rect);
                }
            }

            if (IsStroked && StrokeWidth > 0)
            {
                using (var pen = new Pen(StrokeColor, StrokeWidth))
                {
                    g.DrawEllipse(pen, rect);
                }
            }
        }

        public override bool HitTest(PointF point)
        {
            if (!Visible) return false;
            
            // 椭圆方程: (px-cx)²/rx² + (py-cy)²/ry² <= 1
            float dx = point.X - X;
            float dy = point.Y - Y;
            float rx = RadiusX;
            float ry = RadiusY;
            
            if (rx <= 0 || ry <= 0) return false;
            
            float normalizedX = dx / rx;
            float normalizedY = dy / ry;
            return (normalizedX * normalizedX + normalizedY * normalizedY) <= 1.0f;
        }

        public override RectangleF GetBounds()
        {
            return new RectangleF(X - RadiusX, Y - RadiusY, RadiusX * 2, RadiusY * 2);
        }
    }
}
