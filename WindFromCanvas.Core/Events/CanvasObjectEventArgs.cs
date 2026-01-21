using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Objects;

namespace WindFromCanvas.Core.Events
{
    public class CanvasObjectEventArgs : EventArgs
    {
        public CanvasObject Object { get; }
        public PointF Location { get; }
        public MouseButtons Button { get; }

        public CanvasObjectEventArgs(CanvasObject obj, PointF location, MouseButtons button)
        {
            Object = obj;
            Location = location;
            Button = button;
        }
    }
}
