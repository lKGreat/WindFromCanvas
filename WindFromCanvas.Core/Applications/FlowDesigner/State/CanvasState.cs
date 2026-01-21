using System;
using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 画布状态（匹配 Activepieces CanvasState）
    /// </summary>
    public class CanvasState
    {
        /// <summary>
        /// 是否只读
        /// </summary>
        public bool Readonly { get; set; }

        /// <summary>
        /// 隐藏测试组件
        /// </summary>
        public bool HideTestWidget { get; set; }

        /// <summary>
        /// 右侧边栏类型
        /// </summary>
        public RightSideBarType RightSidebar { get; set; }

        /// <summary>
        /// 选中的步骤名称
        /// </summary>
        public string SelectedStep { get; set; }

        /// <summary>
        /// 正在拖拽的步骤名称
        /// </summary>
        public string ActiveDraggingStep { get; set; }

        /// <summary>
        /// 选中的分支索引
        /// </summary>
        public int? SelectedBranchIndex { get; set; }

        /// <summary>
        /// 显示小地图
        /// </summary>
        public bool ShowMinimap { get; set; }

        /// <summary>
        /// 选中的节点列表
        /// </summary>
        public string[] SelectedNodes { get; set; }

        /// <summary>
        /// 平移模式
        /// </summary>
        public PanningMode PanningMode { get; set; }

        /// <summary>
        /// 是否聚焦在列表映射模式输入框内
        /// </summary>
        public bool IsFocusInsideListMapperModeInput { get; set; }

        public CanvasState()
        {
            Readonly = false;
            HideTestWidget = false;
            RightSidebar = RightSideBarType.NONE;
            SelectedStep = null;
            ActiveDraggingStep = null;
            SelectedBranchIndex = null;
            ShowMinimap = false;
            SelectedNodes = new string[0];
            PanningMode = PanningMode.Pan;
            IsFocusInsideListMapperModeInput = false;
        }
    }

    /// <summary>
    /// 右侧边栏类型
    /// </summary>
    public enum RightSideBarType
    {
        NONE,
        PIECE_SETTINGS,
        STEP_SETTINGS
    }

    /// <summary>
    /// 平移模式
    /// </summary>
    public enum PanningMode
    {
        Grab,
        Pan
    }
}
