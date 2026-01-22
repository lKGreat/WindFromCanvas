namespace WindFromCanvas.Core.Applications.FlowDesigner.Rendering
{
    /// <summary>
    /// 渲染层类型
    /// </summary>
    public enum RenderLayerType
    {
        /// <summary>
        /// 背景层（最底层）
        /// </summary>
        Background = 0,

        /// <summary>
        /// 网格层
        /// </summary>
        Grid = 1,

        /// <summary>
        /// 连线层
        /// </summary>
        Connection = 2,

        /// <summary>
        /// 节点层
        /// </summary>
        Node = 3,

        /// <summary>
        /// 选择层
        /// </summary>
        Selection = 4,

        /// <summary>
        /// 覆盖层（对齐线、预览等，最顶层）
        /// </summary>
        Overlay = 5
    }
}
