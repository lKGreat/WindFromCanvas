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
        /// 业务属性容器（类似LogicFlow的Properties字典）
        /// 用于存储节点的自定义业务数据，与Settings分离
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; }

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
            Properties = new Dictionary<string, object>();
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
                Properties = new Dictionary<string, object>(this.Properties),
                Valid = this.Valid,
                Skip = this.Skip,
                Description = this.Description
            };
        }

        #region 连接规则校验

        /// <summary>
        /// 获取作为源节点的连接规则
        /// 返回可以连接的目标节点类型列表，null表示允许所有类型
        /// </summary>
        public virtual List<FlowNodeType> GetConnectedSourceRules()
        {
            // 默认允许连接所有类型，子类可重写此方法定义特定规则
            return null;
        }

        /// <summary>
        /// 获取作为目标节点的连接规则
        /// 返回可以被哪些源节点类型连接，null表示允许所有类型
        /// </summary>
        public virtual List<FlowNodeType> GetConnectedTargetRules()
        {
            // 默认允许被所有类型连接，子类可重写此方法定义特定规则
            return null;
        }

        /// <summary>
        /// 检查是否可以连接到目标节点
        /// </summary>
        public bool CanConnectTo(FlowNodeData targetNode)
        {
            if (targetNode == null)
                return false;

            var sourceRules = GetConnectedSourceRules();
            var targetRules = targetNode.GetConnectedTargetRules();

            // 检查源节点的输出规则
            if (sourceRules != null && !sourceRules.Contains(targetNode.Type))
                return false;

            // 检查目标节点的输入规则
            if (targetRules != null && !targetRules.Contains(this.Type))
                return false;

            return true;
        }

        #endregion

        #region 锚点定义

        /// <summary>
        /// 获取节点的锚点列表（虚方法，子类可重写定义特定锚点）
        /// </summary>
        public virtual List<AnchorPoint> GetAnchors()
        {
            // 如果已有锚点定义，直接返回
            if (Anchors != null && Anchors.Count > 0)
                return Anchors;

            // 默认创建标准的输入输出锚点
            return CreateDefaultAnchors();
        }

        /// <summary>
        /// 创建默认锚点配置（上方输入，下方输出）
        /// </summary>
        protected virtual List<AnchorPoint> CreateDefaultAnchors()
        {
            var anchors = new List<AnchorPoint>();

            // 默认输入锚点（顶部中心）
            anchors.Add(new AnchorPoint
            {
                Id = AnchorPoint.GenerateId(Name, AnchorDirection.Input, 0),
                RelativeX = 0,
                RelativeY = -0.5f, // 相对于节点中心的上方
                Direction = AnchorDirection.Input,
                MaxConnections = -1, // 无限制
                Visible = true
            });

            // 默认输出锚点（底部中心）
            anchors.Add(new AnchorPoint
            {
                Id = AnchorPoint.GenerateId(Name, AnchorDirection.Output, 0),
                RelativeX = 0,
                RelativeY = 0.5f, // 相对于节点中心的下方
                Direction = AnchorDirection.Output,
                MaxConnections = -1, // 无限制
                Visible = true
            });

            return anchors;
        }

        #endregion

        #region 数据序列化接口

        /// <summary>
        /// 获取节点数据（用于序列化和导出）
        /// </summary>
        public virtual Dictionary<string, object> GetData()
        {
            var data = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["displayName"] = DisplayName,
                ["type"] = Type.ToString(),
                ["x"] = PositionX,
                ["y"] = PositionY,
                ["valid"] = Valid,
                ["skip"] = Skip,
                ["description"] = Description ?? string.Empty,
                ["iconPath"] = IconPath ?? string.Empty,
                ["iconType"] = IconType ?? string.Empty,
                ["status"] = Status.ToString()
            };

            // 添加Settings
            if (Settings != null && Settings.Count > 0)
            {
                data["settings"] = new Dictionary<string, object>(Settings);
            }

            // 添加Properties
            if (Properties != null && Properties.Count > 0)
            {
                data["properties"] = new Dictionary<string, object>(Properties);
            }

            // 添加Anchors信息
            if (Anchors != null && Anchors.Count > 0)
            {
                var anchorsList = new List<Dictionary<string, object>>();
                foreach (var anchor in Anchors)
                {
                    anchorsList.Add(new Dictionary<string, object>
                    {
                        ["id"] = anchor.Id,
                        ["x"] = anchor.RelativeX,
                        ["y"] = anchor.RelativeY,
                        ["direction"] = anchor.Direction.ToString(),
                        ["maxConnections"] = anchor.MaxConnections,
                        ["visible"] = anchor.Visible
                    });
                }
                data["anchors"] = anchorsList;
            }

            return data;
        }

        /// <summary>
        /// 从数据字典设置节点属性（用于反序列化和导入）
        /// </summary>
        public virtual void SetData(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            if (data.ContainsKey("name"))
                Name = data["name"]?.ToString();

            if (data.ContainsKey("displayName"))
                DisplayName = data["displayName"]?.ToString();

            if (data.ContainsKey("type") && Enum.TryParse<FlowNodeType>(data["type"]?.ToString(), out var type))
                Type = type;

            if (data.ContainsKey("x") && float.TryParse(data["x"]?.ToString(), out var x))
                PositionX = x;

            if (data.ContainsKey("y") && float.TryParse(data["y"]?.ToString(), out var y))
                PositionY = y;

            if (data.ContainsKey("valid") && bool.TryParse(data["valid"]?.ToString(), out var valid))
                Valid = valid;

            if (data.ContainsKey("skip") && bool.TryParse(data["skip"]?.ToString(), out var skip))
                Skip = skip;

            if (data.ContainsKey("description"))
                Description = data["description"]?.ToString();

            if (data.ContainsKey("iconPath"))
                IconPath = data["iconPath"]?.ToString();

            if (data.ContainsKey("iconType"))
                IconType = data["iconType"]?.ToString();

            if (data.ContainsKey("status") && Enum.TryParse<NodeStatus>(data["status"]?.ToString(), out var status))
                Status = status;

            // 设置Settings
            if (data.ContainsKey("settings") && data["settings"] is Dictionary<string, object> settings)
            {
                Settings = new Dictionary<string, object>(settings);
            }

            // 设置Properties
            if (data.ContainsKey("properties") && data["properties"] is Dictionary<string, object> properties)
            {
                Properties = new Dictionary<string, object>(properties);
            }
        }

        #endregion
    }
}
