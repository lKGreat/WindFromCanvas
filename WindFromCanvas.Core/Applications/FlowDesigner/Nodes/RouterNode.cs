using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 路由器节点（多分支路由，参考Activepieces实现）
    /// </summary>
    public class RouterNode : FlowNode
    {
        /// <summary>
        /// 分支列表
        /// </summary>
        public System.Collections.Generic.List<RouterBranch> Branches { get; set; }

        public RouterNode() : base()
        {
            Width = 232f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(255, 255, 255);
            BorderColor = Color.FromArgb(226, 232, 240);
            TextColor = Color.FromArgb(15, 23, 42);
            Draggable = true;
            EnableShadow = true;
            Branches = new System.Collections.Generic.List<RouterBranch>();
        }

        public RouterNode(FlowNodeData data) : base(data)
        {
            Width = 232f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(255, 255, 255);
            BorderColor = Color.FromArgb(226, 232, 240);
            TextColor = Color.FromArgb(15, 23, 42);
            Draggable = true;
            EnableShadow = true;
            Branches = new System.Collections.Generic.List<RouterBranch>();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);

            // 绘制分支标签（如果有）
            if (Branches != null && Branches.Count > 0)
            {
                var bounds = GetBounds();
                var labelY = bounds.Bottom + 5;
                
                foreach (var branch in Branches)
                {
                    if (!string.IsNullOrEmpty(branch.Label))
                    {
                        using (var brush = new SolidBrush(TextColor))
                        using (var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Near
                        })
                        {
                            g.DrawString(branch.Label, SystemFonts.DefaultFont, brush, 
                                new RectangleF(bounds.X, labelY, bounds.Width, 20), sf);
                            labelY += 20;
                        }
                    }
                }
            }
        }

        protected override void DrawPorts(Graphics g)
        {
            var bounds = GetBounds();
            
            // 输入端口（左侧中点）
            if (InputPorts.Count == 0)
            {
                InputPorts.Add(new PointF(bounds.Left, bounds.Y + bounds.Height / 2));
            }
            
            // 输出端口（根据分支数量，从右侧扩展）
            OutputPorts.Clear();
            if (Branches != null && Branches.Count > 0)
            {
                var spacing = bounds.Height / (Branches.Count + 1);
                for (int i = 0; i < Branches.Count; i++)
                {
                    OutputPorts.Add(new PointF(bounds.Right, bounds.Y + spacing * (i + 1)));
                }
            }
            else
            {
                OutputPorts.Add(new PointF(bounds.Right, bounds.Y + bounds.Height / 2));
            }

            base.DrawPorts(g);
        }
    }

    /// <summary>
    /// 路由器分支
    /// </summary>
    public class RouterBranch
    {
        public string Label { get; set; }
        public string Condition { get; set; }
        public FlowNode TargetNode { get; set; }
    }
}
