using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations
{
    /// <summary>
    /// 流程操作接口
    /// </summary>
    public interface IFlowOperation
    {
        /// <summary>
        /// 执行操作
        /// </summary>
        FlowVersion Execute(FlowVersion flowVersion);
    }
}
