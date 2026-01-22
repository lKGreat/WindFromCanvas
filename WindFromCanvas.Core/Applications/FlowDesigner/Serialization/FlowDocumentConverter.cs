using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Serialization
{
    /// <summary>
    /// FlowDocument 和 FlowVersion 之间的双向转换器
    /// </summary>
    public class FlowDocumentConverter
    {
        #region Document -> Version 转换

        /// <summary>
        /// 将 FlowDocument 转换为 FlowVersion
        /// </summary>
        public FlowVersion ConvertToVersion(FlowDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var version = new FlowVersion
            {
                Id = document.Id ?? Guid.NewGuid().ToString(),
                FlowId = document.Id ?? Guid.NewGuid().ToString(),
                DisplayName = document.DisplayName ?? "新流程",
                SchemaVersion = document.Version ?? "1.0",
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.ModifiedAt,
                Valid = true,
                State = FlowVersionState.DRAFT,
                Notes = new List<Note>()
            };

            // 构建节点字典用于快速查找
            var nodeDict = document.Nodes?.ToDictionary(n => n.Name) ?? new Dictionary<string, FlowNodeData>();
            
            // 构建连接关系图
            var connectionMap = BuildConnectionMap(document.Connections);

            // 查找开始节点作为触发器
            var startNode = document.Nodes?.FirstOrDefault(n => n.Type == FlowNodeType.Start);
            if (startNode != null)
            {
                version.Trigger = ConvertToTrigger(startNode, nodeDict, connectionMap);
            }
            else
            {
                version.Trigger = new EmptyTrigger();
            }

            return version;
        }

        /// <summary>
        /// 构建连接映射（源节点 -> 目标节点列表）
        /// </summary>
        private Dictionary<string, List<FlowConnectionData>> BuildConnectionMap(List<FlowConnectionData> connections)
        {
            var map = new Dictionary<string, List<FlowConnectionData>>();
            if (connections == null) return map;

            foreach (var conn in connections)
            {
                if (!map.ContainsKey(conn.SourceNode))
                {
                    map[conn.SourceNode] = new List<FlowConnectionData>();
                }
                map[conn.SourceNode].Add(conn);
            }

            return map;
        }

        /// <summary>
        /// 将开始节点转换为触发器
        /// </summary>
        private FlowTrigger ConvertToTrigger(
            FlowNodeData startNode,
            Dictionary<string, FlowNodeData> nodeDict,
            Dictionary<string, List<FlowConnectionData>> connectionMap)
        {
            var trigger = new EmptyTrigger
            {
                Name = startNode.Name,
                DisplayName = startNode.DisplayName ?? "触发器",
                Valid = startNode.Valid,
                Skip = startNode.Skip
            };

            // 查找触发器的下一个动作
            if (connectionMap.TryGetValue(startNode.Name, out var connections) && connections.Count > 0)
            {
                var nextNodeName = connections[0].TargetNode;
                if (nodeDict.TryGetValue(nextNodeName, out var nextNode))
                {
                    trigger.NextAction = ConvertToAction(nextNode, nodeDict, connectionMap, new HashSet<string>());
                }
            }

            return trigger;
        }

        /// <summary>
        /// 将节点转换为动作（递归）
        /// </summary>
        private FlowAction ConvertToAction(
            FlowNodeData node,
            Dictionary<string, FlowNodeData> nodeDict,
            Dictionary<string, List<FlowConnectionData>> connectionMap,
            HashSet<string> visited)
        {
            if (node == null || visited.Contains(node.Name))
                return null;

            // 防止循环引用
            visited.Add(node.Name);

            FlowAction action = null;

            switch (node.Type)
            {
                case FlowNodeType.End:
                    // 结束节点不转换为动作
                    return null;

                case FlowNodeType.Code:
                    action = ConvertToCodeAction(node);
                    break;

                case FlowNodeType.Piece:
                case FlowNodeType.Process:
                    action = ConvertToPieceAction(node);
                    break;

                case FlowNodeType.Decision:
                    action = ConvertToRouterAction(node, nodeDict, connectionMap, visited);
                    break;

                case FlowNodeType.Loop:
                    action = ConvertToLoopAction(node, nodeDict, connectionMap, visited);
                    break;

                default:
                    action = ConvertToPieceAction(node);
                    break;
            }

            if (action == null)
                return null;

            // 设置下一个动作（除了路由和循环，它们有特殊的子动作处理）
            if (node.Type != FlowNodeType.Decision && node.Type != FlowNodeType.Loop)
            {
                if (connectionMap.TryGetValue(node.Name, out var connections) && connections.Count > 0)
                {
                    var nextNodeName = connections[0].TargetNode;
                    if (nodeDict.TryGetValue(nextNodeName, out var nextNode))
                    {
                        action.NextAction = ConvertToAction(nextNode, nodeDict, connectionMap, new HashSet<string>(visited));
                    }
                }
            }

            return action;
        }

        /// <summary>
        /// 转换为代码动作
        /// </summary>
        private CodeAction ConvertToCodeAction(FlowNodeData node)
        {
            var action = new CodeAction
            {
                Name = node.Name,
                DisplayName = node.DisplayName,
                Valid = node.Valid,
                Skip = node.Skip,
                Settings = new CodeActionSettings
                {
                    SourceCode = new SourceCode
                    {
                        Code = node.Properties?.ContainsKey("code") == true 
                            ? node.Properties["code"]?.ToString() 
                            : "",
                        PackageJson = "{}"
                    }
                }
            };

            // 复制输入参数
            if (node.Settings != null)
            {
                action.Settings.Input = new Dictionary<string, object>(node.Settings);
            }

            return action;
        }

        /// <summary>
        /// 转换为组件动作
        /// </summary>
        private PieceAction ConvertToPieceAction(FlowNodeData node)
        {
            var action = new PieceAction
            {
                Name = node.Name,
                DisplayName = node.DisplayName,
                Valid = node.Valid,
                Skip = node.Skip,
                Settings = new PieceActionSettings
                {
                    PieceName = node.Properties?.ContainsKey("pieceName") == true
                        ? node.Properties["pieceName"]?.ToString()
                        : "unknown",
                    PieceVersion = "1.0.0",
                    ActionName = node.Properties?.ContainsKey("actionName") == true
                        ? node.Properties["actionName"]?.ToString()
                        : "execute"
                }
            };

            // 复制输入参数
            if (node.Settings != null)
            {
                action.Settings.Input = new Dictionary<string, object>(node.Settings);
            }

            return action;
        }

        /// <summary>
        /// 转换为路由动作
        /// </summary>
        private RouterAction ConvertToRouterAction(
            FlowNodeData node,
            Dictionary<string, FlowNodeData> nodeDict,
            Dictionary<string, List<FlowConnectionData>> connectionMap,
            HashSet<string> visited)
        {
            var action = new RouterAction
            {
                Name = node.Name,
                DisplayName = node.DisplayName,
                Valid = node.Valid,
                Skip = node.Skip,
                Settings = new RouterActionSettings
                {
                    ExecutionType = RouterExecutionType.EXECUTE_FIRST_MATCH,
                    Branches = new List<RouterBranch>()
                },
                Children = new List<FlowAction>()
            };

            // 获取所有分支连接
            if (connectionMap.TryGetValue(node.Name, out var connections))
            {
                int branchIndex = 0;
                foreach (var conn in connections)
                {
                    var branch = new RouterBranch
                    {
                        BranchName = !string.IsNullOrEmpty(conn.Label) 
                            ? conn.Label 
                            : $"分支 {branchIndex + 1}",
                        BranchType = branchIndex == connections.Count - 1 
                            ? BranchExecutionType.FALLBACK 
                            : BranchExecutionType.CONDITION,
                        Conditions = new List<List<BranchCondition>>()
                    };

                    // 添加条件
                    if (node.Properties?.ContainsKey($"condition_{branchIndex}") == true)
                    {
                        branch.Conditions.Add(new List<BranchCondition>
                        {
                            new BranchCondition
                            {
                                FirstValue = node.Properties[$"condition_{branchIndex}"]?.ToString(),
                                Operator = BranchOperator.TEXT_EXACTLY_MATCHES,
                                SecondValue = "true"
                            }
                        });
                    }

                    action.Settings.Branches.Add(branch);

                    // 转换分支的下一个动作
                    if (nodeDict.TryGetValue(conn.TargetNode, out var targetNode))
                    {
                        var childAction = ConvertToAction(targetNode, nodeDict, connectionMap, new HashSet<string>(visited));
                        action.Children.Add(childAction);
                    }

                    branchIndex++;
                }
            }

            return action;
        }

        /// <summary>
        /// 转换为循环动作
        /// </summary>
        private LoopOnItemsAction ConvertToLoopAction(
            FlowNodeData node,
            Dictionary<string, FlowNodeData> nodeDict,
            Dictionary<string, List<FlowConnectionData>> connectionMap,
            HashSet<string> visited)
        {
            var action = new LoopOnItemsAction
            {
                Name = node.Name,
                DisplayName = node.DisplayName,
                Valid = node.Valid,
                Skip = node.Skip,
                Settings = new LoopOnItemsActionSettings
                {
                    Items = node.Properties?.ContainsKey("items") == true
                        ? node.Properties["items"]?.ToString()
                        : "[]"
                }
            };

            // 查找循环体内的第一个动作
            if (connectionMap.TryGetValue(node.Name, out var connections))
            {
                // 假设第一个连接是循环体内的动作
                var loopBodyConnection = connections.FirstOrDefault(c => c.Label == "loop" || c.Type == FlowConnectionType.LoopStartEdge);
                if (loopBodyConnection == null && connections.Count > 0)
                {
                    loopBodyConnection = connections[0];
                }

                if (loopBodyConnection != null && nodeDict.TryGetValue(loopBodyConnection.TargetNode, out var firstLoopNode))
                {
                    action.FirstLoopAction = ConvertToAction(firstLoopNode, nodeDict, connectionMap, new HashSet<string>(visited));
                }

                // 查找循环结束后的下一个动作
                var nextConnection = connections.FirstOrDefault(c => c.Label == "next" || c.Type != FlowConnectionType.LoopStartEdge);
                if (nextConnection != null && nextConnection != loopBodyConnection && nodeDict.TryGetValue(nextConnection.TargetNode, out var nextNode))
                {
                    action.NextAction = ConvertToAction(nextNode, nodeDict, connectionMap, new HashSet<string>(visited));
                }
            }

            return action;
        }

        #endregion

        #region Version -> Document 转换

        /// <summary>
        /// 将 FlowVersion 转换为 FlowDocument
        /// </summary>
        public FlowDocument ConvertToDocument(FlowVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            var document = new FlowDocument
            {
                Id = version.Id,
                DisplayName = version.DisplayName,
                Version = version.SchemaVersion,
                CreatedAt = version.CreatedAt,
                ModifiedAt = version.UpdatedAt,
                Nodes = new List<FlowNodeData>(),
                Connections = new List<FlowConnectionData>()
            };

            // 用于计算节点位置
            float currentY = 100;
            const float nodeSpacingY = 120;
            const float centerX = 400;

            // 转换触发器为开始节点
            if (version.Trigger != null)
            {
                var startNode = ConvertTriggerToNode(version.Trigger, centerX, currentY);
                document.Nodes.Add(startNode);
                currentY += nodeSpacingY;

                // 递归转换动作链
                if (version.Trigger.NextAction != null)
                {
                    ConvertActionToNodes(version.Trigger.NextAction, document, startNode.Name, 
                        centerX, ref currentY, nodeSpacingY);
                }
            }

            // 添加结束节点（如果流程有内容但没有结束节点）
            if (document.Nodes.Count > 0 && !document.Nodes.Any(n => n.Type == FlowNodeType.End))
            {
                var endNode = new FlowNodeData
                {
                    Name = "end_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    DisplayName = "结束",
                    Type = FlowNodeType.End,
                    Position = new PointF(centerX, currentY),
                    Valid = true
                };
                document.Nodes.Add(endNode);

                // 连接最后一个节点到结束节点
                var lastNode = document.Nodes[document.Nodes.Count - 2];
                if (lastNode.Type != FlowNodeType.Decision && lastNode.Type != FlowNodeType.Loop)
                {
                    document.Connections.Add(FlowConnectionData.Create(lastNode.Name, endNode.Name));
                }
            }

            return document;
        }

        /// <summary>
        /// 将触发器转换为开始节点
        /// </summary>
        private FlowNodeData ConvertTriggerToNode(FlowTrigger trigger, float x, float y)
        {
            return new FlowNodeData
            {
                Name = trigger.Name ?? "trigger",
                DisplayName = trigger.DisplayName ?? "触发器",
                Type = FlowNodeType.Start,
                Position = new PointF(x, y),
                Valid = trigger.Valid,
                Skip = trigger.Skip
            };
        }

        /// <summary>
        /// 递归将动作转换为节点
        /// </summary>
        private void ConvertActionToNodes(
            FlowAction action,
            FlowDocument document,
            string previousNodeName,
            float x,
            ref float y,
            float spacingY)
        {
            if (action == null)
                return;

            // 检查是否已经处理过该节点（防止循环）
            if (document.Nodes.Any(n => n.Name == action.Name))
                return;

            FlowNodeData node = null;

            switch (action)
            {
                case CodeAction codeAction:
                    node = ConvertCodeActionToNode(codeAction, x, y);
                    break;

                case PieceAction pieceAction:
                    node = ConvertPieceActionToNode(pieceAction, x, y);
                    break;

                case RouterAction routerAction:
                    node = ConvertRouterActionToNode(routerAction, x, y);
                    document.Nodes.Add(node);
                    
                    // 添加连接
                    document.Connections.Add(FlowConnectionData.Create(previousNodeName, node.Name));
                    y += spacingY;

                    // 处理路由分支
                    ConvertRouterBranches(routerAction, document, node.Name, x, ref y, spacingY);
                    return;

                case LoopOnItemsAction loopAction:
                    node = ConvertLoopActionToNode(loopAction, x, y);
                    document.Nodes.Add(node);
                    
                    // 添加连接
                    document.Connections.Add(FlowConnectionData.Create(previousNodeName, node.Name));
                    y += spacingY;

                    // 处理循环体
                    ConvertLoopBody(loopAction, document, node.Name, x, ref y, spacingY);
                    return;

                default:
                    node = new FlowNodeData
                    {
                        Name = action.Name,
                        DisplayName = action.DisplayName,
                        Type = FlowNodeType.Process,
                        Position = new PointF(x, y),
                        Valid = action.Valid,
                        Skip = action.Skip
                    };
                    break;
            }

            if (node != null)
            {
                document.Nodes.Add(node);
                
                // 添加连接
                document.Connections.Add(FlowConnectionData.Create(previousNodeName, node.Name));
                y += spacingY;

                // 处理下一个动作
                if (action.NextAction != null)
                {
                    ConvertActionToNodes(action.NextAction, document, node.Name, x, ref y, spacingY);
                }
            }
        }

        /// <summary>
        /// 转换代码动作为节点
        /// </summary>
        private FlowNodeData ConvertCodeActionToNode(CodeAction action, float x, float y)
        {
            var node = new FlowNodeData
            {
                Name = action.Name,
                DisplayName = action.DisplayName,
                Type = FlowNodeType.Code,
                Position = new PointF(x, y),
                Valid = action.Valid,
                Skip = action.Skip,
                Properties = new Dictionary<string, object>()
            };

            if (action.Settings?.SourceCode?.Code != null)
            {
                node.Properties["code"] = action.Settings.SourceCode.Code;
            }

            if (action.Settings?.Input != null)
            {
                node.Settings = new Dictionary<string, object>(action.Settings.Input);
            }

            return node;
        }

        /// <summary>
        /// 转换组件动作为节点
        /// </summary>
        private FlowNodeData ConvertPieceActionToNode(PieceAction action, float x, float y)
        {
            var node = new FlowNodeData
            {
                Name = action.Name,
                DisplayName = action.DisplayName,
                Type = FlowNodeType.Piece,
                Position = new PointF(x, y),
                Valid = action.Valid,
                Skip = action.Skip,
                Properties = new Dictionary<string, object>()
            };

            if (action.Settings != null)
            {
                node.Properties["pieceName"] = action.Settings.PieceName;
                node.Properties["pieceVersion"] = action.Settings.PieceVersion;
                node.Properties["actionName"] = action.Settings.ActionName;

                if (action.Settings.Input != null)
                {
                    node.Settings = new Dictionary<string, object>(action.Settings.Input);
                }
            }

            return node;
        }

        /// <summary>
        /// 转换路由动作为节点
        /// </summary>
        private FlowNodeData ConvertRouterActionToNode(RouterAction action, float x, float y)
        {
            var node = new FlowNodeData
            {
                Name = action.Name,
                DisplayName = action.DisplayName,
                Type = FlowNodeType.Decision,
                Position = new PointF(x, y),
                Valid = action.Valid,
                Skip = action.Skip,
                Properties = new Dictionary<string, object>()
            };

            // 保存分支条件
            if (action.Settings?.Branches != null)
            {
                for (int i = 0; i < action.Settings.Branches.Count; i++)
                {
                    var branch = action.Settings.Branches[i];
                    node.Properties[$"branch_{i}_name"] = branch.BranchName;
                    if (branch.Conditions?.Count > 0 && branch.Conditions[0]?.Count > 0)
                    {
                        node.Properties[$"condition_{i}"] = branch.Conditions[0][0].FirstValue;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// 转换循环动作为节点
        /// </summary>
        private FlowNodeData ConvertLoopActionToNode(LoopOnItemsAction action, float x, float y)
        {
            var node = new FlowNodeData
            {
                Name = action.Name,
                DisplayName = action.DisplayName,
                Type = FlowNodeType.Loop,
                Position = new PointF(x, y),
                Valid = action.Valid,
                Skip = action.Skip,
                Properties = new Dictionary<string, object>()
            };

            if (action.Settings != null)
            {
                node.Properties["items"] = action.Settings.Items;
            }

            return node;
        }

        /// <summary>
        /// 处理路由分支
        /// </summary>
        private void ConvertRouterBranches(
            RouterAction routerAction,
            FlowDocument document,
            string routerNodeName,
            float baseX,
            ref float y,
            float spacingY)
        {
            if (routerAction.Children == null || routerAction.Children.Count == 0)
            {
                // 没有分支，继续下一个动作
                if (routerAction.NextAction != null)
                {
                    ConvertActionToNodes(routerAction.NextAction, document, routerNodeName, baseX, ref y, spacingY);
                }
                return;
            }

            float branchSpacing = 200;
            float startX = baseX - (routerAction.Children.Count - 1) * branchSpacing / 2;
            float maxY = y;

            for (int i = 0; i < routerAction.Children.Count; i++)
            {
                var childAction = routerAction.Children[i];
                if (childAction == null) continue;

                float branchX = startX + i * branchSpacing;
                float branchY = y;

                string label = routerAction.Settings?.Branches?.Count > i 
                    ? routerAction.Settings.Branches[i].BranchName 
                    : $"分支 {i + 1}";

                // 转换分支动作
                ConvertActionToNodes(childAction, document, routerNodeName, branchX, ref branchY, spacingY);

                // 添加带标签的连接
                var lastAddedConn = document.Connections.LastOrDefault();
                if (lastAddedConn != null && lastAddedConn.SourceNode == routerNodeName)
                {
                    lastAddedConn.Label = label;
                    lastAddedConn.Type = FlowConnectionType.RouterStartEdge;
                }

                maxY = Math.Max(maxY, branchY);
            }

            y = maxY;

            // 处理路由后的下一个动作
            if (routerAction.NextAction != null)
            {
                ConvertActionToNodes(routerAction.NextAction, document, routerNodeName, baseX, ref y, spacingY);
            }
        }

        /// <summary>
        /// 处理循环体
        /// </summary>
        private void ConvertLoopBody(
            LoopOnItemsAction loopAction,
            FlowDocument document,
            string loopNodeName,
            float x,
            ref float y,
            float spacingY)
        {
            if (loopAction.FirstLoopAction != null)
            {
                float loopBodyX = x + 50; // 稍微偏移以显示层次
                float loopBodyY = y;

                // 添加循环体开始连接
                var startConn = FlowConnectionData.Create(loopNodeName, loopAction.FirstLoopAction.Name);
                startConn.Label = "循环体";
                startConn.Type = FlowConnectionType.LoopStartEdge;

                // 转换循环体内的动作
                ConvertActionToNodes(loopAction.FirstLoopAction, document, loopNodeName, loopBodyX, ref loopBodyY, spacingY);

                // 更新连接类型
                var conn = document.Connections.FirstOrDefault(c => c.SourceNode == loopNodeName);
                if (conn != null)
                {
                    conn.Type = FlowConnectionType.LoopStartEdge;
                }

                y = loopBodyY;
            }

            // 处理循环后的下一个动作
            if (loopAction.NextAction != null)
            {
                ConvertActionToNodes(loopAction.NextAction, document, loopNodeName, x, ref y, spacingY);
            }
        }

        #endregion
    }
}
