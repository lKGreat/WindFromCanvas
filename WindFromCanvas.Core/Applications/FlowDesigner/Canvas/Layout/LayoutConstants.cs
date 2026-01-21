namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout
{
    /// <summary>
    /// 布局常量（匹配 Activepieces flow-canvas-utils/consts.ts）
    /// </summary>
    public static class LayoutConstants
    {
        /// <summary>
        /// 步骤之间的垂直间距
        /// </summary>
        public const float VERTICAL_SPACE_BETWEEN_STEPS = 60f;

        /// <summary>
        /// 节点之间的水平间距
        /// </summary>
        public const float HORIZONTAL_SPACE_BETWEEN_NODES = 80f;

        /// <summary>
        /// 弧线长度
        /// </summary>
        public const float ARC_LENGTH = 15f;

        /// <summary>
        /// 步骤和线条之间的垂直间距
        /// </summary>
        public const float VERTICAL_SPACE_BETWEEN_STEP_AND_LINE = 7f;

        /// <summary>
        /// 循环和子节点之间的垂直偏移
        /// </summary>
        public const float VERTICAL_OFFSET_BETWEEN_LOOP_AND_CHILD = VERTICAL_SPACE_BETWEEN_STEPS * 1.5f + 2 * ARC_LENGTH;

        /// <summary>
        /// 路由和子节点之间的垂直偏移
        /// </summary>
        public const float VERTICAL_OFFSET_BETWEEN_ROUTER_AND_CHILD = VERTICAL_OFFSET_BETWEEN_LOOP_AND_CHILD + LABEL_HEIGHT;

        /// <summary>
        /// 标签高度
        /// </summary>
        public const float LABEL_HEIGHT = 30f;

        /// <summary>
        /// 标签垂直内边距
        /// </summary>
        public const float LABEL_VERTICAL_PADDING = 12f;

        /// <summary>
        /// 线条宽度
        /// </summary>
        public const float LINE_WIDTH = 1.5f;

        /// <summary>
        /// 构建器头部高度
        /// </summary>
        public const float BUILDER_HEADER_HEIGHT = 60f;

        /// <summary>
        /// 节点尺寸
        /// </summary>
        public static class NodeSize
        {
            public static readonly System.Drawing.SizeF STEP = new System.Drawing.SizeF(232f, 60f);
            public static readonly System.Drawing.SizeF ADD_BUTTON = new System.Drawing.SizeF(20f, 20f);
            public static readonly System.Drawing.SizeF BIG_ADD_BUTTON = new System.Drawing.SizeF(50f, 50f);
            public static readonly System.Drawing.SizeF LOOP_RETURN_NODE = new System.Drawing.SizeF(232f, 60f);
            public static readonly System.Drawing.SizeF GRAPH_END_WIDGET = new System.Drawing.SizeF(0f, 0f);
        }

        /// <summary>
        /// 缩放范围
        /// </summary>
        public const float MIN_ZOOM = 0.5f;
        public const float MAX_ZOOM = 1.5f;
        public const float DEFAULT_ZOOM = 1.0f;
    }
}
