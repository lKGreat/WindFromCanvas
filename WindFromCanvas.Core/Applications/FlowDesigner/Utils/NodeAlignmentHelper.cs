using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 节点对齐和分布辅助类
    /// </summary>
    public static class NodeAlignmentHelper
    {
        /// <summary>
        /// 对齐方式
        /// </summary>
        public enum AlignmentType
        {
            Left,       // 左对齐
            Right,      // 右对齐
            Top,        // 顶对齐
            Bottom,     // 底对齐
            CenterX,    // 水平居中
            CenterY     // 垂直居中
        }

        /// <summary>
        /// 分布方式
        /// </summary>
        public enum DistributionType
        {
            Horizontal, // 水平均匀分布
            Vertical    // 垂直均匀分布
        }

        /// <summary>
        /// 对齐节点
        /// </summary>
        public static void AlignNodes(IEnumerable<FlowNode> nodes, AlignmentType alignmentType)
        {
            if (nodes == null || !nodes.Any()) return;

            var nodeList = nodes.ToList();
            if (nodeList.Count < 2) return;

            var bounds = nodeList.Select(n => n.GetBounds()).ToList();

            switch (alignmentType)
            {
                case AlignmentType.Left:
                    var minLeft = bounds.Min(b => b.Left);
                    foreach (var node in nodeList)
                    {
                        node.X = minLeft;
                        node.UpdatePosition();
                    }
                    break;

                case AlignmentType.Right:
                    var maxRight = bounds.Max(b => b.Right);
                    foreach (var node in nodeList)
                    {
                        node.X = maxRight - node.Width;
                        node.UpdatePosition();
                    }
                    break;

                case AlignmentType.Top:
                    var minTop = bounds.Min(b => b.Top);
                    foreach (var node in nodeList)
                    {
                        node.Y = minTop;
                        node.UpdatePosition();
                    }
                    break;

                case AlignmentType.Bottom:
                    var maxBottom = bounds.Max(b => b.Bottom);
                    foreach (var node in nodeList)
                    {
                        node.Y = maxBottom - node.Height;
                        node.UpdatePosition();
                    }
                    break;

                case AlignmentType.CenterX:
                    var centerX = bounds.Average(b => b.Left + b.Width / 2);
                    foreach (var node in nodeList)
                    {
                        node.X = centerX - node.Width / 2;
                        node.UpdatePosition();
                    }
                    break;

                case AlignmentType.CenterY:
                    var centerY = bounds.Average(b => b.Top + b.Height / 2);
                    foreach (var node in nodeList)
                    {
                        node.Y = centerY - node.Height / 2;
                        node.UpdatePosition();
                    }
                    break;
            }
        }

        /// <summary>
        /// 分布节点
        /// </summary>
        public static void DistributeNodes(IEnumerable<FlowNode> nodes, DistributionType distributionType)
        {
            if (nodes == null || !nodes.Any()) return;

            var nodeList = nodes.ToList();
            if (nodeList.Count < 3) return; // 至少需要3个节点才能分布

            var bounds = nodeList.Select(n => n.GetBounds()).ToList();

            switch (distributionType)
            {
                case DistributionType.Horizontal:
                    // 按X坐标排序
                    var sortedByX = nodeList.OrderBy(n => n.X).ToList();
                    var minX = sortedByX.First().X;
                    var maxX = sortedByX.Last().X;
                    var totalWidth = maxX - minX;
                    var spacing = totalWidth / (sortedByX.Count - 1);

                    for (int i = 0; i < sortedByX.Count; i++)
                    {
                        sortedByX[i].X = minX + i * spacing;
                        sortedByX[i].UpdatePosition();
                    }
                    break;

                case DistributionType.Vertical:
                    // 按Y坐标排序
                    var sortedByY = nodeList.OrderBy(n => n.Y).ToList();
                    var minY = sortedByY.First().Y;
                    var maxY = sortedByY.Last().Y;
                    var totalHeight = maxY - minY;
                    var spacingY = totalHeight / (sortedByY.Count - 1);

                    for (int i = 0; i < sortedByY.Count; i++)
                    {
                        sortedByY[i].Y = minY + i * spacingY;
                        sortedByY[i].UpdatePosition();
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取对齐辅助线
        /// </summary>
        public static List<RectangleF> GetAlignmentGuides(IEnumerable<FlowNode> nodes, FlowNode draggingNode)
        {
            var guides = new List<RectangleF>();
            if (nodes == null || draggingNode == null) return guides;

            var nodeList = nodes.Where(n => n != draggingNode).ToList();
            if (nodeList.Count == 0) return guides;

            var draggingBounds = draggingNode.GetBounds();
            const float guideThickness = 1f;
            const float guideLength = 1000f; // 辅助线长度

            foreach (var node in nodeList)
            {
                var bounds = node.GetBounds();

                // 左对齐线
                if (Math.Abs(bounds.Left - draggingBounds.Left) < 5)
                {
                    guides.Add(new RectangleF(bounds.Left, -guideLength / 2, guideThickness, guideLength));
                }

                // 右对齐线
                if (Math.Abs(bounds.Right - draggingBounds.Right) < 5)
                {
                    guides.Add(new RectangleF(bounds.Right, -guideLength / 2, guideThickness, guideLength));
                }

                // 顶对齐线
                if (Math.Abs(bounds.Top - draggingBounds.Top) < 5)
                {
                    guides.Add(new RectangleF(-guideLength / 2, bounds.Top, guideLength, guideThickness));
                }

                // 底对齐线
                if (Math.Abs(bounds.Bottom - draggingBounds.Bottom) < 5)
                {
                    guides.Add(new RectangleF(-guideLength / 2, bounds.Bottom, guideLength, guideThickness));
                }

                // 水平中心对齐线
                var nodeCenterX = bounds.Left + bounds.Width / 2;
                var draggingCenterX = draggingBounds.Left + draggingBounds.Width / 2;
                if (Math.Abs(nodeCenterX - draggingCenterX) < 5)
                {
                    guides.Add(new RectangleF(nodeCenterX, -guideLength / 2, guideThickness, guideLength));
                }

                // 垂直中心对齐线
                var nodeCenterY = bounds.Top + bounds.Height / 2;
                var draggingCenterY = draggingBounds.Top + draggingBounds.Height / 2;
                if (Math.Abs(nodeCenterY - draggingCenterY) < 5)
                {
                    guides.Add(new RectangleF(-guideLength / 2, nodeCenterY, guideLength, guideThickness));
                }
            }

            return guides;
        }

        /// <summary>
        /// 自动吸附对齐
        /// </summary>
        public static void SnapToAlignment(FlowNode draggingNode, IEnumerable<FlowNode> otherNodes, float snapDistance = 5f)
        {
            if (draggingNode == null || otherNodes == null) return;

            var draggingBounds = draggingNode.GetBounds();
            var minDistance = float.MaxValue;
            float? snapX = null;
            float? snapY = null;

            foreach (var node in otherNodes)
            {
                var bounds = node.GetBounds();

                // 检查左对齐
                var distLeft = Math.Abs(bounds.Left - draggingBounds.Left);
                if (distLeft < snapDistance && distLeft < minDistance)
                {
                    minDistance = distLeft;
                    snapX = bounds.Left;
                }

                // 检查右对齐
                var distRight = Math.Abs(bounds.Right - draggingBounds.Right);
                if (distRight < snapDistance && distRight < minDistance)
                {
                    minDistance = distRight;
                    snapX = bounds.Right - draggingNode.Width;
                }

                // 检查顶对齐
                var distTop = Math.Abs(bounds.Top - draggingBounds.Top);
                if (distTop < snapDistance && distTop < minDistance)
                {
                    minDistance = distTop;
                    snapY = bounds.Top;
                }

                // 检查底对齐
                var distBottom = Math.Abs(bounds.Bottom - draggingBounds.Bottom);
                if (distBottom < snapDistance && distBottom < minDistance)
                {
                    minDistance = distBottom;
                    snapY = bounds.Bottom - draggingNode.Height;
                }

                // 检查水平中心对齐
                var nodeCenterX = bounds.Left + bounds.Width / 2;
                var draggingCenterX = draggingBounds.Left + draggingBounds.Width / 2;
                var distCenterX = Math.Abs(nodeCenterX - draggingCenterX);
                if (distCenterX < snapDistance && distCenterX < minDistance)
                {
                    minDistance = distCenterX;
                    snapX = nodeCenterX - draggingNode.Width / 2;
                }

                // 检查垂直中心对齐
                var nodeCenterY = bounds.Top + bounds.Height / 2;
                var draggingCenterY = draggingBounds.Top + draggingBounds.Height / 2;
                var distCenterY = Math.Abs(nodeCenterY - draggingCenterY);
                if (distCenterY < snapDistance && distCenterY < minDistance)
                {
                    minDistance = distCenterY;
                    snapY = nodeCenterY - draggingNode.Height / 2;
                }
            }

            // 应用吸附
            if (snapX.HasValue)
            {
                draggingNode.X = snapX.Value;
            }
            if (snapY.HasValue)
            {
                draggingNode.Y = snapY.Value;
            }
        }
    }
}
