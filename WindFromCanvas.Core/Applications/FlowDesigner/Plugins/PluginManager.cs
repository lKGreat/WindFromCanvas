using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins
{
    /// <summary>
    /// 6.1 增强的插件管理器
    /// 支持依赖检查、版本管理、热加载、配置持久化和错误隔离
    /// </summary>
    public class PluginManager
    {
        private readonly Dictionary<string, IFlowPlugin> _plugins = new Dictionary<string, IFlowPlugin>();
        private readonly Dictionary<string, PluginError> _pluginErrors = new Dictionary<string, PluginError>();
        private readonly PluginContext _context;
        private readonly string _configDirectory;

        /// <summary>
        /// 插件加载事件
        /// </summary>
        public event EventHandler<PluginEventArgs> PluginLoaded;

        /// <summary>
        /// 插件卸载事件
        /// </summary>
        public event EventHandler<PluginEventArgs> PluginUnloaded;

        /// <summary>
        /// 插件错误事件
        /// </summary>
        public event EventHandler<PluginErrorEventArgs> PluginError;

        /// <summary>
        /// 插件状态变更事件
        /// </summary>
        public event EventHandler<PluginStateChangedEventArgs> PluginStateChanged;

        public PluginManager(BuilderStateStore stateStore, string configDirectory = null)
        {
            _context = new PluginContext(stateStore);
            _configDirectory = configDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindFromCanvas", "Plugins");
            
            EnsureConfigDirectory();
        }

        private void EnsureConfigDirectory()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        #region 6.1.1 插件依赖检查

        /// <summary>
        /// 检查插件依赖
        /// </summary>
        public DependencyCheckResult CheckDependencies(IFlowPlugin plugin)
        {
            var result = new DependencyCheckResult { PluginName = plugin.PluginName };

            if (plugin.Dependencies == null || plugin.Dependencies.Count == 0)
            {
                result.AllSatisfied = true;
                return result;
            }

            foreach (var dependency in plugin.Dependencies)
            {
                var depPlugin = GetPlugin(dependency.PluginName);
                
                if (depPlugin == null)
                {
                    if (!dependency.IsOptional)
                    {
                        result.MissingDependencies.Add(dependency);
                    }
                }
                else if (!dependency.IsSatisfiedBy(depPlugin.Version))
                {
                    result.VersionMismatches.Add(new VersionMismatch
                    {
                        Dependency = dependency,
                        ActualVersion = depPlugin.Version
                    });
                }
                else
                {
                    result.SatisfiedDependencies.Add(dependency);
                }
            }

            result.AllSatisfied = result.MissingDependencies.Count == 0 && 
                                  result.VersionMismatches.Count == 0;
            return result;
        }

        /// <summary>
        /// 获取依赖加载顺序
        /// </summary>
        public List<IFlowPlugin> GetLoadOrder(IEnumerable<IFlowPlugin> plugins)
        {
            var sorted = new List<IFlowPlugin>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();
            var pluginMap = plugins.ToDictionary(p => p.PluginName);

            void Visit(IFlowPlugin plugin)
            {
                if (visited.Contains(plugin.PluginName))
                    return;

                if (visiting.Contains(plugin.PluginName))
                    throw new InvalidOperationException($"Circular dependency detected involving plugin '{plugin.PluginName}'");

                visiting.Add(plugin.PluginName);

                if (plugin.Dependencies != null)
                {
                    foreach (var dep in plugin.Dependencies.Where(d => !d.IsOptional))
                    {
                        if (pluginMap.TryGetValue(dep.PluginName, out var depPlugin))
                        {
                            Visit(depPlugin);
                        }
                    }
                }

                visiting.Remove(plugin.PluginName);
                visited.Add(plugin.PluginName);
                sorted.Add(plugin);
            }

            foreach (var plugin in plugins)
            {
                Visit(plugin);
            }

            return sorted;
        }

        #endregion

        #region 6.1.2 插件版本管理

        /// <summary>
        /// 检查插件版本兼容性
        /// </summary>
        public bool IsVersionCompatible(IFlowPlugin newPlugin, IFlowPlugin existingPlugin)
        {
            if (existingPlugin == null) return true;
            return newPlugin.Version >= existingPlugin.Version;
        }

        /// <summary>
        /// 获取插件版本信息
        /// </summary>
        public PluginVersionInfo GetVersionInfo(string pluginName)
        {
            if (_plugins.TryGetValue(pluginName, out var plugin))
            {
                return new PluginVersionInfo
                {
                    PluginName = plugin.PluginName,
                    Version = plugin.Version,
                    State = plugin.State,
                    DependencyCount = plugin.Dependencies?.Count ?? 0
                };
            }
            return null;
        }

        #endregion

        /// <summary>
        /// 加载插件（带依赖检查）
        /// </summary>
        public PluginLoadResult LoadPlugin(IFlowPlugin plugin)
        {
            var result = new PluginLoadResult { PluginName = plugin?.PluginName };

            if (plugin == null)
            {
                result.Success = false;
                result.ErrorMessage = "Plugin cannot be null";
                return result;
            }

            if (string.IsNullOrEmpty(plugin.PluginName))
            {
                result.Success = false;
                result.ErrorMessage = "Plugin name cannot be null or empty";
                return result;
            }

            if (_plugins.ContainsKey(plugin.PluginName))
            {
                result.Success = false;
                result.ErrorMessage = $"Plugin '{plugin.PluginName}' is already loaded";
                return result;
            }

            // 6.1.1 检查依赖
            var depCheck = CheckDependencies(plugin);
            if (!depCheck.AllSatisfied)
            {
                result.Success = false;
                result.ErrorMessage = $"Dependencies not satisfied: {string.Join(", ", depCheck.MissingDependencies.Select(d => d.PluginName))}";
                result.DependencyResult = depCheck;
                return result;
            }

            // 6.1.5 错误隔离：在try-catch中初始化
            try
            {
                // 6.1.4 加载保存的配置
                var savedConfig = LoadPluginConfiguration(plugin.PluginName);
                if (savedConfig != null)
                {
                    plugin.ApplyConfiguration(savedConfig);
                }

                plugin.Initialize(_context);
                _plugins[plugin.PluginName] = plugin;
                _pluginErrors.Remove(plugin.PluginName);

                result.Success = true;
                OnPluginLoaded(plugin);
            }
            catch (Exception ex)
            {
                // 6.1.5 记录错误但不影响其他插件
                var error = new PluginError
                {
                    PluginName = plugin.PluginName,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    OccurredAt = DateTime.UtcNow
                };
                _pluginErrors[plugin.PluginName] = error;

                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;

                OnPluginError(plugin.PluginName, ex);
            }

            return result;
        }

        /// <summary>
        /// 批量加载插件（自动排序依赖）
        /// </summary>
        public List<PluginLoadResult> LoadPlugins(IEnumerable<IFlowPlugin> plugins)
        {
            var results = new List<PluginLoadResult>();
            var sortedPlugins = GetLoadOrder(plugins);

            foreach (var plugin in sortedPlugins)
            {
                results.Add(LoadPlugin(plugin));
            }

            return results;
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
                // 检查是否有其他插件依赖此插件
                var dependents = GetDependentPlugins(name);
                if (dependents.Any())
                {
                    // 先卸载依赖的插件
                    foreach (var dependent in dependents)
                    {
                        UnloadPlugin(dependent);
                    }
                }

                try
                {
                    // 6.1.4 保存配置
                    SavePluginConfiguration(plugin.GetConfiguration());

                    plugin.Destroy();
                    OnPluginStateChanged(plugin, PluginState.Stopped);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error destroying plugin '{name}': {ex.Message}");
                    OnPluginError(name, ex);
                }
                finally
                {
                    _plugins.Remove(name);
                    _context.UnregisterPlugin(name);
                    OnPluginUnloaded(name);
                }
            }
        }

        /// <summary>
        /// 获取依赖指定插件的所有插件
        /// </summary>
        public List<string> GetDependentPlugins(string pluginName)
        {
            return _plugins.Values
                .Where(p => p.Dependencies != null && 
                           p.Dependencies.Any(d => d.PluginName == pluginName && !d.IsOptional))
                .Select(p => p.PluginName)
                .ToList();
        }

        #region 6.1.3 插件热加载

        /// <summary>
        /// 重新加载插件
        /// </summary>
        public bool ReloadPlugin(string name)
        {
            if (!_plugins.TryGetValue(name, out var plugin))
            {
                return false;
            }

            try
            {
                OnPluginStateChanged(plugin, PluginState.Reloading);
                plugin.Reload();
                OnPluginStateChanged(plugin, PluginState.Running);
                return true;
            }
            catch (Exception ex)
            {
                OnPluginError(name, ex);
                return false;
            }
        }

        /// <summary>
        /// 重新加载所有插件
        /// </summary>
        public void ReloadAllPlugins()
        {
            foreach (var pluginName in _plugins.Keys.ToList())
            {
                ReloadPlugin(pluginName);
            }
        }

        #endregion

        #region 6.1.4 配置持久化

        /// <summary>
        /// 保存插件配置
        /// </summary>
        public void SavePluginConfiguration(PluginConfiguration config)
        {
            if (config == null || string.IsNullOrEmpty(config.PluginName))
                return;

            try
            {
                var filePath = GetConfigFilePath(config.PluginName);
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving plugin config: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载插件配置
        /// </summary>
        public PluginConfiguration LoadPluginConfiguration(string pluginName)
        {
            try
            {
                var filePath = GetConfigFilePath(pluginName);
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<PluginConfiguration>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading plugin config: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 保存所有插件配置
        /// </summary>
        public void SaveAllConfigurations()
        {
            foreach (var plugin in _plugins.Values)
            {
                SavePluginConfiguration(plugin.GetConfiguration());
            }
        }

        private string GetConfigFilePath(string pluginName)
        {
            return Path.Combine(_configDirectory, $"{pluginName}.config.json");
        }

        #endregion

        #region 6.1.5 错误隔离

        /// <summary>
        /// 获取插件错误
        /// </summary>
        public PluginError GetPluginError(string pluginName)
        {
            _pluginErrors.TryGetValue(pluginName, out var error);
            return error;
        }

        /// <summary>
        /// 获取所有插件错误
        /// </summary>
        public IEnumerable<PluginError> GetAllErrors()
        {
            return _pluginErrors.Values;
        }

        /// <summary>
        /// 清除插件错误
        /// </summary>
        public void ClearPluginError(string pluginName)
        {
            _pluginErrors.Remove(pluginName);
        }

        #endregion

        /// <summary>
        /// 获取插件
        /// </summary>
        public IFlowPlugin GetPlugin(string name)
        {
            _plugins.TryGetValue(name, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 获取指定类型的插件
        /// </summary>
        public T GetPlugin<T>(string name) where T : class, IFlowPlugin
        {
            return GetPlugin(name) as T;
        }

        /// <summary>
        /// 获取所有已加载的插件
        /// </summary>
        public IEnumerable<IFlowPlugin> GetAllPlugins()
        {
            return _plugins.Values;
        }

        /// <summary>
        /// 获取指定状态的插件
        /// </summary>
        public IEnumerable<IFlowPlugin> GetPluginsByState(PluginState state)
        {
            return _plugins.Values.Where(p => p.State == state);
        }

        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        public bool IsPluginLoaded(string name)
        {
            return _plugins.ContainsKey(name);
        }

        /// <summary>
        /// 渲染所有插件的UI（带错误隔离）
        /// </summary>
        public void RenderPlugins(System.Drawing.Graphics g, System.Drawing.RectangleF viewport)
        {
            foreach (var plugin in _plugins.Values.Where(p => p.State == PluginState.Running))
            {
                try
                {
                    plugin.Render(g, viewport);
                }
                catch (Exception ex)
                {
                    // 6.1.5 错误隔离：单个插件渲染失败不影响其他插件
                    System.Diagnostics.Debug.WriteLine($"Error rendering plugin '{plugin.PluginName}': {ex.Message}");
                    OnPluginError(plugin.PluginName, ex);
                }
            }
        }

        /// <summary>
        /// 获取插件上下文
        /// </summary>
        public IPluginContext Context => _context;

        #region 事件触发

        private void OnPluginLoaded(IFlowPlugin plugin)
        {
            PluginLoaded?.Invoke(this, new PluginEventArgs { Plugin = plugin });
        }

        private void OnPluginUnloaded(string pluginName)
        {
            PluginUnloaded?.Invoke(this, new PluginEventArgs { PluginName = pluginName });
        }

        private void OnPluginError(string pluginName, Exception ex)
        {
            PluginError?.Invoke(this, new PluginErrorEventArgs
            {
                PluginName = pluginName,
                Error = ex
            });
        }

        private void OnPluginStateChanged(IFlowPlugin plugin, PluginState newState)
        {
            PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs
            {
                Plugin = plugin,
                NewState = newState
            });
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 依赖检查结果
    /// </summary>
    public class DependencyCheckResult
    {
        public string PluginName { get; set; }
        public bool AllSatisfied { get; set; }
        public List<PluginDependency> MissingDependencies { get; } = new List<PluginDependency>();
        public List<PluginDependency> SatisfiedDependencies { get; } = new List<PluginDependency>();
        public List<VersionMismatch> VersionMismatches { get; } = new List<VersionMismatch>();
    }

    /// <summary>
    /// 版本不匹配信息
    /// </summary>
    public class VersionMismatch
    {
        public PluginDependency Dependency { get; set; }
        public Version ActualVersion { get; set; }
    }

    /// <summary>
    /// 插件加载结果
    /// </summary>
    public class PluginLoadResult
    {
        public string PluginName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public DependencyCheckResult DependencyResult { get; set; }
    }

    /// <summary>
    /// 插件版本信息
    /// </summary>
    public class PluginVersionInfo
    {
        public string PluginName { get; set; }
        public Version Version { get; set; }
        public PluginState State { get; set; }
        public int DependencyCount { get; set; }
    }

    /// <summary>
    /// 插件错误信息
    /// </summary>
    public class PluginError
    {
        public string PluginName { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// 插件事件参数
    /// </summary>
    public class PluginEventArgs : EventArgs
    {
        public IFlowPlugin Plugin { get; set; }
        public string PluginName { get; set; }
    }

    /// <summary>
    /// 插件错误事件参数
    /// </summary>
    public class PluginErrorEventArgs : EventArgs
    {
        public string PluginName { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// 插件状态变更事件参数
    /// </summary>
    public class PluginStateChangedEventArgs : EventArgs
    {
        public IFlowPlugin Plugin { get; set; }
        public PluginState NewState { get; set; }
    }

    #endregion
}
