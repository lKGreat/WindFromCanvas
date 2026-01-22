using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 图标缓存管理器
    /// 支持文件图标、嵌入式图标、文本图标、默认图标
    /// </summary>
    public class IconCache
    {
        private static IconCache _instance;
        private static readonly object _instanceLock = new object();
        
        private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, Color> _nodeTypeColors = new Dictionary<string, Color>();
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
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IconCache();
                        }
                    }
                }
                return _instance;
            }
        }

        private IconCache()
        {
            InitializeNodeTypeColors();
        }

        /// <summary>
        /// 初始化节点类型颜色
        /// </summary>
        private void InitializeNodeTypeColors()
        {
            _nodeTypeColors["start"] = Color.FromArgb(76, 175, 80);      // 绿色
            _nodeTypeColors["end"] = Color.FromArgb(239, 68, 68);        // 红色
            _nodeTypeColors["process"] = Color.FromArgb(59, 130, 246);   // 蓝色
            _nodeTypeColors["decision"] = Color.FromArgb(234, 179, 8);   // 黄色
            _nodeTypeColors["loop"] = Color.FromArgb(147, 51, 234);      // 紫色
            _nodeTypeColors["code"] = Color.FromArgb(16, 185, 129);      // 青色
            _nodeTypeColors["piece"] = Color.FromArgb(121, 85, 72);      // 棕色
            _nodeTypeColors["group"] = Color.FromArgb(158, 158, 158);    // 灰色
            _nodeTypeColors["default"] = Color.FromArgb(148, 163, 184);  // 默认灰色
        }

        /// <summary>
        /// 获取图标（支持缓存）
        /// </summary>
        /// <param name="iconPath">图标路径</param>
        /// <param name="iconType">图标类型（Image、Svg、Font、Text、Emoji）</param>
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
                    // 对于文件图标，检查文件是否已更新
                    if (iconType.ToLower() != "text" && iconType.ToLower() != "emoji" && File.Exists(iconPath))
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
                        case "text":
                            icon = CreateTextIcon(iconPath, targetSize, Color.White, _nodeTypeColors["default"]);
                            break;
                        case "emoji":
                            icon = CreateEmojiIcon(iconPath, targetSize);
                            break;
                        case "embedded":
                            icon = LoadEmbeddedIcon(iconPath, targetSize);
                            break;
                        case "svg":
                            // SVG 支持：尝试简单的解析，或返回占位符
                            icon = CreateSvgPlaceholder(iconPath, targetSize);
                            break;
                        case "font":
                            icon = CreateFontIcon(iconPath, targetSize);
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
                return CreateHighQualityThumbnail(originalImage, targetSize);
            }
        }

        /// <summary>
        /// 创建高质量缩略图
        /// </summary>
        private Image CreateHighQualityThumbnail(Image original, Size targetSize)
        {
            var thumbnail = new Bitmap(targetSize.Width, targetSize.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                
                // 保持宽高比居中绘制
                float scale = Math.Min((float)targetSize.Width / original.Width, (float)targetSize.Height / original.Height);
                int scaledWidth = (int)(original.Width * scale);
                int scaledHeight = (int)(original.Height * scale);
                int x = (targetSize.Width - scaledWidth) / 2;
                int y = (targetSize.Height - scaledHeight) / 2;
                
                g.DrawImage(original, x, y, scaledWidth, scaledHeight);
            }
            return thumbnail;
        }

        /// <summary>
        /// 加载嵌入式资源图标
        /// </summary>
        private Image LoadEmbeddedIcon(string resourceName, Size targetSize)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var image = Image.FromStream(stream))
                        {
                            return CreateHighQualityThumbnail(image, targetSize);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load embedded icon: {resourceName}, Error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 创建文本图标（单个字符或简短文本）
        /// </summary>
        public Image CreateTextIcon(string text, Size size, Color textColor, Color backgroundColor)
        {
            var icon = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // 绘制圆角背景
                var rect = new RectangleF(1, 1, size.Width - 2, size.Height - 2);
                using (var path = CreateRoundedRectanglePath(rect, 4))
                using (var brush = new SolidBrush(backgroundColor))
                {
                    g.FillPath(brush, path);
                }

                // 绘制文本
                var displayText = text.Length > 2 ? text.Substring(0, 2) : text;
                var fontSize = size.Width * 0.4f;
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                using (var brush = new SolidBrush(textColor))
                {
                    var textSize = g.MeasureString(displayText, font);
                    var x = (size.Width - textSize.Width) / 2;
                    var y = (size.Height - textSize.Height) / 2;
                    g.DrawString(displayText, font, brush, x, y);
                }
            }
            return icon;
        }

        /// <summary>
        /// 创建 Emoji 图标
        /// </summary>
        private Image CreateEmojiIcon(string emoji, Size size)
        {
            var icon = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var fontSize = size.Width * 0.7f;
                using (var font = new Font("Segoe UI Emoji", fontSize))
                using (var brush = new SolidBrush(Color.Black))
                {
                    var textSize = g.MeasureString(emoji, font);
                    var x = (size.Width - textSize.Width) / 2;
                    var y = (size.Height - textSize.Height) / 2;
                    g.DrawString(emoji, font, brush, x, y);
                }
            }
            return icon;
        }

        /// <summary>
        /// 创建字体图标（如 FontAwesome）
        /// </summary>
        private Image CreateFontIcon(string iconCode, Size size)
        {
            // 简单实现：使用 Segoe UI Symbol 字体
            var icon = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var fontSize = size.Width * 0.6f;
                using (var font = new Font("Segoe UI Symbol", fontSize))
                using (var brush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    var textSize = g.MeasureString(iconCode, font);
                    var x = (size.Width - textSize.Width) / 2;
                    var y = (size.Height - textSize.Height) / 2;
                    g.DrawString(iconCode, font, brush, x, y);
                }
            }
            return icon;
        }

        /// <summary>
        /// 创建 SVG 占位符图标
        /// </summary>
        private Image CreateSvgPlaceholder(string svgPath, Size size)
        {
            // SVG 支持需要第三方库，这里创建一个占位符
            var icon = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new RectangleF(2, 2, size.Width - 4, size.Height - 4);
                using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                {
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }

                // 绘制 SVG 文字
                using (var font = new Font("Segoe UI", 6))
                using (var brush = new SolidBrush(Color.Gray))
                {
                    g.DrawString("SVG", font, brush, rect.X + 2, rect.Y + rect.Height / 2 - 4);
                }
            }
            return icon;
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
        /// 获取缓存统计信息
        /// </summary>
        public (int Count, long ApproximateSize) GetCacheStats()
        {
            lock (_lockObject)
            {
                int count = _imageCache.Count;
                long size = 0;
                foreach (var img in _imageCache.Values)
                {
                    if (img is Bitmap bmp)
                    {
                        size += bmp.Width * bmp.Height * 4; // 估算 ARGB 大小
                    }
                }
                return (count, size);
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
        /// 获取节点类型颜色
        /// </summary>
        public Color GetNodeTypeColor(string nodeType)
        {
            var key = nodeType.ToLower();
            return _nodeTypeColors.ContainsKey(key) ? _nodeTypeColors[key] : _nodeTypeColors["default"];
        }

        /// <summary>
        /// 创建默认图标
        /// </summary>
        private Image CreateDefaultIcon(string nodeType, Size size)
        {
            var icon = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                var rect = new RectangleF(2, 2, size.Width - 4, size.Height - 4);
                var color = GetNodeTypeColor(nodeType);
                var darkerColor = ControlPaint.Dark(color, 0.2f);

                using (var brush = new SolidBrush(color))
                using (var pen = new Pen(darkerColor, 1.5f))
                {
                    switch (nodeType.ToLower())
                    {
                        case "start":
                            // 播放按钮形状
                            g.FillEllipse(brush, rect);
                            g.DrawEllipse(pen, rect);
                            DrawPlayIcon(g, rect, Color.White);
                            break;

                        case "end":
                            // 停止按钮形状
                            g.FillEllipse(brush, rect);
                            g.DrawEllipse(pen, rect);
                            DrawStopIcon(g, rect, Color.White);
                            break;

                        case "process":
                            // 齿轮或矩形
                            using (var path = CreateRoundedRectanglePath(rect, 4))
                            {
                                g.FillPath(brush, path);
                                g.DrawPath(pen, path);
                            }
                            DrawGearIcon(g, rect, Color.White);
                            break;

                        case "decision":
                            // 菱形
                            DrawDiamond(g, rect, brush, pen);
                            DrawQuestionIcon(g, rect, Color.White);
                            break;

                        case "loop":
                            // 循环箭头
                            g.FillEllipse(brush, rect);
                            g.DrawEllipse(pen, rect);
                            DrawLoopIcon(g, rect, Color.White);
                            break;

                        case "code":
                            // 代码括号
                            using (var path = CreateRoundedRectanglePath(rect, 4))
                            {
                                g.FillPath(brush, path);
                                g.DrawPath(pen, path);
                            }
                            DrawCodeIcon(g, rect, Color.White);
                            break;

                        case "piece":
                            // 拼图图标
                            using (var path = CreateRoundedRectanglePath(rect, 4))
                            {
                                g.FillPath(brush, path);
                                g.DrawPath(pen, path);
                            }
                            DrawPuzzleIcon(g, rect, Color.White);
                            break;

                        case "group":
                            // 分组图标
                            using (var path = CreateRoundedRectanglePath(rect, 4))
                            {
                                g.FillPath(brush, path);
                                g.DrawPath(pen, path);
                            }
                            DrawGroupIcon(g, rect, Color.White);
                            break;

                        default:
                            // 默认矩形
                            using (var path = CreateRoundedRectanglePath(rect, 4))
                            {
                                g.FillPath(brush, path);
                                g.DrawPath(pen, path);
                            }
                            break;
                    }
                }
            }

            return icon;
        }

        #region 图标绘制辅助方法

        private GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawDiamond(Graphics g, RectangleF rect, Brush brush, Pen pen)
        {
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
            g.DrawPolygon(pen, points);
        }

        private void DrawPlayIcon(Graphics g, RectangleF rect, Color color)
        {
            var margin = rect.Width * 0.3f;
            var centerX = rect.X + rect.Width / 2 + 1;
            var centerY = rect.Y + rect.Height / 2;
            var size = rect.Width * 0.35f;
            
            var points = new[]
            {
                new PointF(centerX - size * 0.4f, centerY - size),
                new PointF(centerX + size * 0.6f, centerY),
                new PointF(centerX - size * 0.4f, centerY + size)
            };
            
            using (var brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, points);
            }
        }

        private void DrawStopIcon(Graphics g, RectangleF rect, Color color)
        {
            var margin = rect.Width * 0.35f;
            var stopRect = new RectangleF(rect.X + margin, rect.Y + margin, rect.Width - margin * 2, rect.Height - margin * 2);
            
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, stopRect);
            }
        }

        private void DrawGearIcon(Graphics g, RectangleF rect, Color color)
        {
            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;
            var radius = rect.Width * 0.25f;
            
            using (var pen = new Pen(color, 1.5f))
            {
                g.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2, radius * 2);
                // 简化的齿轮齿
                for (int i = 0; i < 4; i++)
                {
                    var angle = i * 90 * Math.PI / 180;
                    var x1 = centerX + (float)(Math.Cos(angle) * radius);
                    var y1 = centerY + (float)(Math.Sin(angle) * radius);
                    var x2 = centerX + (float)(Math.Cos(angle) * radius * 1.5f);
                    var y2 = centerY + (float)(Math.Sin(angle) * radius * 1.5f);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }

        private void DrawQuestionIcon(Graphics g, RectangleF rect, Color color)
        {
            using (var font = new Font("Segoe UI", rect.Width * 0.4f, FontStyle.Bold))
            using (var brush = new SolidBrush(color))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("?", font, brush, rect, sf);
            }
        }

        private void DrawLoopIcon(Graphics g, RectangleF rect, Color color)
        {
            var margin = rect.Width * 0.25f;
            var arcRect = new RectangleF(rect.X + margin, rect.Y + margin, rect.Width - margin * 2, rect.Height - margin * 2);
            
            using (var pen = new Pen(color, 2f))
            {
                g.DrawArc(pen, arcRect, 45, 270);
                // 箭头
                var arrowSize = rect.Width * 0.15f;
                var arrowX = arcRect.X + arcRect.Width * 0.85f;
                var arrowY = arcRect.Y + arcRect.Height * 0.15f;
                g.DrawLine(pen, arrowX, arrowY, arrowX + arrowSize, arrowY + arrowSize * 0.5f);
                g.DrawLine(pen, arrowX, arrowY, arrowX - arrowSize * 0.3f, arrowY + arrowSize);
            }
        }

        private void DrawCodeIcon(Graphics g, RectangleF rect, Color color)
        {
            using (var font = new Font("Consolas", rect.Width * 0.35f, FontStyle.Bold))
            using (var brush = new SolidBrush(color))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("</>", font, brush, rect, sf);
            }
        }

        private void DrawPuzzleIcon(Graphics g, RectangleF rect, Color color)
        {
            using (var font = new Font("Segoe UI Symbol", rect.Width * 0.4f))
            using (var brush = new SolidBrush(color))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("◈", font, brush, rect, sf);
            }
        }

        private void DrawGroupIcon(Graphics g, RectangleF rect, Color color)
        {
            var margin = rect.Width * 0.2f;
            var innerRect = new RectangleF(rect.X + margin, rect.Y + margin, rect.Width - margin * 2, rect.Height - margin * 2);
            
            using (var pen = new Pen(color, 1.5f))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, innerRect.X, innerRect.Y, innerRect.Width, innerRect.Height);
            }
        }

        #endregion
    }
}
