using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin
{
    /// <summary>
    /// BPMN数据适配器（实现LogicFlow内部JSON格式与BPMN 2.0 XML格式的双向转换）
    /// </summary>
    public class BpmnAdapter : IDataAdapter
    {
        private const string BpmnNamespace = "http://www.omg.org/spec/BPMN/20100524/MODEL";
        private const string BpmnDiNamespace = "http://www.omg.org/spec/BPMN/20100524/DI";
        private const string DcNamespace = "http://www.omg.org/spec/DD/20100524/DC";
        private const string DiNamespace = "http://www.omg.org/spec/DD/20100524/DI";

        /// <summary>
        /// 将外部BPMN XML格式转换为内部格式
        /// </summary>
        public object AdapterIn(object externalData)
        {
            if (externalData is string xmlString)
            {
                return ConvertBpmnXmlToFlowVersion(xmlString);
            }
            return externalData;
        }

        /// <summary>
        /// 将内部格式转换为外部BPMN XML格式
        /// </summary>
        public object AdapterOut(object internalData)
        {
            if (internalData is FlowVersion flowVersion)
            {
                return ConvertFlowVersionToBpmnXml(flowVersion);
            }
            return internalData;
        }

        /// <summary>
        /// 将BPMN XML转换为FlowVersion
        /// </summary>
        private FlowVersion ConvertBpmnXmlToFlowVersion(string xmlString)
        {
            try
            {
                var doc = XDocument.Parse(xmlString);
                XNamespace bpmn = BpmnNamespace;
                XNamespace bpmndi = BpmnDiNamespace;

                var process = doc.Descendants(bpmn + "process").FirstOrDefault();
                if (process == null)
                {
                    throw new InvalidOperationException("BPMN XML does not contain a process element");
                }

                var flowVersion = new FlowVersion
                {
                    Id = process.Attribute("id")?.Value ?? Guid.NewGuid().ToString(),
                    DisplayName = process.Attribute("name")?.Value ?? "BPMN Process",
                    State = Core.Enums.FlowVersionState.DRAFT
                };

                // 解析BPMN元素并转换为FlowVersion结构
                // 这里需要根据实际的BPMN结构进行解析
                // 简化实现：只处理基本结构

                return flowVersion;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse BPMN XML", ex);
            }
        }

        /// <summary>
        /// 将FlowVersion转换为BPMN XML
        /// </summary>
        private string ConvertFlowVersionToBpmnXml(FlowVersion flowVersion)
        {
            try
            {
                XNamespace bpmn = BpmnNamespace;
                XNamespace bpmndi = BpmnDiNamespace;
                XNamespace dc = DcNamespace;
                XNamespace di = DiNamespace;

                var definitions = new XElement(bpmn + "definitions",
                    new XAttribute("xmlns", bpmn),
                    new XAttribute("xmlns:bpmndi", bpmndi),
                    new XAttribute("xmlns:dc", dc),
                    new XAttribute("xmlns:di", di),
                    new XAttribute("targetNamespace", "http://www.example.org/bpmn"),
                    new XAttribute("id", "Definitions_1")
                );

                var process = new XElement(bpmn + "process",
                    new XAttribute("id", flowVersion.Id ?? "Process_1"),
                    new XAttribute("isExecutable", "true")
                );

                if (!string.IsNullOrEmpty(flowVersion.DisplayName))
                {
                    process.Add(new XAttribute("name", flowVersion.DisplayName));
                }

                // 转换FlowVersion中的步骤为BPMN元素
                // 这里需要根据实际的FlowVersion结构进行转换
                // 简化实现：只处理基本结构

                definitions.Add(process);

                // 添加BPMN DI（Diagram Interchange）信息
                var diagram = new XElement(bpmndi + "BPMNDiagram",
                    new XAttribute("id", "BPMNDiagram_1")
                );

                var plane = new XElement(bpmndi + "BPMNPlane",
                    new XAttribute("id", "BPMNPlane_1"),
                    new XAttribute("bpmnElement", process.Attribute("id").Value)
                );

                diagram.Add(plane);
                definitions.Add(diagram);

                var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), definitions);
                return doc.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert FlowVersion to BPMN XML", ex);
            }
        }
    }
}
