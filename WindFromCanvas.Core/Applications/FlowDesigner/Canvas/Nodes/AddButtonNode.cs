using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 添加按钮节点（匹配 Activepieces AddButton）
    /// </summary>
    public class AddButtonNode : BaseCanvasNode
    {
        public override SizeF Size => LayoutConstants.NodeSize.ADD_BUTTON;
        public string EdgeId { get; set; }
        public string ParentStepName { get; set; }
        public Core.Enums.StepLocationRelativeToParent StepLocationRelativeToParent { get; set; }
        public int? BranchIndex { get; set; }

        public AddButtonNode(string edgeId) : base(edgeId)
        {
            EdgeId = edgeId;
            Selectable = false;
            Draggable = false;
        }

        public override void Draw(Graphics g, float zoom)
        {
            var bounds = Bounds;
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;
            var radius = bounds.Width / 2;

            // 绘制圆形背景
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1f / zoom))
            {
                g.DrawEllipse(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            // 绘制加号
            var lineLength = bounds.Width * 0.4f;
            var lineWidth = 2f / zoom;
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
