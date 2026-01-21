using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Templates
{
    /// <summary>
    /// 节点模板（预定义配置）
    /// </summary>
    public class NodeTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public FlowNodeType NodeType { get; set; }
        public Dictionary<string, object> DefaultSettings { get; set; }
        public string IconPath { get; set; }
        public string Category { get; set; }

        public NodeTemplate()
        {
            DefaultSettings = new Dictionary<string, object>();
        }

        /// <summary>
        /// 从模板创建节点数据
        /// </summary>
        public FlowNodeData CreateNodeData(string nodeName)
        {
            return new FlowNodeData
            {
                Name = nodeName,
                DisplayName = this.Name,
                Type = this.NodeType,
                Description = this.Description,
                Settings = new Dictionary<string, object>(this.DefaultSettings),
                IconPath = this.IconPath
            };
        }
    }
}
