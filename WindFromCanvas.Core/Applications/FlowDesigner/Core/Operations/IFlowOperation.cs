using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 流程操作接口（支持撤销/重做和命令合并）
    /// </summary>
    public interface IFlowOperation
    {
        /// <summary>
        /// 执行操作
        /// </summary>
        FlowVersion Execute(FlowVersion flowVersion);

        /// <summary>
        /// 撤销操作
        /// </summary>
        FlowVersion Undo(FlowVersion flowVersion);

        /// <summary>
        /// 4.1.5 检查是否可以与另一个操作合并（命令合并/防抖）
        /// </summary>
        bool CanMerge(IFlowOperation other);

        /// <summary>
        /// 4.1.5 合并操作（将another合并到当前操作）
        /// </summary>
        IFlowOperation Merge(IFlowOperation other);
    }
}
