using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin
{
    /// <summary>
    /// 6.2.7 & 6.2.8 BPMN数据适配器
    /// 实现LogicFlow内部JSON格式与BPMN 2.0 XML格式的完整双向转换
    /// </summary>
    public class BpmnAdapter : IDataAdapter
    {
        // BPMN 2.0 命名空间定义
        private static readonly XNamespace BpmnNs = "http://www.omg.org/spec/BPMN/20100524/MODEL";
        private static readonly XNamespace BpmnDiNs = "http://www.omg.org/spec/BPMN/20100524/DI";
        private static readonly XNamespace DcNs = "http://www.omg.org/spec/DD/20100524/DC";
        private static readonly XNamespace DiNs = "http://www.omg.org/spec/DD/20100524/DI";

        // BPMN元素映射
        private static readonly Dictionary<string, BpmnNodeType> ElementToNodeType = new Dictionary<string, BpmnNodeType>
        {
            { "startEvent", BpmnNodeType.StartEvent },
            { "endEvent", BpmnNodeType.EndEvent },
            { "intermediateCatchEvent", BpmnNodeType.IntermediateEvent },
            { "intermediateThrowEvent", BpmnNodeType.IntermediateEvent },
            { "userTask", BpmnNodeType.UserTask },
            { "serviceTask", BpmnNodeType.ServiceTask },
            { "scriptTask", BpmnNodeType.ScriptTask },
            { "manualTask", BpmnNodeType.ManualTask },
            { "exclusiveGateway", BpmnNodeType.ExclusiveGateway },
            { "parallelGateway", BpmnNodeType.ParallelGateway },
            { "inclusiveGateway", BpmnNodeType.InclusiveGateway },
            { "eventBasedGateway", BpmnNodeType.EventBasedGateway },
            { "subProcess", BpmnNodeType.SubProcess },
            { "callActivity", BpmnNodeType.CallActivity }
        };

        private static readonly Dictionary<BpmnNodeType, string> NodeTypeToElement = 
            ElementToNodeType.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        /// <summary>
        /// 将外部BPMN XML格式转换为内部格式
        /// </summary>
        public object AdapterIn(object externalData)
        {
            if (externalData is string xmlString)
            {
                return ParseBpmnXml(xmlString);
            }
            return externalData;
        }

        /// <summary>
        /// 将内部格式转换为外部BPMN XML格式
        /// </summary>
        public object AdapterOut(object internalData)
        {
            if (internalData is BpmnDocument doc)
            {
                return GenerateBpmnXml(doc);
            }
            if (internalData is FlowVersion flowVersion)
            {
                return ConvertFlowVersionToBpmnXml(flowVersion);
            }
            return internalData;
        }

        #region 6.2.7 XML解析

        /// <summary>
        /// 解析BPMN XML为内部文档结构
        /// </summary>
        public BpmnDocument ParseBpmnXml(string xmlString)
        {
            try
            {
                var xdoc = XDocument.Parse(xmlString);
                var definitions = xdoc.Root;
                
                if (definitions == null || definitions.Name.LocalName != "definitions")
                {
                    throw new InvalidOperationException("Invalid BPMN XML: missing definitions element");
                }

                var document = new BpmnDocument
                {
                    Id = definitions.Attribute("id")?.Value ?? Guid.NewGuid().ToString(),
                    TargetNamespace = definitions.Attribute("targetNamespace")?.Value
                };

                // 解析所有流程
                foreach (var processElement in definitions.Elements().Where(e => e.Name.LocalName == "process"))
                {
                    var process = ParseProcess(processElement);
                    document.Processes.Add(process);
                }

                // 解析图形信息
                var diagram = definitions.Elements().FirstOrDefault(e => e.Name.LocalName == "BPMNDiagram");
                if (diagram != null)
                {
                    ParseDiagramInfo(diagram, document);
                }

                return document;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Failed to parse BPMN XML: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// 解析流程元素
        /// </summary>
        private BpmnProcess ParseProcess(XElement processElement)
        {
            var process = new BpmnProcess
            {
                Id = processElement.Attribute("id")?.Value,
                Name = processElement.Attribute("name")?.Value,
                IsExecutable = bool.TryParse(processElement.Attribute("isExecutable")?.Value, out var exec) && exec
            };

            foreach (var element in processElement.Elements())
            {
                var localName = element.Name.LocalName;

                // 解析流程节点
                if (ElementToNodeType.TryGetValue(localName, out var nodeType))
                {
                    var node = ParseFlowNode(element, nodeType);
                    process.Nodes.Add(node);
                }
                // 解析顺序流
                else if (localName == "sequenceFlow")
                {
                    var flow = ParseSequenceFlow(element);
                    process.SequenceFlows.Add(flow);
                }
            }

            return process;
        }

        /// <summary>
        /// 解析流程节点
        /// </summary>
        private BpmnNodeData ParseFlowNode(XElement element, BpmnNodeType nodeType)
        {
            var node = new BpmnNodeData
            {
                BpmnId = element.Attribute("id")?.Value,
                Name = element.Attribute("id")?.Value,
                DisplayName = element.Attribute("name")?.Value ?? element.Attribute("id")?.Value,
                BpmnType = nodeType,
                Type = MapBpmnTypeToFlowType(nodeType)
            };

            // 解析任务特定属性
            if (nodeType == BpmnNodeType.UserTask)
            {
                var camunda = XNamespace.Get("http://camunda.org/schema/1.0/bpmn");
                node.Assignee = element.Attribute(camunda + "assignee")?.Value;
                node.CandidateUsers = element.Attribute(camunda + "candidateUsers")?.Value;
                node.CandidateGroups = element.Attribute(camunda + "candidateGroups")?.Value;
                node.FormKey = element.Attribute(camunda + "formKey")?.Value;
            }
            else if (nodeType == BpmnNodeType.ServiceTask)
            {
                node.Implementation = element.Attribute("implementation")?.Value;
                node.OperationRef = element.Attribute("operationRef")?.Value;
            }
            else if (nodeType == BpmnNodeType.ScriptTask)
            {
                node.ScriptFormat = element.Attribute("scriptFormat")?.Value;
                var scriptElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "script");
                node.Script = scriptElement?.Value;
            }

            // 解析网关默认流
            if (nodeType == BpmnNodeType.ExclusiveGateway || nodeType == BpmnNodeType.InclusiveGateway)
            {
                node.DefaultFlow = element.Attribute("default")?.Value;
            }

            // 解析文档说明
            var docElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "documentation");
            node.Documentation = docElement?.Value;

            return node;
        }

        /// <summary>
        /// 解析顺序流
        /// </summary>
        private BpmnSequenceFlowData ParseSequenceFlow(XElement element)
        {
            var flow = new BpmnSequenceFlowData
            {
                BpmnId = element.Attribute("id")?.Value,
                Name = element.Attribute("name")?.Value,
                SourceNodeName = element.Attribute("sourceRef")?.Value,
                TargetNodeName = element.Attribute("targetRef")?.Value
            };

            // 解析条件表达式
            var conditionElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "conditionExpression");
            if (conditionElement != null)
            {
                flow.ConditionExpression = conditionElement.Value;
            }

            return flow;
        }

        /// <summary>
        /// 解析图形布局信息
        /// </summary>
        private void ParseDiagramInfo(XElement diagram, BpmnDocument document)
        {
            var plane = diagram.Elements().FirstOrDefault(e => e.Name.LocalName == "BPMNPlane");
            if (plane == null) return;

            foreach (var shapeElement in plane.Elements().Where(e => e.Name.LocalName == "BPMNShape"))
            {
                var bpmnElement = shapeElement.Attribute("bpmnElement")?.Value;
                var boundsElement = shapeElement.Elements().FirstOrDefault(e => e.Name.LocalName == "Bounds");
                
                if (boundsElement != null && !string.IsNullOrEmpty(bpmnElement))
                {
                    var x = ParseFloat(boundsElement.Attribute("x")?.Value);
                    var y = ParseFloat(boundsElement.Attribute("y")?.Value);
                    var width = ParseFloat(boundsElement.Attribute("width")?.Value);
                    var height = ParseFloat(boundsElement.Attribute("height")?.Value);

                    // 查找对应的节点并设置位置
                    foreach (var process in document.Processes)
                    {
                        var node = process.Nodes.FirstOrDefault(n => n.BpmnId == bpmnElement);
                        if (node != null)
                        {
                            node.PositionX = x;
                            node.PositionY = y;
                            break;
                        }
                    }
                }
            }

            // 解析边的路径点
            foreach (var edgeElement in plane.Elements().Where(e => e.Name.LocalName == "BPMNEdge"))
            {
                var bpmnElement = edgeElement.Attribute("bpmnElement")?.Value;
                var waypoints = edgeElement.Elements()
                    .Where(e => e.Name.LocalName == "waypoint")
                    .Select(wp => new PointF(
                        ParseFloat(wp.Attribute("x")?.Value),
                        ParseFloat(wp.Attribute("y")?.Value)))
                    .ToList();

                if (!string.IsNullOrEmpty(bpmnElement) && waypoints.Count > 0)
                {
                    foreach (var process in document.Processes)
                    {
                        var flow = process.SequenceFlows.FirstOrDefault(f => f.BpmnId == bpmnElement);
                        if (flow != null)
                        {
                            flow.WayPoints = waypoints;
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region 6.2.8 XML生成

        /// <summary>
        /// 生成BPMN XML
        /// </summary>
        public string GenerateBpmnXml(BpmnDocument document)
        {
            try
            {
                var definitions = new XElement(BpmnNs + "definitions",
                    new XAttribute("xmlns", BpmnNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "bpmndi", BpmnDiNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "dc", DcNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "di", DiNs.NamespaceName),
                    new XAttribute("id", document.Id ?? "Definitions_1"),
                    new XAttribute("targetNamespace", document.TargetNamespace ?? "http://bpmn.io/schema/bpmn")
                );

                // 生成流程元素
                foreach (var process in document.Processes)
                {
                    var processElement = GenerateProcess(process);
                    definitions.Add(processElement);
                }

                // 生成图形信息
                var diagramElement = GenerateDiagram(document);
                definitions.Add(diagramElement);

                var xdoc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    definitions
                );

                return xdoc.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Failed to generate BPMN XML: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// 生成流程元素
        /// </summary>
        private XElement GenerateProcess(BpmnProcess process)
        {
            var processElement = new XElement(BpmnNs + "process",
                new XAttribute("id", process.Id ?? "Process_1"),
                new XAttribute("isExecutable", process.IsExecutable.ToString().ToLower())
            );

            if (!string.IsNullOrEmpty(process.Name))
            {
                processElement.Add(new XAttribute("name", process.Name));
            }

            // 生成节点元素
            foreach (var node in process.Nodes)
            {
                var nodeElement = GenerateFlowNode(node);
                processElement.Add(nodeElement);
            }

            // 生成顺序流元素
            foreach (var flow in process.SequenceFlows)
            {
                var flowElement = GenerateSequenceFlow(flow);
                processElement.Add(flowElement);
            }

            return processElement;
        }

        /// <summary>
        /// 生成流程节点元素
        /// </summary>
        private XElement GenerateFlowNode(BpmnNodeData node)
        {
            var elementName = NodeTypeToElement.TryGetValue(node.BpmnType, out var name) ? name : "task";
            var element = new XElement(BpmnNs + elementName,
                new XAttribute("id", node.BpmnId ?? node.Name ?? Guid.NewGuid().ToString())
            );

            if (!string.IsNullOrEmpty(node.DisplayName))
            {
                element.Add(new XAttribute("name", node.DisplayName));
            }

            // 添加文档说明
            if (!string.IsNullOrEmpty(node.Documentation))
            {
                element.Add(new XElement(BpmnNs + "documentation", node.Documentation));
            }

            // 任务特定属性
            if (node.BpmnType == BpmnNodeType.UserTask)
            {
                var camunda = XNamespace.Get("http://camunda.org/schema/1.0/bpmn");
                if (!string.IsNullOrEmpty(node.Assignee))
                    element.Add(new XAttribute(camunda + "assignee", node.Assignee));
                if (!string.IsNullOrEmpty(node.CandidateUsers))
                    element.Add(new XAttribute(camunda + "candidateUsers", node.CandidateUsers));
                if (!string.IsNullOrEmpty(node.CandidateGroups))
                    element.Add(new XAttribute(camunda + "candidateGroups", node.CandidateGroups));
                if (!string.IsNullOrEmpty(node.FormKey))
                    element.Add(new XAttribute(camunda + "formKey", node.FormKey));
            }
            else if (node.BpmnType == BpmnNodeType.ServiceTask)
            {
                if (!string.IsNullOrEmpty(node.Implementation))
                    element.Add(new XAttribute("implementation", node.Implementation));
                if (!string.IsNullOrEmpty(node.OperationRef))
                    element.Add(new XAttribute("operationRef", node.OperationRef));
            }
            else if (node.BpmnType == BpmnNodeType.ScriptTask)
            {
                if (!string.IsNullOrEmpty(node.ScriptFormat))
                    element.Add(new XAttribute("scriptFormat", node.ScriptFormat));
                if (!string.IsNullOrEmpty(node.Script))
                    element.Add(new XElement(BpmnNs + "script", node.Script));
            }

            // 网关默认流
            if ((node.BpmnType == BpmnNodeType.ExclusiveGateway || node.BpmnType == BpmnNodeType.InclusiveGateway)
                && !string.IsNullOrEmpty(node.DefaultFlow))
            {
                element.Add(new XAttribute("default", node.DefaultFlow));
            }

            return element;
        }

        /// <summary>
        /// 生成顺序流元素
        /// </summary>
        private XElement GenerateSequenceFlow(BpmnSequenceFlowData flow)
        {
            var element = new XElement(BpmnNs + "sequenceFlow",
                new XAttribute("id", flow.BpmnId ?? Guid.NewGuid().ToString()),
                new XAttribute("sourceRef", flow.SourceNodeName),
                new XAttribute("targetRef", flow.TargetNodeName)
            );

            if (!string.IsNullOrEmpty(flow.Name))
            {
                element.Add(new XAttribute("name", flow.Name));
            }

            if (!string.IsNullOrEmpty(flow.ConditionExpression))
            {
                element.Add(new XElement(BpmnNs + "conditionExpression",
                    new XAttribute(XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance") + "type", "bpmn:tFormalExpression"),
                    flow.ConditionExpression));
            }

            return element;
        }

        /// <summary>
        /// 生成图形布局信息
        /// </summary>
        private XElement GenerateDiagram(BpmnDocument document)
        {
            var diagram = new XElement(BpmnDiNs + "BPMNDiagram",
                new XAttribute("id", "BPMNDiagram_1")
            );

            foreach (var process in document.Processes)
            {
                var plane = new XElement(BpmnDiNs + "BPMNPlane",
                    new XAttribute("id", "BPMNPlane_" + process.Id),
                    new XAttribute("bpmnElement", process.Id)
                );

                // 生成节点形状
                foreach (var node in process.Nodes)
                {
                    var shape = GenerateShape(node);
                    plane.Add(shape);
                }

                // 生成边
                foreach (var flow in process.SequenceFlows)
                {
                    var edge = GenerateEdge(flow);
                    plane.Add(edge);
                }

                diagram.Add(plane);
            }

            return diagram;
        }

        /// <summary>
        /// 生成节点形状元素
        /// </summary>
        private XElement GenerateShape(BpmnNodeData node)
        {
            var (width, height) = GetNodeSize(node.BpmnType);
            
            return new XElement(BpmnDiNs + "BPMNShape",
                new XAttribute("id", node.BpmnId + "_di"),
                new XAttribute("bpmnElement", node.BpmnId),
                new XElement(DcNs + "Bounds",
                    new XAttribute("x", node.PositionX.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("y", node.PositionY.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("height", height.ToString(CultureInfo.InvariantCulture))
                )
            );
        }

        /// <summary>
        /// 生成边元素
        /// </summary>
        private XElement GenerateEdge(BpmnSequenceFlowData flow)
        {
            var edge = new XElement(BpmnDiNs + "BPMNEdge",
                new XAttribute("id", flow.BpmnId + "_di"),
                new XAttribute("bpmnElement", flow.BpmnId)
            );

            if (flow.WayPoints != null && flow.WayPoints.Count > 0)
            {
                foreach (var point in flow.WayPoints)
                {
                    edge.Add(new XElement(DiNs + "waypoint",
                        new XAttribute("x", point.X.ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("y", point.Y.ToString(CultureInfo.InvariantCulture))
                    ));
                }
            }

            return edge;
        }

        #endregion

        #region 辅助方法

        private FlowNodeType MapBpmnTypeToFlowType(BpmnNodeType bpmnType)
        {
            switch (bpmnType)
            {
                case BpmnNodeType.StartEvent:
                    return FlowNodeType.Start;
                case BpmnNodeType.EndEvent:
                    return FlowNodeType.End;
                case BpmnNodeType.ExclusiveGateway:
                case BpmnNodeType.ParallelGateway:
                case BpmnNodeType.InclusiveGateway:
                case BpmnNodeType.EventBasedGateway:
                    return FlowNodeType.Decision;
                case BpmnNodeType.ScriptTask:
                    return FlowNodeType.Code;
                default:
                    return FlowNodeType.Process;
            }
        }

        private (float width, float height) GetNodeSize(BpmnNodeType nodeType)
        {
            switch (nodeType)
            {
                case BpmnNodeType.StartEvent:
                case BpmnNodeType.EndEvent:
                case BpmnNodeType.IntermediateEvent:
                    return (36, 36);
                case BpmnNodeType.ExclusiveGateway:
                case BpmnNodeType.ParallelGateway:
                case BpmnNodeType.InclusiveGateway:
                case BpmnNodeType.EventBasedGateway:
                    return (50, 50);
                default:
                    return (100, 80);
            }
        }

        private float ParseFloat(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result);
            return result;
        }

        /// <summary>
        /// 将FlowVersion转换为BPMN XML（兼容旧接口）
        /// </summary>
        private string ConvertFlowVersionToBpmnXml(FlowVersion flowVersion)
        {
            var document = new BpmnDocument
            {
                Id = "Definitions_" + (flowVersion.Id ?? Guid.NewGuid().ToString()),
                TargetNamespace = "http://bpmn.io/schema/bpmn"
            };

            var process = new BpmnProcess
            {
                Id = flowVersion.Id ?? "Process_1",
                Name = flowVersion.DisplayName,
                IsExecutable = true
            };

            document.Processes.Add(process);

            return GenerateBpmnXml(document);
        }

        #endregion
    }

    #region BPMN文档模型

    /// <summary>
    /// BPMN文档
    /// </summary>
    public class BpmnDocument
    {
        public string Id { get; set; }
        public string TargetNamespace { get; set; }
        public List<BpmnProcess> Processes { get; set; } = new List<BpmnProcess>();
    }

    /// <summary>
    /// BPMN流程
    /// </summary>
    public class BpmnProcess
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsExecutable { get; set; }
        public List<BpmnNodeData> Nodes { get; set; } = new List<BpmnNodeData>();
        public List<BpmnSequenceFlowData> SequenceFlows { get; set; } = new List<BpmnSequenceFlowData>();
    }

    #endregion
}
