using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 拖拽状态
    /// </summary>
    public class DragState
    {
        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool IsDragging { get; set; }

        /// <summary>
        /// 拖拽的项ID
        /// </summary>
        public string DraggedItemId { get; set; }

        /// <summary>
        /// 拖拽开始位置
        /// </summary>
        public PointF StartPosition { get; set; }

        /// <summary>
        /// 当前拖拽位置
        /// </summary>
        public PointF CurrentPosition { get; set; }

        /// <summary>
        /// 悬停的目标ID
        /// </summary>
        public string HoveredTargetId { get; set; }

        public DragState()
        {
            IsDragging = false;
            DraggedItemId = null;
            StartPosition = PointF.Empty;
            CurrentPosition = PointF.Empty;
            HoveredTargetId = null;
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void StartDrag(string itemId, PointF position)
        {
            IsDragging = true;
            DraggedItemId = itemId;
            StartPosition = position;
            CurrentPosition = position;
            HoveredTargetId = null;
        }

        /// <summary>
        /// 更新拖拽位置
        /// </summary>
        public void UpdateDrag(PointF position, string hoveredTargetId = null)
        {
            CurrentPosition = position;
            HoveredTargetId = hoveredTargetId;
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag()
        {
            IsDragging = false;
            DraggedItemId = null;
            StartPosition = PointF.Empty;
            CurrentPosition = PointF.Empty;
            HoveredTargetId = null;
        }
    }
}
