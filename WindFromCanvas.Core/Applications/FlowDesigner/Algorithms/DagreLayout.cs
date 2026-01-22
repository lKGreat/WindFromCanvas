using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Algorithms
{
    /// <summary>
    /// 5.3.1 布局算法接口
    /// </summary>
    public interface ILayoutAlgorithm
    {
        /// <summary>
        /// 算法名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 应用布局
        /// </summary>
        LayoutResult ApplyLayout(FlowGraph graph, LayoutOptions options);

        /// <summary>
        /// 异步应用布局
        /// </summary>
        Task<LayoutResult> ApplyLayoutAsync(FlowGraph graph, LayoutOptions options, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 布局选项
    /// </summary>
    public class LayoutOptions
    {
        /// <summary>
        /// 布局方向
        /// </summary>
        public LayoutDirection Direction { get; set; } = LayoutDirection.TopToBottom;

        /// <summary>
        /// 节点之间的水平间距
        /// </summary>
        public float NodeSeparation { get; set; } = 50f;

        /// <summary>
        /// 层级之间的垂直间距
        /// </summary>
        public float RankSeparation { get; set; } = 80f;

        /// <summary>
        /// 边缘标签的间距
        /// </summary>
        public float EdgeLabelSeparation { get; set; } = 10f;

        /// <summary>
        /// 是否对齐到网格
        /// </summary>
        public bool AlignToGrid { get; set; } = true;

        /// <summary>
        /// 网格大小
        /// </summary>
        public float GridSize { get; set; } = 10f;

        /// <summary>
        /// 布局边距
        /// </summary>
        public float Margin { get; set; } = 50f;

        /// <summary>
        /// 是否启用布局动画
        /// </summary>
        public bool Animate { get; set; } = true;

        /// <summary>
        /// 动画持续时间（毫秒）
        /// </summary>
        public int AnimationDuration { get; set; } = 300;

        /// <summary>
        /// 默认选项
        /// </summary>
        public static LayoutOptions Default => new LayoutOptions();
    }

    /// <summary>
    /// 布局方向
    /// </summary>
    public enum LayoutDirection
    {
        /// <summary>
        /// 从上到下
        /// </summary>
        TopToBottom,

        /// <summary>
        /// 从下到上
        /// </summary>
        BottomToTop,

        /// <summary>
        /// 从左到右
        /// </summary>
        LeftToRight,

        /// <summary>
        /// 从右到左
        /// </summary>
        RightToLeft
    }

    /// <summary>
    /// 布局结果
    /// </summary>
    public class LayoutResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 节点位置映射（节点ID -> 新位置）
        /// </summary>
        public Dictionary<string, PointF> NodePositions { get; set; } = new Dictionary<string, PointF>();

        /// <summary>
        /// 边缘路径映射（边缘ID -> 路径点列表）
        /// </summary>
        public Dictionary<string, List<PointF>> EdgePaths { get; set; } = new Dictionary<string, List<PointF>>();

        /// <summary>
        /// 布局边界框
        /// </summary>
        public RectangleF BoundingBox { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 布局耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// 5.3.2 Dagre层次布局算法实现
    /// 基于Sugiyama框架的层次图布局算法
    /// </summary>
    public class DagreLayout : ILayoutAlgorithm
    {
        public string Name => "Dagre";

        private class GraphNode
        {
            public string Id { get; set; }
            public ICanvasNode CanvasNode { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public int Rank { get; set; } = -1;
            public int Order { get; set; } = -1;
            public float X { get; set; }
            public float Y { get; set; }
            public List<string> Predecessors { get; } = new List<string>();
            public List<string> Successors { get; } = new List<string>();
            public bool IsVirtual { get; set; } = false;
        }

        private class GraphEdge
        {
            public string Id { get; set; }
            public string SourceId { get; set; }
            public string TargetId { get; set; }
            public List<PointF> Points { get; } = new List<PointF>();
        }

        /// <summary>
        /// 5.3.3 应用层次布局
        /// </summary>
        public LayoutResult ApplyLayout(FlowGraph graph, LayoutOptions options)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new LayoutResult { Success = true };

            try
            {
                if (graph == null || graph.Nodes.Count == 0)
                {
                    result.Success = true;
                    return result;
                }

                // 构建内部图表示
                var (nodes, edges) = BuildInternalGraph(graph);

                if (nodes.Count == 0)
                {
                    return result;
                }

                // Step 1: 分配层级（Rank Assignment）
                AssignRanks(nodes, edges);

                // Step 2: 添加虚拟节点处理长边
                AddVirtualNodes(nodes, edges);

                // Step 3: 排序节点以减少交叉（Crossing Reduction）
                ReduceCrossings(nodes, options);

                // Step 4: 分配坐标（Coordinate Assignment）
                AssignCoordinates(nodes, edges, options);

                // Step 5: 计算边缘路径
                ComputeEdgePaths(nodes, edges, options, result);

                // 计算边界框
                result.BoundingBox = ComputeBoundingBox(nodes, options);

                // 收集结果（排除虚拟节点）
                foreach (var node in nodes.Values.Where(n => !n.IsVirtual))
                {
                    result.NodePositions[node.Id] = new PointF(node.X, node.Y);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// 5.3.4 异步布局（支持取消）
        /// </summary>
        public Task<LayoutResult> ApplyLayoutAsync(FlowGraph graph, LayoutOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ApplyLayout(graph, options);
            }, cancellationToken);
        }

        /// <summary>
        /// 构建内部图表示
        /// </summary>
        private (Dictionary<string, GraphNode> nodes, List<GraphEdge> edges) BuildInternalGraph(FlowGraph graph)
        {
            var nodes = new Dictionary<string, GraphNode>();
            var edges = new List<GraphEdge>();

            // 添加节点
            foreach (var canvasNode in graph.Nodes)
            {
                var id = canvasNode.Id ?? Guid.NewGuid().ToString();
                nodes[id] = new GraphNode
                {
                    Id = id,
                    CanvasNode = canvasNode,
                    Width = canvasNode.Bounds.Width,
                    Height = canvasNode.Bounds.Height,
                    X = canvasNode.Bounds.X,
                    Y = canvasNode.Bounds.Y
                };
            }

            // 添加边
            foreach (var canvasEdge in graph.Edges)
            {
                var sourceId = canvasEdge.SourceNodeId;
                var targetId = canvasEdge.TargetNodeId;

                if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId) &&
                    nodes.ContainsKey(sourceId) && nodes.ContainsKey(targetId))
                {
                    edges.Add(new GraphEdge
                    {
                        Id = canvasEdge.Id ?? Guid.NewGuid().ToString(),
                        SourceId = sourceId,
                        TargetId = targetId
                    });

                    nodes[sourceId].Successors.Add(targetId);
                    nodes[targetId].Predecessors.Add(sourceId);
                }
            }

            return (nodes, edges);
        }

        /// <summary>
        /// 分配层级（使用最长路径算法）
        /// </summary>
        private void AssignRanks(Dictionary<string, GraphNode> nodes, List<GraphEdge> edges)
        {
            // 找到所有入度为0的节点作为起始节点
            var startNodes = nodes.Values.Where(n => n.Predecessors.Count == 0).ToList();

            if (startNodes.Count == 0 && nodes.Count > 0)
            {
                // 如果没有入度为0的节点（可能有环），选择任意节点
                startNodes.Add(nodes.Values.First());
            }

            // BFS分配层级
            var visited = new HashSet<string>();
            var queue = new Queue<string>();

            foreach (var startNode in startNodes)
            {
                startNode.Rank = 0;
                queue.Enqueue(startNode.Id);
                visited.Add(startNode.Id);
            }

            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                var node = nodes[nodeId];

                foreach (var successorId in node.Successors)
                {
                    if (!nodes.ContainsKey(successorId)) continue;

                    var successor = nodes[successorId];
                    var newRank = node.Rank + 1;

                    if (successor.Rank < newRank)
                    {
                        successor.Rank = newRank;
                    }

                    if (!visited.Contains(successorId))
                    {
                        visited.Add(successorId);
                        queue.Enqueue(successorId);
                    }
                }
            }

            // 处理未访问的节点（断开的组件）
            foreach (var node in nodes.Values)
            {
                if (node.Rank < 0)
                {
                    node.Rank = 0;
                }
            }
        }

        /// <summary>
        /// 添加虚拟节点处理跨多层的边
        /// </summary>
        private void AddVirtualNodes(Dictionary<string, GraphNode> nodes, List<GraphEdge> edges)
        {
            var newEdges = new List<GraphEdge>();
            var edgesToRemove = new List<GraphEdge>();

            foreach (var edge in edges)
            {
                if (!nodes.ContainsKey(edge.SourceId) || !nodes.ContainsKey(edge.TargetId))
                    continue;

                var source = nodes[edge.SourceId];
                var target = nodes[edge.TargetId];

                var rankDiff = target.Rank - source.Rank;

                if (rankDiff > 1)
                {
                    // 需要添加虚拟节点
                    edgesToRemove.Add(edge);
                    var prevNodeId = edge.SourceId;

                    for (int i = 1; i < rankDiff; i++)
                    {
                        var virtualId = $"virtual_{edge.Id}_{i}";
                        var virtualNode = new GraphNode
                        {
                            Id = virtualId,
                            Width = 0,
                            Height = 0,
                            Rank = source.Rank + i,
                            IsVirtual = true
                        };
                        virtualNode.Predecessors.Add(prevNodeId);
                        nodes[virtualId] = virtualNode;

                        nodes[prevNodeId].Successors.Remove(edge.TargetId);
                        nodes[prevNodeId].Successors.Add(virtualId);

                        newEdges.Add(new GraphEdge
                        {
                            Id = $"{edge.Id}_seg_{i}",
                            SourceId = prevNodeId,
                            TargetId = virtualId
                        });

                        prevNodeId = virtualId;
                    }

                    // 最后一段边
                    nodes[prevNodeId].Successors.Add(edge.TargetId);
                    target.Predecessors.Remove(edge.SourceId);
                    target.Predecessors.Add(prevNodeId);

                    newEdges.Add(new GraphEdge
                    {
                        Id = $"{edge.Id}_seg_{rankDiff}",
                        SourceId = prevNodeId,
                        TargetId = edge.TargetId
                    });
                }
            }

            foreach (var edge in edgesToRemove)
            {
                edges.Remove(edge);
            }
            edges.AddRange(newEdges);
        }

        /// <summary>
        /// 减少边交叉（使用重心法）
        /// </summary>
        private void ReduceCrossings(Dictionary<string, GraphNode> nodes, LayoutOptions options)
        {
            // 按层级分组
            var ranks = nodes.Values
                .GroupBy(n => n.Rank)
                .OrderBy(g => g.Key)
                .Select(g => g.ToList())
                .ToList();

            // 初始化顺序
            for (int r = 0; r < ranks.Count; r++)
            {
                for (int i = 0; i < ranks[r].Count; i++)
                {
                    ranks[r][i].Order = i;
                }
            }

            // 迭代优化（重心法）
            const int maxIterations = 24;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                bool improved = false;

                // 从上到下
                for (int r = 1; r < ranks.Count; r++)
                {
                    if (ReorderRankByBarycenter(ranks[r], nodes, true))
                        improved = true;
                }

                // 从下到上
                for (int r = ranks.Count - 2; r >= 0; r--)
                {
                    if (ReorderRankByBarycenter(ranks[r], nodes, false))
                        improved = true;
                }

                if (!improved) break;
            }
        }

        /// <summary>
        /// 使用重心法重新排序一层的节点
        /// </summary>
        private bool ReorderRankByBarycenter(List<GraphNode> rank, Dictionary<string, GraphNode> nodes, bool useAbove)
        {
            var barycenters = new Dictionary<string, float>();

            foreach (var node in rank)
            {
                var neighbors = useAbove ? node.Predecessors : node.Successors;
                if (neighbors.Count > 0)
                {
                    float sum = 0;
                    int count = 0;
                    foreach (var neighborId in neighbors)
                    {
                        if (nodes.TryGetValue(neighborId, out var neighbor))
                        {
                            sum += neighbor.Order;
                            count++;
                        }
                    }
                    barycenters[node.Id] = count > 0 ? sum / count : node.Order;
                }
                else
                {
                    barycenters[node.Id] = node.Order;
                }
            }

            // 按重心排序
            var oldOrder = rank.Select(n => n.Id).ToList();
            rank.Sort((a, b) => barycenters[a.Id].CompareTo(barycenters[b.Id]));

            // 更新顺序
            for (int i = 0; i < rank.Count; i++)
            {
                rank[i].Order = i;
            }

            // 检查是否有变化
            var newOrder = rank.Select(n => n.Id).ToList();
            return !oldOrder.SequenceEqual(newOrder);
        }

        /// <summary>
        /// 分配坐标
        /// </summary>
        private void AssignCoordinates(Dictionary<string, GraphNode> nodes, List<GraphEdge> edges, LayoutOptions options)
        {
            bool isVertical = options.Direction == LayoutDirection.TopToBottom ||
                             options.Direction == LayoutDirection.BottomToTop;

            // 按层级分组
            var ranks = nodes.Values
                .GroupBy(n => n.Rank)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(n => n.Order).ToList())
                .ToList();

            // 计算每层的起始位置
            float currentRankPos = options.Margin;
            float maxCrossExtent = 0;

            // 第一遍：计算每层的主轴位置和交叉轴范围
            for (int r = 0; r < ranks.Count; r++)
            {
                var rank = ranks[r];
                float maxNodeExtent = 0;
                float currentCrossPos = options.Margin;

                foreach (var node in rank)
                {
                    float nodeMainExtent = isVertical ? node.Height : node.Width;
                    float nodeCrossExtent = isVertical ? node.Width : node.Height;

                    maxNodeExtent = Math.Max(maxNodeExtent, nodeMainExtent);

                    if (isVertical)
                    {
                        node.X = currentCrossPos;
                        node.Y = currentRankPos;
                    }
                    else
                    {
                        node.X = currentRankPos;
                        node.Y = currentCrossPos;
                    }

                    currentCrossPos += nodeCrossExtent + options.NodeSeparation;
                }

                maxCrossExtent = Math.Max(maxCrossExtent, currentCrossPos - options.NodeSeparation);
                currentRankPos += maxNodeExtent + options.RankSeparation;
            }

            // 居中对齐每层
            foreach (var rank in ranks)
            {
                float rankCrossExtent = 0;
                foreach (var node in rank)
                {
                    float nodeCrossExtent = isVertical ? node.Width : node.Height;
                    rankCrossExtent += nodeCrossExtent;
                }
                rankCrossExtent += (rank.Count - 1) * options.NodeSeparation;

                float offset = (maxCrossExtent - rankCrossExtent) / 2;
                foreach (var node in rank)
                {
                    if (isVertical)
                    {
                        node.X += offset;
                    }
                    else
                    {
                        node.Y += offset;
                    }
                }
            }

            // 处理反向布局
            if (options.Direction == LayoutDirection.BottomToTop ||
                options.Direction == LayoutDirection.RightToLeft)
            {
                float maxPos = currentRankPos;
                foreach (var node in nodes.Values)
                {
                    if (isVertical)
                    {
                        node.Y = maxPos - node.Y - node.Height;
                    }
                    else
                    {
                        node.X = maxPos - node.X - node.Width;
                    }
                }
            }

            // 对齐到网格
            if (options.AlignToGrid)
            {
                foreach (var node in nodes.Values)
                {
                    node.X = (float)Math.Round(node.X / options.GridSize) * options.GridSize;
                    node.Y = (float)Math.Round(node.Y / options.GridSize) * options.GridSize;
                }
            }
        }

        /// <summary>
        /// 计算边缘路径
        /// </summary>
        private void ComputeEdgePaths(Dictionary<string, GraphNode> nodes, List<GraphEdge> edges, LayoutOptions options, LayoutResult result)
        {
            // 合并虚拟节点的路径
            var virtualPaths = new Dictionary<string, List<PointF>>();

            foreach (var edge in edges)
            {
                if (!nodes.ContainsKey(edge.SourceId) || !nodes.ContainsKey(edge.TargetId))
                    continue;

                var source = nodes[edge.SourceId];
                var target = nodes[edge.TargetId];

                var sourceCenter = new PointF(
                    source.X + source.Width / 2,
                    source.Y + source.Height / 2
                );
                var targetCenter = new PointF(
                    target.X + target.Width / 2,
                    target.Y + target.Height / 2
                );

                // 检查是否是虚拟边的一部分
                var baseEdgeId = edge.Id.Contains("_seg_") ?
                    edge.Id.Substring(0, edge.Id.LastIndexOf("_seg_")) :
                    edge.Id;

                if (!virtualPaths.ContainsKey(baseEdgeId))
                {
                    virtualPaths[baseEdgeId] = new List<PointF>();
                }

                if (virtualPaths[baseEdgeId].Count == 0)
                {
                    virtualPaths[baseEdgeId].Add(sourceCenter);
                }

                if (target.IsVirtual)
                {
                    virtualPaths[baseEdgeId].Add(targetCenter);
                }
                else
                {
                    virtualPaths[baseEdgeId].Add(targetCenter);
                }
            }

            // 将路径添加到结果
            foreach (var kvp in virtualPaths)
            {
                if (kvp.Value.Count >= 2)
                {
                    result.EdgePaths[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// 计算边界框
        /// </summary>
        private RectangleF ComputeBoundingBox(Dictionary<string, GraphNode> nodes, LayoutOptions options)
        {
            if (nodes.Count == 0)
                return RectangleF.Empty;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var node in nodes.Values.Where(n => !n.IsVirtual))
            {
                minX = Math.Min(minX, node.X);
                minY = Math.Min(minY, node.Y);
                maxX = Math.Max(maxX, node.X + node.Width);
                maxY = Math.Max(maxY, node.Y + node.Height);
            }

            return new RectangleF(
                minX - options.Margin,
                minY - options.Margin,
                maxX - minX + 2 * options.Margin,
                maxY - minY + 2 * options.Margin
            );
        }
    }

    /// <summary>
    /// 5.3.5 布局配置工厂
    /// </summary>
    public static class LayoutOptionsFactory
    {
        /// <summary>
        /// 紧凑布局
        /// </summary>
        public static LayoutOptions Compact => new LayoutOptions
        {
            NodeSeparation = 30f,
            RankSeparation = 50f,
            Margin = 20f
        };

        /// <summary>
        /// 宽松布局
        /// </summary>
        public static LayoutOptions Spacious => new LayoutOptions
        {
            NodeSeparation = 80f,
            RankSeparation = 120f,
            Margin = 80f
        };

        /// <summary>
        /// 水平布局
        /// </summary>
        public static LayoutOptions Horizontal => new LayoutOptions
        {
            Direction = LayoutDirection.LeftToRight,
            NodeSeparation = 60f,
            RankSeparation = 100f
        };

        /// <summary>
        /// 垂直布局
        /// </summary>
        public static LayoutOptions Vertical => new LayoutOptions
        {
            Direction = LayoutDirection.TopToBottom,
            NodeSeparation = 50f,
            RankSeparation = 80f
        };
    }
}
