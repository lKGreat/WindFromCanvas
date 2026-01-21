using System.Drawing;

namespace WindFromCanvas.Core.Styles
{
    public class SolidColorStyle : IFillStyle
    {
        public Color Color { get; set; }

        public SolidColorStyle(Color color)
        {
            Color = color;
        }

        public Brush CreateBrush(RectangleF bounds)
        {
            return new SolidBrush(Color);
        }

        public static implicit operator SolidColorStyle(Color color)
        {
            return new SolidColorStyle(color);
        }
    }
}
