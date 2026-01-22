using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Connections;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Validation
{
    #region 验证结果模型

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; }
        public List<ValidationWarning> Warnings { get; set; }
        public ValidationStatistics Statistics { get; set; }

        public ValidationResult()
        {
            Errors = new List<ValidationError>();
            Warnings = new List<ValidationWarning>();
            Statistics = new ValidationStatistics();
            IsValid = true;
        }

        /// <summary>
        /// 合并验证结果
        /// </summary>
        public void Merge(ValidationResult other)
        {
            if (other == null) return;
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            IsValid = IsValid && other.IsValid;
        }
    }

    /// <summary>
    /// 验证统计
    /// </summary>
    public class ValidationStatistics
    {
        public int TotalNodes { get; set; }
        public int ValidNodes { get; set; }
        public int InvalidNodes { get; set; }
        public int TotalConnections { get; set; }
        public int ValidConnections { get; set; }
        public int InvalidConnections { get; set; }
        public int OrphanedNodes { get; set; }
        public int CyclesDetected { get; set; }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string NodeName { get; set; }
        public string ConnectionId { get; set; }
        public string Message { get; set; }
        public ValidationErrorType Type { get; set; }
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        public PointF? Location { get; set; }
        public string RuleName { get; set; }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        public string NodeName { get; set; }
        public string Message { get; set; }
        public PointF? Location { get; set; }
    }

    /// <summary>
    /// 验证错误类型
    /// </summary>
    public enum ValidationErrorType
    {
        MissingRequiredProperty,
        InvalidConnection,
        CircularConnection,
        OrphanedNode,
        UnconnectedPort,
        InvalidNodeType,
        DuplicateNodeName,
        InvalidPropertyValue,
        MissingStartNode,
        MissingEndNode,
        MultipleStartNodes,
        UnreachableNode,
        CustomRule
    }

    /// <summary>
    /// 验证严重性
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    #endregion

    #region 8.2.4 自定义验证规则接口

    /// <summary>
    /// 验证规则接口
    /// </summary>
    public interface IValidationRule
    {
        string Name { get; }
        string Description { get; }
        bool IsEnabled { get; set; }
        ValidationResult Validate(FlowValidationContext context);
    }

    /// <summary>
    /// 验证上下文
    /// </summary>
    public class FlowValidationContext
    {
        public FlowDocument Document { get; set; }
        public Dictionary<string, FlowNode> Nodes { get; set; }
        public List<FlowConnection> Connections { get; set; }
        public Dictionary<string, FlowNodeData> NodeDataMap { get; set; }
        public List<FlowConnectionData> ConnectionDataList { get; set; }
    }

    #endregion

    /// <summary>
    /// 8.2 流程验证器
    /// 支持连接完整性验证、循环检测、必填属性验证、自定义规则、验证结果可视化
    /// </summary>
    public class FlowValidator
    {
        #region 字段

        private readonly List<IValidationRule> _customRules = new List<IValidationRule>();
        private static FlowValidator _instance;
        private static readonly object _lock = new object();

        #endregion

        #region 单例

        public static FlowValidator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FlowValidator();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 构造

        public FlowValidator()
        {
            // 注册内置验证规则
            RegisterBuiltInRules();
        }

        private void RegisterBuiltInRules()
        {
            _customRules.Add(new StartNodeRule());
            _customRules.Add(new EndNodeRule());
            _customRules.Add(new DuplicateNameRule());
            _customRules.Add(new ReachabilityRule());
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 注册自定义验证规则
        /// </summary>
        public void RegisterRule(IValidationRule rule)
        {
            if (rule != null && !_customRules.Any(r => r.Name == rule.Name))
            {
                _customRules.Add(rule);
            }
        }

        /// <summary>
        /// 移除验证规则
        /// </summary>
        public void RemoveRule(string ruleName)
        {
            _customRules.RemoveAll(r => r.Name == ruleName);
        }

        /// <summary>
        /// 获取所有规则
        /// </summary>
        public IReadOnlyList<IValidationRule> GetRules()
        {
            return _customRules.AsReadOnly();
        }

        #endregion

        #region 8.2.1 连接完整性验证

        /// <summary>
        /// 验证节点
        /// </summary>
        public ValidationResult ValidateNode(FlowNodeData nodeData)
        {
            var result = new ValidationResult();

            if (nodeData == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "节点数据为空",
                    Type = ValidationErrorType.MissingRequiredProperty,
                    Severity = ValidationSeverity.Error
                });
                return result;
            }

            // 8.2.3 验证必填属性
            if (string.IsNullOrEmpty(nodeData.Name))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    NodeName = nodeData.Name,
                    Message = "节点名称不能为空",
                    Type = ValidationErrorType.MissingRequiredProperty,
                    Severity = ValidationSeverity.Error,
                    Location = nodeData.Position
                });
            }

            if (string.IsNullOrEmpty(nodeData.DisplayName))
            {
                result.Warnings.Add(new ValidationWarning
                {
                    NodeName = nodeData.Name,
                    Message = "节点显示名称为空",
                    Location = nodeData.Position
                });
            }

            // 验证节点特定属性
            ValidateNodeTypeSpecificProperties(nodeData, result);

            return result;
        }

        private void ValidateNodeTypeSpecificProperties(FlowNodeData nodeData, ValidationResult result)
        {
            switch (nodeData.Type)
            {
                case FlowNodeType.Decision:
                    // 决策节点应该有条件表达式
                    if (nodeData.Properties == null || !nodeData.Properties.ContainsKey("condition"))
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            NodeName = nodeData.Name,
                            Message = "决策节点缺少条件表达式",
                            Location = nodeData.Position
                        });
                    }
                    break;

                case FlowNodeType.Loop:
                    // 循环节点应该有循环条件
                    if (nodeData.Properties == null || !nodeData.Properties.ContainsKey("loopCondition"))
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            NodeName = nodeData.Name,
                            Message = "循环节点缺少循环条件",
                            Location = nodeData.Position
                        });
                    }
                    break;
            }
        }

        /// <summary>
        /// 验证连接
        /// </summary>
        public ValidationResult ValidateConnection(
            FlowConnectionData connectionData,
            Dictionary<string, FlowNodeData> nodes)
        {
            var result = new ValidationResult();

            if (connectionData == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "连接数据为空",
                    Type = ValidationErrorType.InvalidConnection,
                    Severity = ValidationSeverity.Error
                });
                return result;
            }

            // 验证源节点和目标节点是否存在
            if (!nodes.ContainsKey(connectionData.SourceNode))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = string.Format("源节点 '{0}' 不存在", connectionData.SourceNode),
                    Type = ValidationErrorType.InvalidConnection,
                    Severity = ValidationSeverity.Error
                });
            }

            if (!nodes.ContainsKey(connectionData.TargetNode))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = string.Format("目标节点 '{0}' 不存在", connectionData.TargetNode),
                    Type = ValidationErrorType.InvalidConnection,
                    Severity = ValidationSeverity.Error
                });
            }

            // 验证不能连接到自身
            if (connectionData.SourceNode == connectionData.TargetNode)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "节点不能连接到自身",
                    Type = ValidationErrorType.InvalidConnection,
                    Severity = ValidationSeverity.Error
                });
            }

            // 验证连接规则
            if (nodes.ContainsKey(connectionData.SourceNode) && nodes.ContainsKey(connectionData.TargetNode))
            {
                var sourceNode = nodes[connectionData.SourceNode];
                var targetNode = nodes[connectionData.TargetNode];

                // 结束节点不能有输出
                if (sourceNode.Type == FlowNodeType.End)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        NodeName = connectionData.SourceNode,
                        Message = "结束节点不能有输出连接",
                        Type = ValidationErrorType.InvalidConnection,
                        Severity = ValidationSeverity.Error
                    });
                }

                // 开始节点不能有输入
                if (targetNode.Type == FlowNodeType.Start)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        NodeName = connectionData.TargetNode,
                        Message = "开始节点不能有输入连接",
                        Type = ValidationErrorType.InvalidConnection,
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            return result;
        }

        #endregion

        #region 8.2.2 循环检测

        /// <summary>
        /// 检查是否存在循环连接
        /// </summary>
        public bool HasCircularConnection(
            FlowConnectionData connectionData,
            Dictionary<string, FlowNodeData> nodes,
            List<FlowConnectionData> allConnections)
        {
            if (connectionData == null || nodes == null || allConnections == null)
                return false;

            // 使用DFS检查是否存在循环
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            return HasCycleDFS(connectionData.TargetNode, connectionData.SourceNode,
                nodes, allConnections, visited, recursionStack);
        }

        /// <summary>
        /// 检测图中所有循环
        /// </summary>
        public List<List<string>> DetectAllCycles(
            Dictionary<string, FlowNodeData> nodes,
            List<FlowConnectionData> connections)
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var recursionStack = new Stack<string>();

            foreach (var node in nodes.Keys)
            {
                if (!visited.Contains(node))
                {
                    DetectCyclesDFS(node, connections, visited, recursionStack, cycles);
                }
            }

            return cycles;
        }

        private void DetectCyclesDFS(
            string currentNode,
            List<FlowConnectionData> connections,
            HashSet<string> visited,
            Stack<string> recursionStack,
            List<List<string>> cycles)
        {
            visited.Add(currentNode);
            recursionStack.Push(currentNode);

            var outgoingConnections = connections.Where(c => c.SourceNode == currentNode).ToList();
            foreach (var conn in outgoingConnections)
            {
                if (!visited.Contains(conn.TargetNode))
                {
                    DetectCyclesDFS(conn.TargetNode, connections, visited, recursionStack, cycles);
                }
                else if (recursionStack.Contains(conn.TargetNode))
                {
                    // 发现循环
                    var cycle = new List<string>();
                    var stackArray = recursionStack.ToArray();
                    var cycleStartIndex = Array.IndexOf(stackArray, conn.TargetNode);
                    for (int i = cycleStartIndex; i >= 0; i--)
                    {
                        cycle.Add(stackArray[i]);
                    }
                    cycle.Add(conn.TargetNode);
                    cycles.Add(cycle);
                }
            }

            recursionStack.Pop();
        }

        private bool HasCycleDFS(string currentNode, string targetNode,
            Dictionary<string, FlowNodeData> nodes,
            List<FlowConnectionData> connections,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            if (currentNode == targetNode)
                return true;

            if (recursionStack.Contains(currentNode))
                return false;

            visited.Add(currentNode);
            recursionStack.Add(currentNode);

            var outgoingConnections = connections.Where(c => c.SourceNode == currentNode).ToList();
            foreach (var conn in outgoingConnections)
            {
                if (HasCycleDFS(conn.TargetNode, targetNode, nodes, connections, visited, recursionStack))
                {
                    return true;
                }
            }

            recursionStack.Remove(currentNode);
            return false;
        }

        #endregion

        #region 完整流程验证

        /// <summary>
        /// 验证流程完整性
        /// </summary>
        public ValidationResult ValidateFlow(FlowDocument document,
            Dictionary<string, FlowNode> nodes = null,
            List<FlowConnection> connections = null)
        {
            var result = new ValidationResult();

            if (document == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "流程文档为空",
                    Type = ValidationErrorType.MissingRequiredProperty,
                    Severity = ValidationSeverity.Critical
                });
                return result;
            }

            var nodeDict = document.Nodes?.ToDictionary(n => n.Name) ?? new Dictionary<string, FlowNodeData>();
            var connectionList = document.Connections ?? new List<FlowConnectionData>();

            // 初始化统计
            result.Statistics.TotalNodes = nodeDict.Count;
            result.Statistics.TotalConnections = connectionList.Count;

            // 创建验证上下文
            var context = new FlowValidationContext
            {
                Document = document,
                Nodes = nodes,
                Connections = connections,
                NodeDataMap = nodeDict,
                ConnectionDataList = connectionList
            };

            // 验证所有节点
            int validNodes = 0;
            foreach (var nodeData in document.Nodes ?? new List<FlowNodeData>())
            {
                var nodeResult = ValidateNode(nodeData);
                result.Errors.AddRange(nodeResult.Errors);
                result.Warnings.AddRange(nodeResult.Warnings);
                if (nodeResult.IsValid) validNodes++;
            }
            result.Statistics.ValidNodes = validNodes;
            result.Statistics.InvalidNodes = result.Statistics.TotalNodes - validNodes;

            // 验证所有连接
            int validConnections = 0;
            foreach (var connData in connectionList)
            {
                var connResult = ValidateConnection(connData, nodeDict);
                result.Errors.AddRange(connResult.Errors);
                result.Warnings.AddRange(connResult.Warnings);
                if (connResult.IsValid) validConnections++;

                // 检查循环连接
                if (HasCircularConnection(connData, nodeDict, connectionList))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Message = string.Format("连接 '{0}' -> '{1}' 会导致循环", connData.SourceNode, connData.TargetNode),
                        Type = ValidationErrorType.CircularConnection,
                        Severity = ValidationSeverity.Error
                    });
                    result.Statistics.CyclesDetected++;
                }
            }
            result.Statistics.ValidConnections = validConnections;
            result.Statistics.InvalidConnections = result.Statistics.TotalConnections - validConnections;

            // 检查孤立节点
            foreach (var nodeData in document.Nodes ?? new List<FlowNodeData>())
            {
                var hasInput = connectionList.Any(c => c.TargetNode == nodeData.Name);
                var hasOutput = connectionList.Any(c => c.SourceNode == nodeData.Name);

                if (nodeData.Type != FlowNodeType.Start && nodeData.Type != FlowNodeType.End)
                {
                    if (!hasInput && !hasOutput)
                    {
                        result.Statistics.OrphanedNodes++;
                        result.Warnings.Add(new ValidationWarning
                        {
                            NodeName = nodeData.Name,
                            Message = "节点没有连接，可能是孤立节点",
                            Location = nodeData.Position
                        });
                    }
                    else if (!hasInput)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            NodeName = nodeData.Name,
                            Message = "节点没有输入连接",
                            Location = nodeData.Position
                        });
                    }
                }
            }

            // 8.2.4 执行自定义规则
            foreach (var rule in _customRules.Where(r => r.IsEnabled))
            {
                try
                {
                    var ruleResult = rule.Validate(context);
                    result.Merge(ruleResult);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Message = string.Format("验证规则 '{0}' 执行失败: {1}", rule.Name, ex.Message)
                    });
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        #endregion

        #region 8.2.5 验证结果可视化

        /// <summary>
        /// 获取验证错误的高亮位置
        /// </summary>
        public List<ValidationHighlight> GetValidationHighlights(
            ValidationResult result,
            Dictionary<string, FlowNode> nodes)
        {
            var highlights = new List<ValidationHighlight>();

            foreach (var error in result.Errors)
            {
                var highlight = new ValidationHighlight
                {
                    Type = HighlightType.Error,
                    Message = error.Message,
                    Color = Color.FromArgb(200, 239, 68, 68) // 红色
                };

                if (!string.IsNullOrEmpty(error.NodeName) && nodes != null && nodes.TryGetValue(error.NodeName, out var node))
                {
                    highlight.Bounds = node.GetBounds();
                    highlight.NodeName = error.NodeName;
                }
                else if (error.Location.HasValue)
                {
                    highlight.Bounds = new RectangleF(error.Location.Value.X - 20, error.Location.Value.Y - 20, 40, 40);
                }

                highlights.Add(highlight);
            }

            foreach (var warning in result.Warnings)
            {
                var highlight = new ValidationHighlight
                {
                    Type = HighlightType.Warning,
                    Message = warning.Message,
                    Color = Color.FromArgb(200, 234, 179, 8) // 黄色
                };

                if (!string.IsNullOrEmpty(warning.NodeName) && nodes != null && nodes.TryGetValue(warning.NodeName, out var node))
                {
                    highlight.Bounds = node.GetBounds();
                    highlight.NodeName = warning.NodeName;
                }
                else if (warning.Location.HasValue)
                {
                    highlight.Bounds = new RectangleF(warning.Location.Value.X - 20, warning.Location.Value.Y - 20, 40, 40);
                }

                highlights.Add(highlight);
            }

            return highlights;
        }

        #endregion
    }

    #region 验证高亮

    public enum HighlightType
    {
        Error,
        Warning,
        Info
    }

    public class ValidationHighlight
    {
        public HighlightType Type { get; set; }
        public string NodeName { get; set; }
        public string Message { get; set; }
        public RectangleF Bounds { get; set; }
        public Color Color { get; set; }
    }

    #endregion

    #region 内置验证规则

    /// <summary>
    /// 开始节点规则
    /// </summary>
    public class StartNodeRule : IValidationRule
    {
        public string Name => "StartNodeRule";
        public string Description => "检查流程是否有且仅有一个开始节点";
        public bool IsEnabled { get; set; } = true;

        public ValidationResult Validate(FlowValidationContext context)
        {
            var result = new ValidationResult();
            var startNodes = context.NodeDataMap.Values.Where(n => n.Type == FlowNodeType.Start).ToList();

            if (startNodes.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "流程缺少开始节点",
                    Type = ValidationErrorType.MissingStartNode,
                    Severity = ValidationSeverity.Critical,
                    RuleName = Name
                });
            }
            else if (startNodes.Count > 1)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = string.Format("流程有多个开始节点 ({0}个)", startNodes.Count),
                    Type = ValidationErrorType.MultipleStartNodes,
                    Severity = ValidationSeverity.Error,
                    RuleName = Name
                });
            }

            return result;
        }
    }

    /// <summary>
    /// 结束节点规则
    /// </summary>
    public class EndNodeRule : IValidationRule
    {
        public string Name => "EndNodeRule";
        public string Description => "检查流程是否至少有一个结束节点";
        public bool IsEnabled { get; set; } = true;

        public ValidationResult Validate(FlowValidationContext context)
        {
            var result = new ValidationResult();
            var endNodes = context.NodeDataMap.Values.Where(n => n.Type == FlowNodeType.End).ToList();

            if (endNodes.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "流程缺少结束节点",
                    Type = ValidationErrorType.MissingEndNode,
                    Severity = ValidationSeverity.Warning,
                    RuleName = Name
                });
            }

            return result;
        }
    }

    /// <summary>
    /// 重复名称规则
    /// </summary>
    public class DuplicateNameRule : IValidationRule
    {
        public string Name => "DuplicateNameRule";
        public string Description => "检查是否有重复的节点名称";
        public bool IsEnabled { get; set; } = true;

        public ValidationResult Validate(FlowValidationContext context)
        {
            var result = new ValidationResult();
            var names = context.Document.Nodes?.Select(n => n.Name).ToList() ?? new List<string>();
            var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            foreach (var duplicate in duplicates)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    NodeName = duplicate,
                    Message = string.Format("节点名称 '{0}' 重复", duplicate),
                    Type = ValidationErrorType.DuplicateNodeName,
                    Severity = ValidationSeverity.Error,
                    RuleName = Name
                });
            }

            return result;
        }
    }

    /// <summary>
    /// 可达性规则
    /// </summary>
    public class ReachabilityRule : IValidationRule
    {
        public string Name => "ReachabilityRule";
        public string Description => "检查是否所有节点都可以从开始节点到达";
        public bool IsEnabled { get; set; } = true;

        public ValidationResult Validate(FlowValidationContext context)
        {
            var result = new ValidationResult();
            var startNode = context.NodeDataMap.Values.FirstOrDefault(n => n.Type == FlowNodeType.Start);

            if (startNode == null)
                return result;

            // BFS找到所有可达节点
            var reachable = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(startNode.Name);
            reachable.Add(startNode.Name);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var outgoing = context.ConnectionDataList.Where(c => c.SourceNode == current);

                foreach (var conn in outgoing)
                {
                    if (!reachable.Contains(conn.TargetNode))
                    {
                        reachable.Add(conn.TargetNode);
                        queue.Enqueue(conn.TargetNode);
                    }
                }
            }

            // 检查不可达节点
            foreach (var nodeData in context.NodeDataMap.Values)
            {
                if (!reachable.Contains(nodeData.Name) && nodeData.Type != FlowNodeType.Start)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        NodeName = nodeData.Name,
                        Message = string.Format("节点 '{0}' 从开始节点不可达", nodeData.DisplayName ?? nodeData.Name),
                        Location = nodeData.Position
                    });
                }
            }

            return result;
        }
    }

    #endregion
}
