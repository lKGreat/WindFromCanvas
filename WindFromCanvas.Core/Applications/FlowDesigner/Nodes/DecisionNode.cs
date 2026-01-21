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
            Width = 120f;
            Height = 80f;
            BackgroundColor = Color.FromArgb(255, 193, 7); // 黄色
            BorderColor = Color.FromArgb(255, 160, 0);
            TextColor = Color.Black;
            Draggable = true;
            
            // 判断节点：1个输入端口，2个输出端口
            InputPorts.Add(new PointF(0, Height / 2)); // 左侧中点
            OutputPorts.Add(new PointF(Width, Height / 3)); // 右侧上（True）
            OutputPorts.Add(new PointF(Width, Height * 2 / 3)); // 右侧下（False）
        }

        public DecisionNode(FlowNodeData data) : base(data)
        {
            Width = 120f;
            Height = 80f;
            BackgroundColor = Color.FromArgb(255, 193, 7);
            BorderColor = Color.FromArgb(255, 160, 0);
            TextColor = Color.Black;
            Draggable = true;
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            var bounds = GetBounds();
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;

            // 绘制菱形路径
            using (var path = CreateDiamondPath(bounds))
            {
                // 填充背景
                using (var brush = new SolidBrush(BackgroundColor))
                {
                    g.FillPath(brush, path);
                }

                // 绘制边框
                var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
                using (var pen = new Pen(borderColor, BorderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

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
        }

        /// <summary>
        /// 创建菱形路径
        /// </summary>
        private GraphicsPath CreateDiamondPath(RectangleF rect)
        {
            var path = new GraphicsPath();
            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;
            var halfWidth = rect.Width / 2 - BorderWidth;
            var halfHeight = rect.Height / 2 - BorderWidth;

            path.AddPolygon(new[]
            {
                new PointF(centerX, rect.Y + BorderWidth), // 上
                new PointF(rect.Right - BorderWidth, centerY), // 右
                new PointF(centerX, rect.Bottom - BorderWidth), // 下
                new PointF(rect.X + BorderWidth, centerY) // 左
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
