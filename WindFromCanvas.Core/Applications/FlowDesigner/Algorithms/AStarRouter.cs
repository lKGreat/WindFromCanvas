using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Algorithms
{
    /// <summary>
    /// A*路径路由算法（用于直角折线连线的智能避障）
    /// 5.1 优化版本：包含PriorityQueue、路径缓存、路径平滑和多障碍物避让
    /// </summary>
    public class AStarRouter
    {
        private const float GridSize = 10f; // 网格大小
        private const float Margin = 50f; // 边界边距
        private const float SmoothingTolerance = 1.0f; // 路径平滑容差

        /// <summary>
        /// 5.1.3 路径缓存（LRU缓存，key: start_end_obstacles, value: path）
        /// </summary>
        private readonly LRUCache<string, List<PointF>> _pathCache;
        private const int MaxCacheSize = 200;
        private readonly object _cacheLock = new object();

        /// <summary>
        /// 5.1.1 高性能优先队列（二叉堆实现）
        /// </summary>
        private class PriorityQueue<T>
        {
            private readonly List<(T Item, float Priority)> _heap = new List<(T, float)>();
            private readonly Dictionary<T, int> _indexMap = new Dictionary<T, int>();

            public int Count => _heap.Count;
            public bool IsEmpty => _heap.Count == 0;

            public void Enqueue(T item, float priority)
            {
                if (_indexMap.ContainsKey(item))
                {
                    // 更新优先级
                    UpdatePriority(item, priority);
                    return;
                }

                _heap.Add((item, priority));
                int index = _heap.Count - 1;
                _indexMap[item] = index;
                BubbleUp(index);
            }

            public T Dequeue()
            {
                if (_heap.Count == 0)
                    throw new InvalidOperationException("Priority queue is empty");

                var result = _heap[0].Item;
                _indexMap.Remove(result);

                int lastIndex = _heap.Count - 1;
                if (lastIndex > 0)
                {
                    _heap[0] = _heap[lastIndex];
                    _indexMap[_heap[0].Item] = 0;
                }
                _heap.RemoveAt(lastIndex);

                if (_heap.Count > 0)
                    BubbleDown(0);

                return result;
            }

            public bool Contains(T item) => _indexMap.ContainsKey(item);

            public void UpdatePriority(T item, float newPriority)
            {
                if (!_indexMap.TryGetValue(item, out int index))
                    return;

                float oldPriority = _heap[index].Priority;
                _heap[index] = (item, newPriority);

                if (newPriority < oldPriority)
                    BubbleUp(index);
                else
                    BubbleDown(index);
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parentIndex = (index - 1) / 2;
                    if (_heap[index].Priority >= _heap[parentIndex].Priority)
                        break;

                    Swap(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void BubbleDown(int index)
            {
                int lastIndex = _heap.Count - 1;
                while (true)
                {
                    int smallest = index;
                    int leftChild = 2 * index + 1;
                    int rightChild = 2 * index + 2;

                    if (leftChild <= lastIndex && _heap[leftChild].Priority < _heap[smallest].Priority)
                        smallest = leftChild;
                    if (rightChild <= lastIndex && _heap[rightChild].Priority < _heap[smallest].Priority)
                        smallest = rightChild;

                    if (smallest == index)
                        break;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int i, int j)
            {
                var temp = _heap[i];
                _heap[i] = _heap[j];
                _heap[j] = temp;
                _indexMap[_heap[i].Item] = i;
                _indexMap[_heap[j].Item] = j;
            }

            public void Clear()
            {
                _heap.Clear();
                _indexMap.Clear();
            }
        }

        /// <summary>
        /// 5.1.3 LRU缓存实现
        /// </summary>
        private class LRUCache<TKey, TValue>
        {
            private readonly int _capacity;
            private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _cache;
            private readonly LinkedList<(TKey Key, TValue Value)> _lruList;

            public LRUCache(int capacity)
            {
                _capacity = capacity;
                _cache = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
                _lruList = new LinkedList<(TKey, TValue)>();
            }

            public bool TryGet(TKey key, out TValue value)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // 移动到列表头部（最近使用）
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
                value = default;
                return false;
            }

            public void Add(TKey key, TValue value)
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    // 更新现有项
                    _lruList.Remove(existingNode);
                    existingNode.Value = (key, value);
                    _lruList.AddFirst(existingNode);
                    return;
                }

                // 如果缓存已满，移除最久未使用的项
                if (_cache.Count >= _capacity)
                {
                    var lastNode = _lruList.Last;
                    if (lastNode != null)
                    {
                        _cache.Remove(lastNode.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                // 添加新项
                var newNode = new LinkedListNode<(TKey, TValue)>((key, value));
                _lruList.AddFirst(newNode);
                _cache[key] = newNode;
            }

            public void Clear()
            {
                _cache.Clear();
                _lruList.Clear();
            }

            public int Count => _cache.Count;
        }

        /// <summary>
        /// 路径节点
        /// </summary>
        private class PathNode : IEquatable<PathNode>
        {
            public PointF Position { get; set; }
            public float GCost { get; set; } // 从起点到当前节点的实际代价
            public float HCost { get; set; } // 从当前节点到终点的启发代价（曼哈顿距离）
            public float FCost => GCost + HCost; // 总代价
            public PathNode Parent { get; set; }

            public PathNode(PointF position)
            {
                Position = position;
                GCost = float.MaxValue;
            }

            public bool Equals(PathNode other)
            {
                if (other == null) return false;
                return Math.Abs(Position.X - other.Position.X) < 0.1f &&
                       Math.Abs(Position.Y - other.Position.Y) < 0.1f;
            }

            public override bool Equals(object obj) => Equals(obj as PathNode);

            public override int GetHashCode()
            {
                // 使用网格化的坐标作为哈希值，提高性能
                int x = (int)(Position.X / GridSize);
                int y = (int)(Position.Y / GridSize);
                return x * 73856093 ^ y * 19349663;
            }
        }

        public AStarRouter()
        {
            _pathCache = new LRUCache<string, List<PointF>>(MaxCacheSize);
        }

        /// <summary>
        /// 查找路径（同步方法）
        /// </summary>
        public List<PointF> FindPath(PointF start, PointF end, List<RectangleF> obstacles)
        {
            if (obstacles == null)
            {
                obstacles = new List<RectangleF>();
            }

            // 5.1.3 检查缓存（线程安全）
            var cacheKey = GenerateCacheKey(start, end, obstacles);
            lock (_cacheLock)
            {
                if (_pathCache.TryGet(cacheKey, out var cachedPath))
                {
                    return new List<PointF>(cachedPath);
                }
            }

            // 1. 建立搜索边界框
            var bounds = CreateSearchBounds(start, end, Margin);

            // 2. 生成候选路径点（优化网格化）
            var waypoints = GenerateWaypoints(bounds, obstacles, start, end);

            // 3. A*搜索（使用优先队列优化）
            var path = SearchPathOptimized(start, end, waypoints, obstacles);

            // 5.1.4 路径平滑处理
            path = SmoothPath(path, obstacles);

            // 5.1.3 缓存路径（线程安全）
            lock (_cacheLock)
            {
                _pathCache.Add(cacheKey, new List<PointF>(path));
            }

            return path;
        }

        /// <summary>
        /// 异步查找路径（用于后台计算）
        /// </summary>
        public Task<List<PointF>> FindPathAsync(PointF start, PointF end, List<RectangleF> obstacles, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return FindPath(start, end, obstacles);
            }, cancellationToken);
        }

        /// <summary>
        /// 批量查找路径（并行处理多条路径）
        /// </summary>
        public List<List<PointF>> FindPathsBatch(List<(PointF Start, PointF End)> pathRequests, List<RectangleF> obstacles)
        {
            if (pathRequests == null || pathRequests.Count == 0)
                return new List<List<PointF>>();

            var results = new List<PointF>[pathRequests.Count];

            Parallel.For(0, pathRequests.Count, i =>
            {
                var request = pathRequests[i];
                results[i] = FindPath(request.Start, request.End, obstacles);
            });

            return results.ToList();
        }

        /// <summary>
        /// 5.1.3 生成缓存键（使用哈希优化）
        /// </summary>
        private string GenerateCacheKey(PointF start, PointF end, List<RectangleF> obstacles)
        {
            // 使用更高效的哈希计算
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)(start.X / GridSize);
                hash = hash * 31 + (int)(start.Y / GridSize);
                hash = hash * 31 + (int)(end.X / GridSize);
                hash = hash * 31 + (int)(end.Y / GridSize);

                // 障碍物哈希（只计算位置相关的哈希，不需要完整字符串）
                foreach (var o in obstacles)
                {
                    hash = hash * 31 + (int)(o.X / GridSize);
                    hash = hash * 31 + (int)(o.Y / GridSize);
                    hash = hash * 31 + (int)(o.Width / GridSize);
                    hash = hash * 31 + (int)(o.Height / GridSize);
                }

                return $"{start.X:F0},{start.Y:F0}_{end.X:F0},{end.Y:F0}_{hash}";
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _pathCache.Clear();
            }
        }

        /// <summary>
        /// 获取缓存命中率统计
        /// </summary>
        public int CacheCount
        {
            get
            {
                lock (_cacheLock)
                {
                    return _pathCache.Count;
                }
            }
        }

        /// <summary>
        /// 使缓存失效（当障碍物发生变化时调用）
        /// </summary>
        public void InvalidateCache()
        {
            ClearCache();
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
        /// 5.1.2 / 5.1.5 生成候选路径点（优化网格点生成策略，支持多障碍物避让）
        /// </summary>
        private List<PointF> GenerateWaypoints(RectangleF bounds, List<RectangleF> obstacles, PointF start, PointF end)
        {
            var waypoints = new List<PointF> { start, end };

            // 5.1.2 优化策略：优先生成障碍物边缘的网格点
            // 在障碍物边缘添加关键点（避障更精确）
            foreach (var obstacle in obstacles)
            {
                // 添加障碍物四个角及其周围的点
                var corners = new[]
                {
                    new PointF(obstacle.Left - GridSize, obstacle.Top - GridSize),
                    new PointF(obstacle.Right + GridSize, obstacle.Top - GridSize),
                    new PointF(obstacle.Left - GridSize, obstacle.Bottom + GridSize),
                    new PointF(obstacle.Right + GridSize, obstacle.Bottom + GridSize),
                };

                foreach (var corner in corners)
                {
                    if (bounds.Contains(corner) && !IsInAnyObstacle(corner, obstacles))
                    {
                        waypoints.Add(corner);
                    }
                }
            }

            // 在边界框内生成稀疏网格点（减少搜索空间）
            float step = GridSize * 2; // 5.1.2 使用更大的步长减少计算
            for (float x = bounds.Left; x <= bounds.Right; x += step)
            {
                for (float y = bounds.Top; y <= bounds.Bottom; y += step)
                {
                    var point = new PointF(x, y);

                    // 跳过起点和终点
                    if (Distance(point, start) < GridSize || Distance(point, end) < GridSize)
                    {
                        continue;
                    }

                    // 5.1.5 检查是否在任何障碍物内（多障碍物避让）
                    if (!IsInAnyObstacle(point, obstacles))
                    {
                        waypoints.Add(point);
                    }
                }
            }

            return waypoints;
        }

        /// <summary>
        /// 5.1.5 检查点是否在任何障碍物内
        /// </summary>
        private bool IsInAnyObstacle(PointF point, List<RectangleF> obstacles)
        {
            foreach (var obstacle in obstacles)
            {
                if (obstacle.Contains(point))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 5.1.1 A*搜索路径（使用优先队列优化）
        /// </summary>
        private List<PointF> SearchPathOptimized(PointF start, PointF end, List<PointF> waypoints, List<RectangleF> obstacles)
        {
            if (waypoints.Count == 0)
            {
                return new List<PointF> { start, end };
            }

            // 5.1.1 使用优先队列代替列表排序
            var openSet = new PriorityQueue<PathNode>();
            var closedSet = new HashSet<PointF>(new PointFComparer());
            var nodeMap = new Dictionary<PointF, PathNode>(new PointFComparer());

            // 创建起点节点
            var startNode = new PathNode(start)
            {
                GCost = 0,
                HCost = ManhattanHeuristic(start, end)
            };
            openSet.Enqueue(startNode, startNode.FCost);
            nodeMap[start] = startNode;

            // 为所有路径点创建节点（延迟创建优化）
            foreach (var waypoint in waypoints)
            {
                if (!nodeMap.ContainsKey(waypoint))
                {
                    var node = new PathNode(waypoint)
                    {
                        HCost = ManhattanHeuristic(waypoint, end)
                    };
                    nodeMap[waypoint] = node;
                }
            }

            // 确保终点在节点映射中
            if (!nodeMap.ContainsKey(end))
            {
                nodeMap[end] = new PathNode(end)
                {
                    HCost = 0
                };
            }

            while (!openSet.IsEmpty)
            {
                // 5.1.1 从优先队列中取出F代价最小的节点（O(log n)）
                var currentNode = openSet.Dequeue();
                
                // 已经处理过的节点跳过
                if (closedSet.Contains(currentNode.Position))
                    continue;
                    
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
                        
                        // 5.1.1 入队或更新优先级
                        openSet.Enqueue(neighbor, neighbor.FCost);
                    }
                }
            }

            // 如果找不到路径，返回直线路径
            return new List<PointF> { start, end };
        }

        /// <summary>
        /// PointF比较器（用于HashSet和Dictionary）
        /// </summary>
        private class PointFComparer : IEqualityComparer<PointF>
        {
            public bool Equals(PointF a, PointF b)
            {
                return Math.Abs(a.X - b.X) < 0.1f && Math.Abs(a.Y - b.Y) < 0.1f;
            }

            public int GetHashCode(PointF p)
            {
                int x = (int)(p.X / GridSize);
                int y = (int)(p.Y / GridSize);
                return x * 73856093 ^ y * 19349663;
            }
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

        /// <summary>
        /// 5.1.4 路径平滑处理
        /// 使用线段简化算法（类似Douglas-Peucker）减少路径点数量
        /// </summary>
        private List<PointF> SmoothPath(List<PointF> path, List<RectangleF> obstacles)
        {
            if (path == null || path.Count <= 2)
                return path;

            // 1. 首先尝试直接连接可达的点（贪心优化）
            var smoothed = GreedySmooth(path, obstacles);

            // 2. 然后进行路径简化（移除冗余的中间点）
            smoothed = SimplifyPath(smoothed, SmoothingTolerance);

            // 3. 最后确保路径仍然有效（不穿过障碍物）
            return ValidatePath(smoothed, obstacles) ? smoothed : path;
        }

        /// <summary>
        /// 贪心平滑：尝试跳过中间点直接连接
        /// </summary>
        private List<PointF> GreedySmooth(List<PointF> path, List<RectangleF> obstacles)
        {
            if (path.Count <= 2)
                return path;

            var result = new List<PointF> { path[0] };
            int current = 0;

            while (current < path.Count - 1)
            {
                // 尝试找到能直接到达的最远点
                int furthest = current + 1;
                for (int i = path.Count - 1; i > current + 1; i--)
                {
                    if (CanReachOrthogonal(path[current], path[i], obstacles))
                    {
                        furthest = i;
                        break;
                    }
                }

                result.Add(path[furthest]);
                current = furthest;
            }

            return result;
        }

        /// <summary>
        /// 检查是否可以通过正交路径（水平+垂直）到达
        /// </summary>
        private bool CanReachOrthogonal(PointF from, PointF to, List<RectangleF> obstacles)
        {
            // 尝试两种路径：先水平后垂直，或先垂直后水平
            var midPoint1 = new PointF(to.X, from.Y);
            var midPoint2 = new PointF(from.X, to.Y);

            // 路径1：水平->垂直
            bool path1Valid = CanReach(from, midPoint1, obstacles) && 
                              CanReach(midPoint1, to, obstacles);

            // 路径2：垂直->水平
            bool path2Valid = CanReach(from, midPoint2, obstacles) && 
                              CanReach(midPoint2, to, obstacles);

            return path1Valid || path2Valid;
        }

        /// <summary>
        /// Douglas-Peucker路径简化算法
        /// </summary>
        private List<PointF> SimplifyPath(List<PointF> path, float tolerance)
        {
            if (path.Count <= 2)
                return path;

            // 找到离起点-终点连线最远的点
            float maxDist = 0;
            int maxIndex = 0;

            var start = path[0];
            var end = path[path.Count - 1];

            for (int i = 1; i < path.Count - 1; i++)
            {
                float dist = PerpendicularDistance(path[i], start, end);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxIndex = i;
                }
            }

            // 如果最大距离大于容差，递归简化
            if (maxDist > tolerance)
            {
                var left = SimplifyPath(path.Take(maxIndex + 1).ToList(), tolerance);
                var right = SimplifyPath(path.Skip(maxIndex).ToList(), tolerance);

                // 合并结果（移除重复的中间点）
                var result = new List<PointF>(left);
                result.RemoveAt(result.Count - 1);
                result.AddRange(right);
                return result;
            }
            else
            {
                // 只保留起点和终点
                return new List<PointF> { start, end };
            }
        }

        /// <summary>
        /// 计算点到线段的垂直距离
        /// </summary>
        private float PerpendicularDistance(PointF point, PointF lineStart, PointF lineEnd)
        {
            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;

            // 如果线段长度为0，返回点到起点的距离
            float lineLengthSq = dx * dx + dy * dy;
            if (lineLengthSq < 0.0001f)
                return Distance(point, lineStart);

            // 计算垂直距离
            float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lineLengthSq;
            t = Math.Max(0, Math.Min(1, t));

            var projection = new PointF(
                lineStart.X + t * dx,
                lineStart.Y + t * dy
            );

            return Distance(point, projection);
        }

        /// <summary>
        /// 验证路径是否有效（不穿过障碍物）
        /// </summary>
        private bool ValidatePath(List<PointF> path, List<RectangleF> obstacles)
        {
            if (path.Count < 2)
                return true;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!CanReach(path[i], path[i + 1], obstacles))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 计算路径总长度
        /// </summary>
        public float CalculatePathLength(List<PointF> path)
        {
            if (path == null || path.Count < 2)
                return 0;

            float totalLength = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                totalLength += Distance(path[i], path[i + 1]);
            }
            return totalLength;
        }

        /// <summary>
        /// 获取路径的正交化版本（确保所有线段都是水平或垂直的）
        /// </summary>
        public List<PointF> GetOrthogonalPath(List<PointF> path)
        {
            if (path == null || path.Count < 2)
                return path;

            var result = new List<PointF> { path[0] };

            for (int i = 1; i < path.Count; i++)
            {
                var prev = result[result.Count - 1];
                var curr = path[i];

                // 如果不是水平或垂直，添加中间点
                if (Math.Abs(prev.X - curr.X) > 0.1f && Math.Abs(prev.Y - curr.Y) > 0.1f)
                {
                    // 选择先水平还是先垂直（选择移动距离较大的方向先移动）
                    if (Math.Abs(prev.X - curr.X) > Math.Abs(prev.Y - curr.Y))
                    {
                        result.Add(new PointF(curr.X, prev.Y)); // 先水平
                    }
                    else
                    {
                        result.Add(new PointF(prev.X, curr.Y)); // 先垂直
                    }
                }

                result.Add(curr);
            }

            return result;
        }
    }
}
