using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Events;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Connections
{
    /// <summary>
    /// 流程连接线
    /// </summary>
    public class FlowConnection : CanvasObject
    {
        /// <summary>
        /// 连接数据
        /// </summary>
        public FlowConnectionData Data { get; set; }

        /// <summary>
        /// 源节点
        /// </summary>
        public FlowNode SourceNode { get; set; }

        /// <summary>
        /// 目标节点
        /// </summary>
        public FlowNode TargetNode { get; set; }

        /// <summary>
        /// 是否为循环返回连接
        /// </summary>
        public bool IsLoopReturn { get; set; }

        /// <summary>
        /// 连接线颜色
        /// </summary>
        public Color LineColor { get; set; } = Color.FromArgb(150, 150, 150);

        /// <summary>
        /// 连接线宽度
        /// </summary>
        public float LineWidth { get; set; } = 2f;

        /// <summary>
        /// 箭头大小
        /// </summary>
        public float ArrowSize { get; set; } = 8f;

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否悬停
        /// </summary>
        public bool IsHovered { get; set; }

        public FlowConnection()
        {
            Draggable = false;
            ZIndex = 1; // 连接线在节点下方
        }

        public FlowConnection(FlowConnectionData data, FlowNode sourceNode, FlowNode targetNode) : this()
        {
            Data = data;
            SourceNode = sourceNode;
            TargetNode = targetNode;
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || SourceNode == null || TargetNode == null) return;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty) return;

            // 绘制连接线
            var color = IsSelected ? Color.FromArgb(0, 120, 215) : 
                       (IsHovered ? Color.FromArgb(100, 100, 100) : LineColor);
            
            if (IsLoopReturn)
            {
                // 循环返回连接：绘制曲线
                DrawLoopReturnLine(g, startPoint, endPoint, color);
            }
            else
            {
                // 普通连接：绘制直线
                using (var pen = new Pen(color, LineWidth))
                {
                    pen.EndCap = LineCap.Round;
                    pen.StartCap = LineCap.Round;
                    g.DrawLine(pen, startPoint, endPoint);
                }
            }

            // 绘制箭头
            DrawArrow(g, startPoint, endPoint, color);
        }

        /// <summary>
        /// 绘制循环返回线（曲线）
        /// </summary>
        private void DrawLoopReturnLine(Graphics g, PointF start, PointF end, Color color)
        {
            using (var pen = new Pen(color, LineWidth))
            {
                pen.EndCap = LineCap.Round;
                pen.StartCap = LineCap.Round;

                // 计算控制点，创建曲线
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var controlOffset = Math.Max(50, Math.Abs(dy) * 0.5f);

                var controlPoint1 = new PointF(start.X, start.Y + controlOffset);
                var controlPoint2 = new PointF(end.X, end.Y - controlOffset);

                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddBezier(start, controlPoint1, controlPoint2, end);
                    g.DrawPath(pen, path);
                }
            }
        }

        /// <summary>
        /// 获取连接点（节点边缘的中点）
        /// </summary>
        private PointF GetConnectionPoint(FlowNode node, bool isSource)
        {
            if (node == null) return PointF.Empty;

            var bounds = node.GetBounds();
            if (isSource)
            {
                // 源节点：右边缘中点
                return new PointF(bounds.Right, bounds.Y + bounds.Height / 2);
            }
            else
            {
                // 目标节点：左边缘中点
                return new PointF(bounds.Left, bounds.Y + bounds.Height / 2);
            }
        }

        /// <summary>
        /// 绘制箭头
        /// </summary>
        private void DrawArrow(Graphics g, PointF start, PointF end, Color color)
        {
            // 计算箭头方向
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = (float)Math.Sqrt(dx * dx + dy * dy);

            if (length < ArrowSize) return;

            // 归一化方向向量
            var nx = dx / length;
            var ny = dy / length;

            // 箭头尖端位置（稍微向内，避免重叠）
            var arrowTip = new PointF(
                end.X - nx * (TargetNode.Width / 2 + 5),
                end.Y - ny * (TargetNode.Height / 2 + 5)
            );

            // 箭头两边的点
            var arrowLeft = new PointF(
                arrowTip.X - ArrowSize * nx - ArrowSize * ny,
                arrowTip.Y - ArrowSize * ny + ArrowSize * nx
            );
            var arrowRight = new PointF(
                arrowTip.X - ArrowSize * nx + ArrowSize * ny,
                arrowTip.Y - ArrowSize * ny - ArrowSize * nx
            );

            // 绘制箭头
            using (var brush = new SolidBrush(color))
            {
                var points = new[] { arrowTip, arrowLeft, arrowRight };
                g.FillPolygon(brush, points);
            }
        }

        public override bool HitTest(PointF point)
        {
            if (SourceNode == null || TargetNode == null) return false;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty) return false;

            // 检查点是否在连接线附近（容差范围内）
            const float tolerance = 5f;
            return IsPointNearLine(point, startPoint, endPoint, tolerance);
        }

        /// <summary>
        /// 检查点是否在直线附近
        /// </summary>
        private bool IsPointNearLine(PointF point, PointF lineStart, PointF lineEnd, float tolerance)
        {
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var lengthSquared = dx * dx + dy * dy;

            if (lengthSquared < 0.01f) return false;

            // 计算点到直线的距离
            var t = Math.Max(0, Math.Min(1, 
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared));
            
            var projection = new PointF(
                lineStart.X + t * dx,
                lineStart.Y + t * dy
            );

            var distanceSquared = (point.X - projection.X) * (point.X - projection.X) +
                                 (point.Y - projection.Y) * (point.Y - projection.Y);

            return distanceSquared <= tolerance * tolerance;
        }

        public override RectangleF GetBounds()
        {
            if (SourceNode == null || TargetNode == null)
                return RectangleF.Empty;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty)
                return RectangleF.Empty;

            var minX = Math.Min(startPoint.X, endPoint.X);
            var minY = Math.Min(startPoint.Y, endPoint.Y);
            var maxX = Math.Max(startPoint.X, endPoint.X);
            var maxY = Math.Max(startPoint.Y, endPoint.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 更新连接（当节点移动时调用）
        /// </summary>
        public void Update()
        {
            // 连接线位置由源节点和目标节点决定，不需要单独的位置属性
            // 但可以触发重绘
        }
    }
}
