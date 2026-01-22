using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Adapters
{
    /// <summary>
    /// 6.3.1 图数据适配器接口
    /// 用于在不同数据格式之间进行转换
    /// </summary>
    public interface IGraphAdapter
    {
        /// <summary>
        /// 适配器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// MIME类型
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// 6.3.2 将外部数据转换为内部图数据（导入）
        /// </summary>
        AdapterResult<GraphData> AdapterIn(string externalData);

        /// <summary>
        /// 6.3.2 将外部数据转换为内部图数据（导入，带选项）
        /// </summary>
        AdapterResult<GraphData> AdapterIn(string externalData, AdapterOptions options);

        /// <summary>
        /// 6.3.3 将内部图数据转换为外部格式（导出）
        /// </summary>
        AdapterResult<string> AdapterOut(GraphData internalData);

        /// <summary>
        /// 6.3.3 将内部图数据转换为外部格式（导出，带选项）
        /// </summary>
        AdapterResult<string> AdapterOut(GraphData internalData, AdapterOptions options);

        /// <summary>
        /// 6.3.6 验证外部数据格式
        /// </summary>
        ValidationResult Validate(string externalData);

        /// <summary>
        /// 检查是否支持指定格式
        /// </summary>
        bool CanHandle(string format);
    }

    /// <summary>
    /// 适配器结果
    /// </summary>
    public class AdapterResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public List<AdapterWarning> Warnings { get; } = new List<AdapterWarning>();

        public static AdapterResult<T> Succeed(T data)
        {
            return new AdapterResult<T> { Success = true, Data = data };
        }

        public static AdapterResult<T> Fail(string errorMessage)
        {
            return new AdapterResult<T> { Success = false, ErrorMessage = errorMessage };
        }
    }

    /// <summary>
    /// 适配器警告
    /// </summary>
    public class AdapterWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Path { get; set; }
    }

    /// <summary>
    /// 适配器选项
    /// </summary>
    public class AdapterOptions
    {
        /// <summary>
        /// 是否严格模式（严格模式下警告视为错误）
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// 是否保留未知属性
        /// </summary>
        public bool PreserveUnknownProperties { get; set; } = true;

        /// <summary>
        /// 6.3.5 属性映射
        /// </summary>
        public PropertyMapping PropertyMapping { get; set; }

        /// <summary>
        /// 是否格式化输出
        /// </summary>
        public bool PrettyPrint { get; set; } = true;

        /// <summary>
        /// 默认选项
        /// </summary>
        public static AdapterOptions Default => new AdapterOptions();
    }

    /// <summary>
    /// 6.3.5 属性映射配置
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// 节点属性映射（外部名称 -> 内部名称）
        /// </summary>
        public Dictionary<string, string> NodeProperties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 边属性映射（外部名称 -> 内部名称）
        /// </summary>
        public Dictionary<string, string> EdgeProperties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 节点类型映射（外部类型 -> 内部类型）
        /// </summary>
        public Dictionary<string, string> NodeTypes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 反向映射（用于导出）
        /// </summary>
        public PropertyMapping Reverse()
        {
            var reversed = new PropertyMapping();
            
            foreach (var kvp in NodeProperties)
                reversed.NodeProperties[kvp.Value] = kvp.Key;
            
            foreach (var kvp in EdgeProperties)
                reversed.EdgeProperties[kvp.Value] = kvp.Key;
            
            foreach (var kvp in NodeTypes)
                reversed.NodeTypes[kvp.Value] = kvp.Key;
            
            return reversed;
        }

        /// <summary>
        /// 映射属性名称
        /// </summary>
        public string MapNodeProperty(string propertyName, bool reverse = false)
        {
            var dict = reverse ? Reverse().NodeProperties : NodeProperties;
            return dict.TryGetValue(propertyName, out var mapped) ? mapped : propertyName;
        }

        /// <summary>
        /// 映射节点类型
        /// </summary>
        public string MapNodeType(string nodeType, bool reverse = false)
        {
            var dict = reverse ? Reverse().NodeTypes : NodeTypes;
            return dict.TryGetValue(nodeType, out var mapped) ? mapped : nodeType;
        }
    }

    /// <summary>
    /// 6.3.6 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; } = new List<ValidationError>();
        public List<AdapterWarning> Warnings { get; } = new List<AdapterWarning>();

        public static ValidationResult Valid()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Invalid(string errorMessage)
        {
            var result = new ValidationResult { IsValid = false };
            result.Errors.Add(new ValidationError { Message = errorMessage });
            return result;
        }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Path { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
    }

    /// <summary>
    /// 图数据传输对象
    /// </summary>
    public class GraphData
    {
        /// <summary>
        /// 图ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 图名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 节点列表
        /// </summary>
        public List<NodeData> Nodes { get; set; } = new List<NodeData>();

        /// <summary>
        /// 边列表
        /// </summary>
        public List<EdgeData> Edges { get; set; } = new List<EdgeData>();

        /// <summary>
        /// 扩展属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 节点数据传输对象
    /// </summary>
    public class NodeData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 边数据传输对象
    /// </summary>
    public class EdgeData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public string SourceAnchorId { get; set; }
        public string TargetAnchorId { get; set; }
        public string Label { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 适配器基类
    /// </summary>
    public abstract class GraphAdapterBase : IGraphAdapter
    {
        public abstract string Name { get; }
        public abstract string[] SupportedExtensions { get; }
        public abstract string MimeType { get; }

        public virtual AdapterResult<GraphData> AdapterIn(string externalData)
        {
            return AdapterIn(externalData, AdapterOptions.Default);
        }

        public abstract AdapterResult<GraphData> AdapterIn(string externalData, AdapterOptions options);

        public virtual AdapterResult<string> AdapterOut(GraphData internalData)
        {
            return AdapterOut(internalData, AdapterOptions.Default);
        }

        public abstract AdapterResult<string> AdapterOut(GraphData internalData, AdapterOptions options);

        public abstract ValidationResult Validate(string externalData);

        public virtual bool CanHandle(string format)
        {
            if (string.IsNullOrEmpty(format)) return false;
            
            format = format.TrimStart('.').ToLowerInvariant();
            foreach (var ext in SupportedExtensions)
            {
                if (ext.TrimStart('.').ToLowerInvariant() == format)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 应用属性映射
        /// </summary>
        protected void ApplyPropertyMapping(GraphData data, PropertyMapping mapping, bool reverse = false)
        {
            if (mapping == null || data == null) return;

            foreach (var node in data.Nodes)
            {
                node.Type = mapping.MapNodeType(node.Type, reverse);
                
                var mappedProperties = new Dictionary<string, object>();
                foreach (var kvp in node.Properties)
                {
                    var mappedKey = mapping.MapNodeProperty(kvp.Key, reverse);
                    mappedProperties[mappedKey] = kvp.Value;
                }
                node.Properties = mappedProperties;
            }
        }
    }
}
