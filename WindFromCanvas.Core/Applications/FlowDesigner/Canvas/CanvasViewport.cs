using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas
{
    /// <summary>
    /// 画布视口（处理缩放、平移）
    /// </summary>
    public class CanvasViewport
    {
        /// <summary>
        /// 视口位置（X坐标）
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// 视口位置（Y坐标）
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 缩放级别
        /// </summary>
        public float Zoom { get; set; }

        /// <summary>
        /// 视口大小
        /// </summary>
        public SizeF ViewportSize { get; set; }

        /// <summary>
        /// 平移范围限制
        /// </summary>
        public RectangleF? TranslateExtent { get; set; }

        public CanvasViewport()
        {
            X = 0;
            Y = 0;
            Zoom = Layout.LayoutConstants.DEFAULT_ZOOM;
            ViewportSize = SizeF.Empty;
        }

        /// <summary>
        /// 设置缩放级别（限制在范围内）
        /// </summary>
        public void SetZoom(float zoom)
        {
            Zoom = Math.Max(Layout.LayoutConstants.MIN_ZOOM, 
                Math.Min(Layout.LayoutConstants.MAX_ZOOM, zoom));
        }

        /// <summary>
        /// 缩放增量
        /// </summary>
        public void ZoomIn(float delta = 0.1f)
        {
            SetZoom(Zoom + delta);
        }

        /// <summary>
        /// 缩小
        /// </summary>
        public void ZoomOut(float delta = 0.1f)
        {
            SetZoom(Zoom - delta);
        }

        /// <summary>
        /// 平移视口
        /// </summary>
        public void Pan(float deltaX, float deltaY)
        {
            X += deltaX;
            Y += deltaY;

            // 应用平移限制
            if (TranslateExtent.HasValue)
            {
                var extent = TranslateExtent.Value;
                X = Math.Max(extent.Left, Math.Min(extent.Right, X));
                Y = Math.Max(extent.Top, Math.Min(extent.Bottom, Y));
            }
        }

        /// <summary>
        /// 设置视口位置
        /// </summary>
        public void SetViewport(float x, float y, float zoom)
        {
            X = x;
            Y = y;
            SetZoom(zoom);
        }

        /// <summary>
        /// 将屏幕坐标转换为画布坐标
        /// </summary>
        public PointF ScreenToCanvas(PointF screenPoint)
        {
            return new PointF(
                (screenPoint.X - X) / Zoom,
                (screenPoint.Y - Y) / Zoom
            );
        }

        /// <summary>
        /// 将画布坐标转换为屏幕坐标
        /// </summary>
        public PointF CanvasToScreen(PointF canvasPoint)
        {
            return new PointF(
                canvasPoint.X * Zoom + X,
                canvasPoint.Y * Zoom + Y
            );
        }

        /// <summary>
        /// 应用变换到 Graphics 对象
        /// </summary>
        public void ApplyTransform(Graphics g)
        {
            g.TranslateTransform(X, Y);
            g.ScaleTransform(Zoom, Zoom);
        }

        /// <summary>
        /// 重置变换
        /// </summary>
        public void ResetTransform(Graphics g)
        {
            g.ResetTransform();
        }

        /// <summary>
        /// 适应视图（居中显示指定区域）
        /// </summary>
        public void FitView(RectangleF bounds, SizeF viewportSize, float padding = 20f)
        {
            if (bounds.IsEmpty || viewportSize.IsEmpty)
                return;

            // 计算缩放比例
            float scaleX = (viewportSize.Width - padding * 2) / bounds.Width;
            float scaleY = (viewportSize.Height - padding * 2) / bounds.Height;
            float scale = Math.Min(scaleX, scaleY);
            
            // 限制缩放范围
            SetZoom(Math.Max(Layout.LayoutConstants.MIN_ZOOM, 
                Math.Min(Layout.LayoutConstants.MAX_ZOOM, scale)));

            // 计算居中位置
            float centerX = bounds.X + bounds.Width / 2;
            float centerY = bounds.Y + bounds.Height / 2;

            X = viewportSize.Width / 2 - centerX * Zoom;
            Y = viewportSize.Height / 2 - centerY * Zoom;
        }
    }
}
