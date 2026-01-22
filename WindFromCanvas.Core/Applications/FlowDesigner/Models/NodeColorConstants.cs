using System;
using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 节点颜色标准常量
    /// 统一管理所有节点类型的标准颜色，确保视觉设计的一致性
    /// </summary>
    public static class NodeColorConstants
    {
        #region 基础节点颜色

        /// <summary>
        /// Start节点背景色（绿色）
        /// 标准：#4CAF50 (76, 175, 80)
        /// </summary>
        public static readonly Color StartGreen = Color.FromArgb(76, 175, 80);

        /// <summary>
        /// Start节点边框色（深绿色）
        /// </summary>
        public static readonly Color StartGreenBorder = Color.FromArgb(56, 142, 60);

        /// <summary>
        /// End节点背景色（红色）
        /// 标准：#E53935 (229, 57, 53)
        /// </summary>
        public static readonly Color EndRed = Color.FromArgb(229, 57, 53);

        /// <summary>
        /// End节点边框色（深红色）
        /// </summary>
        public static readonly Color EndRedBorder = Color.FromArgb(198, 40, 40);

        /// <summary>
        /// Process节点边框色（蓝色）
        /// 标准：#2196F3 (33, 150, 243)
        /// </summary>
        public static readonly Color ProcessBlue = Color.FromArgb(33, 150, 243);

        /// <summary>
        /// Decision节点边框色（黄色）
        /// 标准：#FFC107 (255, 193, 7)
        /// </summary>
        public static readonly Color DecisionYellow = Color.FromArgb(255, 193, 7);

        /// <summary>
        /// Loop节点边框色（紫色）
        /// 标准：#9C27B0 (156, 39, 176)
        /// </summary>
        public static readonly Color LoopPurple = Color.FromArgb(156, 39, 176);

        /// <summary>
        /// Code节点边框色（青色）
        /// 标准：#009688 (0, 150, 136)
        /// </summary>
        public static readonly Color CodeCyan = Color.FromArgb(0, 150, 136);

        /// <summary>
        /// Piece节点边框色（棕色）
        /// 标准：#795548 (121, 85, 72)
        /// </summary>
        public static readonly Color PieceBrown = Color.FromArgb(121, 85, 72);

        #endregion

        #region BPMN事件节点颜色

        /// <summary>
        /// BPMN开始事件填充色（淡绿色）
        /// </summary>
        public static readonly Color BpmnStartEventFill = Color.FromArgb(200, 230, 201);

        /// <summary>
        /// BPMN开始事件边框色（绿色）
        /// </summary>
        public static readonly Color BpmnStartEventBorder = Color.FromArgb(67, 160, 71);

        /// <summary>
        /// BPMN结束事件填充色（淡红色）
        /// </summary>
        public static readonly Color BpmnEndEventFill = Color.FromArgb(255, 205, 210);

        /// <summary>
        /// BPMN结束事件边框色（红色）
        /// </summary>
        public static readonly Color BpmnEndEventBorder = Color.FromArgb(229, 57, 53);

        /// <summary>
        /// BPMN中间事件边框色（橙色）
        /// </summary>
        public static readonly Color BpmnIntermediateEventBorder = Color.FromArgb(255, 152, 0);

        #endregion

        #region BPMN任务节点颜色

        /// <summary>
        /// BPMN用户任务颜色（橙色）
        /// 标准：#FF9800 (255, 152, 0)
        /// </summary>
        public static readonly Color BpmnUserTaskOrange = Color.FromArgb(255, 152, 0);

        /// <summary>
        /// BPMN服务任务颜色（蓝色）
        /// 标准：#2196F3 (33, 150, 243)
        /// </summary>
        public static readonly Color BpmnServiceTaskBlue = Color.FromArgb(33, 150, 243);

        /// <summary>
        /// BPMN脚本任务颜色（紫色）
        /// 标准：#9C27B0 (156, 39, 176)
        /// </summary>
        public static readonly Color BpmnScriptTaskPurple = Color.FromArgb(156, 39, 176);

        /// <summary>
        /// BPMN手动任务颜色（灰色）
        /// </summary>
        public static readonly Color BpmnManualTaskGray = Color.FromArgb(158, 158, 158);

        #endregion

        #region BPMN网关节点颜色

        /// <summary>
        /// BPMN排他网关颜色（黄色）
        /// 标准：#FFC107 (255, 193, 7)
        /// </summary>
        public static readonly Color BpmnExclusiveGatewayYellow = Color.FromArgb(255, 193, 7);

        /// <summary>
        /// BPMN并行网关颜色（绿色）
        /// 标准：#4CAF50 (76, 175, 80)
        /// </summary>
        public static readonly Color BpmnParallelGatewayGreen = Color.FromArgb(76, 175, 80);

        /// <summary>
        /// BPMN包容网关颜色（橙色）
        /// 标准：#FF9800 (255, 152, 0)
        /// </summary>
        public static readonly Color BpmnInclusiveGatewayOrange = Color.FromArgb(255, 152, 0);

        /// <summary>
        /// BPMN事件网关颜色（紫色）
        /// </summary>
        public static readonly Color BpmnEventBasedGatewayPurple = Color.FromArgb(156, 39, 176);

        #endregion

        #region 通用UI颜色

        /// <summary>
        /// 白色背景（节点默认背景）
        /// </summary>
        public static readonly Color WhiteBackground = Color.FromArgb(255, 255, 255);

        /// <summary>
        /// 标准边框颜色（浅灰）
        /// Activepieces标准：#E2E8F0 (226, 232, 240)
        /// </summary>
        public static readonly Color BorderGray = Color.FromArgb(226, 232, 240);

        /// <summary>
        /// 选中边框颜色（Activepieces蓝色）
        /// 标准：#3B82F6 (59, 130, 246)
        /// </summary>
        public static readonly Color SelectedBorderBlue = Color.FromArgb(59, 130, 246);

        /// <summary>
        /// 悬停边框颜色（Activepieces ring颜色）
        /// 标准：#94A3B8 (148, 163, 184)
        /// </summary>
        public static readonly Color HoverBorderGray = Color.FromArgb(148, 163, 184);

        /// <summary>
        /// 文本主色（深灰）
        /// 标准：#0F172A (15, 23, 42)
        /// </summary>
        public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);

        /// <summary>
        /// 文本次要色（中灰）
        /// </summary>
        public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);

        #endregion

        #region 状态指示器颜色

        /// <summary>
        /// 运行中状态（蓝色）
        /// Activepieces标准：#3B82F6 (59, 130, 246)
        /// </summary>
        public static readonly Color StatusRunningBlue = Color.FromArgb(59, 130, 246);

        /// <summary>
        /// 成功状态（绿色）
        /// Activepieces标准：#10B981 (16, 185, 129)
        /// </summary>
        public static readonly Color StatusSuccessGreen = Color.FromArgb(16, 185, 129);

        /// <summary>
        /// 失败状态（红色）
        /// Activepieces标准：#EF4444 (239, 68, 68)
        /// </summary>
        public static readonly Color StatusFailedRed = Color.FromArgb(239, 68, 68);

        /// <summary>
        /// 跳过状态（灰色）
        /// Activepieces标准：#94A3B8 (148, 163, 184)
        /// </summary>
        public static readonly Color StatusSkippedGray = Color.FromArgb(148, 163, 184);

        #endregion

        #region 辅助颜色

        /// <summary>
        /// 图标背景色（浅灰）
        /// </summary>
        public static readonly Color IconBackground = Color.FromArgb(248, 250, 252);

        /// <summary>
        /// 图标边框色
        /// </summary>
        public static readonly Color IconBorder = Color.FromArgb(226, 232, 240);

        /// <summary>
        /// 阴影颜色（半透明黑）
        /// </summary>
        public static readonly Color Shadow = Color.FromArgb(30, 0, 0, 0);

        /// <summary>
        /// 选中阴影颜色（更深）
        /// </summary>
        public static readonly Color SelectedShadow = Color.FromArgb(60, 0, 0, 0);

        #endregion
    }
}
