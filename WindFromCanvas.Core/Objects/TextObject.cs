using System;
using System.Drawing;

namespace WindFromCanvas.Core.Objects
{
    public enum TextAlign
    {
        Left,
        Center,
        Right
    }

    public enum TextBaseline
    {
        Top,
        Middle,
        Bottom,
        Alphabetic
    }

    public class TextObject : CanvasObject
    {
        public string Text { get; set; }
        public Font Font { get; set; } = new Font("Arial", 12f);
        public TextAlign TextAlign { get; set; } = TextAlign.Left;
        public TextBaseline TextBaseline { get; set; } = TextBaseline.Top;
        public Color FillColor { get; set; } = Color.Black;
        public Color StrokeColor { get; set; } = Color.Black;
        public float StrokeWidth { get; set; } = 1f;
        public bool IsFilled { get; set; } = true;
        public bool IsStroked { get; set; } = false;

        public override void Draw(Graphics g)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || Font == null) return;

            var bounds = GetBounds();
            var format = new StringFormat();
            
            switch (TextAlign)
            {
                case TextAlign.Left:
                    format.Alignment = StringAlignment.Near;
                    break;
                case TextAlign.Center:
                    format.Alignment = StringAlignment.Center;
                    break;
                case TextAlign.Right:
                    format.Alignment = StringAlignment.Far;
                    break;
            }

            switch (TextBaseline)
            {
                case TextBaseline.Top:
                    format.LineAlignment = StringAlignment.Near;
                    break;
                case TextBaseline.Middle:
                    format.LineAlignment = StringAlignment.Center;
                    break;
                case TextBaseline.Bottom:
                case TextBaseline.Alphabetic:
                    format.LineAlignment = StringAlignment.Far;
                    break;
            }

            if (IsFilled)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.DrawString(Text, Font, brush, bounds, format);
                }
            }

            if (IsStroked && StrokeWidth > 0)
            {
                using (var pen = new Pen(StrokeColor, StrokeWidth))
                {
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddString(Text, Font.FontFamily, (int)Font.Style, Font.Size, bounds, format);
                        g.DrawPath(pen, path);
                    }
                }
            }
        }

        public override bool HitTest(PointF point)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || Font == null) return false;
            
            var bounds = GetBounds();
            return bounds.Contains(point);
        }

        public override RectangleF GetBounds()
        {
            if (string.IsNullOrEmpty(Text) || Font == null)
                return new RectangleF(X, Y, 0, 0);
            
            // 使用临时Graphics测量文本大小
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                var size = g.MeasureString(Text, Font);
                float x = X;
                
                switch (TextAlign)
                {
                    case TextAlign.Center:
                        x -= size.Width / 2;
                        break;
                    case TextAlign.Right:
                        x -= size.Width;
                        break;
                }
                
                float y = Y;
                switch (TextBaseline)
                {
                    case TextBaseline.Middle:
                        y -= size.Height / 2;
                        break;
                    case TextBaseline.Bottom:
                    case TextBaseline.Alphabetic:
                        y -= size.Height;
                        break;
                }
                
                return new RectangleF(x, y, size.Width, size.Height);
            }
        }
    }
}
