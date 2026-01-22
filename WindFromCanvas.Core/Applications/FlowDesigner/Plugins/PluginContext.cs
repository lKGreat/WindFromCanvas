using System;
using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins
{
    /// <summary>
    /// 插件上下文实现
    /// </summary>
    internal class PluginContext : IPluginContext
    {
        private readonly BuilderStateStore _stateStore;
        private readonly Dictionary<string, NodeTypeRegistration> _nodeTypes = new Dictionary<string, NodeTypeRegistration>();
        private readonly Dictionary<string, Type> _edgeTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, IDataAdapter> _adapters = new Dictionary<string, IDataAdapter>();
        private readonly EventBus _eventBus;

        public PluginContext(BuilderStateStore stateStore)
        {
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _eventBus = new EventBus();
        }

        public BuilderStateStore StateStore => _stateStore;

        public IEventBus EventBus => _eventBus;

        public void RegisterNodeType(string type, Type modelType, Type viewType)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException("Type cannot be null or empty", nameof(type));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (viewType == null)
            {
                throw new ArgumentNullException(nameof(viewType));
            }

            _nodeTypes[type] = new NodeTypeRegistration
            {
                Type = type,
                ModelType = modelType,
                ViewType = viewType
            };
        }

        public void RegisterEdgeType(string type, Type edgeType)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException("Type cannot be null or empty", nameof(type));
            }

            if (edgeType == null)
            {
                throw new ArgumentNullException(nameof(edgeType));
            }

            _edgeTypes[type] = edgeType;
        }

        public void RegisterAdapter(string name, IDataAdapter adapter)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }

            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            _adapters[name] = adapter;
        }

        public Dictionary<string, NodeTypeRegistration> GetRegisteredNodeTypes()
        {
            return new Dictionary<string, NodeTypeRegistration>(_nodeTypes);
        }

        public Dictionary<string, Type> GetRegisteredEdgeTypes()
        {
            return new Dictionary<string, Type>(_edgeTypes);
        }

        /// <summary>
        /// 取消注册插件（清理插件注册的类型）
        /// </summary>
        internal void UnregisterPlugin(string pluginName)
        {
            // 移除以插件名称为前缀的节点类型
            var keysToRemove = new List<string>();
            foreach (var key in _nodeTypes.Keys)
            {
                if (key.StartsWith(pluginName + ":", StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _nodeTypes.Remove(key);
            }

            // 移除以插件名称为前缀的连线类型
            keysToRemove.Clear();
            foreach (var key in _edgeTypes.Keys)
            {
                if (key.StartsWith(pluginName + ":", StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _edgeTypes.Remove(key);
            }

            // 移除适配器
            _adapters.Remove(pluginName);
        }
    }

    /// <summary>
    /// 事件总线实现
    /// </summary>
    internal class EventBus : IEventBus
    {
        private readonly Dictionary<string, List<Action<object>>> _handlers = new Dictionary<string, List<Action<object>>>();

        public void Publish(string eventName, object eventData)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (_handlers.TryGetValue(eventName, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in event handler for '{eventName}': {ex.Message}");
                    }
                }
            }
        }

        public IDisposable Subscribe(string eventName, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!_handlers.ContainsKey(eventName))
            {
                _handlers[eventName] = new List<Action<object>>();
            }

            _handlers[eventName].Add(handler);

            return new EventSubscription(() =>
            {
                if (_handlers.TryGetValue(eventName, out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                    {
                        _handlers.Remove(eventName);
                    }
                }
            });
        }

        private class EventSubscription : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;

            public EventSubscription(Action unsubscribe)
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
