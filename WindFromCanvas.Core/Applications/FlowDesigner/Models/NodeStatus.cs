namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 节点状态枚举
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None,

        /// <summary>
        /// 运行中（蓝色旋转动画）
        /// </summary>
        Running,

        /// <summary>
        /// 成功（绿色对勾）
        /// </summary>
        Success,

        /// <summary>
        /// 失败（红色叉号）
        /// </summary>
        Failed,

        /// <summary>
        /// 跳过（灰色斜杠）
        /// </summary>
        Skipped
    }
}
