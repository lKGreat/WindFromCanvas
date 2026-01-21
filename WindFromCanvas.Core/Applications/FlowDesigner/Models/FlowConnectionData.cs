using System;
using System.Runtime.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 流程连接数据（可序列化）
    /// </summary>
    [DataContract]
    [Serializable]
    public class FlowConnectionData
    {
        /// <summary>
        /// 源节点名称
        /// </summary>
        [DataMember]
        public string SourceNode { get; set; }

        /// <summary>
        /// 目标节点名称
        /// </summary>
        [DataMember]
        public string TargetNode { get; set; }

        /// <summary>
        /// 源端口名称（可选，用于多端口节点）
        /// </summary>
        [DataMember]
        public string SourcePort { get; set; }

        /// <summary>
        /// 目标端口名称（可选，用于多端口节点）
        /// </summary>
        [DataMember]
        public string TargetPort { get; set; }

        /// <summary>
        /// 连接标签/条件（用于分支节点）
        /// </summary>
        [DataMember]
        public string Label { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        [DataMember]
        public FlowConnectionType Type { get; set; }

        public FlowConnectionData()
        {
            Type = FlowConnectionType.StraightLine;
        }

        /// <summary>
        /// 创建连接数据
        /// </summary>
        public static FlowConnectionData Create(string sourceNode, string targetNode, 
            string sourcePort = null, string targetPort = null, 
            FlowConnectionType type = FlowConnectionType.StraightLine)
        {
            return new FlowConnectionData
            {
                SourceNode = sourceNode,
                TargetNode = targetNode,
                SourcePort = sourcePort,
                TargetPort = targetPort,
                Type = type
            };
        }
    }

    /// <summary>
    /// 连接类型枚举
    /// </summary>
    public enum FlowConnectionType
    {
        /// <summary>
        /// 直线连接
        /// </summary>
        StraightLine,

        /// <summary>
        /// 循环开始边
        /// </summary>
        LoopStartEdge,

        /// <summary>
        /// 循环返回边
        /// </summary>
        LoopReturnEdge,

        /// <summary>
        /// 路由开始边
        /// </summary>
        RouterStartEdge,

        /// <summary>
        /// 路由结束边
        /// </summary>
        RouterEndEdge
    }
}
