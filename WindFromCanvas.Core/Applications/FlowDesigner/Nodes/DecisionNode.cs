using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 判断节点 - 菱形黄色节点，1个输入端口，2个输出端口（True/False）
    /// </summary>
    public class DecisionNode : FlowNode
    {
        public DecisionNode() : base()
        {
            Width = 232f; // Activepieces标准尺寸
            Height = 60f;
            BackgroundColor = Color.FromArgb(255, 255, 255); // 白色背景
            BorderColor = Color.FromArgb(226, 232, 240);
            TextColor = Color.FromArgb(15, 23, 42);
            Draggable = true;
            EnableShadow = true;
            
            // 判断节点：1个输入端口，2个输出端口
            InputPorts.Add(new PointF(0, Height / 2)); // 左侧中点
            OutputPorts.Add(new PointF(Width, Height / 3)); // 右侧上（True）
            OutputPorts.Add(new PointF(Width, Height * 2 / 3)); // 右侧下（False）
        }

        public DecisionNode(FlowNodeData data) : base(data)
        {
            Width = 232f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(255, 255, 255);
            BorderColor = Color.FromArgb(226, 232, 240);
            TextColor = Color.FromArgb(15, 23, 42);
            Draggable = true;
            EnableShadow = true;
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            var bounds = GetBounds();
            var rect = new RectangleF(X, Y, Width, Height);

            // 绘制菱形路径（判断节点使用菱形）
            using (var path = CreateDiamondPath(bounds))
            {
                // 绘制阴影（如果启用）
                if (EnableShadow)
                {
                    DrawShadow(g, path);
                }

                // 填充背景（渐变或纯色）
                if (EnableGradient && GradientStartColor != Color.Empty && GradientEndColor != Color.Empty)
                {
                    DrawGradientBackground(g, path, rect);
                }
                else
                {
                    using (var brush = new SolidBrush(BackgroundColor))
                    {
                        g.FillPath(brush, path);
                    }
                }

                // 绘制边框
                var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
                var borderWidth = IsSelected ? SelectedBorderWidth : BorderWidth;
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 绘制节点图标
            DrawIcon(g, rect);

            // 绘制文本
            var text = Data?.DisplayName ?? Data?.Name ?? "判断";
            using (var brush = new SolidBrush(TextColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(text, SystemFonts.DefaultFont, brush, bounds, sf);
            }

            // 绘制端口
            DrawPorts(g);

            // 绘制验证错误图标
            if (!string.IsNullOrEmpty(ValidationError) || (Data != null && !Data.Valid))
            {
                DrawValidationError(g, rect);
            }
        }

        /// <summary>
        /// 创建菱形路径（判断节点使用菱形，但保持232x60尺寸）
        /// </summary>
        private GraphicsPath CreateDiamondPath(RectangleF rect)
        {
            var path = new GraphicsPath();
            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;
            var borderWidth = IsSelected ? SelectedBorderWidth : BorderWidth;
            var halfWidth = rect.Width / 2 - borderWidth;
            var halfHeight = rect.Height / 2 - borderWidth;

            path.AddPolygon(new[]
            {
                new PointF(centerX, rect.Y + borderWidth), // 上
                new PointF(rect.Right - borderWidth, centerY), // 右
                new PointF(centerX, rect.Bottom - borderWidth), // 下
                new PointF(rect.X + borderWidth, centerY) // 左
            });
            path.CloseFigure();

            return path;
        }

        public override bool HitTest(PointF point)
        {
            var bounds = GetBounds();
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;
            var halfWidth = bounds.Width / 2;
            var halfHeight = bounds.Height / 2;

            // 检查点是否在菱形内
            var dx = Math.Abs(point.X - centerX);
            var dy = Math.Abs(point.Y - centerY);
            
            // 菱形方程：(dx / halfWidth) + (dy / halfHeight) <= 1
            return (dx / halfWidth) + (dy / halfHeight) <= 1;
        }

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        protected override void DrawPorts(Graphics g)
        {
            var bounds = GetBounds();
            
            // 更新端口位置
            if (InputPorts.Count == 0)
            {
                InputPorts.Add(new PointF(bounds.Left, bounds.Y + bounds.Height / 2));
            }
            else
            {
                InputPorts[0] = new PointF(bounds.Left, bounds.Y + bounds.Height / 2);
            }

            if (OutputPorts.Count < 2)
            {
                OutputPorts.Clear();
                OutputPorts.Add(new PointF(bounds.Right, bounds.Y + bounds.Height / 3));
                OutputPorts.Add(new PointF(bounds.Right, bounds.Y + bounds.Height * 2 / 3));
            }
            else
            {
                OutputPorts[0] = new PointF(bounds.Right, bounds.Y + bounds.Height / 3);
                OutputPorts[1] = new PointF(bounds.Right, bounds.Y + bounds.Height * 2 / 3);
            }

            base.DrawPorts(g);
        }
    }
}
