using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Connections;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Validation
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; }
        public List<ValidationWarning> Warnings { get; set; }

        public ValidationResult()
        {
            Errors = new List<ValidationError>();
            Warnings = new List<ValidationWarning>();
            IsValid = true;
        }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string NodeName { get; set; }
        public string Message { get; set; }
        public ValidationErrorType Type { get; set; }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        public string NodeName { get; set; }
        public string Message { get; set; }
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
        UnconnectedPort
    }

    /// <summary>
    /// 流程验证器
    /// </summary>
    public static class FlowValidator
    {
        /// <summary>
        /// 验证节点
        /// </summary>
        public static ValidationResult ValidateNode(FlowNodeData nodeData)
        {
            var result = new ValidationResult();

            if (nodeData == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "节点数据为空",
                    Type = ValidationErrorType.MissingRequiredProperty
                });
                return result;
            }

            // 验证必填属性
            if (string.IsNullOrEmpty(nodeData.Name))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    NodeName = nodeData.Name,
                    Message = "节点名称不能为空",
                    Type = ValidationErrorType.MissingRequiredProperty
                });
            }

            if (string.IsNullOrEmpty(nodeData.DisplayName))
            {
                result.Warnings.Add(new ValidationWarning
                {
                    NodeName = nodeData.Name,
                    Message = "节点显示名称为空"
                });
            }

            return result;
        }

        /// <summary>
        /// 验证连接
        /// </summary>
        public static ValidationResult ValidateConnection(
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
                    Type = ValidationErrorType.InvalidConnection
                });
                return result;
            }

            // 验证源节点和目标节点是否存在
            if (!nodes.ContainsKey(connectionData.SourceNode))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = $"源节点 '{connectionData.SourceNode}' 不存在",
                    Type = ValidationErrorType.InvalidConnection
                });
            }

            if (!nodes.ContainsKey(connectionData.TargetNode))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = $"目标节点 '{connectionData.TargetNode}' 不存在",
                    Type = ValidationErrorType.InvalidConnection
                });
            }

            // 验证不能连接到自身
            if (connectionData.SourceNode == connectionData.TargetNode)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "节点不能连接到自身",
                    Type = ValidationErrorType.InvalidConnection
                });
            }

            return result;
        }

        /// <summary>
        /// 检查循环连接
        /// </summary>
        public static bool HasCircularConnection(
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

        private static bool HasCycleDFS(string currentNode, string targetNode,
            Dictionary<string, FlowNodeData> nodes,
            List<FlowConnectionData> connections,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            if (currentNode == targetNode)
                return true;

            if (recursionStack.Contains(currentNode))
                return false; // 已经在当前路径中

            visited.Add(currentNode);
            recursionStack.Add(currentNode);

            // 查找所有从当前节点出发的连接
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

        /// <summary>
        /// 验证流程完整性
        /// </summary>
        public static ValidationResult ValidateFlow(FlowDocument document, 
            Dictionary<string, FlowNode> nodes, 
            List<FlowConnection> connections)
        {
            var result = new ValidationResult();

            if (document == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Message = "流程文档为空",
                    Type = ValidationErrorType.MissingRequiredProperty
                });
                return result;
            }

            var nodeDict = document.Nodes?.ToDictionary(n => n.Name) ?? new Dictionary<string, FlowNodeData>();
            var connectionList = document.Connections ?? new List<FlowConnectionData>();

            // 验证所有节点
            foreach (var nodeData in document.Nodes ?? new List<FlowNodeData>())
            {
                var nodeResult = ValidateNode(nodeData);
                result.Errors.AddRange(nodeResult.Errors);
                result.Warnings.AddRange(nodeResult.Warnings);
            }

            // 验证所有连接
            foreach (var connData in connectionList)
            {
                var connResult = ValidateConnection(connData, nodeDict);
                result.Errors.AddRange(connResult.Errors);
                result.Warnings.AddRange(connResult.Warnings);

                // 检查循环连接
                if (HasCircularConnection(connData, nodeDict, connectionList))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Message = $"连接 '{connData.SourceNode}' -> '{connData.TargetNode}' 会导致循环",
                        Type = ValidationErrorType.CircularConnection
                    });
                }
            }

            // 检查孤立节点（没有输入也没有输出连接）
            foreach (var nodeData in document.Nodes ?? new List<FlowNodeData>())
            {
                var hasInput = connectionList.Any(c => c.TargetNode == nodeData.Name);
                var hasOutput = connectionList.Any(c => c.SourceNode == nodeData.Name);

                // 开始节点和结束节点例外
                if (nodeData.Type != FlowNodeType.Start && nodeData.Type != FlowNodeType.End)
                {
                    if (!hasInput && !hasOutput)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            NodeName = nodeData.Name,
                            Message = "节点没有连接，可能是孤立节点"
                        });
                    }
                    else if (!hasInput)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            NodeName = nodeData.Name,
                            Message = "节点没有输入连接"
                        });
                    }
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
}
