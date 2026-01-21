using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// 路由分支
    /// </summary>
    public class RouterBranch
    {
        /// <summary>
        /// 分支条件（二维数组：OR 条件组，每个组内是 AND 条件）
        /// </summary>
        public List<List<BranchCondition>> Conditions { get; set; }

        /// <summary>
        /// 分支类型
        /// </summary>
        public BranchExecutionType BranchType { get; set; }

        /// <summary>
        /// 分支名称
        /// </summary>
        public string BranchName { get; set; }

        public RouterBranch()
        {
            Conditions = new List<List<BranchCondition>>();
        }
    }

    /// <summary>
    /// 分支条件
    /// </summary>
    public class BranchCondition
    {
        public string FirstValue { get; set; }
        public string SecondValue { get; set; }
        public BranchOperator Operator { get; set; }
        public bool? CaseSensitive { get; set; }
    }

    /// <summary>
    /// 分支操作符
    /// </summary>
    public enum BranchOperator
    {
        TEXT_CONTAINS,
        TEXT_DOES_NOT_CONTAIN,
        TEXT_EXACTLY_MATCHES,
        TEXT_DOES_NOT_EXACTLY_MATCH,
        TEXT_STARTS_WITH,
        TEXT_DOES_NOT_START_WITH,
        TEXT_ENDS_WITH,
        TEXT_DOES_NOT_END_WITH,
        NUMBER_IS_GREATER_THAN,
        NUMBER_IS_LESS_THAN,
        NUMBER_IS_EQUAL_TO,
        BOOLEAN_IS_TRUE,
        BOOLEAN_IS_FALSE,
        DATE_IS_BEFORE,
        DATE_IS_EQUAL,
        DATE_IS_AFTER,
        LIST_CONTAINS,
        LIST_DOES_NOT_CONTAIN,
        LIST_IS_EMPTY,
        LIST_IS_NOT_EMPTY,
        EXISTS,
        DOES_NOT_EXIST
    }
}
