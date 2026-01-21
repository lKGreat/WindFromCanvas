using System.Collections.Generic;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 选择状态
    /// </summary>
    public class SelectionState
    {
        /// <summary>
        /// 选中的节点ID列表
        /// </summary>
        public List<string> SelectedNodeIds { get; set; }

        /// <summary>
        /// 选中的边缘ID列表
        /// </summary>
        public List<string> SelectedEdgeIds { get; set; }

        /// <summary>
        /// 选择矩形区域
        /// </summary>
        public SelectionRectangle SelectionRectangle { get; set; }

        public SelectionState()
        {
            SelectedNodeIds = new List<string>();
            SelectedEdgeIds = new List<string>();
            SelectionRectangle = null;
        }

        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            SelectedNodeIds.Clear();
            SelectedEdgeIds.Clear();
            SelectionRectangle = null;
        }
    }

    /// <summary>
    /// 选择矩形区域
    /// </summary>
    public class SelectionRectangle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
}
