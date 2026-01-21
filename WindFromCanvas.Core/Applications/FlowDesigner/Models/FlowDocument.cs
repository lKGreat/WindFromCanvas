using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 流程文档（可序列化）
    /// </summary>
    [Serializable]
    public class FlowDocument
    {
        /// <summary>
        /// 流程唯一标识
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 流程显示名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 流程描述
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 所有节点数据
        /// </summary>
        [DataMember]
        public List<FlowNodeData> Nodes { get; set; }

        /// <summary>
        /// 所有连接数据
        /// </summary>
        [DataMember]
        public List<FlowConnectionData> Connections { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DataMember]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [DataMember]
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember]
        public string Version { get; set; }

        public FlowDocument()
        {
            Id = Guid.NewGuid().ToString();
            DisplayName = "新流程";
            Nodes = new List<FlowNodeData>();
            Connections = new List<FlowConnectionData>();
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
            Version = "1.0";
        }

        /// <summary>
        /// 创建新流程文档
        /// </summary>
        public static FlowDocument CreateNew(string displayName = "新流程")
        {
            var doc = new FlowDocument
            {
                DisplayName = displayName
            };
            return doc;
        }
    }
}
