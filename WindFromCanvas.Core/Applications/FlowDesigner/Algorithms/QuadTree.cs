using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Algorithms
{
    /// <summary>
    /// 可边界化接口
    /// </summary>
    public interface IBoundable
    {
        RectangleF Bounds { get; }
    }

    /// <summary>
    /// 四叉树配置
    /// </summary>
    public class QuadTreeConfig
    {
        /// <summary>
        /// 每个节点最大项目数（5.2.1 动态细分阈值）
        /// </summary>
        public int MaxItemsPerNode { get; set; } = 10;

        /// <summary>
        /// 最大深度
        /// </summary>
        public int MaxDepth { get; set; } = 8;

        /// <summary>
        /// 最小节点尺寸（避免过度细分）
        /// </summary>
        public float MinNodeSize { get; set; } = 10f;

        /// <summary>
        /// 合并阈值（子节点总项目数低于此值时合并）
        /// </summary>
        public int MergeThreshold { get; set; } = 5;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static QuadTreeConfig Default => new QuadTreeConfig();

        /// <summary>
        /// 高性能配置（更激进的细分）
        /// </summary>
        public static QuadTreeConfig HighPerformance => new QuadTreeConfig
        {
            MaxItemsPerNode = 5,
            MaxDepth = 10,
            MinNodeSize = 5f,
            MergeThreshold = 3
        };
    }

    /// <summary>
    /// 四叉树空间索引（用于优化大规模节点的碰撞检测和空间查询）
    /// 5.2 优化版本：包含动态细分、批量查询、树重建机制
    /// </summary>
    public class QuadTree<T> where T : IBoundable
    {
        private readonly QuadTreeConfig _config;
        private RectangleF _bounds;
        private readonly List<T> _items = new List<T>();
        private QuadTree<T>[] _children;
        private readonly int _depth;
        private int _totalItemCount = 0;
        private bool _needsRebuild = false;
        private readonly object _lock = new object();

        // 5.2.4 树重建统计
        private int _insertCount = 0;
        private int _removeCount = 0;
        private const int RebuildThreshold = 100; // 操作次数阈值

        public QuadTree(RectangleF bounds) : this(bounds, 0, QuadTreeConfig.Default)
        {
        }

        public QuadTree(RectangleF bounds, QuadTreeConfig config) : this(bounds, 0, config)
        {
        }

        private QuadTree(RectangleF bounds, int depth, QuadTreeConfig config)
        {
            _bounds = bounds;
            _depth = depth;
            _config = config ?? QuadTreeConfig.Default;
        }

        /// <summary>
        /// 5.2.2 优化插入项目（支持线程安全）
        /// </summary>
        public void Insert(T item)
        {
            if (item == null)
            {
                return;
            }

            lock (_lock)
            {
                InsertInternal(item);
                _insertCount++;
                CheckRebuildNeeded();
            }
        }

        private void InsertInternal(T item)
        {
            if (!_bounds.IntersectsWith(item.Bounds))
            {
                return;
            }

            _totalItemCount++;

            // 如果有子节点，尝试插入到子节点
            if (_children != null)
            {
                int index = GetChildIndex(item.Bounds);
                if (index != -1)
                {
                    _children[index].InsertInternal(item);
                    return;
                }
            }

            // 添加到当前节点
            _items.Add(item);

            // 5.2.1 动态细分阈值检查
            if (ShouldSubdivide())
            {
                Subdivide();
            }
        }

        /// <summary>
        /// 5.2.2 批量插入（优化性能）
        /// </summary>
        public void InsertRange(IEnumerable<T> items)
        {
            if (items == null) return;

            lock (_lock)
            {
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        InsertInternal(item);
                    }
                }
                _insertCount += items.Count();
                CheckRebuildNeeded();
            }
        }

        /// <summary>
        /// 5.2.1 判断是否应该细分
        /// </summary>
        private bool ShouldSubdivide()
        {
            // 检查深度限制
            if (_depth >= _config.MaxDepth)
                return false;

            // 检查最小节点尺寸
            if (_bounds.Width / 2 < _config.MinNodeSize || _bounds.Height / 2 < _config.MinNodeSize)
                return false;

            // 检查项目数量
            return _items.Count > _config.MaxItemsPerNode;
        }

        /// <summary>
        /// 查询区域内的所有项目
        /// </summary>
        public List<T> Query(RectangleF area)
        {
            var result = new List<T>();
            lock (_lock)
            {
                QueryRecursive(area, result);
            }
            return result;
        }

        /// <summary>
        /// 查询点附近的项目
        /// </summary>
        public List<T> Query(PointF point, float radius)
        {
            var area = new RectangleF(
                point.X - radius,
                point.Y - radius,
                radius * 2,
                radius * 2
            );
            return Query(area);
        }

        /// <summary>
        /// 5.2.3 批量查询（一次查询多个区域）
        /// </summary>
        public Dictionary<RectangleF, List<T>> QueryBatch(IEnumerable<RectangleF> areas)
        {
            var results = new Dictionary<RectangleF, List<T>>();
            
            lock (_lock)
            {
                foreach (var area in areas)
                {
                    var items = new List<T>();
                    QueryRecursive(area, items);
                    results[area] = items;
                }
            }
            
            return results;
        }

        /// <summary>
        /// 5.2.3 批量点查询
        /// </summary>
        public Dictionary<PointF, List<T>> QueryBatchPoints(IEnumerable<PointF> points, float radius)
        {
            var results = new Dictionary<PointF, List<T>>();
            
            lock (_lock)
            {
                foreach (var point in points)
                {
                    var area = new RectangleF(
                        point.X - radius,
                        point.Y - radius,
                        radius * 2,
                        radius * 2
                    );
                    var items = new List<T>();
                    QueryRecursive(area, items);
                    results[point] = items;
                }
            }
            
            return results;
        }

        /// <summary>
        /// 查询与给定项目相交的所有项目
        /// </summary>
        public List<T> QueryIntersecting(T item)
        {
            if (item == null)
                return new List<T>();
            
            return Query(item.Bounds).Where(i => !ReferenceEquals(i, item)).ToList();
        }

        /// <summary>
        /// 查询最近的项目
        /// </summary>
        public T QueryNearest(PointF point)
        {
            // 从小半径开始逐步扩大搜索范围
            float radius = 10f;
            float maxRadius = Math.Max(_bounds.Width, _bounds.Height);
            
            while (radius <= maxRadius)
            {
                var candidates = Query(point, radius);
                if (candidates.Count > 0)
                {
                    // 找到真正最近的
                    return candidates.OrderBy(item =>
                    {
                        var center = GetCenter(item.Bounds);
                        return (center.X - point.X) * (center.X - point.X) + 
                               (center.Y - point.Y) * (center.Y - point.Y);
                    }).First();
                }
                radius *= 2;
            }
            
            return default;
        }

        private PointF GetCenter(RectangleF rect)
        {
            return new PointF(
                rect.X + rect.Width / 2,
                rect.Y + rect.Height / 2
            );
        }

        /// <summary>
        /// 移除项目
        /// </summary>
        public bool Remove(T item)
        {
            if (item == null)
            {
                return false;
            }

            lock (_lock)
            {
                bool removed = RemoveInternal(item);
                if (removed)
                {
                    _removeCount++;
                    _totalItemCount--;
                    CheckRebuildNeeded();
                    TryMergeChildren();
                }
                return removed;
            }
        }

        private bool RemoveInternal(T item)
        {
            if (!_bounds.IntersectsWith(item.Bounds))
            {
                return false;
            }

            // 尝试从当前节点移除
            if (_items.Remove(item))
            {
                return true;
            }

            // 尝试从子节点移除
            if (_children != null)
            {
                int index = GetChildIndex(item.Bounds);
                if (index != -1)
                {
                    return _children[index].RemoveInternal(item);
                }
                
                // 如果索引为-1，项目可能跨越多个子节点，需要搜索所有子节点
                foreach (var child in _children)
                {
                    if (child.RemoveInternal(item))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 批量移除
        /// </summary>
        public int RemoveRange(IEnumerable<T> items)
        {
            if (items == null) return 0;

            int removedCount = 0;
            lock (_lock)
            {
                foreach (var item in items)
                {
                    if (RemoveInternal(item))
                    {
                        removedCount++;
                        _totalItemCount--;
                    }
                }
                _removeCount += removedCount;
                CheckRebuildNeeded();
                TryMergeChildren();
            }
            return removedCount;
        }

        /// <summary>
        /// 尝试合并子节点（当子节点总项目数过少时）
        /// </summary>
        private void TryMergeChildren()
        {
            if (_children == null) return;

            int totalChildItems = 0;
            foreach (var child in _children)
            {
                totalChildItems += child.GetItemCountDirect();
            }

            if (totalChildItems <= _config.MergeThreshold)
            {
                // 将所有子节点的项目合并到当前节点
                foreach (var child in _children)
                {
                    _items.AddRange(child.GetAllItemsDirect());
                }
                _children = null;
            }
        }

        private int GetItemCountDirect()
        {
            int count = _items.Count;
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    count += child.GetItemCountDirect();
                }
            }
            return count;
        }

        private List<T> GetAllItemsDirect()
        {
            var result = new List<T>(_items);
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    result.AddRange(child.GetAllItemsDirect());
                }
            }
            return result;
        }

        /// <summary>
        /// 清空所有项目
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
                _children = null;
                _totalItemCount = 0;
                _insertCount = 0;
                _removeCount = 0;
                _needsRebuild = false;
            }
        }

        /// <summary>
        /// 5.2.4 检查是否需要重建
        /// </summary>
        private void CheckRebuildNeeded()
        {
            if (_insertCount + _removeCount >= RebuildThreshold)
            {
                _needsRebuild = true;
            }
        }

        /// <summary>
        /// 5.2.4 重建四叉树（优化结构）
        /// </summary>
        public void Rebuild()
        {
            lock (_lock)
            {
                if (!_needsRebuild && _children != null)
                    return;

                // 获取所有项目
                var allItems = GetAllItemsDirect();

                // 清空当前结构
                _items.Clear();
                _children = null;
                _totalItemCount = 0;

                // 重新插入所有项目
                foreach (var item in allItems)
                {
                    InsertInternal(item);
                }

                _insertCount = 0;
                _removeCount = 0;
                _needsRebuild = false;
            }
        }

        /// <summary>
        /// 是否需要重建
        /// </summary>
        public bool NeedsRebuild => _needsRebuild;

        /// <summary>
        /// 获取所有项目
        /// </summary>
        public List<T> GetAllItems()
        {
            lock (_lock)
            {
                var result = new List<T>();
                GetAllItemsRecursive(result);
                return result;
            }
        }

        /// <summary>
        /// 获取边界
        /// </summary>
        public RectangleF Bounds => _bounds;

        /// <summary>
        /// 获取项目数量（缓存值，O(1)）
        /// </summary>
        public int Count => _totalItemCount;

        /// <summary>
        /// 获取树深度
        /// </summary>
        public int Depth => _depth;

        /// <summary>
        /// 是否有子节点
        /// </summary>
        public bool HasChildren => _children != null;

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public QuadTreeStatistics GetStatistics()
        {
            lock (_lock)
            {
                var stats = new QuadTreeStatistics();
                CollectStatistics(stats);
                return stats;
            }
        }

        private void CollectStatistics(QuadTreeStatistics stats)
        {
            stats.TotalNodes++;
            stats.TotalItems += _items.Count;
            stats.MaxDepth = Math.Max(stats.MaxDepth, _depth);

            if (_children == null)
            {
                stats.LeafNodes++;
            }
            else
            {
                foreach (var child in _children)
                {
                    child.CollectStatistics(stats);
                }
            }
        }

        /// <summary>
        /// 5.2.1 细分节点（使用配置）
        /// </summary>
        private void Subdivide()
        {
            if (_children != null)
            {
                return; // 已经细分过
            }

            float halfWidth = _bounds.Width / 2f;
            float halfHeight = _bounds.Height / 2f;
            float x = _bounds.X;
            float y = _bounds.Y;

            _children = new QuadTree<T>[4];
            _children[0] = new QuadTree<T>(new RectangleF(x, y, halfWidth, halfHeight), _depth + 1, _config); // 左上
            _children[1] = new QuadTree<T>(new RectangleF(x + halfWidth, y, halfWidth, halfHeight), _depth + 1, _config); // 右上
            _children[2] = new QuadTree<T>(new RectangleF(x, y + halfHeight, halfWidth, halfHeight), _depth + 1, _config); // 左下
            _children[3] = new QuadTree<T>(new RectangleF(x + halfWidth, y + halfHeight, halfWidth, halfHeight), _depth + 1, _config); // 右下

            // 将当前项目重新分配到子节点（不更新总数，因为只是重新分配）
            var itemsToRedistribute = new List<T>(_items);
            _items.Clear();

            foreach (var item in itemsToRedistribute)
            {
                int index = GetChildIndex(item.Bounds);
                if (index != -1)
                {
                    _children[index].InsertInternal(item);
                    _children[index]._totalItemCount--; // 因为InsertInternal会增加计数，这里要减回来
                }
                else
                {
                    // 如果项目跨越多个子节点，保留在当前节点
                    _items.Add(item);
                }
            }
        }

        /// <summary>
        /// 获取项目所属的子节点索引
        /// </summary>
        private int GetChildIndex(RectangleF bounds)
        {
            if (_children == null)
            {
                return -1;
            }

            float midX = _bounds.X + _bounds.Width / 2f;
            float midY = _bounds.Y + _bounds.Height / 2f;

            bool left = bounds.Right <= midX;
            bool right = bounds.Left >= midX;
            bool top = bounds.Bottom <= midY;
            bool bottom = bounds.Top >= midY;

            // 如果项目跨越多个区域，返回-1
            if ((left && right) || (top && bottom))
            {
                return -1;
            }

            if (top && left) return 0;      // 左上
            if (top && right) return 1;     // 右上
            if (bottom && left) return 2;   // 左下
            if (bottom && right) return 3;   // 右下

            return -1; // 跨越多个区域
        }

        /// <summary>
        /// 递归查询
        /// </summary>
        private void QueryRecursive(RectangleF area, List<T> result)
        {
            if (!_bounds.IntersectsWith(area))
            {
                return;
            }

            // 添加当前节点的项目
            foreach (var item in _items)
            {
                if (area.IntersectsWith(item.Bounds))
                {
                    result.Add(item);
                }
            }

            // 查询子节点
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.QueryRecursive(area, result);
                }
            }
        }

        /// <summary>
        /// 递归获取所有项目
        /// </summary>
        private void GetAllItemsRecursive(List<T> result)
        {
            result.AddRange(_items);

            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.GetAllItemsRecursive(result);
                }
            }
        }

        /// <summary>
        /// 更新项目位置（移除后重新插入）
        /// </summary>
        public bool Update(T item, RectangleF oldBounds)
        {
            lock (_lock)
            {
                // 创建临时项目用于查找旧位置
                if (RemoveAtBounds(item, oldBounds))
                {
                    InsertInternal(item);
                    return true;
                }
                return false;
            }
        }

        private bool RemoveAtBounds(T item, RectangleF bounds)
        {
            if (!_bounds.IntersectsWith(bounds))
            {
                return false;
            }

            // 尝试从当前节点移除
            if (_items.Remove(item))
            {
                _totalItemCount--;
                return true;
            }

            // 尝试从子节点移除
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    if (child.RemoveAtBounds(item, bounds))
                    {
                        _totalItemCount--;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 扩展四叉树边界（当项目超出当前边界时）
        /// </summary>
        public void ExpandBounds(RectangleF newBounds)
        {
            lock (_lock)
            {
                if (_bounds.Contains(newBounds))
                    return;

                // 计算新的边界
                var expandedBounds = RectangleF.Union(_bounds, newBounds);
                
                // 如果边界变化较大，重建整棵树
                if (expandedBounds.Width > _bounds.Width * 2 || expandedBounds.Height > _bounds.Height * 2)
                {
                    var allItems = GetAllItemsDirect();
                    _bounds = expandedBounds;
                    _items.Clear();
                    _children = null;
                    _totalItemCount = 0;

                    foreach (var item in allItems)
                    {
                        InsertInternal(item);
                    }
                }
                else
                {
                    _bounds = expandedBounds;
                }
            }
        }

        /// <summary>
        /// 遍历所有项目（不创建列表）
        /// </summary>
        public void ForEach(Action<T> action)
        {
            if (action == null) return;

            lock (_lock)
            {
                foreach (var item in _items)
                {
                    action(item);
                }

                if (_children != null)
                {
                    foreach (var child in _children)
                    {
                        child.ForEachInternal(action);
                    }
                }
            }
        }

        private void ForEachInternal(Action<T> action)
        {
            foreach (var item in _items)
            {
                action(item);
            }

            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.ForEachInternal(action);
                }
            }
        }

        /// <summary>
        /// 条件查询（使用谓词过滤）
        /// </summary>
        public List<T> QueryWhere(RectangleF area, Func<T, bool> predicate)
        {
            var result = new List<T>();
            lock (_lock)
            {
                QueryWhereRecursive(area, predicate, result);
            }
            return result;
        }

        private void QueryWhereRecursive(RectangleF area, Func<T, bool> predicate, List<T> result)
        {
            if (!_bounds.IntersectsWith(area))
            {
                return;
            }

            foreach (var item in _items)
            {
                if (area.IntersectsWith(item.Bounds) && predicate(item))
                {
                    result.Add(item);
                }
            }

            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.QueryWhereRecursive(area, predicate, result);
                }
            }
        }
    }

    /// <summary>
    /// 四叉树统计信息
    /// </summary>
    public class QuadTreeStatistics
    {
        /// <summary>
        /// 总节点数
        /// </summary>
        public int TotalNodes { get; set; }

        /// <summary>
        /// 叶子节点数
        /// </summary>
        public int LeafNodes { get; set; }

        /// <summary>
        /// 总项目数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 最大深度
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// 平均每节点项目数
        /// </summary>
        public float AverageItemsPerNode => TotalNodes > 0 ? (float)TotalItems / TotalNodes : 0;

        public override string ToString()
        {
            return $"Nodes: {TotalNodes}, Leaves: {LeafNodes}, Items: {TotalItems}, MaxDepth: {MaxDepth}, Avg: {AverageItemsPerNode:F2}";
        }
    }
}
