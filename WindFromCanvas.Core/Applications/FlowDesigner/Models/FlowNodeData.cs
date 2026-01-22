using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 流程节点数据（可序列化）
    /// </summary>
    [DataContract]
    [Serializable]
    public class FlowNodeData
    {
        /// <summary>
        /// 节点唯一名称/标识
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 节点显示名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 节点类型
        /// </summary>
        [DataMember]
        public FlowNodeType Type { get; set; }

        /// <summary>
        /// 节点在画布上的位置（X坐标）
        /// </summary>
        [DataMember]
        public float PositionX { get; set; }

        /// <summary>
        /// 节点在画布上的位置（Y坐标）
        /// </summary>
        [DataMember]
        public float PositionY { get; set; }

        /// <summary>
        /// 节点在画布上的位置
        /// </summary>
        public PointF Position
        {
            get => new PointF(PositionX, PositionY);
            set
            {
                PositionX = value.X;
                PositionY = value.Y;
            }
        }

        /// <summary>
        /// 节点配置/设置（JSON序列化友好）
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Settings { get; set; }

        /// <summary>
        /// 节点是否有效（通过验证）
        /// </summary>
        [DataMember]
        public bool Valid { get; set; }

        /// <summary>
        /// 是否跳过执行此节点
        /// </summary>
        [DataMember]
        public bool Skip { get; set; }

        /// <summary>
        /// 节点描述/备注
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 节点图标路径（支持PNG、SVG、Font图标）
        /// </summary>
        [DataMember]
        public string IconPath { get; set; }

        /// <summary>
        /// 节点图标类型（Image、Svg、Font）
        /// </summary>
        [DataMember]
        public string IconType { get; set; }

        /// <summary>
        /// 节点状态
        /// </summary>
        [DataMember]
        public NodeStatus Status { get; set; } = NodeStatus.None;

        /// <summary>
        /// 锚点列表（连接点）
        /// </summary>
        [DataMember]
        public List<AnchorPoint> Anchors { get; set; }

        public FlowNodeData()
        {
            Settings = new Dictionary<string, object>();
            Valid = true;
            Skip = false;
            Anchors = new List<AnchorPoint>();
        }

        /// <summary>
        /// 克隆节点数据
        /// </summary>
        public FlowNodeData Clone()
        {
            return new FlowNodeData
            {
                Name = this.Name,
                DisplayName = this.DisplayName,
                Type = this.Type,
                Position = this.Position,
                Settings = new Dictionary<string, object>(this.Settings),
                Valid = this.Valid,
                Skip = this.Skip,
                Description = this.Description
            };
        }
    }
}
