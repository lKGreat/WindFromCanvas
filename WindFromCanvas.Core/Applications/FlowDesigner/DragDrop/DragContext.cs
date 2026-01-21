using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 拖拽上下文
    /// </summary>
    public class DragContext
    {
        public IDraggable Item { get; set; }
        public PointF StartPosition { get; set; }
        public PointF CurrentPosition { get; set; }
        public IDropTarget HoveredTarget { get; set; }
    }
}
