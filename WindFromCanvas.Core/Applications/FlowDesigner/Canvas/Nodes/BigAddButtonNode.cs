using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 大添加按钮节点（用于空分支，匹配 Activepieces BigAddButton）
    /// </summary>
    public class BigAddButtonNode : BaseCanvasNode
    {
        public override SizeF Size => LayoutConstants.NodeSize.BIG_ADD_BUTTON;
        public string EdgeId { get; set; }
        public string ParentStepName { get; set; }
        public Core.Enums.StepLocationRelativeToParent StepLocationRelativeToParent { get; set; }
        public int? BranchIndex { get; set; }

        public BigAddButtonNode(string id) : base(id)
        {
            Selectable = false;
            Draggable = false;
        }

        public override void Draw(Graphics g, float zoom)
        {
            var bounds = Bounds;
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;

            // 绘制圆角矩形背景
            var radius = 4f / zoom;
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();

                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillPath(brush, path);
                }

                using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1f / zoom))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 绘制加号
            var lineLength = bounds.Width * 0.5f;
            var lineWidth = 3f / zoom;
            using (var pen = new Pen(Color.FromArgb(100, 100, 100), lineWidth))
            {
                // 横线
                g.DrawLine(pen,
                    centerX - lineLength / 2, centerY,
                    centerX + lineLength / 2, centerY);
                // 竖线
                g.DrawLine(pen,
                    centerX, centerY - lineLength / 2,
                    centerX, centerY + lineLength / 2);
            }
        }
    }
}
