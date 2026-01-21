using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Interaction
{
    /// <summary>
    /// 选择管理器（处理单选、框选、多选）
    /// </summary>
    public class SelectionManager
    {
        private readonly BuilderStateStore _stateStore;
        private PointF _selectionStart;
        private bool _isSelecting;

        public SelectionManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        /// <summary>
        /// 开始框选
        /// </summary>
        public void StartSelection(PointF startPoint)
        {
            _selectionStart = startPoint;
            _isSelecting = true;
            _stateStore.Selection.SelectionRectangle = new SelectionRectangle
            {
                X = startPoint.X,
                Y = startPoint.Y,
                Width = 0,
                Height = 0
            };
        }

        /// <summary>
        /// 更新框选
        /// </summary>
        public void UpdateSelection(PointF currentPoint)
        {
            if (!_isSelecting) return;

            var rect = _stateStore.Selection.SelectionRectangle;
            rect.Width = currentPoint.X - _selectionStart.X;
            rect.Height = currentPoint.Y - _selectionStart.Y;
        }

        /// <summary>
        /// 结束框选
        /// </summary>
        public void EndSelection(List<ICanvasNode> nodes, bool appendToSelection = false)
        {
            if (!_isSelecting) return;

            var rect = _stateStore.Selection.SelectionRectangle;
            var normalizedRect = NormalizeRectangle(rect);

            var selectedNodes = nodes.Where(node =>
                node.Selectable && Intersects(normalizedRect, node.Bounds)
            ).ToList();

            if (appendToSelection)
            {
                // 追加到现有选择
                var existingIds = _stateStore.Selection.SelectedNodeIds.ToHashSet();
                foreach (var node in selectedNodes)
                {
                    if (!existingIds.Contains(node.Id))
                    {
                        _stateStore.Selection.SelectedNodeIds.Add(node.Id);
                    }
                }
            }
            else
            {
                // 替换选择
                _stateStore.Selection.SelectedNodeIds.Clear();
                _stateStore.Selection.SelectedNodeIds.AddRange(selectedNodes.Select(n => n.Id));
            }

            _stateStore.SetSelectedNodes(_stateStore.Selection.SelectedNodeIds.ToArray());
            _isSelecting = false;
            _stateStore.Selection.SelectionRectangle = null;
        }

        /// <summary>
        /// 选择单个节点
        /// </summary>
        public void SelectNode(ICanvasNode node, bool appendToSelection = false)
        {
            if (!node.Selectable) return;

            if (appendToSelection)
            {
                if (!_stateStore.Selection.SelectedNodeIds.Contains(node.Id))
                {
                    _stateStore.Selection.SelectedNodeIds.Add(node.Id);
                }
            }
            else
            {
                _stateStore.Selection.SelectedNodeIds.Clear();
                _stateStore.Selection.SelectedNodeIds.Add(node.Id);
            }

            _stateStore.SetSelectedNodes(_stateStore.Selection.SelectedNodeIds.ToArray());
        }

        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            _stateStore.Selection.ClearSelection();
            _stateStore.SetSelectedNodes(new string[0]);
        }

        /// <summary>
        /// 规范化矩形（确保宽度和高度为正）
        /// </summary>
        private RectangleF NormalizeRectangle(SelectionRectangle rect)
        {
            var x = Math.Min(rect.X, rect.X + rect.Width);
            var y = Math.Min(rect.Y, rect.Y + rect.Height);
            var width = Math.Abs(rect.Width);
            var height = Math.Abs(rect.Height);
            return new RectangleF(x, y, width, height);
        }

        /// <summary>
        /// 检查两个矩形是否相交
        /// </summary>
        private bool Intersects(RectangleF rect1, RectangleF rect2)
        {
            return rect1.IntersectsWith(rect2);
        }
    }
}
