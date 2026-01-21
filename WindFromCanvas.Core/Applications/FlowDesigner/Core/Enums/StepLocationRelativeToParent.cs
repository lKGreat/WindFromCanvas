namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums
{
    /// <summary>
    /// 步骤相对于父节点的位置枚举
    /// </summary>
    public enum StepLocationRelativeToParent
    {
        /// <summary>
        /// 在父节点之后
        /// </summary>
        AFTER,

        /// <summary>
        /// 在循环内部
        /// </summary>
        INSIDE_LOOP,

        /// <summary>
        /// 在分支内部
        /// </summary>
        INSIDE_BRANCH
    }
}
