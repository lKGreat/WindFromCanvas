using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core;
using WindFromCanvasCoreCanvas = WindFromCanvas.Core.Canvas;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Connections;
using WindFromCanvas.Core.Applications.FlowDesigner.Commands;
using WindFromCanvas.Core.Applications.FlowDesigner.Clipboard;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;
using WindFromCanvas.Core.Applications.FlowDesigner.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.Validation;
using WindFromCanvas.Core.Applications.FlowDesigner.Animation;
using WindFromCanvas.Core.Applications.FlowDesigner.Rendering;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges;
using WindFromCanvas.Core.Applications.FlowDesigner.Interaction;
using WindFromCanvas.Core.Events;

namespace WindFromCanvas.Core.Applications.FlowDesigner
{
    /// <summary>
    /// 流程设计器画布主控件
    /// </summary>
    public class FlowDesignerCanvas : WindFromCanvasCoreCanvas
    {
        /// <summary>
        /// 流程文档
        /// </summary>
        public FlowDocument Document { get; set; }

        /// <summary>
        /// 坐标变换模型（管理缩放和平移）
        /// </summary>
        public TransformModel Transform { get; private set; }

        /// <summary>
        /// 分层渲染器（管理6层渲染）
        /// </summary>
        private LayeredRenderer _layeredRenderer;

        /// <summary>
        /// 图构建器（从FlowVersion构建FlowGraph）
        /// </summary>
        private FlowGraphBuilder _graphBuilder;

        /// <summary>
        /// 当前的FlowGraph（用于渲染）
        /// </summary>
        private FlowGraph _currentGraph;

        /// <summary>
        /// 选择管理器（处理单选、框选、多选）
        /// </summary>
        private SelectionManager _selectionManager;

        /// <summary>
        /// 快捷键管理器（处理键盘快捷键）
        /// </summary>
        private Interaction.ShortcutManager _shortcutManager;

        /// <summary>
        /// Invalidate防抖定时器（优化重绘频率）
        /// </summary>
        private System.Windows.Forms.Timer _invalidateDebounceTimer;

        /// <summary>
        /// 是否有待处理的Invalidate请求
        /// </summary>
        private bool _hasPendingInvalidate = false;

        /// <summary>
        /// 所有节点
        /// </summary>
        private Dictionary<string, FlowNode> _nodes = new Dictionary<string, FlowNode>();

        /// <summary>
        /// 获取所有节点（用于小地图等）
        /// </summary>
        public IReadOnlyCollection<FlowNode> GetNodes()
        {
            return _nodes.Values;
        }

        /// <summary>
        /// 所有连接
        /// </summary>
        private Dictionary<string, FlowConnection> _connections = new Dictionary<string, FlowConnection>();

        /// <summary>
        /// 选中的节点
        /// </summary>
        private HashSet<FlowNode> _selectedNodes = new HashSet<FlowNode>();

        /// <summary>
        /// 当前拖拽的节点
        /// </summary>
        private FlowNode _draggingNode;

        /// <summary>
        /// 拖拽预览位置
        /// </summary>
        private PointF _dragPreviewPosition = PointF.Empty;

        /// <summary>
        /// 多选拖拽的节点列表
        /// </summary>
        private List<FlowNode> _draggingNodes = new List<FlowNode>();

        /// <summary>
        /// 是否正在框选
        /// </summary>
        private bool _isSelecting;

        /// <summary>
        /// 框选起始点
        /// </summary>
        private PointF _selectionStart;

        /// <summary>
        /// 框选结束点
        /// </summary>
        private PointF _selectionEnd;

        /// <summary>
        /// 是否正在创建连接
        /// </summary>
        private bool _isCreatingConnection;

        /// <summary>
        /// 连接起始节点
        /// </summary>
        private FlowNode _connectionSourceNode;

        /// <summary>
        /// 连接预览终点
        /// </summary>
        private PointF _connectionPreviewEnd;

        /// <summary>
        /// 候选目标节点（正在连接时鼠标悬停的节点）
        /// </summary>
        private FlowNode _potentialTargetNode;

        /// <summary>
        /// 连接预览动画偏移（用于虚线流动效果）
        /// </summary>
        private float _connectionPreviewAnimationOffset = 0f;

        /// <summary>
        /// 当前右键点击的连接线
        /// </summary>
        private FlowConnection _rightClickedConnection;

        /// <summary>
        /// 是否显示对齐辅助线
        /// </summary>
        public bool ShowAlignmentGuides { get; set; } = true;

        /// <summary>
        /// 对齐辅助线
        /// </summary>
        private List<RectangleF> _alignmentGuides = new List<RectangleF>();

        /// <summary>
        /// 验证结果
        /// </summary>
        public ValidationResult LastValidationResult { get; private set; }

        /// <summary>
        /// 是否启用视口裁剪（只渲染可见区域）
        /// </summary>
        public bool EnableViewportCulling { get; set; } = true;

        /// <summary>
        /// 是否启用脏区域刷新
        /// </summary>
        public bool EnableDirtyRegionRefresh { get; set; } = true;

        /// <summary>
        /// 脏区域列表
        /// </summary>
        private List<RectangleF> _dirtyRegions = new List<RectangleF>();

        /// <summary>
        /// 工具箱面板
        /// </summary>
        public Widgets.ToolboxPanel Toolbox { get; set; }

        /// <summary>
        /// 属性面板
        /// </summary>
        public Widgets.NodePropertiesPanel PropertiesPanel { get; set; }

        /// <summary>
        /// 上下文菜单
        /// </summary>
        private Widgets.FlowContextMenu _contextMenu;

        /// <summary>
        /// 命令管理器
        /// </summary>
        public CommandManager CommandManager { get; private set; }

        /// <summary>
        /// 画布缩放比例
        /// </summary>
        public float ZoomFactor { get; private set; } = 1.0f;

        /// <summary>
        /// 画布偏移量
        /// </summary>
        public PointF PanOffset { get; set; } = PointF.Empty;

        /// <summary>
        /// 是否显示网格
        /// </summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>
        /// 网格大小
        /// </summary>
        public float GridSize { get; set; } = 20f;

        /// <summary>
        /// 是否正在平移
        /// </summary>
        private bool _isPanning;

        /// <summary>
        /// 平移起始点
        /// </summary>
        private PointF _panStartPoint;

        public FlowDesignerCanvas()
        {
            Document = new FlowDocument();
            BackgroundColor = Color.FromArgb(250, 250, 250);
            CommandManager = new CommandManager();
            
            // 初始化坐标变换模型
            Transform = new TransformModel();
            Transform.PropertyChanged += Transform_PropertyChanged;
            
            // 初始化分层渲染器
            InitializeLayeredRenderer();
            
            // 初始化图构建器
            _graphBuilder = new FlowGraphBuilder();
            
            // 初始化Invalidate防抖定时器（2.3.4 优化Invalidate调用频率）
            InitializeInvalidateDebounce();
            
            // 3.1.1 初始化选择管理器
            _selectionManager = new SelectionManager(BuilderStateStore.Instance);
            
            // 3.5.7 初始化快捷键管理器
            _shortcutManager = new Interaction.ShortcutManager(BuilderStateStore.Instance);
            
            // 订阅节点拖拽事件
            this.MouseDown += FlowDesignerCanvas_MouseDown;
            this.MouseMove += FlowDesignerCanvas_MouseMove;
            this.MouseUp += FlowDesignerCanvas_MouseUp;
            this.DragEnter += FlowDesignerCanvas_DragEnter;
            this.DragOver += FlowDesignerCanvas_DragOver;
            this.DragDrop += FlowDesignerCanvas_DragDrop;
            this.AllowDrop = true;
            this.KeyDown += FlowDesignerCanvas_KeyDown;
            this.KeyUp += FlowDesignerCanvas_KeyUp;
            this.MouseWheel += FlowDesignerCanvas_MouseWheel;
            this.MouseClick += FlowDesignerCanvas_MouseClick;
            this.MouseDoubleClick += FlowDesignerCanvas_MouseDoubleClick;
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;

            // 初始化上下文菜单
            _contextMenu = new Widgets.FlowContextMenu();
            _contextMenu.AddNodeRequested += ContextMenu_AddNodeRequested;
            _contextMenu.PasteRequested += ContextMenu_PasteRequested;
            _contextMenu.CopyRequested += ContextMenu_CopyRequested;
            _contextMenu.DeleteRequested += ContextMenu_DeleteRequested;
            _contextMenu.PropertiesRequested += ContextMenu_PropertiesRequested;
            _contextMenu.SkipNodeRequested += ContextMenu_SkipNodeRequested;

            // 订阅动画更新事件，触发重绘
            WindFromCanvas.Core.Applications.FlowDesigner.Animation.AnimationManager.Instance.AnimationUpdated += (s, e) => Invalidate();
        }

        /// <summary>
        /// 添加节点（内部方法，不记录命令）
        /// </summary>
        internal void AddNodeInternal(FlowNode node)
        {
            if (node == null || node.Data == null) return;

            _nodes[node.Data.Name] = node;
            AddObject(node);
            
            // 订阅节点事件
            node.DragStart += Node_DragStart;
            node.Drag += Node_Drag;
            node.DragEnd += Node_DragEnd;

            // 添加淡入动画
            if (AnimationManager.Instance.IsEnabled)
            {
                var fadeInAnimation = AnimationManager.Instance.CreateFadeInAnimation(0.3f);
                AnimationManager.Instance.AddAnimation(node, fadeInAnimation);
            }
            node.Click += Node_Click;
            node.MouseEnter += Node_MouseEnter;
            node.MouseLeave += Node_MouseLeave;

            Invalidate();
        }

        private void Node_DragStart(object sender, CanvasObjectEventArgs e)
        {
            var node = sender as FlowNode;
            if (node != null)
            {
                _nodeBeingDragged = node;
                _dragStartPosition = new PointF(node.X, node.Y);
            }
        }

        private void Node_Drag(object sender, CanvasObjectEventArgs e)
        {
            var node = sender as FlowNode;
            if (node != null && ShowAlignmentGuides)
            {
                // 更新对齐辅助线
                var otherNodes = _nodes.Values.Where(n => n != node).ToList();
                _alignmentGuides = NodeAlignmentHelper.GetAlignmentGuides(otherNodes, node);
                Invalidate();
            }
        }

        /// <summary>
        /// 添加节点（通过命令）
        /// </summary>
        public void AddNode(FlowNode node)
        {
            if (node == null || node.Data == null) return;

            var command = new AddNodeCommand(this, node);
            CommandManager.Execute(command);
        }

