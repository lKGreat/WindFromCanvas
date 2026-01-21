namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 放置目标接口
    /// </summary>
    public interface IDropTarget
    {
        /// <summary>
        /// 目标ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 接受的拖拽类型
        /// </summary>
        string AcceptsDragType { get; }

        /// <summary>
        /// 是否可以接受拖拽项
        /// </summary>
        bool CanAccept(IDraggable draggable);
    }
}
