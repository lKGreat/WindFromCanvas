using System;
using System.Collections.Generic;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout
{
    /// <summary>
    /// 流程图构建器（匹配 Activepieces flow-canvas-utils.ts）
    /// </summary>
    public class FlowGraphBuilder
    {
        /// <summary>
        /// 构建流程图
        /// </summary>
        public FlowGraph BuildGraph(Core.Models.FlowVersion version)
        {
            var graph = new FlowGraph();
            
            // 构建步骤图
            var stepsGraph = BuildFlowGraphRecursive(version.Trigger, PointF.Empty);
            graph.Merge(stepsGraph);

            // 添加备注节点
            AddNotes(version.Notes, graph);

            return graph;
        }

        /// <summary>
        /// 递归构建流程图
        /// </summary>
        private FlowGraph BuildFlowGraphRecursive(IStep step, PointF offset)
        {
            if (step == null)
            {
                return new FlowGraph();
            }

            var graph = new FlowGraph();

            // 1. 创建步骤节点
            var stepNode = CreateStepNode(step, offset);
            graph.Nodes.Add(stepNode);

            // 2. 处理子图（循环或路由）
            FlowGraph childGraph = null;
            if (step is LoopOnItemsAction loop)
            {
                childGraph = BuildLoopChildGraph(loop, offset);
            }
            else if (step is RouterAction router)
            {
                childGraph = BuildRouterChildGraph(router, offset);
            }

            if (childGraph != null)
            {
                graph.Merge(childGraph);
            }

            // 3. 创建结束节点和边缘
            var graphHeight = CalculateGraphHeight(graph);
            var endNode = CreateGraphEndNode(step.Name, offset, graphHeight);
            graph.Nodes.Add(endNode);

            // 创建到结束节点的边缘
            if (step is FlowAction action && action.NextAction != null)
            {
                var edge = CreateStraightLineEdge(step.Name, endNode.Id, true);
                graph.Edges.Add(edge);
            }
            else if (step is FlowTrigger trigger && trigger.NextAction != null)
            {
                var edge = CreateStraightLineEdge(step.Name, endNode.Id, true);
                graph.Edges.Add(edge);
            }
            else
            {
                var edge = CreateStraightLineEdge(step.Name, endNode.Id, false);
                graph.Edges.Add(edge);
            }

            // 4. 递归处理下一个动作
            FlowAction nextAction = null;
            if (step is FlowAction action2)
            {
                nextAction = action2.NextAction;
            }
            else if (step is FlowTrigger trigger2)
            {
                nextAction = trigger2.NextAction;
            }

            if (nextAction != null)
            {
                var nextOffset = CalculateNextOffset(graph, offset);
                var nextGraph = BuildFlowGraphRecursive(nextAction, nextOffset);
                graph.Merge(nextGraph);
            }

            return graph;
        }

        /// <summary>
        /// 创建步骤节点
        /// </summary>
        private ICanvasNode CreateStepNode(IStep step, PointF offset)
        {
            var node = new StepNode(step);
            node.Position = offset;
            return node;
        }

        /// <summary>
        /// 创建图结束节点
        /// </summary>
        private ICanvasNode CreateGraphEndNode(string parentStepName, PointF offset, float graphHeight)
        {
            var endNode = new GraphEndNode($"{parentStepName}-subgraph-end");
            endNode.Position = new PointF(
                offset.X + LayoutConstants.NodeSize.STEP.Width / 2,
                offset.Y + graphHeight
            );
            return endNode;
        }

        /// <summary>
        /// 创建直线边缘
        /// </summary>
        private ICanvasEdge CreateStraightLineEdge(string sourceId, string targetId, bool drawArrowHead)
        {
            return new StraightLineEdge($"{sourceId}-{targetId}-edge")
            {
                SourceId = sourceId,
                TargetId = targetId,
                DrawArrowHead = drawArrowHead
            };
        }

        /// <summary>
        /// 计算图的高度
        /// </summary>
        private float CalculateGraphHeight(FlowGraph graph)
        {
            if (graph.Nodes.Count == 0)
            {
                return LayoutConstants.NodeSize.STEP.Height + LayoutConstants.VERTICAL_SPACE_BETWEEN_STEPS;
            }

            var bbox = graph.CalculateBoundingBox();
            return bbox.Height;
        }

        /// <summary>
        /// 计算下一个节点的偏移
        /// </summary>
        private PointF CalculateNextOffset(FlowGraph graph, PointF currentOffset)
        {
            var bbox = graph.CalculateBoundingBox();
            return new PointF(
                currentOffset.X,
                currentOffset.Y + bbox.Height
            );
        }

        /// <summary>
        /// 构建循环子图
        /// </summary>
        private FlowGraph BuildLoopChildGraph(LoopOnItemsAction loop, PointF parentOffset)
        {
            var graph = new FlowGraph();
            string firstChildNodeId = null;
            string childEndNodeId = null;

            if (loop.FirstLoopAction != null)
            {
                // 有子动作，递归构建
                var childOffset = new PointF(
                    parentOffset.X + LayoutConstants.NodeSize.STEP.Width + LayoutConstants.HORIZONTAL_SPACE_BETWEEN_NODES,
                    parentOffset.Y + LayoutConstants.NodeSize.STEP.Height + LayoutConstants.VERTICAL_OFFSET_BETWEEN_LOOP_AND_CHILD
                );
                var childGraph = BuildFlowGraphRecursive(loop.FirstLoopAction, childOffset);
                graph.Merge(childGraph);
                
                // 找到第一个子节点ID（应该是步骤节点）
                firstChildNodeId = loop.FirstLoopAction.Name;
                // 找到子图的结束节点ID
                childEndNodeId = $"{loop.FirstLoopAction.Name}-subgraph-end";
            }
            else
            {
                // 空循环，创建大添加按钮
                var bigAddButton = new BigAddButtonNode($"{loop.Name}-big-add-button-{loop.Name}-loop-start-edge");
                bigAddButton.Position = new PointF(
                    parentOffset.X + LayoutConstants.NodeSize.STEP.Width + LayoutConstants.HORIZONTAL_SPACE_BETWEEN_NODES,
                    parentOffset.Y + LayoutConstants.NodeSize.STEP.Height + LayoutConstants.VERTICAL_OFFSET_BETWEEN_LOOP_AND_CHILD
                );
                bigAddButton.ParentStepName = loop.Name;
                bigAddButton.StepLocationRelativeToParent = StepLocationRelativeToParent.INSIDE_LOOP;
                graph.Nodes.Add(bigAddButton);
                firstChildNodeId = bigAddButton.Id;
            }

            // 添加循环开始边缘：从循环节点到第一个子节点
            if (firstChildNodeId != null)
            {
                var loopStartEdge = new LoopStartEdge($"{loop.Name}-loop-start-edge")
                {
                    SourceId = loop.Name,
                    TargetId = firstChildNodeId,
                    IsLoopEmpty = loop.FirstLoopAction == null
                };
                graph.Edges.Add(loopStartEdge);
            }

            // 添加循环返回边缘：从子图结束返回到循环节点
            if (childEndNodeId != null)
            {
                var loopReturnEdge = new LoopReturnEdge($"{loop.Name}-loop-return-edge")
                {
                    SourceId = childEndNodeId,
                    TargetId = loop.Name,
                    ParentStepName = loop.Name,
                    IsLoopEmpty = false,
                    DrawArrowHeadAfterEnd = true,
                    VerticalSpaceBetweenReturnNodeStartAndEnd = LayoutConstants.VERTICAL_SPACE_BETWEEN_STEPS
                };
                graph.Edges.Add(loopReturnEdge);
            }

            return graph;
        }

        /// <summary>
        /// 构建路由子图
        /// </summary>
        private FlowGraph BuildRouterChildGraph(RouterAction router, PointF parentOffset)
        {
            var graph = new FlowGraph();
            var childGraphs = new List<FlowGraph>();
            var branchStartNodeIds = new List<string>();
            var branchEndNodeIds = new List<string>();

            // 为每个分支构建子图
            for (int i = 0; i < router.Children.Count; i++)
            {
                var child = router.Children[i];
                var branchOffset = new PointF(
                    parentOffset.X + i * (LayoutConstants.NodeSize.STEP.Width + LayoutConstants.HORIZONTAL_SPACE_BETWEEN_NODES),
                    parentOffset.Y + LayoutConstants.NodeSize.STEP.Height + LayoutConstants.VERTICAL_OFFSET_BETWEEN_ROUTER_AND_CHILD
                );

                if (child != null)
                {
                    var childGraph = BuildFlowGraphRecursive(child, branchOffset);
                    childGraphs.Add(childGraph);
                    branchStartNodeIds.Add(child.Name);
                    branchEndNodeIds.Add($"{child.Name}-subgraph-end");
                }
                else
                {
                    // 空分支，创建大添加按钮
                    var bigAddButton = new BigAddButtonNode($"{router.Name}-big-add-button-{router.Name}-branch-{i}-start-edge");
                    bigAddButton.Position = branchOffset;
                    bigAddButton.ParentStepName = router.Name;
                    bigAddButton.StepLocationRelativeToParent = StepLocationRelativeToParent.INSIDE_BRANCH;
                    bigAddButton.BranchIndex = i;
                    var emptyGraph = new FlowGraph();
                    emptyGraph.Nodes.Add(bigAddButton);
                    childGraphs.Add(emptyGraph);
                    branchStartNodeIds.Add(bigAddButton.Id);
                    branchEndNodeIds.Add(bigAddButton.Id); // 空分支的结束节点就是开始节点
                }
            }

            // 合并所有子图
            foreach (var childGraph in childGraphs)
            {
                graph.Merge(childGraph);
            }

            // 添加路由开始边缘：从路由节点到每个分支的第一个节点
            var branches = router.Settings?.Branches ?? new List<Core.Models.RouterBranch>();
            for (int i = 0; i < branchStartNodeIds.Count; i++)
            {
                var branchStartNodeId = branchStartNodeIds[i];
                var branch = i < branches.Count ? branches[i] : null;
                var label = branch != null && !string.IsNullOrEmpty(branch.BranchName) 
                    ? branch.BranchName 
                    : $"Branch {i + 1}";
                
                var routerStartEdge = new RouterStartEdge($"{router.Name}-router-start-edge-{i}")
                {
                    SourceId = router.Name,
                    TargetId = branchStartNodeId,
                    IsBranchEmpty = router.Children[i] == null,
                    Label = label,
                    DrawHorizontalLine = true,
                    DrawStartingVerticalLine = i == 0, // 第一个分支绘制起始垂直线
                    BranchIndex = i
                };
                graph.Edges.Add(routerStartEdge);
            }

            // 添加路由结束边缘：从每个分支的结束节点到路由结束节点
            // 路由结束节点是路由节点的 GraphEndNode
            var routerEndNodeId = $"{router.Name}-subgraph-end";
            
            for (int i = 0; i < branchEndNodeIds.Count; i++)
            {
                var branchEndNodeId = branchEndNodeIds[i];
                var isLastBranch = i == branchEndNodeIds.Count - 1;
                
                var routerEndEdge = new RouterEndEdge($"{router.Name}-router-end-edge-{i}")
                {
                    SourceId = branchEndNodeId,
                    TargetId = routerEndNodeId,
                    DrawHorizontalLine = true,
                    DrawEndingVerticalLine = isLastBranch, // 最后一个分支绘制结束垂直线
                    VerticalSpaceBetweenLastNodeInBranchAndEndLine = LayoutConstants.VERTICAL_SPACE_BETWEEN_STEPS,
                    RouterOrBranchStepName = router.Name,
                    IsNextStepEmpty = router.NextAction == null
                };
                graph.Edges.Add(routerEndEdge);
            }

            return graph;
        }

        /// <summary>
        /// 添加备注节点
        /// </summary>
        private void AddNotes(List<Note> notes, FlowGraph graph)
        {
            foreach (var note in notes)
            {
                var noteNode = new NoteNode(note.Id)
                {
                    Position = note.Position,
                    Content = note.Content,
                    Color = note.Color
                };
                noteNode.SetSize(note.Size);
                graph.Nodes.Add(noteNode);
            }
        }
    }
}
