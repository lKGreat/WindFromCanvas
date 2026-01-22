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
        /// 3.4.4 对齐吸附阈值（1像素精度）
        /// </summary>
        public const float SnapThreshold = 1f;

        /// <summary>
        /// 对齐线检测阈值
        /// </summary>
        public const float AlignmentDetectionThreshold = 5f;

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
        /// 对齐线信息（用于渲染）
        /// </summary>
        public class AlignmentGuide
        {
            public AlignmentType Type { get; set; }
            public float Position { get; set; }
            public PointF Start { get; set; }
            public PointF End { get; set; }
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
        /// 3.4.2 / 3.4.3 获取对齐辅助线（中心线和边框对齐检测）
        /// </summary>
        public static List<AlignmentGuide> GetAlignmentGuides(IEnumerable<FlowNode> nodes, FlowNode draggingNode, RectangleF viewport)
        {
            var guides = new List<AlignmentGuide>();
            if (nodes == null || draggingNode == null) return guides;

            var nodeList = nodes.Where(n => n != draggingNode).ToList();
            if (nodeList.Count == 0) return guides;

            var draggingBounds = draggingNode.GetBounds();

            foreach (var node in nodeList)
            {
                var bounds = node.GetBounds();

                // 3.4.2 中心线对齐检测
                // 水平中心对齐线
                var nodeCenterX = bounds.Left + bounds.Width / 2;
                var draggingCenterX = draggingBounds.Left + draggingBounds.Width / 2;
                if (Math.Abs(nodeCenterX - draggingCenterX) < AlignmentDetectionThreshold)
                {
                    guides.Add(new AlignmentGuide
                    {
                        Type = AlignmentType.CenterX,
                        Position = nodeCenterX,
                        Start = new PointF(nodeCenterX, viewport.Top),
                        End = new PointF(nodeCenterX, viewport.Bottom)
                    });
                }

                // 垂直中心对齐线
                var nodeCenterY = bounds.Top + bounds.Height / 2;
                var draggingCenterY = draggingBounds.Top + draggingBounds.Height / 2;
                if (Math.Abs(nodeCenterY - draggingCenterY) < AlignmentDetectionThreshold)
                {
                    guides.Add(new AlignmentGuide
                    {
                        Type = AlignmentType.CenterY,
                        Position = nodeCenterY,
                        Start = new PointF(viewport.Left, nodeCenterY),
                        End = new PointF(viewport.Right, nodeCenterY)
                    });
                }

                // 3.4.3 边框对齐检测
                // 左对齐线
                if (Math.Abs(bounds.Left - draggingBounds.Left) < AlignmentDetectionThreshold)
                {
                    guides.Add(new AlignmentGuide
                    {
                        Type = AlignmentType.Left,
                        Position = bounds.Left,
                        Start = new PointF(bounds.Left, viewport.Top),
                        End = new PointF(bounds.Left, viewport.Bottom)
                    });
                }

                // 右对齐线
                if (Math.Abs(bounds.Right - draggingBounds.Right) < AlignmentDetectionThreshold)
                {
                    guides.Add(new AlignmentGuide
                    {
                        Type = AlignmentType.Right,
                        Position = bounds.Right,
                        Start = new PointF(bounds.Right, viewport.Top),
                        End = new PointF(bounds.Right, viewport.Bottom)
                    });
                }

                // 顶对齐线
                if (Math.Abs(bounds.Top - draggingBounds.Top) < AlignmentDetectionThreshold)
                {
                    guides.Add(new AlignmentGuide
                    {
                        Type = AlignmentType.Top,
                        Position = bounds.Top,
                        Start = new PointF(viewport.Left, bounds.Top),
                        End = new PointF(viewport.Right, bounds.Top)
                    });
                }

                // 底对齐线
                if (Math.Abs(bounds.Bottom - draggingBounds.Bottom) < AlignmentDetectionThreshold)
                {
                    guides.Add(new AlignmentGuide
                    {
                        Type = AlignmentType.Bottom,
                        Position = bounds.Bottom,
                        Start = new PointF(viewport.Left, bounds.Bottom),
                        End = new PointF(viewport.Right, bounds.Bottom)
                    });
                }
            }

            return guides;
        }

        /// <summary>
        /// 3.4.2 / 3.4.3 / 3.4.4 获取对齐辅助线（旧接口，保持兼容）
        /// </summary>
        public static List<RectangleF> GetAlignmentGuides(IEnumerable<FlowNode> nodes, FlowNode draggingNode)
        {
            var guides = new List<RectangleF>();
            if (nodes == null || draggingNode == null) return guides;

            var nodeList = nodes.Where(n => n != draggingNode).ToList();
            if (nodeList.Count == 0) return guides;

            var draggingBounds = draggingNode.GetBounds();
            const float guideThickness = 1f;
            const float guideLength = 10000f; // 辅助线长度

            foreach (var node in nodeList)
            {
                var bounds = node.GetBounds();

                // 左对齐线
                if (Math.Abs(bounds.Left - draggingBounds.Left) < AlignmentDetectionThreshold)
                {
                    guides.Add(new RectangleF(bounds.Left, -guideLength / 2, guideThickness, guideLength));
                }

                // 右对齐线
                if (Math.Abs(bounds.Right - draggingBounds.Right) < AlignmentDetectionThreshold)
                {
                    guides.Add(new RectangleF(bounds.Right, -guideLength / 2, guideThickness, guideLength));
                }

                // 顶对齐线
                if (Math.Abs(bounds.Top - draggingBounds.Top) < AlignmentDetectionThreshold)
                {
                    guides.Add(new RectangleF(-guideLength / 2, bounds.Top, guideLength, guideThickness));
                }

                // 底对齐线
                if (Math.Abs(bounds.Bottom - draggingBounds.Bottom) < AlignmentDetectionThreshold)
                {
                    guides.Add(new RectangleF(-guideLength / 2, bounds.Bottom, guideLength, guideThickness));
                }

                // 水平中心对齐线
                var nodeCenterX = bounds.Left + bounds.Width / 2;
                var draggingCenterX = draggingBounds.Left + draggingBounds.Width / 2;
                if (Math.Abs(nodeCenterX - draggingCenterX) < AlignmentDetectionThreshold)
                {
                    guides.Add(new RectangleF(nodeCenterX, -guideLength / 2, guideThickness, guideLength));
                }

                // 垂直中心对齐线
                var nodeCenterY = bounds.Top + bounds.Height / 2;
                var draggingCenterY = draggingBounds.Top + draggingBounds.Height / 2;
                if (Math.Abs(nodeCenterY - draggingCenterY) < AlignmentDetectionThreshold)
                {
                    guides.Add(new RectangleF(-guideLength / 2, nodeCenterY, guideLength, guideThickness));
                }
            }

            return guides;
        }

        /// <summary>
        /// 3.4.4 / 3.4.6 自动吸附对齐（1像素阈值，支持多节点同时对齐）
        /// </summary>
        public static void SnapToAlignment(FlowNode draggingNode, IEnumerable<FlowNode> otherNodes, float snapDistance = SnapThreshold)
        {
            if (draggingNode == null || otherNodes == null) return;

            var draggingBounds = draggingNode.GetBounds();
            float? snapX = null;
            float? snapY = null;
            float minXDistance = snapDistance;
            float minYDistance = snapDistance;

            foreach (var node in otherNodes)
            {
                var bounds = node.GetBounds();

                // 3.4.3 边框对齐检测
                // 左对齐
                var distLeft = Math.Abs(bounds.Left - draggingBounds.Left);
                if (distLeft < minXDistance)
                {
                    minXDistance = distLeft;
                    snapX = bounds.Left;
                }

                // 右对齐
                var distRight = Math.Abs(bounds.Right - draggingBounds.Right);
                if (distRight < minXDistance)
                {
                    minXDistance = distRight;
                    snapX = bounds.Right - draggingNode.Width;
                }

                // 顶对齐
                var distTop = Math.Abs(bounds.Top - draggingBounds.Top);
                if (distTop < minYDistance)
                {
                    minYDistance = distTop;
                    snapY = bounds.Top;
                }

                // 底对齐
                var distBottom = Math.Abs(bounds.Bottom - draggingBounds.Bottom);
                if (distBottom < minYDistance)
                {
                    minYDistance = distBottom;
                    snapY = bounds.Bottom - draggingNode.Height;
                }

                // 3.4.2 中心线对齐检测
                // 水平中心对齐
                var nodeCenterX = bounds.Left + bounds.Width / 2;
                var draggingCenterX = draggingBounds.Left + draggingBounds.Width / 2;
                var distCenterX = Math.Abs(nodeCenterX - draggingCenterX);
                if (distCenterX < minXDistance)
                {
                    minXDistance = distCenterX;
                    snapX = nodeCenterX - draggingNode.Width / 2;
                }

                // 垂直中心对齐
                var nodeCenterY = bounds.Top + bounds.Height / 2;
                var draggingCenterY = draggingBounds.Top + draggingBounds.Height / 2;
                var distCenterY = Math.Abs(nodeCenterY - draggingCenterY);
                if (distCenterY < minYDistance)
                {
                    minYDistance = distCenterY;
                    snapY = nodeCenterY - draggingNode.Height / 2;
                }
            }

            // 3.4.4 应用1像素阈值吸附
            if (snapX.HasValue)
            {
                draggingNode.X = snapX.Value;
            }
            if (snapY.HasValue)
            {
                draggingNode.Y = snapY.Value;
            }
        }

        /// <summary>
        /// 3.4.6 多节点同时对齐（批量吸附）
        /// </summary>
        public static void SnapMultipleNodesToAlignment(List<FlowNode> draggingNodes, IEnumerable<FlowNode> otherNodes, float snapDistance = SnapThreshold)
        {
            if (draggingNodes == null || draggingNodes.Count == 0)
                return;

            // 以第一个节点为基准计算偏移
            var mainNode = draggingNodes[0];
            var originalPosition = new PointF(mainNode.X, mainNode.Y);

            // 对主节点进行吸附
            SnapToAlignment(mainNode, otherNodes, snapDistance);

            // 计算偏移量
            var deltaX = mainNode.X - originalPosition.X;
            var deltaY = mainNode.Y - originalPosition.Y;

            // 应用相同的偏移到其他节点
            for (int i = 1; i < draggingNodes.Count; i++)
            {
                var node = draggingNodes[i];
                node.X += deltaX;
                node.Y += deltaY;
                node.UpdatePosition();
            }
        }
    }
}
