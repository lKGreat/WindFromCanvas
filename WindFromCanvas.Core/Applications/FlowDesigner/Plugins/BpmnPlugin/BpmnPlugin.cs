using System;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin
{
    /// <summary>
    /// BPMN插件（BPMN 2.0标准支持）
    /// </summary>
    public class BpmnPlugin : IFlowPlugin
    {
        private IPluginContext _context;
        private BpmnAdapter _adapter;

        public string PluginName => "BPMN";

        public Version Version => new Version(1, 0, 0);

        public void Initialize(IPluginContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // 注册BPMN节点类型
            RegisterBpmnNodeTypes();

            // 创建并注册数据适配器
            _adapter = new BpmnAdapter();
            _context.RegisterAdapter("BPMN", _adapter);
        }

        public void Render(System.Drawing.Graphics g, System.Drawing.RectangleF viewport)
        {
            // BPMN插件不需要自定义渲染
        }

        public void Destroy()
        {
            // 清理资源
            _adapter = null;
            _context = null;
        }

        /// <summary>
        /// 注册BPMN节点类型
        /// </summary>
        private void RegisterBpmnNodeTypes()
        {
            // 注册BPMN标准节点类型
            // 注意：这里需要实际的Model和View类型，暂时使用占位符
            // 实际实现时需要创建对应的BpmnStartEventModel、BpmnStartEventView等类

            // _context.RegisterNodeType("bpmn:startEvent", typeof(BpmnStartEventModel), typeof(BpmnStartEventView));
            // _context.RegisterNodeType("bpmn:endEvent", typeof(BpmnEndEventModel), typeof(BpmnEndEventView));
            // _context.RegisterNodeType("bpmn:userTask", typeof(BpmnUserTaskModel), typeof(BpmnUserTaskView));
            // _context.RegisterNodeType("bpmn:serviceTask", typeof(BpmnServiceTaskModel), typeof(BpmnServiceTaskView));
            // _context.RegisterNodeType("bpmn:exclusiveGateway", typeof(BpmnExclusiveGatewayModel), typeof(BpmnExclusiveGatewayView));
            // _context.RegisterNodeType("bpmn:parallelGateway", typeof(BpmnParallelGatewayModel), typeof(BpmnParallelGatewayView));
            // _context.RegisterNodeType("bpmn:sequenceFlow", typeof(BpmnSequenceFlowModel), typeof(BpmnSequenceFlowView));
        }
    }
}
