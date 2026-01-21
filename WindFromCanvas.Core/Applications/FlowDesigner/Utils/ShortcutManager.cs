using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 快捷键管理器
    /// </summary>
    public class ShortcutManager
    {
        private Dictionary<Keys, Action> _shortcuts = new Dictionary<Keys, Action>();
        private Dictionary<string, Keys> _shortcutNames = new Dictionary<string, Keys>();

        /// <summary>
        /// 注册快捷键
        /// </summary>
        public void Register(string name, Keys keys, Action action)
        {
            _shortcuts[keys] = action;
            _shortcutNames[name] = keys;
        }

        /// <summary>
        /// 处理按键事件
        /// </summary>
        public bool HandleKeyDown(Keys keyData)
        {
            if (_shortcuts.TryGetValue(keyData, out var action))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取快捷键描述
        /// </summary>
        public string GetShortcutDescription(string name)
        {
            if (_shortcutNames.TryGetValue(name, out var keys))
            {
                return FormatKeys(keys);
            }
            return string.Empty;
        }

        /// <summary>
        /// 格式化按键显示
        /// </summary>
        private string FormatKeys(Keys keys)
        {
            var parts = new List<string>();
            
            if ((keys & Keys.Control) == Keys.Control)
                parts.Add("Ctrl");
            if ((keys & Keys.Shift) == Keys.Shift)
                parts.Add("Shift");
            if ((keys & Keys.Alt) == Keys.Alt)
                parts.Add("Alt");
            
            var key = keys & ~Keys.Control & ~Keys.Shift & ~Keys.Alt;
            if (key != Keys.None)
            {
                parts.Add(key.ToString());
            }
            
            return string.Join(" + ", parts);
        }

        /// <summary>
        /// 获取所有快捷键
        /// </summary>
        public Dictionary<string, Keys> GetAllShortcuts()
        {
            return new Dictionary<string, Keys>(_shortcutNames);
        }
    }

    /// <summary>
    /// Activepieces标准快捷键定义
    /// </summary>
    public static class FlowDesignerShortcuts
    {
        public const string Copy = "Copy";
        public const string Paste = "Paste";
        public const string Delete = "Delete";
        public const string Minimap = "Minimap";
        public const string Skip = "Skip";
        public const string ExitDrag = "ExitDrag";

        /// <summary>
        /// 初始化标准快捷键
        /// </summary>
        public static void InitializeStandardShortcuts(ShortcutManager manager)
        {
            // Ctrl+C: 复制
            manager.Register(Copy, Keys.Control | Keys.C, null);
            
            // Ctrl+V: 粘贴
            manager.Register(Paste, Keys.Control | Keys.V, null);
            
            // Shift+Delete: 删除
            manager.Register(Delete, Keys.Shift | Keys.Delete, null);
            
            // Ctrl+M: 小地图
            manager.Register(Minimap, Keys.Control | Keys.M, null);
            
            // Ctrl+E: 跳过节点
            manager.Register(Skip, Keys.Control | Keys.E, null);
            
            // Escape: 取消操作
            manager.Register(ExitDrag, Keys.Escape, null);
        }
    }
}
