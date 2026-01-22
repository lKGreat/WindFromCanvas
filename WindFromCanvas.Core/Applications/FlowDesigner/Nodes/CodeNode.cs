using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 代码节点 - 执行自定义代码脚本
    /// 标准：232x60px圆角矩形，青色边框，代码图标(<>)
    /// </summary>
    public class CodeNode : FlowNode
    {
        /// <summary>
        /// 脚本代码内容
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// 脚本语言（默认C#）
        /// </summary>
        public string ScriptLanguage { get; set; } = "csharp";

        /// <summary>
        /// 脚本超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        public CodeNode() : base()
        {
            Width = NodeSizeConstants.CodeWidth;
            Height = NodeSizeConstants.CodeHeight;
            BackgroundColor = NodeColorConstants.WhiteBackground;
            BorderColor = NodeColorConstants.CodeCyan;
            TextColor = NodeColorConstants.TextPrimary;
            CornerRadius = NodeSizeConstants.CornerRadius;
            EnableShadow = true;
            Draggable = true;
        }

        public CodeNode(FlowNodeData data) : base(data)
        {
            Width = NodeSizeConstants.CodeWidth;
            Height = NodeSizeConstants.CodeHeight;
            BackgroundColor = NodeColorConstants.WhiteBackground;
            BorderColor = NodeColorConstants.CodeCyan;
            TextColor = NodeColorConstants.TextPrimary;
            CornerRadius = NodeSizeConstants.CornerRadius;
            EnableShadow = true;
            Draggable = true;

            // 从Data加载脚本信息
            if (data?.Properties != null)
            {
                if (data.Properties.ContainsKey("script"))
                    Script = data.Properties["script"]?.ToString();
                if (data.Properties.ContainsKey("scriptLanguage"))
                    ScriptLanguage = data.Properties["scriptLanguage"]?.ToString();
                if (data.Properties.ContainsKey("timeoutMs") && int.TryParse(data.Properties["timeoutMs"]?.ToString(), out var timeout))
                    TimeoutMs = timeout;
            }
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            var bounds = GetBounds();
            var rect = new RectangleF(X, Y, Width, Height);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 创建圆角矩形路径
            using (var path = CreateRoundedRectangle(rect, CornerRadius))
            {
                // 绘制阴影
                if (EnableShadow)
                {
                    DrawShadow(g, path);
                }

                // 填充背景
                using (var brush = new SolidBrush(BackgroundColor))
                {
                    g.FillPath(brush, path);
                }

                // 绘制边框
                var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
                var borderWidth = IsSelected ? SelectedBorderWidth : BorderWidth;
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 绘制代码图标(<>)
            DrawCodeIcon(g, rect);

            // 绘制节点名称
            DrawText(g, rect);

            // 绘制端口
            DrawPorts(g);

            // 绘制验证错误图标
            if (!string.IsNullOrEmpty(ValidationError) || (Data != null && !Data.Valid))
            {
                DrawValidationError(g, rect);
            }

            // 绘制状态指示器
            if (Data != null && Data.Status != NodeStatus.None)
            {
                DrawStatusIndicator(g, rect);
            }
        }

        /// <summary>
        /// 绘制代码图标(<>)
        /// </summary>
        protected virtual void DrawCodeIcon(Graphics g, RectangleF rect)
        {
            var iconSize = NodeSizeConstants.IconSize;
            var iconRect = new RectangleF(
                rect.X + 10,
                rect.Y + (rect.Height - iconSize) / 2,
                iconSize,
                iconSize
            );

            // 绘制图标背景
            using (var bgPath = CreateRoundedRectangle(iconRect, 4f))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(224, 247, 250))) // 淡青色背景
                {
                    g.FillPath(bgBrush, bgPath);
                }
                using (var bgPen = new Pen(NodeColorConstants.CodeCyan, 1f))
                {
                    g.DrawPath(bgPen, bgPath);
                }
            }

            // 绘制<>符号
            using (var font = new Font("Consolas", 11f, FontStyle.Bold))
            using (var brush = new SolidBrush(NodeColorConstants.CodeCyan))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("<>", font, brush, iconRect, format);
            }
        }

        /// <summary>
        /// 保存节点数据
        /// </summary>
        public void SaveToData()
        {
            if (Data == null) return;

            if (Data.Properties == null)
                Data.Properties = new System.Collections.Generic.Dictionary<string, object>();

            Data.Properties["script"] = Script ?? string.Empty;
            Data.Properties["scriptLanguage"] = ScriptLanguage ?? "csharp";
            Data.Properties["timeoutMs"] = TimeoutMs;
        }
    }
}
