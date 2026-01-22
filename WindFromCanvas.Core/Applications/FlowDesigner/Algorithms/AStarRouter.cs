using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Algorithms
{
    /// <summary>
    /// A*路径路由算法（用于直角折线连线的智能避障）
    /// </summary>
    public class AStarRouter
    {
        private const float GridSize = 10f; // 网格大小
        private const float Margin = 50f; // 边界边距

        /// <summary>
        /// 路径节点
        /// </summary>
        private class PathNode
        {
            public PointF Position { get; set; }
            public float GCost { get; set; } // 从起点到当前节点的实际代价
            public float HCost { get; set; } // 从当前节点到终点的启发代价（曼哈顿距离）
            public float FCost => GCost + HCost; // 总代价
            public PathNode Parent { get; set; }

            public PathNode(PointF position)
            {
                Position = position;
            }
        }

        /// <summary>
        /// 查找路径
        /// </summary>
        public List<PointF> FindPath(PointF start, PointF end, List<RectangleF> obstacles)
        {
            if (obstacles == null)
            {
                obstacles = new List<RectangleF>();
            }

            // 1. 建立搜索边界框
            var bounds = CreateSearchBounds(start, end, Margin);

            // 2. 生成候选路径点（网格化）
            var waypoints = GenerateWaypoints(bounds, obstacles, start, end);

            // 3. A*搜索
            return SearchPath(start, end, waypoints, obstacles);
        }

        /// <summary>
        /// 创建搜索边界框
        /// </summary>
        private RectangleF CreateSearchBounds(PointF start, PointF end, float margin)
        {
            var minX = Math.Min(start.X, end.X) - margin;
            var minY = Math.Min(start.Y, end.Y) - margin;
            var maxX = Math.Max(start.X, end.X) + margin;
            var maxY = Math.Max(start.Y, end.Y) + margin;

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 生成候选路径点（网格化）
        /// </summary>
        private List<PointF> GenerateWaypoints(RectangleF bounds, List<RectangleF> obstacles, PointF start, PointF end)
        {
            var waypoints = new List<PointF> { start, end };

            // 在边界框内生成网格点
            for (float x = bounds.Left; x <= bounds.Right; x += GridSize)
            {
                for (float y = bounds.Top; y <= bounds.Bottom; y += GridSize)
                {
                    var point = new PointF(x, y);

                    // 跳过起点和终点
                    if (Distance(point, start) < GridSize || Distance(point, end) < GridSize)
                    {
                        continue;
                    }

                    // 检查是否在障碍物内
                    bool inObstacle = obstacles.Any(obs => obs.Contains(point));
                    if (!inObstacle)
                    {
                        waypoints.Add(point);
                    }
                }
            }

            return waypoints;
        }

        /// <summary>
        /// A*搜索路径
        /// </summary>
        private List<PointF> SearchPath(PointF start, PointF end, List<PointF> waypoints, List<RectangleF> obstacles)
        {
            if (waypoints.Count == 0)
            {
                return new List<PointF> { start, end };
            }

            var openSet = new List<PathNode>();
            var closedSet = new HashSet<PointF>();
            var nodeMap = new Dictionary<PointF, PathNode>();

            // 创建起点节点
            var startNode = new PathNode(start)
            {
                GCost = 0,
                HCost = ManhattanHeuristic(start, end)
            };
            openSet.Add(startNode);
            nodeMap[start] = startNode;

            // 为所有路径点创建节点
            foreach (var waypoint in waypoints)
            {
                if (!nodeMap.ContainsKey(waypoint))
                {
                    nodeMap[waypoint] = new PathNode(waypoint)
                    {
                        GCost = float.MaxValue,
                        HCost = ManhattanHeuristic(waypoint, end)
                    };
                }
            }

            while (openSet.Count > 0)
            {
                // 找到F代价最小的节点
                var currentNode = openSet.OrderBy(n => n.FCost).First();
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Position);

                // 如果到达终点
                if (Distance(currentNode.Position, end) < GridSize)
                {
                    return ReconstructPath(currentNode, end);
                }

                // 检查所有相邻节点
                foreach (var waypoint in waypoints)
                {
                    if (closedSet.Contains(waypoint))
                    {
                        continue;
                    }

                    // 检查是否可以直接到达（无障碍物阻挡）
                    if (!CanReach(currentNode.Position, waypoint, obstacles))
                    {
                        continue;
                    }

                    var neighbor = nodeMap[waypoint];
                    var tentativeGCost = currentNode.GCost + Distance(currentNode.Position, waypoint);

                    if (tentativeGCost < neighbor.GCost)
                    {
                        neighbor.Parent = currentNode;
                        neighbor.GCost = tentativeGCost;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            // 如果找不到路径，返回直线路径
            return new List<PointF> { start, end };
        }

        /// <summary>
        /// 检查两点之间是否可以到达（无障碍物阻挡）
        /// </summary>
        private bool CanReach(PointF from, PointF to, List<RectangleF> obstacles)
        {
            // 检查水平线段
            if (Math.Abs(from.Y - to.Y) < 0.1f)
            {
                var minX = Math.Min(from.X, to.X);
                var maxX = Math.Max(from.X, to.X);
                foreach (var obs in obstacles)
                {
                    if (obs.Top <= from.Y && obs.Bottom >= from.Y &&
                        obs.Left < maxX && obs.Right > minX)
                    {
                        return false;
                    }
                }
                return true;
            }

            // 检查垂直线段
            if (Math.Abs(from.X - to.X) < 0.1f)
            {
                var minY = Math.Min(from.Y, to.Y);
                var maxY = Math.Max(from.Y, to.Y);
                foreach (var obs in obstacles)
                {
                    if (obs.Left <= from.X && obs.Right >= from.X &&
                        obs.Top < maxY && obs.Bottom > minY)
                    {
                        return false;
                    }
                }
                return true;
            }

            // 对于非水平/垂直的线段，使用简化的检查
            return true;
        }

        /// <summary>
        /// 重构路径
        /// </summary>
        private List<PointF> ReconstructPath(PathNode node, PointF end)
        {
            var path = new List<PointF> { end };
            var current = node;

            while (current != null)
            {
                path.Insert(0, current.Position);
                current = current.Parent;
            }

            // 优化路径：移除共线的中间点
            return OptimizePath(path);
        }

        /// <summary>
        /// 优化路径：移除共线的中间点
        /// </summary>
        private List<PointF> OptimizePath(List<PointF> path)
        {
            if (path.Count <= 2)
            {
                return path;
            }

            var optimized = new List<PointF> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = optimized[optimized.Count - 1];
                var curr = path[i];
                var next = path[i + 1];

                // 检查三点是否共线
                if (!IsCollinear(prev, curr, next))
                {
                    optimized.Add(curr);
                }
            }

            optimized.Add(path[path.Count - 1]);
            return optimized;
        }

        /// <summary>
        /// 检查三点是否共线
        /// </summary>
        private bool IsCollinear(PointF p1, PointF p2, PointF p3)
        {
            // 检查是否在同一水平线
            if (Math.Abs(p1.Y - p2.Y) < 0.1f && Math.Abs(p2.Y - p3.Y) < 0.1f)
            {
                return true;
            }

            // 检查是否在同一垂直线
            if (Math.Abs(p1.X - p2.X) < 0.1f && Math.Abs(p2.X - p3.X) < 0.1f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 曼哈顿距离启发函数
        /// </summary>
        private float ManhattanHeuristic(PointF a, PointF b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        private float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
