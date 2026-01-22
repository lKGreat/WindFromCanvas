using System;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins
{
    /// <summary>
    /// 流程插件接口
    /// </summary>
    public interface IFlowPlugin
    {
        /// <summary>
        /// 插件名称（唯一标识）
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// 初始化插件
        /// </summary>
        void Initialize(IPluginContext context);

        /// <summary>
        /// 渲染插件UI（可选，用于在画布上绘制自定义内容）
        /// </summary>
        void Render(Graphics g, RectangleF viewport);

        /// <summary>
        /// 销毁插件（清理资源）
        /// </summary>
        void Destroy();
    }
}
