using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas
{
    /// <summary>
    /// 流程画布控件（主画布，匹配 Activepieces FlowCanvas）
    /// </summary>
    public class FlowCanvas : Control
    {
        private CanvasViewport _viewport;
        private BuilderStateStore _stateStore;
        private bool _isPanning;
        private PointF _lastPanPoint;
        private bool _isInitialized;

        public FlowCanvas()
        {
            _viewport = new CanvasViewport();
            _stateStore = BuilderStateStore.Instance;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            
            BackColor = Color.FromArgb(250, 250, 250);
            _isPanning = false;
            _isInitialized = false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_isInitialized)
            {
                InitializeCanvas();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_isInitialized)
            {
                _viewport.ViewportSize = Size;
            }
        }

        private void InitializeCanvas()
        {
            _viewport.ViewportSize = Size;
            _isInitialized = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (!_isInitialized)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 保存原始变换
            var originalTransform = g.Transform;

            // 应用视口变换
            _viewport.ApplyTransform(g);

            // 绘制背景
            DrawBackground(g);

            // 绘制流程内容（节点、边缘等）
            DrawFlowContent(g);

            // 恢复原始变换
            g.Transform = originalTransform;

            // 绘制覆盖层（拖拽预览、选择框等）
            DrawOverlays(g);
        }

        /// <summary>
        /// 绘制背景（点阵）
        /// </summary>
        private void DrawBackground(Graphics g)
        {
            var viewportBounds = GetViewportBounds();
            var gridSize = 10f / _viewport.Zoom;

            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1f / _viewport.Zoom))
            {
                // 绘制点阵
                for (float x = viewportBounds.Left; x <= viewportBounds.Right; x += gridSize)
                {
                    for (float y = viewportBounds.Top; y <= viewportBounds.Bottom; y += gridSize)
                    {
                        g.DrawEllipse(pen, x - 0.5f, y - 0.5f, 1f, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制流程内容
        /// </summary>
        private void DrawFlowContent(Graphics g)
        {
            // TODO: 实现节点和边缘的绘制
            // 这将在后续阶段实现
        }

        /// <summary>
        /// 绘制覆盖层
        /// </summary>
        private void DrawOverlays(Graphics g)
        {
            // 绘制选择框
            if (_stateStore.Selection.SelectionRectangle != null)
            {
                DrawSelectionRectangle(g, _stateStore.Selection.SelectionRectangle);
            }

            // 绘制拖拽预览
            if (_stateStore.Drag.IsDragging)
            {
                DrawDragPreview(g);
            }
        }

        /// <summary>
        /// 绘制选择矩形
        /// </summary>
        private void DrawSelectionRectangle(Graphics g, SelectionRectangle rect)
        {
            using (var brush = new SolidBrush(Color.FromArgb(50, 59, 130, 246)))
            using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 1f))
            {
                var screenRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                g.FillRectangle(brush, screenRect);
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        /// <summary>
        /// 绘制拖拽预览
        /// </summary>
        private void DrawDragPreview(Graphics g)
        {
            // TODO: 实现拖拽预览绘制
        }

        /// <summary>
        /// 获取视口边界（画布坐标）
        /// </summary>
        private RectangleF GetViewportBounds()
        {
            var topLeft = _viewport.ScreenToCanvas(PointF.Empty);
            var bottomRight = _viewport.ScreenToCanvas(new PointF(Width, Height));
            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y
            );
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Middle || 
                (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Space))
            {
                _isPanning = true;
                _lastPanPoint = e.Location;
                Cursor = Cursors.Hand;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isPanning)
            {
                var deltaX = e.X - _lastPanPoint.X;
                var deltaY = e.Y - _lastPanPoint.Y;
                _viewport.Pan(deltaX, deltaY);
                _lastPanPoint = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Control.ModifierKeys == Keys.Control)
            {
                // 缩放
                var zoomDelta = e.Delta > 0 ? 0.1f : -0.1f;
                var oldZoom = _viewport.Zoom;
                _viewport.ZoomIn(zoomDelta);

                // 以鼠标位置为中心缩放
                var mousePos = e.Location;
                var canvasPos = _viewport.ScreenToCanvas(mousePos);
                var zoomFactor = _viewport.Zoom / oldZoom;
                _viewport.X = mousePos.X - canvasPos.X * _viewport.Zoom;
                _viewport.Y = mousePos.Y - canvasPos.Y * _viewport.Zoom;

                Invalidate();
            }
            else
            {
                // 垂直滚动
                _viewport.Pan(0, -e.Delta);
                Invalidate();
            }
        }

        /// <summary>
        /// 获取视口
        /// </summary>
        public CanvasViewport Viewport => _viewport;
    }
}
