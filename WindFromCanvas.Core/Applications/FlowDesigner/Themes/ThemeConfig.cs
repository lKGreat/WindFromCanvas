using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Themes
{
    /// <summary>
    /// 主题配置基类
    /// </summary>
    public abstract class ThemeConfig
    {
        public abstract Color Background { get; }
        public abstract Color Foreground { get; }
        public abstract Color Border { get; }
        public abstract Color Primary { get; }
        public abstract Color Success { get; }
        public abstract Color Error { get; }
        public abstract Color Warning { get; }
        public abstract Color Ring { get; } // 悬停边框色
        public abstract Color GridColor { get; }
    }

    /// <summary>
    /// 浅色主题（基于Activepieces）
    /// </summary>
    public class LightTheme : ThemeConfig
    {
        public override Color Background => Color.FromArgb(255, 255, 255); // #FFFFFF
        public override Color Foreground => Color.FromArgb(15, 23, 42); // #0F172A
        public override Color Border => Color.FromArgb(226, 232, 240); // #E2E8F0
        public override Color Primary => Color.FromArgb(59, 130, 246); // #3B82F6
        public override Color Success => Color.FromArgb(16, 185, 129); // #10B981
        public override Color Error => Color.FromArgb(239, 68, 68); // #EF4444
        public override Color Warning => Color.FromArgb(234, 179, 8); // #EAB308
        public override Color Ring => Color.FromArgb(148, 163, 184); // #94A3B8
        public override Color GridColor => Color.FromArgb(226, 232, 240); // #E2E8F0
    }

    /// <summary>
    /// 深色主题（基于Activepieces深色模式）
    /// </summary>
    public class DarkTheme : ThemeConfig
    {
        public override Color Background => Color.FromArgb(15, 23, 42); // #0F172A
        public override Color Foreground => Color.FromArgb(248, 250, 252); // #F8FAFC
        public override Color Border => Color.FromArgb(51, 65, 85); // #334155
        public override Color Primary => Color.FromArgb(96, 165, 250); // #60A5FA
        public override Color Success => Color.FromArgb(52, 211, 153); // #34D399
        public override Color Error => Color.FromArgb(248, 113, 113); // #F87171
        public override Color Warning => Color.FromArgb(251, 191, 36); // #FBBF24
        public override Color Ring => Color.FromArgb(100, 116, 139); // #64748B
        public override Color GridColor => Color.FromArgb(51, 65, 85); // #334155
    }
}
