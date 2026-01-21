using System;
using System.Drawing;
using WindFromCanvas.Core.Events;

namespace WindFromCanvas.Core.Objects
{
    public abstract class CanvasObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool Visible { get; set; } = true;
        public bool Draggable { get; set; } = false;
        public int ZIndex { get; set; }

        public event EventHandler<CanvasObjectEventArgs> Click;
        public event EventHandler<CanvasObjectEventArgs> MouseEnter;
        public event EventHandler<CanvasObjectEventArgs> MouseLeave;
        public event EventHandler<CanvasObjectEventArgs> MouseDown;
        public event EventHandler<CanvasObjectEventArgs> MouseUp;
        public event EventHandler<CanvasObjectEventArgs> DragStart;
        public event EventHandler<CanvasObjectEventArgs> Drag;
        public event EventHandler<CanvasObjectEventArgs> DragEnd;

        public abstract void Draw(Graphics g);
        public abstract bool HitTest(PointF point);
        public abstract RectangleF GetBounds();

        internal void OnClick(CanvasObjectEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        internal void OnMouseEnter(CanvasObjectEventArgs e)
        {
            MouseEnter?.Invoke(this, e);
        }

        internal void OnMouseLeave(CanvasObjectEventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }

        internal void OnMouseDown(CanvasObjectEventArgs e)
        {
            MouseDown?.Invoke(this, e);
        }

        internal void OnMouseUp(CanvasObjectEventArgs e)
        {
            MouseUp?.Invoke(this, e);
        }

        internal void OnDragStart(CanvasObjectEventArgs e)
        {
            DragStart?.Invoke(this, e);
        }

        internal void OnDrag(CanvasObjectEventArgs e)
        {
            Drag?.Invoke(this, e);
        }

        internal void OnDragEnd(CanvasObjectEventArgs e)
        {
            DragEnd?.Invoke(this, e);
        }
    }
}
