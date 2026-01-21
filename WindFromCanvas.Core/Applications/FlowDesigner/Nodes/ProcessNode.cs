using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 处理节点 - 矩形蓝色节点，输入和输出端口
    /// </summary>
    public class ProcessNode : FlowNode
    {
        public ProcessNode() : base()
        {
            Width = 150f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(33, 150, 243); // 蓝色
            BorderColor = Color.FromArgb(25, 118, 210);
            TextColor = Color.White;
            Draggable = true;
        }

        public ProcessNode(FlowNodeData data) : base(data)
        {
            Width = 150f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(33, 150, 243);
            BorderColor = Color.FromArgb(25, 118, 210);
            TextColor = Color.White;
            Draggable = true;
        }

        protected override void DrawIcon(Graphics g, RectangleF rect)
        {
            // 处理节点图标：绘制一个齿轮或处理图标
            var iconSize = 24f;
            var iconRect = new RectangleF(
                rect.X + 10,
                rect.Y + (rect.Height - iconSize) / 2,
                iconSize,
                iconSize
            );

            // 绘制一个简单的矩形图标
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, iconRect);
            }
            using (var pen = new Pen(Color.FromArgb(33, 150, 243), 2))
            {
                g.DrawRectangle(pen, iconRect.X, iconRect.Y, iconRect.Width, iconRect.Height);
            }
        }
    }
}
