using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Interaction
{
    /// <summary>
    /// 上下文菜单管理器（匹配 Activepieces CanvasContextMenu）
    /// </summary>
    public class ContextMenuManager
    {
        private readonly BuilderStateStore _stateStore;
        private ContextMenuStrip _contextMenu;

        public ContextMenuManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _contextMenu = new ContextMenuStrip();
            InitializeContextMenu();
        }

        private void InitializeContextMenu()
        {
            // TODO: 实现上下文菜单项
            // 替换、复制、复制、跳过/取消跳过、粘贴在后面、粘贴到循环内、粘贴到分支内、删除
        }

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        public void ShowContextMenu(Control control, Point location)
        {
            _contextMenu.Show(control, location);
        }
    }
}
