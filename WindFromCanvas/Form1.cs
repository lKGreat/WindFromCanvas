using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Styles;
using WindFromCanvas.Core.Events;
using WindFromCanvas.Core.Applications.FlowDesigner;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Widgets;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;
using WindFromCanvas.Core.Applications.FlowDesigner.Animation;
using WindFromCanvas.Core.Applications.FlowDesigner.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins.DynamicGroup;

namespace WindFromCanvas
{
    public partial class Form1 : Form
    {
        private RectangleObject _draggableRect;
        private EllipseObject _animatedCircle;
        private float _animationAngle = 0f;
        
        // 流程设计器相关控件
        private CanvasControlPanel _controlPanel;
        private MinimapControl _minimapControl;
        private MenuStrip _menuStrip;
        private ToolStripMenuItem _themeMenu;
        private ShortcutManager _shortcutManager;
        
        // 新的流程设计器（基于 FlowVersion）
        private FlowCanvas _newFlowCanvas;
        private BuilderStateStore _stateStore;
        private WindFromCanvas.Core.Applications.FlowDesigner.Widgets.StepSettingsPanel _stepSettingsPanel;
        private WindFromCanvas.Core.Applications.FlowDesigner.Widgets.FlowVersionsList _versionsList;
        private WindFromCanvas.Core.Applications.FlowDesigner.Widgets.RunsList _runsList;
        private WindFromCanvas.Core.Applications.FlowDesigner.Widgets.RunDetailsPanel _runDetailsPanel;

        public Form1()
        {
            InitializeComponent();
            SetupMenu();
            SetupDemo();
            SetupFlowDesigner();
            SetupNewFlowDesigner(); // 添加新的流程设计器示例
            SetupCompleteDesignerDemo(); // 设置完整设计器演示
        }

        private void SetupMenu()
        {
            _menuStrip = new MenuStrip();
            
            // 文件菜单
            var fileMenu = new ToolStripMenuItem("文件(&F)");
            fileMenu.DropDownItems.Add("新建(&N)", null, (s, e) => flowDesignerCanvas1.Clear());
            fileMenu.DropDownItems.Add("打开(&O)...", null, (s, e) => MessageBox.Show("打开功能演示", "提示"));
            fileMenu.DropDownItems.Add("保存(&S)...", null, (s, e) => MessageBox.Show("保存功能演示", "提示"));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("退出(&X)", null, (s, e) => Close());
            
            // 视图菜单
            var viewMenu = new ToolStripMenuItem("视图(&V)");
            var minimapItem = new ToolStripMenuItem("小地图(&M)", null, (s, e) => ToggleMinimap());
            minimapItem.ShortcutKeys = Keys.Control | Keys.M;
            viewMenu.DropDownItems.Add(minimapItem);
            viewMenu.DropDownItems.Add("适应视图(&F)", null, (s, e) => flowDesignerCanvas1.FitToView());
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            _themeMenu = new ToolStripMenuItem("主题(&T)");
            _themeMenu.DropDownItems.Add("浅色主题", null, (s, e) => SwitchTheme(true));
            _themeMenu.DropDownItems.Add("深色主题", null, (s, e) => SwitchTheme(false));
            viewMenu.DropDownItems.Add(_themeMenu);
            
            // 工具菜单
            var toolsMenu = new ToolStripMenuItem("工具(&T)");
            toolsMenu.DropDownItems.Add("快捷键帮助(&H)...", null, (s, e) => ShowShortcutHelp());
            toolsMenu.DropDownItems.Add("性能监控", null, (s, e) => ShowPerformanceInfo());
            
            // 帮助菜单
            var helpMenu = new ToolStripMenuItem("帮助(&H)");
            helpMenu.DropDownItems.Add("关于(&A)...", null, (s, e) => 
                MessageBox.Show("WindFromCanvas 流程设计器\n\n基于Activepieces设计\n支持35+功能特性", 
                    "关于", MessageBoxButtons.OK, MessageBoxIcon.Information));
            
            // 演示菜单
            var demoMenu = new ToolStripMenuItem("演示(&D)");
            demoMenu.DropDownItems.Add("创建新流程设计器", null, (s, e) => CreateCompleteFlowDesignerDemo());
            demoMenu.DropDownItems.Add("演示操作命令", null, (s, e) => ShowOperationDemo());
            demoMenu.DropDownItems.Add("演示版本管理", null, (s, e) => ShowVersionDemo());
            demoMenu.DropDownItems.Add("演示运行系统", null, (s, e) => ShowRunDemo());
            demoMenu.DropDownItems.Add(new ToolStripSeparator());
            demoMenu.DropDownItems.Add("LogicFlow架构演示", null, (s, e) => ShowLogicFlowDemo());
            demoMenu.DropDownItems.Add("演示所有节点类型", null, (s, e) => ShowAllNodeTypesDemo());
            demoMenu.DropDownItems.Add("演示BPMN节点", null, (s, e) => ShowBpmnNodesDemo());
            demoMenu.DropDownItems.Add(new ToolStripSeparator());
            demoMenu.DropDownItems.Add("完整设计器-连接节点", null, (s, e) => DemoConnectNodes());
            demoMenu.DropDownItems.Add("完整设计器-删除节点", null, (s, e) => DemoDeleteNode());
            demoMenu.DropDownItems.Add("完整设计器-删除连接", null, (s, e) => DemoDeleteConnection());
            
            _menuStrip.Items.AddRange(new[] { fileMenu, viewMenu, toolsMenu, demoMenu, helpMenu });
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);
        }

        /// <summary>
        /// 显示操作命令演示
        /// </summary>
        private void ShowOperationDemo()
        {
            var demoForm = new Form
            {
                Text = "操作命令演示",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            var textBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                Text = @"操作命令演示
================

1. 添加动作 (ADD_ACTION)
   - 在指定步骤后添加新动作
   - 支持添加到循环内 (INSIDE_LOOP)
   - 支持添加到分支内 (INSIDE_BRANCH)

2. 删除动作 (DELETE_ACTION)
   - 删除指定步骤
   - 自动处理连接关系

3. 移动动作 (MOVE_ACTION)
   - 将动作移动到新位置
   - 支持移动到循环或分支内

4. 更新动作 (UPDATE_ACTION)
   - 更新动作的属性和设置

5. 复制动作 (DUPLICATE_ACTION)
   - 复制动作并生成新名称

6. 分支操作
   - ADD_BRANCH: 添加分支
   - DELETE_BRANCH: 删除分支
   - DUPLICATE_BRANCH: 复制分支
   - MOVE_BRANCH: 移动分支

7. 跳过功能 (SET_SKIP_ACTION)
   - 跳过/取消跳过动作

8. 备注操作
   - ADD_NOTE: 添加备注
   - UPDATE_NOTE: 更新备注
   - DELETE_NOTE: 删除备注

所有操作都支持撤销/重做 (Ctrl+Z / Ctrl+Y)"
            };

            demoForm.Controls.Add(textBox);
            demoForm.ShowDialog(this);
        }

