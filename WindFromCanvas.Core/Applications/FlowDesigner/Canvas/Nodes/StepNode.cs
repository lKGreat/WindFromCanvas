using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 步骤节点（匹配 Activepieces StepNode）
    /// </summary>
    public class StepNode : BaseCanvasNode
    {
        private readonly IStep _step;
        public override SizeF Size => LayoutConstants.NodeSize.STEP;

        public IStep Step => _step;

        public StepNode(IStep step) : base(step.Name)
        {
            _step = step;
            Draggable = !(_step is FlowTrigger);
        }

        public override void Draw(Graphics g, float zoom)
        {
            var bounds = Bounds;
            var isTrigger = _step is FlowTrigger;
            var isSkipped = _step.Skip;
            var isValid = _step.Valid;

            // 绘制节点背景
            using (var brush = new SolidBrush(isSkipped 
                ? Color.FromArgb(240, 240, 240) 
                : Color.White))
            {
                var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                
                // 触发器节点左上角不圆角
                if (isTrigger)
                {
                    g.FillRectangle(brush, rect);
                }
                else
                {
                    var path = CreateRoundedRectangle(rect, 4f / zoom);
                    g.FillPath(brush, path);
                }
            }

            // 绘制边框
            var borderColor = IsSelected 
                ? Color.FromArgb(255, 59, 130, 246)
                : Color.FromArgb(200, 200, 200);
            
            using (var pen = new Pen(borderColor, 1f / zoom))
            {
                var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                if (isTrigger)
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    var path = CreateRoundedRectangle(rect, 4f / zoom);
                    g.DrawPath(pen, path);
                }
            }

            // 绘制无效/跳过图标
            if (!isValid || isSkipped)
            {
                DrawStatusIcon(g, bounds, isValid, isSkipped, zoom);
            }

            // 绘制节点内容（图标、名称等）
            DrawNodeContent(g, bounds, zoom);

            // 绘制选中边框
            DrawSelectionBorder(g, bounds, zoom);
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// 绘制状态图标
        /// </summary>
        private void DrawStatusIcon(Graphics g, RectangleF bounds, bool isValid, bool isSkipped, float zoom)
        {
            var iconSize = 16f / zoom;
            var iconX = bounds.Right - iconSize - 4f / zoom;
            var iconY = bounds.Top + 4f / zoom;

            if (!isValid)
            {
                // 绘制错误图标（红色X）
                using (var pen = new Pen(Color.Red, 2f / zoom))
                {
                    g.DrawLine(pen, iconX, iconY, iconX + iconSize, iconY + iconSize);
                    g.DrawLine(pen, iconX + iconSize, iconY, iconX, iconY + iconSize);
                }
            }
            else if (isSkipped)
            {
                // 绘制跳过图标（灰色斜线）
                using (var pen = new Pen(Color.Gray, 2f / zoom))
                {
                    g.DrawLine(pen, iconX, iconY + iconSize / 2, iconX + iconSize, iconY + iconSize / 2);
                }
            }
        }

        /// <summary>
        /// 绘制节点内容
        /// </summary>
        private void DrawNodeContent(Graphics g, RectangleF bounds, float zoom)
        {
            var padding = 12f / zoom;
            var contentX = bounds.X + padding;
            var contentY = bounds.Y + padding;
            var contentWidth = bounds.Width - padding * 2;
            var contentHeight = bounds.Height - padding * 2;

            // 绘制图标（占位）
            var iconSize = 24f / zoom;
            var iconRect = new RectangleF(contentX, contentY, iconSize, iconSize);
            DrawIcon(g, iconRect, zoom);

            // 绘制文本
            var textX = contentX + iconSize + 8f / zoom;
            var textRect = new RectangleF(textX, contentY, contentWidth - iconSize - 8f / zoom, contentHeight);
            DrawText(g, textRect, _step.DisplayName, zoom);
        }

        /// <summary>
        /// 绘制图标（占位实现）
        /// </summary>
        private void DrawIcon(Graphics g, RectangleF rect, float zoom)
        {
            using (var brush = new SolidBrush(Color.FromArgb(100, 100, 100)))
            {
                g.FillEllipse(brush, rect);
            }
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        private void DrawText(Graphics g, RectangleF rect, string text, float zoom)
        {
            using (var font = new Font("Microsoft YaHei", 12f / zoom))
            using (var brush = new SolidBrush(Color.Black))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(text, font, brush, rect, format);
            }
        }
    }
}
