using System;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 流程节点类型枚举
    /// </summary>
    public enum FlowNodeType
    {
        /// <summary>
        /// 开始节点
        /// </summary>
        Start,

        /// <summary>
        /// 处理/操作节点
        /// </summary>
        Process,

        /// <summary>
        /// 判断/路由节点
        /// </summary>
        Decision,

        /// <summary>
        /// 循环节点
        /// </summary>
        Loop,

        /// <summary>
        /// 结束节点
        /// </summary>
        End,

        /// <summary>
        /// 代码节点
        /// </summary>
        Code,

        /// <summary>
        /// 组件节点
        /// </summary>
        Piece,

        /// <summary>
        /// 组/容器节点
        /// </summary>
        Group
    }
}