        /// <summary>
        /// 显示版本管理演示
        /// </summary>
        private void ShowVersionDemo()
        {
            var demoForm = new Form
            {
                Text = "版本管理演示",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            var textBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                Text = @"版本管理演示
================

功能：
1. 保存版本
   - VersionManager.Instance.SaveVersion(version)

2. 获取所有版本
   - VersionManager.Instance.GetVersions(flowId)

3. 创建新版本（从现有版本复制）
   - VersionManager.Instance.CreateVersionFrom(flowId, sourceVersionId, newDisplayName)

4. 发布版本
   - VersionManager.Instance.PublishVersion(flowId, versionId)

5. 使用版本作为草稿
   - VersionManager.Instance.UseAsDraft(flowId, versionId)

版本状态：
- DRAFT: 草稿
- LOCKED: 已锁定（已发布）

UI组件：
- FlowVersionsList: 版本列表组件
- 支持查看、切换、恢复版本"
            };

            demoForm.Controls.Add(textBox);
            demoForm.ShowDialog(this);
        }

        /// <summary>
        /// 显示运行系统演示
        /// </summary>
        private void ShowRunDemo()
        {
            var demoForm = new Form
            {
                Text = "运行系统演示",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            var textBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                Text = @"运行系统演示
================

功能：
1. 运行列表 (RunsList)
   - 显示流程运行历史
   - 显示运行状态、时间、持续时间

2. 运行详情 (RunDetailsPanel)
   - 显示步骤执行顺序
   - 显示每个步骤的输入/输出
   - 显示执行时间和错误信息

运行状态：
- RUNNING: 运行中
- SUCCESS: 成功
- FAILED: 失败

使用示例：
var run = new FlowRun
{
    Status = FlowRunStatus.SUCCESS,
    StartTime = DateTime.Now,
    Duration = TimeSpan.FromSeconds(30),
    StepsExecuted = 3
};

run.StepOutputs[""step1""] = new StepOutput
{
    Input = new { data = ""test"" },
    Output = new { result = ""processed"" },
    Success = true
};"
            };

            demoForm.Controls.Add(textBox);
            demoForm.ShowDialog(this);
        }

        /// <summary>
        /// 显示LogicFlow架构演示
        /// </summary>
        private void ShowLogicFlowDemo()
        {
            var demoForm = new LogicFlowDemo();
            demoForm.ShowDialog(this);
        }

        /// <summary>
        /// 演示所有节点类型
        /// </summary>
        private void ShowAllNodeTypesDemo()
        {
            flowDesignerCanvas1.Clear();

            float startX = 50;
            float startY = 50;
            float spacing = 280;
            float ySpacing = 120;

            // === 基础节点类型 ===
            
            // 1. Start节点
            var startData = new FlowNodeData
            {
                Name = "start",
                DisplayName = "开始",
                Type = FlowNodeType.Start,
                Position = new PointF(startX, startY),
                Description = "流程起始点，只有输出端口"
            };
            flowDesignerCanvas1.AddNode(new StartNode(startData));

            // 2. Process节点
            var processData = new FlowNodeData
            {
                Name = "process",
                DisplayName = "处理节点",
                Type = FlowNodeType.Process,
                Position = new PointF(startX + spacing, startY),
                Description = "执行操作或任务"
            };
            flowDesignerCanvas1.AddNode(new ProcessNode(processData));

            // 3. Decision节点
            var decisionData = new FlowNodeData
            {
                Name = "decision",
                DisplayName = "判断节点",
                Type = FlowNodeType.Decision,
                Position = new PointF(startX + spacing * 2, startY),
                Description = "条件分支，2个输出端口"
            };
            flowDesignerCanvas1.AddNode(new DecisionNode(decisionData));

            // 4. End节点
            var endData = new FlowNodeData
            {
                Name = "end",
                DisplayName = "结束",
                Type = FlowNodeType.End,
                Position = new PointF(startX + spacing * 3, startY),
                Description = "流程终止点，只有输入端口"
            };
            flowDesignerCanvas1.AddNode(new EndNode(endData));

            // === 第二行：控制节点和数据节点 ===

            // 5. Loop节点
            var loopData = new FlowNodeData
            {
                Name = "loop",
                DisplayName = "循环节点",
                Type = FlowNodeType.Loop,
                Position = new PointF(startX, startY + ySpacing),
                Description = "重复执行一组操作"
            };
            flowDesignerCanvas1.AddNode(new LoopNode(loopData));

            // 6. Code节点
            var codeData = new FlowNodeData
            {
                Name = "code",
                DisplayName = "代码节点",
                Type = FlowNodeType.Code,
                Position = new PointF(startX + spacing, startY + ySpacing),
                Description = "执行自定义代码脚本"
            };
            codeData.Properties["script"] = "// JavaScript代码\nreturn { result: 'success' };";
            codeData.Properties["scriptLanguage"] = "javascript";
            flowDesignerCanvas1.AddNode(new CodeNode(codeData));

            // 7. Piece节点
            var pieceData = new FlowNodeData
            {
                Name = "piece",
                DisplayName = "组件节点",
                Type = FlowNodeType.Piece,
                Position = new PointF(startX + spacing * 2, startY + ySpacing),
                Description = "引用可复用组件"
            };
            pieceData.Properties["pieceId"] = "email-component";
            pieceData.Properties["pieceVersion"] = "1.0.0";
            flowDesignerCanvas1.AddNode(new PieceNode(pieceData));

            // 8. Group节点
            var groupData = new GroupNodeData
            {
                Name = "group",
                Type = FlowNodeType.Group,
                PositionX = startX + spacing * 3,
                PositionY = startY + ySpacing,
                GroupWidth = 250,
                GroupHeight = 180,
                IsCollapsed = false
            };
            var groupNode = new GroupNode(groupData);
            flowDesignerCanvas1.AddObject(groupNode);

            // 显示提示
            ToastNotification.Show("已展示所有基础节点类型（8种）\n\n包括：Start、End、Process、Decision、Loop、Code、Piece、Group", ToastType.Success, this);
        }

