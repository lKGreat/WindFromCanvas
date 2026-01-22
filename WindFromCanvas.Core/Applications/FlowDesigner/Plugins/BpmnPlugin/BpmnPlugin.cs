using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin
{
    /// <summary>
    /// BPMN插件（BPMN 2.0标准支持）
    /// </summary>
    public class BpmnPlugin : FlowPluginBase
    {
        private BpmnAdapter _adapter;

        public override string PluginName => "BPMN";
        public override string DisplayName => "BPMN 2.0";
        public override string Description => "BPMN 2.0 标准流程图支持";
        public override Version Version => new Version(1, 0, 0);

        protected override void OnInitialize()
        {
            // 注册BPMN节点类型
            RegisterBpmnNodeTypes();

            // 创建并注册数据适配器
            _adapter = new BpmnAdapter();
            Context.RegisterAdapter("BPMN", _adapter);
        }

        public override void Render(System.Drawing.Graphics g, System.Drawing.RectangleF viewport)
        {
            // BPMN插件不需要自定义渲染
        }

        protected override void OnDestroy()
        {
            // 清理资源
            _adapter = null;
        }

        /// <summary>
        /// 注册BPMN节点类型
        /// </summary>
        private void RegisterBpmnNodeTypes()
        {
            // 注册BPMN标准节点类型
            // 注意：这里需要实际的Model和View类型，暂时使用占位符
            // 实际实现时需要创建对应的BpmnStartEventModel、BpmnStartEventView等类

            // Context.RegisterNodeType("bpmn:startEvent", typeof(BpmnStartEventModel), typeof(BpmnStartEventView));
            // Context.RegisterNodeType("bpmn:endEvent", typeof(BpmnEndEventModel), typeof(BpmnEndEventView));
            // Context.RegisterNodeType("bpmn:userTask", typeof(BpmnUserTaskModel), typeof(BpmnUserTaskView));
            // Context.RegisterNodeType("bpmn:serviceTask", typeof(BpmnServiceTaskModel), typeof(BpmnServiceTaskView));
            // Context.RegisterNodeType("bpmn:exclusiveGateway", typeof(BpmnExclusiveGatewayModel), typeof(BpmnExclusiveGatewayView));
            // Context.RegisterNodeType("bpmn:parallelGateway", typeof(BpmnParallelGatewayModel), typeof(BpmnParallelGatewayView));
            // Context.RegisterNodeType("bpmn:sequenceFlow", typeof(BpmnSequenceFlowModel), typeof(BpmnSequenceFlowView));
        }
    }
}
