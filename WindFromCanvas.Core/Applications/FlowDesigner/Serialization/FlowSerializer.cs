using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Serialization
{
    /// <summary>
    /// 流程序列化器
    /// </summary>
    public static class FlowSerializer
    {
        /// <summary>
        /// 序列化流程文档为JSON字符串
        /// </summary>
        public static string SerializeToJson(FlowDocument document)
        {
            if (document == null) return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(FlowDocument));
                    serializer.WriteObject(stream, document);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"序列化流程文档失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化流程文档
        /// </summary>
        public static FlowDocument DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(FlowDocument));
                    return serializer.ReadObject(stream) as FlowDocument;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"反序列化流程文档失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存流程文档到文件
        /// </summary>
        public static void SaveToFile(FlowDocument document, string filePath)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("文件路径不能为空", nameof(filePath));

            var json = SerializeToJson(document);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            document.ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// 从文件加载流程文档
        /// </summary>
        public static FlowDocument LoadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("文件路径不能为空", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("文件不存在", filePath);

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return DeserializeFromJson(json);
        }
    }
}