        /// <summary>
        /// 演示BPMN节点
        /// </summary>
        private void ShowBpmnNodesDemo()
        {
            flowDesignerCanvas1.Clear();

            float startX = 50;
            float startY = 50;
            float spacing = 150;
            float ySpacing = 120;

            // === BPMN事件节点 ===
            
            // 1. StartEvent
            var startEventData = new BpmnNodeData
            {
                Name = "startEvent",
                DisplayName = "开始事件",
                BpmnType = BpmnNodeType.StartEvent,
                Type = FlowNodeType.Start,
                PositionX = startX,
                PositionY = startY
            };
            flowDesignerCanvas1.AddNode(new StartEventNode(startEventData));

            // 2. IntermediateEvent
            var intermediateEventData = new BpmnNodeData
            {
                Name = "intermediateEvent",
                DisplayName = "中间事件",
                BpmnType = BpmnNodeType.IntermediateEvent,
                Type = FlowNodeType.Process,
                PositionX = startX + spacing,
                PositionY = startY
            };
            flowDesignerCanvas1.AddNode(new IntermediateEventNode(intermediateEventData));

            // 3. EndEvent
            var endEventData = new BpmnNodeData
            {
                Name = "endEvent",
                DisplayName = "结束事件",
                BpmnType = BpmnNodeType.EndEvent,
                Type = FlowNodeType.End,
                PositionX = startX + spacing * 2,
                PositionY = startY
            };
            flowDesignerCanvas1.AddNode(new EndEventNode(endEventData));

            // === BPMN任务节点 ===

            // 4. UserTask
            var userTaskData = new BpmnNodeData
            {
                Name = "userTask",
                DisplayName = "用户任务",
                BpmnType = BpmnNodeType.UserTask,
                Type = FlowNodeType.Process,
                PositionX = startX,
                PositionY = startY + ySpacing,
                Assignee = "admin"
            };
            flowDesignerCanvas1.AddNode(new UserTaskNode(userTaskData));

            // 5. ServiceTask
            var serviceTaskData = new BpmnNodeData
            {
                Name = "serviceTask",
                DisplayName = "服务任务",
                BpmnType = BpmnNodeType.ServiceTask,
                Type = FlowNodeType.Process,
                PositionX = startX + spacing,
                PositionY = startY + ySpacing
            };
            flowDesignerCanvas1.AddNode(new ServiceTaskNode(serviceTaskData));

            // 6. ScriptTask
            var scriptTaskData = new BpmnNodeData
            {
                Name = "scriptTask",
                DisplayName = "脚本任务",
                BpmnType = BpmnNodeType.ScriptTask,
                Type = FlowNodeType.Code,
                PositionX = startX + spacing * 2,
                PositionY = startY + ySpacing,
                ScriptFormat = "javascript",
                Script = "console.log('Hello BPMN');"
            };
            flowDesignerCanvas1.AddNode(new ScriptTaskNode(scriptTaskData));

            // 7. ManualTask
            var manualTaskData = new BpmnNodeData
            {
                Name = "manualTask",
                DisplayName = "手动任务",
                BpmnType = BpmnNodeType.ManualTask,
                Type = FlowNodeType.Process,
                PositionX = startX + spacing * 3,
                PositionY = startY + ySpacing
            };
            flowDesignerCanvas1.AddNode(new ManualTaskNode(manualTaskData));

            // === BPMN网关节点 ===

            // 8. ExclusiveGateway
            var exclusiveGatewayData = new BpmnNodeData
            {
                Name = "exclusiveGateway",
                DisplayName = "排他网关",
                BpmnType = BpmnNodeType.ExclusiveGateway,
                Type = FlowNodeType.Decision,
                PositionX = startX,
                PositionY = startY + ySpacing * 2
            };
            flowDesignerCanvas1.AddNode(new ExclusiveGatewayNode(exclusiveGatewayData));

            // 9. ParallelGateway
            var parallelGatewayData = new BpmnNodeData
            {
                Name = "parallelGateway",
                DisplayName = "并行网关",
                BpmnType = BpmnNodeType.ParallelGateway,
                Type = FlowNodeType.Decision,
                PositionX = startX + spacing,
                PositionY = startY + ySpacing * 2
            };
            flowDesignerCanvas1.AddNode(new ParallelGatewayNode(parallelGatewayData));

            // 10. InclusiveGateway
            var inclusiveGatewayData = new BpmnNodeData
            {
                Name = "inclusiveGateway",
                DisplayName = "包容网关",
                BpmnType = BpmnNodeType.InclusiveGateway,
                Type = FlowNodeType.Decision,
                PositionX = startX + spacing * 2,
                PositionY = startY + ySpacing * 2
            };
            flowDesignerCanvas1.AddNode(new InclusiveGatewayNode(inclusiveGatewayData));

            // 11. EventBasedGateway
            var eventGatewayData = new BpmnNodeData
            {
                Name = "eventGateway",
                DisplayName = "事件网关",
                BpmnType = BpmnNodeType.EventBasedGateway,
                Type = FlowNodeType.Decision,
                PositionX = startX + spacing * 3,
                PositionY = startY + ySpacing * 2
            };
            flowDesignerCanvas1.AddNode(new EventBasedGatewayNode(eventGatewayData));

            // === BPMN子流程节点 ===

            // 12. SubProcess
            var subProcessData = new BpmnNodeData
            {
                Name = "subProcess",
                DisplayName = "子流程",
                BpmnType = BpmnNodeType.SubProcess,
                Type = FlowNodeType.Group,
                PositionX = startX,
                PositionY = startY + ySpacing * 3
            };
            flowDesignerCanvas1.AddNode(new SubProcessNode(subProcessData));

            // 13. CallActivity
            var callActivityData = new BpmnNodeData
            {
                Name = "callActivity",
                DisplayName = "调用活动",
                BpmnType = BpmnNodeType.CallActivity,
                Type = FlowNodeType.Process,
                PositionX = startX + spacing * 2,
                PositionY = startY + ySpacing * 3
            };
            callActivityData.Properties["calledElement"] = "external-process";
            flowDesignerCanvas1.AddNode(new CallActivityNode(callActivityData));

            // 适应视图
            flowDesignerCanvas1.FitToView();

            // 显示提示
            ToastNotification.Show("已展示所有BPMN节点类型（13种）\n\n包括：\n事件（3种）、任务（4种）、网关（4种）、子流程（2种）", ToastType.Info, this);
        }

        private void SetupFlowDesigner()
        {
            // 设置工具箱和属性面板
            flowDesignerCanvas1.Toolbox = toolboxPanel1;
            flowDesignerCanvas1.PropertiesPanel = propertiesPanel1;

            // 创建底部控制面板
            _controlPanel = new CanvasControlPanel(flowDesignerCanvas1);
            _controlPanel.MinimapToggleRequested += (s, e) => ToggleMinimap();
            _controlPanel.ZoomInRequested += (s, e) => flowDesignerCanvas1.ZoomIn();
            _controlPanel.ZoomOutRequested += (s, e) => flowDesignerCanvas1.ZoomOut();
            _controlPanel.FitToViewRequested += (s, e) => flowDesignerCanvas1.FitToView();
            _controlPanel.AddNoteRequested += (s, e) => AddNoteNode();
            tabPageFlowDesigner.Controls.Add(_controlPanel);
            _controlPanel.BringToFront();

            // 创建小地图（默认隐藏）
            _minimapControl = new MinimapControl(flowDesignerCanvas1);
            _minimapControl.Location = new Point(10, 50);
            _minimapControl.Size = new Size(150, 100);
            _minimapControl.Visible = false;
            tabPageFlowDesigner.Controls.Add(_minimapControl);
            _minimapControl.BringToFront();

            // 注册快捷键
            RegisterShortcuts();

            // 创建丰富的示例流程
            CreateDemoFlow();
        }

