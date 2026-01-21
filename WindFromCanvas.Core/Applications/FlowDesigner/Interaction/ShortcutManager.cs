using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Interaction
{
    /// <summary>
    /// 快捷键管理器（匹配 Activepieces shortcuts.ts）
    /// </summary>
    public class ShortcutManager
    {
        private readonly BuilderStateStore _stateStore;
        private readonly Dictionary<Keys, Action> _shortcuts;

        public ShortcutManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _shortcuts = new Dictionary<Keys, Action>();
            InitializeShortcuts();
        }

        private void InitializeShortcuts()
        {
            // Ctrl+C - 复制
            RegisterShortcut(Keys.Control | Keys.C, () =>
            {
                // TODO: 实现复制逻辑
            });

            // Ctrl+V - 粘贴
            RegisterShortcut(Keys.Control | Keys.V, () =>
            {
                // TODO: 实现粘贴逻辑
            });

            // Shift+Delete - 删除
            RegisterShortcut(Keys.Shift | Keys.Delete, () =>
            {
                // TODO: 实现删除逻辑
            });

            // Ctrl+E - 跳过
            RegisterShortcut(Keys.Control | Keys.E, () =>
            {
                // TODO: 实现跳过逻辑
            });

            // Ctrl+M - 小地图
            RegisterShortcut(Keys.Control | Keys.M, () =>
            {
                _stateStore.Canvas.ShowMinimap = !_stateStore.Canvas.ShowMinimap;
            });

            // Escape - 退出拖拽
            RegisterShortcut(Keys.Escape, () =>
            {
                if (_stateStore.Drag.IsDragging)
                {
                    _stateStore.EndDrag();
                }
            });
        }

        /// <summary>
        /// 注册快捷键
        /// </summary>
        public void RegisterShortcut(Keys keys, Action action)
        {
            _shortcuts[keys] = action;
        }

        /// <summary>
        /// 处理按键
        /// </summary>
        public bool HandleKeyPress(Keys keys)
        {
            if (_shortcuts.TryGetValue(keys, out var action))
            {
                action();
                return true;
            }
            return false;
        }
    }
}
