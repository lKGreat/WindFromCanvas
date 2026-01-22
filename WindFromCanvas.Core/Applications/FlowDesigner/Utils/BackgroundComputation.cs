using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using WindFromCanvas.Core.Applications.FlowDesigner.Algorithms;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 5.5.1 后台计算管理器
    /// 将重型计算移出主线程，避免UI卡顿
    /// </summary>
    public class BackgroundComputation : IDisposable
    {
        private static BackgroundComputation _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static BackgroundComputation Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new BackgroundComputation();
                        }
                    }
                }
                return _instance;
            }
        }

        // 任务队列
        private readonly ConcurrentQueue<ComputationTask> _taskQueue = new ConcurrentQueue<ComputationTask>();
        
        // 正在运行的任务
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningTasks = new ConcurrentDictionary<string, CancellationTokenSource>();
        
        // 工作线程
        private readonly List<Thread> _workers = new List<Thread>();
        
        // 最大并行任务数
        private readonly int _maxConcurrency;
        
        // 是否已释放
        private bool _disposed = false;
        
        // 是否正在运行
        private volatile bool _isRunning = true;
        
        // 等待信号
        private readonly AutoResetEvent _workAvailable = new AutoResetEvent(false);

        // A* 路由器实例
        private readonly AStarRouter _router = new AStarRouter();
        
        // 布局算法实例
        private readonly DagreLayout _layoutAlgorithm = new DagreLayout();

        /// <summary>
        /// 任务完成事件
        /// </summary>
        public event EventHandler<ComputationCompletedEventArgs> TaskCompleted;

        /// <summary>
        /// 任务错误事件
        /// </summary>
        public event EventHandler<ComputationErrorEventArgs> TaskError;

        /// <summary>
        /// 任务进度事件
        /// </summary>
        public event EventHandler<ComputationProgressEventArgs> TaskProgress;

        private BackgroundComputation()
        {
            _maxConcurrency = Math.Max(1, Environment.ProcessorCount - 1);
            StartWorkers();
        }

        /// <summary>
        /// 启动工作线程
        /// </summary>
        private void StartWorkers()
        {
            for (int i = 0; i < _maxConcurrency; i++)
            {
                var worker = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = $"BackgroundComputation-Worker-{i}"
                };
                _workers.Add(worker);
                worker.Start();
            }
        }

        /// <summary>
        /// 工作线程循环
        /// </summary>
        private void WorkerLoop()
        {
            while (_isRunning)
            {
                _workAvailable.WaitOne(100);
                
                while (_taskQueue.TryDequeue(out var task))
                {
                    if (!_isRunning) break;
                    
                    try
                    {
                        ExecuteTask(task);
                    }
                    catch (Exception ex)
                    {
                        OnTaskError(task.TaskId, ex);
                    }
                }
            }
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        private void ExecuteTask(ComputationTask task)
        {
            if (task.CancellationToken.IsCancellationRequested)
                return;

            switch (task.TaskType)
            {
                case ComputationTaskType.PathFinding:
                    ExecutePathFinding(task);
                    break;
                case ComputationTaskType.Layout:
                    ExecuteLayout(task);
                    break;
                case ComputationTaskType.BatchPathFinding:
                    ExecuteBatchPathFinding(task);
                    break;
                case ComputationTaskType.Custom:
                    ExecuteCustom(task);
                    break;
            }
        }

        #region 5.5.2 A* 路径计算

        /// <summary>
        /// 异步计算路径
        /// </summary>
        public string ComputePathAsync(PointF start, PointF end, List<RectangleF> obstacles, Action<List<PointF>> callback)
        {
            var taskId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            _runningTasks[taskId] = cts;

            var task = new ComputationTask
            {
                TaskId = taskId,
                TaskType = ComputationTaskType.PathFinding,
                Parameters = new PathFindingParams
                {
                    Start = start,
                    End = end,
                    Obstacles = obstacles ?? new List<RectangleF>()
                },
                Callback = callback,
                CancellationToken = cts.Token
            };

            _taskQueue.Enqueue(task);
            _workAvailable.Set();

            return taskId;
        }

        private void ExecutePathFinding(ComputationTask task)
        {
            var parameters = task.Parameters as PathFindingParams;
            if (parameters == null) return;

            var path = _router.FindPath(parameters.Start, parameters.End, parameters.Obstacles);
            
            _runningTasks.TryRemove(task.TaskId, out _);
            
            if (!task.CancellationToken.IsCancellationRequested)
            {
                OnTaskCompleted(task.TaskId, ComputationTaskType.PathFinding, path);
                (task.Callback as Action<List<PointF>>)?.Invoke(path);
            }
        }

        /// <summary>
        /// 批量异步计算路径
        /// </summary>
        public string ComputePathsBatchAsync(List<(PointF Start, PointF End)> pathRequests, List<RectangleF> obstacles, Action<List<List<PointF>>> callback)
        {
            var taskId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            _runningTasks[taskId] = cts;

            var task = new ComputationTask
            {
                TaskId = taskId,
                TaskType = ComputationTaskType.BatchPathFinding,
                Parameters = new BatchPathFindingParams
                {
                    PathRequests = pathRequests,
                    Obstacles = obstacles ?? new List<RectangleF>()
                },
                Callback = callback,
                CancellationToken = cts.Token
            };

            _taskQueue.Enqueue(task);
            _workAvailable.Set();

            return taskId;
        }

        private void ExecuteBatchPathFinding(ComputationTask task)
        {
            var parameters = task.Parameters as BatchPathFindingParams;
            if (parameters == null) return;

            var results = new List<List<PointF>>();
            var total = parameters.PathRequests.Count;
            var completed = 0;

            foreach (var request in parameters.PathRequests)
            {
                if (task.CancellationToken.IsCancellationRequested)
                    break;

                var path = _router.FindPath(request.Start, request.End, parameters.Obstacles);
                results.Add(path);

                completed++;
                OnTaskProgress(task.TaskId, completed, total);
            }

            _runningTasks.TryRemove(task.TaskId, out _);

            if (!task.CancellationToken.IsCancellationRequested)
            {
                OnTaskCompleted(task.TaskId, ComputationTaskType.BatchPathFinding, results);
                (task.Callback as Action<List<List<PointF>>>)?.Invoke(results);
            }
        }

        #endregion

        #region 5.5.3 布局计算

        /// <summary>
        /// 异步计算布局
        /// </summary>
        public string ComputeLayoutAsync(FlowGraph graph, LayoutOptions options, Action<LayoutResult> callback)
        {
            var taskId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            _runningTasks[taskId] = cts;

            var task = new ComputationTask
            {
                TaskId = taskId,
                TaskType = ComputationTaskType.Layout,
                Parameters = new LayoutParams
                {
                    Graph = graph,
                    Options = options ?? LayoutOptions.Default
                },
                Callback = callback,
                CancellationToken = cts.Token
            };

            _taskQueue.Enqueue(task);
            _workAvailable.Set();

            return taskId;
        }

        private void ExecuteLayout(ComputationTask task)
        {
            var parameters = task.Parameters as LayoutParams;
            if (parameters == null) return;

            var result = _layoutAlgorithm.ApplyLayout(parameters.Graph, parameters.Options);

            _runningTasks.TryRemove(task.TaskId, out _);

            if (!task.CancellationToken.IsCancellationRequested)
            {
                OnTaskCompleted(task.TaskId, ComputationTaskType.Layout, result);
                (task.Callback as Action<LayoutResult>)?.Invoke(result);
            }
        }

        #endregion

        #region 自定义计算

        /// <summary>
        /// 执行自定义后台计算
        /// </summary>
        public string ExecuteAsync<TResult>(Func<CancellationToken, TResult> computation, Action<TResult> callback)
        {
            var taskId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            _runningTasks[taskId] = cts;

            var task = new ComputationTask
            {
                TaskId = taskId,
                TaskType = ComputationTaskType.Custom,
                Parameters = new CustomParams<TResult>
                {
                    Computation = computation,
                    Callback = callback
                },
                CancellationToken = cts.Token
            };

            _taskQueue.Enqueue(task);
            _workAvailable.Set();

            return taskId;
        }

        private void ExecuteCustom(ComputationTask task)
        {
            var parameters = task.Parameters;
            if (parameters == null) return;

            // 使用反射调用泛型方法
            var paramsType = parameters.GetType();
            if (paramsType.IsGenericType && paramsType.GetGenericTypeDefinition() == typeof(CustomParams<>))
            {
                var computationProp = paramsType.GetProperty("Computation");
                var callbackProp = paramsType.GetProperty("Callback");
                
                var computation = computationProp?.GetValue(parameters) as Delegate;
                var callback = callbackProp?.GetValue(parameters) as Delegate;

                if (computation != null)
                {
                    try
                    {
                        var result = computation.DynamicInvoke(task.CancellationToken);
                        
                        _runningTasks.TryRemove(task.TaskId, out _);

                        if (!task.CancellationToken.IsCancellationRequested)
                        {
                            OnTaskCompleted(task.TaskId, ComputationTaskType.Custom, result);
                            callback?.DynamicInvoke(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _runningTasks.TryRemove(task.TaskId, out _);
                        OnTaskError(task.TaskId, ex.InnerException ?? ex);
                    }
                }
            }
        }

        #endregion

        #region 5.5.5 任务取消

        /// <summary>
        /// 取消任务
        /// </summary>
        public bool CancelTask(string taskId)
        {
            if (_runningTasks.TryRemove(taskId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAllTasks()
        {
            foreach (var kvp in _runningTasks)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _runningTasks.Clear();

            // 清空队列
            while (_taskQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 检查任务是否正在运行
        /// </summary>
        public bool IsTaskRunning(string taskId)
        {
            return _runningTasks.ContainsKey(taskId);
        }

        /// <summary>
        /// 获取正在运行的任务数
        /// </summary>
        public int RunningTaskCount => _runningTasks.Count;

        /// <summary>
        /// 获取队列中等待的任务数
        /// </summary>
        public int QueuedTaskCount => _taskQueue.Count;

        #endregion

        #region 事件触发

        private void OnTaskCompleted(string taskId, ComputationTaskType taskType, object result)
        {
            TaskCompleted?.Invoke(this, new ComputationCompletedEventArgs
            {
                TaskId = taskId,
                TaskType = taskType,
                Result = result
            });
        }

        private void OnTaskError(string taskId, Exception error)
        {
            TaskError?.Invoke(this, new ComputationErrorEventArgs
            {
                TaskId = taskId,
                Error = error
            });
        }

        private void OnTaskProgress(string taskId, int completed, int total)
        {
            TaskProgress?.Invoke(this, new ComputationProgressEventArgs
            {
                TaskId = taskId,
                Completed = completed,
                Total = total,
                ProgressPercent = total > 0 ? (float)completed / total * 100 : 0
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _isRunning = false;
            CancelAllTasks();
            
            // 唤醒所有工作线程以便退出
            for (int i = 0; i < _maxConcurrency; i++)
            {
                _workAvailable.Set();
            }

            // 等待工作线程退出
            foreach (var worker in _workers)
            {
                worker.Join(1000);
            }

            _workAvailable.Dispose();
        }

        #endregion
    }

    #region 任务类型和参数

    /// <summary>
    /// 计算任务类型
    /// </summary>
    public enum ComputationTaskType
    {
        PathFinding,
        BatchPathFinding,
        Layout,
        Custom
    }

    /// <summary>
    /// 计算任务
    /// </summary>
    internal class ComputationTask
    {
        public string TaskId { get; set; }
        public ComputationTaskType TaskType { get; set; }
        public object Parameters { get; set; }
        public object Callback { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    /// 路径查找参数
    /// </summary>
    internal class PathFindingParams
    {
        public PointF Start { get; set; }
        public PointF End { get; set; }
        public List<RectangleF> Obstacles { get; set; }
    }

    /// <summary>
    /// 批量路径查找参数
    /// </summary>
    internal class BatchPathFindingParams
    {
        public List<(PointF Start, PointF End)> PathRequests { get; set; }
        public List<RectangleF> Obstacles { get; set; }
    }

    /// <summary>
    /// 布局参数
    /// </summary>
    internal class LayoutParams
    {
        public FlowGraph Graph { get; set; }
        public LayoutOptions Options { get; set; }
    }

    /// <summary>
    /// 自定义计算参数
    /// </summary>
    internal class CustomParams<TResult>
    {
        public Func<CancellationToken, TResult> Computation { get; set; }
        public Action<TResult> Callback { get; set; }
    }

    #endregion

    #region 事件参数

    /// <summary>
    /// 计算完成事件参数
    /// </summary>
    public class ComputationCompletedEventArgs : EventArgs
    {
        public string TaskId { get; set; }
        public ComputationTaskType TaskType { get; set; }
        public object Result { get; set; }
    }

    /// <summary>
    /// 计算错误事件参数
    /// </summary>
    public class ComputationErrorEventArgs : EventArgs
    {
        public string TaskId { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// 计算进度事件参数
    /// </summary>
    public class ComputationProgressEventArgs : EventArgs
    {
        public string TaskId { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
        public float ProgressPercent { get; set; }
    }

    #endregion

    /// <summary>
    /// 5.5.4 计算结果回调帮助类（用于UI线程同步）
    /// </summary>
    public static class ComputationCallback
    {
        /// <summary>
        /// 在UI线程上执行回调
        /// </summary>
        public static Action<T> OnUIThread<T>(System.Windows.Forms.Control control, Action<T> callback)
        {
            return result =>
            {
                if (control == null || control.IsDisposed)
                    return;

                if (control.InvokeRequired)
                {
                    control.BeginInvoke(new Action(() => callback?.Invoke(result)));
                }
                else
                {
                    callback?.Invoke(result);
                }
            };
        }

        /// <summary>
        /// 创建带错误处理的回调
        /// </summary>
        public static Action<T> WithErrorHandling<T>(Action<T> callback, Action<Exception> errorHandler)
        {
            return result =>
            {
                try
                {
                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke(ex);
                }
            };
        }
    }
}
