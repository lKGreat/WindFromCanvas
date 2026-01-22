using System;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 节点尺寸标准常量
    /// 统一管理所有节点类型的标准尺寸，确保设计规范的一致性
    /// </summary>
    public static class NodeSizeConstants
    {
        #region 基础节点尺寸

        /// <summary>
        /// Start/End节点尺寸（圆形）
        /// 标准：60x60px
        /// </summary>
        public const float StartEndSize = 60f;

        /// <summary>
        /// Process节点宽度（Activepieces标准）
        /// 标准：232px
        /// </summary>
        public const float ProcessWidth = 232f;

        /// <summary>
        /// Process节点高度（Activepieces标准）
        /// 标准：60px
        /// </summary>
        public const float ProcessHeight = 60f;

        /// <summary>
        /// Decision节点宽度（菱形）
        /// 标准：232px
        /// </summary>
        public const float DecisionWidth = 232f;

        /// <summary>
        /// Decision节点高度（菱形）
        /// 标准：60px
        /// </summary>
        public const float DecisionHeight = 60f;

        /// <summary>
        /// Loop节点宽度
        /// 标准：232px
        /// </summary>
        public const float LoopWidth = 232f;

        /// <summary>
        /// Loop节点高度
        /// 标准：60px
        /// </summary>
        public const float LoopHeight = 60f;

        /// <summary>
        /// Code节点宽度
        /// 标准：232px
        /// </summary>
        public const float CodeWidth = 232f;

        /// <summary>
        /// Code节点高度
        /// 标准：60px
        /// </summary>
        public const float CodeHeight = 60f;

        /// <summary>
        /// Piece节点宽度
        /// 标准：232px
        /// </summary>
        public const float PieceWidth = 232f;

        /// <summary>
        /// Piece节点高度
        /// 标准：60px
        /// </summary>
        public const float PieceHeight = 60f;

        #endregion

        #region BPMN节点尺寸

        /// <summary>
        /// BPMN事件节点尺寸（圆形）
        /// 标准：36x36px
        /// </summary>
        public const float BpmnEventSize = 36f;

        /// <summary>
        /// BPMN网关节点尺寸（菱形）
        /// 标准：50x50px
        /// </summary>
        public const float BpmnGatewaySize = 50f;

        /// <summary>
        /// BPMN任务节点宽度
        /// 标准：100px
        /// </summary>
        public const float BpmnTaskWidth = 100f;

        /// <summary>
        /// BPMN任务节点高度
        /// 标准：80px
        /// </summary>
        public const float BpmnTaskHeight = 80f;

        /// <summary>
        /// BPMN子流程默认宽度
        /// 标准：200px（可展开）
        /// </summary>
        public const float BpmnSubProcessWidth = 200f;

        /// <summary>
        /// BPMN子流程默认高度
        /// 标准：150px（可展开）
        /// </summary>
        public const float BpmnSubProcessHeight = 150f;

        #endregion

        #region Group节点尺寸

        /// <summary>
        /// Group节点默认宽度
        /// 标准：300px
        /// </summary>
        public const float GroupWidth = 300f;

        /// <summary>
        /// Group节点默认高度
        /// 标准：200px
        /// </summary>
        public const float GroupHeight = 200f;

        /// <summary>
        /// Group节点折叠后宽度
        /// 标准：200px
        /// </summary>
        public const float GroupCollapsedWidth = 200f;

        /// <summary>
        /// Group节点折叠后高度
        /// 标准：50px
        /// </summary>
        public const float GroupCollapsedHeight = 50f;

        #endregion

        #region 通用尺寸参数

        /// <summary>
        /// 标准圆角半径（Activepieces标准）
        /// </summary>
        public const float CornerRadius = 4f;

        /// <summary>
        /// BPMN任务节点圆角半径
        /// </summary>
        public const float BpmnTaskCornerRadius = 10f;

        /// <summary>
        /// 端口大小
        /// </summary>
        public const float PortSize = 8f;

        /// <summary>
        /// 端口热区大小（用于点击检测）
        /// </summary>
        public const float PortHitSize = 12f;

        /// <summary>
        /// 节点图标大小（Activepieces标准）
        /// </summary>
        public const float IconSize = 24f;

        /// <summary>
        /// 状态指示器大小
        /// </summary>
        public const float StatusIndicatorSize = 16f;

        #endregion
    }
}
