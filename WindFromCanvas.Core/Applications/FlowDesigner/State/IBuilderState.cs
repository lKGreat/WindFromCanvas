namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 构建器状态接口（组合所有状态分片）
    /// </summary>
    public interface IBuilderState
    {
        CanvasState Canvas { get; }
        FlowState Flow { get; }
        SelectionState Selection { get; }
        DragState Drag { get; }
    }
}
