using System.Drawing;

namespace WindFromCanvas.Core.Styles
{
    public interface IFillStyle
    {
        Brush CreateBrush(RectangleF bounds);
    }
}
