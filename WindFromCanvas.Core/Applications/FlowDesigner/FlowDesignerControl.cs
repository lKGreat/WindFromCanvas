using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Connections;
using WindFromCanvas.Core.Applications.FlowDesigner.Widgets;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;
using WindFromCanvas.Core.Applications.FlowDesigner.Commands;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;
using WindFromCanvas.Core.Applications.FlowDesigner.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner
{
    /// <summary>
    /// 完整的流程设计器用户控件
    /// 整合工具箱、画布、属性面板等组件，提供BPMN 2.0和LogicFlow的全部能力
    /// </summary>
    public class FlowDesignerControl : UserControl
    {
        #region 核心组件

        /// <summary>
        /// 流程画布
        /// </summary>
        private FlowDesignerCanvas _canvas;

        /// <summary>
        /// 节点工具箱
        /// </summary>
        private ToolboxPanel _toolbox;

        /// <summary>
        /// 节点属性面板
        /// </summary>
        private NodePropertiesPanel _propertiesPanel;

        /// <summary>
        /// 小地图控件
        /// </summary>
        private MinimapControl _minimap;

        /// <summary>
        /// 画布控制面板
        /// </summary>
        private CanvasControlPanel _canvasControlPanel;

        #endregion

        #region 布局控件

        /// <summary>
        /// 主分割容器（左侧工具箱 | 中央+右侧）
        /// </summary>
        private SplitContainer _mainSplitter;

        /// <summary>
        /// 右侧分割容器（中央画布 | 右侧属性面板）
        /// </summary>
        private SplitContainer _rightSplitter;

        /// <summary>
        /// 左侧面板容器
        /// </summary>
        private Panel _leftPanel;

        /// <summary>
        /// 左侧面板分割器（工具箱 | 小地图）
        /// </summary>
        private SplitContainer _leftSplitter;

        /// <summary>
        /// 顶部工具栏
        /// </summary>
        private ToolStrip _toolbar;

        /// <summary>
        /// 底部状态栏
        /// </summary>
        private StatusStrip _statusBar;

        /// <summary>
        /// 状态栏标签 - 缩放比例
        /// </summary>
        private ToolStripStatusLabel _zoomLabel;

        /// <summary>
        /// 状态栏标签 - 节点数量
        /// </summary>
        private ToolStripStatusLabel _nodeCountLabel;

        /// <summary>
        /// 状态栏标签 - 选中状态
        /// </summary>
        private ToolStripStatusLabel _selectionLabel;

        /// <summary>
        /// 左侧面板折叠按钮
        /// </summary>
        private Button _leftCollapseButton;

        /// <summary>
        /// 右侧面板折叠按钮
        /// </summary>
        private Button _rightCollapseButton;

        #endregion

        #region 状态字段

        /// <summary>
        /// 左侧面板是否折叠
        /// </summary>
        private bool _isLeftPanelCollapsed;

        /// <summary>
        /// 右侧面板是否折叠
        /// </summary>
        private bool _isRightPanelCollapsed;

        /// <summary>
        /// 左侧面板展开时的宽度
        /// </summary>
        private int _leftPanelExpandedWidth = 240;

        /// <summary>
        /// 右侧面板展开时的宽度
        /// </summary>
        private int _rightPanelExpandedWidth = 280;

        /// <summary>
        /// 快捷键管理器
        /// </summary>
        private ShortcutManager _shortcutManager;

        /// <summary>
        /// 命令管理器
        /// </summary>
        private CommandManager _commandManager;

        /// <summary>
        /// 序列化器
        /// </summary>
        private FlowSerializer _serializer;

        /// <summary>
        /// 当前选中的节点
        /// </summary>
        private FlowNode _selectedNode;

        /// <summary>
        /// 当前选中的连接
        /// </summary>
        private FlowConnection _selectedConnection;

        #endregion

        #region 事件

        /// <summary>
        /// 节点选择变化事件
        /// </summary>
        public event EventHandler<FlowNode> NodeSelectionChanged;

        /// <summary>
        /// 连接创建事件
        /// </summary>
        public event EventHandler<FlowConnection> ConnectionCreated;

        /// <summary>
        /// 节点删除事件
        /// </summary>
        public event EventHandler<FlowNode> NodeDeleted;

        /// <summary>
        /// 连接删除事件
        /// </summary>
        public event EventHandler<FlowConnection> ConnectionDeleted;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取画布控件
        /// </summary>
        public FlowDesignerCanvas Canvas => _canvas;

        /// <summary>
        /// 获取工具箱面板
        /// </summary>
        public ToolboxPanel Toolbox => _toolbox;

        /// <summary>
        /// 获取属性面板
        /// </summary>
        public NodePropertiesPanel PropertiesPanel => _propertiesPanel;

        /// <summary>
        /// 获取当前缩放比例
        /// </summary>
        public float ZoomFactor => _canvas?.ZoomFactor ?? 1.0f;

        /// <summary>
        /// 设置缩放比例
        /// </summary>
        public void SetZoom(float factor)
        {
            _canvas?.SetZoom(factor);
            UpdateStatusBar();
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        public IReadOnlyCollection<FlowNode> Nodes => _canvas?.GetNodes();

        /// <summary>
        /// 获取选中的节点
        /// </summary>
        public FlowNode SelectedNode => _selectedNode;

        /// <summary>
        /// 获取选中的连接
        /// </summary>
        public FlowConnection SelectedConnection => _selectedConnection;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建流程设计器控件
        /// </summary>
        public FlowDesignerControl()
        {
            InitializeComponent();
            _serializer = new FlowSerializer();
            IntegrateComponents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 设置控件基本属性
            this.Name = "FlowDesignerControl";
            this.Size = new Size(1200, 800);
            this.BackColor = Color.White;

            // 设置双缓冲
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);

            // 创建主分割容器（左侧工具箱 | 中央+右侧）
            _mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = _leftPanelExpandedWidth,
                SplitterWidth = 4,
                FixedPanel = FixedPanel.Panel1,
                BorderStyle = BorderStyle.None
            };

            // 创建右侧分割容器（中央画布 | 右侧属性面板）
            _rightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 4,
                FixedPanel = FixedPanel.Panel2,
                BorderStyle = BorderStyle.None
            };

            // 创建左侧面板容器
            _leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Instance.CurrentTheme.Background,
                Padding = new Padding(0)
            };

            // 创建左侧内部分割器（工具箱 | 小地图）
            _leftSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 500,
                SplitterWidth = 4,
                FixedPanel = FixedPanel.Panel2,
                BorderStyle = BorderStyle.None,
                Panel2MinSize = 120
            };

            // 组装布局
            _leftPanel.Controls.Add(_leftSplitter);
            _mainSplitter.Panel1.Controls.Add(_leftPanel);
            _mainSplitter.Panel2.Controls.Add(_rightSplitter);

            // 创建顶部工具栏
            CreateToolbar();

            // 创建底部状态栏
            CreateStatusBar();

            // 按正确顺序添加控件（Dock顺序重要）
            this.Controls.Add(_mainSplitter);
            this.Controls.Add(_statusBar);
            this.Controls.Add(_toolbar);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 创建顶部工具栏
        /// </summary>
        private void CreateToolbar()
        {
            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.System,
                AutoSize = true,
                Padding = new Padding(4, 2, 4, 2)
            };

            // 新建按钮
            var newButton = new ToolStripButton
            {
                Text = "新建",
                ToolTipText = "新建流程 (Ctrl+N)",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            newButton.Click += (s, e) => Clear();
            _toolbar.Items.Add(newButton);

            // 打开按钮
            var openButton = new ToolStripButton
            {
                Text = "打开",
                ToolTipText = "打开流程文件 (Ctrl+O)",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            openButton.Click += (s, e) => OnOpenClick();
            _toolbar.Items.Add(openButton);

            // 保存按钮
            var saveButton = new ToolStripButton
            {
                Text = "保存",
                ToolTipText = "保存流程文件 (Ctrl+S)",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            saveButton.Click += (s, e) => OnSaveClick();
            _toolbar.Items.Add(saveButton);

            _toolbar.Items.Add(new ToolStripSeparator());

            // 撤销按钮
            var undoButton = new ToolStripButton
            {
                Text = "撤销",
                ToolTipText = "撤销 (Ctrl+Z)",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            undoButton.Click += (s, e) => Undo();
            _toolbar.Items.Add(undoButton);

            // 重做按钮
            var redoButton = new ToolStripButton
            {
                Text = "重做",
                ToolTipText = "重做 (Ctrl+Y)",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            redoButton.Click += (s, e) => Redo();
            _toolbar.Items.Add(redoButton);

            _toolbar.Items.Add(new ToolStripSeparator());

            // 放大按钮
            var zoomInButton = new ToolStripButton
            {
                Text = "+",
                ToolTipText = "放大",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            zoomInButton.Click += (s, e) => ZoomIn();
            _toolbar.Items.Add(zoomInButton);

            // 缩放比例标签
            var zoomLabelItem = new ToolStripLabel
            {
                Text = "100%",
                AutoSize = true
            };
            _toolbar.Items.Add(zoomLabelItem);

            // 缩小按钮
            var zoomOutButton = new ToolStripButton
            {
                Text = "-",
                ToolTipText = "缩小",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            zoomOutButton.Click += (s, e) => ZoomOut();
            _toolbar.Items.Add(zoomOutButton);

            // 适应视图按钮
            var fitViewButton = new ToolStripButton
            {
                Text = "适应",
                ToolTipText = "适应视图",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            fitViewButton.Click += (s, e) => FitToView();
            _toolbar.Items.Add(fitViewButton);

            _toolbar.Items.Add(new ToolStripSeparator());

            // 删除按钮
            var deleteButton = new ToolStripButton
            {
                Text = "删除",
                ToolTipText = "删除选中项 (Delete)",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            deleteButton.Click += (s, e) => DeleteSelectedNodes();
            _toolbar.Items.Add(deleteButton);

            _toolbar.Items.Add(new ToolStripSeparator());

            // 主题切换
            var themeDropDown = new ToolStripDropDownButton
            {
                Text = "主题",
                ToolTipText = "切换主题"
            };
            themeDropDown.DropDownItems.Add("浅色主题", null, (s, e) => SwitchTheme(true));
            themeDropDown.DropDownItems.Add("深色主题", null, (s, e) => SwitchTheme(false));
            _toolbar.Items.Add(themeDropDown);
        }

        /// <summary>
        /// 创建底部状态栏
        /// </summary>
        private void CreateStatusBar()
        {
            _statusBar = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                SizingGrip = true
            };

            // 缩放比例标签
            _zoomLabel = new ToolStripStatusLabel
            {
                Text = "缩放: 100%",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 100
            };
            _statusBar.Items.Add(_zoomLabel);

            // 节点数量标签
            _nodeCountLabel = new ToolStripStatusLabel
            {
                Text = "节点: 0",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 100
            };
            _statusBar.Items.Add(_nodeCountLabel);

            // 选中状态标签
            _selectionLabel = new ToolStripStatusLabel
            {
                Text = "未选中",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statusBar.Items.Add(_selectionLabel);

            // 提示信息
            var tipLabel = new ToolStripStatusLabel
            {
                Text = "提示: 从左侧工具箱拖拽节点到画布",
                BorderSides = ToolStripStatusLabelBorderSides.Left
            };
            _statusBar.Items.Add(tipLabel);
        }

        /// <summary>
        /// 打开文件点击事件
        /// </summary>
        private void OnOpenClick()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "流程文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Title = "打开流程文件";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFromFile(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// 保存文件点击事件
        /// </summary>
        private void OnSaveClick()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "流程文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Title = "保存流程文件";
                dialog.DefaultExt = "json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SaveToFile(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// 创建并集成所有子组件
        /// </summary>
        private void IntegrateComponents()
        {
            // 2.1 集成ToolboxPanel到左侧面板上部
            IntegrateToolbox();

            // 2.3 集成MinimapControl到左侧面板下部
            IntegrateMinimap();

            // 3.1 集成FlowDesignerCanvas到中央
            IntegrateCanvas();

            // 4.1 集成NodePropertiesPanel到右侧
            IntegratePropertiesPanel();

            // 2.2 & 4.3 添加折叠按钮
            AddCollapseButtons();

            // 连接事件
            ConnectEvents();
        }

        /// <summary>
        /// 2.1 集成ToolboxPanel到左侧面板
        /// </summary>
        private void IntegrateToolbox()
        {
            _toolbox = new ToolboxPanel
            {
                Dock = DockStyle.Fill
            };
            _leftSplitter.Panel1.Controls.Add(_toolbox);
        }

        /// <summary>
        /// 2.3 集成MinimapControl到左侧底部
        /// </summary>
        private void IntegrateMinimap()
        {
            // 小地图面板标题
            var minimapHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = ThemeManager.Instance.CurrentTheme.NodeBackground
            };

            var minimapLabel = new Label
            {
                Text = "小地图",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeManager.Instance.CurrentTheme.TextPrimary
            };
            minimapHeaderPanel.Controls.Add(minimapLabel);

            var minimapContainer = new Panel
            {
                Dock = DockStyle.Fill
            };

            _leftSplitter.Panel2.Controls.Add(minimapContainer);
            _leftSplitter.Panel2.Controls.Add(minimapHeaderPanel);

            // 延迟创建小地图（需要画布实例）
        }

        /// <summary>
        /// 3.1 集成FlowDesignerCanvas到中央
        /// </summary>
        private void IntegrateCanvas()
        {
            _canvas = new FlowDesignerCanvas
            {
                Dock = DockStyle.Fill
            };

            // 设置工具箱和属性面板的引用
            _canvas.Toolbox = _toolbox;

            // 获取命令管理器
            _commandManager = _canvas.CommandManager;

            _rightSplitter.Panel1.Controls.Add(_canvas);
        }

        /// <summary>
        /// 4.1 集成NodePropertiesPanel到右侧
        /// </summary>
        private void IntegratePropertiesPanel()
        {
            _propertiesPanel = new NodePropertiesPanel
            {
                Dock = DockStyle.Fill
            };

            // 设置画布的属性面板引用
            _canvas.PropertiesPanel = _propertiesPanel;

            _rightSplitter.Panel2.Controls.Add(_propertiesPanel);
        }

        /// <summary>
        /// 2.2 & 4.3 添加折叠按钮
        /// </summary>
        private void AddCollapseButtons()
        {
            // 左侧折叠按钮
            _leftCollapseButton = new Button
            {
                Text = "◀",
                Size = new Size(20, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = ThemeManager.Instance.CurrentTheme.NodeBackground
            };
            _leftCollapseButton.FlatAppearance.BorderSize = 0;
            _leftCollapseButton.Click += LeftCollapseButton_Click;

            // 右侧折叠按钮
            _rightCollapseButton = new Button
            {
                Text = "▶",
                Size = new Size(20, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = ThemeManager.Instance.CurrentTheme.NodeBackground
            };
            _rightCollapseButton.FlatAppearance.BorderSize = 0;
            _rightCollapseButton.Click += RightCollapseButton_Click;

            // 将折叠按钮添加到分割面板边缘
            _mainSplitter.Panel1.Controls.Add(_leftCollapseButton);
            _leftCollapseButton.BringToFront();
            _leftCollapseButton.Location = new Point(_mainSplitter.Panel1.Width - 20, (_mainSplitter.Panel1.Height - 60) / 2);
            _leftCollapseButton.Anchor = AnchorStyles.Right;

            _rightSplitter.Panel2.Controls.Add(_rightCollapseButton);
            _rightCollapseButton.BringToFront();
            _rightCollapseButton.Location = new Point(0, (_rightSplitter.Panel2.Height - 60) / 2);
            _rightCollapseButton.Anchor = AnchorStyles.Left;
        }

        /// <summary>
        /// 左侧折叠按钮点击
        /// </summary>
        private void LeftCollapseButton_Click(object sender, EventArgs e)
        {
            _isLeftPanelCollapsed = !_isLeftPanelCollapsed;
            if (_isLeftPanelCollapsed)
            {
                _mainSplitter.SplitterDistance = 24;
                _leftCollapseButton.Text = "▶";
            }
            else
            {
                _mainSplitter.SplitterDistance = _leftPanelExpandedWidth;
                _leftCollapseButton.Text = "◀";
            }
        }

        /// <summary>
        /// 右侧折叠按钮点击
        /// </summary>
        private void RightCollapseButton_Click(object sender, EventArgs e)
        {
            _isRightPanelCollapsed = !_isRightPanelCollapsed;
            if (_isRightPanelCollapsed)
            {
                _rightSplitter.SplitterDistance = _rightSplitter.Width - 24;
                _rightCollapseButton.Text = "◀";
            }
            else
            {
                _rightSplitter.SplitterDistance = _rightSplitter.Width - _rightPanelExpandedWidth;
                _rightCollapseButton.Text = "▶";
            }
        }

        /// <summary>
        /// 连接所有事件
        /// </summary>
        private void ConnectEvents()
        {
            // 3.2 连接工具箱拖拽事件
            if (_toolbox != null)
            {
                _toolbox.NodeDragStarted += Toolbox_NodeDragStarted;
            }

            // 3.3 & 4.2 连接画布事件
            if (_canvas != null)
            {
                // 监听画布鼠标点击事件来检测节点选择
                _canvas.MouseClick += Canvas_MouseClick;

                // 监听缩放和大小变化
                _canvas.SizeChanged += (s, e) => UpdateStatusBar();

                // 定时器更新状态栏
                var updateTimer = new Timer { Interval = 200 };
                updateTimer.Tick += (s, e) => UpdateStatusBar();
                updateTimer.Start();
            }

            // 监听主题变化
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        }

        /// <summary>
        /// 工具箱节点拖拽开始
        /// </summary>
        private void Toolbox_NodeDragStarted(object sender, ToolboxPanel.NodeTypeItem item)
        {
            // 记录使用
            _toolbox?.RecordUsage(item.Type);
        }

        /// <summary>
        /// 画布鼠标点击事件
        /// </summary>
        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            // 节点选择由画布内部处理，这里更新属性面板
            // 使用延迟更新以确保画布内部选择已完成
            BeginInvoke(new Action(() =>
            {
                // 获取当前选中的节点（从画布的_selectedNodes集合）
                var nodes = _canvas?.GetNodes();
                if (nodes != null)
                {
                    // 查找被点击的节点
                    var clickPoint = e.Location;
                    foreach (var node in nodes)
                    {
                        if (node.GetBounds().Contains(clickPoint))
                        {
                            SetSelectedNodeInternal(node);
                            return;
                        }
                    }
                }
                // 点击空白区域，清除选择
                _selectedNode = null;
                _propertiesPanel?.SetSelectedNode(null);
                UpdateStatusBar();
            }));
        }

        /// <summary>
        /// 主题变化处理
        /// </summary>
        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            _leftPanel.BackColor = e.NewTheme.Background;
            if (_leftCollapseButton != null)
                _leftCollapseButton.BackColor = e.NewTheme.NodeBackground;
            if (_rightCollapseButton != null)
                _rightCollapseButton.BackColor = e.NewTheme.NodeBackground;
        }

        /// <summary>
        /// 设置右侧面板宽度（在加载后调用）
        /// </summary>
        private void SetupRightPanelWidth()
        {
            if (_rightSplitter != null && _rightSplitter.Width > _rightPanelExpandedWidth + 100)
            {
                _rightSplitter.SplitterDistance = _rightSplitter.Width - _rightPanelExpandedWidth;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetupRightPanelWidth();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (_rightSplitter != null && !_isRightPanelCollapsed && _rightSplitter.Width > _rightPanelExpandedWidth + 100)
            {
                _rightSplitter.SplitterDistance = _rightSplitter.Width - _rightPanelExpandedWidth;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加节点到画布
        /// </summary>
        public void AddNode(FlowNode node)
        {
            _canvas?.AddNode(node);
            UpdateStatusBar();
        }

        /// <summary>
        /// 删除选中的节点
        /// </summary>
        public void DeleteSelectedNodes()
        {
            if (_canvas != null && _selectedNode != null)
            {
                var node = _selectedNode;
                _canvas.RemoveNode(node);
                _selectedNode = null;
                NodeDeleted?.Invoke(this, node);
                UpdateStatusBar();
            }
        }

        /// <summary>
        /// 创建两个节点之间的连接
        /// </summary>
        public FlowConnection CreateConnection(FlowNode source, FlowNode target)
        {
            var connection = _canvas?.CreateConnection(source, target);
            if (connection != null)
            {
                ConnectionCreated?.Invoke(this, connection);
            }
            return connection;
        }

        /// <summary>
        /// 删除选中的连接
        /// </summary>
        public void DeleteSelectedConnections()
        {
            if (_canvas != null && _selectedConnection != null)
            {
                var connection = _selectedConnection;
                _canvas.RemoveConnection(connection);
                _selectedConnection = null;
                ConnectionDeleted?.Invoke(this, connection);
            }
        }

        /// <summary>
        /// 删除指定的连接
        /// </summary>
        public void DeleteConnection(FlowConnection connection)
        {
            if (_canvas != null && connection != null)
            {
                _canvas.RemoveConnection(connection);
                if (_selectedConnection == connection)
                {
                    _selectedConnection = null;
                }
                ConnectionDeleted?.Invoke(this, connection);
            }
        }

        /// <summary>
        /// 清空画布
        /// </summary>
        public void Clear()
        {
            _canvas?.Clear();
            _selectedNode = null;
            _selectedConnection = null;
            _propertiesPanel?.SetSelectedNode(null);
            UpdateStatusBar();
        }

        /// <summary>
        /// 适应视图
        /// </summary>
        public void FitToView()
        {
            _canvas?.FitToView();
            UpdateStatusBar();
        }

        /// <summary>
        /// 放大
        /// </summary>
        public void ZoomIn()
        {
            _canvas?.ZoomIn();
            UpdateStatusBar();
        }

        /// <summary>
        /// 缩小
        /// </summary>
        public void ZoomOut()
        {
            _canvas?.ZoomOut();
            UpdateStatusBar();
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        public void SwitchTheme(bool isLight)
        {
            if (isLight)
            {
                ThemeManager.Instance.SetTheme(new LightTheme());
            }
            else
            {
                ThemeManager.Instance.SetTheme(new DarkTheme());
            }
            _canvas?.Invalidate();
            _toolbox?.Invalidate();
            _propertiesPanel?.Invalidate();
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            _canvas?.SaveToFile(filePath);
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            _canvas?.LoadFromFile(filePath);
            UpdateStatusBar();
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public void Undo()
        {
            _commandManager?.Undo();
            _canvas?.Invalidate();
            UpdateStatusBar();
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        public void Redo()
        {
            _commandManager?.Redo();
            _canvas?.Invalidate();
            UpdateStatusBar();
        }

        /// <summary>
        /// 选择节点
        /// </summary>
        public void SelectNode(FlowNode node)
        {
            _selectedNode = node;
            _selectedConnection = null;
            _canvas?.SelectNode(node);
            _propertiesPanel?.SetSelectedNode(node);
            NodeSelectionChanged?.Invoke(this, node);
            UpdateStatusBar();
        }

        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            _selectedNode = null;
            _selectedConnection = null;
            _canvas?.ClearSelection();
            _propertiesPanel?.SetSelectedNode(null);
            UpdateStatusBar();
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 更新状态栏
        /// </summary>
        private void UpdateStatusBar()
        {
            if (_zoomLabel != null)
            {
                _zoomLabel.Text = string.Format("缩放: {0:P0}", _canvas?.ZoomFactor ?? 1.0f);
            }

            if (_nodeCountLabel != null)
            {
                var nodeCount = _canvas?.GetNodes()?.Count ?? 0;
                _nodeCountLabel.Text = string.Format("节点: {0}", nodeCount);
            }

            if (_selectionLabel != null)
            {
                _selectionLabel.Text = _selectedNode != null
                    ? string.Format("选中: {0}", _selectedNode.Data?.DisplayName ?? _selectedNode.Data?.Name ?? "未命名")
                    : "未选中";
            }
        }

        /// <summary>
        /// 内部设置选中节点（不触发画布选择）
        /// </summary>
        internal void SetSelectedNodeInternal(FlowNode node)
        {
            _selectedNode = node;
            _propertiesPanel?.SetSelectedNode(node);
            NodeSelectionChanged?.Invoke(this, node);
            UpdateStatusBar();
        }

        /// <summary>
        /// 内部设置选中连接
        /// </summary>
        internal void SetSelectedConnectionInternal(FlowConnection connection)
        {
            _selectedConnection = connection;
            UpdateStatusBar();
        }

        #endregion
    }
}
