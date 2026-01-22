using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 组件节点 - 引用可复用组件
    /// 标准：232x60px圆角矩形，棕色边框，组件图标(◈)
    /// </summary>
    public class PieceNode : FlowNode
    {
        /// <summary>
        /// 组件ID
        /// </summary>
        public string PieceId { get; set; }

        /// <summary>
        /// 组件版本
        /// </summary>
        public string PieceVersion { get; set; }

        /// <summary>
        /// 组件类型
        /// </summary>
        public string PieceType { get; set; }

        /// <summary>
        /// 组件配置参数
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> PieceConfig { get; set; }

        public PieceNode() : base()
        {
            Width = NodeSizeConstants.PieceWidth;
            Height = NodeSizeConstants.PieceHeight;
            BackgroundColor = NodeColorConstants.WhiteBackground;
            BorderColor = NodeColorConstants.PieceBrown;
            TextColor = NodeColorConstants.TextPrimary;
            CornerRadius = NodeSizeConstants.CornerRadius;
            EnableShadow = true;
            Draggable = true;

            PieceConfig = new System.Collections.Generic.Dictionary<string, object>();
        }

        public PieceNode(FlowNodeData data) : base(data)
        {
            Width = NodeSizeConstants.PieceWidth;
            Height = NodeSizeConstants.PieceHeight;
            BackgroundColor = NodeColorConstants.WhiteBackground;
            BorderColor = NodeColorConstants.PieceBrown;
            TextColor = NodeColorConstants.TextPrimary;
            CornerRadius = NodeSizeConstants.CornerRadius;
            EnableShadow = true;
            Draggable = true;

            PieceConfig = new System.Collections.Generic.Dictionary<string, object>();

            // 从Data加载组件信息
            if (data?.Properties != null)
            {
                if (data.Properties.ContainsKey("pieceId"))
                    PieceId = data.Properties["pieceId"]?.ToString();
                if (data.Properties.ContainsKey("pieceVersion"))
                    PieceVersion = data.Properties["pieceVersion"]?.ToString();
                if (data.Properties.ContainsKey("pieceType"))
                    PieceType = data.Properties["pieceType"]?.ToString();
                if (data.Properties.ContainsKey("pieceConfig") && data.Properties["pieceConfig"] is System.Collections.Generic.Dictionary<string, object> config)
                    PieceConfig = config;
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

            // 绘制组件图标(◈)
            DrawPieceIcon(g, rect);

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
        /// 绘制组件图标(◈)
        /// </summary>
        protected virtual void DrawPieceIcon(Graphics g, RectangleF rect)
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
                using (var bgBrush = new SolidBrush(Color.FromArgb(239, 235, 233))) // 淡棕色背景
                {
                    g.FillPath(bgBrush, bgPath);
                }
                using (var bgPen = new Pen(NodeColorConstants.PieceBrown, 1f))
                {
                    g.DrawPath(bgPen, bgPath);
                }
            }

            // 绘制菱形图标
            var centerX = iconRect.X + iconRect.Width / 2;
            var centerY = iconRect.Y + iconRect.Height / 2;
            var size = iconRect.Width * 0.5f;

            using (var path = new GraphicsPath())
            {
                // 创建菱形路径
                path.AddPolygon(new PointF[]
                {
                    new PointF(centerX, centerY - size),           // 上
                    new PointF(centerX + size, centerY),           // 右
                    new PointF(centerX, centerY + size),           // 下
                    new PointF(centerX - size, centerY)            // 左
                });
                path.CloseFigure();

                using (var brush = new SolidBrush(NodeColorConstants.PieceBrown))
                {
                    g.FillPath(brush, path);
                }

                // 绘制内部小菱形
                var innerSize = size * 0.5f;
                using (var innerPath = new GraphicsPath())
                {
                    innerPath.AddPolygon(new PointF[]
                    {
                        new PointF(centerX, centerY - innerSize),
                        new PointF(centerX + innerSize, centerY),
                        new PointF(centerX, centerY + innerSize),
                        new PointF(centerX - innerSize, centerY)
                    });
                    innerPath.CloseFigure();

                    using (var innerBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(innerBrush, innerPath);
                    }
                }
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

            Data.Properties["pieceId"] = PieceId ?? string.Empty;
            Data.Properties["pieceVersion"] = PieceVersion ?? string.Empty;
            Data.Properties["pieceType"] = PieceType ?? string.Empty;
            Data.Properties["pieceConfig"] = PieceConfig;
        }
    }
}
