using System;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Rendering
{
    /// <summary>
    /// 分层渲染器（使用RenderLayerManager进行分层渲染）
    /// </summary>
    public class LayeredRenderer
    {
        private readonly RenderLayerManager _layerManager;
        private readonly BuilderStateStore _stateStore;
        private FlowGraph _currentGraph;

        public LayeredRenderer(BuilderStateStore stateStore)
        {
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _layerManager = new RenderLayerManager();
            RegisterRenderCallbacks();
        }

        /// <summary>
        /// 设置当前图形
        /// </summary>
        public void SetGraph(FlowGraph graph)
        {
            _currentGraph = graph;
            _layerManager.MarkAllLayersDirty();
        }

        /// <summary>
        /// 注册渲染回调
        /// </summary>
        private void RegisterRenderCallbacks()
        {
            // 背景层
            _layerManager.RegisterRenderCallback(RenderLayerType.Background, (g, viewport) =>
            {
                // 绘制背景色
                g.Clear(Color.FromArgb(250, 250, 250));
            });

            // 网格层
            _layerManager.RegisterRenderCallback(RenderLayerType.Grid, (g, viewport) =>
            {
                DrawGrid(g, viewport);
            });

            // 连线层
            _layerManager.RegisterRenderCallback(RenderLayerType.Connection, (g, viewport) =>
            {
                DrawConnections(g, viewport);
            });

            // 节点层
            _layerManager.RegisterRenderCallback(RenderLayerType.Node, (g, viewport) =>
            {
                DrawNodes(g, viewport);
            });

            // 选择层
            _layerManager.RegisterRenderCallback(RenderLayerType.Selection, (g, viewport) =>
            {
                DrawSelection(g, viewport);
            });

            // 覆盖层
            _layerManager.RegisterRenderCallback(RenderLayerType.Overlay, (g, viewport) =>
            {
                DrawOverlay(g, viewport);
            });
        }

        /// <summary>
        /// 渲染到Graphics
        /// </summary>
        public void Render(Graphics g, RectangleF viewport, float zoom)
        {
            _layerManager.Render(g, viewport);
        }

        /// <summary>
        /// 标记层为脏
        /// </summary>
        public void MarkLayerDirty(RenderLayerType layer)
        {
            _layerManager.MarkLayerDirty(layer);
        }

        /// <summary>
        /// 绘制网格
        /// </summary>
        private void DrawGrid(Graphics g, RectangleF viewport)
        {
            const float gridSize = 20f;
            var pen = new Pen(Color.FromArgb(200, 200, 200), 1f);

            float startX = (float)(Math.Floor(viewport.Left / gridSize) * gridSize);
            float startY = (float)(Math.Floor(viewport.Top / gridSize) * gridSize);

            // 绘制垂直线
            for (float x = startX; x <= viewport.Right; x += gridSize)
            {
                g.DrawLine(pen, x, viewport.Top, x, viewport.Bottom);
            }

            // 绘制水平线
            for (float y = startY; y <= viewport.Bottom; y += gridSize)
            {
                g.DrawLine(pen, viewport.Left, y, viewport.Right, y);
            }

            pen.Dispose();
        }

        /// <summary>
        /// 绘制连线
        /// </summary>
        private void DrawConnections(Graphics g, RectangleF viewport)
        {
            if (_currentGraph?.Edges == null)
            {
                return;
            }

            foreach (var edge in _currentGraph.Edges)
            {
                // 这里需要根据实际的edge实现来绘制
                // 暂时跳过，因为需要节点位置信息
            }
        }

        /// <summary>
        /// 绘制节点
        /// </summary>
        private void DrawNodes(Graphics g, RectangleF viewport)
        {
            if (_currentGraph?.Nodes == null)
            {
                return;
            }

            foreach (var node in _currentGraph.Nodes)
            {
                if (viewport.IntersectsWith(node.Bounds))
                {
                    node.Draw(g, 1.0f); // 假设zoom为1.0
                }
            }
        }

        /// <summary>
        /// 绘制选择
        /// </summary>
        private void DrawSelection(Graphics g, RectangleF viewport)
        {
            if (_stateStore?.Selection?.SelectedNodeIds == null)
            {
                return;
            }

            // 绘制选中节点的边框
            if (_currentGraph?.Nodes != null)
            {
                var pen = new Pen(Color.FromArgb(59, 130, 246), 2f);
                foreach (var nodeId in _stateStore.Selection.SelectedNodeIds)
                {
                    var node = _currentGraph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (node != null && viewport.IntersectsWith(node.Bounds))
                    {
                        var bounds = node.Bounds;
                        g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    }
                }
                pen.Dispose();
            }
        }

        /// <summary>
        /// 绘制覆盖层（对齐线、预览等）
        /// </summary>
        private void DrawOverlay(Graphics g, RectangleF viewport)
        {
            // 这里可以绘制对齐线、拖拽预览等
            // 具体实现取决于实际需求
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _layerManager?.Dispose();
        }
    }
}
