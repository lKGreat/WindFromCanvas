using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Objects;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Connections
{
    /// <summary>
    /// 路由器分支连接（多分支布局，参考Activepieces实现）
    /// </summary>
    public class RouterConnection : FlowConnection
    {
        /// <summary>
        /// 分支索引
        /// </summary>
        public int BranchIndex { get; set; }

        /// <summary>
        /// 分支标签（True/False等）
        /// </summary>
        public string BranchLabel { get; set; }

        public RouterConnection(FlowConnectionData data, FlowNode sourceNode, FlowNode targetNode, int branchIndex, string branchLabel) 
            : base(data, sourceNode, targetNode)
        {
            BranchIndex = branchIndex;
            BranchLabel = branchLabel;
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || SourceNode == null || TargetNode == null) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty) return;

            var color = IsSelected ? SelectedLineColor : 
                       (IsHovered ? HoverLineColor : LineColor);

            // 路由器连接：从节点右侧分支点出发，水平扩展后垂直向下
            using (var pen = new Pen(color, LineWidth))
            {
                pen.EndCap = LineCap.Round;
                pen.StartCap = LineCap.Round;

                var horizontalOffset = 50f; // 水平扩展距离
                var midY = (startPoint.Y + endPoint.Y) / 2;

                using (var path = new GraphicsPath())
                {
                    // 从起点向右
                    path.AddLine(startPoint, new PointF(startPoint.X + horizontalOffset, startPoint.Y));
                    
                    // 垂直向下到中点
                    path.AddLine(new PointF(startPoint.X + horizontalOffset, startPoint.Y),
                        new PointF(startPoint.X + horizontalOffset, midY));
                    
                    // 水平向右到目标
                    path.AddLine(new PointF(startPoint.X + horizontalOffset, midY),
                        new PointF(endPoint.X - horizontalOffset, midY));
                    
                    // 垂直向下到终点
                    path.AddLine(new PointF(endPoint.X - horizontalOffset, midY),
                        new PointF(endPoint.X - horizontalOffset, endPoint.Y));
                    
                    // 到终点
                    path.AddLine(new PointF(endPoint.X - horizontalOffset, endPoint.Y), endPoint);

                    g.DrawPath(pen, path);
                }
            }

            // 绘制分支标签
            if (!string.IsNullOrEmpty(BranchLabel))
            {
                var labelPoint = new PointF(
                    startPoint.X + 25,
                    startPoint.Y - 15
                );
                using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString(BranchLabel, SystemFonts.DefaultFont, brush, labelPoint);
                }
            }

            // 绘制箭头
            if (!IsDashed)
            {
                DrawArrow(g, startPoint, endPoint, color);
            }
        }
    }
}
