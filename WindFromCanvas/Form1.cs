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

        public Form1()
        {
            InitializeComponent();
            SetupMenu();
            SetupDemo();
            SetupFlowDesigner();
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
            
            _menuStrip.Items.AddRange(new[] { fileMenu, viewMenu, toolsMenu, helpMenu });
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);
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

            // 5. 结束节点
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
    }
}
