using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Connections;
using WindFromCanvas.Core.Applications.FlowDesigner.Commands;
using WindFromCanvas.Core.Applications.FlowDesigner.Clipboard;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;
using WindFromCanvas.Core.Applications.FlowDesigner.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.Validation;
using WindFromCanvas.Core.Events;

namespace WindFromCanvas.Core.Applications.FlowDesigner
{
    /// <summary>
    /// 流程设计器画布主控件
    /// </summary>
    public class FlowDesignerCanvas : Canvas
    {
        /// <summary>
        /// 流程文档
        /// </summary>
        public FlowDocument Document { get; set; }

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
        /// 移除节点
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

                    _draggingNode = hitNode;
                    _dragPreviewPosition = e.Location;
                    
                    // 如果当前节点已选中，检查是否多选拖拽
                    if (_selectedNodes.Contains(hitNode) && _selectedNodes.Count > 1)
                    {
                        _draggingNodes = new List<FlowNode>(_selectedNodes);
                    }
                    else
                    {
                        _draggingNodes.Clear();
                        _draggingNodes.Add(hitNode);
                    }
                }
                else
                {
                    // 开始框选
                    _isSelecting = true;
                    _selectionStart = e.Location;
                    _selectionEnd = e.Location;
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
                Invalidate();
            }
            else if (_isSelecting)
            {
                _selectionEnd = e.Location;
                Invalidate();
            }
            else if (_isCreatingConnection && _connectionSourceNode != null)
            {
                _connectionPreviewEnd = e.Location;
                Invalidate();
            }
            else if (_draggingNode != null)
            {
                // 更新拖拽预览位置
                _dragPreviewPosition = e.Location;
                Invalidate();
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
                    // 完成框选
                    SelectNodesInRectangle();
                    _isSelecting = false;
                    Invalidate();
                }
                else if (_isCreatingConnection)
                {
                    // 完成连接创建
                    var worldPoint = ScreenToWorld(e.Location);
                    var targetNode = HitTestNode(worldPoint);
                    if (targetNode != null && targetNode != _connectionSourceNode)
                    {
                        CreateConnection(_connectionSourceNode, targetNode);
                    }
                    _isCreatingConnection = false;
                    _connectionSourceNode = null;
                    Invalidate();
                }

                _draggingNode = null;
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
            g.Clear(BackgroundColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 绘制网格（在变换之前）
            if (ShowGrid)
            {
                DrawGrid(g);
            }

            // 应用变换
            g.TranslateTransform(PanOffset.X, PanOffset.Y);
            g.ScaleTransform(ZoomFactor, ZoomFactor);

            // 计算视口（世界坐标）
            var viewport = new RectangleF(
                -PanOffset.X / ZoomFactor,
                -PanOffset.Y / ZoomFactor,
                this.Width / ZoomFactor,
                this.Height / ZoomFactor
            );

            // 绘制所有对象（节点和连接）
            foreach (var obj in Objects.OrderBy(o => o.ZIndex))
            {
                if (obj.Visible)
                {
                    // 视口裁剪：只渲染可见区域的对象
                    if (EnableViewportCulling)
                    {
                        var bounds = obj.GetBounds();
                        if (!viewport.IntersectsWith(bounds))
                        {
                            continue; // 跳过不可见的对象
                        }
                    }

                    obj.Draw(g);
                }
            }

            // 重置变换
            g.ResetTransform();

            // 绘制框选矩形（优化视觉，参考Activepieces）
            if (_isSelecting)
            {
                var rect = new RectangleF(
                    Math.Min(_selectionStart.X, _selectionEnd.X),
                    Math.Min(_selectionStart.Y, _selectionEnd.Y),
                    Math.Abs(_selectionEnd.X - _selectionStart.X),
                    Math.Abs(_selectionEnd.Y - _selectionStart.Y)
                );

                // 半透明蓝色背景（Activepieces风格）
                using (var brush = new SolidBrush(Color.FromArgb(30, 59, 130, 246)))
                {
                    g.FillRectangle(brush, rect);
                }
                
                // 高亮候选节点
                var worldRect = new RectangleF(
                    ScreenToWorld(new PointF(rect.X, rect.Y)),
                    new SizeF(rect.Width / ZoomFactor, rect.Height / ZoomFactor)
                );
                
                foreach (var node in _nodes.Values)
                {
                    var bounds = node.GetBounds();
                    if (worldRect.IntersectsWith(bounds) || worldRect.Contains(bounds))
                    {
                        // 绘制节点高亮边框
                        var screenBounds = new RectangleF(
                            bounds.X * ZoomFactor + PanOffset.X,
                            bounds.Y * ZoomFactor + PanOffset.Y,
                            bounds.Width * ZoomFactor,
                            bounds.Height * ZoomFactor
                        );
                        using (var pen = new Pen(Color.FromArgb(100, 59, 130, 246), 2f))
                        {
                            g.DrawRectangle(pen, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);
                        }
                    }
                }

                // 绘制框选边框
                using (var pen = new Pen(Color.FromArgb(59, 130, 246), 1.5f))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }

            // 绘制拖拽预览（半透明预览层）
            if (_draggingNode != null && _draggingNodes.Count > 0)
            {
                DrawDragPreview(g, _draggingNodes, _dragPreviewPosition);
            }

            // 绘制连接预览线
            if (_isCreatingConnection && _connectionSourceNode != null)
            {
                var startPoint = new PointF(
                    (_connectionSourceNode.X + _connectionSourceNode.Width) * ZoomFactor + PanOffset.X,
                    (_connectionSourceNode.Y + _connectionSourceNode.Height / 2) * ZoomFactor + PanOffset.Y
                );

                using (var pen = new Pen(Color.Gray, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawLine(pen, startPoint, _connectionPreviewEnd);
                }
            }

            // 绘制对齐辅助线
            if (ShowAlignmentGuides && _alignmentGuides.Count > 0)
            {
                g.TranslateTransform(PanOffset.X, PanOffset.Y);
                g.ScaleTransform(ZoomFactor, ZoomFactor);

                using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    foreach (var guide in _alignmentGuides)
                    {
                        g.DrawRectangle(pen, guide.X, guide.Y, guide.Width, guide.Height);
                    }
                }

                g.ResetTransform();
            }
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
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                if (_selectedNodes.Count > 0)
                {
                    DeleteSelectedNodes();
                    e.Handled = true;
                }
            }
            else if (e.Control && e.KeyCode == Keys.Z)
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
            else if (e.Control && e.KeyCode == Keys.C)
            {
                // Ctrl+C 复制
                CopySelectedNodes();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                // Ctrl+V 粘贴
                PasteNodes();
                e.Handled = true;
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
            FlowSerializer.SaveToFile(Document, filePath);
        }

        /// <summary>
        /// 从文件加载流程
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            var document = FlowSerializer.LoadFromFile(filePath);
            if (document != null)
            {
                LoadDocument(document);
            }
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
    }
}
