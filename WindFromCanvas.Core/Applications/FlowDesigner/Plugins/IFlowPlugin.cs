using System;
using System.Collections.Generic;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins
{
    /// <summary>
    /// 流程插件接口
    /// </summary>
    public interface IFlowPlugin
    {
        /// <summary>
        /// 插件名称（唯一标识）
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// 插件显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 插件描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// 6.1.1 插件依赖列表
        /// </summary>
        IReadOnlyList<PluginDependency> Dependencies { get; }

        /// <summary>
        /// 插件状态
        /// </summary>
        PluginState State { get; }

        /// <summary>
        /// 初始化插件
        /// </summary>
        void Initialize(IPluginContext context);

        /// <summary>
        /// 渲染插件UI（可选，用于在画布上绘制自定义内容）
        /// </summary>
        void Render(Graphics g, RectangleF viewport);

        /// <summary>
        /// 销毁插件（清理资源）
        /// </summary>
        void Destroy();

        /// <summary>
        /// 6.1.4 获取插件配置
        /// </summary>
        PluginConfiguration GetConfiguration();

        /// <summary>
        /// 6.1.4 应用插件配置
        /// </summary>
        void ApplyConfiguration(PluginConfiguration config);

        /// <summary>
        /// 6.1.3 热重载（当插件代码/配置变化时）
        /// </summary>
        void Reload();
    }

    /// <summary>
    /// 6.1.1 插件依赖描述
    /// </summary>
    public class PluginDependency
    {
        /// <summary>
        /// 依赖的插件名称
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// 最低版本要求
        /// </summary>
        public Version MinVersion { get; set; }

        /// <summary>
        /// 最高版本要求（可选）
        /// </summary>
        public Version MaxVersion { get; set; }

        /// <summary>
        /// 是否可选依赖
        /// </summary>
        public bool IsOptional { get; set; }

        public PluginDependency(string pluginName, Version minVersion = null, bool isOptional = false)
        {
            PluginName = pluginName;
            MinVersion = minVersion ?? new Version(1, 0, 0);
            IsOptional = isOptional;
        }

        /// <summary>
        /// 检查版本是否满足
        /// </summary>
        public bool IsSatisfiedBy(Version version)
        {
            if (version == null) return false;
            if (version < MinVersion) return false;
            if (MaxVersion != null && version > MaxVersion) return false;
            return true;
        }
    }

    /// <summary>
    /// 插件状态
    /// </summary>
    public enum PluginState
    {
        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized,

        /// <summary>
        /// 正在加载
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载运行中
        /// </summary>
        Running,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error,

        /// <summary>
        /// 正在重新加载
        /// </summary>
        Reloading
    }

    /// <summary>
    /// 6.1.4 插件配置
    /// </summary>
    public class PluginConfiguration
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// 配置版本
        /// </summary>
        public int ConfigVersion { get; set; } = 1;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 配置项
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 获取配置值
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (Settings.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            Settings[key] = value;
        }
    }

    /// <summary>
    /// 插件基类（提供默认实现）
    /// </summary>
    public abstract class FlowPluginBase : IFlowPlugin
    {
        protected IPluginContext Context { get; private set; }
        
        public abstract string PluginName { get; }
        public virtual string DisplayName => PluginName;
        public virtual string Description => string.Empty;
        public abstract Version Version { get; }
        public virtual IReadOnlyList<PluginDependency> Dependencies => Array.Empty<PluginDependency>();
        
        private PluginState _state = PluginState.Uninitialized;
        public PluginState State
        {
            get => _state;
            protected set => _state = value;
        }

        protected PluginConfiguration Configuration { get; private set; } = new PluginConfiguration();

        public virtual void Initialize(IPluginContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            State = PluginState.Loading;
            
            try
            {
                OnInitialize();
                State = PluginState.Running;
            }
            catch
            {
                State = PluginState.Error;
                throw;
            }
        }

        /// <summary>
        /// 子类实现的初始化逻辑
        /// </summary>
        protected virtual void OnInitialize() { }

        public virtual void Render(Graphics g, RectangleF viewport) { }

        public virtual void Destroy()
        {
            State = PluginState.Stopped;
            OnDestroy();
        }

        /// <summary>
        /// 子类实现的销毁逻辑
        /// </summary>
        protected virtual void OnDestroy() { }

        public virtual PluginConfiguration GetConfiguration()
        {
            Configuration.PluginName = PluginName;
            return Configuration;
        }

        public virtual void ApplyConfiguration(PluginConfiguration config)
        {
            if (config == null) return;
            Configuration = config;
            OnConfigurationChanged();
        }

        /// <summary>
        /// 配置变更时调用
        /// </summary>
        protected virtual void OnConfigurationChanged() { }

        public virtual void Reload()
        {
            var previousState = State;
            State = PluginState.Reloading;
            
            try
            {
                OnReload();
                State = previousState == PluginState.Running ? PluginState.Running : PluginState.Paused;
            }
            catch
            {
                State = PluginState.Error;
                throw;
            }
        }

        /// <summary>
        /// 子类实现的重载逻辑
        /// </summary>
        protected virtual void OnReload() { }
    }
}
