using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Animation;
using WindFromCanvas.Core.Events;
using WindFromCanvas.Core.Objects;

namespace WindFromCanvas.Core
{
    public class Canvas : Control
    {
        private List<CanvasObject> _objects = new List<CanvasObject>();
        private CanvasObject _hoveredObject;
        private CanvasObject _pressedObject;
        private CanvasObject _draggingObject;
        private PointF _dragOffset;

        public Color BackgroundColor { get; set; } = Color.White;

        public IReadOnlyList<CanvasObject> Objects => _objects.AsReadOnly();
        public AnimationTimer Animation { get; private set; }

        public Canvas()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            
            Animation = new AnimationTimer();
            Animation.OnFrame += (deltaTime) => Invalidate();
        }

        public void AddObject(CanvasObject obj)
        {
            if (obj != null && !_objects.Contains(obj))
            {
                _objects.Add(obj);
                Invalidate();
            }
        }

        public void RemoveObject(CanvasObject obj)
        {
            if (obj != null && _objects.Remove(obj))
            {
                Invalidate();
            }
        }

        public void Clear()
        {
            _objects.Clear();
            Invalidate();
        }

        public CanvasRenderingContext2D GetContext2D()
        {
            return new CanvasRenderingContext2D(this);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackgroundColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var obj in _objects.OrderBy(o => o.ZIndex))
            {
                if (obj.Visible)
                {
                    obj.Draw(e.Graphics);
                }
            }

            base.OnPaint(e);
        }

        private CanvasObject HitTest(PointF point)
        {
            return _objects.OrderByDescending(o => o.ZIndex)
                           .FirstOrDefault(o => o.Visible && o.HitTest(point));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_draggingObject != null)
            {
                _draggingObject.X = e.X - _dragOffset.X;
                _draggingObject.Y = e.Y - _dragOffset.Y;
                var dragArgs = new CanvasObjectEventArgs(_draggingObject, e.Location, e.Button);
                _draggingObject.OnDrag(dragArgs);
                Invalidate();
            }
            else
            {
                var hit = HitTest(e.Location);
                
                if (hit != _hoveredObject)
                {
                    if (_hoveredObject != null)
                    {
                        var leaveArgs = new CanvasObjectEventArgs(_hoveredObject, e.Location, e.Button);
                        _hoveredObject.OnMouseLeave(leaveArgs);
                    }
                    
                    _hoveredObject = hit;
                    
                    if (_hoveredObject != null)
                    {
                        var enterArgs = new CanvasObjectEventArgs(_hoveredObject, e.Location, e.Button);
                        _hoveredObject.OnMouseEnter(enterArgs);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            var hit = HitTest(e.Location);
            _pressedObject = hit;
            
            if (hit != null)
            {
                var args = new CanvasObjectEventArgs(hit, e.Location, e.Button);
                hit.OnMouseDown(args);
                
                if (hit.Draggable)
                {
                    _draggingObject = hit;
                    _dragOffset = new PointF(e.X - hit.X, e.Y - hit.Y);
                    Capture = true;
                    hit.OnDragStart(args);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (_draggingObject != null)
            {
                var args = new CanvasObjectEventArgs(_draggingObject, e.Location, e.Button);
                _draggingObject.OnDragEnd(args);
                _draggingObject = null;
                Capture = false;
            }
            
            if (_pressedObject != null)
            {
                var args = new CanvasObjectEventArgs(_pressedObject, e.Location, e.Button);
                _pressedObject.OnMouseUp(args);
                
                // 如果按下和释放时是同一个对象，触发Click事件
                var hit = HitTest(e.Location);
                if (hit == _pressedObject)
                {
                    hit.OnClick(args);
                }
            }
            
            _pressedObject = null;
        }

        public void StartAnimation(Action<double> onUpdate = null)
        {
            if (onUpdate != null)
            {
                Animation.OnFrame += (deltaTime) =>
                {
                    onUpdate(deltaTime);
                };
            }
            Animation.Start();
        }

        public void StopAnimation()
        {
            Animation.Stop();
        }
    }
}
