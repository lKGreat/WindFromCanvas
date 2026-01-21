using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Interaction
{
    /// <summary>
    /// 剪贴板管理器（匹配 Activepieces bulk-actions.ts）
    /// </summary>
    public class ClipboardManager
    {
        private const string COPY_ACTIONS_FORMAT = "COPY_ACTIONS";

        /// <summary>
        /// 复制动作到剪贴板
        /// </summary>
        public void CopyActions(List<FlowAction> actions)
        {
            var data = new ClipboardData
            {
                type = COPY_ACTIONS_FORMAT,
                actions = actions
            };

            // 使用简单的 JSON 序列化
            var json = SerializeClipboardData(data);
            System.Windows.Forms.Clipboard.SetText(json);
        }

        /// <summary>
        /// 从剪贴板获取动作
        /// </summary>
        public List<FlowAction> GetActionsFromClipboard()
        {
            try
            {
                var text = System.Windows.Forms.Clipboard.GetText();
                if (string.IsNullOrEmpty(text))
                    return new List<FlowAction>();

                var data = DeserializeClipboardData(text);
                if (data?.type == COPY_ACTIONS_FORMAT && data.actions != null)
                {
                    return data.actions;
                }
            }
            catch
            {
                // 忽略错误
            }

            return new List<FlowAction>();
        }

        private string SerializeClipboardData(ClipboardData data)
        {
            // 简化实现：使用 DataContractJsonSerializer
            using (var stream = new System.IO.MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(ClipboardData));
                serializer.WriteObject(stream, data);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private ClipboardData DeserializeClipboardData(string json)
        {
            using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(ClipboardData));
                return (ClipboardData)serializer.ReadObject(stream);
            }
        }

        [System.Runtime.Serialization.DataContract]
        private class ClipboardData
        {
            [System.Runtime.Serialization.DataMember]
            public string type { get; set; }
            
            [System.Runtime.Serialization.DataMember]
            public List<FlowAction> actions { get; set; }
        }
    }
}
