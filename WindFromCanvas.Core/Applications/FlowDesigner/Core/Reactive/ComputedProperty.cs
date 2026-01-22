using System;
using System.Collections.Generic;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Reactive
{
    /// <summary>
    /// 计算属性（模拟MobX的@computed）
    /// </summary>
    public class ComputedProperty<T> : ObservableProperty<T>, IObservable
    {
        private readonly Func<T> _computeFunction;
        private readonly List<IObservable> _dependencies = new List<IObservable>();
        private bool _isDirty = true;
        private T _cachedValue;

        public ComputedProperty(Func<T> computeFunction)
        {
            _computeFunction = computeFunction ?? throw new ArgumentNullException(nameof(computeFunction));
        }

        public new T Value
        {
            get
            {
                if (_isDirty)
                {
                    _cachedValue = _computeFunction();
                    _isDirty = false;
                }
                return _cachedValue;
            }
        }

        /// <summary>
        /// 添加依赖
        /// </summary>
        public void AddDependency(IObservable observable)
        {
            if (!_dependencies.Contains(observable))
            {
                _dependencies.Add(observable);
                observable.Subscribe(() => MarkDirty());
            }
        }

        /// <summary>
        /// 标记为脏（需要重新计算）
        /// </summary>
        public void MarkDirty()
        {
            if (!_isDirty)
            {
                _isDirty = true;
                var oldValue = _cachedValue;
                _cachedValue = default(T);
                Notify();
            }
        }

        /// <summary>
        /// 强制重新计算
        /// </summary>
        public void Recompute()
        {
            _isDirty = true;
            var value = Value; // 触发计算
        }
    }

    /// <summary>
    /// 可观察对象接口
    /// </summary>
    public interface IObservable
    {
        IDisposable Subscribe(Action observer);
    }
}
