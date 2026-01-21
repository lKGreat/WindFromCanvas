namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums
{
    /// <summary>
    /// 流程操作类型枚举
    /// </summary>
    public enum FlowOperationType
    {
        /// <summary>
        /// 锁定并发布
        /// </summary>
        LOCK_AND_PUBLISH,

        /// <summary>
        /// 更改状态
        /// </summary>
        CHANGE_STATUS,

        /// <summary>
        /// 锁定流程
        /// </summary>
        LOCK_FLOW,

        /// <summary>
        /// 更改文件夹
        /// </summary>
        CHANGE_FOLDER,

        /// <summary>
        /// 更改名称
        /// </summary>
        CHANGE_NAME,

        /// <summary>
        /// 移动动作
        /// </summary>
        MOVE_ACTION,

        /// <summary>
        /// 导入流程
        /// </summary>
        IMPORT_FLOW,

        /// <summary>
        /// 更新触发器
        /// </summary>
        UPDATE_TRIGGER,

        /// <summary>
        /// 添加动作
        /// </summary>
        ADD_ACTION,

        /// <summary>
        /// 更新动作
        /// </summary>
        UPDATE_ACTION,

        /// <summary>
        /// 删除动作
        /// </summary>
        DELETE_ACTION,

        /// <summary>
        /// 复制动作
        /// </summary>
        DUPLICATE_ACTION,

        /// <summary>
        /// 使用为草稿
        /// </summary>
        USE_AS_DRAFT,

        /// <summary>
        /// 删除分支
        /// </summary>
        DELETE_BRANCH,

        /// <summary>
        /// 添加分支
        /// </summary>
        ADD_BRANCH,

        /// <summary>
        /// 复制分支
        /// </summary>
        DUPLICATE_BRANCH,

        /// <summary>
        /// 设置跳过动作
        /// </summary>
        SET_SKIP_ACTION,

        /// <summary>
        /// 更新元数据
        /// </summary>
        UPDATE_METADATA,

        /// <summary>
        /// 移动分支
        /// </summary>
        MOVE_BRANCH,

        /// <summary>
        /// 保存示例数据
        /// </summary>
        SAVE_SAMPLE_DATA,

        /// <summary>
        /// 更新节省分钟数
        /// </summary>
        UPDATE_MINUTES_SAVED,

        /// <summary>
        /// 更新所有者
        /// </summary>
        UPDATE_OWNER,

        /// <summary>
        /// 更新备注
        /// </summary>
        UPDATE_NOTE,

        /// <summary>
        /// 删除备注
        /// </summary>
        DELETE_NOTE,

        /// <summary>
        /// 添加备注
        /// </summary>
        ADD_NOTE
    }
}
