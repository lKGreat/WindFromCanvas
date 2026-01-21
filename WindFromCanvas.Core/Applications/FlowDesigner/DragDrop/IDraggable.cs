namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 可拖拽接口
    /// </summary>
    public interface IDraggable
    {
        /// <summary>
        /// 拖拽项ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 拖拽类型标签
        /// </summary>
        string DragType { get; }
    }
}
