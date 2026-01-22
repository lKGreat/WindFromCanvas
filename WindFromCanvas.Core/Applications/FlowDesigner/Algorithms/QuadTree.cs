using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
    /// 四叉树空间索引（用于优化大规模节点的碰撞检测和空间查询）
    /// </summary>
    public class QuadTree<T> where T : IBoundable
    {
        private const int MaxItems = 10; // 每个节点最大项目数
        private const int MaxDepth = 8;  // 最大深度

        private readonly RectangleF _bounds;
        private readonly List<T> _items = new List<T>();
        private QuadTree<T>[] _children;
        private readonly int _depth;

        public QuadTree(RectangleF bounds) : this(bounds, 0)
        {
        }

        private QuadTree(RectangleF bounds, int depth)
        {
            _bounds = bounds;
            _depth = depth;
        }

        /// <summary>
        /// 插入项目
        /// </summary>
        public void Insert(T item)
        {
            if (item == null)
            {
                return;
            }

            if (!_bounds.IntersectsWith(item.Bounds))
            {
                return;
            }

            // 如果有子节点，尝试插入到子节点
            if (_children != null)
            {
                int index = GetChildIndex(item.Bounds);
                if (index != -1)
                {
                    _children[index].Insert(item);
                    return;
                }
            }

            // 添加到当前节点
            _items.Add(item);

            // 如果超过最大项目数且未达到最大深度，则细分
            if (_items.Count > MaxItems && _depth < MaxDepth)
            {
                Subdivide();
            }
        }

        /// <summary>
        /// 查询区域内的所有项目
        /// </summary>
        public List<T> Query(RectangleF area)
        {
            var result = new List<T>();
            QueryRecursive(area, result);
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
        /// 移除项目
        /// </summary>
        public bool Remove(T item)
        {
            if (item == null)
            {
                return false;
            }

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
                    return _children[index].Remove(item);
                }
            }

            return false;
        }

        /// <summary>
        /// 清空所有项目
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _children = null;
        }

        /// <summary>
        /// 获取所有项目
        /// </summary>
        public List<T> GetAllItems()
        {
            var result = new List<T>();
            GetAllItemsRecursive(result);
            return result;
        }

        /// <summary>
        /// 获取边界
        /// </summary>
        public RectangleF Bounds => _bounds;

        /// <summary>
        /// 获取项目数量
        /// </summary>
        public int Count
        {
            get
            {
                int count = _items.Count;
                if (_children != null)
                {
                    foreach (var child in _children)
                    {
                        count += child.Count;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 细分节点
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
            _children[0] = new QuadTree<T>(new RectangleF(x, y, halfWidth, halfHeight), _depth + 1); // 左上
            _children[1] = new QuadTree<T>(new RectangleF(x + halfWidth, y, halfWidth, halfHeight), _depth + 1); // 右上
            _children[2] = new QuadTree<T>(new RectangleF(x, y + halfHeight, halfWidth, halfHeight), _depth + 1); // 左下
            _children[3] = new QuadTree<T>(new RectangleF(x + halfWidth, y + halfHeight, halfWidth, halfHeight), _depth + 1); // 右下

            // 将当前项目重新分配到子节点
            var itemsToRedistribute = new List<T>(_items);
            _items.Clear();

            foreach (var item in itemsToRedistribute)
            {
                int index = GetChildIndex(item.Bounds);
                if (index != -1)
                {
                    _children[index].Insert(item);
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
    }
}