        private void CreateDemoFlow()
        {
            // 1. 开始节点（带状态）
            var startNodeData = new FlowNodeData
            {
                Name = "start",
                DisplayName = "开始",
                Type = FlowNodeType.Start,
                Position = new PointF(100, 150),
                Status = NodeStatus.Success
            };
            var startNode = new StartNode(startNodeData);
            flowDesignerCanvas1.AddNode(startNode);

            // 2. 处理节点（带图标和状态）
            var processNodeData = new FlowNodeData
            {
                Name = "process1",
                DisplayName = "数据处理",
                Type = FlowNodeType.Process,
                Position = new PointF(350, 150),
                Description = "执行数据处理操作",
                Status = NodeStatus.Running
            };
            var processNode = new ProcessNode(processNodeData);
            flowDesignerCanvas1.AddNode(processNode);

            // 3. 判断节点
            var decisionNodeData = new FlowNodeData
            {
                Name = "decision1",
                DisplayName = "条件判断",
                Type = FlowNodeType.Decision,
                Position = new PointF(600, 150),
                Description = "判断数据是否有效"
            };
            var decisionNode = new DecisionNode(decisionNodeData);
            flowDesignerCanvas1.AddNode(decisionNode);

            // 4. 循环节点
            var loopNodeData = new FlowNodeData
            {
                Name = "loop1",
                DisplayName = "循环处理",
                Type = FlowNodeType.Loop,
                Position = new PointF(350, 350),
                Description = "循环处理数组数据"
            };
            var loopNode = new LoopNode(loopNodeData);
            flowDesignerCanvas1.AddNode(loopNode);

            // 5. Code节点（新增）
            var codeNodeData = new FlowNodeData
            {
                Name = "code1",
                DisplayName = "执行代码",
                Type = FlowNodeType.Code,
                Position = new PointF(600, 350),
                Description = "执行自定义JavaScript代码"
            };
            codeNodeData.Properties["script"] = "// 示例代码\nreturn { processed: true };";
            codeNodeData.Properties["scriptLanguage"] = "javascript";
            var codeNode = new CodeNode(codeNodeData);
            flowDesignerCanvas1.AddNode(codeNode);

            // 6. Piece节点（新增）
            var pieceNodeData = new FlowNodeData
            {
                Name = "piece1",
                DisplayName = "发送邮件",
                Type = FlowNodeType.Piece,
                Position = new PointF(100, 500),
                Description = "邮件组件"
            };
            pieceNodeData.Properties["pieceId"] = "email-sender";
            pieceNodeData.Properties["pieceVersion"] = "1.0.0";
            pieceNodeData.Properties["pieceType"] = "notification";
            var pieceNode = new PieceNode(pieceNodeData);
            flowDesignerCanvas1.AddNode(pieceNode);

            // 7. 结束节点
            var endNodeData = new FlowNodeData
            {
                Name = "end",
                DisplayName = "结束",
                Type = FlowNodeType.End,
                Position = new PointF(850, 150),
                Status = NodeStatus.None
            };
            var endNode = new EndNode(endNodeData);
            flowDesignerCanvas1.AddNode(endNode);

            // 创建连接（带标签）
            var conn1 = flowDesignerCanvas1.CreateConnection(startNode, processNode);
            if (conn1 != null && conn1.Data != null)
            {
                conn1.Data.Label = "开始流程";
            }

            var conn2 = flowDesignerCanvas1.CreateConnection(processNode, decisionNode);
            if (conn2 != null && conn2.Data != null)
            {
                conn2.Data.Label = "处理完成";
            }

            var conn3 = flowDesignerCanvas1.CreateConnection(decisionNode, loopNode);
            if (conn3 != null && conn3.Data != null)
            {
                conn3.Data.Label = "需要循环";
            }

            var conn4 = flowDesignerCanvas1.CreateConnection(decisionNode, endNode);
            if (conn4 != null && conn4.Data != null)
            {
                conn4.Data.Label = "完成";
            }

            // 演示：设置节点状态（模拟运行状态）
            var statusTimer = new Timer { Interval = 3000 };
            statusTimer.Tick += (s, e) =>
            {
                // 切换处理节点的状态
                if (processNodeData.Status == NodeStatus.Running)
                {
                    processNodeData.Status = NodeStatus.Success;
                }
                else
                {
                    processNodeData.Status = NodeStatus.Running;
                }
                flowDesignerCanvas1.Invalidate();
            };
            statusTimer.Start();

            // 创建循环返回连接
            var loopReturnConn = flowDesignerCanvas1.CreateConnection(loopNode, processNode);
            if (loopReturnConn != null)
            {
                loopReturnConn.IsLoopReturn = true;
                // 设置避让节点
                loopReturnConn.AvoidanceNodes = new System.Collections.Generic.List<FlowNode> { decisionNode };
            }

            // 添加笔记节点
            var noteData = new NoteData
            {
                Id = Guid.NewGuid().ToString(),
                Content = "这是一个演示笔记\n\n功能说明：\n- 支持多行文本\n- 可调整大小\n- 多种颜色主题\n- 双击编辑",
                PositionX = 100,
                PositionY = 400,
                Width = 200,
                Height = 150,
                Color = NoteColorVariant.Yellow
            };
            var noteNode = new NoteNode(noteData);
            flowDesignerCanvas1.AddObject(noteNode);

            // 演示Toast通知
            var toastTimer = new Timer { Interval = 2000 };
            toastTimer.Tick += (s, e) =>
            {
                ToastNotification.Show("欢迎使用流程设计器！\n所有35个功能已实现完成", ToastType.Info, this);
                toastTimer.Stop();
            };
            toastTimer.Start();

            // 演示：添加节点时自动触发淡入动画（已在AddNode中实现）
            // 注意：选中节点的动画可以通过监听选择变化来实现
            
            // 定期更新控制面板的缩放标签
            var updateTimer = new Timer { Interval = 100 };
            updateTimer.Tick += (s, e) => _controlPanel?.UpdateZoomLabel();
            updateTimer.Start();
        }

