using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Reactive
{
    /// <summary>
    /// 响应式存储（模拟MobX的Store）
    /// </summary>
    public class ReactiveStore : INotifyPropertyChanged
    {
        private readonly Dictionary<string, IObservable> _properties = new Dictionary<string, IObservable>();
        private readonly Dictionary<string, List<Action>> _reactions = new Dictionary<string, List<Action>>();

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 创建可观察属性
        /// </summary>
        public ObservableProperty<T> Observable<T>(string name, T initialValue = default(T))
        {
            if (_properties.ContainsKey(name))
            {
                throw new InvalidOperationException($"Property '{name}' already exists");
            }

            var property = new ObservableProperty<T>(initialValue);
            _properties[name] = property;
            return property;
        }

        /// <summary>
        /// 创建计算属性
        /// </summary>
        public ComputedProperty<T> Computed<T>(string name, Func<T> computeFunction)
        {
            if (_properties.ContainsKey(name))
            {
                throw new InvalidOperationException($"Property '{name}' already exists");
            }

            var property = new ComputedProperty<T>(computeFunction);
            _properties[name] = property;
            return property;
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        public ObservableProperty<T> GetProperty<T>(string name)
        {
            if (_properties.TryGetValue(name, out var prop) && prop is ObservableProperty<T> typedProp)
            {
                return typedProp;
            }
            return null;
        }

        /// <summary>
        /// 添加反应（当依赖的属性变化时触发）
        /// </summary>
        public IDisposable Reaction(string name, Action reaction, params string[] dependencies)
        {
            if (!_reactions.ContainsKey(name))
            {
                _reactions[name] = new List<Action>();
            }

            _reactions[name].Add(reaction);

            // 订阅依赖
            var subscriptions = new List<IDisposable>();
            foreach (var depName in dependencies)
            {
                if (_properties.TryGetValue(depName, out var dep))
                {
                    var sub = dep.Subscribe(() => reaction());
                    subscriptions.Add(sub);
                }
            }

            return new CompositeSubscription(subscriptions);
        }

        /// <summary>
        /// 执行动作（模拟MobX的@action）
        /// </summary>
        public void Action(string name, Action action)
        {
            try
            {
                action?.Invoke();
                OnPropertyChanged(name);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Action '{name}' failed", ex);
            }
        }

        /// <summary>
        /// 执行动作并返回值
        /// </summary>
        public T Action<T>(string name, Func<T> action)
        {
            try
            {
                var result = action != null ? action() : default(T);
                OnPropertyChanged(name);
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Action '{name}' failed", ex);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class CompositeSubscription : IDisposable
        {
            private readonly List<IDisposable> _subscriptions;
            private bool _disposed;

            public CompositeSubscription(List<IDisposable> subscriptions)
            {
                _subscriptions = subscriptions;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    foreach (var sub in _subscriptions)
                    {
                        sub?.Dispose();
                    }
                    _disposed = true;
                }
            }
        }
    }
}
