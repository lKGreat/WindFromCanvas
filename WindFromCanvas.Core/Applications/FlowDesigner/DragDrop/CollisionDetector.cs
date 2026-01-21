using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 碰撞检测器（匹配 Activepieces rectIntersection）
    /// </summary>
    public class CollisionDetector
    {
        /// <summary>
        /// 检测碰撞（矩形相交）
        /// </summary>
        public IDropTarget DetectCollision(DragContext context, List<IDropTarget> targets, RectangleF dragRect)
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
    }

    /// <summary>
    /// 有边界的接口
    /// </summary>
    public interface IHasBounds
    {
        RectangleF Bounds { get; }
    }
}
