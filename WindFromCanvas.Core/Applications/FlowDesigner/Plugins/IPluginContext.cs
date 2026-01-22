using System;
using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins
{
    /// <summary>
    /// 插件上下文接口（提供插件访问核心功能的接口）
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// 状态存储
        /// </summary>
        BuilderStateStore StateStore { get; }

        /// <summary>
        /// 事件总线（用于发布和订阅事件）
        /// </summary>
        IEventBus EventBus { get; }

        /// <summary>
        /// 注册节点类型
        /// </summary>
        /// <param name="type">节点类型标识（如 "bpmn:startEvent"）</param>
        /// <param name="modelType">节点模型类型</param>
        /// <param name="viewType">节点视图类型</param>
        void RegisterNodeType(string type, Type modelType, Type viewType);

        /// <summary>
        /// 注册连线类型
        /// </summary>
        /// <param name="type">连线类型标识</param>
        /// <param name="edgeType">连线类型</param>
        void RegisterEdgeType(string type, Type edgeType);

        /// <summary>
        /// 注册数据适配器
        /// </summary>
        /// <param name="name">适配器名称</param>
        /// <param name="adapter">适配器实例</param>
        void RegisterAdapter(string name, IDataAdapter adapter);

        /// <summary>
        /// 获取已注册的节点类型
        /// </summary>
        Dictionary<string, NodeTypeRegistration> GetRegisteredNodeTypes();

        /// <summary>
        /// 获取已注册的连线类型
        /// </summary>
        Dictionary<string, Type> GetRegisteredEdgeTypes();
    }

    /// <summary>
    /// 节点类型注册信息
    /// </summary>
    public class NodeTypeRegistration
    {
        public string Type { get; set; }
        public Type ModelType { get; set; }
        public Type ViewType { get; set; }
    }

    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        void Publish(string eventName, object eventData);

        /// <summary>
        /// 订阅事件
        /// </summary>
        IDisposable Subscribe(string eventName, Action<object> handler);
    }

    /// <summary>
    /// 数据适配器接口
    /// </summary>
    public interface IDataAdapter
    {
        /// <summary>
        /// 将外部数据格式转换为内部格式
        /// </summary>
        object AdapterIn(object externalData);

        /// <summary>
        /// 将内部数据格式转换为外部格式
        /// </summary>
        object AdapterOut(object internalData);
    }
}
