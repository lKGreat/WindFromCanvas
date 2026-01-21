using System;
using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 流程状态（匹配 Activepieces FlowState）
    /// </summary>
    public class FlowState
    {
        /// <summary>
        /// 流程版本
        /// </summary>
        public FlowVersion FlowVersion { get; set; }

        /// <summary>
        /// 输出示例数据
        /// </summary>
        public Dictionary<string, object> OutputSampleData { get; set; }

        /// <summary>
        /// 输入示例数据
        /// </summary>
        public Dictionary<string, object> InputSampleData { get; set; }

        /// <summary>
        /// 是否正在保存
        /// </summary>
        public bool Saving { get; set; }

        /// <summary>
        /// 是否正在发布
        /// </summary>
        public bool IsPublishing { get; set; }

        /// <summary>
        /// 操作监听器列表
        /// </summary>
        public List<Action<FlowVersion, FlowOperationRequest>> OperationListeners { get; set; }

        public FlowState()
        {
            FlowVersion = new FlowVersion();
            OutputSampleData = new Dictionary<string, object>();
            InputSampleData = new Dictionary<string, object>();
            Saving = false;
            IsPublishing = false;
            OperationListeners = new List<Action<FlowVersion, FlowOperationRequest>>();
        }
    }

    /// <summary>
    /// 流程操作请求（临时定义，后续会在 Operations 中完善）
    /// </summary>
    public class FlowOperationRequest
    {
        public Core.Enums.FlowOperationType Type { get; set; }
        public object Request { get; set; }
    }
}
