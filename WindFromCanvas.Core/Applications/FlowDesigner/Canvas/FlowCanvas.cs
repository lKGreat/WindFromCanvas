using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges;
using WindFromCanvas.Core.Applications.FlowDesigner.Interaction;
using WindFromCanvas.Core.Applications.FlowDesigner.DragDrop;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas
{
    /// <summary>
    /// 流程画布控件（主画布，匹配 Activepieces FlowCanvas）
    /// </summary>
    public class FlowCanvas : Control
    {
        private CanvasViewport _viewport;
        private BuilderStateStore _stateStore;
        private FlowGraphBuilder _graphBuilder;
        private FlowGraph _currentGraph;
        private SelectionManager _selectionManager;
        private DragDropManager _dragDropManager;
        private bool _isPanning;
        private PointF _lastPanPoint;
        private bool _isInitialized;
        private bool _isDragging;
        private bool _isSelecting;
        private ICanvasNode _draggedNode;

        public FlowCanvas()
        {
            _viewport = new CanvasViewport();
            _stateStore = BuilderStateStore.Instance;
            _graphBuilder = new FlowGraphBuilder();
            _currentGraph = null;
            _selectionManager = new SelectionManager(_stateStore);
            _dragDropManager = new DragDropManager(_stateStore);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            
            BackColor = Color.FromArgb(250, 250, 250);
            _isPanning = false;
            _isInitialized = false;
            _isDragging = false;
            _isSelecting = false;
            
            // 订阅状态变化，重建图
            _stateStore.PropertyChanged += StateStore_PropertyChanged;
            
            // 订阅拖拽事件
            _dragDropManager.DragStarted += DragDropManager_DragStarted;
            _dragDropManager.DragUpdated += DragDropManager_DragUpdated;
            _dragDropManager.DragEnded += DragDropManager_DragEnded;
        }
        
        private void DragDropManager_DragStarted(object sender, DragContext e)
        {
            Invalidate();
        }
        
        private void DragDropManager_DragUpdated(object sender, DragContext e)
        {
            Invalidate();
        }
        
        private void DragDropManager_DragEnded(object sender, DragContext e)
        {
            _draggedNode = null;
            Invalidate();
        }
        
        private void StateStore_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BuilderStateStore.Flow) || 
                e.PropertyName == "Flow.FlowVersion")
            {
                RebuildGraph();
            }
        }
        
        /// <summary>
        /// 重建流程图
        /// </summary>
        private void RebuildGraph()
        {
            if (_stateStore?.Flow?.FlowVersion != null)
            {
                _currentGraph = _graphBuilder.BuildGraph(_stateStore.Flow.FlowVersion);
                RegisterDropTargets();
                Invalidate();
            }
        }

        /// <summary>
        /// 注册放置目标
        /// </summary>
        private void RegisterDropTargets()
        {
            if (_currentGraph == null) return;

            // 清除现有目标
            foreach (var target in _dragDropManager.GetDropTargets())
            {
                _dragDropManager.UnregisterDropTarget(target);
            }

            // 注册添加按钮作为放置目标
            foreach (var node in _currentGraph.Nodes)
            {
                if (node is AddButtonNode addButton)
                {
                    var dropTarget = new DragDrop.DropTarget(
                        addButton.Id,
                        addButton.ParentStepName,
                        addButton.StepLocationRelativeToParent,
                        addButton.BranchIndex
                    );
                    _dragDropManager.RegisterDropTarget(dropTarget);
                }
                else if (node is BigAddButtonNode bigAddButton)
                {
                    var dropTarget = new DragDrop.DropTarget(
                        bigAddButton.Id,
                        bigAddButton.ParentStepName,
                        bigAddButton.StepLocationRelativeToParent,
                        bigAddButton.BranchIndex
                    );
                    _dragDropManager.RegisterDropTarget(dropTarget);
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_isInitialized)
            {
                InitializeCanvas();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_isInitialized)
            {
                _viewport.ViewportSize = Size;
            }
        }

        private void InitializeCanvas()
        {
            _viewport.ViewportSize = Size;
            _isInitialized = true;
            RebuildGraph();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (!_isInitialized)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 保存原始变换
            var originalTransform = g.Transform;

            // 应用视口变换
            _viewport.ApplyTransform(g);

            // 绘制背景
            DrawBackground(g);

            // 绘制流程内容（节点、边缘等）
            DrawFlowContent(g);

            // 恢复原始变换
            g.Transform = originalTransform;

            // 绘制覆盖层（拖拽预览、选择框等）
            DrawOverlays(g);
        }

        /// <summary>
        /// 绘制背景（点阵）
        /// </summary>
        private void DrawBackground(Graphics g)
        {
            var viewportBounds = GetViewportBounds();
            var gridSize = 10f / _viewport.Zoom;

            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1f / _viewport.Zoom))
            {
                // 绘制点阵
                for (float x = viewportBounds.Left; x <= viewportBounds.Right; x += gridSize)
                {
                    for (float y = viewportBounds.Top; y <= viewportBounds.Bottom; y += gridSize)
                    {
                        g.DrawEllipse(pen, x - 0.5f, y - 0.5f, 1f, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制流程内容
        /// </summary>
        private void DrawFlowContent(Graphics g)
        {
            if (_currentGraph == null)
            {
                return;
            }
            
            var zoom = _viewport.Zoom;
            
            // 创建节点位置字典，用于边缘绘制
            var nodePositions = new Dictionary<string, PointF>();
            foreach (var node in _currentGraph.Nodes)
            {
                // 获取节点中心位置（边缘需要连接到中心）
                var centerPos = new PointF(
                    node.Position.X + node.Size.Width / 2,
                    node.Position.Y + node.Size.Height / 2
                );
                nodePositions[node.Id] = centerPos;
            }
            
            // 先绘制边缘（在节点下方）
            foreach (var edge in _currentGraph.Edges)
            {
                if (nodePositions.TryGetValue(edge.SourceId, out var sourcePos) &&
                    nodePositions.TryGetValue(edge.TargetId, out var targetPos))
                {
                    edge.Draw(g, zoom, sourcePos, targetPos);
                }
            }
            
            // 再绘制节点（在边缘上方）
            foreach (var node in _currentGraph.Nodes)
            {
                // 更新节点的选中状态
                node.IsSelected = _stateStore.Selection.SelectedNodeIds.Contains(node.Id);
                
                node.Draw(g, zoom);
            }
        }

        /// <summary>
        /// 绘制覆盖层
        /// </summary>
        private void DrawOverlays(Graphics g)
        {
            // 绘制选择框
            if (_stateStore.Selection.SelectionRectangle != null)
            {
                DrawSelectionRectangle(g, _stateStore.Selection.SelectionRectangle);
            }

            // 绘制拖拽预览
            if (_stateStore.Drag.IsDragging)
            {
                DrawDragPreview(g);
            }
        }

        /// <summary>
        /// 绘制选择矩形
        /// </summary>
        private void DrawSelectionRectangle(Graphics g, SelectionRectangle rect)
        {
            using (var brush = new SolidBrush(Color.FromArgb(50, 59, 130, 246)))
            using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 1f))
            {
                var screenRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                g.FillRectangle(brush, screenRect);
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        /// <summary>
        /// 绘制拖拽预览
        /// </summary>
        private void DrawDragPreview(Graphics g)
        {
            if (_draggedNode == null || !_stateStore.Drag.IsDragging)
            {
                return;
            }

            var dragState = _stateStore.Drag;
            var offsetX = dragState.CurrentPosition.X - dragState.StartPosition.X;
            var offsetY = dragState.CurrentPosition.Y - dragState.StartPosition.Y;
            
            // 保存变换
            var originalTransform = g.Transform;
            
            // 应用视口变换
            _viewport.ApplyTransform(g);
            
            // 绘制半透明的拖拽预览
            var previewBounds = new RectangleF(
                _draggedNode.Bounds.X + offsetX,
                _draggedNode.Bounds.Y + offsetY,
                _draggedNode.Bounds.Width,
                _draggedNode.Bounds.Height
            );
            
            using (var brush = new SolidBrush(Color.FromArgb(128, 59, 130, 246)))
            using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 2f / _viewport.Zoom))
            {
                // 绘制半透明背景
                var path = new GraphicsPath();
                path.AddRectangle(previewBounds);
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            
            // 恢复变换
            g.Transform = originalTransform;
        }

        /// <summary>
        /// 获取视口边界（画布坐标）
        /// </summary>
        private RectangleF GetViewportBounds()
        {
            var topLeft = _viewport.ScreenToCanvas(PointF.Empty);
            var bottomRight = _viewport.ScreenToCanvas(new PointF(Width, Height));
            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y
            );
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            var canvasPoint = _viewport.ScreenToCanvas(e.Location);

            // 检查是否点击了节点
            ICanvasNode clickedNode = null;
            if (_currentGraph != null)
            {
                clickedNode = _currentGraph.Nodes
                    .Where(n => n.Selectable && n.Contains(canvasPoint))
                    .OrderByDescending(n => n.Bounds.Width * n.Bounds.Height) // 选择最大的节点（最上层）
                    .FirstOrDefault();
            }

            if (e.Button == MouseButtons.Middle || 
                (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Space))
            {
                // 平移模式
                _isPanning = true;
                _lastPanPoint = e.Location;
                Cursor = Cursors.Hand;
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (clickedNode != null)
                {
                    // 点击了节点，开始拖拽或选择
                    var appendSelection = (Control.ModifierKeys & Keys.Shift) != 0;
                    _selectionManager.SelectNode(clickedNode, appendSelection);
                    
                    if (clickedNode.Draggable && clickedNode is IDraggable draggable)
                    {
                        _isDragging = true;
                        _draggedNode = clickedNode;
                        _dragDropManager.StartDrag(draggable, canvasPoint);
                    }
                }
                else
                {
                    // 空白区域，开始框选
                    if ((Control.ModifierKeys & Keys.Shift) == 0)
                    {
                        _selectionManager.ClearSelection();
                    }
                    _isSelecting = true;
                    _selectionManager.StartSelection(canvasPoint);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // 右键点击，显示上下文菜单
                var contextMenuManager = new Interaction.ContextMenuManager(_stateStore);
                if (clickedNode != null)
                {
                    _selectionManager.SelectNode(clickedNode, false);
                    contextMenuManager.ShowContextMenu(this, e.Location, clickedNode.Id);
                }
                else
                {
                    contextMenuManager.ShowContextMenu(this, e.Location);
                }
            }
            
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var canvasPoint = _viewport.ScreenToCanvas(e.Location);

            if (_isPanning)
            {
                var deltaX = e.X - _lastPanPoint.X;
                var deltaY = e.Y - _lastPanPoint.Y;
                _viewport.Pan(deltaX, deltaY);
                _lastPanPoint = e.Location;
                Invalidate();
            }
            else if (_isDragging && _draggedNode != null)
            {
                // 更新拖拽
                var dragRect = new RectangleF(
                    _draggedNode.Bounds.X + (canvasPoint.X - _stateStore.Drag.StartPosition.X),
                    _draggedNode.Bounds.Y + (canvasPoint.Y - _stateStore.Drag.StartPosition.Y),
                    _draggedNode.Bounds.Width,
                    _draggedNode.Bounds.Height
                );
                _dragDropManager.UpdateDrag(canvasPoint, dragRect);
            }
            else if (_isSelecting)
            {
                // 更新框选
                _selectionManager.UpdateSelection(canvasPoint);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
            else if (_isDragging)
            {
                _isDragging = false;
                _dragDropManager.EndDrag();
                _draggedNode = null;
            }
            else if (_isSelecting)
            {
                var canvasPoint = _viewport.ScreenToCanvas(e.Location);
                var appendSelection = (Control.ModifierKeys & Keys.Shift) != 0;
                if (_currentGraph != null)
                {
                    _selectionManager.EndSelection(_currentGraph.Nodes.ToList(), appendSelection);
                }
                _isSelecting = false;
            }
            
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Control.ModifierKeys == Keys.Control)
            {
                // 缩放
                var zoomDelta = e.Delta > 0 ? 0.1f : -0.1f;
                var oldZoom = _viewport.Zoom;
                _viewport.ZoomIn(zoomDelta);

                // 以鼠标位置为中心缩放
                var mousePos = e.Location;
                var canvasPos = _viewport.ScreenToCanvas(mousePos);
                var zoomFactor = _viewport.Zoom / oldZoom;
                _viewport.X = mousePos.X - canvasPos.X * _viewport.Zoom;
                _viewport.Y = mousePos.Y - canvasPos.Y * _viewport.Zoom;

                Invalidate();
            }
            else
            {
                // 垂直滚动
                _viewport.Pan(0, -e.Delta);
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // 处理撤销/重做
            if (e.Control && e.KeyCode == Keys.Z)
            {
                _stateStore.Undo();
                e.Handled = true;
                Invalidate();
                return;
            }
            if (e.Control && e.KeyCode == Keys.Y)
            {
                _stateStore.Redo();
                e.Handled = true;
                Invalidate();
                return;
            }
            
            // 处理其他快捷键
            var shortcutManager = new ShortcutManager(_stateStore);
            if (shortcutManager.HandleKeyPress(e.KeyData))
            {
                e.Handled = true;
                Invalidate();
            }
        }

        /// <summary>
        /// 获取视口
        /// </summary>
        public CanvasViewport Viewport => _viewport;
    }
}