        /// <summary>
        /// 设置新的流程设计器（基于 FlowVersion，匹配 Activepieces）
        /// </summary>
        private void SetupNewFlowDesigner()
        {
            // 初始化状态存储
            _stateStore = BuilderStateStore.Instance;
            
            // 创建新的流程版本
            var flowVersion = new FlowVersion
            {
                Id = Guid.NewGuid().ToString(),
                FlowId = Guid.NewGuid().ToString(),
                DisplayName = "示例流程",
                Trigger = new EmptyTrigger
                {
                    Name = "trigger",
                    DisplayName = "触发器"
                },
                State = FlowVersionState.DRAFT
            };

            // 初始化状态
            _stateStore.Initialize(flowVersion, false);

            // 注意：不在这里调用 CreateNewFlowDesignerDemo()
            // 演示操作应该通过菜单项手动触发，避免在启动时执行过多操作
        }

        /// <summary>
        /// 创建新的流程设计器演示
        /// </summary>
        private void CreateNewFlowDesignerDemo()
        {
            // 演示1: 创建流程版本并添加动作
            DemoCreateFlowWithActions();

            // 演示2: 使用操作命令添加动作
            DemoAddActionOperation();

            // 演示3: 使用操作命令添加分支
            DemoAddBranchOperation();

            // 演示4: 使用操作命令添加备注
            DemoAddNoteOperation();

            // 演示5: 使用快捷键
            DemoShortcuts();

            // 演示6: 使用上下文菜单
            DemoContextMenu();

            // 演示7: 版本管理
            DemoVersionManagement();

            // 演示8: 运行系统
            DemoRunSystem();
        }

        /// <summary>
        /// 演示1: 创建流程版本并添加动作
        /// </summary>
        private void DemoCreateFlowWithActions()
        {
            var flowVersion = _stateStore.Flow.FlowVersion;
            var trigger = flowVersion.Trigger;

            // 创建代码动作
            var codeAction = new CodeAction
            {
                Name = "step1",
                DisplayName = "处理数据",
                Type = FlowActionType.CODE,
                Settings = new CodeActionSettings
                {
                    SourceCode = new SourceCode
                    {
                        Code = "return { result: input.data }",
                        PackageJson = "{}"
                    }
                }
            };

            // 创建循环动作
            var loopAction = new LoopOnItemsAction
            {
                Name = "step2",
                DisplayName = "循环处理",
                Type = FlowActionType.LOOP_ON_ITEMS,
                Settings = new LoopOnItemsActionSettings
                {
                    Items = "{{trigger.body.items}}"
                }
            };

            // 创建路由动作
            var routerAction = new RouterAction
            {
                Name = "step3",
                DisplayName = "条件路由",
                Type = FlowActionType.ROUTER,
                Settings = new RouterActionSettings
                {
                    ExecutionType = RouterExecutionType.EXECUTE_FIRST_MATCH,
                    Branches = new System.Collections.Generic.List<WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.RouterBranch>
                    {
                        new WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.RouterBranch
                        {
                            BranchName = "条件1",
                            BranchType = BranchExecutionType.CONDITION,
                            Conditions = new System.Collections.Generic.List<System.Collections.Generic.List<BranchCondition>>
                            {
                                new System.Collections.Generic.List<BranchCondition>
                                {
                                    new BranchCondition
                                    {
                                        FirstValue = "{{step1.result}}",
                                        SecondValue = "success",
                                        Operator = BranchOperator.TEXT_EXACTLY_MATCHES
                                    }
                                }
                            }
                        },
                        new WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.RouterBranch
                        {
                            BranchName = "回退",
                            BranchType = BranchExecutionType.FALLBACK
                        }
                    }
                },
                Children = new System.Collections.Generic.List<FlowAction> { null, null }
            };

            // 连接动作
            trigger.NextAction = codeAction;
            codeAction.NextAction = loopAction;
            loopAction.NextAction = routerAction;

            // 更新状态
            _stateStore.Flow.FlowVersion = flowVersion;
            _stateStore.OnPropertyChanged(nameof(BuilderStateStore.Flow));
        }

        /// <summary>
        /// 演示2: 使用操作命令添加动作
        /// </summary>
        private void DemoAddActionOperation()
        {
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_ACTION,
                Request = new AddActionRequest
                {
                    Action = new CodeAction
                    {
                        Name = "newStep",
                        DisplayName = "新步骤",
                        Type = FlowActionType.CODE,
                        Settings = new CodeActionSettings
                        {
                            SourceCode = new SourceCode
                            {
                                Code = "console.log('Hello World');",
                                PackageJson = "{}"
                            }
                        }
                    },
                    ParentStepName = "step1",
                    StepLocationRelativeToParent = StepLocationRelativeToParent.AFTER
                }
            };

