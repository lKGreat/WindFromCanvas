using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Templates
{
    /// <summary>
    /// 模板管理器（单例）
    /// </summary>
    public class TemplateManager
    {
        private static TemplateManager _instance;
        private List<NodeTemplate> _templates = new List<NodeTemplate>();

        public static TemplateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TemplateManager();
                    _instance.InitializeDefaultTemplates();
                }
                return _instance;
            }
        }

        private TemplateManager()
        {
        }

        /// <summary>
        /// 初始化默认模板
        /// </summary>
        private void InitializeDefaultTemplates()
        {
            // 基础节点模板
            _templates.Add(new NodeTemplate
            {
                Id = "process-basic",
                Name = "基础处理",
                Description = "执行基本处理操作",
                NodeType = FlowNodeType.Process,
                Category = "基础"
            });

            _templates.Add(new NodeTemplate
            {
                Id = "decision-basic",
                Name = "条件判断",
                Description = "根据条件进行分支",
                NodeType = FlowNodeType.Decision,
                Category = "控制"
            });

            _templates.Add(new NodeTemplate
            {
                Id = "loop-basic",
                Name = "循环处理",
                Description = "循环执行操作",
                NodeType = FlowNodeType.Loop,
                Category = "控制"
            });
        }

        /// <summary>
        /// 获取所有模板
        /// </summary>
        public List<NodeTemplate> GetAllTemplates()
        {
            return new List<NodeTemplate>(_templates);
        }

        /// <summary>
        /// 按分类获取模板
        /// </summary>
        public List<NodeTemplate> GetTemplatesByCategory(string category)
        {
            return _templates.Where(t => t.Category == category).ToList();
        }

        /// <summary>
        /// 添加自定义模板
        /// </summary>
        public void AddTemplate(NodeTemplate template)
        {
            if (template != null && !_templates.Any(t => t.Id == template.Id))
            {
                _templates.Add(template);
            }
        }

        /// <summary>
        /// 根据ID获取模板
        /// </summary>
        public NodeTemplate GetTemplate(string id)
        {
            return _templates.FirstOrDefault(t => t.Id == id);
        }
    }
}
