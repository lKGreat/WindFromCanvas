namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums
{
    /// <summary>
    /// 流程动作类型枚举
    /// </summary>
    public enum FlowActionType
    {
        /// <summary>
        /// 代码块
        /// </summary>
        CODE,

        /// <summary>
        /// 组件动作
        /// </summary>
        PIECE,

        /// <summary>
        /// 循环遍历
        /// </summary>
        LOOP_ON_ITEMS,

        /// <summary>
        /// 路由/分支
        /// </summary>
        ROUTER
    }
}
