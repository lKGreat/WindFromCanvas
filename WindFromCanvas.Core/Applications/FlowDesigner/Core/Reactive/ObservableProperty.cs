using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Reactive
{
    /// <summary>
    /// 可观察属性（模拟MobX的@observable）
    /// </summary>
    public class ObservableProperty<T> : INotifyPropertyChanged, IObservable
    {
        private T _value;
        private readonly List<Action<T, T>> _observers = new List<Action<T, T>>();
        private readonly List<Action> _changeObservers = new List<Action>();
        private bool _isNotifying;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableProperty()
        {
        }

        public ObservableProperty(T initialValue)
        {
            _value = initialValue;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    var oldValue = _value;
                    _value = value;
                    NotifyObservers(oldValue, value);
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        /// <summary>
        /// 订阅值变化事件
        /// </summary>
        public IDisposable Subscribe(Action<T, T> observer)
        {
            _observers.Add(observer);
            return new Subscription(() => _observers.Remove(observer));
        }

        /// <summary>
        /// 订阅变化事件（不传递旧值和新值）
        /// </summary>
        public IDisposable Subscribe(Action observer)
        {
            _changeObservers.Add(observer);
            return new Subscription(() => _changeObservers.Remove(observer));
        }

        /// <summary>
        /// 静默设置值（不触发通知）
        /// </summary>
        public void SetValueSilent(T value)
        {
            _value = value;
        }

        /// <summary>
        /// 手动触发通知
        /// </summary>
        public void Notify()
        {
            NotifyObservers(_value, _value);
        }

        private void NotifyObservers(T oldValue, T newValue)
        {
            if (_isNotifying) return; // 防止循环通知

            _isNotifying = true;
            try
            {
                foreach (var observer in _observers)
                {
                    observer?.Invoke(oldValue, newValue);
                }

                foreach (var observer in _changeObservers)
                {
                    observer?.Invoke();
                }
            }
            finally
            {
                _isNotifying = false;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static implicit operator T(ObservableProperty<T> property)
        {
            return property.Value;
        }

        private class Subscription : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;

            public Subscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _unsubscribe?.Invoke();
                    _disposed = true;
                }
            }
        }
    }
}
