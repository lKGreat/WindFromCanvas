using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Algorithms;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Interaction
{
    /// <summary>
    /// 5.4.1 画布级事件委托系统
    /// 通过在画布级别统一处理事件，减少事件监听器数量，提高性能
    /// </summary>
    public class EventDelegation
    {
        private readonly Control _canvas;
        private readonly QuadTree<EventTarget> _targetTree;
        private readonly Dictionary<string, EventTarget> _targets = new Dictionary<string, EventTarget>();
        
        // 5.4.3 事件防抖
        private readonly EventDebouncer _debouncer = new EventDebouncer();
        
        // 5.4.4 高频事件节流
        private readonly EventThrottler _throttler = new EventThrottler();
        
        // 当前悬停的目标
        private EventTarget _currentHoverTarget;
        
        // 当前按下的目标
        private EventTarget _currentPressedTarget;
        
        // 事件处理器注册表
        private readonly Dictionary<EventType, List<EventHandler<CanvasEventArgs>>> _globalHandlers = new Dictionary<EventType, List<EventHandler<CanvasEventArgs>>>();

        /// <summary>
        /// 初始化事件委托系统
        /// </summary>
        public EventDelegation(Control canvas, RectangleF bounds)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _targetTree = new QuadTree<EventTarget>(bounds);
            
            // 订阅画布事件
            AttachCanvasEvents();
        }

        /// <summary>
        /// 5.4.1 附加画布事件
        /// </summary>
        private void AttachCanvasEvents()
        {
            _canvas.MouseDown += OnCanvasMouseDown;
            _canvas.MouseMove += OnCanvasMouseMove;
            _canvas.MouseUp += OnCanvasMouseUp;
            _canvas.MouseClick += OnCanvasMouseClick;
            _canvas.MouseDoubleClick += OnCanvasMouseDoubleClick;
            _canvas.MouseWheel += OnCanvasMouseWheel;
            _canvas.MouseEnter += OnCanvasMouseEnter;
            _canvas.MouseLeave += OnCanvasMouseLeave;
            _canvas.KeyDown += OnCanvasKeyDown;
            _canvas.KeyUp += OnCanvasKeyUp;
        }

        /// <summary>
        /// 分离画布事件
        /// </summary>
        public void DetachCanvasEvents()
        {
            _canvas.MouseDown -= OnCanvasMouseDown;
            _canvas.MouseMove -= OnCanvasMouseMove;
            _canvas.MouseUp -= OnCanvasMouseUp;
            _canvas.MouseClick -= OnCanvasMouseClick;
            _canvas.MouseDoubleClick -= OnCanvasMouseDoubleClick;
            _canvas.MouseWheel -= OnCanvasMouseWheel;
            _canvas.MouseEnter -= OnCanvasMouseEnter;
            _canvas.MouseLeave -= OnCanvasMouseLeave;
            _canvas.KeyDown -= OnCanvasKeyDown;
            _canvas.KeyUp -= OnCanvasKeyUp;
        }

        /// <summary>
        /// 注册事件目标
        /// </summary>
        public void RegisterTarget(EventTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.Id)) return;
            
            _targets[target.Id] = target;
            _targetTree.Insert(target);
        }

        /// <summary>
        /// 批量注册事件目标
        /// </summary>
        public void RegisterTargets(IEnumerable<EventTarget> targets)
        {
            if (targets == null) return;
            
            var targetList = targets.ToList();
            foreach (var target in targetList)
            {
                if (target != null && !string.IsNullOrEmpty(target.Id))
                {
                    _targets[target.Id] = target;
                }
            }
            _targetTree.InsertRange(targetList.Where(t => t != null));
        }

        /// <summary>
        /// 注销事件目标
        /// </summary>
        public void UnregisterTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            
            if (_targets.TryGetValue(targetId, out var target))
            {
                _targets.Remove(targetId);
                _targetTree.Remove(target);
                
                if (_currentHoverTarget?.Id == targetId)
                    _currentHoverTarget = null;
                if (_currentPressedTarget?.Id == targetId)
                    _currentPressedTarget = null;
            }
        }

        /// <summary>
        /// 更新事件目标的边界
        /// </summary>
        public void UpdateTargetBounds(string targetId, RectangleF newBounds)
        {
            if (_targets.TryGetValue(targetId, out var target))
            {
                var oldBounds = target.Bounds;
                target.SetBounds(newBounds);
                _targetTree.Update(target, oldBounds);
            }
        }

        /// <summary>
        /// 注册全局事件处理器
        /// </summary>
        public void AddGlobalHandler(EventType eventType, EventHandler<CanvasEventArgs> handler)
        {
            if (!_globalHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<EventHandler<CanvasEventArgs>>();
                _globalHandlers[eventType] = handlers;
            }
            handlers.Add(handler);
        }

        /// <summary>
        /// 移除全局事件处理器
        /// </summary>
        public void RemoveGlobalHandler(EventType eventType, EventHandler<CanvasEventArgs> handler)
        {
            if (_globalHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// 5.4.2 事件目标识别
        /// </summary>
        private EventTarget HitTest(PointF point)
        {
            // 使用四叉树快速查询
            var candidates = _targetTree.Query(point, 1f);
            
            // 按层级排序（Z-Index），返回最上层的目标
            return candidates
                .Where(t => t.HitTest(point))
                .OrderByDescending(t => t.ZIndex)
                .FirstOrDefault();
        }

        /// <summary>
        /// 查询区域内的所有目标
        /// </summary>
        public List<EventTarget> QueryTargets(RectangleF area)
        {
            return _targetTree.Query(area);
        }

        #region 鼠标事件处理

        private void OnCanvasMouseDown(object sender, MouseEventArgs e)
        {
            var point = new PointF(e.X, e.Y);
            var target = HitTest(point);
            
            _currentPressedTarget = target;
            
            var args = CreateEventArgs(EventType.MouseDown, point, e);
            args.Target = target;
            
            // 先触发全局处理器
            TriggerGlobalHandlers(EventType.MouseDown, args);
            
            // 再触发目标处理器
            target?.OnMouseDown(args);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var point = new PointF(e.X, e.Y);
            
            // 5.4.5 节流高频事件
            if (!_throttler.ShouldProcess("MouseMove", 16)) // ~60fps
                return;

            var target = HitTest(point);
            var args = CreateEventArgs(EventType.MouseMove, point, e);
            args.Target = target;
            
            // 处理进入/离开事件
            if (target != _currentHoverTarget)
            {
                if (_currentHoverTarget != null)
                {
                    var leaveArgs = CreateEventArgs(EventType.MouseLeave, point, e);
                    leaveArgs.Target = _currentHoverTarget;
                    _currentHoverTarget.OnMouseLeave(leaveArgs);
                    TriggerGlobalHandlers(EventType.MouseLeave, leaveArgs);
                }
                
                if (target != null)
                {
                    var enterArgs = CreateEventArgs(EventType.MouseEnter, point, e);
                    enterArgs.Target = target;
                    target.OnMouseEnter(enterArgs);
                    TriggerGlobalHandlers(EventType.MouseEnter, enterArgs);
                }
                
                _currentHoverTarget = target;
            }
            
            // 触发移动事件
            TriggerGlobalHandlers(EventType.MouseMove, args);
            target?.OnMouseMove(args);
            
            // 如果正在按下，触发拖拽
            if (_currentPressedTarget != null && e.Button != MouseButtons.None)
            {
                var dragArgs = CreateEventArgs(EventType.Drag, point, e);
                dragArgs.Target = _currentPressedTarget;
                _currentPressedTarget.OnDrag(dragArgs);
                TriggerGlobalHandlers(EventType.Drag, dragArgs);
            }
        }

        private void OnCanvasMouseUp(object sender, MouseEventArgs e)
        {
            var point = new PointF(e.X, e.Y);
            var target = HitTest(point);
            
            var args = CreateEventArgs(EventType.MouseUp, point, e);
            args.Target = target;
            
            TriggerGlobalHandlers(EventType.MouseUp, args);
            target?.OnMouseUp(args);
            
            // 触发放置事件
            if (_currentPressedTarget != null && target != _currentPressedTarget)
            {
                var dropArgs = CreateEventArgs(EventType.Drop, point, e);
                dropArgs.Target = target;
                dropArgs.SourceTarget = _currentPressedTarget;
                target?.OnDrop(dropArgs);
                TriggerGlobalHandlers(EventType.Drop, dropArgs);
            }
            
            _currentPressedTarget = null;
        }

        private void OnCanvasMouseClick(object sender, MouseEventArgs e)
        {
            var point = new PointF(e.X, e.Y);
            var target = HitTest(point);
            
            var args = CreateEventArgs(EventType.Click, point, e);
            args.Target = target;
            
            TriggerGlobalHandlers(EventType.Click, args);
            target?.OnClick(args);
        }

        private void OnCanvasMouseDoubleClick(object sender, MouseEventArgs e)
        {
            var point = new PointF(e.X, e.Y);
            var target = HitTest(point);
            
            var args = CreateEventArgs(EventType.DoubleClick, point, e);
            args.Target = target;
            
            TriggerGlobalHandlers(EventType.DoubleClick, args);
            target?.OnDoubleClick(args);
        }

        private void OnCanvasMouseWheel(object sender, MouseEventArgs e)
        {
            var point = new PointF(e.X, e.Y);
            
            // 5.4.3 防抖滚轮事件
            _debouncer.Debounce("MouseWheel", 50, () =>
            {
                var target = HitTest(point);
                var args = CreateEventArgs(EventType.MouseWheel, point, e);
                args.Target = target;
                args.WheelDelta = e.Delta;
                
                TriggerGlobalHandlers(EventType.MouseWheel, args);
                target?.OnMouseWheel(args);
            });
        }

        private void OnCanvasMouseEnter(object sender, EventArgs e)
        {
            var args = new CanvasEventArgs { EventType = EventType.CanvasEnter };
            TriggerGlobalHandlers(EventType.CanvasEnter, args);
        }

        private void OnCanvasMouseLeave(object sender, EventArgs e)
        {
            if (_currentHoverTarget != null)
            {
                var leaveArgs = new CanvasEventArgs
                {
                    EventType = EventType.MouseLeave,
                    Target = _currentHoverTarget
                };
                _currentHoverTarget.OnMouseLeave(leaveArgs);
                _currentHoverTarget = null;
            }
            
            var args = new CanvasEventArgs { EventType = EventType.CanvasLeave };
            TriggerGlobalHandlers(EventType.CanvasLeave, args);
        }

        #endregion

        #region 键盘事件处理

        private void OnCanvasKeyDown(object sender, KeyEventArgs e)
        {
            var args = new CanvasEventArgs
            {
                EventType = EventType.KeyDown,
                Key = e.KeyCode,
                Modifiers = e.Modifiers,
                Target = _currentHoverTarget
            };
            
            TriggerGlobalHandlers(EventType.KeyDown, args);
            _currentHoverTarget?.OnKeyDown(args);
            
            e.Handled = args.Handled;
        }

        private void OnCanvasKeyUp(object sender, KeyEventArgs e)
        {
            var args = new CanvasEventArgs
            {
                EventType = EventType.KeyUp,
                Key = e.KeyCode,
                Modifiers = e.Modifiers,
                Target = _currentHoverTarget
            };
            
            TriggerGlobalHandlers(EventType.KeyUp, args);
            _currentHoverTarget?.OnKeyUp(args);
            
            e.Handled = args.Handled;
        }

        #endregion

        private CanvasEventArgs CreateEventArgs(EventType eventType, PointF point, MouseEventArgs e)
        {
            return new CanvasEventArgs
            {
                EventType = eventType,
                Position = point,
                Button = e.Button,
                Clicks = e.Clicks,
                Modifiers = Control.ModifierKeys
            };
        }

        private void TriggerGlobalHandlers(EventType eventType, CanvasEventArgs args)
        {
            if (_globalHandlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers.ToList())
                {
                    handler?.Invoke(this, args);
                    if (args.StopPropagation) break;
                }
            }
        }

        /// <summary>
        /// 清除所有目标
        /// </summary>
        public void Clear()
        {
            _targets.Clear();
            _targetTree.Clear();
            _currentHoverTarget = null;
            _currentPressedTarget = null;
        }

        /// <summary>
        /// 重建四叉树（当大量目标位置改变时调用）
        /// </summary>
        public void RebuildIndex()
        {
            _targetTree.Rebuild();
        }
    }

    /// <summary>
    /// 事件目标基类
    /// </summary>
    public class EventTarget : IBoundable
    {
        public string Id { get; set; }
        public RectangleF Bounds { get; private set; }
        public int ZIndex { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        public object Tag { get; set; }

        public EventTarget(string id, RectangleF bounds)
        {
            Id = id;
            Bounds = bounds;
        }

        public void SetBounds(RectangleF bounds)
        {
            Bounds = bounds;
        }

        /// <summary>
        /// 命中测试
        /// </summary>
        public virtual bool HitTest(PointF point)
        {
            return IsEnabled && Bounds.Contains(point);
        }

        // 事件处理回调
        public event EventHandler<CanvasEventArgs> MouseDown;
        public event EventHandler<CanvasEventArgs> MouseMove;
        public event EventHandler<CanvasEventArgs> MouseUp;
        public event EventHandler<CanvasEventArgs> MouseEnter;
        public event EventHandler<CanvasEventArgs> MouseLeave;
        public event EventHandler<CanvasEventArgs> Click;
        public event EventHandler<CanvasEventArgs> DoubleClick;
        public event EventHandler<CanvasEventArgs> MouseWheel;
        public event EventHandler<CanvasEventArgs> Drag;
        public event EventHandler<CanvasEventArgs> Drop;
        public event EventHandler<CanvasEventArgs> KeyDown;
        public event EventHandler<CanvasEventArgs> KeyUp;

        internal void OnMouseDown(CanvasEventArgs args) => MouseDown?.Invoke(this, args);
        internal void OnMouseMove(CanvasEventArgs args) => MouseMove?.Invoke(this, args);
        internal void OnMouseUp(CanvasEventArgs args) => MouseUp?.Invoke(this, args);
        internal void OnMouseEnter(CanvasEventArgs args) => MouseEnter?.Invoke(this, args);
        internal void OnMouseLeave(CanvasEventArgs args) => MouseLeave?.Invoke(this, args);
        internal void OnClick(CanvasEventArgs args) => Click?.Invoke(this, args);
        internal void OnDoubleClick(CanvasEventArgs args) => DoubleClick?.Invoke(this, args);
        internal void OnMouseWheel(CanvasEventArgs args) => MouseWheel?.Invoke(this, args);
        internal void OnDrag(CanvasEventArgs args) => Drag?.Invoke(this, args);
        internal void OnDrop(CanvasEventArgs args) => Drop?.Invoke(this, args);
        internal void OnKeyDown(CanvasEventArgs args) => KeyDown?.Invoke(this, args);
        internal void OnKeyUp(CanvasEventArgs args) => KeyUp?.Invoke(this, args);
    }

    /// <summary>
    /// 画布事件参数
    /// </summary>
    public class CanvasEventArgs : EventArgs
    {
        public EventType EventType { get; set; }
        public PointF Position { get; set; }
        public MouseButtons Button { get; set; }
        public int Clicks { get; set; }
        public Keys Modifiers { get; set; }
        public Keys Key { get; set; }
        public int WheelDelta { get; set; }
        public EventTarget Target { get; set; }
        public EventTarget SourceTarget { get; set; }
        public bool Handled { get; set; }
        public bool StopPropagation { get; set; }
    }

    /// <summary>
    /// 事件类型
    /// </summary>
    public enum EventType
    {
        MouseDown,
        MouseMove,
        MouseUp,
        MouseEnter,
        MouseLeave,
        Click,
        DoubleClick,
        MouseWheel,
        Drag,
        Drop,
        KeyDown,
        KeyUp,
        CanvasEnter,
        CanvasLeave
    }

    /// <summary>
    /// 5.4.3 事件防抖器
    /// </summary>
    public class EventDebouncer
    {
        private readonly Dictionary<string, System.Threading.Timer> _timers = new Dictionary<string, System.Threading.Timer>();
        private readonly object _lock = new object();

        /// <summary>
        /// 防抖执行
        /// </summary>
        public void Debounce(string key, int delayMs, Action action)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(key, out var existingTimer))
                {
                    existingTimer.Dispose();
                }

                _timers[key] = new System.Threading.Timer(_ =>
                {
                    lock (_lock)
                    {
                        _timers.Remove(key);
                    }
                    action?.Invoke();
                }, null, delayMs, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// 取消防抖
        /// </summary>
        public void Cancel(string key)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(key, out var timer))
                {
                    timer.Dispose();
                    _timers.Remove(key);
                }
            }
        }

        /// <summary>
        /// 清除所有
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Dispose();
                }
                _timers.Clear();
            }
        }
    }

    /// <summary>
    /// 5.4.4 事件节流器
    /// </summary>
    public class EventThrottler
    {
        private readonly Dictionary<string, DateTime> _lastExecutionTimes = new Dictionary<string, DateTime>();
        private readonly object _lock = new object();

        /// <summary>
        /// 检查是否应该处理事件
        /// </summary>
        public bool ShouldProcess(string key, int intervalMs)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                if (_lastExecutionTimes.TryGetValue(key, out var lastTime))
                {
                    if ((now - lastTime).TotalMilliseconds < intervalMs)
                    {
                        return false;
                    }
                }
                
                _lastExecutionTimes[key] = now;
                return true;
            }
        }

        /// <summary>
        /// 节流执行
        /// </summary>
        public void Throttle(string key, int intervalMs, Action action)
        {
            if (ShouldProcess(key, intervalMs))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// 重置节流器
        /// </summary>
        public void Reset(string key)
        {
            lock (_lock)
            {
                _lastExecutionTimes.Remove(key);
            }
        }

        /// <summary>
        /// 清除所有
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _lastExecutionTimes.Clear();
            }
        }
    }
}
