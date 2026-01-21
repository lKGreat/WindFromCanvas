using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 处理节点 - 矩形蓝色节点，输入和输出端口
    /// </summary>
    public class ProcessNode : FlowNode
    {
        public ProcessNode() : base()
        {
            Width = 232f; // Activepieces标准尺寸
            Height = 60f;
            BackgroundColor = Color.FromArgb(255, 255, 255); // 白色背景
            BorderColor = Color.FromArgb(226, 232, 240); // Activepieces边框色
            TextColor = Color.FromArgb(15, 23, 42); // Activepieces前景色
            Draggable = true;
            EnableShadow = true;
        }

        public ProcessNode(FlowNodeData data) : base(data)
        {
            Width = 232f;
            Height = 60f;
            BackgroundColor = Color.FromArgb(255, 255, 255);
            BorderColor = Color.FromArgb(226, 232, 240);
            TextColor = Color.FromArgb(15, 23, 42);
            Draggable = true;
            EnableShadow = true;
        }

        // ProcessNode使用基类的DrawIcon方法，会自动加载默认图标
    }
}
