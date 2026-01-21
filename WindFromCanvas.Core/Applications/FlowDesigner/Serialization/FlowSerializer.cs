using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Serialization
{
    /// <summary>
    /// 流程序列化器（JSON格式，匹配 Activepieces）
    /// </summary>
    public class FlowSerializer
    {
        /// <summary>
        /// 序列化流程版本
        /// </summary>
        public string Serialize(WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion version)
        {
            // 使用 DataContractJsonSerializer 进行序列化
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion));
                serializer.WriteObject(stream, version);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// 反序列化流程版本
        /// </summary>
        public WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion Deserialize(string json)
        {
            // 使用 DataContractJsonSerializer 进行反序列化
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion));
                return (WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion)serializer.ReadObject(stream);
            }
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        public void SaveToFile(WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion version, string filePath)
        {
            var json = Serialize(version);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        public WindFromCanvas.Core.Applications.FlowDesigner.Core.Models.FlowVersion LoadFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return Deserialize(json);
        }

        /// <summary>
        /// 泛型序列化方法
        /// </summary>
        public string Serialize<T>(T obj) where T : class
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, obj);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// 泛型反序列化方法
        /// </summary>
        public T Deserialize<T>(string json) where T : class
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }
    }
}
