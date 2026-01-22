using System;
using System.Collections.Generic;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin
{
    /// <summary>
    /// 6.2 BPMN插件（BPMN 2.0标准支持）
    /// 提供完整的BPMN 2.0节点类型和XML双向转换
    /// </summary>
    public class BpmnPlugin : FlowPluginBase
    {
        private BpmnAdapter _adapter;
        private readonly Dictionary<string, Type> _nodeTypes = new Dictionary<string, Type>();

        public override string PluginName => "BPMN";
        public override string DisplayName => "BPMN 2.0";
        public override string Description => "BPMN 2.0 标准流程图支持，包含事件、任务、网关等节点类型";
        public override Version Version => new Version(1, 0, 0);

        // BPMN节点类型列表
        public IReadOnlyDictionary<string, Type> NodeTypes => _nodeTypes;

        protected override void OnInitialize()
        {
            // 6.2.1-6.2.6 注册BPMN节点类型
            RegisterBpmnNodeTypes();

            // 创建并注册数据适配器
            _adapter = new BpmnAdapter();
            Context.RegisterAdapter("BPMN", _adapter);

            // 订阅相关事件
            Context.EventBus.Subscribe("import:bpmn", OnImportBpmn);
            Context.EventBus.Subscribe("export:bpmn", OnExportBpmn);
        }

        protected override void OnDestroy()
        {
            _adapter = null;
            _nodeTypes.Clear();
        }

        /// <summary>
        /// 6.2.1-6.2.6 注册BPMN标准节点类型
        /// </summary>
        private void RegisterBpmnNodeTypes()
        {
            // 事件节点
            RegisterNodeType("bpmn:startEvent", typeof(BpmnNodeData), typeof(StartEventNode));
            RegisterNodeType("bpmn:endEvent", typeof(BpmnNodeData), typeof(EndEventNode));
            RegisterNodeType("bpmn:intermediateEvent", typeof(BpmnNodeData), typeof(IntermediateEventNode));

            // 任务节点
            RegisterNodeType("bpmn:userTask", typeof(BpmnNodeData), typeof(UserTaskNode));
            RegisterNodeType("bpmn:serviceTask", typeof(BpmnNodeData), typeof(ServiceTaskNode));
            RegisterNodeType("bpmn:scriptTask", typeof(BpmnNodeData), typeof(ScriptTaskNode));
            RegisterNodeType("bpmn:manualTask", typeof(BpmnNodeData), typeof(ManualTaskNode));

            // 网关节点
            RegisterNodeType("bpmn:exclusiveGateway", typeof(BpmnNodeData), typeof(ExclusiveGatewayNode));
            RegisterNodeType("bpmn:parallelGateway", typeof(BpmnNodeData), typeof(ParallelGatewayNode));
            RegisterNodeType("bpmn:inclusiveGateway", typeof(BpmnNodeData), typeof(InclusiveGatewayNode));
            RegisterNodeType("bpmn:eventBasedGateway", typeof(BpmnNodeData), typeof(EventBasedGatewayNode));

            // 子流程节点
            RegisterNodeType("bpmn:subProcess", typeof(BpmnNodeData), typeof(SubProcessNode));
            RegisterNodeType("bpmn:callActivity", typeof(BpmnNodeData), typeof(CallActivityNode));
        }

        /// <summary>
        /// 注册节点类型
        /// </summary>
        private void RegisterNodeType(string typeName, Type dataType, Type nodeType)
        {
            _nodeTypes[typeName] = nodeType;
            Context.RegisterNodeType(typeName, dataType, nodeType);
        }

        /// <summary>
        /// 获取BPMN适配器
        /// </summary>
        public BpmnAdapter GetAdapter()
        {
            return _adapter;
        }

        /// <summary>
        /// 导入BPMN XML
        /// </summary>
        public BpmnDocument ImportFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML content is empty");

            return _adapter.ParseBpmnXml(xml);
        }

        /// <summary>
        /// 导出为BPMN XML
        /// </summary>
        public string ExportToXml(BpmnDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return _adapter.GenerateBpmnXml(document);
        }

        /// <summary>
        /// 创建BPMN节点
        /// </summary>
        public BpmnNode CreateNode(BpmnNodeType nodeType, PointF position, string name = null)
        {
            BpmnNode node;
            var data = new BpmnNodeData
            {
                BpmnType = nodeType,
                BpmnId = string.Format("{0}_{1}", nodeType, Guid.NewGuid().ToString("N").Substring(0, 8)),
                Name = name ?? nodeType.ToString(),
                DisplayName = name ?? GetDefaultDisplayName(nodeType),
                PositionX = position.X,
                PositionY = position.Y
            };

            switch (nodeType)
            {
                case BpmnNodeType.StartEvent:
                    node = new StartEventNode(data);
                    break;
                case BpmnNodeType.EndEvent:
                    node = new EndEventNode(data);
                    break;
                case BpmnNodeType.IntermediateEvent:
                    node = new IntermediateEventNode(data);
                    break;
                case BpmnNodeType.UserTask:
                    node = new UserTaskNode(data);
                    break;
                case BpmnNodeType.ServiceTask:
                    node = new ServiceTaskNode(data);
                    break;
                case BpmnNodeType.ScriptTask:
                    node = new ScriptTaskNode(data);
                    break;
                case BpmnNodeType.ManualTask:
                    node = new ManualTaskNode(data);
                    break;
                case BpmnNodeType.ExclusiveGateway:
                    node = new ExclusiveGatewayNode(data);
                    break;
                case BpmnNodeType.ParallelGateway:
                    node = new ParallelGatewayNode(data);
                    break;
                case BpmnNodeType.InclusiveGateway:
                    node = new InclusiveGatewayNode(data);
                    break;
                case BpmnNodeType.EventBasedGateway:
                    node = new EventBasedGatewayNode(data);
                    break;
                case BpmnNodeType.SubProcess:
                    node = new SubProcessNode(data);
                    break;
                case BpmnNodeType.CallActivity:
                    node = new CallActivityNode(data);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Node type {0} is not supported", nodeType));
            }

            node.X = position.X;
            node.Y = position.Y;

            return node;
        }

        /// <summary>
        /// 获取节点默认显示名称
        /// </summary>
        private string GetDefaultDisplayName(BpmnNodeType nodeType)
        {
            switch (nodeType)
            {
                case BpmnNodeType.StartEvent: return "开始";
                case BpmnNodeType.EndEvent: return "结束";
                case BpmnNodeType.UserTask: return "用户任务";
                case BpmnNodeType.ServiceTask: return "服务任务";
                case BpmnNodeType.ScriptTask: return "脚本任务";
                case BpmnNodeType.ManualTask: return "手动任务";
                case BpmnNodeType.ExclusiveGateway: return "排他网关";
                case BpmnNodeType.ParallelGateway: return "并行网关";
                case BpmnNodeType.InclusiveGateway: return "包容网关";
                case BpmnNodeType.EventBasedGateway: return "事件网关";
                case BpmnNodeType.SubProcess: return "子流程";
                case BpmnNodeType.CallActivity: return "调用活动";
                default: return nodeType.ToString();
            }
        }

        /// <summary>
        /// 获取BPMN工具箱项目
        /// </summary>
        public List<BpmnToolboxItem> GetToolboxItems()
        {
            return new List<BpmnToolboxItem>
            {
                // 事件类别
                new BpmnToolboxItem { Category = "事件", Name = "开始事件", NodeType = BpmnNodeType.StartEvent, Icon = "●", Description = "BPMN流程开始" },
                new BpmnToolboxItem { Category = "事件", Name = "结束事件", NodeType = BpmnNodeType.EndEvent, Icon = "◉", Description = "BPMN流程结束" },
                new BpmnToolboxItem { Category = "事件", Name = "中间事件", NodeType = BpmnNodeType.IntermediateEvent, Icon = "◎", Description = "中间事件捕获/抛出" },
                
                // 任务类别
                new BpmnToolboxItem { Category = "任务", Name = "用户任务", NodeType = BpmnNodeType.UserTask, Icon = "👤", Description = "需要人工处理的任务" },
                new BpmnToolboxItem { Category = "任务", Name = "服务任务", NodeType = BpmnNodeType.ServiceTask, Icon = "⚙", Description = "自动服务调用" },
                new BpmnToolboxItem { Category = "任务", Name = "脚本任务", NodeType = BpmnNodeType.ScriptTask, Icon = "📜", Description = "执行脚本代码" },
                new BpmnToolboxItem { Category = "任务", Name = "手动任务", NodeType = BpmnNodeType.ManualTask, Icon = "✋", Description = "手动执行的任务" },
                
                // 网关类别
                new BpmnToolboxItem { Category = "网关", Name = "排他网关", NodeType = BpmnNodeType.ExclusiveGateway, Icon = "◇✕", Description = "条件分支（互斥）" },
                new BpmnToolboxItem { Category = "网关", Name = "并行网关", NodeType = BpmnNodeType.ParallelGateway, Icon = "◇+", Description = "并行分支/合并" },
                new BpmnToolboxItem { Category = "网关", Name = "包容网关", NodeType = BpmnNodeType.InclusiveGateway, Icon = "◇○", Description = "条件分支（包容）" },
                new BpmnToolboxItem { Category = "网关", Name = "事件网关", NodeType = BpmnNodeType.EventBasedGateway, Icon = "◇⬟", Description = "基于事件的分支" },
                
                // 子流程类别
                new BpmnToolboxItem { Category = "子流程", Name = "子流程", NodeType = BpmnNodeType.SubProcess, Icon = "▭", Description = "嵌套子流程" },
                new BpmnToolboxItem { Category = "子流程", Name = "调用活动", NodeType = BpmnNodeType.CallActivity, Icon = "⊞", Description = "调用外部流程" }
            };
        }

        /// <summary>
        /// 处理BPMN导入事件
        /// </summary>
        private void OnImportBpmn(object eventData)
        {
            if (eventData is string xml)
            {
                try
                {
                    var document = ImportFromXml(xml);
                    Context.EventBus.Publish("bpmn:imported", document);
                }
                catch (Exception ex)
                {
                    Context.EventBus.Publish("bpmn:importError", ex.Message);
                }
            }
        }

        /// <summary>
        /// 处理BPMN导出事件
        /// </summary>
        private void OnExportBpmn(object eventData)
        {
            if (eventData is BpmnDocument document)
            {
                try
                {
                    var xml = ExportToXml(document);
                    Context.EventBus.Publish("bpmn:exported", xml);
                }
                catch (Exception ex)
                {
                    Context.EventBus.Publish("bpmn:exportError", ex.Message);
                }
            }
        }

        public override void Render(Graphics g, RectangleF viewport)
        {
            // BPMN节点由画布统一渲染
        }

        protected override void OnConfigurationChanged()
        {
            // 应用配置变更
        }
    }

    /// <summary>
    /// 6.2.9 BPMN工具箱项目
    /// </summary>
    public class BpmnToolboxItem
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public BpmnNodeType NodeType { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}
