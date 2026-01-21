using System;
using System.Collections.Generic;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 拖拽管理器（匹配 Activepieces DndContext）
    /// </summary>
    public class DragDropManager
    {
        private readonly BuilderStateStore _stateStore;
        private readonly CollisionDetector _collisionDetector;
        private DragContext _currentDrag;
        private List<IDropTarget> _dropTargets;

        public event EventHandler<DragContext> DragStarted;
        public event EventHandler<DragContext> DragUpdated;
        public event EventHandler<DragContext> DragEnded;

        public DragDropManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _collisionDetector = new CollisionDetector();
            _dropTargets = new List<IDropTarget>();
        }

        /// <summary>
        /// 注册放置目标
        /// </summary>
        public void RegisterDropTarget(IDropTarget target)
        {
            if (!_dropTargets.Contains(target))
            {
                _dropTargets.Add(target);
            }
        }

        /// <summary>
        /// 取消注册放置目标
        /// </summary>
        public void UnregisterDropTarget(IDropTarget target)
        {
            _dropTargets.Remove(target);
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void StartDrag(IDraggable item, PointF position)
        {
            _currentDrag = new DragContext
            {
                Item = item,
                StartPosition = position,
                CurrentPosition = position
            };

            _stateStore.StartDrag(item.Id, position);
            DragStarted?.Invoke(this, _currentDrag);
        }

        /// <summary>
        /// 更新拖拽
        /// </summary>
        public void UpdateDrag(PointF position, RectangleF dragRect)
        {
            if (_currentDrag == null) return;

            _currentDrag.CurrentPosition = position;
            var collision = _collisionDetector.DetectCollision(_currentDrag, _dropTargets, dragRect);
            _currentDrag.HoveredTarget = collision;

            _stateStore.UpdateDrag(position, collision?.Id);
            DragUpdated?.Invoke(this, _currentDrag);
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag()
        {
            if (_currentDrag == null) return;

            if (_currentDrag.HoveredTarget != null)
            {
                ExecuteDrop(_currentDrag);
            }

            _stateStore.EndDrag();
            DragEnded?.Invoke(this, _currentDrag);
            _currentDrag = null;
        }

        /// <summary>
        /// 取消拖拽
        /// </summary>
        public void CancelDrag()
        {
            _currentDrag = null;
            _stateStore.EndDrag();
        }

        /// <summary>
        /// 执行放置操作
        /// </summary>
        private void ExecuteDrop(DragContext context)
        {
            // TODO: 实现放置逻辑，调用操作命令
            // 这将在 Operations 中实现
        }
    }
}
