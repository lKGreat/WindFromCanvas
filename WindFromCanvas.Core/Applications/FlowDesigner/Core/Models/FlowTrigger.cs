using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// 流程触发器基类
    /// </summary>
    public abstract class FlowTrigger : IStep
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public FlowTriggerType Type { get; set; }
        public bool Valid { get; set; }
        public bool Skip { get; set; }
        public FlowAction NextAction { get; set; }

        protected FlowTrigger()
        {
            Name = "trigger";
            Valid = true;
            Skip = false;
        }
    }

    /// <summary>
    /// 空触发器
    /// </summary>
    public class EmptyTrigger : FlowTrigger
    {
        public EmptyTrigger()
        {
            Type = FlowTriggerType.EMPTY;
            DisplayName = "触发器";
        }
    }

    /// <summary>
    /// 组件触发器
    /// </summary>
    public class PieceTrigger : FlowTrigger
    {
        public PieceTriggerSettings Settings { get; set; }

        public PieceTrigger()
        {
            Type = FlowTriggerType.PIECE_TRIGGER;
        }
    }

    /// <summary>
    /// 组件触发器设置
    /// </summary>
    public class PieceTriggerSettings
    {
        public Dictionary<string, PropertySettings> PropertySettings { get; set; }
        public string PieceName { get; set; }
        public string PieceVersion { get; set; }
        public string TriggerName { get; set; }
        public Dictionary<string, object> Input { get; set; }
        public string CustomLogoUrl { get; set; }

        public PieceTriggerSettings()
        {
            PropertySettings = new Dictionary<string, PropertySettings>();
            Input = new Dictionary<string, object>();
        }
    }
}
