using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 拖拽预览覆盖层（绘制拖拽时的半透明预览和目标高亮）
    /// </summary>
    public class DragOverlay
    {
        private FlowNode _draggingNode;
        private List<FlowNode> _draggingNodes;
        private PointF _currentPosition;
        private FlowNode _dropTarget;
        private bool _canDrop;

        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool IsDragging => _draggingNode != null || (_draggingNodes != null && _draggingNodes.Count > 0);

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void StartDrag(FlowNode node, PointF position)
        {
            _draggingNode = node;
            _draggingNodes = new List<FlowNode> { node };
            _currentPosition = position;
            _dropTarget = null;
            _canDrop = false;
        }

        /// <summary>
        /// 开始多节点拖拽
        /// </summary>
        public void StartDrag(List<FlowNode> nodes, PointF position)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            _draggingNode = nodes[0];
            _draggingNodes = new List<FlowNode>(nodes);
            _currentPosition = position;
            _dropTarget = null;
            _canDrop = false;
        }

        /// <summary>
        /// 更新拖拽位置和目标
        /// </summary>
        public void UpdateDrag(PointF position, FlowNode dropTarget, bool canDrop)
        {
            _currentPosition = position;
            _dropTarget = dropTarget;
            _canDrop = canDrop;
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag()
        {
            _draggingNode = null;
            _draggingNodes = null;
            _dropTarget = null;
            _canDrop = false;
        }

        /// <summary>
        /// 取消拖拽
        /// </summary>
        public void CancelDrag()
        {
            EndDrag();
        }

        /// <summary>
        /// 3.2.2 渲染拖拽预览
        /// </summary>
        public void RenderDragPreview(Graphics g, float zoom)
        {
            if (!IsDragging || _draggingNodes == null || _draggingNodes.Count == 0)
                return;

            // 绘制半透明预览
            using (var brush = new SolidBrush(Color.FromArgb(100, Color.LightBlue)))
            using (var pen = new Pen(Color.FromArgb(150, Color.Blue), 2f / zoom))
            {
                pen.DashStyle = DashStyle.Dash;

                foreach (var node in _draggingNodes)
                {
                    if (node == null)
                        continue;

                    var bounds = node.GetBounds();
                    
                    // 计算预览位置（相对于主拖拽节点的偏移）
                    var offset = _currentPosition;
                    if (_draggingNode != null)
                    {
                        var mainBounds = _draggingNode.GetBounds();
                        offset = new PointF(
                            _currentPosition.X + (bounds.X - mainBounds.X),
                            _currentPosition.Y + (bounds.Y - mainBounds.Y)
                        );
                    }

                    // 绘制预览矩形
                    var previewRect = new RectangleF(offset.X, offset.Y, bounds.Width, bounds.Height);
                    g.FillRectangle(brush, previewRect);
                    g.DrawRectangle(pen, previewRect.X, previewRect.Y, previewRect.Width, previewRect.Height);

                    // 绘制节点标题
                    if (node.Data != null && !string.IsNullOrEmpty(node.Data.DisplayName))
                    {
                        using (var font = new Font("Microsoft YaHei", 9f))
                        using (var textBrush = new SolidBrush(Color.FromArgb(150, Color.Black)))
                        {
                            var textSize = g.MeasureString(node.Data.DisplayName, font);
                            var textPos = new PointF(
                                offset.X + (bounds.Width - textSize.Width) / 2,
                                offset.Y + (bounds.Height - textSize.Height) / 2
                            );
                            g.DrawString(node.Data.DisplayName, font, textBrush, textPos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 3.2.3 渲染放置目标高亮
        /// </summary>
        public void RenderDropTargetHighlight(Graphics g, float zoom)
        {
            if (_dropTarget == null)
                return;

            var bounds = _dropTarget.GetBounds();
            
            // 根据是否可以放置选择不同的颜色
            Color highlightColor = _canDrop 
                ? Color.FromArgb(100, 0, 255, 0)   // 绿色 - 可以放置
                : Color.FromArgb(100, 255, 0, 0);   // 红色 - 不能放置

            using (var brush = new SolidBrush(highlightColor))
            using (var pen = new Pen(highlightColor, 3f / zoom))
            {
                // 绘制发光效果的边框
                var expandedBounds = new RectangleF(
                    bounds.X - 5,
                    bounds.Y - 5,
                    bounds.Width + 10,
                    bounds.Height + 10
                );

                g.FillRectangle(brush, expandedBounds);
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        /// <summary>
        /// 渲染覆盖层（组合拖拽预览和目标高亮）
        /// </summary>
        public void Render(Graphics g, float zoom)
        {
            if (!IsDragging)
                return;

            // 先绘制放置目标高亮（在下层）
            RenderDropTargetHighlight(g, zoom);

            // 再绘制拖拽预览（在上层）
            RenderDragPreview(g, zoom);
        }
    }
}
