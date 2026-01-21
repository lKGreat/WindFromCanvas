using System;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 循环节点 - 矩形紫色节点，循环体子节点容器，返回连接线
    /// </summary>
    public class LoopNode : FlowNode
    {
        /// <summary>
        /// 循环体子节点容器（后续实现）
        /// </summary>
        public System.Collections.Generic.List<FlowNode> ChildNodes { get; set; }

        public LoopNode() : base()
        {
            Width = 200f;
            Height = 100f;
            BackgroundColor = Color.FromArgb(156, 39, 176); // 紫色
            BorderColor = Color.FromArgb(123, 31, 162);
            TextColor = Color.White;
            Draggable = true;
            ChildNodes = new System.Collections.Generic.List<FlowNode>();
        }

        public LoopNode(FlowNodeData data) : base(data)
        {
            Width = 200f;
            Height = 100f;
            BackgroundColor = Color.FromArgb(156, 39, 176);
            BorderColor = Color.FromArgb(123, 31, 162);
            TextColor = Color.White;
            Draggable = true;
            ChildNodes = new System.Collections.Generic.List<FlowNode>();
        }

        protected override void DrawIcon(Graphics g, RectangleF rect)
        {
            // 循环节点图标：绘制一个循环箭头
            var iconSize = 24f;
            var iconRect = new RectangleF(
                rect.X + 10,
                rect.Y + (rect.Height - iconSize) / 2,
                iconSize,
                iconSize
            );

            // 绘制一个简单的循环图标（圆形箭头）
            using (var pen = new Pen(Color.White, 2))
            {
                // 绘制圆弧表示循环
                g.DrawArc(pen, iconRect.X, iconRect.Y, iconSize, iconSize, 0, 270);
                
                // 绘制箭头
                var arrowX = iconRect.X + iconSize;
                var arrowY = iconRect.Y + iconSize / 2;
                g.DrawLine(pen, arrowX - 5, arrowY - 3, arrowX, arrowY);
                g.DrawLine(pen, arrowX - 5, arrowY + 3, arrowX, arrowY);
            }
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);

            // 绘制循环边框（特殊样式，表示包含子节点）
            if (ChildNodes != null && ChildNodes.Count > 0)
            {
                // 计算子节点的边界
                var childBounds = CalculateChildBounds();
                if (!childBounds.IsEmpty)
                {
                    var expandedBounds = new RectangleF(
                        childBounds.X - 20,
                        childBounds.Y - 20,
                        childBounds.Width + 40,
                        childBounds.Height + 40
                    );

                    // 绘制循环容器边框
                    using (var pen = new Pen(BorderColor, BorderWidth * 1.5f))
                    {
                        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        g.DrawRectangle(pen, expandedBounds.X, expandedBounds.Y, 
                            expandedBounds.Width, expandedBounds.Height);
                    }

                    // 绘制循环标签
                    using (var brush = new SolidBrush(BorderColor))
                    using (var font = new Font(SystemFonts.DefaultFont.FontFamily, 9, FontStyle.Bold))
                    {
                        var labelText = "循环体";
                        var labelSize = g.MeasureString(labelText, font);
                        g.DrawString(labelText, font, brush, 
                            expandedBounds.X + 5, expandedBounds.Y - labelSize.Height - 2);
                    }
                }
            }
        }

        /// <summary>
        /// 计算子节点的边界
        /// </summary>
        private RectangleF CalculateChildBounds()
        {
            if (ChildNodes == null || ChildNodes.Count == 0)
                return RectangleF.Empty;

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var child in ChildNodes)
            {
                var bounds = child.GetBounds();
                minX = Math.Min(minX, bounds.Left);
                minY = Math.Min(minY, bounds.Top);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChildNode(FlowNode childNode)
        {
            if (childNode != null && !ChildNodes.Contains(childNode))
            {
                ChildNodes.Add(childNode);
                // 子节点应该在循环节点内部
                childNode.ZIndex = this.ZIndex + 1;
            }
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        public void RemoveChildNode(FlowNode childNode)
        {
            if (childNode != null)
            {
                ChildNodes.Remove(childNode);
            }
        }
    }
}
