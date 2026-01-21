using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 开始节点 - 圆形绿色节点，只有输出端口
    /// </summary>
    public class StartNode : FlowNode
    {
        public StartNode() : base()
        {
            Width = 60f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(76, 175, 80); // 绿色
            BorderColor = Color.FromArgb(56, 142, 60);
            TextColor = Color.White;
            Draggable = false; // 固定位置，不可删除
        }

        public StartNode(FlowNodeData data) : base(data)
        {
            Width = 60f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(76, 175, 80);
            BorderColor = Color.FromArgb(56, 142, 60);
            TextColor = Color.White;
            Draggable = false;
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            var centerX = X + Width / 2;
            var centerY = Y + Height / 2;
            var radius = Width / 2 - BorderWidth;

            // 绘制圆形背景
            using (var brush = new SolidBrush(BackgroundColor))
            {
                g.FillEllipse(brush, X + BorderWidth, Y + BorderWidth, Width - BorderWidth * 2, Height - BorderWidth * 2);
            }

            // 绘制边框
            var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
            using (var pen = new Pen(borderColor, BorderWidth))
            {
                g.DrawEllipse(pen, X + BorderWidth, Y + BorderWidth, Width - BorderWidth * 2, Height - BorderWidth * 2);
            }

            // 绘制文本
            var text = Data?.DisplayName ?? "开始";
            using (var brush = new SolidBrush(TextColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(text, SystemFonts.DefaultFont, brush, 
                    new RectangleF(X, Y, Width, Height), sf);
            }
        }

        public override bool HitTest(PointF point)
        {
            var centerX = X + Width / 2;
            var centerY = Y + Height / 2;
            var radius = Width / 2;
            var dx = point.X - centerX;
            var dy = point.Y - centerY;
            return dx * dx + dy * dy <= radius * radius;
        }

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }
    }
}