        /// <summary>
        /// 移除节点（带淡出动画）
        /// </summary>
        public void RemoveNode(FlowNode node)
        {
            if (node == null || node.Data == null) return;

            _nodes.Remove(node.Data.Name);
            RemoveObject(node);
            _selectedNodes.Remove(node);

            // 移除相关连接
            var connectionsToRemove = _connections.Values
                .Where(c => c.SourceNode == node || c.TargetNode == node)
                .ToList();

            foreach (var conn in connectionsToRemove)
            {
                RemoveConnection(conn);
            }

            Invalidate();
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        public void AddConnection(FlowConnection connection)
        {
            if (connection == null || connection.Data == null) return;

            var key = $"{connection.Data.SourceNode}_{connection.Data.TargetNode}";
            _connections[key] = connection;
            AddObject(connection);
            Invalidate();
        }

        /// <summary>
        /// 移除连接
        /// </summary>
        public void RemoveConnection(FlowConnection connection)
        {
            if (connection == null || connection.Data == null) return;

            var key = $"{connection.Data.SourceNode}_{connection.Data.TargetNode}";
            _connections.Remove(key);
            RemoveObject(connection);
            Invalidate();
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        public FlowConnection CreateConnection(FlowNode sourceNode, FlowNode targetNode, 
            string sourcePort = null, string targetPort = null)
        {
            if (sourceNode == null || targetNode == null || sourceNode.Data == null || targetNode.Data == null)
                return null;

            // 验证连接
            if (!CanCreateConnection(sourceNode, targetNode))
            {
                return null;
            }

            var connectionData = FlowConnectionData.Create(
                sourceNode.Data.Name,
                targetNode.Data.Name,
                sourcePort,
                targetPort
            );

            var connection = new FlowConnection(connectionData, sourceNode, targetNode);
            
            // 添加连接建立动画
            if (AnimationManager.Instance.IsEnabled)
            {
                connection.BuildProgress = 0f;
                var buildAnimation = AnimationManager.Instance.CreateConnectionBuildAnimation(0.5f);
                AnimationManager.Instance.AddAnimation(connection, buildAnimation);
                
                // 延迟启动流动动画（在建立动画完成后）
                var timer = new Timer();
                timer.Interval = 600; // 500ms动画 + 100ms缓冲
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    connection.BuildProgress = 1f;
                    var flowAnimation = AnimationManager.Instance.CreateConnectionFlowAnimation();
                    AnimationManager.Instance.AddAnimation(connection, flowAnimation);
                };
                timer.Start();
            }
            else
            {
                // 动画未启用时，直接启动流动动画
                connection.BuildProgress = 1f;
                var flowAnimation = AnimationManager.Instance.CreateConnectionFlowAnimation();
                AnimationManager.Instance.AddAnimation(connection, flowAnimation);
            }
            
            // 添加端口脉冲动画
            if (AnimationManager.Instance.IsEnabled)
            {
                var sourcePortPulse = AnimationManager.Instance.CreatePortPulseAnimation(0.6f);
                AnimationManager.Instance.AddAnimation(sourceNode, sourcePortPulse);
                
                var targetPortPulse = AnimationManager.Instance.CreatePortPulseAnimation(0.6f);
                AnimationManager.Instance.AddAnimation(targetNode, targetPortPulse);
            }
            
            // 标记端口为已连接
            if (sourceNode.OutputPorts.Count > 0)
            {
                sourceNode.ConnectedOutputPorts.Add(0);
            }
            if (targetNode.InputPorts.Count > 0)
            {
                targetNode.ConnectedInputPorts.Add(0);
            }
            
            AddConnection(connection);
            Document.Connections.Add(connectionData);
            return connection;
        }

        /// <summary>
        /// 清除所有选中
        /// </summary>
        public void ClearSelection()
        {
            foreach (var node in _selectedNodes)
            {
                node.IsSelected = false;
            }
            _selectedNodes.Clear();
            
            // 清空属性面板
            if (PropertiesPanel != null)
            {
                PropertiesPanel.SetSelectedNode(null);
            }
            
            if (EnableDirtyRegionRefresh)
            {
                // 标记所有选中节点的区域为脏区域
                foreach (var node in _selectedNodes)
                {
                    MarkDirtyRegion(node.GetBounds());
                }
                RefreshDirtyRegions();
            }
            else
            {
                Invalidate();
            }
        }

        /// <summary>
        /// 选中节点
        /// </summary>
        public void SelectNode(FlowNode node, bool addToSelection = false)
        {
            if (node == null) return;

            if (!addToSelection)
            {
                ClearSelection();
            }

            node.IsSelected = true;
            _selectedNodes.Add(node);
            Invalidate();
        }

        private PointF _dragStartPosition;
        private FlowNode _nodeBeingDragged;

        private void Node_DragEnd(object sender, CanvasObjectEventArgs e)
        {
            var node = sender as FlowNode;
            if (node != null && _nodeBeingDragged == node)
            {
                // 自动吸附对齐
                if (ShowAlignmentGuides)
                {
                    var otherNodes = _nodes.Values.Where(n => n != node).ToList();
                    NodeAlignmentHelper.SnapToAlignment(node, otherNodes);
                }

                var newPosition = new PointF(node.X, node.Y);
                if (_dragStartPosition != newPosition)
                {
                    // 创建移动命令
                    var command = new MoveNodeCommand(this, node, _dragStartPosition, newPosition);
                    CommandManager.Execute(command);
                }
                _nodeBeingDragged = null;
                _alignmentGuides.Clear();
                Invalidate();
            }
        }

        private void Node_Click(object sender, CanvasObjectEventArgs e)
        {
            var node = sender as FlowNode;
            if (node != null)
            {
                bool addToSelection = (Control.ModifierKeys & Keys.Control) != 0;
                SelectNode(node, addToSelection);
            }
        }

        private void Node_MouseEnter(object sender, CanvasObjectEventArgs e)
        {
            var node = sender as FlowNode;
            if (node != null)
            {
                node.IsHovered = true;
                Invalidate();
            }
        }

        private void Node_MouseLeave(object sender, CanvasObjectEventArgs e)
        {
            var node = sender as FlowNode;
            if (node != null)
            {
                node.IsHovered = false;
                Invalidate();
            }
        }

        private void FlowDesignerCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Space))
            {
                // 开始平移
                _isPanning = true;
                _panStartPoint = e.Location;
                this.Cursor = Cursors.Hand;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                var worldPoint = ScreenToWorld(e.Location);
                var hitNode = HitTestNode(worldPoint);
                
                // 3.1.5 处理Ctrl多选逻辑
                bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                // 3.1.6 处理Shift范围选择逻辑
                bool isShiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

                if (hitNode != null)
                {
                    // 检查是否点击了输出端口
                    var outputPort = hitNode.HitTestPort(worldPoint, true);
                    if (outputPort.HasValue)
                    {
                        // 开始创建连接
                        StartConnection(hitNode);
                        _connectionPreviewEnd = e.Location;
                        return;
                    }

                    // 3.1.2 使用SelectionManager选择节点
                    // 获取对应的ICanvasNode
                    var canvasNode = GetCanvasNodeFromFlowNode(hitNode);
                    if (canvasNode != null)
                    {
                        _selectionManager.SelectNode(canvasNode, isCtrlPressed);
                    }

                    _draggingNode = hitNode;
                    _dragPreviewPosition = e.Location;
                    
                    // 如果当前节点已选中，检查是否多选拖拽
                    if (_selectedNodes.Contains(hitNode) && _selectedNodes.Count > 1)
                    {
                        _draggingNodes = new List<FlowNode>(_selectedNodes);
                    }
                    else if (!isCtrlPressed)
                    {
                        _draggingNodes.Clear();
                        _draggingNodes.Add(hitNode);
                    }

                    // 3.1.7 标记选择层需要重绘（视觉反馈）
                    MarkLayerDirty(RenderLayerType.Selection);
                }
                else
                {
                    // 3.1.2 开始框选
                    _isSelecting = true;
                    _selectionStart = e.Location;
                    _selectionEnd = e.Location;
                    
                    // 使用SelectionManager开始框选
                    var canvasPoint = Transform.ClientToCanvas(e.Location);
                    _selectionManager.StartSelection(canvasPoint);
                    
                    // 如果没有按Ctrl，清除现有选择
                    if (!isCtrlPressed)
                    {
                        _selectedNodes.Clear();
                    }
                }
            }
        }

        private void FlowDesignerCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var deltaX = e.X - _panStartPoint.X;
                var deltaY = e.Y - _panStartPoint.Y;
                PanOffset = new PointF(PanOffset.X + deltaX, PanOffset.Y + deltaY);
                _panStartPoint = e.Location;
                
                // 2.4.5 优化滚动时的重绘性能（使用防抖）
                InvalidateDebounced();
            }
            else if (_isSelecting)
            {
                // 3.1.3 更新框选
                _selectionEnd = e.Location;
                
                // 使用SelectionManager更新框选
                var canvasPoint = Transform.ClientToCanvas(e.Location);
                _selectionManager.UpdateSelection(canvasPoint);
                
                // 3.1.7 标记选择层需要重绘
                MarkLayerDirty(RenderLayerType.Selection);
            }
            else if (_isCreatingConnection && _connectionSourceNode != null)
            {
                _connectionPreviewEnd = e.Location;
                
                // 检测候选目标节点
                var worldPoint = ScreenToWorld(e.Location);
                var targetNode = HitTestNode(worldPoint);
                
                // 清除之前的锚点高亮
                _hoveredAnchor = null;
                
                // 清除之前的目标节点端口高亮
                if (_potentialTargetNode != null && _potentialTargetNode != targetNode)
                {
                    _potentialTargetNode.HoveredInputPortIndex = -1;
                }
                
                // 设置新的目标节点
                _potentialTargetNode = targetNode;
                
                // 3.3.4 / 3.3.5 锚点感应和连接规则校验
                if (_potentialTargetNode != null && _potentialTargetNode != _connectionSourceNode)
                {
                    // 3.3.5 实时校验连接规则
                    bool canConnect = ValidateConnection(_connectionSourceNode, _potentialTargetNode);
                    
                    if (canConnect)
                    {
                        // 3.3.2 查找最近的锚点
                        var nearestAnchor = FindNearestAnchor(worldPoint, _potentialTargetNode);
                        
                        if (nearestAnchor != null)
                        {
                            // 3.3.4 高亮锚点（端口）
                            _hoveredAnchor = nearestAnchor;
                            
                            // 锚点吸附：将预览终点吸附到锚点位置
                            var nodeBounds = _potentialTargetNode.GetBounds();
                            var nodeCenter = new PointF(
                                nodeBounds.X + nodeBounds.Width / 2,
                                nodeBounds.Y + nodeBounds.Height / 2
                            );
                            var anchorWorldPos = new PointF(
                                nodeCenter.X + nearestAnchor.RelativeX * nodeBounds.Width,
                                nodeCenter.Y + nearestAnchor.RelativeY * nodeBounds.Height
                            );
                            _connectionPreviewEnd = WorldToScreen(anchorWorldPos);
                        }
                    }
                }
                
                // 更新连接预览动画
                _connectionPreviewAnimationOffset += 2f;
                if (_connectionPreviewAnimationOffset > 20f)
                    _connectionPreviewAnimationOffset = 0f;
                
                Invalidate();
            }
            else if (_draggingNode != null)
            {
                // 更新拖拽预览位置
                var worldPos = ScreenToWorld(e.Location);
                _dragPreviewPosition = worldPos;
                
                // 临时更新节点位置（用于计算对齐）
                var originalPos = new PointF(_draggingNode.X, _draggingNode.Y);
                _draggingNode.X = worldPos.X;
                _draggingNode.Y = worldPos.Y;
                
                // 3.4.2 / 3.4.3 / 3.4.4 计算对齐辅助线并自动吸附
                var otherNodes = _nodes.Values.Where(n => n != _draggingNode);
                
                if (_draggingNodes.Count > 1)
                {
                    // 3.4.6 多节点同时对齐
                    NodeAlignmentHelper.SnapMultipleNodesToAlignment(_draggingNodes, otherNodes);
                }
                else
                {
                    // 单节点吸附
                    NodeAlignmentHelper.SnapToAlignment(_draggingNode, otherNodes);
                }
                
                // 计算对齐辅助线（用于视觉渲染）
                var viewport = GetVisibleCanvasBounds();
                _currentAlignmentGuides = NodeAlignmentHelper.GetAlignmentGuides(otherNodes, _draggingNode, viewport);
                
                // 更新拖拽预览位置（应用吸附后的位置）
                _dragPreviewPosition = new PointF(_draggingNode.X, _draggingNode.Y);
                
                // 恢复原始位置（实际移动在MouseUp时完成）
                _draggingNode.X = originalPos.X;
                _draggingNode.Y = originalPos.Y;
                
                // 标记覆盖层需要重绘（显示对齐线）
                MarkLayerDirty(RenderLayerType.Overlay);
            }
        }

        private void FlowDesignerCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Space))
            {
                _isPanning = false;
                this.Cursor = Cursors.Default;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (_isSelecting)
                {
                    // 3.1.4 完成框选
                    bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    
                    // 使用SelectionManager完成框选
                    var canvasNodes = _currentGraph?.Nodes ?? new List<ICanvasNode>();
                    _selectionManager.EndSelection(canvasNodes.ToList(), isCtrlPressed);
                    
                    // 同步选择状态到_selectedNodes
                    SyncSelectionState();
                    
                    _isSelecting = false;
                    
                    // 3.1.7 标记选择层需要重绘
                    MarkLayerDirty(RenderLayerType.Selection);
                }
                else if (_isCreatingConnection)
                {
                    // 完成连接创建
                    var worldPoint = ScreenToWorld(e.Location);
                    var targetNode = HitTestNode(worldPoint);
                    
                    // 如果没有直接点击节点，尝试使用候选目标节点
                    if (targetNode == null && _potentialTargetNode != null)
                    {
                        targetNode = _potentialTargetNode;
                    }
                    
                    if (targetNode != null && targetNode != _connectionSourceNode)
                    {
                        CreateConnection(_connectionSourceNode, targetNode);
                    }
                    
                    // 清除状态
                    if (_potentialTargetNode != null)
                    {
                        _potentialTargetNode.HoveredInputPortIndex = -1;
                        _potentialTargetNode = null;
                    }
                    
                    _isCreatingConnection = false;
                    _connectionSourceNode = null;
                    _connectionPreviewAnimationOffset = 0f;
                    InvalidateDebounced();
                }

                _draggingNode = null;
                
                // 清除对齐辅助线
                _currentAlignmentGuides = null;
                MarkLayerDirty(RenderLayerType.Overlay);
            }
        }

        /// <summary>
        /// 检测点击的节点（使用世界坐标）
        /// </summary>
        private FlowNode HitTestNode(PointF worldPoint)
        {
            return _nodes.Values
                .OrderByDescending(n => n.ZIndex)
                .FirstOrDefault(n => n.Visible && n.HitTest(worldPoint));
        }

        /// <summary>
        /// 框选节点
        /// </summary>
        private void SelectNodesInRectangle()
        {
            var screenRect = new RectangleF(
                Math.Min(_selectionStart.X, _selectionEnd.X),
                Math.Min(_selectionStart.Y, _selectionEnd.Y),
                Math.Abs(_selectionEnd.X - _selectionStart.X),
                Math.Abs(_selectionEnd.Y - _selectionStart.Y)
            );

            // 转换为世界坐标
            var worldRect = new RectangleF(
                ScreenToWorld(new PointF(screenRect.X, screenRect.Y)),
                new SizeF(screenRect.Width / ZoomFactor, screenRect.Height / ZoomFactor)
            );

            // 支持Ctrl和Shift追加选择（参考Activepieces）
            bool addToSelection = (Control.ModifierKeys & (Keys.Control | Keys.Shift)) != 0;
            if (!addToSelection)
            {
                ClearSelection();
            }

            foreach (var node in _nodes.Values)
            {
                var bounds = node.GetBounds();
                // 智能框选：部分包含也算（参考Activepieces）
                if (worldRect.IntersectsWith(bounds) || worldRect.Contains(bounds))
                {
                    SelectNode(node, true);
                }
            }
        }

        /// <summary>
        /// 更新节点的所有连接
        /// </summary>
        internal void UpdateConnectionsForNode(FlowNode node)
        {
            foreach (var conn in _connections.Values)
            {
                if (conn.SourceNode == node || conn.TargetNode == node)
                {
                    conn.Update();
                    // 标记连接区域为脏区域
                    if (EnableDirtyRegionRefresh)
                    {
                        MarkDirtyRegion(conn.GetBounds());
                    }
                }
            }

            // 标记节点区域为脏区域
            if (EnableDirtyRegionRefresh)
            {
                MarkDirtyRegion(node.GetBounds());
            }
        }

        /// <summary>
        /// 标记脏区域
        /// </summary>
        private void MarkDirtyRegion(RectangleF region)
        {
            // 转换为屏幕坐标
            var screenRegion = new RectangleF(
                region.X * ZoomFactor + PanOffset.X,
                region.Y * ZoomFactor + PanOffset.Y,
                region.Width * ZoomFactor,
                region.Height * ZoomFactor
            );

            // 扩展区域以确保完全覆盖
            screenRegion.Inflate(10, 10);

            _dirtyRegions.Add(screenRegion);
        }

        /// <summary>
        /// 只刷新脏区域
        /// </summary>
        public void RefreshDirtyRegions()
        {
            if (!EnableDirtyRegionRefresh || _dirtyRegions.Count == 0)
            {
                Invalidate();
                return;
            }

            foreach (var region in _dirtyRegions)
            {
                Invalidate(new Rectangle(
                    (int)region.X,
                    (int)region.Y,
                    (int)region.Width,
                    (int)region.Height
                ));
            }

            _dirtyRegions.Clear();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 计算视口（Canvas坐标系）
            var viewport = GetVisibleCanvasBounds();

            // 使用LayeredRenderer进行6层渲染
            if (_layeredRenderer != null)
            {
                _layeredRenderer.Render(g, viewport, Transform.Zoom);
            }
            else
            {
                // 降级渲染（如果LayeredRenderer未初始化）
                g.Clear(BackgroundColor);
            }

            // 记录性能
            PerformanceMonitor.Instance.RecordFrame();
        }

        /// <summary>
        /// 网格类型（点状或线状）
        /// </summary>
        public enum GridType
        {
            Lines,  // 线状网格
            Dots    // 点状网格（Activepieces风格）
        }

        /// <summary>
        /// 网格类型
        /// </summary>
        public GridType GridStyle { get; set; } = GridType.Dots;

        /// <summary>
        /// 绘制网格（支持点状和线状）
        /// </summary>
        private void DrawGrid(Graphics g)
        {
            var theme = Themes.ThemeManager.Instance.CurrentTheme;
            var gridColor = theme.GridColor;
            
            if (GridStyle == GridType.Dots)
            {
                DrawDotGrid(g, gridColor);
            }
            else
            {
                DrawLineGrid(g, gridColor);
            }
        }

        /// <summary>
        /// 绘制点状网格（Activepieces风格）
        /// </summary>
        private void DrawDotGrid(Graphics g, Color gridColor)
        {
            var gridSize = GridSize * ZoomFactor;
            var dotSize = 1f; // Activepieces标准：1px
            var startX = (PanOffset.X % gridSize) - gridSize;
            var startY = (PanOffset.Y % gridSize) - gridSize;

            // 根据缩放级别调整点的大小和间距
            if (ZoomFactor < 0.5f)
            {
                // 缩放太小时，增大间距，减少点数
                gridSize *= 2;
                startX = (PanOffset.X % gridSize) - gridSize;
                startY = (PanOffset.Y % gridSize) - gridSize;
            }
            else if (ZoomFactor > 1.5f)
            {
                // 放大时，可以显示更密集的点
                dotSize = 1.5f;
            }

            using (var brush = new SolidBrush(gridColor))
            {
                for (float x = startX; x < this.Width + gridSize; x += gridSize)
                {
                    for (float y = startY; y < this.Height + gridSize; y += gridSize)
                    {
                        // 绘制点（小圆）
                        g.FillEllipse(brush, x - dotSize / 2, y - dotSize / 2, dotSize, dotSize);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制线状网格（原有实现）
        /// </summary>
        private void DrawLineGrid(Graphics g, Color gridColor)
        {
            var gridSize = GridSize * ZoomFactor;
            var startX = (PanOffset.X % gridSize) - gridSize;
            var startY = (PanOffset.Y % gridSize) - gridSize;

            using (var pen = new Pen(gridColor, 1))
            {
                // 绘制垂直线
                for (float x = startX; x < this.Width + gridSize; x += gridSize)
                {
                    g.DrawLine(pen, x, 0, x, this.Height);
                }

                // 绘制水平线
                for (float y = startY; y < this.Height + gridSize; y += gridSize)
                {
                    g.DrawLine(pen, 0, y, this.Width, y);
                }
            }
        }

        /// <summary>
        /// 是否显示小地图
        /// </summary>
        public bool ShowMinimap { get; set; } = false;

        /// <summary>
        /// 平移模式枚举
        /// </summary>
        public enum CanvasPanningMode
        {
            Pan,   // 选择模式
            Grab   // 抓取模式
        }

        /// <summary>
        /// 平移模式（grab或pan）
        /// </summary>
        public CanvasPanningMode PanningMode { get; set; } = CanvasPanningMode.Pan;

        /// <summary>
        /// 缩放画布
        /// </summary>
        public void SetZoom(float factor, PointF? centerPoint = null)
        {
            factor = Math.Max(0.5f, Math.Min(2.0f, factor)); // 限制在50%-200%（Activepieces标准）

            if (centerPoint.HasValue)
            {
                // 以指定点为中心缩放
                var worldPoint = ScreenToWorld(centerPoint.Value);
                ZoomFactor = factor;
                var newWorldPoint = ScreenToWorld(centerPoint.Value);
                PanOffset = new PointF(
                    PanOffset.X + (worldPoint.X - newWorldPoint.X) * ZoomFactor,
                    PanOffset.Y + (worldPoint.Y - newWorldPoint.Y) * ZoomFactor
                );
            }
            else
            {
                ZoomFactor = factor;
            }

            Invalidate();
        }

        /// <summary>
        /// 屏幕坐标转世界坐标
        /// </summary>
        private PointF ScreenToWorld(PointF screenPoint)
        {
            return new PointF(
                (screenPoint.X - PanOffset.X) / ZoomFactor,
                (screenPoint.Y - PanOffset.Y) / ZoomFactor
            );
        }

        /// <summary>
        /// 世界坐标转屏幕坐标
        /// </summary>
        private PointF WorldToScreen(PointF worldPoint)
        {
            return new PointF(
                worldPoint.X * ZoomFactor + PanOffset.X,
                worldPoint.Y * ZoomFactor + PanOffset.Y
            );
        }

        /// <summary>
        /// 缩放画布（以鼠标为中心）
        /// </summary>
        public void ZoomIn(PointF? centerPoint = null)
        {
            var newZoom = Math.Min(2.0f, ZoomFactor + 0.1f);
            SetZoom(newZoom, centerPoint);
        }

        /// <summary>
        /// 缩小画布（以鼠标为中心）
        /// </summary>
        public void ZoomOut(PointF? centerPoint = null)
        {
            var newZoom = Math.Max(0.5f, ZoomFactor - 0.1f);
            SetZoom(newZoom, centerPoint);
        }

        /// <summary>
        /// 适应视图（Fit to View）- 自动缩放和居中显示所有节点
        /// </summary>
        public void FitToView()
        {
            if (_nodes.Count == 0) return;

            // 计算所有节点的边界框
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var node in _nodes.Values)
            {
                var bounds = node.GetBounds();
                minX = Math.Min(minX, bounds.Left);
                minY = Math.Min(minY, bounds.Top);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            var graphWidth = maxX - minX;
            var graphHeight = maxY - minY;

            if (graphWidth <= 0 || graphHeight <= 0) return;

            // 添加边距（100px，Activepieces标准）
            var padding = 100f;
            var targetWidth = this.Width - padding * 2;
            var targetHeight = this.Height - padding * 2;

            // 计算缩放比例（保持宽高比，限制在0.9-1.25之间，参考Activepieces）
            var zoomRatio = Math.Min(
                Math.Max(targetWidth / graphWidth, targetHeight / graphHeight),
                1.25f
            );
            zoomRatio = Math.Max(zoomRatio, 0.9f);

            // 设置缩放
            ZoomFactor = zoomRatio;

            // 居中显示
            var centerX = (minX + maxX) / 2;
            var centerY = (minY + maxY) / 2;
            var screenCenterX = this.Width / 2;
            var screenCenterY = this.Height / 2;

            PanOffset = new PointF(
                screenCenterX - centerX * ZoomFactor,
                screenCenterY - centerY * ZoomFactor + padding * ZoomFactor
            );

            Invalidate();
        }

        /// <summary>
        /// 确保节点在视口内可见
        /// </summary>
        public void EnsureNodeVisible(FlowNode node)
        {
            if (node == null) return;

            var bounds = node.GetBounds();
            var screenBounds = new RectangleF(
                bounds.X * ZoomFactor + PanOffset.X,
                bounds.Y * ZoomFactor + PanOffset.Y,
                bounds.Width * ZoomFactor,
                bounds.Height * ZoomFactor
            );

            var viewport = new RectangleF(0, 0, this.Width, this.Height);
            
            // 检查节点是否在视口外
            if (!viewport.Contains(screenBounds))
            {
                // 计算需要移动的距离
                var deltaX = 0f;
                var deltaY = 0f;

                if (screenBounds.Right > viewport.Right)
                    deltaX = viewport.Right - screenBounds.Right - bounds.Width * ZoomFactor;
                else if (screenBounds.Left < viewport.Left)
                    deltaX = viewport.Left - screenBounds.Left;

                if (screenBounds.Bottom > viewport.Bottom)
                    deltaY = viewport.Bottom - screenBounds.Bottom - bounds.Height * ZoomFactor;
                else if (screenBounds.Top < viewport.Top)
                    deltaY = viewport.Top - screenBounds.Top;

                PanOffset = new PointF(PanOffset.X + deltaX, PanOffset.Y + deltaY);
                Invalidate();
            }
        }

        /// <summary>
        /// 处理鼠标滚轮事件
        /// </summary>
        private void FlowDesignerCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                var delta = e.Delta > 0 ? 1.1f : 0.9f;
                SetZoom(ZoomFactor * delta, e.Location);
            }
        }

        /// <summary>
        /// 处理键盘释放事件
        /// </summary>
        private void FlowDesignerCanvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _isPanning = false;
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 处理拖拽进入
        /// </summary>
        private void FlowDesignerCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FlowNodeType)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// 处理拖拽悬停
        /// </summary>
        private void FlowDesignerCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FlowNodeType)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// 处理拖拽释放
        /// </summary>
        private void FlowDesignerCanvas_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FlowNodeType)) && Toolbox != null)
            {
                var nodeType = (FlowNodeType)e.Data.GetData(typeof(FlowNodeType));
                var position = this.PointToClient(new Point(e.X, e.Y));
                
                var node = Toolbox.CreateNode(nodeType, position);
                if (node != null)
                {
                    AddNode(node);
                }
            }
        }

        /// <summary>
        /// 开始创建连接（从节点输出端口）
        /// </summary>
        public void StartConnection(FlowNode sourceNode)
        {
            if (sourceNode != null)
            {
                _isCreatingConnection = true;
                _connectionSourceNode = sourceNode;
            }
        }

        /// <summary>
        /// 删除选中的节点
        /// </summary>
        public void DeleteSelectedNodes()
        {
            var nodesToDelete = _selectedNodes.ToList();
            foreach (var node in nodesToDelete)
            {
                RemoveNode(node);
                if (node.Data != null)
                {
                    Document.Nodes.Remove(node.Data);
                }
            }
            _selectedNodes.Clear();
            Invalidate();
        }

        /// <summary>
        /// 处理键盘事件
        /// </summary>
        private void FlowDesignerCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            // 3.5.7 使用ShortcutManager统一处理快捷键
            var keyData = e.KeyData;
            
            // 尝试由ShortcutManager处理
            if (_shortcutManager != null && _shortcutManager.HandleKeyPress(keyData))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                // 刷新画布以显示更改
                RefreshCanvas();
                return;
            }

            // 保留原有的撤销/重做逻辑（不在ShortcutManager中）
            if (e.Control && e.KeyCode == Keys.Z)
            {
                // Ctrl+Z 撤销
                if (CommandManager.CanUndo)
                {
                    CommandManager.Undo();
                    e.Handled = true;
                }
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                // Ctrl+Y 重做
                if (CommandManager.CanRedo)
                {
                    CommandManager.Redo();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 复制选中的节点
        /// </summary>
        public void CopySelectedNodes()
        {
            if (_selectedNodes.Count == 0) return;

            var connections = _connections.Values
                .Where(c => _selectedNodes.Contains(c.SourceNode) && _selectedNodes.Contains(c.TargetNode))
                .ToList();

            Clipboard.FlowClipboard.CopyNodes(_selectedNodes, connections);
        }

        /// <summary>
        /// 粘贴节点
        /// </summary>
        public void PasteNodes()
        {
            var clipboardData = Clipboard.FlowClipboard.PasteNodes();
            if (clipboardData == null || clipboardData.Nodes == null || clipboardData.Nodes.Count == 0)
                return;

            // 计算偏移量（避免重叠）
            var offsetX = 50f;
            var offsetY = 50f;

            // 创建节点映射（旧名称 -> 新节点）
            var nodeMap = new Dictionary<string, FlowNode>();
            var newNodes = new List<FlowNode>();

            foreach (var nodeData in clipboardData.Nodes)
            {
                // 生成新名称
                var newName = GenerateUniqueNodeName(nodeData.Name);
                var newData = nodeData.Clone();
                newData.Name = newName;
                newData.Position = new PointF(
                    nodeData.Position.X + offsetX,
                    nodeData.Position.Y + offsetY
                );

                // 创建节点
                FlowNode newNode = null;
                if (Toolbox != null)
                {
                    newNode = Toolbox.CreateNodeFromData(newData);
                }
                else
                {
                    // 如果没有工具箱，使用默认创建方法
                    newNode = CreateNodeFromData(newData);
                }

                if (newNode != null)
                {
                    nodeMap[nodeData.Name] = newNode;
                    newNodes.Add(newNode);
                    AddNode(newNode);
                }
            }

            // 重建连接
            foreach (var connData in clipboardData.Connections)
            {
                if (nodeMap.TryGetValue(connData.SourceNode, out var sourceNode) &&
                    nodeMap.TryGetValue(connData.TargetNode, out var targetNode))
                {
                    CreateConnection(sourceNode, targetNode, connData.SourcePort, connData.TargetPort);
                }
            }

            // 选中新粘贴的节点
            ClearSelection();
            foreach (var node in newNodes)
            {
                SelectNode(node, true);
            }
        }

        /// <summary>
        /// 生成唯一的节点名称
        /// </summary>
        private string GenerateUniqueNodeName(string baseName)
        {
            var name = baseName;
            int counter = 1;

            while (_nodes.ContainsKey(name) || Document.Nodes.Any(n => n.Name == name))
            {
                name = $"{baseName}_{counter}";
                counter++;
            }

            return name;
        }

        /// <summary>
        /// 从数据创建节点（默认实现）
        /// </summary>
        private FlowNode CreateNodeFromData(FlowNodeData data)
        {
            switch (data.Type)
            {
                case FlowNodeType.Start:
                    return new StartNode(data);
                case FlowNodeType.Process:
                    return new ProcessNode(data);
                case FlowNodeType.Decision:
                    return new DecisionNode(data);
                case FlowNodeType.Loop:
                    return new LoopNode(data);
                case FlowNodeType.End:
                    return new EndNode(data);
                default:
                    return new ProcessNode(data);
            }
        }

        /// <summary>
        /// 保存流程到文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            // TODO: 适配 FlowDocument 到 FlowVersion 的转换
            // 当前保持原有逻辑，使用旧的序列化方式
            var serializer = new FlowSerializer();
            // 注意：这里需要将 FlowDocument 转换为 FlowVersion，暂时注释
            // serializer.SaveToFile(ConvertDocumentToVersion(Document), filePath);
        }

        /// <summary>
        /// 从文件加载流程
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            // TODO: 适配 FlowVersion 到 FlowDocument 的转换
            // 当前保持原有逻辑，使用旧的序列化方式
            var serializer = new FlowSerializer();
            // 注意：这里需要将 FlowVersion 转换为 FlowDocument，暂时注释
            // var version = serializer.LoadFromFile(filePath);
            // if (version != null)
            // {
            //     LoadDocument(ConvertVersionToDocument(version));
            // }
        }

        /// <summary>
        /// 加载流程文档
        /// </summary>
        public void LoadDocument(FlowDocument document)
        {
            if (document == null) return;

            // 清空当前内容
            Clear();
            Document = document;

            // 创建节点
            if (Toolbox != null && document.Nodes != null)
            {
                foreach (var nodeData in document.Nodes)
                {
                    var node = Toolbox.CreateNodeFromData(nodeData);
                    if (node != null)
                    {
                        AddNodeInternal(node);
                    }
                }
            }

            // 创建连接
            if (document.Connections != null)
            {
                foreach (var connData in document.Connections)
                {
                    if (_nodes.TryGetValue(connData.SourceNode, out var sourceNode) &&
                        _nodes.TryGetValue(connData.TargetNode, out var targetNode))
                    {
                        CreateConnection(sourceNode, targetNode, connData.SourcePort, connData.TargetPort);
                    }
                }
            }

            Invalidate();
        }

        /// <summary>
        /// 清空画布
        /// </summary>
        public new void Clear()
        {
            _nodes.Clear();
            _connections.Clear();
            _selectedNodes.Clear();
            base.Clear();
            Document = new FlowDocument();
            Invalidate();
        }

        /// <summary>
        /// 导出为图片
        /// </summary>
        public Bitmap ExportToImage()
        {
            // 计算所有节点的边界
            if (_nodes.Count == 0)
            {
                return new Bitmap(800, 600);
            }

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var node in _nodes.Values)
            {
                var bounds = node.GetBounds();
                minX = Math.Min(minX, bounds.Left);
                minY = Math.Min(minY, bounds.Top);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            // 添加边距
            var padding = 50f;
            var width = (int)(maxX - minX + padding * 2);
            var height = (int)(maxY - minY + padding * 2);

            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TranslateTransform(-minX + padding, -minY + padding);

                // 绘制所有对象
                foreach (var obj in Objects.OrderBy(o => o.ZIndex))
                {
                    if (obj.Visible)
                    {
                        obj.Draw(g);
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// 导出为PNG文件
        /// </summary>
        public void ExportToPng(string filePath)
        {
            var bitmap = ExportToImage();
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Dispose();
        }

        /// <summary>
        /// 绘制连接预览（贝塞尔曲线，带目标高亮）
        /// </summary>
        private void DrawConnectionPreview(Graphics g)
        {
            if (_connectionSourceNode == null) return;

            // 获取源节点输出端口位置（屏幕坐标）
            var sourceBounds = _connectionSourceNode.GetBounds();
            var sourcePort = new PointF(
                sourceBounds.Right * ZoomFactor + PanOffset.X,
                (sourceBounds.Y + sourceBounds.Height / 2) * ZoomFactor + PanOffset.Y
            );

            // 目标点（屏幕坐标）
            var targetPoint = _connectionPreviewEnd;

            // 如果存在候选目标节点，高亮其输入端口
            if (_potentialTargetNode != null && _potentialTargetNode != _connectionSourceNode)
            {
                var targetBounds = _potentialTargetNode.GetBounds();
                
                // 绘制目标节点高亮光环
                var highlightRect = new RectangleF(
                    targetBounds.X * ZoomFactor + PanOffset.X - 5,
                    targetBounds.Y * ZoomFactor + PanOffset.Y - 5,
                    targetBounds.Width * ZoomFactor + 10,
                    targetBounds.Height * ZoomFactor + 10
                );
                
                using (var pen = new Pen(Color.FromArgb(100, 59, 130, 246), 3f))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(pen, highlightRect.X, highlightRect.Y, highlightRect.Width, highlightRect.Height);
                }
            }

            // 计算贝塞尔曲线控制点
            var dx = Math.Abs(targetPoint.X - sourcePort.X);
            var controlOffset = Math.Max(50f, dx * 0.4f);
            var cp1 = new PointF(sourcePort.X + controlOffset, sourcePort.Y);
            var cp2 = new PointF(targetPoint.X - controlOffset, targetPoint.Y);

            // 绘制贝塞尔曲线预览
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddBezier(sourcePort, cp1, cp2, targetPoint);

                // 使用虚线样式，带流动动画效果
                using (var pen = new Pen(Color.FromArgb(148, 163, 184), 2f))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    pen.DashPattern = new float[] { 8f, 4f };
                    pen.DashOffset = _connectionPreviewAnimationOffset;
                    g.DrawPath(pen, path);
                }
            }

            // 绘制源端口高亮
            DrawPortHighlight(g, sourcePort, true);
            
            // 如果接近目标端口，绘制目标端口高亮
            if (_potentialTargetNode != null && _potentialTargetNode.HoveredInputPortIndex >= 0)
            {
                var targetPort = _potentialTargetNode.InputPorts[_potentialTargetNode.HoveredInputPortIndex];
                var targetPortScreen = WorldToScreen(targetPort);
                DrawPortHighlight(g, targetPortScreen, false);
            }
        }

        /// <summary>
        /// 绘制端口高亮
        /// </summary>
        private void DrawPortHighlight(Graphics g, PointF portScreen, bool isOutput)
        {
            var highlightSize = 16f;
            var highlightRect = new RectangleF(
                portScreen.X - highlightSize / 2,
                portScreen.Y - highlightSize / 2,
                highlightSize,
                highlightSize
            );

            // 外圈高亮
            using (var brush = new SolidBrush(Color.FromArgb(50, 59, 130, 246)))
            {
                g.FillEllipse(brush, highlightRect);
            }

            // 内圈
            var innerSize = highlightSize * 0.6f;
            var innerRect = new RectangleF(
                portScreen.X - innerSize / 2,
                portScreen.Y - innerSize / 2,
                innerSize,
                innerSize
            );
            using (var brush = new SolidBrush(Color.FromArgb(150, 59, 130, 246)))
            {
                g.FillEllipse(brush, innerRect);
            }
        }

        /// <summary>
        /// 绘制拖拽预览（半透明预览层，参考Activepieces）
        /// </summary>
        private void DrawDragPreview(Graphics g, List<FlowNode> nodes, PointF mousePosition)
        {
            if (nodes.Count == 0) return;

            // 计算第一个节点的偏移量
            var firstNode = nodes[0];
            var offsetX = mousePosition.X - (firstNode.X * ZoomFactor + PanOffset.X);
            var offsetY = mousePosition.Y - (firstNode.Y * ZoomFactor + PanOffset.Y);

            foreach (var node in nodes)
            {
                var bounds = node.GetBounds();
                var previewRect = new RectangleF(
                    bounds.X * ZoomFactor + PanOffset.X + offsetX - bounds.Width * ZoomFactor / 2,
                    bounds.Y * ZoomFactor + PanOffset.Y + offsetY - bounds.Height * ZoomFactor / 2,
                    bounds.Width * ZoomFactor,
                    bounds.Height * ZoomFactor
                );

                // 绘制半透明预览（75x75px，参考Activepieces）
                var previewSize = 75f;
                var centerRect = new RectangleF(
                    previewRect.X + (previewRect.Width - previewSize) / 2,
                    previewRect.Y + (previewRect.Height - previewSize) / 2,
                    previewSize,
                    previewSize
                );

                // 绘制半透明背景
                using (var brush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                {
                    g.FillRectangle(brush, centerRect);
                }

                // 绘制边框
                using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 2f))
                {
                    g.DrawRectangle(pen, centerRect.X, centerRect.Y, centerRect.Width, centerRect.Height);
                }

                // 如果是多选，显示数量徽章
                if (nodes.Count > 1)
                {
                    var badgeRect = new RectangleF(
                        centerRect.Right - 20,
                        centerRect.Top - 10,
                        25,
                        25
                    );
                    using (var brush = new SolidBrush(Color.FromArgb(59, 130, 246)))
                    {
                        g.FillEllipse(brush, badgeRect);
                    }
                    using (var brush = new SolidBrush(Color.White))
                    using (var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    })
                    {
                        g.DrawString(nodes.Count.ToString(), SystemFonts.DefaultFont, brush, badgeRect, sf);
                    }
                }
                else
                {
                    // 单节点预览：显示节点图标（简化）
                    var iconSize = 30f;
                    var iconRect = new RectangleF(
                        centerRect.X + (centerRect.Width - iconSize) / 2,
                        centerRect.Y + (centerRect.Height - iconSize) / 2,
                        iconSize,
                        iconSize
                    );
                    using (var brush = new SolidBrush(Color.FromArgb(59, 130, 246)))
                    {
                        g.FillRectangle(brush, iconRect);
                    }
                }
            }
        }

        /// <summary>
        /// 处理鼠标双击事件（节点编辑）
        /// </summary>
        private void FlowDesignerCanvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var worldPoint = ScreenToWorld(e.Location);
                var hitNode = HitTestNode(worldPoint);
                
                if (hitNode != null && hitNode.Data != null)
                {
                    // 双击节点进入编辑模式
                    StartNodeNameEdit(hitNode);
                }
            }
        }

        /// <summary>
        /// 当前正在编辑的节点
        /// </summary>
        private FlowNode _editingNode;
        private TextBox _nodeNameEditor;

        /// <summary>
        /// 开始编辑节点名称
        /// </summary>
        private void StartNodeNameEdit(FlowNode node)
        {
            if (node == null || node.Data == null) return;

            _editingNode = node;
            var bounds = node.GetBounds();
            var screenBounds = new RectangleF(
                bounds.X * ZoomFactor + PanOffset.X,
                bounds.Y * ZoomFactor + PanOffset.Y,
                bounds.Width * ZoomFactor,
                bounds.Height * ZoomFactor
            );

            // 创建内嵌文本框
            _nodeNameEditor = new TextBox
            {
                Text = node.Data.DisplayName ?? node.Data.Name ?? "",
                Location = new Point((int)screenBounds.X + 40, (int)screenBounds.Y + 5),
                Size = new Size((int)screenBounds.Width - 50, (int)screenBounds.Height - 10),
                Font = SystemFonts.DefaultFont,
                BorderStyle = BorderStyle.FixedSingle
            };
            _nodeNameEditor.KeyDown += NodeNameEditor_KeyDown;
            _nodeNameEditor.LostFocus += NodeNameEditor_LostFocus;
            _nodeNameEditor.SelectAll();

            this.Controls.Add(_nodeNameEditor);
            _nodeNameEditor.Focus();
        }

        private void NodeNameEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FinishNodeNameEdit();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelNodeNameEdit();
            }
        }

        private void NodeNameEditor_LostFocus(object sender, EventArgs e)
        {
            FinishNodeNameEdit();
        }

        private void FinishNodeNameEdit()
        {
            if (_editingNode != null && _nodeNameEditor != null)
            {
                var newName = _nodeNameEditor.Text.Trim();
                
                // 验证名称唯一性
                if (!string.IsNullOrEmpty(newName) && IsNodeNameUnique(newName, _editingNode))
                {
                    _editingNode.Data.DisplayName = newName;
                    if (string.IsNullOrEmpty(_editingNode.Data.Name))
                    {
                        _editingNode.Data.Name = newName;
                    }
                }

                CancelNodeNameEdit();
            }
        }

        private void CancelNodeNameEdit()
        {
            if (_nodeNameEditor != null)
            {
                this.Controls.Remove(_nodeNameEditor);
                _nodeNameEditor.Dispose();
                _nodeNameEditor = null;
            }
            _editingNode = null;
            Invalidate();
        }

        /// <summary>
        /// 检查节点名称是否唯一
        /// </summary>
        private bool IsNodeNameUnique(string name, FlowNode excludeNode)
        {
            foreach (var node in _nodes.Values)
            {
                if (node != excludeNode && 
                    (node.Data.Name == name || node.Data.DisplayName == name))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 应用连线动画效果
        /// </summary>
        private void ApplyConnectionAnimations(Graphics g, Connections.FlowConnection connection, List<NodeAnimation> animations)
        {
            foreach (var animation in animations)
            {
                switch (animation.Type)
                {
                    case AnimationType.ConnectionFlow:
                        connection.FlowAnimationOffset = animation.FlowAnimationOffset;
                        break;
                    case AnimationType.ConnectionBuild:
                        connection.BuildProgress = animation.BuildProgress;
                        break;
                }
            }
            
            // 绘制连线（动画值已更新）
            connection.Draw(g);
        }

        /// <summary>
        /// 应用节点动画效果
        /// </summary>
        private void ApplyNodeAnimations(Graphics g, FlowNode node, List<NodeAnimation> animations)
        {
            float opacity = 1f;
            float scale = 1f;
            float rotation = 0f;

            foreach (var animation in animations)
            {
                switch (animation.Type)
                {
                    case AnimationType.FadeIn:
                    case AnimationType.FadeOut:
                        opacity = Math.Min(opacity, animation.CurrentOpacity);
                        break;
                    case AnimationType.ScalePulse:
                        scale = animation.CurrentScale;
                        break;
                    case AnimationType.Rotate:
                        rotation = animation.RotationAngle;
                        break;
                    case AnimationType.PortPulse:
                        // 端口脉冲动画在 DrawPorts 中处理
                        DrawPortPulse(g, node, animation);
                        break;
                }
            }

            // 应用变换
            var bounds = node.GetBounds();
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;

            g.TranslateTransform(centerX, centerY);
            if (rotation != 0)
            {
                g.RotateTransform(rotation);
            }
            if (scale != 1f)
            {
                g.ScaleTransform(scale, scale);
            }
            g.TranslateTransform(-centerX, -centerY);

            // 应用透明度（使用ColorMatrix）
            if (opacity < 1f)
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, opacity, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
                var imageAttributes = new System.Drawing.Imaging.ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);
                
                // 使用临时位图绘制
                var tempBitmap = new Bitmap((int)(bounds.Width + 10), (int)(bounds.Height + 10));
                using (var tempG = Graphics.FromImage(tempBitmap))
                {
                    tempG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    tempG.TranslateTransform(-bounds.X + 5, -bounds.Y + 5);
                    node.Draw(tempG);
                }
                var destRect = new Rectangle(
                    (int)(bounds.X - 5), (int)(bounds.Y - 5), 
                    (int)(bounds.Width + 10), (int)(bounds.Height + 10));
                g.DrawImage(tempBitmap, 
                    destRect,
                    0, 0, tempBitmap.Width, tempBitmap.Height, 
                    GraphicsUnit.Pixel, imageAttributes);
                tempBitmap.Dispose();
                imageAttributes.Dispose();
            }
            else
            {
                node.Draw(g);
            }

            // 重置变换
            g.ResetTransform();
        }

        /// <summary>
        /// 绘制端口脉冲动画
        /// </summary>
        private void DrawPortPulse(Graphics g, FlowNode node, NodeAnimation animation)
        {
            var bounds = node.GetBounds();
            var ports = node.OutputPorts.Concat(node.InputPorts).ToList();
            
            foreach (var port in ports)
            {
                var screenPort = WorldToScreen(port);
                var pulseRect = new RectangleF(
                    screenPort.X - animation.PulseRadius,
                    screenPort.Y - animation.PulseRadius,
                    animation.PulseRadius * 2,
                    animation.PulseRadius * 2
                );
                
                using (var brush = new SolidBrush(Color.FromArgb(
                    (int)(animation.CurrentOpacity * 255),
                    59, 130, 246)))
                {
                    g.FillEllipse(brush, pulseRect);
                }
            }
        }

        /// <summary>
        /// 处理鼠标点击事件（右键菜单）
        /// </summary>
        private void FlowDesignerCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var worldPoint = ScreenToWorld(e.Location);
                var hitNode = HitTestNode(worldPoint);
                var hitConnection = HitTestConnection(worldPoint);

                if (hitNode != null)
                {
                    // 显示节点菜单
                    SelectNode(hitNode);
                    _contextMenu.ShowNodeMenu(this, e.Location, hitNode);
                }
                else if (hitConnection != null)
                {
                    // 显示连接线菜单
                    _rightClickedConnection = hitConnection;
                    _contextMenu.ShowConnectionMenu(this, e.Location);
                }
                else
                {
                    // 显示画布菜单
                    _contextMenu.ShowCanvasMenu(this, e.Location);
                }
            }
        }

        /// <summary>
        /// 检测点击的连接线
        /// </summary>
        private FlowConnection HitTestConnection(PointF worldPoint)
        {
            return _connections.Values
                .OrderByDescending(c => c.ZIndex)
                .FirstOrDefault(c => c.Visible && c.HitTest(worldPoint));
        }

        private void ContextMenu_AddNodeRequested(object sender, FlowNodeType nodeType)
        {
            if (Toolbox == null) return;

            var worldPoint = ScreenToWorld(_contextMenu.Location);
            var node = Toolbox.CreateNode(nodeType, worldPoint);
            if (node != null)
            {
                AddNode(node);
            }
        }

        private void ContextMenu_PasteRequested(object sender, EventArgs e)
        {
            PasteNodes();
        }

        private void ContextMenu_CopyRequested(object sender, EventArgs e)
        {
            CopySelectedNodes();
        }

        private void ContextMenu_DeleteRequested(object sender, EventArgs e)
        {
            if (_selectedNodes.Count > 0)
            {
                DeleteSelectedNodes();
            }
            else if (_rightClickedConnection != null)
            {
                // 删除连接线
                var connection = _rightClickedConnection;
                RemoveConnection(connection);
                if (connection.Data != null)
                {
                    Document.Connections.Remove(connection.Data);
                }
                _rightClickedConnection = null;
            }
        }

        private void ContextMenu_PropertiesRequested(object sender, EventArgs e)
        {
            if (_selectedNodes.Count > 0)
            {
                var node = _selectedNodes.First();
                if (PropertiesPanel != null)
                {
                    PropertiesPanel.SetSelectedNode(node);
                }
            }
        }

        private void ContextMenu_SkipNodeRequested(object sender, EventArgs e)
        {
            if (_selectedNodes.Count > 0)
            {
                var node = _selectedNodes.First();
                if (node.Data != null)
                {
                    node.Data.Skip = !node.Data.Skip;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 验证流程
        /// </summary>
        public ValidationResult ValidateFlow()
        {
            LastValidationResult = FlowValidator.ValidateFlow(Document, _nodes, _connections.Values.ToList());
            
            // 更新节点的验证状态
            foreach (var node in _nodes.Values)
            {
                if (node.Data != null)
                {
                    var nodeErrors = LastValidationResult.Errors
                        .Where(e => e.NodeName == node.Data.Name)
                        .ToList();
                    
                    node.ValidationError = nodeErrors.Count > 0 
                        ? string.Join(", ", nodeErrors.Select(e => e.Message))
                        : null;
                    
                    node.Data.Valid = nodeErrors.Count == 0;
                }
            }

            Invalidate();
            return LastValidationResult;
        }

        /// <summary>
        /// 验证节点
        /// </summary>
        public void ValidateNode(FlowNode node)
        {
            if (node?.Data == null) return;

            var result = FlowValidator.ValidateNode(node.Data);
            node.ValidationError = result.Errors.Count > 0
                ? string.Join(", ", result.Errors.Select(e => e.Message))
                : null;
            node.Data.Valid = result.IsValid;
            Invalidate();
        }

        /// <summary>
        /// 验证连接（创建前检查）
        /// </summary>
        public bool CanCreateConnection(FlowNode sourceNode, FlowNode targetNode)
        {
            if (sourceNode == null || targetNode == null) return false;
            if (sourceNode == targetNode) return false;

            // 检查是否已存在连接
            var existingConnection = _connections.Values
                .FirstOrDefault(c => c.SourceNode == sourceNode && c.TargetNode == targetNode);
            if (existingConnection != null) return false;

            // 检查循环连接
            var connectionData = FlowConnectionData.Create(
                sourceNode.Data.Name,
                targetNode.Data.Name
            );
            var nodeDict = Document.Nodes.ToDictionary(n => n.Name);
            if (FlowValidator.HasCircularConnection(connectionData, nodeDict, Document.Connections))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建循环返回连接
        /// </summary>
        public FlowConnection CreateLoopReturnConnection(FlowNode loopNode, FlowNode lastNode)
        {
            if (loopNode == null || lastNode == null || 
                loopNode.Data == null || lastNode.Data == null)
                return null;

            var connectionData = FlowConnectionData.Create(
                lastNode.Data.Name,
                loopNode.Data.Name
            );
            connectionData.Type = FlowConnectionType.LoopReturnEdge;

            var connection = new FlowConnection(connectionData, lastNode, loopNode);
            connection.IsLoopReturn = true;
            AddConnection(connection);
            Document.Connections.Add(connectionData);
            return connection;
        }

        #region TransformModel集成

        /// <summary>
        /// TransformModel属性变更事件处理
        /// 当缩放或平移发生变化时，重绘画布
        /// </summary>
        private void Transform_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TransformModel.Zoom) || 
                e.PropertyName == nameof(TransformModel.Translation))
            {
                // 2.4.4 更新视口边界（虚拟滚动）
                UpdateViewportBounds();

                // 标记所有层为脏，需要重绘
                _layeredRenderer?.MarkLayerDirty(RenderLayerType.Background);
                _layeredRenderer?.MarkLayerDirty(RenderLayerType.Grid);
                _layeredRenderer?.MarkLayerDirty(RenderLayerType.Connection);
                _layeredRenderer?.MarkLayerDirty(RenderLayerType.Node);
                _layeredRenderer?.MarkLayerDirty(RenderLayerType.Selection);
                _layeredRenderer?.MarkLayerDirty(RenderLayerType.Overlay);
                
                // 触发重绘（使用防抖优化）
                InvalidateDebounced();
            }
        }

        /// <summary>
        /// 获取画布可见区域（Canvas坐标系）
        /// </summary>
        public RectangleF GetVisibleCanvasBounds()
        {
            return Transform.GetVisibleBounds(new SizeF(Width, Height));
        }

        #endregion

        #region LayeredRenderer集成

        /// <summary>
        /// 初始化分层渲染器并注册渲染回调
        /// </summary>
        private void InitializeLayeredRenderer()
        {
            // 使用BuilderStateStore单例
            var stateStore = BuilderStateStore.Instance;
            _layeredRenderer = new LayeredRenderer(stateStore);
            
            // 注册自定义渲染回调（覆盖默认实现）
            RegisterLayeredRenderCallbacks();
        }

        /// <summary>
        /// 注册分层渲染回调
        /// </summary>
        private void RegisterLayeredRenderCallbacks()
        {
            var layerManager = typeof(LayeredRenderer)
                .GetField("_layerManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_layeredRenderer) as RenderLayerManager;

            if (layerManager == null)
                return;

            // 2.1.2 背景层渲染回调
            layerManager.RegisterRenderCallback(RenderLayerType.Background, (g, viewport) =>
            {
                // 绘制背景色
                g.Clear(BackgroundColor);
            });

            // 2.1.3 网格层渲染回调
            layerManager.RegisterRenderCallback(RenderLayerType.Grid, (g, viewport) =>
            {
                if (ShowGrid)
                {
                    RenderGridLayer(g, viewport);
                }
            });

            // 2.1.4 连线层渲染回调
            layerManager.RegisterRenderCallback(RenderLayerType.Connection, (g, viewport) =>
            {
                RenderConnectionLayer(g, viewport);
            });

            // 2.1.5 节点层渲染回调
            layerManager.RegisterRenderCallback(RenderLayerType.Node, (g, viewport) =>
            {
                RenderNodeLayer(g, viewport);
            });

            // 2.1.6 选择层渲染回调
            layerManager.RegisterRenderCallback(RenderLayerType.Selection, (g, viewport) =>
            {
                RenderSelectionLayer(g, viewport);
            });

            // 2.1.7 覆盖层渲染回调（对齐线/预览）
            layerManager.RegisterRenderCallback(RenderLayerType.Overlay, (g, viewport) =>
            {
                RenderOverlayLayer(g, viewport);
            });
        }

        /// <summary>
        /// 渲染网格层
        /// </summary>
        private void RenderGridLayer(Graphics g, RectangleF viewport)
        {
            const float gridSize = 20f;
            using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1f))
            {
                // 计算网格起始位置（Canvas坐标）
                float startX = (float)(Math.Floor(viewport.Left / gridSize) * gridSize);
                float startY = (float)(Math.Floor(viewport.Top / gridSize) * gridSize);

                // 绘制垂直线
                for (float x = startX; x <= viewport.Right; x += gridSize)
                {
                    var clientStart = Transform.CanvasToClient(x, viewport.Top);
                    var clientEnd = Transform.CanvasToClient(x, viewport.Bottom);
                    g.DrawLine(pen, clientStart, clientEnd);
                }

                // 绘制水平线
                for (float y = startY; y <= viewport.Bottom; y += gridSize)
                {
                    var clientStart = Transform.CanvasToClient(viewport.Left, y);
                    var clientEnd = Transform.CanvasToClient(viewport.Right, y);
                    g.DrawLine(pen, clientStart, clientEnd);
                }
            }
        }

        /// <summary>
        /// 渲染连线层（2.4.3 实现连线可见性过滤）
        /// </summary>
        private void RenderConnectionLayer(Graphics g, RectangleF viewport)
        {
            // 应用Transform变换到Graphics
            var state = g.Save();
            g.TranslateTransform(Transform.Translation.X, Transform.Translation.Y);
            g.ScaleTransform(Transform.Zoom, Transform.Zoom);

            // 2.4.3 获取可见连线（虚拟滚动优化）
            var visibleConnections = GetVisibleConnections(viewport);

            // 绘制可见连线
            foreach (var connection in visibleConnections)
            {
                if (connection != null && connection.Visible)
                {
                    connection.Draw(g);
                }
            }

            g.Restore(state);
        }

        /// <summary>
        /// 渲染节点层（2.4.2 实现节点可见性过滤）
        /// </summary>
        private void RenderNodeLayer(Graphics g, RectangleF viewport)
        {
            // 应用Transform变换到Graphics
            var state = g.Save();
            g.TranslateTransform(Transform.Translation.X, Transform.Translation.Y);
            g.ScaleTransform(Transform.Zoom, Transform.Zoom);

            // 2.4.2 获取可见节点（虚拟滚动优化）
            var visibleNodes = GetVisibleNodes(viewport);
            
            // 按ZIndex排序绘制节点
            var sortedNodes = visibleNodes.OrderBy(n => n.ZIndex).ToList();
            
            foreach (var node in sortedNodes)
            {
                if (node != null && node.Visible)
                {
                    // 绘制节点
                    node.Draw(g);
                }
            }

            g.Restore(state);
        }

        /// <summary>
        /// 渲染选择层
        /// </summary>
        private void RenderSelectionLayer(Graphics g, RectangleF viewport)
        {
            // 应用Transform变换到Graphics
            var state = g.Save();
            g.TranslateTransform(Transform.Translation.X, Transform.Translation.Y);
            g.ScaleTransform(Transform.Zoom, Transform.Zoom);

            // 绘制选中节点的高亮边框
            if (_selectedNodes.Count > 0)
            {
                using (var pen = new Pen(Color.FromArgb(59, 130, 246), 2f / Transform.Zoom))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    
                    foreach (var node in _selectedNodes)
                    {
                        if (node != null && node.Visible)
                        {
                            var bounds = node.GetBounds();
                            g.DrawRectangle(pen, bounds.X - 2, bounds.Y - 2, 
                                bounds.Width + 4, bounds.Height + 4);
                        }
                    }
                }
            }

            // 绘制框选矩形
            if (_isSelecting)
            {
                using (var pen = new Pen(Color.FromArgb(100, 59, 130, 246), 1f / Transform.Zoom))
                using (var brush = new SolidBrush(Color.FromArgb(30, 59, 130, 246)))
                {
                    var selRect = GetNormalizedRectangle(_selectionStart, _selectionEnd);
                    g.FillRectangle(brush, selRect);
                    g.DrawRectangle(pen, selRect.X, selRect.Y, selRect.Width, selRect.Height);
                }
            }

            g.Restore(state);
        }

        /// <summary>
        /// 渲染覆盖层（对齐线、拖拽预览等）
        /// </summary>
        private void RenderOverlayLayer(Graphics g, RectangleF viewport)
        {
            // 应用Transform变换到Graphics
            var state = g.Save();
            g.TranslateTransform(Transform.Translation.X, Transform.Translation.Y);
            g.ScaleTransform(Transform.Zoom, Transform.Zoom);

            // 绘制拖拽预览
            if (_draggingNode != null && _dragPreviewPosition != PointF.Empty)
            {
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.LightBlue)))
                using (var pen = new Pen(Color.FromArgb(150, Color.Blue), 1f / Transform.Zoom))
                {
                    var bounds = _draggingNode.GetBounds();
                    var previewRect = new RectangleF(
                        _dragPreviewPosition.X,
                        _dragPreviewPosition.Y,
                        bounds.Width,
                        bounds.Height);
                    g.FillRectangle(brush, previewRect);
                    g.DrawRectangle(pen, previewRect.X, previewRect.Y, 
                        previewRect.Width, previewRect.Height);
                }
            }

            // 3.3.3 / 3.3.4 绘制连线预览（虚线预连线+端口高亮）
            if (_isCreatingConnection && _connectionSourceNode != null)
            {
                var sourceBounds = _connectionSourceNode.GetBounds();
                var sourceCenter = new PointF(
                    sourceBounds.X + sourceBounds.Width / 2,
                    sourceBounds.Y + sourceBounds.Height / 2);

                var previewEnd = Transform.ClientToCanvas(_connectionPreviewEnd);

                // 根据是否可以连接选择颜色
                Color previewColor = Color.Gray;
                if (_potentialTargetNode != null)
                {
                    bool canConnect = ValidateConnection(_connectionSourceNode, _potentialTargetNode);
                    previewColor = canConnect ? Color.FromArgb(59, 130, 246) : Color.FromArgb(239, 68, 68);
                }

                // 3.3.3 绘制虚线预连线
                using (var pen = new Pen(Color.FromArgb(150, previewColor), 2f / Transform.Zoom))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawLine(pen, sourceCenter, previewEnd);
                }

                // 3.3.4 绘制端口高亮动画
                if (_hoveredAnchor != null && _potentialTargetNode != null)
                {
                    var nodeBounds = _potentialTargetNode.GetBounds();
                    var nodeCenter = new PointF(
                        nodeBounds.X + nodeBounds.Width / 2,
                        nodeBounds.Y + nodeBounds.Height / 2
                    );
                    var anchorPos = new PointF(
                        nodeCenter.X + _hoveredAnchor.RelativeX * nodeBounds.Width,
                        nodeCenter.Y + _hoveredAnchor.RelativeY * nodeBounds.Height
                    );

                    // 绘制脉冲效果的锚点高亮
                    using (var brush = new SolidBrush(Color.FromArgb(100, 59, 130, 246)))
                    using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 2f / Transform.Zoom))
                    {
                        float radius = 8f / Transform.Zoom;
                        g.FillEllipse(brush, anchorPos.X - radius, anchorPos.Y - radius, radius * 2, radius * 2);
                        g.DrawEllipse(pen, anchorPos.X - radius, anchorPos.Y - radius, radius * 2, radius * 2);
                    }
                }
            }

            // 3.4.5 绘制对齐线（视觉渲染）
            if (_currentAlignmentGuides != null && _currentAlignmentGuides.Count > 0)
            {
                using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 1f / Transform.Zoom))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    
                    foreach (var guide in _currentAlignmentGuides)
                    {
                        g.DrawLine(pen, guide.Start, guide.End);
                    }
                }
            }

            g.Restore(state);
        }

        /// <summary>
        /// 标记特定层需要重绘
        /// </summary>
        public void MarkLayerDirty(RenderLayerType layer)
        {
            _layeredRenderer?.MarkLayerDirty(layer);
            Invalidate();
        }

        /// <summary>
        /// 获取标准化矩形（确保宽高为正）
        /// </summary>
        private RectangleF GetNormalizedRectangle(PointF start, PointF end)
        {
            float x = Math.Min(start.X, end.X);
            float y = Math.Min(start.Y, end.Y);
            float width = Math.Abs(end.X - start.X);
            float height = Math.Abs(end.Y - start.Y);
            return new RectangleF(x, y, width, height);
        }

        #endregion

        #region FlowGraphBuilder集成

        /// <summary>
        /// 从FlowVersion构建并加载FlowGraph到画布
        /// 2.2.4 实现图数据到画布对象的映射
        /// </summary>
        public void LoadFlowVersion(FlowVersion flowVersion)
        {
            if (flowVersion == null)
                return;

            // 清空当前画布
            ClearCanvas();

            // 使用FlowGraphBuilder构建FlowGraph
            _currentGraph = _graphBuilder.BuildGraph(flowVersion);
            
            if (_currentGraph == null)
                return;

            // 2.2.4 将FlowGraph的节点映射到画布对象
            MapGraphNodesToCanvas(_currentGraph);

            // 2.2.4 将FlowGraph的边缘映射到画布对象  
            MapGraphEdgesToCanvas(_currentGraph);

            // 将图传递给LayeredRenderer
            _layeredRenderer?.SetGraph(_currentGraph);

            // 2.2.5 触发画布刷新
            RefreshCanvas();
        }

        /// <summary>
        /// 将FlowGraph的节点映射到画布FlowNode对象
        /// </summary>
        private void MapGraphNodesToCanvas(FlowGraph graph)
        {
            foreach (var canvasNode in graph.Nodes)
            {
                // 根据Canvas节点类型创建对应的FlowNode
                FlowNode flowNode = null;

                if (canvasNode is StepNode stepNode)
                {
                    // 从StepNode创建FlowNode
                    flowNode = CreateFlowNodeFromStepNode(stepNode);
                }
                else if (canvasNode is AddButtonNode || canvasNode is BigAddButtonNode)
                {
                    // 添加按钮节点，暂时跳过
                    // TODO: 未来可以实现特殊的按钮节点类型
                    continue;
                }
                else if (canvasNode is GraphEndNode)
                {
                    // 结束节点，暂时跳过或创建虚拟节点
                    continue;
                }

                if (flowNode != null)
                {
                    // 设置节点位置
                    flowNode.X = canvasNode.Position.X;
                    flowNode.Y = canvasNode.Position.Y;

                    // 添加到画布
                    AddNode(flowNode);
                }
            }
        }

        /// <summary>
        /// 从StepNode创建FlowNode
        /// </summary>
        private FlowNode CreateFlowNodeFromStepNode(StepNode stepNode)
        {
            if (stepNode.Step == null)
                return null;

            // 根据Step类型创建对应的FlowNode
            var nodeData = new FlowNodeData
            {
                Name = stepNode.Step.Name,
                DisplayName = stepNode.Step.DisplayName ?? stepNode.Step.Name,
                Type = GetNodeTypeFromStep(stepNode.Step),
                Position = stepNode.Position,
                Valid = stepNode.Step.Valid,
                Settings = new Dictionary<string, object>()
            };

            // 创建FlowNode实例
            FlowNode flowNode = null;
            switch (nodeData.Type)
            {
                case FlowNodeType.Start:
                    flowNode = new StartNode(nodeData);
                    break;
                case FlowNodeType.Process:
                    flowNode = new ProcessNode(nodeData);
                    break;
                case FlowNodeType.Decision:
                    flowNode = new DecisionNode(nodeData);
                    break;
                case FlowNodeType.Loop:
                    flowNode = new LoopNode(nodeData);
                    break;
                case FlowNodeType.End:
                    flowNode = new EndNode(nodeData);
                    break;
                default:
                    flowNode = new ProcessNode(nodeData);
                    break;
            }

            return flowNode;
        }

        /// <summary>
        /// 从IStep获取FlowNodeType
        /// </summary>
        private FlowNodeType GetNodeTypeFromStep(IStep step)
        {
            if (step is FlowTrigger)
                return FlowNodeType.Start;
            else if (step is LoopOnItemsAction)
                return FlowNodeType.Loop;
            else if (step is RouterAction)
                return FlowNodeType.Decision;
            else if (step is FlowAction)
                return FlowNodeType.Process;
            else
                return FlowNodeType.Process;
        }

        /// <summary>
        /// 将FlowGraph的边缘映射到画布FlowConnection对象
        /// </summary>
        private void MapGraphEdgesToCanvas(FlowGraph graph)
        {
            foreach (var canvasEdge in graph.Edges)
            {
                // 检查源节点和目标节点是否存在
                if (!_nodes.ContainsKey(canvasEdge.SourceId) || 
                    !_nodes.ContainsKey(canvasEdge.TargetId))
                {
                    // 节点不存在，跳过该边缘
                    continue;
                }

                var sourceNode = _nodes[canvasEdge.SourceId];
                var targetNode = _nodes[canvasEdge.TargetId];

                // 创建连接数据
                var connectionData = FlowConnectionData.Create(
                    canvasEdge.SourceId,
                    canvasEdge.TargetId,
                    type: GetConnectionTypeFromEdge(canvasEdge)
                );

                // 创建连接对象
                var connection = new Connections.FlowConnection(connectionData, sourceNode, targetNode);

                // 添加到画布
                AddConnection(connection);
            }
        }

        /// <summary>
        /// 从ICanvasEdge获取FlowConnectionType
        /// </summary>
        private FlowConnectionType GetConnectionTypeFromEdge(ICanvasEdge edge)
        {
            if (edge is LoopStartEdge)
                return FlowConnectionType.LoopStartEdge;
            else if (edge is LoopReturnEdge)
                return FlowConnectionType.LoopReturnEdge;
            else if (edge is RouterStartEdge)
                return FlowConnectionType.RouterStartEdge;
            else if (edge is RouterEndEdge)
                return FlowConnectionType.RouterEndEdge;
            else
                return FlowConnectionType.StraightLine;
        }

        /// <summary>
        /// 清空画布
        /// </summary>
        private void ClearCanvas()
        {
            // 清空节点
            _nodes.Clear();
            
            // 清空连接
            _connections.Clear();

            // 清空选择
            _selectedNodes.Clear();
        }

        /// <summary>
        /// 刷新画布 - 2.2.5 实现画布刷新触发机制
        /// </summary>
        public void RefreshCanvas()
        {
            // 标记所有层为脏，需要重绘
            _layeredRenderer?.MarkLayerDirty(RenderLayerType.Background);
            _layeredRenderer?.MarkLayerDirty(RenderLayerType.Grid);
            _layeredRenderer?.MarkLayerDirty(RenderLayerType.Connection);
            _layeredRenderer?.MarkLayerDirty(RenderLayerType.Node);
            _layeredRenderer?.MarkLayerDirty(RenderLayerType.Selection);
            _layeredRenderer?.MarkLayerDirty(RenderLayerType.Overlay);

            // 触发重绘（使用防抖优化）
            InvalidateDebounced();

            // 记录性能
            PerformanceMonitor.Instance.RecordFrame();
        }

        #endregion

        #region 渐进连线（Phase 3.3）

        /// <summary>
        /// 锚点感应距离阈值（像素）
        /// </summary>
        private const float AnchorSnapThreshold = 20f;

        /// <summary>
        /// 当前悬停的锚点
        /// </summary>
        private AnchorPoint _hoveredAnchor;

        /// <summary>
        /// 当前的对齐辅助线
        /// </summary>
        private List<NodeAlignmentHelper.AlignmentGuide> _currentAlignmentGuides;

        /// <summary>
        /// 3.3.1 / 3.3.2 检测最近的锚点（实时距离计算和阈值检测）
        /// </summary>
        private AnchorPoint FindNearestAnchor(PointF worldPoint, FlowNode targetNode)
        {
            if (targetNode == null || targetNode.Data == null)
                return null;

            // 获取目标节点的锚点列表
            var anchors = targetNode.Data.GetAnchors();
            if (anchors == null || anchors.Count == 0)
                return null;

            AnchorPoint nearestAnchor = null;
            float minDistance = float.MaxValue;

            var nodeBounds = targetNode.GetBounds();
            var nodeCenter = new PointF(
                nodeBounds.X + nodeBounds.Width / 2,
                nodeBounds.Y + nodeBounds.Height / 2
            );

            foreach (var anchor in anchors)
            {
                // 只检测输入锚点
                if (anchor.Direction != AnchorDirection.Input)
                    continue;

                // 计算锚点的世界坐标
                var anchorWorldPos = new PointF(
                    nodeCenter.X + anchor.RelativeX * nodeBounds.Width,
                    nodeCenter.Y + anchor.RelativeY * nodeBounds.Height
                );

                // 3.3.1 计算距离
                float distance = CalculateDistance(worldPoint, anchorWorldPos);

                // 3.3.2 检查是否在阈值内
                if (distance < AnchorSnapThreshold && distance < minDistance)
                {
                    minDistance = distance;
                    nearestAnchor = anchor;
                }
            }

            return nearestAnchor;
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        private float CalculateDistance(PointF p1, PointF p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 3.3.5 验证连接规则（实时校验）
        /// </summary>
        private bool ValidateConnection(FlowNode sourceNode, FlowNode targetNode)
        {
            if (sourceNode == null || targetNode == null)
                return false;

            // 不能连接到自己
            if (sourceNode == targetNode)
                return false;

            // 使用FlowNodeData的CanConnectTo方法进行规则校验
            if (sourceNode.Data != null && targetNode.Data != null)
            {
                if (!sourceNode.Data.CanConnectTo(targetNode.Data))
                    return false;
            }

            // 3.3.6 去重逻辑：检查是否已存在相同的连接
            if (_connections.Values.Any(c => 
                c.SourceNode == sourceNode && 
                c.TargetNode == targetNode))
            {
                return false; // 已存在相同连接
            }

            // 检查循环连接（防止死循环）
            if (WouldCreateCycle(sourceNode, targetNode))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查连接是否会创建循环
        /// </summary>
        private bool WouldCreateCycle(FlowNode sourceNode, FlowNode targetNode)
        {
            // 使用BFS检查从targetNode是否能到达sourceNode
            var visited = new HashSet<string>();
            var queue = new Queue<FlowNode>();
            queue.Enqueue(targetNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current.Data?.Name))
                    continue;

                visited.Add(current.Data?.Name);

                // 如果到达了sourceNode，说明会形成循环
                if (current == sourceNode)
                    return true;

                // 查找所有从current出发的连接
                var outgoingConnections = _connections.Values
                    .Where(c => c.SourceNode == current);

                foreach (var conn in outgoingConnections)
                {
                    if (conn.TargetNode != null)
                    {
                        queue.Enqueue(conn.TargetNode);
                    }
                }
            }

            return false;
        }

        #endregion

        #region SelectionManager集成（Phase 3.1）

        /// <summary>
        /// 获取FlowNode对应的ICanvasNode
        /// </summary>
        private ICanvasNode GetCanvasNodeFromFlowNode(FlowNode flowNode)
        {
            if (_currentGraph == null || flowNode == null)
                return null;

            // 查找匹配的ICanvasNode
            return _currentGraph.Nodes.FirstOrDefault(n => n.Id == flowNode.Data?.Name);
        }

        /// <summary>
        /// 3.1.7 同步选择状态（从SelectionManager到_selectedNodes）
        /// </summary>
        private void SyncSelectionState()
        {
            _selectedNodes.Clear();
            
            var stateStore = BuilderStateStore.Instance;
            if (stateStore?.Selection?.SelectedNodeIds != null)
            {
                foreach (var nodeId in stateStore.Selection.SelectedNodeIds)
                {
                    if (_nodes.TryGetValue(nodeId, out var node))
                    {
                        _selectedNodes.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// 3.1.6 范围选择（从第一个选中节点到当前节点）
        /// </summary>
        private void SelectRange(FlowNode targetNode)
        {
            if (_selectedNodes.Count == 0 || targetNode == null)
                return;

            var firstSelected = _selectedNodes.First();
            var firstBounds = firstSelected.GetBounds();
            var targetBounds = targetNode.GetBounds();

            // 计算包含两个节点的矩形
            var minX = Math.Min(firstBounds.Left, targetBounds.Left);
            var minY = Math.Min(firstBounds.Top, targetBounds.Top);
            var maxX = Math.Max(firstBounds.Right, targetBounds.Right);
            var maxY = Math.Max(firstBounds.Bottom, targetBounds.Bottom);

            var selectionRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);

            // 选择矩形内的所有节点
            foreach (var node in _nodes.Values)
            {
                var bounds = node.GetBounds();
                if (selectionRect.IntersectsWith(bounds) && !_selectedNodes.Contains(node))
                {
                    _selectedNodes.Add(node);
                }
            }

            // 更新状态存储
            var stateStore = BuilderStateStore.Instance;
            if (stateStore != null)
            {
                stateStore.Selection.SelectedNodeIds.Clear();
                stateStore.Selection.SelectedNodeIds.AddRange(_selectedNodes.Select(n => n.Data?.Name).Where(n => n != null));
                stateStore.SetSelectedNodes(stateStore.Selection.SelectedNodeIds.ToArray());
            }

            // 标记选择层需要重绘
            MarkLayerDirty(RenderLayerType.Selection);
        }

        #endregion

        #region 虚拟滚动（Phase 2.4）

        /// <summary>
        /// 2.4.1 / 2.4.2 获取可见节点（虚拟滚动优化）
        /// 仅返回在视口范围内的节点
        /// </summary>
        private IEnumerable<FlowNode> GetVisibleNodes(RectangleF viewport)
        {
            // 扩展视口边界（预加载边缘节点，避免滚动时闪烁）
            const float padding = 100f;
            var expandedViewport = new RectangleF(
                viewport.X - padding,
                viewport.Y - padding,
                viewport.Width + padding * 2,
                viewport.Height + padding * 2
            );

            // 过滤可见节点
            foreach (var node in _nodes.Values)
            {
                if (node == null || !node.Visible)
                    continue;

                var bounds = node.GetBounds();
                
                // 检查节点是否在扩展视口内
                if (expandedViewport.IntersectsWith(bounds))
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// 2.4.1 / 2.4.3 获取可见连线（虚拟滚动优化）
        /// 仅返回在视口范围内的连线
        /// </summary>
        private IEnumerable<Connections.FlowConnection> GetVisibleConnections(RectangleF viewport)
        {
            // 扩展视口边界
            const float padding = 100f;
            var expandedViewport = new RectangleF(
                viewport.X - padding,
                viewport.Y - padding,
                viewport.Width + padding * 2,
                viewport.Height + padding * 2
            );

            // 过滤可见连线
            foreach (var connection in _connections.Values)
            {
                if (connection == null || !connection.Visible)
                    continue;

                // 检查连线的两个端点是否在视口内
                if (connection.SourceNode != null && connection.TargetNode != null)
                {
                    var sourceBounds = connection.SourceNode.GetBounds();
                    var targetBounds = connection.TargetNode.GetBounds();

                    // 如果任意一个端点在视口内，就渲染这条连线
                    if (expandedViewport.IntersectsWith(sourceBounds) || 
                        expandedViewport.IntersectsWith(targetBounds))
                    {
                        yield return connection;
                    }
                }
            }
        }

        /// <summary>
        /// 2.4.4 监听视口变化（滚动、缩放、平移）
        /// 当视口变化时，更新GraphModel的ViewportBounds
        /// </summary>
        private void UpdateViewportBounds()
        {
            var viewport = GetVisibleCanvasBounds();
            
            // 更新GraphModel的视口边界（用于虚拟滚动）
            var stateStore = BuilderStateStore.Instance;
            if (stateStore?.Graph != null)
            {
                stateStore.Graph.ViewportBounds = viewport;
            }
        }

        /// <summary>
        /// 2.4.5 获取虚拟滚动统计信息（性能监控）
        /// </summary>
        public VirtualScrollStats GetVirtualScrollStats()
        {
            var viewport = GetVisibleCanvasBounds();
            var visibleNodes = GetVisibleNodes(viewport).Count();
            var visibleConnections = GetVisibleConnections(viewport).Count();

            return new VirtualScrollStats
            {
                TotalNodes = _nodes.Count,
                VisibleNodes = visibleNodes,
                TotalConnections = _connections.Count,
                VisibleConnections = visibleConnections,
                CullingRatio = _nodes.Count > 0 ? (1.0f - (float)visibleNodes / _nodes.Count) : 0f
            };
        }

        /// <summary>
        /// 虚拟滚动统计信息（2.4.5 性能监控）
        /// </summary>
        public class VirtualScrollStats
        {
            public int TotalNodes { get; set; }
            public int VisibleNodes { get; set; }
            public int TotalConnections { get; set; }
            public int VisibleConnections { get; set; }
            public float CullingRatio { get; set; } // 裁剪比例（0-1，越大表示优化效果越好）
        }

        #endregion

        #region 脏区域管理（Phase 2.3）

        /// <summary>
        /// 2.3.4 初始化Invalidate防抖定时器
        /// 优化Invalidate调用频率，避免过度重绘
        /// </summary>
        private void InitializeInvalidateDebounce()
        {
            _invalidateDebounceTimer = new System.Windows.Forms.Timer();
            _invalidateDebounceTimer.Interval = 16; // ~60 FPS (16ms)
            _invalidateDebounceTimer.Tick += (s, e) =>
            {
                _invalidateDebounceTimer.Stop();
                if (_hasPendingInvalidate)
                {
                    _hasPendingInvalidate = false;
                    Invalidate();
                }
            };
        }

        /// <summary>
        /// 2.3.4 使用防抖的Invalidate（优化调用频率）
        /// </summary>
        private void InvalidateDebounced()
        {
            _hasPendingInvalidate = true;
            
            if (!_invalidateDebounceTimer.Enabled)
            {
                _invalidateDebounceTimer.Start();
            }
        }

        /// <summary>
        /// 2.3.2 标记节点区域为脏（节点移动时调用）
        /// </summary>
        public void MarkNodeDirty(FlowNode node)
        {
            if (node == null)
                return;

            var bounds = node.GetBounds();
            
            // 转换为Client坐标
            var clientBounds = Transform.CanvasRectToClient(bounds);

            // 标记节点层和连线层的脏区域（节点移动会影响连线）
            var layerManager = GetLayerManager();
            layerManager?.MarkRegionDirty(RenderLayerType.Node, clientBounds);
            layerManager?.MarkRegionDirty(RenderLayerType.Connection, clientBounds);
            layerManager?.MarkRegionDirty(RenderLayerType.Selection, clientBounds);

            // 触发重绘
            InvalidateDebounced();
        }

        /// <summary>
        /// 2.3.3 标记连线区域为脏（连线更新时调用）
        /// </summary>
        public void MarkConnectionDirty(Connections.FlowConnection connection)
        {
            if (connection == null)
                return;

            // 计算连线的边界矩形
            var bounds = CalculateConnectionBounds(connection);
            
            // 转换为Client坐标
            var clientBounds = Transform.CanvasRectToClient(bounds);

            // 标记连线层的脏区域
            var layerManager = GetLayerManager();
            layerManager?.MarkRegionDirty(RenderLayerType.Connection, clientBounds);

            // 触发重绘
            InvalidateDebounced();
        }

        /// <summary>
        /// 计算连线的边界矩形
        /// </summary>
        private RectangleF CalculateConnectionBounds(Connections.FlowConnection connection)
        {
            if (connection.SourceNode == null || connection.TargetNode == null)
                return RectangleF.Empty;

            var sourceBounds = connection.SourceNode.GetBounds();
            var targetBounds = connection.TargetNode.GetBounds();

            // 计算包含源和目标的矩形
            float minX = Math.Min(sourceBounds.Left, targetBounds.Left);
            float minY = Math.Min(sourceBounds.Top, targetBounds.Top);
            float maxX = Math.Max(sourceBounds.Right, targetBounds.Right);
            float maxY = Math.Max(sourceBounds.Bottom, targetBounds.Bottom);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 获取LayerManager实例（通过反射访问private字段）
        /// </summary>
        private RenderLayerManager GetLayerManager()
        {
            if (_layeredRenderer == null)
                return null;

            var field = typeof(LayeredRenderer).GetField("_layerManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return field?.GetValue(_layeredRenderer) as RenderLayerManager;
        }

        /// <summary>
        /// 2.3.5 获取渲染性能统计（用于性能监控）
        /// </summary>
        public RenderPerformanceStats GetRenderPerformanceStats()
        {
            var layerManager = GetLayerManager();
            if (layerManager == null)
                return null;

            return new RenderPerformanceStats
            {
                DirtyLayerCount = layerManager.GetDirtyLayerCount(),
                NodeLayerDirtyRegions = layerManager.GetDirtyRegionCount(RenderLayerType.Node),
                ConnectionLayerDirtyRegions = layerManager.GetDirtyRegionCount(RenderLayerType.Connection),
                CurrentFPS = PerformanceMonitor.Instance.GetCurrentFPS(),
                AverageFPS = PerformanceMonitor.Instance.GetAverageFPS(),
                IsPerformanceGood = PerformanceMonitor.Instance.IsPerformanceGood()
            };
        }

        /// <summary>
        /// 渲染性能统计（2.3.5 性能监控指标）
        /// </summary>
        public class RenderPerformanceStats
        {
            public int DirtyLayerCount { get; set; }
            public int NodeLayerDirtyRegions { get; set; }
            public int ConnectionLayerDirtyRegions { get; set; }
            public float CurrentFPS { get; set; }
            public float AverageFPS { get; set; }
            public bool IsPerformanceGood { get; set; }
        }

        #endregion
    }
}
