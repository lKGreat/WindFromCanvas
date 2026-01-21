using System;
using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// 流程动作基类
    /// </summary>
    public abstract class FlowAction : IStep
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public FlowActionType Type { get; set; }
        public bool Valid { get; set; }
        public bool Skip { get; set; }
        public FlowAction NextAction { get; set; }

        protected FlowAction()
        {
            Valid = true;
            Skip = false;
        }
    }

    /// <summary>
    /// 代码动作
    /// </summary>
    public class CodeAction : FlowAction
    {
        public CodeActionSettings Settings { get; set; }

        public CodeAction()
        {
            Type = FlowActionType.CODE;
        }
    }

    /// <summary>
    /// 代码动作设置
    /// </summary>
    public class CodeActionSettings
    {
        public SourceCode SourceCode { get; set; }
        public Dictionary<string, object> Input { get; set; }
        public string CustomLogoUrl { get; set; }
        public ActionErrorHandlingOptions ErrorHandlingOptions { get; set; }

        public CodeActionSettings()
        {
            Input = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 源代码
    /// </summary>
    public class SourceCode
    {
        public string PackageJson { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// 组件动作
    /// </summary>
    public class PieceAction : FlowAction
    {
        public PieceActionSettings Settings { get; set; }

        public PieceAction()
        {
            Type = FlowActionType.PIECE;
        }
    }

    /// <summary>
    /// 组件动作设置
    /// </summary>
    public class PieceActionSettings
    {
        public Dictionary<string, PropertySettings> PropertySettings { get; set; }
        public string PieceName { get; set; }
        public string PieceVersion { get; set; }
        public string ActionName { get; set; }
        public Dictionary<string, object> Input { get; set; }
        public string CustomLogoUrl { get; set; }
        public ActionErrorHandlingOptions ErrorHandlingOptions { get; set; }

        public PieceActionSettings()
        {
            PropertySettings = new Dictionary<string, PropertySettings>();
            Input = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 属性设置
    /// </summary>
    public class PropertySettings
    {
        public object Value { get; set; }
    }

    /// <summary>
    /// 错误处理选项
    /// </summary>
    public class ActionErrorHandlingOptions
    {
        public ErrorHandlingOption ContinueOnFailure { get; set; }
        public ErrorHandlingOption RetryOnFailure { get; set; }
    }

    /// <summary>
    /// 错误处理选项值
    /// </summary>
    public class ErrorHandlingOption
    {
        public bool? Value { get; set; }
    }

    /// <summary>
    /// 循环动作
    /// </summary>
    public class LoopOnItemsAction : FlowAction
    {
        public LoopOnItemsActionSettings Settings { get; set; }
        public FlowAction FirstLoopAction { get; set; }

        public LoopOnItemsAction()
        {
            Type = FlowActionType.LOOP_ON_ITEMS;
        }
    }

    /// <summary>
    /// 循环动作设置
    /// </summary>
    public class LoopOnItemsActionSettings
    {
        public string Items { get; set; }
        public string CustomLogoUrl { get; set; }
    }

    /// <summary>
    /// 路由动作
    /// </summary>
    public class RouterAction : FlowAction
    {
        public RouterActionSettings Settings { get; set; }
        public List<FlowAction> Children { get; set; }

        public RouterAction()
        {
            Type = FlowActionType.ROUTER;
            Children = new List<FlowAction>();
        }
    }

    /// <summary>
    /// 路由动作设置
    /// </summary>
    public class RouterActionSettings
    {
        public List<RouterBranch> Branches { get; set; }
        public RouterExecutionType ExecutionType { get; set; }
        public string CustomLogoUrl { get; set; }

        public RouterActionSettings()
        {
            Branches = new List<RouterBranch>();
        }
    }
}
