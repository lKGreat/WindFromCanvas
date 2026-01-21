using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Styles;

namespace WindFromCanvas.Core.Rendering
{
    public class DrawingState
    {
        public Matrix Transform { get; set; }
        public IFillStyle FillStyle { get; set; }
        public IFillStyle StrokeStyle { get; set; }
        public float LineWidth { get; set; }
        public float GlobalAlpha { get; set; }
        public Font Font { get; set; }
        public TextAlign TextAlign { get; set; }
        public TextBaseline TextBaseline { get; set; }

        public DrawingState Clone()
        {
            return new DrawingState
            {
                Transform = Transform != null ? Transform.Clone() as Matrix : null,
                FillStyle = FillStyle,
                StrokeStyle = StrokeStyle,
                LineWidth = LineWidth,
                GlobalAlpha = GlobalAlpha,
                Font = Font != null ? (Font)Font.Clone() : null,
                TextAlign = TextAlign,
                TextBaseline = TextBaseline
            };
        }
    }
}
