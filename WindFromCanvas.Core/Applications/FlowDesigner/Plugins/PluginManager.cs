using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins
{
    /// <summary>
    /// 插件管理器
    /// </summary>
    public class PluginManager
    {
        private readonly Dictionary<string, IFlowPlugin> _plugins = new Dictionary<string, IFlowPlugin>();
        private readonly PluginContext _context;

        public PluginManager(BuilderStateStore stateStore)
        {
            _context = new PluginContext(stateStore);
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        public void LoadPlugin(IFlowPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            if (string.IsNullOrEmpty(plugin.PluginName))
            {
                throw new ArgumentException("Plugin name cannot be null or empty", nameof(plugin));
            }

            if (_plugins.ContainsKey(plugin.PluginName))
            {
                throw new InvalidOperationException($"Plugin '{plugin.PluginName}' is already loaded");
            }

            try
            {
                plugin.Initialize(_context);
                _plugins[plugin.PluginName] = plugin;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize plugin '{plugin.PluginName}'", ex);
            }
        }

        /// <summary>
        /// 卸载插件
        /// </summary>
        public void UnloadPlugin(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (_plugins.TryGetValue(name, out var plugin))
            {
                try
                {
                    plugin.Destroy();
                }
                catch (Exception ex)
                {
                    // 记录错误但不抛出异常
                    System.Diagnostics.Debug.WriteLine($"Error destroying plugin '{name}': {ex.Message}");
                }
                finally
                {
                    _plugins.Remove(name);
                    _context.UnregisterPlugin(name);
                }
            }
        }

        /// <summary>
        /// 获取插件
        /// </summary>
        public IFlowPlugin GetPlugin(string name)
        {
            _plugins.TryGetValue(name, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 获取所有已加载的插件
        /// </summary>
        public IEnumerable<IFlowPlugin> GetAllPlugins()
        {
            return _plugins.Values;
        }

        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        public bool IsPluginLoaded(string name)
        {
            return _plugins.ContainsKey(name);
        }

        /// <summary>
        /// 渲染所有插件的UI
        /// </summary>
        public void RenderPlugins(System.Drawing.Graphics g, System.Drawing.RectangleF viewport)
        {
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Render(g, viewport);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error rendering plugin '{plugin.PluginName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取插件上下文（供外部访问）
        /// </summary>
        public IPluginContext Context => _context;
    }
}
