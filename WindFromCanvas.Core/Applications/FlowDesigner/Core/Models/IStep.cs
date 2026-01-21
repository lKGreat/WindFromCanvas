namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// 步骤接口（动作和触发器的共同接口）
    /// </summary>
    public interface IStep
    {
        /// <summary>
        /// 步骤唯一名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        bool Valid { get; set; }

        /// <summary>
        /// 是否跳过
        /// </summary>
        bool Skip { get; set; }
    }
}
