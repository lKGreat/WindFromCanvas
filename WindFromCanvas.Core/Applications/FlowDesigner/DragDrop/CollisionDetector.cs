using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Algorithms;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 碰撞检测器（匹配 Activepieces rectIntersection）
    /// 使用四叉树优化大规模场景的性能
    /// </summary>
    public class CollisionDetector
    {
        private QuadTree<DropTargetWrapper> _quadTree;
        private RectangleF _quadTreeBounds;
        private const int QuadTreeThreshold = 50; // 超过50个目标时使用四叉树

        /// <summary>
        /// 检测碰撞（矩形相交）
        /// </summary>
        public IDropTarget DetectCollision(DragContext context, List<IDropTarget> targets, RectangleF dragRect)
        {
            if (targets == null || targets.Count == 0)
            {
                return null;
            }

            // 对于小规模场景，使用简单遍历
            if (targets.Count < QuadTreeThreshold)
            {
                return DetectCollisionSimple(context, targets, dragRect);
            }

            // 对于大规模场景，使用四叉树优化
            return DetectCollisionWithQuadTree(context, targets, dragRect);
        }

        /// <summary>
        /// 简单碰撞检测（小规模场景）
        /// </summary>
        private IDropTarget DetectCollisionSimple(DragContext context, List<IDropTarget> targets, RectangleF dragRect)
        {
            foreach (var target in targets)
            {
                if (target is IHasBounds boundedTarget)
                {
                    if (dragRect.IntersectsWith(boundedTarget.Bounds))
                    {
                        if (target.CanAccept(context.Item))
                        {
                            return target;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 使用四叉树的碰撞检测（大规模场景）
        /// </summary>
        private IDropTarget DetectCollisionWithQuadTree(DragContext context, List<IDropTarget> targets, RectangleF dragRect)
        {
            // 重建四叉树（如果边界发生变化或目标列表变化）
            RebuildQuadTreeIfNeeded(targets);

            // 查询四叉树
            var candidates = _quadTree.Query(dragRect);

            // 检查候选目标
            foreach (var wrapper in candidates)
            {
                var target = wrapper.Target;
                if (target.CanAccept(context.Item))
                {
                    return target;
                }
            }

            return null;
        }

        /// <summary>
        /// 重建四叉树（如果需要）
        /// </summary>
        private void RebuildQuadTreeIfNeeded(List<IDropTarget> targets)
        {
            // 计算所有目标的边界
            RectangleF? bounds = null;
            foreach (var target in targets)
            {
                if (target is IHasBounds boundedTarget)
                {
                    if (bounds.HasValue)
                    {
                        bounds = RectangleF.Union(bounds.Value, boundedTarget.Bounds);
                    }
                    else
                    {
                        bounds = boundedTarget.Bounds;
                    }
                }
            }

            if (!bounds.HasValue)
            {
                return;
            }

            // 扩展边界（添加边距）
            var expandedBounds = new RectangleF(
                bounds.Value.X - 100,
                bounds.Value.Y - 100,
                bounds.Value.Width + 200,
                bounds.Value.Height + 200
            );

            // 如果边界发生变化，重建四叉树
            if (_quadTree == null || !_quadTreeBounds.Equals(expandedBounds))
            {
                _quadTreeBounds = expandedBounds;
                _quadTree = new QuadTree<DropTargetWrapper>(_quadTreeBounds);

                // 插入所有目标
                foreach (var target in targets)
                {
                    if (target is IHasBounds boundedTarget)
                    {
                        _quadTree.Insert(new DropTargetWrapper(target, boundedTarget.Bounds));
                    }
                }
            }
        }

        /// <summary>
        /// 清除四叉树
        /// </summary>
        public void Clear()
        {
            _quadTree?.Clear();
        }
    }

    /// <summary>
    /// 放置目标包装器（实现IBoundable接口）
    /// </summary>
    internal class DropTargetWrapper : IBoundable
    {
        public IDropTarget Target { get; }
        public RectangleF Bounds { get; }

        public DropTargetWrapper(IDropTarget target, RectangleF bounds)
        {
            Target = target;
            Bounds = bounds;
        }
    }

    /// <summary>
    /// 有边界的接口
    /// </summary>
    public interface IHasBounds
    {
        RectangleF Bounds { get; }
    }
}
