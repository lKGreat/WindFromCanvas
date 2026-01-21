using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Connections
{
    /// <summary>
    /// 连接线路由器（智能路由和避让算法）
    /// </summary>
    public static class ConnectionRouter
    {
        /// <summary>
        /// 路由类型
        /// </summary>
        public enum RoutingType
        {
            Bezier,        // 贝塞尔曲线路由（默认）
            Orthogonal,    // 正交路由（直角折线）
            Avoidance      // 避让路由（自动绕行）
        }

        /// <summary>
        /// 路由路径点集合
        /// </summary>
        public class RoutingPath
        {
            public List<PointF> Points { get; set; } = new List<PointF>();
            public List<PointF> ControlPoints { get; set; } = new List<PointF>(); // 贝塞尔控制点
            public RoutingType Type { get; set; }
        }

        /// <summary>
        /// 计算连接路径
        /// </summary>
        public static RoutingPath CalculatePath(FlowNode sourceNode, FlowNode targetNode, List<FlowNode> obstacles = null)
        {
            if (sourceNode == null || targetNode == null)
                return null;

            var sourcePoint = GetConnectionPoint(sourceNode, true);
            var targetPoint = GetConnectionPoint(targetNode, false);

            if (sourcePoint.IsEmpty || targetPoint.IsEmpty)
                return null;

            obstacles = obstacles ?? new List<FlowNode>();

            // 判断路由类型
            if (IsVerticalLayout(sourcePoint, targetPoint))
            {
                return CreateOrthogonalPath(sourcePoint, targetPoint);
            }
            else if (HasObstacles(sourcePoint, targetPoint, obstacles))
            {
                return CreateAvoidancePath(sourcePoint, targetPoint, obstacles);
            }
            else
            {
                return CreateBezierPath(sourcePoint, targetPoint);
            }
        }

        /// <summary>
        /// 判断是否为垂直布局（节点垂直排列）
        /// </summary>
        private static bool IsVerticalLayout(PointF start, PointF end)
        {
            var horizontalDistance = Math.Abs(end.X - start.X);
            var verticalDistance = Math.Abs(end.Y - start.Y);
            
            // 如果水平距离小于垂直距离的30%，认为是垂直布局
            return horizontalDistance < verticalDistance * 0.3f;
        }

        /// <summary>
        /// 检查路径上是否有障碍节点
        /// </summary>
        private static bool HasObstacles(PointF start, PointF end, List<FlowNode> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
                return false;

            // 创建简单的矩形区域检查
            var minX = Math.Min(start.X, end.X);
            var maxX = Math.Max(start.X, end.X);
            var minY = Math.Min(start.Y, end.Y);
            var maxY = Math.Max(start.Y, end.Y);

            var pathRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);

            foreach (var obstacle in obstacles)
            {
                var bounds = obstacle.GetBounds();
                if (pathRect.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 创建贝塞尔曲线路径
        /// </summary>
        private static RoutingPath CreateBezierPath(PointF start, PointF end)
        {
            var path = new RoutingPath { Type = RoutingType.Bezier };
            
            var dx = Math.Abs(end.X - start.X);
            var controlOffset = Math.Max(50f, dx * 0.4f);
            
            var cp1 = new PointF(start.X + controlOffset, start.Y);
            var cp2 = new PointF(end.X - controlOffset, end.Y);
            
            path.Points.Add(start);
            path.Points.Add(end);
            path.ControlPoints.Add(cp1);
            path.ControlPoints.Add(cp2);
            
            return path;
        }

        /// <summary>
        /// 创建正交路径（直角折线+圆角）
        /// </summary>
        private static RoutingPath CreateOrthogonalPath(PointF start, PointF end)
        {
            var path = new RoutingPath { Type = RoutingType.Orthogonal };
            const float arcLength = 15f;
            
            path.Points.Add(start);
            
            // 计算中间点
            var midX = (start.X + end.X) / 2;
            var midY = (start.Y + end.Y) / 2;
            
            // 如果垂直距离较大，使用三折线
            if (Math.Abs(end.Y - start.Y) > 100f)
            {
                // 从起点垂直向下/上
                path.Points.Add(new PointF(start.X, midY - arcLength));
                
                // 水平到中点
                path.Points.Add(new PointF(midX, midY - arcLength));
                path.Points.Add(new PointF(midX, midY + arcLength));
                
                // 垂直到终点
                path.Points.Add(new PointF(end.X, midY + arcLength));
            }
            else
            {
                // 简单的两折线
                path.Points.Add(new PointF(start.X, midY));
                path.Points.Add(new PointF(end.X, midY));
            }
            
            path.Points.Add(end);
            
            return path;
        }

        /// <summary>
        /// 创建避让路径（自动绕行障碍）
        /// </summary>
        private static RoutingPath CreateAvoidancePath(PointF start, PointF end, List<FlowNode> obstacles)
        {
            var path = new RoutingPath { Type = RoutingType.Avoidance };
            
            // 简化实现：向上绕行
            var minY = Math.Min(start.Y, end.Y);
            var maxY = Math.Max(start.Y, end.Y);
            
            // 找到最高的障碍节点
            float highestObstacleTop = float.MaxValue;
            foreach (var obstacle in obstacles)
            {
                var bounds = obstacle.GetBounds();
                if (bounds.Top < highestObstacleTop)
                {
                    highestObstacleTop = bounds.Top;
                }
            }
            
            // 计算避让高度
            var avoidanceHeight = Math.Max(50f, minY - highestObstacleTop + 30f);
            var topY = minY - avoidanceHeight;
            
            // 创建绕行路径
            path.Points.Add(start);
            path.Points.Add(new PointF(start.X, topY)); // 向上
            path.Points.Add(new PointF(end.X, topY));   // 水平
            path.Points.Add(end);                        // 向下到终点
            
            return path;
        }

        /// <summary>
        /// 获取连接点（节点边缘的中点）
        /// </summary>
        private static PointF GetConnectionPoint(FlowNode node, bool isSource)
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
        /// 绘制路由路径
        /// </summary>
        public static void DrawPath(Graphics g, RoutingPath path, Pen pen)
        {
            if (path == null || path.Points.Count < 2) return;

            switch (path.Type)
            {
                case RoutingType.Bezier:
                    if (path.ControlPoints.Count >= 2)
                    {
                        using (var graphicsPath = new GraphicsPath())
                        {
                            graphicsPath.AddBezier(
                                path.Points[0],
                                path.ControlPoints[0],
                                path.ControlPoints[1],
                                path.Points[1]
                            );
                            g.DrawPath(pen, graphicsPath);
                        }
                    }
                    break;

                case RoutingType.Orthogonal:
                case RoutingType.Avoidance:
                    // 绘制折线（带圆角）
                    const float arcLength = 15f;
                    using (var graphicsPath = new GraphicsPath())
                    {
                        for (int i = 0; i < path.Points.Count - 1; i++)
                        {
                            var p1 = path.Points[i];
                            var p2 = path.Points[i + 1];
                            
                            if (i == 0)
                            {
                                graphicsPath.AddLine(p1, p2);
                            }
                            else
                            {
                                var prev = path.Points[i - 1];
                                // 添加圆角转角
                                if (Math.Abs(p1.X - prev.X) < 1) // 垂直转水平
                                {
                                    graphicsPath.AddLine(prev, new PointF(p1.X, p1.Y - arcLength));
                                    graphicsPath.AddArc(p1.X - arcLength, p1.Y - arcLength * 2, 
                                        arcLength * 2, arcLength * 2, 90, 90);
                                    graphicsPath.AddLine(new PointF(p1.X + arcLength, p1.Y), p2);
                                }
                                else // 水平转垂直
                                {
                                    graphicsPath.AddLine(prev, new PointF(p1.X - arcLength, p1.Y));
                                    graphicsPath.AddArc(p1.X - arcLength * 2, p1.Y - arcLength, 
                                        arcLength * 2, arcLength * 2, 0, 90);
                                    graphicsPath.AddLine(new PointF(p1.X, p1.Y + arcLength), p2);
                                }
                            }
                        }
                        g.DrawPath(pen, graphicsPath);
                    }
                    break;
            }
        }
    }
}
