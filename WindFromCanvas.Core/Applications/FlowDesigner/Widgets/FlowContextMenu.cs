using System;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 流程设计器上下文菜单
    /// </summary>
    public class FlowContextMenu : ContextMenuStrip
    {
        public event EventHandler<FlowNodeType> AddNodeRequested;
        public event EventHandler PasteRequested;
        public event EventHandler CopyRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler PropertiesRequested;
        public event EventHandler SkipNodeRequested;

        private ToolStripMenuItem _addProcessItem;
        private ToolStripMenuItem _addDecisionItem;
        private ToolStripMenuItem _addLoopItem;
        private ToolStripMenuItem _pasteItem;
        private ToolStripMenuItem _copyItem;
        private ToolStripMenuItem _deleteItem;
        private ToolStripMenuItem _propertiesItem;
        private ToolStripMenuItem _skipItem;

        public FlowContextMenu()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            // 添加节点子菜单
            var addNodeMenu = new ToolStripMenuItem("添加节点");
            _addProcessItem = new ToolStripMenuItem("处理节点", null, (s, e) => AddNodeRequested?.Invoke(this, FlowNodeType.Process));
            _addDecisionItem = new ToolStripMenuItem("判断节点", null, (s, e) => AddNodeRequested?.Invoke(this, FlowNodeType.Decision));
            _addLoopItem = new ToolStripMenuItem("循环节点", null, (s, e) => AddNodeRequested?.Invoke(this, FlowNodeType.Loop));

            addNodeMenu.DropDownItems.Add(_addProcessItem);
            addNodeMenu.DropDownItems.Add(_addDecisionItem);
            addNodeMenu.DropDownItems.Add(_addLoopItem);

            // 粘贴菜单
            _pasteItem = new ToolStripMenuItem("粘贴", null, (s, e) => PasteRequested?.Invoke(this, EventArgs.Empty));

            this.Items.Add(addNodeMenu);
            this.Items.Add(new ToolStripSeparator());
            this.Items.Add(_pasteItem);
        }

        /// <summary>
        /// 显示画布菜单
        /// </summary>
        public void ShowCanvasMenu(Control control, System.Drawing.Point location)
        {
            _pasteItem.Enabled = Clipboard.FlowClipboard.HasFlowData();
            this.Show(control, location);
        }

        /// <summary>
        /// 显示节点菜单
        /// </summary>
        public void ShowNodeMenu(Control control, System.Drawing.Point location, FlowNode node)
        {
            this.Items.Clear();

            _copyItem = new ToolStripMenuItem("复制", null, (s, e) => CopyRequested?.Invoke(this, EventArgs.Empty));
            _deleteItem = new ToolStripMenuItem("删除", null, (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty));
            _propertiesItem = new ToolStripMenuItem("属性", null, (s, e) => PropertiesRequested?.Invoke(this, EventArgs.Empty));
            _skipItem = new ToolStripMenuItem("跳过节点", null, (s, e) => SkipNodeRequested?.Invoke(this, EventArgs.Empty))
            {
                Checked = node?.Data?.Skip ?? false
            };

            this.Items.Add(_copyItem);
            this.Items.Add(_deleteItem);
            this.Items.Add(new ToolStripSeparator());
            this.Items.Add(_propertiesItem);
            this.Items.Add(_skipItem);

            this.Show(control, location);
        }

        /// <summary>
        /// 显示连接线菜单
        /// </summary>
        public void ShowConnectionMenu(Control control, System.Drawing.Point location)
        {
            this.Items.Clear();

            _deleteItem = new ToolStripMenuItem("删除连接", null, (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty));
            var addNodeItem = new ToolStripMenuItem("添加中间节点", null, (s, e) => AddNodeRequested?.Invoke(this, FlowNodeType.Process));

            this.Items.Add(_deleteItem);
            this.Items.Add(new ToolStripSeparator());
            this.Items.Add(addNodeItem);

            this.Show(control, location);
        }
    }
}
