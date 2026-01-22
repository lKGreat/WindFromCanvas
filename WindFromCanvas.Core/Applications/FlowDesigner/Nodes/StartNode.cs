using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 开始节点 - 圆形绿色节点，只有输出端口
    /// 标准：60x60px圆形，绿色背景，播放图标(▶)
    /// </summary>
    public class StartNode : FlowNode
    {
        public StartNode() : base()
        {
            Width = Models.NodeSizeConstants.StartEndSize;
            Height = Models.NodeSizeConstants.StartEndSize;
            BackgroundColor = Models.NodeColorConstants.StartGreen;
            BorderColor = Models.NodeColorConstants.StartGreenBorder;
            TextColor = Color.White;
            Draggable = false; // 固定位置，不可删除
            
            // 只有输出端口，没有输入端口
            InputPorts.Clear();
        }

        public StartNode(FlowNodeData data) : base(data)
        {
            Width = Models.NodeSizeConstants.StartEndSize;
            Height = Models.NodeSizeConstants.StartEndSize;
            BackgroundColor = Models.NodeColorConstants.StartGreen;
            BorderColor = Models.NodeColorConstants.StartGreenBorder;
            TextColor = Color.White;
            Draggable = false;
            
            // 只有输出端口，没有输入端口
            InputPorts.Clear();
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

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
            var borderWidth = IsSelected ? SelectedBorderWidth : BorderWidth;
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.DrawEllipse(pen, X + borderWidth / 2, Y + borderWidth / 2, Width - borderWidth, Height - borderWidth);
            }

            // 绘制播放图标(▶)
            DrawPlayIcon(g, centerX, centerY);

            // 绘制输出端口
            DrawPorts(g);
        }

        /// <summary>
        /// 绘制播放图标(▶)
        /// </summary>
        private void DrawPlayIcon(Graphics g, float centerX, float centerY)
        {
            var iconSize = 18f;
            var offsetX = 2f; // 向右偏移使三角形居中

            // 创建播放三角形路径
            using (var path = new GraphicsPath())
            {
                var points = new PointF[]
                {
                    new PointF(centerX - iconSize / 3 + offsetX, centerY - iconSize / 2),
                    new PointF(centerX - iconSize / 3 + offsetX, centerY + iconSize / 2),
                    new PointF(centerX + iconSize * 2 / 3 + offsetX, centerY)
                };
                path.AddPolygon(points);

                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        /// <summary>
        /// 重写端口绘制，只绘制输出端口
        /// </summary>
        protected override void DrawPorts(Graphics g)
        {
            var bounds = GetBounds();
            
            // 只有输出端口（右侧中点）
            if (OutputPorts.Count == 0)
            {
                OutputPorts.Add(new PointF(bounds.Right, bounds.Y + bounds.Height / 2));
            }
            else
            {
                OutputPorts[0] = new PointF(bounds.Right, bounds.Y + bounds.Height / 2);
            }

            // 绘制输出端口
            for (int i = 0; i < OutputPorts.Count; i++)
            {
                var portState = GetPortState(i, false);
                DrawPort(g, OutputPorts[i], false, portState);
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
