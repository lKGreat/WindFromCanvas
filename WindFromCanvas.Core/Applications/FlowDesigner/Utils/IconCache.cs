using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 图标缓存管理器
    /// </summary>
    public class IconCache
    {
        private static IconCache _instance;
        private Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// 图标缓存实例（单例）
        /// </summary>
        public static IconCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new IconCache();
                }
                return _instance;
            }
        }

        private IconCache()
        {
        }

        /// <summary>
        /// 获取图标（支持缓存）
        /// </summary>
        /// <param name="iconPath">图标路径</param>
        /// <param name="iconType">图标类型（Image、Svg、Font）</param>
        /// <param name="size">图标尺寸（默认24x24）</param>
        /// <returns>图标图像，如果加载失败返回null</returns>
        public Image GetIcon(string iconPath, string iconType = "Image", Size? size = null)
        {
            if (string.IsNullOrEmpty(iconPath))
                return null;

            var targetSize = size ?? new Size(24, 24);
            var cacheKey = $"{iconPath}_{iconType}_{targetSize.Width}x{targetSize.Height}";

            lock (_lockObject)
            {
                // 检查缓存
                if (_imageCache.ContainsKey(cacheKey))
                {
                    // 检查文件是否已更新
                    if (File.Exists(iconPath))
                    {
                        var fileTime = File.GetLastWriteTime(iconPath);
                        if (_cacheTimestamps.ContainsKey(cacheKey) && 
                            _cacheTimestamps[cacheKey] >= fileTime)
                        {
                            return _imageCache[cacheKey];
                        }
                    }
                    else
                    {
                        // 文件不存在，返回缓存
                        return _imageCache[cacheKey];
                    }
                }

                // 加载图标
                Image icon = null;
                try
                {
                    switch (iconType.ToLower())
                    {
                        case "image":
                        case "png":
                        case "jpg":
                        case "jpeg":
                        case "bmp":
                        case "gif":
                            icon = LoadImageIcon(iconPath, targetSize);
                            break;
                        case "svg":
                            // SVG支持需要第三方库，这里先返回null
                            // 可以后续集成Svg.NET或类似库
                            icon = null;
                            break;
                        case "font":
                            // Font图标需要字体文件，这里先返回null
                            icon = null;
                            break;
                        default:
                            icon = LoadImageIcon(iconPath, targetSize);
                            break;
                    }

                    if (icon != null)
                    {
                        _imageCache[cacheKey] = icon;
                        _cacheTimestamps[cacheKey] = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不抛出异常
                    System.Diagnostics.Debug.WriteLine($"Failed to load icon: {iconPath}, Error: {ex.Message}");
                }

                return icon;
            }
        }

        /// <summary>
        /// 加载图像图标
        /// </summary>
        private Image LoadImageIcon(string path, Size targetSize)
        {
            if (!File.Exists(path))
                return null;

            using (var originalImage = Image.FromFile(path))
            {
                // 创建高质量缩略图
                var thumbnail = new Bitmap(targetSize.Width, targetSize.Height);
                using (var g = Graphics.FromImage(thumbnail))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.DrawImage(originalImage, 0, 0, targetSize.Width, targetSize.Height);
                }
                return thumbnail;
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_lockObject)
            {
                foreach (var image in _imageCache.Values)
                {
                    image?.Dispose();
                }
                _imageCache.Clear();
                _cacheTimestamps.Clear();
            }
        }

        /// <summary>
        /// 清除指定路径的缓存
        /// </summary>
        public void ClearCache(string iconPath)
        {
            lock (_lockObject)
            {
                var keysToRemove = _imageCache.Keys.Where(k => k.StartsWith(iconPath)).ToList();
                foreach (var key in keysToRemove)
                {
                    _imageCache[key]?.Dispose();
                    _imageCache.Remove(key);
                    _cacheTimestamps.Remove(key);
                }
            }
        }

        /// <summary>
        /// 获取默认图标（根据节点类型）
        /// </summary>
        public Image GetDefaultIcon(string nodeType, Size? size = null)
        {
            var targetSize = size ?? new Size(24, 24);
            var cacheKey = $"default_{nodeType}_{targetSize.Width}x{targetSize.Height}";

            lock (_lockObject)
            {
                if (_imageCache.ContainsKey(cacheKey))
                {
                    return _imageCache[cacheKey];
                }

                // 创建默认图标（简单的几何形状）
                var icon = CreateDefaultIcon(nodeType, targetSize);
                if (icon != null)
                {
                    _imageCache[cacheKey] = icon;
                    _cacheTimestamps[cacheKey] = DateTime.Now;
                }

                return icon;
            }
        }

        /// <summary>
        /// 创建默认图标
        /// </summary>
        private Image CreateDefaultIcon(string nodeType, Size size)
        {
            var icon = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                var rect = new RectangleF(2, 2, size.Width - 4, size.Height - 4);
                var brush = new SolidBrush(Color.FromArgb(100, 148, 163, 184));
                var pen = new Pen(Color.FromArgb(148, 163, 184), 1.5f);

                switch (nodeType.ToLower())
                {
                    case "start":
                        // 圆形图标（绿色）
                        brush.Color = Color.FromArgb(76, 175, 80);
                        g.FillEllipse(brush, rect);
                        pen.Color = Color.FromArgb(56, 142, 60);
                        g.DrawEllipse(pen, rect);
                        break;

                    case "process":
                        // 矩形图标（蓝色）
                        brush.Color = Color.FromArgb(59, 130, 246);
                        g.FillRectangle(brush, rect);
                        pen.Color = Color.FromArgb(37, 99, 235);
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                        break;

                    case "decision":
                        // 菱形图标（黄色）
                        brush.Color = Color.FromArgb(234, 179, 8);
                        var centerX = rect.X + rect.Width / 2;
                        var centerY = rect.Y + rect.Height / 2;
                        var points = new[]
                        {
                            new PointF(centerX, rect.Y),
                            new PointF(rect.Right, centerY),
                            new PointF(centerX, rect.Bottom),
                            new PointF(rect.X, centerY)
                        };
                        g.FillPolygon(brush, points);
                        pen.Color = Color.FromArgb(202, 138, 4);
                        g.DrawPolygon(pen, points);
                        break;

                    case "loop":
                        // 循环图标（紫色）
                        brush.Color = Color.FromArgb(147, 51, 234);
                        g.FillEllipse(brush, rect);
                        pen.Color = Color.FromArgb(126, 34, 206);
                        g.DrawEllipse(pen, rect);
                        // 绘制循环箭头
                        var arrowSize = rect.Width * 0.3f;
                        var arrowRect = new RectangleF(rect.X + rect.Width - arrowSize - 2, rect.Y + 2, arrowSize, arrowSize);
                        g.DrawArc(pen, arrowRect, 0, 270);
                        break;

                    case "end":
                        // 圆形图标（红色）
                        brush.Color = Color.FromArgb(239, 68, 68);
                        g.FillEllipse(brush, rect);
                        pen.Color = Color.FromArgb(220, 38, 38);
                        g.DrawEllipse(pen, rect);
                        break;

                    default:
                        // 默认矩形
                        brush.Color = Color.FromArgb(148, 163, 184);
                        g.FillRectangle(brush, rect);
                        pen.Color = Color.FromArgb(100, 116, 139);
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                        break;
                }
            }

            return icon;
        }
    }
}