            _stateStore.ApplyOperation(operation);
        }

        /// <summary>
        /// 演示3: 使用操作命令添加分支
        /// </summary>
        private void DemoAddBranchOperation()
        {
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_BRANCH,
                Request = new AddBranchRequest
                {
                    StepName = "step3",
                    BranchIndex = 1,
                    BranchName = "新分支",
                    Conditions = new System.Collections.Generic.List<System.Collections.Generic.List<BranchCondition>>
                    {
                        new System.Collections.Generic.List<BranchCondition>
                        {
                            new BranchCondition
                            {
                                FirstValue = "{{step1.result}}",
                                SecondValue = "error",
                                Operator = BranchOperator.TEXT_CONTAINS
                            }
                        }
                    }
                }
            };

            _stateStore.ApplyOperation(operation);
        }

        /// <summary>
        /// 演示4: 使用操作命令添加备注
        /// </summary>
        private void DemoAddNoteOperation()
        {
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_NOTE,
                Request = new AddNoteRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "这是一个演示备注\n\n功能说明：\n- 支持添加备注\n- 支持编辑备注\n- 支持移动备注\n- 支持删除备注",
                    Position = new PointF(500, 300),
                    Size = new SizeF(200, 150),
                    Color = NoteColorVariant.Green
                }
            };

            _stateStore.ApplyOperation(operation);
        }

        /// <summary>
        /// 演示5: 使用快捷键
        /// </summary>
        private void DemoShortcuts()
        {
            // 快捷键已在 ShortcutManager 中注册
            // Ctrl+C: 复制选中节点
            // Ctrl+V: 粘贴节点
            // Shift+Delete: 删除选中节点
            // Ctrl+E: 跳过/取消跳过选中节点
            // Ctrl+M: 切换小地图
            // Escape: 退出拖拽

            // 示例：注册自定义快捷键
            var shortcutManager = new WindFromCanvas.Core.Applications.FlowDesigner.Interaction.ShortcutManager(_stateStore);
            // 快捷键已自动注册，无需手动调用
        }

        /// <summary>
        /// 演示6: 使用上下文菜单
        /// </summary>
        private void DemoContextMenu()
        {
            // 上下文菜单功能已集成到 FlowCanvas
            // 右键点击节点或空白区域即可显示菜单
            // 菜单项包括：
            // - Replace（替换）
            // - Copy（复制）
            // - Duplicate（复制）
            // - Skip/Unskip（跳过/取消跳过）
            // - Paste After（粘贴在后面）
            // - Paste Inside Loop（粘贴到循环内）
            // - Paste Inside Branch（粘贴到分支内）
            // - Delete（删除）
        }

        /// <summary>
        /// 演示7: 版本管理
        /// </summary>
        private void DemoVersionManagement()
        {
            var versionManager = VersionManager.Instance;
            var flowVersion = _stateStore.Flow.FlowVersion;

            // 保存当前版本
            versionManager.SaveVersion(flowVersion);

            // 创建新版本（从当前版本复制）
            var newVersion = versionManager.CreateVersionFrom(
                flowVersion.FlowId,
                flowVersion.Id,
                "版本 2"
            );

            // 获取所有版本
            var versions = versionManager.GetVersions(flowVersion.FlowId);
            Console.WriteLine($"流程共有 {versions.Count} 个版本");

            // 发布版本
            versionManager.PublishVersion(flowVersion.FlowId, flowVersion.Id);

            // 使用版本作为草稿
            var draftVersion = versionManager.UseAsDraft(flowVersion.FlowId, flowVersion.Id);
        }

        /// <summary>
        /// 演示8: 运行系统
        /// </summary>
        private void DemoRunSystem()
        {
            // 创建模拟运行记录
            var run = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.FlowRun
            {
                Id = Guid.NewGuid().ToString(),
                FlowId = _stateStore.Flow.FlowVersion.FlowId,
                FlowVersionId = _stateStore.Flow.FlowVersion.Id,
                Status = FlowRunStatus.SUCCESS,
                StartTime = DateTime.Now.AddMinutes(-5),
                EndTime = DateTime.Now,
                Duration = TimeSpan.FromSeconds(30),
                StepsExecuted = 3
            };

            // 添加步骤输出
            run.StepOutputs["step1"] = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.StepOutput
            {
                StepName = "step1",
                Input = new { data = "test" },
                Output = new { result = "processed" },
                Success = true,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            run.StepOutputs["step2"] = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.StepOutput
            {
                StepName = "step2",
                Input = new { items = new[] { "item1", "item2" } },
                Output = new { processed = 2 },
                Success = true,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            // 运行列表会自动显示运行记录
            // 双击运行记录可以查看详情
        }

        /// <summary>
        /// 演示：在新标签页中创建完整的流程设计器
        /// </summary>
        private void CreateCompleteFlowDesignerDemo()
        {
            try
            {
                // 确保状态存储已初始化
                if (_stateStore == null)
                {
                    _stateStore = BuilderStateStore.Instance;
                    if (_stateStore.Flow?.FlowVersion == null)
                    {
                        // 创建新的流程版本
                        var flowVersion = new FlowVersion
                        {
                            Id = Guid.NewGuid().ToString(),
                            FlowId = Guid.NewGuid().ToString(),
                            DisplayName = "示例流程",
                            Trigger = new EmptyTrigger
                            {
                                Name = "trigger",
                                DisplayName = "触发器"
                            },
                            State = FlowVersionState.DRAFT
                        };
                        _stateStore.Initialize(flowVersion, false);
                    }
                }

                // 创建新的标签页用于展示新的流程设计器
                var newTabPage = new TabPage("新流程设计器");
                newTabPage.UseVisualStyleBackColor = true;

                // 创建分割容器
                var mainSplitContainer = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 500,
                    Parent = newTabPage
                };

                // 创建右侧分割容器（用于步骤设置和版本列表）
                var rightSplitContainer = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Vertical,
                    SplitterDistance = 300,
                    Parent = mainSplitContainer.Panel2
                };

                // 创建新的 FlowCanvas
                _newFlowCanvas = new WindFromCanvas.Core.Applications.FlowDesigner.Canvas.FlowCanvas();
                _newFlowCanvas.Dock = DockStyle.Fill;
                mainSplitContainer.Panel1.Controls.Add(_newFlowCanvas);

                // 创建步骤设置面板
                _stepSettingsPanel = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.StepSettingsPanel(_stateStore);
                _stepSettingsPanel.Dock = DockStyle.Fill;
                rightSplitContainer.Panel1.Controls.Add(_stepSettingsPanel);

                // 创建版本列表
                _versionsList = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.FlowVersionsList(_stateStore);
                _versionsList.Dock = DockStyle.Fill;
                _versionsList.VersionSelected += (s, version) =>
                {
                    if (_stateStore != null && version != null)
                    {
                        _stateStore.Flow.FlowVersion = version;
                        _stateStore.OnPropertyChanged(nameof(BuilderStateStore.Flow));
                    }
                };
                rightSplitContainer.Panel2.Controls.Add(_versionsList);

                // 添加到标签控件
                tabControl1.TabPages.Add(newTabPage);
                tabControl1.SelectedTab = newTabPage;

                MessageBox.Show("新流程设计器已创建！\n\n您可以通过菜单中的演示选项来测试各种功能。", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建流程设计器时出错：\n{ex.Message}\n\n堆栈跟踪：\n{ex.StackTrace}", 
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegisterShortcuts()
        {
            _shortcutManager = new ShortcutManager();
            
            // Ctrl+C: 复制
            _shortcutManager.Register(FlowDesignerShortcuts.Copy, Keys.Control | Keys.C, () =>
            {
                MessageBox.Show("复制功能演示", "快捷键", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            // Ctrl+V: 粘贴
            _shortcutManager.Register(FlowDesignerShortcuts.Paste, Keys.Control | Keys.V, () =>
            {
                MessageBox.Show("粘贴功能演示", "快捷键", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            // Ctrl+M: 切换小地图
            _shortcutManager.Register(FlowDesignerShortcuts.Minimap, Keys.Control | Keys.M, () =>
            {
                ToggleMinimap();
            });

            // F1: 快捷键帮助
            _shortcutManager.Register("Help", Keys.F1, () =>
            {
                ShowShortcutHelp();
            });

            // 注册到画布的KeyDown事件
            flowDesignerCanvas1.KeyDown += (s, e) =>
            {
                if (_shortcutManager.HandleKeyDown(e.KeyData))
                {
                    e.Handled = true;
                }
            };
        }

        private void ToggleMinimap()
        {
            if (_minimapControl != null)
            {
                _minimapControl.Visible = !_minimapControl.Visible;
                _controlPanel?.SetMinimapState(_minimapControl.Visible);
            }
        }

        private void SwitchTheme(bool isLight)
        {
            if (isLight)
            {
                ThemeManager.Instance.SetTheme(new LightTheme());
            }
            else
            {
                ThemeManager.Instance.SetTheme(new DarkTheme());
            }
            
            // 刷新画布
            flowDesignerCanvas1.Invalidate();
            _controlPanel?.Invalidate();
        }

        private void ShowShortcutHelp()
        {
            if (_shortcutManager != null)
            {
                using (var dialog = new ShortcutHelpDialog(_shortcutManager))
                {
                    dialog.ShowDialog(this);
                }
            }
        }

        private void ShowPerformanceInfo()
        {
            var perf = PerformanceMonitor.Instance;
            var message = $"性能监控信息\n\n" +
                         $"当前FPS: {perf.GetCurrentFPS():F1}\n" +
                         $"平均FPS: {perf.GetAverageFPS():F1}\n" +
                         $"最低FPS: {perf.GetMinFPS():F1}\n" +
                         $"建议LOD级别: {perf.GetSuggestedLODLevel()}\n" +
                         $"性能状态: {(perf.IsPerformanceGood() ? "良好" : "需要优化")}";
            
            MessageBox.Show(message, "性能监控", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddNoteNode()
        {
            var noteData = new NoteData
            {
                Id = Guid.NewGuid().ToString(),
                Content = "新笔记\n双击编辑内容",
                PositionX = 200,
                PositionY = 200,
                Width = 150,
                Height = 100,
                Color = NoteColorVariant.Blue
            };
            var noteNode = new NoteNode(noteData);
            flowDesignerCanvas1.AddObject(noteNode);
            
            // 显示成功提示
            ToastNotification.Show("笔记已添加", ToastType.Success, this);
        }

        private void SetupDemo()
        {
            var ctx = canvas1.GetContext2D();

            // ========== 演示1: 基本图形绘制 ==========
            DemoBasicShapes(ctx);

            // ========== 演示2: 路径绘制 ==========
            DemoPathDrawing(ctx);

            // ========== 演示3: 文本绘制 ==========
            DemoTextDrawing(ctx);

            // ========== 演示4: 渐变效果 ==========
            DemoGradient(ctx);

            // ========== 演示5: 可拖拽对象 ==========
            DemoDraggableObject();

            // ========== 演示6: 鼠标事件 ==========
            DemoMouseEvents();

            // ========== 演示7: 动画效果 ==========
            DemoAnimation();
        }

        // 演示1: 基本图形绘制
        private void DemoBasicShapes(CanvasRenderingContext2D ctx)
        {
            // 填充矩形
            ctx.FillStyle = new SolidColorStyle(Color.FromArgb(255, 100, 150));
            ctx.FillRect(20, 20, 100, 80);

            // 描边矩形
            ctx.StrokeStyle = new SolidColorStyle(Color.Blue);
            ctx.LineWidth = 3f;
            ctx.StrokeRect(140, 20, 100, 80);

            // 填充圆形
            ctx.FillStyle = new SolidColorStyle(Color.Green);
            ctx.FillCircle(320, 60, 40);

            // 描边椭圆
            ctx.StrokeStyle = new SolidColorStyle(Color.Purple);
            ctx.LineWidth = 2f;
            ctx.StrokeEllipse(400, 40, 60, 40);

            // 带透明度的矩形
            ctx.GlobalAlpha = 0.5f;
            ctx.FillStyle = new SolidColorStyle(Color.Orange);
            ctx.FillRect(480, 20, 100, 80);
            ctx.GlobalAlpha = 1f;
        }

        // 演示2: 路径绘制
        private void DemoPathDrawing(CanvasRenderingContext2D ctx)
        {
            // 绘制三角形
            ctx.BeginPath();
            ctx.MoveTo(20, 150);
            ctx.LineTo(70, 120);
            ctx.LineTo(120, 150);
            ctx.ClosePath();
            ctx.FillStyle = new SolidColorStyle(Color.Cyan);
            ctx.Fill();

            // 绘制星形
            ctx.BeginPath();
            float centerX = 200;
            float centerY = 135;
            float radius = 30;
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)(i * 4 * Math.PI / 5 - Math.PI / 2);
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = centerY + radius * (float)Math.Sin(angle);
                if (i == 0)
                    ctx.MoveTo(x, y);
                else
                    ctx.LineTo(x, y);
            }
            ctx.ClosePath();
            ctx.StrokeStyle = new SolidColorStyle(Color.Red);
            ctx.LineWidth = 2f;
            ctx.Stroke();

            // 绘制圆弧
            ctx.BeginPath();
            ctx.Arc(320, 135, 30, 0, (float)(Math.PI * 1.5), false);
            ctx.LineTo(320, 135);
            ctx.ClosePath();
            ctx.FillStyle = new SolidColorStyle(Color.Yellow);
            ctx.Fill();

            // 绘制贝塞尔曲线
            ctx.BeginPath();
            ctx.MoveTo(400, 120);
            ctx.BezierCurveTo(420, 100, 480, 100, 500, 120);
            ctx.StrokeStyle = new SolidColorStyle(Color.Magenta);
            ctx.LineWidth = 3f;
            ctx.Stroke();
        }

        // 演示3: 文本绘制
        private void DemoTextDrawing(CanvasRenderingContext2D ctx)
        {
            ctx.Font = "16px Arial";
            ctx.FillStyle = new SolidColorStyle(Color.Black);

            // 左对齐文本
            ctx.TextAlign = TextAlign.Left;
            ctx.TextBaseline = TextBaseline.Top;
            ctx.FillText("左对齐文本", 20, 200);

            // 居中对齐文本
            ctx.TextAlign = TextAlign.Center;
            ctx.FillText("居中文本", 200, 200);

            // 右对齐文本
            ctx.TextAlign = TextAlign.Right;
            ctx.FillText("右对齐文本", 380, 200);

            // 不同字体大小
            ctx.Font = "24px Arial";
            ctx.TextAlign = TextAlign.Left;
            ctx.FillStyle = new SolidColorStyle(Color.DarkBlue);
            ctx.FillText("大字体文本", 20, 230);

            // 描边文本
            ctx.Font = "20px Arial";
            ctx.StrokeStyle = new SolidColorStyle(Color.DarkGreen);
            ctx.LineWidth = 1.5f;
            ctx.StrokeText("描边文本", 20, 270);
        }

        // 演示4: 渐变效果
        private void DemoGradient(CanvasRenderingContext2D ctx)
        {
            // 创建线性渐变
            var gradient = ctx.CreateLinearGradient(500, 200, 700, 300);
            gradient.AddColorStop(0f, Color.Red);
            gradient.AddColorStop(0.5f, Color.Yellow);
            gradient.AddColorStop(1f, Color.Green);

            ctx.FillStyle = gradient;
            ctx.FillRect(500, 200, 200, 100);

            // 渐变圆形
            var gradient2 = ctx.CreateLinearGradient(750, 120, 750, 180);
            gradient2.AddColorStop(0f, Color.Blue);
            gradient2.AddColorStop(1f, Color.Cyan);
            ctx.FillStyle = gradient2;
            ctx.FillCircle(750, 150, 30);
        }

        // 演示5: 可拖拽对象
        private void DemoDraggableObject()
        {
            _draggableRect = new RectangleObject
            {
                X = 50,
                Y = 350,
                Width = 100,
                Height = 80,
                FillColor = Color.FromArgb(200, 100, 200, 255),
                StrokeColor = Color.DarkBlue,
                StrokeWidth = 2f,
                IsFilled = true,
                IsStroked = true,
                Draggable = true,
                ZIndex = 10
            };

            canvas1.AddObject(_draggableRect);

            // 添加拖拽事件处理
            _draggableRect.DragStart += (s, e) =>
            {
                _draggableRect.FillColor = Color.FromArgb(255, 150, 200, 255);
            };

            _draggableRect.DragEnd += (s, e) =>
            {
                _draggableRect.FillColor = Color.FromArgb(200, 100, 200, 255);
            };
        }

        // 演示6: 鼠标事件
        private void DemoMouseEvents()
        {
            var interactiveRect = new RectangleObject
            {
                X = 200,
                Y = 350,
                Width = 120,
                Height = 80,
                FillColor = Color.LightGreen,
                StrokeColor = Color.DarkGreen,
                StrokeWidth = 2f,
                IsFilled = true,
                IsStroked = true,
                ZIndex = 5
            };

            canvas1.AddObject(interactiveRect);

            // 鼠标悬停效果
            Color originalColor = interactiveRect.FillColor;
            interactiveRect.MouseEnter += (s, e) =>
            {
                interactiveRect.FillColor = Color.Green;
                canvas1.Invalidate();
            };

            interactiveRect.MouseLeave += (s, e) =>
            {
                interactiveRect.FillColor = originalColor;
                canvas1.Invalidate();
            };

            // 点击事件
            interactiveRect.Click += (s, e) =>
            {
                MessageBox.Show("矩形被点击了！", "事件演示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        // 演示7: 动画效果
        private void DemoAnimation()
        {
            _animatedCircle = new EllipseObject
            {
                X = 400,
                Y = 350,
                RadiusX = 30,
                RadiusY = 30,
                FillColor = Color.Orange,
                StrokeColor = Color.Red,
                StrokeWidth = 2f,
                IsFilled = true,
                IsStroked = true,
                ZIndex = 8
            };

            canvas1.AddObject(_animatedCircle);

            // 启动动画
            canvas1.StartAnimation((deltaTime) =>
            {
                // 圆形旋转移动
                _animationAngle += (float)(deltaTime * 0.001);
                float radius = 50f;
                _animatedCircle.X = 450 + radius * (float)Math.Cos(_animationAngle);
                _animatedCircle.Y = 390 + radius * (float)Math.Sin(_animationAngle);

                // 颜色变化
                int r = (int)(128 + 127 * Math.Sin(_animationAngle));
                int g = (int)(128 + 127 * Math.Sin(_animationAngle + Math.PI * 2 / 3));
                int b = (int)(128 + 127 * Math.Sin(_animationAngle + Math.PI * 4 / 3));
                _animatedCircle.FillColor = Color.FromArgb(r, g, b);
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 停止动画
            canvas1.StopAnimation();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// 设置完整设计器演示
        /// </summary>
        private void SetupCompleteDesignerDemo()
        {
            // 检查控件是否已创建
            if (flowDesignerControl1 == null) return;

            // 添加演示数据
            CreateCompleteDesignerDemoData();

            // 监听Tab切换事件，当切换到完整设计器时显示提示
            tabControl1.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl1.SelectedTab == tabPageCompleteDesigner)
                {
                    ToastNotification.Show("欢迎使用完整流程设计器!\n\n功能:\n- 左侧工具箱可拖拽节点\n- 右侧显示属性面板\n- 工具栏提供常用操作", ToastType.Info, this);
                }
            };
        }

        /// <summary>
        /// 创建完整设计器演示数据
        /// </summary>
        private void CreateCompleteDesignerDemoData()
        {
            if (flowDesignerControl1?.Canvas == null) return;

            // 创建开始节点
            var startNode = new StartNode(new FlowNodeData
            {
                Name = "start",
                DisplayName = "开始",
                Type = FlowNodeType.Start,
                Position = new PointF(100, 200),
                Status = NodeStatus.Success
            });
            flowDesignerControl1.AddNode(startNode);

            // 创建处理节点
            var processNode = new ProcessNode(new FlowNodeData
            {
                Name = "process1",
                DisplayName = "数据处理",
                Type = FlowNodeType.Process,
                Position = new PointF(350, 200),
                Description = "执行数据处理操作"
            });
            flowDesignerControl1.AddNode(processNode);

            // 创建判断节点
            var decisionNode = new DecisionNode(new FlowNodeData
            {
                Name = "decision1",
                DisplayName = "条件判断",
                Type = FlowNodeType.Decision,
                Position = new PointF(600, 200),
                Description = "判断处理结果"
            });
            flowDesignerControl1.AddNode(decisionNode);

            // 创建结束节点
            var endNode = new EndNode(new FlowNodeData
            {
                Name = "end",
                DisplayName = "结束",
                Type = FlowNodeType.End,
                Position = new PointF(850, 200)
            });
            flowDesignerControl1.AddNode(endNode);

            // 创建连接
            flowDesignerControl1.CreateConnection(startNode, processNode);
            flowDesignerControl1.CreateConnection(processNode, decisionNode);
            flowDesignerControl1.CreateConnection(decisionNode, endNode);
        }

        /// <summary>
        /// 演示节点连接功能
        /// </summary>
        public void DemoConnectNodes()
        {
            if (flowDesignerControl1?.Canvas == null) return;

            var nodes = flowDesignerControl1.Nodes;
            if (nodes == null || nodes.Count < 2) return;

            var nodeList = new System.Collections.Generic.List<FlowNode>(nodes);
            if (nodeList.Count >= 2)
            {
                var connection = flowDesignerControl1.CreateConnection(nodeList[0], nodeList[1]);
                if (connection != null)
                {
                    ToastNotification.Show("已创建连接", ToastType.Success, this);
                }
            }
        }

        /// <summary>
        /// 演示删除节点功能
        /// </summary>
        public void DemoDeleteNode()
        {
            if (flowDesignerControl1 == null) return;

            if (flowDesignerControl1.SelectedNode != null)
            {
                flowDesignerControl1.DeleteSelectedNodes();
                ToastNotification.Show("已删除选中节点", ToastType.Success, this);
            }
            else
            {
                ToastNotification.Show("请先选中一个节点", ToastType.Warning, this);
            }
        }

        /// <summary>
        /// 演示取消连接功能
        /// </summary>
        public void DemoDeleteConnection()
        {
            if (flowDesignerControl1 == null) return;

            if (flowDesignerControl1.SelectedConnection != null)
            {
                flowDesignerControl1.DeleteSelectedConnections();
                ToastNotification.Show("已删除选中连接", ToastType.Success, this);
            }
            else
            {
                ToastNotification.Show("请先选中一个连接", ToastType.Warning, this);
            }
        }
    }
}
