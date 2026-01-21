using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 工具箱面板 - 显示可拖拽的节点类型
    /// </summary>
    public class ToolboxPanel : Panel
    {
        /// <summary>
        /// 节点类型定义
        /// </summary>
        public class NodeTypeItem
        {
            public FlowNodeType Type { get; set; }
            public string DisplayName { get; set; }
            public Color Color { get; set; }
            public Image Icon { get; set; }
        }

        /// <summary>
        /// 节点类型列表
        /// </summary>
        private List<NodeTypeItem> _nodeTypes = new List<NodeTypeItem>();

        /// <summary>
        /// 当前拖拽的节点类型
        /// </summary>
        private FlowNodeType? _draggingNodeType;

        public event EventHandler<FlowNodeType> NodeTypeSelected;

        public ToolboxPanel()
        {
            InitializeNodeTypes();
            SetupUI();
        }

        /// <summary>
        /// 初始化节点类型
        /// </summary>
        private void InitializeNodeTypes()
        {
            _nodeTypes.Add(new NodeTypeItem
            {
                Type = FlowNodeType.Process,
                DisplayName = "处理",
                Color = Color.FromArgb(33, 150, 243)
            });
            _nodeTypes.Add(new NodeTypeItem
            {
                Type = FlowNodeType.Decision,
                DisplayName = "判断",
                Color = Color.FromArgb(255, 193, 7)
            });
            _nodeTypes.Add(new NodeTypeItem
            {
                Type = FlowNodeType.Loop,
                DisplayName = "循环",
                Color = Color.FromArgb(156, 39, 176)
            });
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.AutoScroll = true;
            this.Paint += ToolboxPanel_Paint;
            this.MouseDown += ToolboxPanel_MouseDown;
            this.MouseMove += ToolboxPanel_MouseMove;
            this.MouseUp += ToolboxPanel_MouseUp;
        }

        private void ToolboxPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int y = 10;
            const int itemHeight = 50;
            const int margin = 10;

            foreach (var item in _nodeTypes)
            {
                var rect = new Rectangle(margin, y, this.Width - margin * 2, itemHeight);

                // 绘制背景
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, rect);
                }

                // 绘制颜色指示
                var colorRect = new Rectangle(rect.X + 5, rect.Y + 5, 40, rect.Height - 10);
                using (var brush = new SolidBrush(item.Color))
                {
                    g.FillRectangle(brush, colorRect);
                }

                // 绘制文本
                using (var brush = new SolidBrush(Color.Black))
                using (var font = new Font(SystemFonts.DefaultFont.FontFamily, 10))
                {
                    var textRect = new Rectangle(colorRect.Right + 10, rect.Y, rect.Width - colorRect.Right - 20, rect.Height);
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(item.DisplayName, font, brush, textRect, sf);
                }

                // 绘制边框
                using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
                {
                    g.DrawRectangle(pen, rect);
                }

                y += itemHeight + margin;
            }
        }

        private void ToolboxPanel_MouseDown(object sender, MouseEventArgs e)
        {
            var item = GetItemAt(e.Location);
            if (item != null)
            {
                _draggingNodeType = item.Type;
                this.DoDragDrop(item.Type, DragDropEffects.Copy);
            }
        }

        private void ToolboxPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // 鼠标移动处理
        }

        private void ToolboxPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _draggingNodeType = null;
        }

        /// <summary>
        /// 获取指定位置的节点类型项
        /// </summary>
        private NodeTypeItem GetItemAt(Point location)
        {
            int y = 10;
            const int itemHeight = 50;
            const int margin = 10;

            foreach (var item in _nodeTypes)
            {
                var rect = new Rectangle(margin, y, this.Width - margin * 2, itemHeight);
                if (rect.Contains(location))
                {
                    return item;
                }
                y += itemHeight + margin;
            }

            return null;
        }

        /// <summary>
        /// 创建节点实例
        /// </summary>
        public FlowNode CreateNode(FlowNodeType type, PointF position)
        {
            var nodeData = new FlowNodeData
            {
                Name = Guid.NewGuid().ToString(),
                DisplayName = GetDisplayName(type),
                Type = type,
                Position = position
            };

            return CreateNodeFromData(nodeData);
        }

        /// <summary>
        /// 从数据创建节点
        /// </summary>
        public FlowNode CreateNodeFromData(FlowNodeData data)
        {
            switch (data.Type)
            {
                case FlowNodeType.Start:
                    return new StartNode(data);
                case FlowNodeType.Process:
                    return new ProcessNode(data);
                case FlowNodeType.Decision:
                    return new DecisionNode(data);
                case FlowNodeType.Loop:
                    return new LoopNode(data);
                case FlowNodeType.End:
                    return new EndNode(data);
                default:
                    return new ProcessNode(data);
            }
        }

        private string GetDisplayName(FlowNodeType type)
        {
            switch (type)
            {
                case FlowNodeType.Start:
                    return "开始";
                case FlowNodeType.Process:
                    return "处理";
                case FlowNodeType.Decision:
                    return "判断";
                case FlowNodeType.Loop:
                    return "循环";
                case FlowNodeType.End:
                    return "结束";
                default:
                    return "节点";
            }
        }
    }
}
