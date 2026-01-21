using System;
using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// 流程版本（匹配 Activepieces FlowVersion 结构）
    /// </summary>
    public class FlowVersion
    {
        /// <summary>
        /// 版本ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 流程ID
        /// </summary>
        public string FlowId { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 触发器
        /// </summary>
        public FlowTrigger Trigger { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public FlowVersionState State { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// 备注列表
        /// </summary>
        public List<Note> Notes { get; set; }

        /// <summary>
        /// 更新者ID
        /// </summary>
        public string UpdatedBy { get; set; }

        /// <summary>
        /// 架构版本
        /// </summary>
        public string SchemaVersion { get; set; }

        /// <summary>
        /// 代理ID列表
        /// </summary>
        public List<string> AgentIds { get; set; }

        /// <summary>
        /// 连接ID列表
        /// </summary>
        public List<string> ConnectionIds { get; set; }

        /// <summary>
        /// 备份文件
        /// </summary>
        public Dictionary<string, string> BackupFiles { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        public FlowVersion()
        {
            Id = Guid.NewGuid().ToString();
            FlowId = Guid.NewGuid().ToString();
            DisplayName = "新流程";
            Trigger = new EmptyTrigger();
            State = FlowVersionState.DRAFT;
            Valid = true;
            Notes = new List<Note>();
            AgentIds = new List<string>();
            ConnectionIds = new List<string>();
            BackupFiles = new Dictionary<string, string>();
            SchemaVersion = "16";
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
