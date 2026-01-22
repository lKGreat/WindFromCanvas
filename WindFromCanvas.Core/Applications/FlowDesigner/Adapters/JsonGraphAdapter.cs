using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Adapters
{
    /// <summary>
    /// 6.3.4 JSON图数据适配器
    /// 支持标准JSON格式的导入导出
    /// </summary>
    public class JsonGraphAdapter : GraphAdapterBase
    {
        public override string Name => "JSON";
        public override string[] SupportedExtensions => new[] { ".json", ".flow" };
        public override string MimeType => "application/json";

        private static readonly JsonSerializerSettings _writeSettingsPretty = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly JsonSerializerSettings _writeSettingsCompact = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        public override AdapterResult<GraphData> AdapterIn(string externalData, AdapterOptions options)
        {
            if (string.IsNullOrWhiteSpace(externalData))
            {
                return AdapterResult<GraphData>.Fail("Input data is empty");
            }

            try
            {
                // 首先验证JSON格式
                var validation = Validate(externalData);
                if (!validation.IsValid)
                {
                    return AdapterResult<GraphData>.Fail(
                        string.Join("; ", validation.Errors.ConvertAll(e => e.Message)));
                }

                // 解析JSON
                var jsonObj = JObject.Parse(externalData);
                var graphData = ParseGraphData(jsonObj, options);

                // 应用属性映射
                if (options?.PropertyMapping != null)
                {
                    ApplyPropertyMapping(graphData, options.PropertyMapping, false);
                }

                var result = AdapterResult<GraphData>.Succeed(graphData);
                
                // 添加警告
                foreach (var warning in validation.Warnings)
                {
                    result.Warnings.Add(warning);
                }

                return result;
            }
            catch (JsonReaderException ex)
            {
                return AdapterResult<GraphData>.Fail($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return AdapterResult<GraphData>.Fail($"Unexpected error: {ex.Message}");
            }
        }

        public override AdapterResult<string> AdapterOut(GraphData internalData, AdapterOptions options)
        {
            if (internalData == null)
            {
                return AdapterResult<string>.Fail("Input data is null");
            }

            try
            {
                // 如果有属性映射，应用反向映射
                if (options?.PropertyMapping != null)
                {
                    // 创建副本以避免修改原始数据
                    internalData = CloneGraphData(internalData);
                    ApplyPropertyMapping(internalData, options.PropertyMapping, true);
                }

                var settings = (options?.PrettyPrint ?? true) ? _writeSettingsPretty : _writeSettingsCompact;
                var outputObj = CreateJsonOutput(internalData);
                var json = JsonConvert.SerializeObject(outputObj, settings);

                return AdapterResult<string>.Succeed(json);
            }
            catch (Exception ex)
            {
                return AdapterResult<string>.Fail($"Serialization error: {ex.Message}");
            }
        }

        public override ValidationResult Validate(string externalData)
        {
            if (string.IsNullOrWhiteSpace(externalData))
            {
                return ValidationResult.Invalid("Input data is empty");
            }

            try
            {
                var jsonObj = JObject.Parse(externalData);
                var result = new ValidationResult { IsValid = true };

                // 验证必需字段
                var nodesToken = jsonObj["nodes"] ?? jsonObj["Nodes"];
                if (nodesToken == null)
                {
                    result.Warnings.Add(new AdapterWarning
                    {
                        Code = "MISSING_NODES",
                        Message = "No 'nodes' property found, graph will have no nodes"
                    });
                }
                else if (nodesToken.Type != JTokenType.Array)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "INVALID_NODES",
                        Message = "'nodes' must be an array"
                    });
                    result.IsValid = false;
                }

                // 验证edges字段
                var edgesToken = jsonObj["edges"] ?? jsonObj["Edges"];
                if (edgesToken != null && edgesToken.Type != JTokenType.Array)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "INVALID_EDGES",
                        Message = "'edges' must be an array"
                    });
                    result.IsValid = false;
                }

                return result;
            }
            catch (JsonReaderException ex)
            {
                return ValidationResult.Invalid($"Invalid JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析图数据
        /// </summary>
        private GraphData ParseGraphData(JObject root, AdapterOptions options)
        {
            var graphData = new GraphData();

            // 解析基本属性
            graphData.Id = GetStringValue(root, "id", "Id");
            graphData.Name = GetStringValue(root, "name", "Name");
            graphData.Version = GetStringValue(root, "version", "Version");

            // 解析节点
            var nodesToken = root["nodes"] ?? root["Nodes"];
            if (nodesToken is JArray nodesArray)
            {
                foreach (var nodeToken in nodesArray)
                {
                    if (nodeToken is JObject nodeObj)
                    {
                        graphData.Nodes.Add(ParseNodeData(nodeObj, options));
                    }
                }
            }

            // 解析边
            var edgesToken = root["edges"] ?? root["Edges"];
            if (edgesToken is JArray edgesArray)
            {
                foreach (var edgeToken in edgesArray)
                {
                    if (edgeToken is JObject edgeObj)
                    {
                        graphData.Edges.Add(ParseEdgeData(edgeObj, options));
                    }
                }
            }

            // 解析扩展属性
            if (options?.PreserveUnknownProperties ?? true)
            {
                var propsToken = root["properties"] ?? root["Properties"];
                if (propsToken is JObject propsObj)
                {
                    graphData.Properties = ParseProperties(propsObj);
                }
            }

            return graphData;
        }

        /// <summary>
        /// 解析节点数据
        /// </summary>
        private NodeData ParseNodeData(JObject element, AdapterOptions options)
        {
            var node = new NodeData
            {
                Id = GetStringValue(element, "id", "Id"),
                Type = GetStringValue(element, "type", "Type"),
                Label = GetStringValue(element, "label", "Label", "text", "name"),
                X = GetFloatValue(element, "x", "X"),
                Y = GetFloatValue(element, "y", "Y"),
                Width = GetFloatValue(element, "width", "Width"),
                Height = GetFloatValue(element, "height", "Height")
            };

            // 解析节点属性
            if (options?.PreserveUnknownProperties ?? true)
            {
                var propsToken = element["properties"] ?? element["Properties"] ?? element["data"];
                if (propsToken is JObject propsObj)
                {
                    node.Properties = ParseProperties(propsObj);
                }
            }

            return node;
        }

        /// <summary>
        /// 解析边数据
        /// </summary>
        private EdgeData ParseEdgeData(JObject element, AdapterOptions options)
        {
            var edge = new EdgeData
            {
                Id = GetStringValue(element, "id", "Id"),
                Type = GetStringValue(element, "type", "Type"),
                SourceId = GetStringValue(element, "sourceId", "SourceId", "source", "from"),
                TargetId = GetStringValue(element, "targetId", "TargetId", "target", "to"),
                SourceAnchorId = GetStringValue(element, "sourceAnchorId", "SourceAnchorId"),
                TargetAnchorId = GetStringValue(element, "targetAnchorId", "TargetAnchorId"),
                Label = GetStringValue(element, "label", "Label")
            };

            // 解析边属性
            if (options?.PreserveUnknownProperties ?? true)
            {
                var propsToken = element["properties"] ?? element["Properties"];
                if (propsToken is JObject propsObj)
                {
                    edge.Properties = ParseProperties(propsObj);
                }
            }

            return edge;
        }

        /// <summary>
        /// 解析属性字典
        /// </summary>
        private Dictionary<string, object> ParseProperties(JObject element)
        {
            var properties = new Dictionary<string, object>();

            foreach (var prop in element.Properties())
            {
                properties[prop.Name] = ParseJTokenValue(prop.Value);
            }

            return properties;
        }

        /// <summary>
        /// 解析JToken值
        /// </summary>
        private object ParseJTokenValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Integer:
                    return token.Value<long>();
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Null:
                    return null;
                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                        list.Add(ParseJTokenValue(item));
                    return list;
                case JTokenType.Object:
                    return ParseProperties((JObject)token);
                default:
                    return token.ToString();
            }
        }

        /// <summary>
        /// 获取字符串值（支持多个属性名）
        /// </summary>
        private string GetStringValue(JObject obj, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var token = obj[name];
                if (token != null && token.Type == JTokenType.String)
                {
                    return token.Value<string>();
                }
            }
            return null;
        }

        /// <summary>
        /// 获取浮点值（支持多个属性名）
        /// </summary>
        private float GetFloatValue(JObject obj, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var token = obj[name];
                if (token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer))
                {
                    return token.Value<float>();
                }
            }
            return 0;
        }

        /// <summary>
        /// 创建JSON输出对象
        /// </summary>
        private object CreateJsonOutput(GraphData data)
        {
            return new
            {
                id = data.Id,
                name = data.Name,
                version = data.Version,
                nodes = data.Nodes.Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    label = n.Label,
                    x = n.X,
                    y = n.Y,
                    width = n.Width,
                    height = n.Height,
                    properties = n.Properties.Count > 0 ? n.Properties : null
                }).ToList(),
                edges = data.Edges.Select(e => new
                {
                    id = e.Id,
                    type = e.Type,
                    sourceId = e.SourceId,
                    targetId = e.TargetId,
                    sourceAnchorId = e.SourceAnchorId,
                    targetAnchorId = e.TargetAnchorId,
                    label = e.Label,
                    properties = e.Properties.Count > 0 ? e.Properties : null
                }).ToList(),
                properties = data.Properties.Count > 0 ? data.Properties : null
            };
        }

        /// <summary>
        /// 克隆图数据
        /// </summary>
        private GraphData CloneGraphData(GraphData source)
        {
            var clone = new GraphData
            {
                Id = source.Id,
                Name = source.Name,
                Version = source.Version,
                Properties = new Dictionary<string, object>(source.Properties)
            };

            foreach (var node in source.Nodes)
            {
                clone.Nodes.Add(new NodeData
                {
                    Id = node.Id,
                    Type = node.Type,
                    Label = node.Label,
                    X = node.X,
                    Y = node.Y,
                    Width = node.Width,
                    Height = node.Height,
                    Properties = new Dictionary<string, object>(node.Properties)
                });
            }

            foreach (var edge in source.Edges)
            {
                clone.Edges.Add(new EdgeData
                {
                    Id = edge.Id,
                    Type = edge.Type,
                    SourceId = edge.SourceId,
                    TargetId = edge.TargetId,
                    SourceAnchorId = edge.SourceAnchorId,
                    TargetAnchorId = edge.TargetAnchorId,
                    Label = edge.Label,
                    Properties = new Dictionary<string, object>(edge.Properties)
                });
            }

            return clone;
        }
    }
}
