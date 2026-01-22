using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 结束节点 - 圆形红色节点，只有输入端口
    /// 标准：60x60px圆形，红色背景，停止图标(■)
    /// </summary>
    public class EndNode : FlowNode
    {
        public EndNode() : base()
        {
            Width = Models.NodeSizeConstants.StartEndSize;
            Height = Models.NodeSizeConstants.StartEndSize;
            BackgroundColor = Models.NodeColorConstants.EndRed;
            BorderColor = Models.NodeColorConstants.EndRedBorder;
            TextColor = Color.White;
            Draggable = true;
            
            // 只有输入端口，没有输出端口
            OutputPorts.Clear();
        }

        public EndNode(FlowNodeData data) : base(data)
        {
            Width = Models.NodeSizeConstants.StartEndSize;
            Height = Models.NodeSizeConstants.StartEndSize;
            BackgroundColor = Models.NodeColorConstants.EndRed;
            BorderColor = Models.NodeColorConstants.EndRedBorder;
            TextColor = Color.White;
            Draggable = true;
            
            // 只有输入端口，没有输出端口
            OutputPorts.Clear();
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            var centerX = X + Width / 2;
            var centerY = Y + Height / 2;

            // 绘制圆形背景
            using (var brush = new SolidBrush(BackgroundColor))
            {
                g.FillEllipse(brush, X + BorderWidth, Y + BorderWidth, Width - BorderWidth * 2, Height - BorderWidth * 2);
            }

            // 绘制边框
            var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
            var borderWidth = IsSelected ? SelectedBorderWidth : BorderWidth;
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.DrawEllipse(pen, X + borderWidth / 2, Y + borderWidth / 2, Width - borderWidth, Height - borderWidth);
            }

            // 绘制停止图标(■)
            DrawStopIcon(g, centerX, centerY);

            // 绘制输入端口
            DrawPorts(g);
        }

        /// <summary>
        /// 绘制停止图标(■)
        /// </summary>
        private void DrawStopIcon(Graphics g, float centerX, float centerY)
        {
            var iconSize = 14f;
            var rect = new RectangleF(
                centerX - iconSize / 2,
                centerY - iconSize / 2,
                iconSize,
                iconSize
            );

            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, rect);
            }
        }

        /// <summary>
        /// 重写端口绘制，只绘制输入端口
        /// </summary>
        protected override void DrawPorts(Graphics g)
        {
            var bounds = GetBounds();
            
            // 只有输入端口（左侧中点）
            if (InputPorts.Count == 0)
            {
                InputPorts.Add(new PointF(bounds.Left, bounds.Y + bounds.Height / 2));
            }
            else
            {
                InputPorts[0] = new PointF(bounds.Left, bounds.Y + bounds.Height / 2);
            }

            // 绘制输入端口
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var portState = GetPortState(i, true);
                DrawPort(g, InputPorts[i], true, portState);
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
