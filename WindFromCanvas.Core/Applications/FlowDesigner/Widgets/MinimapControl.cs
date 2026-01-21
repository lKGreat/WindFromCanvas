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
    /// 小地图控件（参考Activepieces实现）
    /// </summary>
    public class MinimapControl : Control
    {
        private FlowDesignerCanvas _canvas;
        private RectangleF _viewportRect;
        private bool _isDragging;
        private PointF _dragStart;

        public MinimapControl(FlowDesignerCanvas canvas)
        {
            _canvas = canvas;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(150, 100);
            this.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            this.Paint += MinimapControl_Paint;
            this.MouseDown += MinimapControl_MouseDown;
            this.MouseMove += MinimapControl_MouseMove;
            this.MouseUp += MinimapControl_MouseUp;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            // 绘制边框
            using (var pen = new Pen(ThemeManager.Instance.CurrentTheme.Border, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private void MinimapControl_Paint(object sender, PaintEventArgs e)
        {
            if (_canvas == null || _canvas.Document == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(ThemeManager.Instance.CurrentTheme.Background);

            // 计算所有节点的边界
            var nodes = _canvas.GetNodes();
            if (nodes.Count == 0) return;

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

            var graphWidth = maxX - minX;
            var graphHeight = maxY - minY;

            if (graphWidth <= 0 || graphHeight <= 0) return;

            // 计算缩放比例以适应小地图
            var scaleX = (this.Width - 10) / graphWidth;
            var scaleY = (this.Height - 10) / graphHeight;
            var scale = Math.Min(scaleX, scaleY);

            var offsetX = 5f;
            var offsetY = 5f;

            // 绘制节点（简化显示）
            var theme = ThemeManager.Instance.CurrentTheme;
            foreach (var node in nodes)
            {
                var bounds = node.GetBounds();
                var rect = new RectangleF(
                    offsetX + (bounds.X - minX) * scale,
                    offsetY + (bounds.Y - minY) * scale,
                    bounds.Width * scale,
                    bounds.Height * scale
                );

                // 根据节点类型使用不同颜色
                Color nodeColor = theme.Primary;
                if (node is StartNode) nodeColor = Color.FromArgb(76, 175, 80);
                else if (node is EndNode) nodeColor = Color.FromArgb(239, 68, 68);
                else if (node is DecisionNode) nodeColor = Color.FromArgb(234, 179, 8);

                using (var brush = new SolidBrush(nodeColor))
                {
                    g.FillRectangle(brush, rect);
                }
            }

            // 绘制视口矩形
            var viewportWorld = new RectangleF(
                -_canvas.PanOffset.X / _canvas.ZoomFactor,
                -_canvas.PanOffset.Y / _canvas.ZoomFactor,
                _canvas.Width / _canvas.ZoomFactor,
                _canvas.Height / _canvas.ZoomFactor
            );

            _viewportRect = new RectangleF(
                offsetX + (viewportWorld.X - minX) * scale,
                offsetY + (viewportWorld.Y - minY) * scale,
                viewportWorld.Width * scale,
                viewportWorld.Height * scale
            );

            using (var pen = new Pen(theme.Primary, 2f))
            {
                g.DrawRectangle(pen, _viewportRect.X, _viewportRect.Y, _viewportRect.Width, _viewportRect.Height);
            }

            // 绘制半透明遮罩（Activepieces风格）
            var maskColor = ThemeManager.Instance.CurrentTheme.Background == Color.FromArgb(255, 255, 255) ?
                Color.FromArgb(14, 0, 0, 0) : Color.FromArgb(204, 0, 0, 0); // 浅色主题14透明度，深色主题80透明度

            using (var brush = new SolidBrush(maskColor))
            {
                // 绘制视口外的遮罩
                var regions = new[]
                {
                    new RectangleF(0, 0, this.Width, _viewportRect.Top),
                    new RectangleF(0, _viewportRect.Top, _viewportRect.Left, _viewportRect.Height),
                    new RectangleF(_viewportRect.Right, _viewportRect.Top, this.Width - _viewportRect.Right, _viewportRect.Height),
                    new RectangleF(0, _viewportRect.Bottom, this.Width, this.Height - _viewportRect.Bottom)
                };

                foreach (var region in regions)
                {
                    if (region.Width > 0 && region.Height > 0)
                    {
                        g.FillRectangle(brush, region);
                    }
                }
            }
        }

        private void MinimapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (_viewportRect.Contains(e.Location))
            {
                _isDragging = true;
                _dragStart = e.Location;
            }
            else
            {
                // 点击视口外，移动视口到点击位置
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

                // 计算世界坐标的移动距离
                var nodes = _canvas.GetNodes();
                if (nodes.Count == 0) return;

                var minX = nodes.Min(n => n.GetBounds().Left);
                var minY = nodes.Min(n => n.GetBounds().Top);
                var maxX = nodes.Max(n => n.GetBounds().Right);
                var maxY = nodes.Max(n => n.GetBounds().Bottom);
                var graphWidth = maxX - minX;
                var graphHeight = maxY - minY;

                var scaleX = (this.Width - 10) / graphWidth;
                var scaleY = (this.Height - 10) / graphHeight;
                var scale = Math.Min(scaleX, scaleY);

                var worldDeltaX = deltaX / scale;
                var worldDeltaY = deltaY / scale;

                // 更新画布偏移（需要通过反射或公共属性）
                UpdateCanvasPanOffset(worldDeltaX, worldDeltaY);
                _canvas.Invalidate();
                this.Invalidate();
            }
        }

        private void MinimapControl_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void MoveViewportTo(PointF minimapPoint)
        {
            var nodes = _canvas.GetNodes();
            if (nodes.Count == 0) return;

            var minX = nodes.Min(n => n.GetBounds().Left);
            var minY = nodes.Min(n => n.GetBounds().Top);
            var maxX = nodes.Max(n => n.GetBounds().Right);
            var maxY = nodes.Max(n => n.GetBounds().Bottom);
            var graphWidth = maxX - minX;
            var graphHeight = maxY - minY;

            var scaleX = (this.Width - 10) / graphWidth;
            var scaleY = (this.Height - 10) / graphHeight;
            var scale = Math.Min(scaleX, scaleY);

            var offsetX = 5f;
            var offsetY = 5f;

            var worldX = minX + (minimapPoint.X - offsetX) / scale;
            var worldY = minY + (minimapPoint.Y - offsetY) / scale;

            // 将视口中心移动到点击位置
            var newPanX = _canvas.Width / 2 - worldX * _canvas.ZoomFactor;
            var newPanY = _canvas.Height / 2 - worldY * _canvas.ZoomFactor;
            UpdateCanvasPanOffset(newPanX - _canvas.PanOffset.X, newPanY - _canvas.PanOffset.Y);
            _canvas.Invalidate();
            this.Invalidate();
        }

        private void UpdateCanvasPanOffset(float deltaX, float deltaY)
        {
            // PanOffset已经是public set，直接更新
            _canvas.PanOffset = new PointF(
                _canvas.PanOffset.X + deltaX * _canvas.ZoomFactor,
                _canvas.PanOffset.Y + deltaY * _canvas.ZoomFactor
            );
        }

        public void UpdateViewport()
        {
            this.Invalidate();
        }
    }
}
