using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 锚点方向
    /// </summary>
    public enum AnchorDirection
    {
        Input,   // 输入锚点
        Output   // 输出锚点
    }

    /// <summary>
    /// 锚点（连接点）
    /// </summary>
    [DataContract]
    [Serializable]
    public class AnchorPoint
    {
        /// <summary>
        /// 锚点ID（格式: {nodeId}_{direction}_{index}）
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 相对位置（相对于节点中心或左上角）
        /// </summary>
        [DataMember]
        public float RelativeX { get; set; }

        [DataMember]
        public float RelativeY { get; set; }

        /// <summary>
        /// 相对位置（便捷属性）
        /// </summary>
        public PointF RelativePosition
        {
            get => new PointF(RelativeX, RelativeY);
            set
            {
                RelativeX = value.X;
                RelativeY = value.Y;
            }
        }

        /// <summary>
        /// 锚点方向
        /// </summary>
        [DataMember]
        public AnchorDirection Direction { get; set; }

        /// <summary>
        /// 最大连接数（-1表示无限制）
        /// </summary>
        [DataMember]
        public int MaxConnections { get; set; } = -1;

        /// <summary>
        /// 允许连接的目标节点类型列表（空列表表示允许所有类型）
        /// </summary>
        [DataMember]
        public List<string> AllowedTargetTypes { get; set; }

        /// <summary>
        /// 锚点显示名称（可选）
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 锚点是否可见
        /// </summary>
        [DataMember]
        public bool Visible { get; set; } = true;

        public AnchorPoint()
        {
            AllowedTargetTypes = new List<string>();
        }

        public AnchorPoint(string id, PointF relativePosition, AnchorDirection direction)
            : this()
        {
            Id = id;
            RelativePosition = relativePosition;
            Direction = direction;
        }

        /// <summary>
        /// 生成锚点ID
        /// </summary>
        public static string GenerateId(string nodeId, AnchorDirection direction, int index)
        {
            return $"{nodeId}_{direction}_{index}";
        }

        /// <summary>
        /// 检查是否可以连接到目标节点类型
        /// </summary>
        public bool CanConnectTo(string targetNodeType)
        {
            if (AllowedTargetTypes == null || AllowedTargetTypes.Count == 0)
            {
                return true; // 没有限制，允许所有类型
            }
            return AllowedTargetTypes.Contains(targetNodeType);
        }

        /// <summary>
        /// 检查是否已达到最大连接数
        /// </summary>
        public bool CanAcceptMoreConnections(int currentConnectionCount)
        {
            if (MaxConnections < 0)
            {
                return true; // 无限制
            }
            return currentConnectionCount < MaxConnections;
        }
    }
}
