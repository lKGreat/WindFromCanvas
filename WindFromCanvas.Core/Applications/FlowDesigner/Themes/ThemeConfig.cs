using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Themes
{
    /// <summary>
    /// 6.4.1 主题配置基类（扩展属性版本）
    /// </summary>
    public abstract class ThemeConfig
    {
        // ==================== 基础颜色 ====================
        public abstract Color Background { get; }
        public abstract Color Foreground { get; }
        public abstract Color Border { get; }
        public abstract Color Primary { get; }
        public abstract Color Success { get; }
        public abstract Color Error { get; }
        public abstract Color Warning { get; }
        public abstract Color Info { get; }
        public abstract Color Ring { get; } // 悬停边框色
        
        // ==================== 画布颜色 ====================
        public abstract Color CanvasBackground { get; }
        public abstract Color GridColor { get; }
        public abstract Color GridColorMinor { get; }
        public abstract Color SelectionRect { get; }
        public abstract Color SelectionFill { get; }
        
        // ==================== 节点颜色 ====================
        public abstract Color NodeBackground { get; }
        public abstract Color NodeBorder { get; }
        public abstract Color NodeBorderHover { get; }
        public abstract Color NodeBorderSelected { get; }
        public abstract Color NodeShadow { get; }
        public abstract Color NodeHeaderBackground { get; }
        public abstract Color NodeHeaderText { get; }
        
        // ==================== 连接线颜色 ====================
        public abstract Color ConnectionLine { get; }
        public abstract Color ConnectionHover { get; }
        public abstract Color ConnectionSelected { get; }
        public abstract Color ConnectionShadow { get; }
        public abstract Color ConnectionPreview { get; }
        
        // ==================== 锚点颜色 ====================
        public abstract Color AnchorDefault { get; }
        public abstract Color AnchorHover { get; }
        public abstract Color AnchorActive { get; }
        public abstract Color AnchorConnected { get; }
        
        // ==================== UI组件颜色 ====================
        public abstract Color ToolbarBackground { get; }
        public abstract Color ToolbarBorder { get; }
        public abstract Color ButtonBackground { get; }
        public abstract Color ButtonHover { get; }
        public abstract Color ButtonActive { get; }
        public abstract Color InputBackground { get; }
        public abstract Color InputBorder { get; }
        public abstract Color InputFocus { get; }
        
        // ==================== 文本颜色 ====================
        public abstract Color TextPrimary { get; }
        public abstract Color TextSecondary { get; }
        public abstract Color TextMuted { get; }
        public abstract Color TextDisabled { get; }
        
        // ==================== 状态颜色 ====================
        public abstract Color StatusRunning { get; }
        public abstract Color StatusSuccess { get; }
        public abstract Color StatusError { get; }
        public abstract Color StatusPending { get; }
        public abstract Color StatusSkipped { get; }

        // ==================== 主题元数据 ====================
        public virtual string ThemeName => GetType().Name;
        public virtual string ThemeId => ThemeName.ToLowerInvariant();
        public virtual bool IsDarkTheme => false;
        public virtual string Author => "System";
        public virtual string Version => "1.0";
    }

    /// <summary>
    /// 浅色主题（基于Activepieces）
    /// </summary>
    public class LightTheme : ThemeConfig
    {
        // 基础颜色
        public override Color Background => Color.FromArgb(255, 255, 255);
        public override Color Foreground => Color.FromArgb(15, 23, 42);
        public override Color Border => Color.FromArgb(226, 232, 240);
        public override Color Primary => Color.FromArgb(59, 130, 246);
        public override Color Success => Color.FromArgb(16, 185, 129);
        public override Color Error => Color.FromArgb(239, 68, 68);
        public override Color Warning => Color.FromArgb(234, 179, 8);
        public override Color Info => Color.FromArgb(59, 130, 246);
        public override Color Ring => Color.FromArgb(148, 163, 184);

        // 画布颜色
        public override Color CanvasBackground => Color.FromArgb(250, 250, 250);
        public override Color GridColor => Color.FromArgb(226, 232, 240);
        public override Color GridColorMinor => Color.FromArgb(241, 245, 249);
        public override Color SelectionRect => Color.FromArgb(59, 130, 246);
        public override Color SelectionFill => Color.FromArgb(30, 59, 130, 246);

        // 节点颜色
        public override Color NodeBackground => Color.FromArgb(255, 255, 255);
        public override Color NodeBorder => Color.FromArgb(226, 232, 240);
        public override Color NodeBorderHover => Color.FromArgb(148, 163, 184);
        public override Color NodeBorderSelected => Color.FromArgb(59, 130, 246);
        public override Color NodeShadow => Color.FromArgb(20, 0, 0, 0);
        public override Color NodeHeaderBackground => Color.FromArgb(248, 250, 252);
        public override Color NodeHeaderText => Color.FromArgb(15, 23, 42);

        // 连接线颜色
        public override Color ConnectionLine => Color.FromArgb(148, 163, 184);
        public override Color ConnectionHover => Color.FromArgb(59, 130, 246);
        public override Color ConnectionSelected => Color.FromArgb(59, 130, 246);
        public override Color ConnectionShadow => Color.FromArgb(30, 0, 0, 0);
        public override Color ConnectionPreview => Color.FromArgb(128, 59, 130, 246);

        // 锚点颜色
        public override Color AnchorDefault => Color.FromArgb(148, 163, 184);
        public override Color AnchorHover => Color.FromArgb(59, 130, 246);
        public override Color AnchorActive => Color.FromArgb(37, 99, 235);
        public override Color AnchorConnected => Color.FromArgb(16, 185, 129);

        // UI组件颜色
        public override Color ToolbarBackground => Color.FromArgb(255, 255, 255);
        public override Color ToolbarBorder => Color.FromArgb(226, 232, 240);
        public override Color ButtonBackground => Color.FromArgb(241, 245, 249);
        public override Color ButtonHover => Color.FromArgb(226, 232, 240);
        public override Color ButtonActive => Color.FromArgb(203, 213, 225);
        public override Color InputBackground => Color.FromArgb(255, 255, 255);
        public override Color InputBorder => Color.FromArgb(226, 232, 240);
        public override Color InputFocus => Color.FromArgb(59, 130, 246);

        // 文本颜色
        public override Color TextPrimary => Color.FromArgb(15, 23, 42);
        public override Color TextSecondary => Color.FromArgb(71, 85, 105);
        public override Color TextMuted => Color.FromArgb(148, 163, 184);
        public override Color TextDisabled => Color.FromArgb(203, 213, 225);

        // 状态颜色
        public override Color StatusRunning => Color.FromArgb(59, 130, 246);
        public override Color StatusSuccess => Color.FromArgb(16, 185, 129);
        public override Color StatusError => Color.FromArgb(239, 68, 68);
        public override Color StatusPending => Color.FromArgb(148, 163, 184);
        public override Color StatusSkipped => Color.FromArgb(203, 213, 225);

        public override string ThemeName => "Light";
        public override bool IsDarkTheme => false;
    }

    /// <summary>
    /// 6.4.2 深色主题（基于Activepieces深色模式）
    /// </summary>
    public class DarkTheme : ThemeConfig
    {
        // 基础颜色
        public override Color Background => Color.FromArgb(15, 23, 42);
        public override Color Foreground => Color.FromArgb(248, 250, 252);
        public override Color Border => Color.FromArgb(51, 65, 85);
        public override Color Primary => Color.FromArgb(96, 165, 250);
        public override Color Success => Color.FromArgb(52, 211, 153);
        public override Color Error => Color.FromArgb(248, 113, 113);
        public override Color Warning => Color.FromArgb(251, 191, 36);
        public override Color Info => Color.FromArgb(96, 165, 250);
        public override Color Ring => Color.FromArgb(100, 116, 139);

        // 画布颜色
        public override Color CanvasBackground => Color.FromArgb(15, 23, 42);
        public override Color GridColor => Color.FromArgb(51, 65, 85);
        public override Color GridColorMinor => Color.FromArgb(30, 41, 59);
        public override Color SelectionRect => Color.FromArgb(96, 165, 250);
        public override Color SelectionFill => Color.FromArgb(30, 96, 165, 250);

        // 节点颜色
        public override Color NodeBackground => Color.FromArgb(30, 41, 59);
        public override Color NodeBorder => Color.FromArgb(51, 65, 85);
        public override Color NodeBorderHover => Color.FromArgb(100, 116, 139);
        public override Color NodeBorderSelected => Color.FromArgb(96, 165, 250);
        public override Color NodeShadow => Color.FromArgb(40, 0, 0, 0);
        public override Color NodeHeaderBackground => Color.FromArgb(51, 65, 85);
        public override Color NodeHeaderText => Color.FromArgb(248, 250, 252);

        // 连接线颜色
        public override Color ConnectionLine => Color.FromArgb(100, 116, 139);
        public override Color ConnectionHover => Color.FromArgb(96, 165, 250);
        public override Color ConnectionSelected => Color.FromArgb(96, 165, 250);
        public override Color ConnectionShadow => Color.FromArgb(50, 0, 0, 0);
        public override Color ConnectionPreview => Color.FromArgb(128, 96, 165, 250);

        // 锚点颜色
        public override Color AnchorDefault => Color.FromArgb(100, 116, 139);
        public override Color AnchorHover => Color.FromArgb(96, 165, 250);
        public override Color AnchorActive => Color.FromArgb(59, 130, 246);
        public override Color AnchorConnected => Color.FromArgb(52, 211, 153);

        // UI组件颜色
        public override Color ToolbarBackground => Color.FromArgb(30, 41, 59);
        public override Color ToolbarBorder => Color.FromArgb(51, 65, 85);
        public override Color ButtonBackground => Color.FromArgb(51, 65, 85);
        public override Color ButtonHover => Color.FromArgb(71, 85, 105);
        public override Color ButtonActive => Color.FromArgb(100, 116, 139);
        public override Color InputBackground => Color.FromArgb(30, 41, 59);
        public override Color InputBorder => Color.FromArgb(51, 65, 85);
        public override Color InputFocus => Color.FromArgb(96, 165, 250);

        // 文本颜色
        public override Color TextPrimary => Color.FromArgb(248, 250, 252);
        public override Color TextSecondary => Color.FromArgb(203, 213, 225);
        public override Color TextMuted => Color.FromArgb(148, 163, 184);
        public override Color TextDisabled => Color.FromArgb(100, 116, 139);

        // 状态颜色
        public override Color StatusRunning => Color.FromArgb(96, 165, 250);
        public override Color StatusSuccess => Color.FromArgb(52, 211, 153);
        public override Color StatusError => Color.FromArgb(248, 113, 113);
        public override Color StatusPending => Color.FromArgb(100, 116, 139);
        public override Color StatusSkipped => Color.FromArgb(71, 85, 105);

        public override string ThemeName => "Dark";
        public override bool IsDarkTheme => true;
    }

    /// <summary>
    /// 6.4.4 自定义主题（可从JSON导入）
    /// </summary>
    public class CustomTheme : ThemeConfig
    {
        private readonly ThemeData _data;

        public CustomTheme(ThemeData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        // 基础颜色
        public override Color Background => ParseColor(_data.Background);
        public override Color Foreground => ParseColor(_data.Foreground);
        public override Color Border => ParseColor(_data.Border);
        public override Color Primary => ParseColor(_data.Primary);
        public override Color Success => ParseColor(_data.Success);
        public override Color Error => ParseColor(_data.Error);
        public override Color Warning => ParseColor(_data.Warning);
        public override Color Info => ParseColor(_data.Info ?? _data.Primary);
        public override Color Ring => ParseColor(_data.Ring);

        // 画布颜色
        public override Color CanvasBackground => ParseColor(_data.CanvasBackground ?? _data.Background);
        public override Color GridColor => ParseColor(_data.GridColor);
        public override Color GridColorMinor => ParseColor(_data.GridColorMinor ?? _data.GridColor);
        public override Color SelectionRect => ParseColor(_data.SelectionRect ?? _data.Primary);
        public override Color SelectionFill => ParseColorWithAlpha(_data.SelectionFill ?? _data.Primary, 30);

        // 节点颜色
        public override Color NodeBackground => ParseColor(_data.NodeBackground ?? _data.Background);
        public override Color NodeBorder => ParseColor(_data.NodeBorder ?? _data.Border);
        public override Color NodeBorderHover => ParseColor(_data.NodeBorderHover ?? _data.Ring);
        public override Color NodeBorderSelected => ParseColor(_data.NodeBorderSelected ?? _data.Primary);
        public override Color NodeShadow => ParseColorWithAlpha(_data.NodeShadow ?? "#000000", 20);
        public override Color NodeHeaderBackground => ParseColor(_data.NodeHeaderBackground ?? _data.Background);
        public override Color NodeHeaderText => ParseColor(_data.NodeHeaderText ?? _data.Foreground);

        // 连接线颜色
        public override Color ConnectionLine => ParseColor(_data.ConnectionLine);
        public override Color ConnectionHover => ParseColor(_data.ConnectionHover ?? _data.Primary);
        public override Color ConnectionSelected => ParseColor(_data.ConnectionSelected ?? _data.Primary);
        public override Color ConnectionShadow => ParseColorWithAlpha(_data.ConnectionShadow ?? "#000000", 30);
        public override Color ConnectionPreview => ParseColorWithAlpha(_data.ConnectionPreview ?? _data.Primary, 128);

        // 锚点颜色
        public override Color AnchorDefault => ParseColor(_data.AnchorDefault ?? _data.Ring);
        public override Color AnchorHover => ParseColor(_data.AnchorHover ?? _data.Primary);
        public override Color AnchorActive => ParseColor(_data.AnchorActive ?? _data.Primary);
        public override Color AnchorConnected => ParseColor(_data.AnchorConnected ?? _data.Success);

        // UI组件颜色
        public override Color ToolbarBackground => ParseColor(_data.ToolbarBackground ?? _data.Background);
        public override Color ToolbarBorder => ParseColor(_data.ToolbarBorder ?? _data.Border);
        public override Color ButtonBackground => ParseColor(_data.ButtonBackground ?? _data.Background);
        public override Color ButtonHover => ParseColor(_data.ButtonHover ?? _data.Border);
        public override Color ButtonActive => ParseColor(_data.ButtonActive ?? _data.Ring);
        public override Color InputBackground => ParseColor(_data.InputBackground ?? _data.Background);
        public override Color InputBorder => ParseColor(_data.InputBorder ?? _data.Border);
        public override Color InputFocus => ParseColor(_data.InputFocus ?? _data.Primary);

        // 文本颜色
        public override Color TextPrimary => ParseColor(_data.TextPrimary ?? _data.Foreground);
        public override Color TextSecondary => ParseColor(_data.TextSecondary ?? _data.Foreground);
        public override Color TextMuted => ParseColor(_data.TextMuted ?? _data.Ring);
        public override Color TextDisabled => ParseColor(_data.TextDisabled ?? _data.Border);

        // 状态颜色
        public override Color StatusRunning => ParseColor(_data.StatusRunning ?? _data.Primary);
        public override Color StatusSuccess => ParseColor(_data.StatusSuccess ?? _data.Success);
        public override Color StatusError => ParseColor(_data.StatusError ?? _data.Error);
        public override Color StatusPending => ParseColor(_data.StatusPending ?? _data.Ring);
        public override Color StatusSkipped => ParseColor(_data.StatusSkipped ?? _data.Border);

        public override string ThemeName => _data.Name ?? "Custom";
        public override string ThemeId => _data.Id ?? ThemeName.ToLowerInvariant();
        public override bool IsDarkTheme => _data.IsDark;
        public override string Author => _data.Author ?? "Unknown";
        public override string Version => _data.Version ?? "1.0";

        /// <summary>
        /// 解析颜色字符串
        /// </summary>
        private Color ParseColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return Color.Gray;

            try
            {
                if (colorString.StartsWith("#"))
                {
                    return ColorTranslator.FromHtml(colorString);
                }
                else if (colorString.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    // 解析 rgb(r, g, b) 或 rgba(r, g, b, a)
                    var values = colorString
                        .Replace("rgba(", "").Replace("rgb(", "").Replace(")", "")
                        .Split(',');
                    
                    int r = int.Parse(values[0].Trim());
                    int g = int.Parse(values[1].Trim());
                    int b = int.Parse(values[2].Trim());
                    int a = values.Length > 3 ? (int)(float.Parse(values[3].Trim()) * 255) : 255;
                    
                    return Color.FromArgb(a, r, g, b);
                }
                else
                {
                    return Color.FromName(colorString);
                }
            }
            catch
            {
                return Color.Gray;
            }
        }

        /// <summary>
        /// 解析颜色并设置透明度
        /// </summary>
        private Color ParseColorWithAlpha(string colorString, int alpha)
        {
            var color = ParseColor(colorString);
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }

    /// <summary>
    /// 主题数据（用于JSON序列化/反序列化）
    /// </summary>
    public class ThemeData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public bool IsDark { get; set; }

        // 基础颜色
        public string Background { get; set; }
        public string Foreground { get; set; }
        public string Border { get; set; }
        public string Primary { get; set; }
        public string Success { get; set; }
        public string Error { get; set; }
        public string Warning { get; set; }
        public string Info { get; set; }
        public string Ring { get; set; }

        // 画布颜色
        public string CanvasBackground { get; set; }
        public string GridColor { get; set; }
        public string GridColorMinor { get; set; }
        public string SelectionRect { get; set; }
        public string SelectionFill { get; set; }

        // 节点颜色
        public string NodeBackground { get; set; }
        public string NodeBorder { get; set; }
        public string NodeBorderHover { get; set; }
        public string NodeBorderSelected { get; set; }
        public string NodeShadow { get; set; }
        public string NodeHeaderBackground { get; set; }
        public string NodeHeaderText { get; set; }

        // 连接线颜色
        public string ConnectionLine { get; set; }
        public string ConnectionHover { get; set; }
        public string ConnectionSelected { get; set; }
        public string ConnectionShadow { get; set; }
        public string ConnectionPreview { get; set; }

        // 锚点颜色
        public string AnchorDefault { get; set; }
        public string AnchorHover { get; set; }
        public string AnchorActive { get; set; }
        public string AnchorConnected { get; set; }

        // UI组件颜色
        public string ToolbarBackground { get; set; }
        public string ToolbarBorder { get; set; }
        public string ButtonBackground { get; set; }
        public string ButtonHover { get; set; }
        public string ButtonActive { get; set; }
        public string InputBackground { get; set; }
        public string InputBorder { get; set; }
        public string InputFocus { get; set; }

        // 文本颜色
        public string TextPrimary { get; set; }
        public string TextSecondary { get; set; }
        public string TextMuted { get; set; }
        public string TextDisabled { get; set; }

        // 状态颜色
        public string StatusRunning { get; set; }
        public string StatusSuccess { get; set; }
        public string StatusError { get; set; }
        public string StatusPending { get; set; }
        public string StatusSkipped { get; set; }
    }
}
