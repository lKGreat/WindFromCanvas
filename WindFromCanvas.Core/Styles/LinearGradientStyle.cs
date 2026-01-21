using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Styles
{
    public class LinearGradientStyle : IFillStyle
    {
        public PointF Start { get; set; }
        public PointF End { get; set; }
        private List<(float offset, Color color)> _colorStops = new List<(float, Color)>();

        public void AddColorStop(float offset, Color color)
        {
            _colorStops.Add((offset, color));
        }

        public Brush CreateBrush(RectangleF bounds)
        {
            if (_colorStops.Count == 0)
                return new SolidBrush(Color.Transparent);

            if (_colorStops.Count == 1)
                return new SolidBrush(_colorStops[0].color);

            var brush = new LinearGradientBrush(Start, End, Color.Transparent, Color.Transparent);
            
            if (_colorStops.Count == 2)
            {
                brush.LinearColors = new[] { _colorStops[0].color, _colorStops[1].color };
            }
            else
            {
                var blend = new ColorBlend(_colorStops.Count);
                for (int i = 0; i < _colorStops.Count; i++)
                {
                    blend.Positions[i] = _colorStops[i].offset;
                    blend.Colors[i] = _colorStops[i].color;
                }
                brush.InterpolationColors = blend;
            }

            return brush;
        }
    }
}
