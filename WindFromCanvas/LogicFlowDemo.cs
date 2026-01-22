using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner;
using WindFromCanvas.Core.Applications.FlowDesigner.Algorithms;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Reactive;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Plugins;
using WindFromCanvas.Core.Applications.FlowDesigner.Rendering;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas
{
    /// <summary>
    /// LogicFlow架构演示窗体
    /// </summary>
    public partial class LogicFlowDemo : Form
    {
        private FlowDesignerCanvas _canvas;
        private PluginManager _pluginManager;
        private ReactiveStore _reactiveStore;
        private LayeredRenderer _layeredRenderer;
        private Panel _toolboxPanel;
        private Panel _propertiesPanel;
        private Panel _controlPanel;
        private TextBox _logTextBox;

        public LogicFlowDemo()
        {
            InitializeComponent();
            InitializeDemo();
        }

        private void InitializeComponent()
        {
            this.Text = "LogicFlow架构演示";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建主分割容器
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 700
            };

            // 创建左右分割容器
            var leftRightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 200
            };

            // 创建右侧分割容器（属性面板和日志）
            var rightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300
            };

            // 创建工具箱面板
            _toolboxPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            InitializeToolbox();

            // 创建画布
            _canvas = new FlowDesignerCanvas
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            // 创建属性面板
            _propertiesPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            InitializePropertiesPanel();

            // 创建日志面板
            _logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen
            };

            // 创建控制面板
            _controlPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 100,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            InitializeControlPanel();

            // 组装界面
            leftRightSplit.Panel1.Controls.Add(_toolboxPanel);
            leftRightSplit.Panel2.Controls.Add(_canvas);

            rightSplit.Panel1.Controls.Add(_propertiesPanel);
            rightSplit.Panel2.Controls.Add(_logTextBox);

            mainSplit.Panel1.Controls.Add(leftRightSplit);
            mainSplit.Panel2.Controls.Add(rightSplit);

            this.Controls.Add(mainSplit);
        }

        private void InitializeDemo()
        {
            // 初始化响应式存储
            _reactiveStore = new ReactiveStore();
            Log("响应式存储已初始化");

            // 初始化插件管理器
            var stateStore = BuilderStateStore.Instance;
            _pluginManager = new PluginManager(stateStore);
            Log("插件管理器已初始化");

            // 加载BPMN插件
            try
            {
                var bpmnPlugin = new WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin.BpmnPlugin();
                _pluginManager.LoadPlugin(bpmnPlugin);
                Log($"插件 '{bpmnPlugin.PluginName}' v{bpmnPlugin.Version} 已加载");
            }
            catch (Exception ex)
            {
                Log($"加载BPMN插件失败: {ex.Message}");
            }

            // 初始化分层渲染器
            _layeredRenderer = new LayeredRenderer(stateStore);
            Log("分层渲染器已初始化");

            // 演示响应式数据流
            DemoReactiveDataFlow();

            // 演示A*路由
            DemoAStarRouting();

            // 演示锚点系统
            DemoAnchorSystem();

            // 演示四叉树优化
            DemoQuadTreeOptimization();

            // 创建示例节点
            CreateDemoNodes();
        }

        private void InitializeToolbox()
        {
            var label = new Label
            {
                Text = "工具箱",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold)
            };
            _toolboxPanel.Controls.Add(label);

            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            // 添加节点类型按钮
            var nodeTypes = new[] { "开始", "处理", "判断", "循环", "结束" };
            foreach (var nodeType in nodeTypes)
            {
                var btn = new Button
                {
                    Text = nodeType,
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(5),
                    FlatStyle = FlatStyle.Flat
                };
                btn.Click += (s, e) => AddNodeFromToolbox(nodeType);
                flowLayout.Controls.Add(btn);
            }

            _toolboxPanel.Controls.Add(flowLayout);
        }

        private void InitializePropertiesPanel()
        {
            var label = new Label
            {
                Text = "属性面板",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold)
            };
            _propertiesPanel.Controls.Add(label);

            var infoLabel = new Label
            {
                Text = "选中节点后显示属性",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };
            _propertiesPanel.Controls.Add(infoLabel);
        }

        private void InitializeControlPanel()
        {
            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            // 响应式数据流演示按钮
            var reactiveBtn = new Button
            {
                Text = "演示响应式数据流",
                Width = 150,
                Height = 35,
                Margin = new Padding(5)
            };
            reactiveBtn.Click += (s, e) => DemoReactiveDataFlow();
            flowLayout.Controls.Add(reactiveBtn);

            // A*路由演示按钮
            var astarBtn = new Button
            {
                Text = "演示A*路由",
                Width = 150,
                Height = 35,
                Margin = new Padding(5)
            };
            astarBtn.Click += (s, e) => DemoAStarRouting();
            flowLayout.Controls.Add(astarBtn);

            // 锚点系统演示按钮
            var anchorBtn = new Button
            {
                Text = "演示锚点系统",
                Width = 150,
                Height = 35,
                Margin = new Padding(5)
            };
            anchorBtn.Click += (s, e) => DemoAnchorSystem();
            flowLayout.Controls.Add(anchorBtn);

            // 四叉树演示按钮
            var quadtreeBtn = new Button
            {
                Text = "演示四叉树优化",
                Width = 150,
                Height = 35,
                Margin = new Padding(5)
            };
            quadtreeBtn.Click += (s, e) => DemoQuadTreeOptimization();
            flowLayout.Controls.Add(quadtreeBtn);

            // 插件系统演示按钮
            var pluginBtn = new Button
            {
                Text = "演示插件系统",
                Width = 150,
                Height = 35,
                Margin = new Padding(5)
            };
            pluginBtn.Click += (s, e) => DemoPluginSystem();
            flowLayout.Controls.Add(pluginBtn);

            // 清除日志按钮
            var clearLogBtn = new Button
            {
                Text = "清除日志",
                Width = 100,
                Height = 35,
                Margin = new Padding(5)
            };
            clearLogBtn.Click += (s, e) => _logTextBox.Clear();
            flowLayout.Controls.Add(clearLogBtn);

            _controlPanel.Controls.Add(flowLayout);
        }

        private void Log(string message)
        {
            if (_logTextBox != null)
            {
                _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                _logTextBox.SelectionStart = _logTextBox.Text.Length;
                _logTextBox.ScrollToCaret();
            }
        }

        private void AddNodeFromToolbox(string nodeType)
        {
            FlowNodeType type = FlowNodeType.Process;
            switch (nodeType)
            {
                case "开始":
                    type = FlowNodeType.Start;
                    break;
                case "处理":
                    type = FlowNodeType.Process;
                    break;
                case "判断":
                    type = FlowNodeType.Decision;
                    break;
                case "循环":
                    type = FlowNodeType.Loop;
                    break;
                case "结束":
                    type = FlowNodeType.End;
                    break;
            }

            var nodeData = new FlowNodeData
            {
                Name = $"node_{Guid.NewGuid():N}",
                DisplayName = nodeType,
                Type = type,
                Position = new PointF(100, 100)
            };

            FlowNode node = null;
            switch (type)
            {
                case FlowNodeType.Start:
                    node = new StartNode(nodeData);
                    break;
                case FlowNodeType.Process:
                    node = new ProcessNode(nodeData);
                    break;
                case FlowNodeType.Decision:
                    node = new DecisionNode(nodeData);
                    break;
                case FlowNodeType.Loop:
                    node = new LoopNode(nodeData);
                    break;
                case FlowNodeType.End:
                    node = new EndNode(nodeData);
                    break;
            }

            if (node != null)
            {
                _canvas.AddNode(node);
                Log($"已添加节点: {nodeType}");
            }
        }

        private void CreateDemoNodes()
        {
            // 创建几个示例节点
            var startNode = new StartNode(new FlowNodeData
            {
                Name = "start",
                DisplayName = "开始",
                Type = FlowNodeType.Start,
                Position = new PointF(100, 200)
            });
            _canvas.AddNode(startNode);

            var processNode = new ProcessNode(new FlowNodeData
            {
                Name = "process1",
                DisplayName = "处理节点",
                Type = FlowNodeType.Process,
                Position = new PointF(300, 200)
            });
            _canvas.AddNode(processNode);

            Log("已创建示例节点");
        }

        private void DemoReactiveDataFlow()
        {
            Log("=== 响应式数据流演示 ===");

            // 创建可观察属性
            var nameProperty = _reactiveStore.Observable<string>("name", "初始值");
            var countProperty = _reactiveStore.Observable<int>("count", 0);

            // 订阅变化
            nameProperty.Subscribe((oldVal, newVal) =>
            {
                Log($"名称变化: {oldVal} -> {newVal}");
            });

            countProperty.Subscribe((oldVal, newVal) =>
            {
                Log($"计数变化: {oldVal} -> {newVal}");
            });

            // 创建计算属性
            var displayText = _reactiveStore.Computed<string>("displayText", () =>
            {
                return $"{nameProperty.Value} (计数: {countProperty.Value})";
            });

            // 修改值
            _reactiveStore.Action("updateName", () =>
            {
                nameProperty.Value = "新名称";
                countProperty.Value = 10;
            });

            Log($"计算属性值: {displayText.Value}");
            Log("响应式数据流演示完成");
        }

        private void DemoAStarRouting()
        {
            Log("=== A*路由算法演示 ===");

            var router = new AStarRouter();
            var start = new PointF(100, 100);
            var end = new PointF(500, 300);

            // 创建障碍物（模拟节点）
            var obstacles = new System.Collections.Generic.List<RectangleF>
            {
                new RectangleF(200, 150, 100, 80),
                new RectangleF(350, 200, 100, 80)
            };

            var path = router.FindPath(start, end, obstacles);
            Log($"找到路径，包含 {path.Count} 个点:");
            foreach (var point in path)
            {
                Log($"  点: ({point.X:F1}, {point.Y:F1})");
            }

            Log("A*路由算法演示完成");
        }

        private void DemoAnchorSystem()
        {
            Log("=== 锚点系统演示 ===");

            // 创建锚点
            var anchor1 = new AnchorPoint
            {
                Id = AnchorPoint.GenerateId("node1", AnchorDirection.Output, 0),
                RelativePosition = new PointF(100, 50),
                Direction = AnchorDirection.Output,
                MaxConnections = 5,
                AllowedTargetTypes = new System.Collections.Generic.List<string> { "Process", "Decision" }
            };

            var anchor2 = new AnchorPoint
            {
                Id = AnchorPoint.GenerateId("node2", AnchorDirection.Input, 0),
                RelativePosition = new PointF(0, 50),
                Direction = AnchorDirection.Input
            };

            Log($"锚点1 ID: {anchor1.Id}");
            Log($"锚点1可以连接到Process: {anchor1.CanConnectTo("Process")}");
            Log($"锚点1可以连接到Loop: {anchor1.CanConnectTo("Loop")}");
            Log($"锚点1可以接受更多连接: {anchor1.CanAcceptMoreConnections(3)}");

            // 创建连接数据（包含锚点ID）
            var connectionData = FlowConnectionData.Create("node1", "node2");
            connectionData.SourceAnchorId = anchor1.Id;
            connectionData.TargetAnchorId = anchor2.Id;

            Log($"连接数据 - 源锚点: {connectionData.SourceAnchorId}");
            Log($"连接数据 - 目标锚点: {connectionData.TargetAnchorId}");
            Log("锚点系统演示完成");
        }

        private void DemoQuadTreeOptimization()
        {
            Log("=== 四叉树空间索引演示 ===");

            var bounds = new RectangleF(0, 0, 1000, 1000);
            var quadTree = new QuadTree<TestBoundable>(bounds);

            // 插入测试对象
            var random = new Random();
            for (int i = 0; i < 100; i++)
            {
                var item = new TestBoundable
                {
                    Bounds = new RectangleF(
                        random.Next(0, 900),
                        random.Next(0, 900),
                        50,
                        50
                    )
                };
                quadTree.Insert(item);
            }

            Log($"四叉树包含 {quadTree.Count} 个项目");

            // 查询区域
            var queryArea = new RectangleF(200, 200, 300, 300);
            var results = quadTree.Query(queryArea);
            Log($"查询区域 ({queryArea}) 找到 {results.Count} 个项目");

            Log("四叉树空间索引演示完成");
        }

        private void DemoPluginSystem()
        {
            Log("=== 插件系统演示 ===");

            var plugins = _pluginManager.GetAllPlugins().ToList();
            Log($"已加载 {plugins.Count} 个插件:");

            foreach (var plugin in plugins)
            {
                Log($"  - {plugin.PluginName} v{plugin.Version}");
            }

            var nodeTypes = _pluginManager.Context.GetRegisteredNodeTypes();
            Log($"已注册 {nodeTypes.Count} 个节点类型");

            var edgeTypes = _pluginManager.Context.GetRegisteredEdgeTypes();
            Log($"已注册 {edgeTypes.Count} 个连线类型");

            Log("插件系统演示完成");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _layeredRenderer?.Dispose();
            base.OnFormClosed(e);
        }
    }

    /// <summary>
    /// 测试用的可边界化对象
    /// </summary>
    internal class TestBoundable : IBoundable
    {
        public RectangleF Bounds { get; set; }
    }
}
