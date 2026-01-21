using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Connections;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Clipboard
{
    /// <summary>
    /// 流程剪贴板数据
    /// </summary>
    [DataContract]
    [Serializable]
    public class FlowClipboardData
    {
        [DataMember]
        public List<FlowNodeData> Nodes { get; set; }
        
        [DataMember]
        public List<FlowConnectionData> Connections { get; set; }

        public FlowClipboardData()
        {
            Nodes = new List<FlowNodeData>();
            Connections = new List<FlowConnectionData>();
        }
    }

    /// <summary>
    /// 流程剪贴板管理器
    /// </summary>
    public static class FlowClipboard
    {
        private const string ClipboardFormat = "WindFromCanvas.FlowData";

        /// <summary>
        /// 复制节点到剪贴板
        /// </summary>
        public static void CopyNodes(IEnumerable<FlowNode> nodes, IEnumerable<FlowConnection> connections)
        {
            if (nodes == null || !nodes.Any()) return;

            var data = new FlowClipboardData();

            // 复制节点数据
            foreach (var node in nodes)
            {
                if (node.Data != null)
                {
                    data.Nodes.Add(node.Data.Clone());
                }
            }

            // 复制连接数据（只复制选中节点之间的连接）
            var nodeNames = new HashSet<string>(data.Nodes.Select(n => n.Name));
            if (connections != null)
            {
                foreach (var conn in connections)
                {
                    if (conn.Data != null &&
                        nodeNames.Contains(conn.Data.SourceNode) &&
                        nodeNames.Contains(conn.Data.TargetNode))
                    {
                        data.Connections.Add(new FlowConnectionData
                        {
                            SourceNode = conn.Data.SourceNode,
                            TargetNode = conn.Data.TargetNode,
                            SourcePort = conn.Data.SourcePort,
                            TargetPort = conn.Data.TargetPort,
                            Label = conn.Data.Label,
                            Type = conn.Data.Type
                        });
                    }
                }
            }

            // 序列化为JSON并放入剪贴板
            try
            {
                using (var stream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(FlowClipboardData));
                    serializer.WriteObject(stream, data);
                    var json = Encoding.UTF8.GetString(stream.ToArray());
                    System.Windows.Forms.Clipboard.SetText(json, System.Windows.Forms.TextDataFormat.UnicodeText);
                }
            }
            catch
            {
                // 剪贴板操作失败，忽略
            }
        }

        /// <summary>
        /// 从剪贴板粘贴节点
        /// </summary>
        public static FlowClipboardData PasteNodes()
        {
            try
            {
                var json = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                if (string.IsNullOrEmpty(json)) return null;

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(FlowClipboardData));
                    var data = serializer.ReadObject(stream) as FlowClipboardData;
                    return data;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查剪贴板是否有流程数据
        /// </summary>
        public static bool HasFlowData()
        {
            try
            {
                var json = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                if (string.IsNullOrEmpty(json)) return false;

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(FlowClipboardData));
                    var data = serializer.ReadObject(stream) as FlowClipboardData;
                    return data != null && data.Nodes != null && data.Nodes.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
