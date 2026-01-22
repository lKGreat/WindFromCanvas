using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 7.3 小地图控件 - 实现实时缩略图和视口拖拽
    /// </summary>
    public class MinimapControl : Control
    {
        #region 字段

        private FlowDesignerCanvas _canvas;
        private RectangleF _viewportRect;
        private RectangleF _contentBounds;
        private bool _isDragging;
        private PointF _dragStart;
        private float _scale = 1f;
        private PointF _offset;
        private Bitmap _cachedThumbnail;
        private bool _needsRedraw = true;
        private Timer _refreshTimer;

        // 配置
        private const int BorderPadding = 4;
        private const int MinSize = 100;
        private const float ViewportBorderWidth = 2f;
        private const int RefreshInterval = 100; // 刷新间隔（毫秒）

        #endregion

        #region 事件

        public event EventHandler<PointF> ViewportMoved;
        public event EventHandler<float> ZoomRequested;

        #endregion

        #region 属性

        /// <summary>
        /// 是否显示节点名称
        /// </summary>
        public bool ShowNodeLabels { get; set; } = false;

        /// <summary>
        /// 是否显示连线
        /// </summary>
        public bool ShowConnections { get; set; } = true;

        /// <summary>
        /// 视口框颜色
        /// </summary>
        public Color ViewportColor { get; set; } = Color.FromArgb(59, 130, 246);

        /// <summary>
        /// 遮罩透明度
        /// </summary>
        public int MaskOpacity { get; set; } = 80;

        #endregion

        #region 构造

        public MinimapControl(FlowDesignerCanvas canvas)
        {
            _canvas = canvas;
            InitializeComponent();
            SubscribeToCanvas();
        }

        public MinimapControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);

            this.Size = new Size(180, 120);
            this.MinimumSize = new Size(MinSize, MinSize);
            this.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            this.Cursor = Cursors.Hand;

            // 定时刷新
            _refreshTimer = new Timer { Interval = RefreshInterval };
            _refreshTimer.Tick += (s, e) =>
            {
                if (_needsRedraw)
                {
                    _needsRedraw = false;
                    InvalidateThumbnail();
                    Invalidate();
                }
            };
            _refreshTimer.Start();

            // 事件绑定
            this.Paint += MinimapControl_Paint;
            this.MouseDown += MinimapControl_MouseDown;
            this.MouseMove += MinimapControl_MouseMove;
            this.MouseUp += MinimapControl_MouseUp;
            this.MouseWheel += MinimapControl_MouseWheel;

            ThemeManager.Instance.ThemeChanged += (s, e) =>
            {
                this.BackColor = e.NewTheme.Background;
                InvalidateThumbnail();
            };
        }

        private void SubscribeToCanvas()
        {
            if (_canvas == null) return;

            _canvas.Paint += (s, e) => MarkNeedsRedraw();
            _canvas.Resize += (s, e) => MarkNeedsRedraw();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置关联的画布
        /// </summary>
        public void SetCanvas(FlowDesignerCanvas canvas)
        {
            _canvas = canvas;
            SubscribeToCanvas();
            InvalidateThumbnail();
        }

        /// <summary>
        /// 强制刷新
        /// </summary>
        public void RefreshMinimap()
        {
            InvalidateThumbnail();
            Invalidate();
        }

        /// <summary>
        /// 标记需要重绘
        /// </summary>
        public void MarkNeedsRedraw()
        {
            _needsRedraw = true;
        }

        /// <summary>
        /// 缩放到适应全部内容
        /// </summary>
        public void FitToContent()
        {
            if (_canvas == null) return;

            var nodes = _canvas.GetNodes().ToList();
            if (nodes.Count == 0) return;

            CalculateContentBounds(nodes);

            // 计算需要的缩放级别
            var contentWidth = _contentBounds.Width + 100;
            var contentHeight = _contentBounds.Height + 100;
            var zoomX = _canvas.Width / contentWidth;
            var zoomY = _canvas.Height / contentHeight;
            var newZoom = Math.Min(zoomX, zoomY);
            newZoom = Math.Max(0.5f, Math.Min(newZoom, 2f));

            // 计算居中偏移
            var centerX = _contentBounds.X + _contentBounds.Width / 2;
            var centerY = _contentBounds.Y + _contentBounds.Height / 2;
            var newPanX = _canvas.Width / 2 - centerX * newZoom;
            var newPanY = _canvas.Height / 2 - centerY * newZoom;

            _canvas.SetZoom(newZoom, new PointF(_canvas.Width / 2, _canvas.Height / 2));
            _canvas.PanOffset = new PointF(newPanX, newPanY);
            _canvas.Invalidate();

            ZoomRequested?.Invoke(this, newZoom);
        }

        #endregion

        #region 7.3.1 实时缩略图渲染

        private void InvalidateThumbnail()
        {
            _cachedThumbnail?.Dispose();
            _cachedThumbnail = null;
        }

        private void MinimapControl_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var theme = ThemeManager.Instance.CurrentTheme;

            // 清除背景
            using (var bgBrush = new SolidBrush(theme.CanvasBackground))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            if (_canvas == null)
            {
                DrawNoCanvasMessage(g, theme);
                DrawBorder(g, theme);
                return;
            }

            var nodes = _canvas.GetNodes().ToList();
            if (nodes.Count == 0)
            {
                DrawEmptyMessage(g, theme);
                DrawBorder(g, theme);
                return;
            }

            // 计算内容边界
            CalculateContentBounds(nodes);
            CalculateScale();

            // 绘制缩略图
            DrawThumbnail(g, nodes, theme);

            // 7.3.2 绘制视口框
            DrawViewport(g, theme);

            // 绘制遮罩
            DrawMask(g);

            // 绘制边框
            DrawBorder(g, theme);
        }

        private void CalculateContentBounds(IEnumerable<FlowNode> nodes)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var node in nodes)
            {
                var bounds = node.GetBounds();
                minX = Math.Min(minX, bounds.Left);
                minY = Math.Min(minY, bounds.Top);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            _contentBounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        private void CalculateScale()
        {
            if (_contentBounds.Width <= 0 || _contentBounds.Height <= 0)
            {
                _scale = 1f;
                _offset = new PointF(BorderPadding, BorderPadding);
                return;
            }

            var availableWidth = this.Width - BorderPadding * 2;
            var availableHeight = this.Height - BorderPadding * 2;

            var scaleX = availableWidth / _contentBounds.Width;
            var scaleY = availableHeight / _contentBounds.Height;
            _scale = Math.Min(scaleX, scaleY);

            // 居中偏移
            var scaledWidth = _contentBounds.Width * _scale;
            var scaledHeight = _contentBounds.Height * _scale;
            _offset = new PointF(
                (this.Width - scaledWidth) / 2,
                (this.Height - scaledHeight) / 2
            );
        }

        private void DrawThumbnail(Graphics g, IEnumerable<FlowNode> nodes, ThemeConfig theme)
        {
            // 绘制连线
            if (ShowConnections)
            {
                using (var pen = new Pen(Color.FromArgb(100, theme.ConnectionLine), 1f))
                {
                    // 简化绘制连线（从连接数据获取）
                    // TODO: 从画布获取连线并绘制
                }
            }

            // 绘制节点
            foreach (var node in nodes)
            {
                DrawNodeThumbnail(g, node, theme);
            }
        }

        private void DrawNodeThumbnail(Graphics g, FlowNode node, ThemeConfig theme)
        {
            var bounds = node.GetBounds();
            var rect = WorldToMinimap(bounds);

            // 选择颜色
            Color nodeColor = GetNodeColor(node, theme);

            // 绘制节点
            using (var brush = new SolidBrush(nodeColor))
            {
                if (rect.Width > 3 && rect.Height > 3)
                {
                    g.FillRectangle(brush, rect);
                }
                else
                {
                    // 太小时绘制为点
                    g.FillEllipse(brush, rect.X - 2, rect.Y - 2, 4, 4);
                }
            }

            // 选中状态
            if (node.IsSelected)
            {
                using (var pen = new Pen(theme.Primary, 2f))
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }

            // 显示标签
            if (ShowNodeLabels && rect.Width > 20 && rect.Height > 10)
            {
                var text = node.Data?.DisplayName ?? node.Data?.Name ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    using (var font = new Font("Segoe UI", 6))
                    using (var brush = new SolidBrush(theme.TextPrimary))
                    {
                        var format = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter
                        };
                        g.DrawString(text, font, brush, rect, format);
                    }
                }
            }
        }

        private Color GetNodeColor(FlowNode node, ThemeConfig theme)
        {
            if (node is StartNode) return Color.FromArgb(76, 175, 80);
            if (node is EndNode) return Color.FromArgb(239, 68, 68);
            if (node is DecisionNode) return Color.FromArgb(234, 179, 8);
            if (node is LoopNode) return Color.FromArgb(156, 39, 176);
            return theme.Primary;
        }

        private void DrawViewport(Graphics g, ThemeConfig theme)
        {
            // 计算当前视口在世界坐标中的位置
            var viewportWorld = new RectangleF(
                -_canvas.PanOffset.X / _canvas.ZoomFactor,
                -_canvas.PanOffset.Y / _canvas.ZoomFactor,
                _canvas.Width / _canvas.ZoomFactor,
                _canvas.Height / _canvas.ZoomFactor
            );

            _viewportRect = WorldToMinimap(viewportWorld);

            // 绘制视口框
            using (var pen = new Pen(ViewportColor, ViewportBorderWidth))
            {
                g.DrawRectangle(pen, _viewportRect.X, _viewportRect.Y, _viewportRect.Width, _viewportRect.Height);
            }

            // 绘制角点手柄
            var handleSize = 4f;
            using (var brush = new SolidBrush(ViewportColor))
            {
                g.FillRectangle(brush, _viewportRect.X - handleSize / 2, _viewportRect.Y - handleSize / 2, handleSize, handleSize);
                g.FillRectangle(brush, _viewportRect.Right - handleSize / 2, _viewportRect.Y - handleSize / 2, handleSize, handleSize);
                g.FillRectangle(brush, _viewportRect.X - handleSize / 2, _viewportRect.Bottom - handleSize / 2, handleSize, handleSize);
                g.FillRectangle(brush, _viewportRect.Right - handleSize / 2, _viewportRect.Bottom - handleSize / 2, handleSize, handleSize);
            }
        }

        private void DrawMask(Graphics g)
        {
            var maskColor = Color.FromArgb(MaskOpacity, 0, 0, 0);
            using (var brush = new SolidBrush(maskColor))
            {
                // 上
                if (_viewportRect.Top > 0)
                    g.FillRectangle(brush, 0, 0, this.Width, _viewportRect.Top);
                // 左
                if (_viewportRect.Left > 0)
                    g.FillRectangle(brush, 0, _viewportRect.Top, _viewportRect.Left, _viewportRect.Height);
                // 右
                if (_viewportRect.Right < this.Width)
                    g.FillRectangle(brush, _viewportRect.Right, _viewportRect.Top, this.Width - _viewportRect.Right, _viewportRect.Height);
                // 下
                if (_viewportRect.Bottom < this.Height)
                    g.FillRectangle(brush, 0, _viewportRect.Bottom, this.Width, this.Height - _viewportRect.Bottom);
            }
        }

        private void DrawBorder(Graphics g, ThemeConfig theme)
        {
            using (var pen = new Pen(theme.Border, 1))
            {
                g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private void DrawNoCanvasMessage(Graphics g, ThemeConfig theme)
        {
            using (var font = new Font("Segoe UI", 9))
            using (var brush = new SolidBrush(theme.TextSecondary))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("未连接画布", font, brush, this.ClientRectangle, format);
            }
        }

        private void DrawEmptyMessage(Graphics g, ThemeConfig theme)
        {
            using (var font = new Font("Segoe UI", 9))
            using (var brush = new SolidBrush(theme.TextSecondary))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("画布为空", font, brush, this.ClientRectangle, format);
            }
        }

        #endregion

        #region 7.3.2 视口拖拽

        private void MinimapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_viewportRect.Contains(e.Location))
            {
                _isDragging = true;
                _dragStart = e.Location;
                this.Cursor = Cursors.SizeAll;
            }
            else
            {
                // 7.3.3 点击定位
                MoveViewportTo(e.Location);
            }
        }

        private void MinimapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var deltaX = e.X - _dragStart.X;
                var deltaY = e.Y - _dragStart.Y;
                _dragStart = e.Location;

                // 转换为世界坐标移动量
                var worldDeltaX = deltaX / _scale;
                var worldDeltaY = deltaY / _scale;

                // 更新画布偏移
                var newPanX = _canvas.PanOffset.X - worldDeltaX * _canvas.ZoomFactor;
                var newPanY = _canvas.PanOffset.Y - worldDeltaY * _canvas.ZoomFactor;

                _canvas.PanOffset = new PointF(newPanX, newPanY);
                _canvas.Invalidate();

                ViewportMoved?.Invoke(this, _canvas.PanOffset);
                Invalidate();
            }
            else
            {
                // 更新光标
                this.Cursor = _viewportRect.Contains(e.Location) ? Cursors.SizeAll : Cursors.Hand;
            }
        }

        private void MinimapControl_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            this.Cursor = _viewportRect.Contains(e.Location) ? Cursors.SizeAll : Cursors.Hand;
        }

        private void MoveViewportTo(PointF minimapPoint)
        {
            if (_canvas == null || _contentBounds.Width <= 0) return;

            // 转换为世界坐标
            var worldPos = MinimapToWorld(minimapPoint);

            // 将视口中心移动到点击位置
            var newPanX = _canvas.Width / 2 - worldPos.X * _canvas.ZoomFactor;
            var newPanY = _canvas.Height / 2 - worldPos.Y * _canvas.ZoomFactor;

            _canvas.PanOffset = new PointF(newPanX, newPanY);
            _canvas.Invalidate();

            ViewportMoved?.Invoke(this, _canvas.PanOffset);
            Invalidate();
        }

        #endregion

        #region 7.3.4 缩放同步

        private void MinimapControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_canvas == null) return;

            // 通过滚轮调整画布缩放
            if (e.Delta > 0)
            {
                _canvas.ZoomIn();
            }
            else
            {
                _canvas.ZoomOut();
            }

            _canvas.Invalidate();
            ZoomRequested?.Invoke(this, _canvas.ZoomFactor);
            Invalidate();
        }

        #endregion

        #region 坐标转换

        private RectangleF WorldToMinimap(RectangleF worldRect)
        {
            return new RectangleF(
                _offset.X + (worldRect.X - _contentBounds.X) * _scale,
                _offset.Y + (worldRect.Y - _contentBounds.Y) * _scale,
                worldRect.Width * _scale,
                worldRect.Height * _scale
            );
        }

        private PointF MinimapToWorld(PointF minimapPoint)
        {
            return new PointF(
                _contentBounds.X + (minimapPoint.X - _offset.X) / _scale,
                _contentBounds.Y + (minimapPoint.Y - _offset.Y) / _scale
            );
        }

        #endregion

        #region 资源清理

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _cachedThumbnail?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
