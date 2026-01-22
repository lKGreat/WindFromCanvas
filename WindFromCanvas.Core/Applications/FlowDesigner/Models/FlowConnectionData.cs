using System;
using System.Collections.Generic;
using System.Drawing;
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
        /// 源锚点ID（精确指定连接的锚点）
        /// </summary>
        [DataMember]
        public string SourceAnchorId { get; set; }

        /// <summary>
        /// 目标锚点ID（精确指定连接的锚点）
        /// </summary>
        [DataMember]
        public string TargetAnchorId { get; set; }

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

        /// <summary>
        /// 路径点列表（用于自定义路径、A*路由等）
        /// 存储连线的中间路径点坐标
        /// </summary>
        [DataMember]
        public List<PointF> PointsList { get; set; }

        /// <summary>
        /// 连线样式（实线、虚线、动画等）
        /// </summary>
        [DataMember]
        public string LineStyle { get; set; }

        /// <summary>
        /// 连线文本（显示在连线上的文本）
        /// </summary>
        [DataMember]
        public string Text { get; set; }

        /// <summary>
        /// 是否启用自动路由（A*算法）
        /// </summary>
        [DataMember]
        public bool AutoRouting { get; set; }

        public FlowConnectionData()
        {
            Type = FlowConnectionType.StraightLine;
            PointsList = new List<PointF>();
            LineStyle = "solid";
            AutoRouting = false;
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

        #region 数据序列化

        /// <summary>
        /// 获取连线数据（用于序列化和导出）
        /// 包含完整的锚点ID信息
        /// </summary>
        public virtual Dictionary<string, object> GetData()
        {
            var data = new Dictionary<string, object>
            {
                ["sourceNode"] = SourceNode ?? string.Empty,
                ["targetNode"] = TargetNode ?? string.Empty,
                ["type"] = Type.ToString()
            };

            // 添加锚点ID（关键：持久化锚点连接信息）
            if (!string.IsNullOrEmpty(SourceAnchorId))
                data["sourceAnchorId"] = SourceAnchorId;

            if (!string.IsNullOrEmpty(TargetAnchorId))
                data["targetAnchorId"] = TargetAnchorId;

            // 添加端口信息（兼容旧数据）
            if (!string.IsNullOrEmpty(SourcePort))
                data["sourcePort"] = SourcePort;

            if (!string.IsNullOrEmpty(TargetPort))
                data["targetPort"] = TargetPort;

            // 添加标签/条件
            if (!string.IsNullOrEmpty(Label))
                data["label"] = Label;

            // 添加路径点列表
            if (PointsList != null && PointsList.Count > 0)
            {
                var pointsArray = new List<Dictionary<string, float>>();
                foreach (var point in PointsList)
                {
                    pointsArray.Add(new Dictionary<string, float>
                    {
                        ["x"] = point.X,
                        ["y"] = point.Y
                    });
                }
                data["pointsList"] = pointsArray;
            }

            // 添加样式信息
            if (!string.IsNullOrEmpty(LineStyle))
                data["lineStyle"] = LineStyle;

            if (!string.IsNullOrEmpty(Text))
                data["text"] = Text;

            data["autoRouting"] = AutoRouting;

            return data;
        }

        /// <summary>
        /// 从数据字典设置连线属性（用于反序列化和导入）
        /// </summary>
        public virtual void SetData(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            if (data.ContainsKey("sourceNode"))
                SourceNode = data["sourceNode"]?.ToString();

            if (data.ContainsKey("targetNode"))
                TargetNode = data["targetNode"]?.ToString();

            if (data.ContainsKey("sourceAnchorId"))
                SourceAnchorId = data["sourceAnchorId"]?.ToString();

            if (data.ContainsKey("targetAnchorId"))
                TargetAnchorId = data["targetAnchorId"]?.ToString();

            if (data.ContainsKey("sourcePort"))
                SourcePort = data["sourcePort"]?.ToString();

            if (data.ContainsKey("targetPort"))
                TargetPort = data["targetPort"]?.ToString();

            if (data.ContainsKey("label"))
                Label = data["label"]?.ToString();

            if (data.ContainsKey("type") && Enum.TryParse<FlowConnectionType>(data["type"]?.ToString(), out var type))
                Type = type;

            if (data.ContainsKey("lineStyle"))
                LineStyle = data["lineStyle"]?.ToString();

            if (data.ContainsKey("text"))
                Text = data["text"]?.ToString();

            if (data.ContainsKey("autoRouting") && bool.TryParse(data["autoRouting"]?.ToString(), out var autoRouting))
                AutoRouting = autoRouting;

            // 解析路径点列表
            if (data.ContainsKey("pointsList") && data["pointsList"] is List<Dictionary<string, float>> pointsArray)
            {
                PointsList = new List<PointF>();
                foreach (var pointData in pointsArray)
                {
                    if (pointData.ContainsKey("x") && pointData.ContainsKey("y"))
                    {
                        PointsList.Add(new PointF(pointData["x"], pointData["y"]));
                    }
                }
            }
        }

        #endregion

        #region 路径更新

        /// <summary>
        /// 更新连线路径（重新计算路径点）
        /// 当节点位置变化或需要重新路由时调用
        /// </summary>
        /// <param name="sourcePos">源节点锚点位置</param>
        /// <param name="targetPos">目标节点锚点位置</param>
        /// <param name="obstacles">障碍物节点边界列表（用于A*避障）</param>
        public virtual void UpdatePath(PointF sourcePos, PointF targetPos, List<RectangleF> obstacles = null)
        {
            if (!AutoRouting)
            {
                // 不启用自动路由时，使用简单的直线或两点路径
                PointsList = new List<PointF> { sourcePos, targetPos };
                return;
            }

            // 启用自动路由时，后续可以集成A*算法计算路径
            // 这里先提供简单的直角路径实现
            PointsList = CalculateOrthogonalPath(sourcePos, targetPos);
        }

        /// <summary>
        /// 计算直角路径（简单实现，后续可替换为A*）
        /// </summary>
        private List<PointF> CalculateOrthogonalPath(PointF start, PointF end)
        {
            var points = new List<PointF>();
            points.Add(start);

            // 计算中间点（创建直角路径）
            float midX = (start.X + end.X) / 2;
            
            // 添加中间点
            points.Add(new PointF(midX, start.Y));
            points.Add(new PointF(midX, end.Y));

            points.Add(end);
            return points;
        }

        /// <summary>
        /// 清除路径点（恢复为简单连线）
        /// </summary>
        public void ClearPath()
        {
            PointsList?.Clear();
        }

        #endregion
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
