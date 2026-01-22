using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Themes
{
    /// <summary>
    /// 6.4 增强的主题管理器
    /// 支持主题切换动画、自定义主题导入和主题预览
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private static readonly object _lock = new object();

        private ThemeConfig _currentTheme;
        private readonly Dictionary<string, ThemeConfig> _registeredThemes = new Dictionary<string, ThemeConfig>(StringComparer.OrdinalIgnoreCase);
        private readonly string _themesDirectory;

        // 6.4.3 主题切换动画
        private bool _isTransitioning = false;
        private ThemeTransition _currentTransition;

        /// <summary>
        /// 主题管理器实例
        /// </summary>
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ThemeManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 当前主题
        /// </summary>
        public ThemeConfig CurrentTheme
        {
            get => _currentTheme ?? new LightTheme();
            private set => _currentTheme = value;
        }

        /// <summary>
        /// 是否正在切换主题
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// 是否启用切换动画
        /// </summary>
        public bool EnableTransitionAnimation { get; set; } = true;

        /// <summary>
        /// 动画持续时间（毫秒）
        /// </summary>
        public int TransitionDuration { get; set; } = 300;

        /// <summary>
        /// 主题变更事件
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// 主题过渡事件（动画中每帧触发）
        /// </summary>
        public event EventHandler<ThemeTransitionEventArgs> ThemeTransitioning;

        /// <summary>
        /// 主题加载事件
        /// </summary>
        public event EventHandler<ThemeLoadedEventArgs> ThemeLoaded;

        private ThemeManager()
        {
            _themesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindFromCanvas", "Themes");

            // 注册内置主题
            RegisterBuiltInThemes();

            // 设置默认主题
            CurrentTheme = new LightTheme();

            // 加载自定义主题
            LoadCustomThemes();
        }

        /// <summary>
        /// 注册内置主题
        /// </summary>
        private void RegisterBuiltInThemes()
        {
            RegisterTheme(new LightTheme());
            RegisterTheme(new DarkTheme());
        }

        /// <summary>
        /// 注册主题
        /// </summary>
        public void RegisterTheme(ThemeConfig theme)
        {
            if (theme == null) return;
            _registeredThemes[theme.ThemeId] = theme;
        }

        /// <summary>
        /// 注销主题
        /// </summary>
        public bool UnregisterTheme(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return false;
            
            // 不允许删除内置主题
            if (themeId.Equals("light", StringComparison.OrdinalIgnoreCase) ||
                themeId.Equals("dark", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _registeredThemes.Remove(themeId);
        }

        /// <summary>
        /// 获取所有注册的主题
        /// </summary>
        public IEnumerable<ThemeConfig> GetAllThemes()
        {
            return _registeredThemes.Values;
        }

        /// <summary>
        /// 根据ID获取主题
        /// </summary>
        public ThemeConfig GetTheme(string themeId)
        {
            _registeredThemes.TryGetValue(themeId, out var theme);
            return theme;
        }

        /// <summary>
        /// 设置主题（带可选动画）
        /// </summary>
        public void SetTheme(ThemeConfig theme, bool animate = true)
        {
            if (theme == null) return;
            if (_isTransitioning) return; // 正在切换中，忽略新请求

            var oldTheme = CurrentTheme;
            
            if (animate && EnableTransitionAnimation && oldTheme != null)
            {
                // 启动过渡动画
                StartThemeTransition(oldTheme, theme);
            }
            else
            {
                // 直接切换
                CurrentTheme = theme;
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
            }
        }

        /// <summary>
        /// 设置主题（通过ID）
        /// </summary>
        public bool SetThemeById(string themeId, bool animate = true)
        {
            var theme = GetTheme(themeId);
            if (theme == null) return false;
            
            SetTheme(theme, animate);
            return true;
        }

        /// <summary>
        /// 6.4.3 启动主题过渡动画
        /// </summary>
        private async void StartThemeTransition(ThemeConfig fromTheme, ThemeConfig toTheme)
        {
            _isTransitioning = true;
            _currentTransition = new ThemeTransition(fromTheme, toTheme, TransitionDuration);

            try
            {
                var startTime = DateTime.UtcNow;
                while (_currentTransition.Progress < 1.0f)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _currentTransition.Progress = Math.Min(1.0f, (float)(elapsed / TransitionDuration));

                    ThemeTransitioning?.Invoke(this, new ThemeTransitionEventArgs
                    {
                        FromTheme = fromTheme,
                        ToTheme = toTheme,
                        Progress = _currentTransition.Progress,
                        InterpolatedTheme = _currentTransition.GetInterpolatedTheme()
                    });

                    await Task.Delay(16); // ~60 FPS
                }

                // 动画完成
                CurrentTheme = toTheme;
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(fromTheme, toTheme));
            }
            finally
            {
                _isTransitioning = false;
                _currentTransition = null;
            }
        }

        /// <summary>
        /// 切换到浅色主题
        /// </summary>
        public void SetLightTheme(bool animate = true)
        {
            SetThemeById("light", animate);
        }

        /// <summary>
        /// 切换到深色主题
        /// </summary>
        public void SetDarkTheme(bool animate = true)
        {
            SetThemeById("dark", animate);
        }

        /// <summary>
        /// 切换主题（浅色/深色）
        /// </summary>
        public void ToggleTheme(bool animate = true)
        {
            if (CurrentTheme.IsDarkTheme)
            {
                SetLightTheme(animate);
            }
            else
            {
                SetDarkTheme(animate);
            }
        }

        #region 6.4.4 自定义主题导入/导出

        /// <summary>
        /// 从JSON文件导入主题
        /// </summary>
        public ThemeConfig ImportTheme(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Theme file not found", filePath);

            var json = File.ReadAllText(filePath);
            return ImportThemeFromJson(json);
        }

        /// <summary>
        /// 从JSON字符串导入主题
        /// </summary>
        public ThemeConfig ImportThemeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON content is empty");

            var themeData = JsonConvert.DeserializeObject<ThemeData>(json);

            if (themeData == null)
                throw new InvalidOperationException("Failed to parse theme data");

            var theme = new CustomTheme(themeData);
            RegisterTheme(theme);
            
            ThemeLoaded?.Invoke(this, new ThemeLoadedEventArgs { Theme = theme, FilePath = null });
            
            return theme;
        }

        /// <summary>
        /// 导出主题到JSON
        /// </summary>
        public string ExportThemeToJson(ThemeConfig theme, bool prettyPrint = true)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            var themeData = new ThemeData
            {
                Id = theme.ThemeId,
                Name = theme.ThemeName,
                Author = theme.Author,
                Version = theme.Version,
                IsDark = theme.IsDarkTheme,
                Background = ColorToHex(theme.Background),
                Foreground = ColorToHex(theme.Foreground),
                Border = ColorToHex(theme.Border),
                Primary = ColorToHex(theme.Primary),
                Success = ColorToHex(theme.Success),
                Error = ColorToHex(theme.Error),
                Warning = ColorToHex(theme.Warning),
                Info = ColorToHex(theme.Info),
                Ring = ColorToHex(theme.Ring),
                CanvasBackground = ColorToHex(theme.CanvasBackground),
                GridColor = ColorToHex(theme.GridColor),
                ConnectionLine = ColorToHex(theme.ConnectionLine),
                NodeBackground = ColorToHex(theme.NodeBackground),
                NodeBorder = ColorToHex(theme.NodeBorder),
                TextPrimary = ColorToHex(theme.TextPrimary),
                TextSecondary = ColorToHex(theme.TextSecondary)
            };

            var settings = new JsonSerializerSettings
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(themeData, settings);
        }

        /// <summary>
        /// 保存主题到文件
        /// </summary>
        public void SaveTheme(ThemeConfig theme, string filePath)
        {
            var json = ExportThemeToJson(theme);
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 加载自定义主题目录
        /// </summary>
        private void LoadCustomThemes()
        {
            if (!Directory.Exists(_themesDirectory))
            {
                Directory.CreateDirectory(_themesDirectory);
                return;
            }

            foreach (var file in Directory.GetFiles(_themesDirectory, "*.json"))
            {
                try
                {
                    ImportTheme(file);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading theme from {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 颜色转十六进制字符串
        /// </summary>
        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        #endregion

        #region 6.4.5 主题预览

        /// <summary>
        /// 创建主题预览
        /// </summary>
        public ThemePreview CreatePreview(ThemeConfig theme)
        {
            return new ThemePreview(theme);
        }

        #endregion
    }

    /// <summary>
    /// 主题变更事件参数
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeConfig OldTheme { get; }
        public ThemeConfig NewTheme { get; }

        public ThemeChangedEventArgs(ThemeConfig oldTheme, ThemeConfig newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }
    }

    /// <summary>
    /// 主题过渡事件参数
    /// </summary>
    public class ThemeTransitionEventArgs : EventArgs
    {
        public ThemeConfig FromTheme { get; set; }
        public ThemeConfig ToTheme { get; set; }
        public float Progress { get; set; }
        public InterpolatedTheme InterpolatedTheme { get; set; }
    }

    /// <summary>
    /// 主题加载事件参数
    /// </summary>
    public class ThemeLoadedEventArgs : EventArgs
    {
        public ThemeConfig Theme { get; set; }
        public string FilePath { get; set; }
    }

    /// <summary>
    /// 6.4.3 主题过渡辅助类
    /// </summary>
    internal class ThemeTransition
    {
        public ThemeConfig FromTheme { get; }
        public ThemeConfig ToTheme { get; }
        public int Duration { get; }
        public float Progress { get; set; }

        public ThemeTransition(ThemeConfig from, ThemeConfig to, int duration)
        {
            FromTheme = from;
            ToTheme = to;
            Duration = duration;
            Progress = 0;
        }

        public InterpolatedTheme GetInterpolatedTheme()
        {
            // 使用缓动函数（ease-out）
            var easedProgress = EaseOutCubic(Progress);
            
            return new InterpolatedTheme
            {
                Background = InterpolateColor(FromTheme.Background, ToTheme.Background, easedProgress),
                Foreground = InterpolateColor(FromTheme.Foreground, ToTheme.Foreground, easedProgress),
                Border = InterpolateColor(FromTheme.Border, ToTheme.Border, easedProgress),
                Primary = InterpolateColor(FromTheme.Primary, ToTheme.Primary, easedProgress),
                CanvasBackground = InterpolateColor(FromTheme.CanvasBackground, ToTheme.CanvasBackground, easedProgress),
                GridColor = InterpolateColor(FromTheme.GridColor, ToTheme.GridColor, easedProgress),
                NodeBackground = InterpolateColor(FromTheme.NodeBackground, ToTheme.NodeBackground, easedProgress),
                NodeBorder = InterpolateColor(FromTheme.NodeBorder, ToTheme.NodeBorder, easedProgress),
                ConnectionLine = InterpolateColor(FromTheme.ConnectionLine, ToTheme.ConnectionLine, easedProgress)
            };
        }

        private float EaseOutCubic(float t)
        {
            return 1 - (float)Math.Pow(1 - t, 3);
        }

        private Color InterpolateColor(Color from, Color to, float t)
        {
            int a = (int)(from.A + (to.A - from.A) * t);
            int r = (int)(from.R + (to.R - from.R) * t);
            int g = (int)(from.G + (to.G - from.G) * t);
            int b = (int)(from.B + (to.B - from.B) * t);
            return Color.FromArgb(a, r, g, b);
        }
    }

    /// <summary>
    /// 插值主题（用于动画过渡）
    /// </summary>
    public class InterpolatedTheme
    {
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Color Border { get; set; }
        public Color Primary { get; set; }
        public Color CanvasBackground { get; set; }
        public Color GridColor { get; set; }
        public Color NodeBackground { get; set; }
        public Color NodeBorder { get; set; }
        public Color ConnectionLine { get; set; }
    }

    /// <summary>
    /// 6.4.5 主题预览
    /// </summary>
    public class ThemePreview
    {
        public ThemeConfig Theme { get; }

        public ThemePreview(ThemeConfig theme)
        {
            Theme = theme;
        }

        /// <summary>
        /// 绘制预览
        /// </summary>
        public void Draw(Graphics g, Rectangle bounds)
        {
            // 绘制背景
            using (var bgBrush = new SolidBrush(Theme.CanvasBackground))
            {
                g.FillRectangle(bgBrush, bounds);
            }

            // 绘制网格
            using (var gridPen = new Pen(Theme.GridColor))
            {
                int gridSize = 20;
                for (int x = bounds.Left; x < bounds.Right; x += gridSize)
                {
                    g.DrawLine(gridPen, x, bounds.Top, x, bounds.Bottom);
                }
                for (int y = bounds.Top; y < bounds.Bottom; y += gridSize)
                {
                    g.DrawLine(gridPen, bounds.Left, y, bounds.Right, y);
                }
            }

            // 绘制示例节点
            var nodeRect = new Rectangle(bounds.X + 20, bounds.Y + 20, 80, 40);
            using (var nodeBrush = new SolidBrush(Theme.NodeBackground))
            using (var nodePen = new Pen(Theme.NodeBorder, 2))
            {
                g.FillRectangle(nodeBrush, nodeRect);
                g.DrawRectangle(nodePen, nodeRect);
            }

            // 绘制示例连线
            using (var linePen = new Pen(Theme.ConnectionLine, 2))
            {
                g.DrawLine(linePen, 
                    nodeRect.Right, nodeRect.Y + nodeRect.Height / 2,
                    bounds.Right - 20, nodeRect.Y + nodeRect.Height / 2);
            }

            // 绘制主题名称
            using (var textBrush = new SolidBrush(Theme.TextPrimary))
            using (var font = new Font("Segoe UI", 9))
            {
                g.DrawString(Theme.ThemeName, font, textBrush, bounds.X + 5, bounds.Bottom - 20);
            }
        }
    }
}
