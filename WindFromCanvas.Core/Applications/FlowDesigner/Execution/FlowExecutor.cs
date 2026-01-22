using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Execution
{
    #region 执行事件参数

    /// <summary>
    /// 执行状态变更事件参数
    /// </summary>
    public class ExecutionStateChangedEventArgs : EventArgs
    {
        public ExecutionState State { get; set; }
        public string CurrentNodeName { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 节点执行事件参数
    /// </summary>
    public class NodeExecutionEventArgs : EventArgs
    {
        public string NodeName { get; set; }
        public NodeExecutionStatus Status { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public Exception Error { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 断点命中事件参数
    /// </summary>
    public class BreakpointHitEventArgs : EventArgs
    {
        public string NodeName { get; set; }
        public Dictionary<string, object> Variables { get; set; }
    }

    #endregion

    #region 枚举

    /// <summary>
    /// 执行状态
    /// </summary>
    public enum ExecutionState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 暂停（断点）
        /// </summary>
        Paused,

        /// <summary>
        /// 单步执行中
        /// </summary>
        Stepping,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 节点执行状态
    /// </summary>
    public enum NodeExecutionStatus
    {
        Pending,
        Running,
        Success,
        Failed,
        Skipped
    }

    #endregion

    /// <summary>
    /// 流程执行器
    /// 支持单步执行、断点调试、状态可视化
    /// </summary>
    public class FlowExecutor : IDisposable
    {
        #region 字段

        private readonly Dictionary<string, FlowNode> _nodes;
        private readonly List<FlowConnectionData> _connections;
        private readonly FlowDocument _document;

        private ExecutionState _state = ExecutionState.Idle;
        private CancellationTokenSource _cancellationTokenSource;
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true);

        private readonly HashSet<string> _breakpoints = new HashSet<string>();
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();
        private readonly Dictionary<string, NodeExecutionResult> _executionResults = new Dictionary<string, NodeExecutionResult>();

        private string _currentNodeName;
        private DateTime _startTime;

        #endregion

        #region 事件

        /// <summary>
        /// 执行状态变更事件
        /// </summary>
        public event EventHandler<ExecutionStateChangedEventArgs> StateChanged;

        /// <summary>
        /// 节点执行事件
        /// </summary>
        public event EventHandler<NodeExecutionEventArgs> NodeExecuted;

        /// <summary>
        /// 断点命中事件
        /// </summary>
        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;

        /// <summary>
        /// 变量变更事件
        /// </summary>
        public event EventHandler<string> VariableChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 当前执行状态
        /// </summary>
        public ExecutionState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, new ExecutionStateChangedEventArgs
                    {
                        State = value,
                        CurrentNodeName = _currentNodeName,
                        Message = GetStateMessage(value)
                    });
                }
            }
        }

        /// <summary>
        /// 当前执行的节点名称
        /// </summary>
        public string CurrentNodeName => _currentNodeName;

        /// <summary>
        /// 变量字典
        /// </summary>
        public IReadOnlyDictionary<string, object> Variables => _variables;

        /// <summary>
        /// 断点集合
        /// </summary>
        public IReadOnlyCollection<string> Breakpoints => _breakpoints;

        /// <summary>
        /// 执行结果
        /// </summary>
        public IReadOnlyDictionary<string, NodeExecutionResult> ExecutionResults => _executionResults;

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ElapsedTime => _state != ExecutionState.Idle 
            ? DateTime.Now - _startTime 
            : TimeSpan.Zero;

        #endregion

        #region 构造

        public FlowExecutor(FlowDocument document, Dictionary<string, FlowNode> nodes)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _connections = document.Connections ?? new List<FlowConnectionData>();
        }

        #endregion

        #region 断点管理

        /// <summary>
        /// 添加断点
        /// </summary>
        public void AddBreakpoint(string nodeName)
        {
            if (!string.IsNullOrEmpty(nodeName))
            {
                _breakpoints.Add(nodeName);
            }
        }

        /// <summary>
        /// 移除断点
        /// </summary>
        public void RemoveBreakpoint(string nodeName)
        {
            _breakpoints.Remove(nodeName);
        }

        /// <summary>
        /// 切换断点
        /// </summary>
        public bool ToggleBreakpoint(string nodeName)
        {
            if (_breakpoints.Contains(nodeName))
            {
                _breakpoints.Remove(nodeName);
                return false;
            }
            else
            {
                _breakpoints.Add(nodeName);
                return true;
            }
        }

        /// <summary>
        /// 清除所有断点
        /// </summary>
        public void ClearBreakpoints()
        {
            _breakpoints.Clear();
        }

        /// <summary>
        /// 检查节点是否有断点
        /// </summary>
        public bool HasBreakpoint(string nodeName)
        {
            return _breakpoints.Contains(nodeName);
        }

        #endregion

        #region 执行控制

        /// <summary>
        /// 开始执行
        /// </summary>
        public async Task StartAsync()
        {
            if (State != ExecutionState.Idle && State != ExecutionState.Completed && 
                State != ExecutionState.Failed && State != ExecutionState.Cancelled)
            {
                return;
            }

            Reset();
            _startTime = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseEvent.Set();
            State = ExecutionState.Running;

            try
            {
                await ExecuteFlowAsync(_cancellationTokenSource.Token);
                if (State == ExecutionState.Running)
                {
                    State = ExecutionState.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                State = ExecutionState.Cancelled;
            }
            catch (Exception ex)
            {
                State = ExecutionState.Failed;
                OnNodeExecuted(_currentNodeName, NodeExecutionStatus.Failed, null, null, ex);
            }
        }

        /// <summary>
        /// 暂停执行
        /// </summary>
        public void Pause()
        {
            if (State == ExecutionState.Running)
            {
                _pauseEvent.Reset();
                State = ExecutionState.Paused;
            }
        }

        /// <summary>
        /// 继续执行
        /// </summary>
        public void Continue()
        {
            if (State == ExecutionState.Paused || State == ExecutionState.Stepping)
            {
                State = ExecutionState.Running;
                _pauseEvent.Set();
            }
        }

        /// <summary>
        /// 单步执行
        /// </summary>
        public void StepOver()
        {
            if (State == ExecutionState.Paused || State == ExecutionState.Stepping)
            {
                State = ExecutionState.Stepping;
                _pauseEvent.Set();
            }
            else if (State == ExecutionState.Idle)
            {
                // 从头开始单步执行
                _ = StartStepByStepAsync();
            }
        }

        /// <summary>
        /// 从头开始单步执行
        /// </summary>
        private async Task StartStepByStepAsync()
        {
            Reset();
            _startTime = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();
            State = ExecutionState.Stepping;

            try
            {
                await ExecuteFlowAsync(_cancellationTokenSource.Token);
                if (State == ExecutionState.Stepping)
                {
                    State = ExecutionState.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                State = ExecutionState.Cancelled;
            }
            catch (Exception ex)
            {
                State = ExecutionState.Failed;
                OnNodeExecuted(_currentNodeName, NodeExecutionStatus.Failed, null, null, ex);
            }
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _pauseEvent.Set(); // 确保不阻塞
            State = ExecutionState.Cancelled;
        }

        /// <summary>
        /// 重置执行器
        /// </summary>
        public void Reset()
        {
            _variables.Clear();
            _executionResults.Clear();
            _currentNodeName = null;
            State = ExecutionState.Idle;

            // 重置所有节点状态
            foreach (var node in _nodes.Values)
            {
                if (node.Data != null)
                {
                    node.Data.Status = NodeStatus.None;
                }
            }
        }

        #endregion

        #region 变量管理

        /// <summary>
        /// 设置变量
        /// </summary>
        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
            VariableChanged?.Invoke(this, name);
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        public object GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var value) ? value : null;
        }

        /// <summary>
        /// 清除变量
        /// </summary>
        public void ClearVariables()
        {
            _variables.Clear();
        }

        #endregion

        #region 执行逻辑

        /// <summary>
        /// 执行流程
        /// </summary>
        private async Task ExecuteFlowAsync(CancellationToken cancellationToken)
        {
            // 查找开始节点
            var startNode = _nodes.Values.FirstOrDefault(n => n.Data?.Type == FlowNodeType.Start);
            if (startNode == null)
            {
                throw new InvalidOperationException("流程缺少开始节点");
            }

            // 从开始节点开始执行
            await ExecuteNodeAsync(startNode.Data.Name, cancellationToken);
        }

        /// <summary>
        /// 执行单个节点
        /// </summary>
        private async Task ExecuteNodeAsync(string nodeName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(nodeName) || !_nodes.TryGetValue(nodeName, out var node))
                return;

            cancellationToken.ThrowIfCancellationRequested();

            _currentNodeName = nodeName;
            var nodeData = node.Data;

            // 检查断点
            if (_breakpoints.Contains(nodeName) || State == ExecutionState.Stepping)
            {
                State = ExecutionState.Paused;
                BreakpointHit?.Invoke(this, new BreakpointHitEventArgs
                {
                    NodeName = nodeName,
                    Variables = new Dictionary<string, object>(_variables)
                });

                // 等待继续或单步
                _pauseEvent.Reset();
                _pauseEvent.WaitOne();

                cancellationToken.ThrowIfCancellationRequested();
            }

            // 检查是否跳过
            if (nodeData.Skip)
            {
                OnNodeExecuted(nodeName, NodeExecutionStatus.Skipped, null, null, null);
                nodeData.Status = NodeStatus.Skipped;
                
                // 执行下一个节点
                var nextNodes = GetNextNodes(nodeName);
                foreach (var nextNode in nextNodes)
                {
                    await ExecuteNodeAsync(nextNode, cancellationToken);
                }
                return;
            }

            // 更新状态为运行中
            nodeData.Status = NodeStatus.Running;
            OnNodeExecuted(nodeName, NodeExecutionStatus.Running, null, null, null);

            var startTime = DateTime.Now;

            try
            {
                // 执行节点（根据类型）
                object output = await ExecuteNodeByTypeAsync(node, cancellationToken);

                // 存储结果
                var result = new NodeExecutionResult
                {
                    NodeName = nodeName,
                    Status = NodeExecutionStatus.Success,
                    Output = output,
                    Duration = DateTime.Now - startTime
                };
                _executionResults[nodeName] = result;

                // 将输出存储为变量
                SetVariable($"{nodeName}.output", output);

                // 更新状态
                nodeData.Status = NodeStatus.Success;
                OnNodeExecuted(nodeName, NodeExecutionStatus.Success, null, output, null, result.Duration);

                // 如果是结束节点，停止执行
                if (nodeData.Type == FlowNodeType.End)
                {
                    return;
                }

                // 执行下一个节点
                var nextNodes = GetNextNodes(nodeName, output);
                foreach (var nextNode in nextNodes)
                {
                    await ExecuteNodeAsync(nextNode, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                var result = new NodeExecutionResult
                {
                    NodeName = nodeName,
                    Status = NodeExecutionStatus.Failed,
                    Error = ex,
                    Duration = DateTime.Now - startTime
                };
                _executionResults[nodeName] = result;

                nodeData.Status = NodeStatus.Failed;
                OnNodeExecuted(nodeName, NodeExecutionStatus.Failed, null, null, ex, result.Duration);

                throw;
            }
        }

        /// <summary>
        /// 根据节点类型执行
        /// </summary>
        private async Task<object> ExecuteNodeByTypeAsync(FlowNode node, CancellationToken cancellationToken)
        {
            var nodeData = node.Data;

            switch (nodeData.Type)
            {
                case FlowNodeType.Start:
                    // 开始节点：返回触发器数据
                    return GetVariable("trigger.body") ?? new { timestamp = DateTime.Now };

                case FlowNodeType.End:
                    // 结束节点：收集所有输出
                    return _variables;

                case FlowNodeType.Process:
                    // 处理节点：模拟执行
                    await Task.Delay(100, cancellationToken); // 模拟处理时间
                    return new { processed = true, nodeName = nodeData.Name };

                case FlowNodeType.Decision:
                    // 判断节点：评估条件
                    return EvaluateCondition(nodeData);

                case FlowNodeType.Loop:
                    // 循环节点：返回循环项
                    return ExecuteLoop(nodeData);

                case FlowNodeType.Code:
                    // 代码节点：执行代码（安全沙箱）
                    return ExecuteCode(nodeData);

                case FlowNodeType.Piece:
                    // 组件节点：调用组件
                    await Task.Delay(50, cancellationToken);
                    object pieceName = null;
                    nodeData.Properties?.TryGetValue("pieceName", out pieceName);
                    return new { component = pieceName, result = "success" };

                default:
                    return null;
            }
        }

        /// <summary>
        /// 评估条件
        /// </summary>
        private object EvaluateCondition(FlowNodeData nodeData)
        {
            // 简单实现：检查条件属性
            if (nodeData.Properties != null && nodeData.Properties.TryGetValue("condition", out var condition))
            {
                var conditionStr = condition?.ToString() ?? "";
                
                // 简单的表达式解析
                if (conditionStr.Contains("=="))
                {
                    var parts = conditionStr.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var left = ResolveExpression(parts[0].Trim());
                        var right = ResolveExpression(parts[1].Trim());
                        return left?.ToString() == right?.ToString();
                    }
                }
                
                // 默认返回true
                return !string.IsNullOrEmpty(conditionStr);
            }

            return true;
        }

        /// <summary>
        /// 执行循环
        /// </summary>
        private object ExecuteLoop(FlowNodeData nodeData)
        {
            // 获取循环项
            if (nodeData.Properties != null && nodeData.Properties.TryGetValue("items", out var items))
            {
                var itemsValue = ResolveExpression(items?.ToString() ?? "[]");
                return itemsValue;
            }

            return new List<object>();
        }

        /// <summary>
        /// 执行代码
        /// </summary>
        private object ExecuteCode(FlowNodeData nodeData)
        {
            // 安全考虑：这里只返回代码内容，不实际执行
            // 实际执行需要沙箱环境
            object codeValue = null;
            nodeData.Properties?.TryGetValue("code", out codeValue);
            var code = codeValue?.ToString() ?? "";
            return new { code = code, executed = false, message = "代码执行需要安全沙箱环境" };
        }

        /// <summary>
        /// 解析表达式
        /// </summary>
        private object ResolveExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return null;

            // 检查是否是变量引用 {{variable}}
            if (expression.StartsWith("{{") && expression.EndsWith("}}"))
            {
                var varName = expression.Substring(2, expression.Length - 4).Trim();
                return GetVariable(varName);
            }

            // 尝试解析为数字
            if (double.TryParse(expression, out var number))
                return number;

            // 检查是否是布尔值
            if (bool.TryParse(expression, out var boolValue))
                return boolValue;

            // 返回原始字符串
            return expression;
        }

        /// <summary>
        /// 获取下一个节点
        /// </summary>
        private List<string> GetNextNodes(string currentNodeName, object executionResult = null)
        {
            var nextNodes = new List<string>();

            // 查找从当前节点出发的连接
            var outgoingConnections = _connections
                .Where(c => c.SourceNode == currentNodeName)
                .ToList();

            if (_nodes.TryGetValue(currentNodeName, out var currentNode))
            {
                // 判断节点：根据条件选择分支
                if (currentNode.Data?.Type == FlowNodeType.Decision)
                {
                    var result = executionResult is bool b ? b : true;
                    
                    // 查找对应的分支
                    foreach (var conn in outgoingConnections)
                    {
                        var label = conn.Label?.ToLower() ?? "";
                        if ((result && (label == "true" || label == "是" || string.IsNullOrEmpty(label))) ||
                            (!result && (label == "false" || label == "否")))
                        {
                            nextNodes.Add(conn.TargetNode);
                            break;
                        }
                    }

                    // 如果没找到匹配的，取第一个
                    if (nextNodes.Count == 0 && outgoingConnections.Count > 0)
                    {
                        nextNodes.Add(outgoingConnections[0].TargetNode);
                    }
                }
                else
                {
                    // 其他节点：执行所有连接的目标节点
                    nextNodes.AddRange(outgoingConnections.Select(c => c.TargetNode));
                }
            }

            return nextNodes;
        }

        #endregion

        #region 辅助方法

        private void OnNodeExecuted(string nodeName, NodeExecutionStatus status, 
            object input, object output, Exception error, TimeSpan duration = default)
        {
            NodeExecuted?.Invoke(this, new NodeExecutionEventArgs
            {
                NodeName = nodeName,
                Status = status,
                Input = input,
                Output = output,
                Error = error,
                Duration = duration
            });
        }

        private string GetStateMessage(ExecutionState state)
        {
            switch (state)
            {
                case ExecutionState.Idle: return "就绪";
                case ExecutionState.Running: return "运行中";
                case ExecutionState.Paused: return $"暂停于 {_currentNodeName}";
                case ExecutionState.Stepping: return $"单步执行 {_currentNodeName}";
                case ExecutionState.Completed: return "执行完成";
                case ExecutionState.Failed: return "执行失败";
                case ExecutionState.Cancelled: return "已取消";
                default: return state.ToString();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _pauseEvent?.Dispose();
        }

        #endregion
    }

    #region 执行结果

    /// <summary>
    /// 节点执行结果
    /// </summary>
    public class NodeExecutionResult
    {
        public string NodeName { get; set; }
        public NodeExecutionStatus Status { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public Exception Error { get; set; }
        public TimeSpan Duration { get; set; }
    }

    #endregion
}
