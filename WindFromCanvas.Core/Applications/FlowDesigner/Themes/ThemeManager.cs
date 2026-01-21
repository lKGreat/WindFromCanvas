using System;
using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Themes
{
    /// <summary>
    /// 主题管理器（单例）
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private ThemeConfig _currentTheme;

        /// <summary>
        /// 主题管理器实例
        /// </summary>
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ThemeManager();
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
        /// 主题变更事件
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        private ThemeManager()
        {
            CurrentTheme = new LightTheme();
        }

        /// <summary>
        /// 设置主题
        /// </summary>
        public void SetTheme(ThemeConfig theme)
        {
            if (theme == null) return;

            var oldTheme = CurrentTheme;
            CurrentTheme = theme;
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
        }

        /// <summary>
        /// 切换到浅色主题
        /// </summary>
        public void SetLightTheme()
        {
            SetTheme(new LightTheme());
        }

        /// <summary>
        /// 切换到深色主题
        /// </summary>
        public void SetDarkTheme()
        {
            SetTheme(new DarkTheme());
        }

        /// <summary>
        /// 切换主题（浅色/深色）
        /// </summary>
        public void ToggleTheme()
        {
            if (CurrentTheme is LightTheme)
            {
                SetDarkTheme();
            }
            else
            {
                SetLightTheme();
            }
        }
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
}
