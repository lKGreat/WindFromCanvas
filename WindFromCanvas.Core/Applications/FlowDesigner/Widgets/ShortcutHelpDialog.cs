using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Utils;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 快捷键帮助对话框
    /// </summary>
    public partial class ShortcutHelpDialog : Form
    {
        private ListView _shortcutList;
        private TextBox _searchBox;
        private ShortcutManager _shortcutManager;

        public ShortcutHelpDialog(ShortcutManager shortcutManager)
        {
            _shortcutManager = shortcutManager;
            InitializeComponent();
            LoadShortcuts();
        }

        private void InitializeComponent()
        {
            this.Text = "快捷键帮助";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 搜索框
            _searchBox = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(this.ClientSize.Width - 24, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "搜索快捷键..."
            };
            _searchBox.Enter += (s, e) => { if (_searchBox.Text == "搜索快捷键...") _searchBox.Text = ""; };
            _searchBox.Leave += (s, e) => { if (string.IsNullOrEmpty(_searchBox.Text)) _searchBox.Text = "搜索快捷键..."; };
            _searchBox.TextChanged += SearchBox_TextChanged;
            this.Controls.Add(_searchBox);

            // 快捷键列表
            _shortcutList = new ListView
            {
                Location = new Point(12, 45),
                Size = new Size(this.ClientSize.Width - 24, this.ClientSize.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _shortcutList.Columns.Add("功能", 200);
            _shortcutList.Columns.Add("快捷键", 250);
            this.Controls.Add(_shortcutList);

            // 关闭按钮
            var closeButton = new Button
            {
                Text = "关闭",
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 75, this.ClientSize.Height - 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(63, 23)
            };
            this.Controls.Add(closeButton);
            this.AcceptButton = closeButton;
        }

        private void LoadShortcuts()
        {
            _shortcutList.Items.Clear();
            
            var shortcuts = _shortcutManager.GetAllShortcuts();
            var categories = new Dictionary<string, List<KeyValuePair<string, Keys>>>
            {
                ["编辑"] = new List<KeyValuePair<string, Keys>>(),
                ["视图"] = new List<KeyValuePair<string, Keys>>(),
                ["其他"] = new List<KeyValuePair<string, Keys>>()
            };

            foreach (var shortcut in shortcuts)
            {
                string category = "其他";
                if (shortcut.Key == FlowDesignerShortcuts.Copy || 
                    shortcut.Key == FlowDesignerShortcuts.Paste || 
                    shortcut.Key == FlowDesignerShortcuts.Delete)
                {
                    category = "编辑";
                }
                else if (shortcut.Key == FlowDesignerShortcuts.Minimap)
                {
                    category = "视图";
                }
                
                categories[category].Add(shortcut);
            }

            foreach (var category in categories)
            {
                if (category.Value.Count > 0)
                {
                    var group = new ListViewGroup(category.Key);
                    _shortcutList.Groups.Add(group);
                    
                    foreach (var shortcut in category.Value)
                    {
                        var item = new ListViewItem(GetShortcutDisplayName(shortcut.Key))
                        {
                            Group = group
                        };
                        item.SubItems.Add(_shortcutManager.GetShortcutDescription(shortcut.Key));
                        _shortcutList.Items.Add(item);
                    }
                }
            }
        }

        private string GetShortcutDisplayName(string name)
        {
            switch (name)
            {
                case FlowDesignerShortcuts.Copy: return "复制";
                case FlowDesignerShortcuts.Paste: return "粘贴";
                case FlowDesignerShortcuts.Delete: return "删除";
                case FlowDesignerShortcuts.Minimap: return "切换小地图";
                case FlowDesignerShortcuts.Skip: return "跳过节点";
                case FlowDesignerShortcuts.ExitDrag: return "取消操作";
                default: return name;
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var searchText = _searchBox.Text.ToLower();
            if (searchText == "搜索快捷键...")
            {
                searchText = "";
            }
            
            foreach (ListViewItem item in _shortcutList.Items)
            {
                var shouldShow = string.IsNullOrEmpty(searchText) || 
                                item.Text.ToLower().Contains(searchText) ||
                                item.SubItems[1].Text.ToLower().Contains(searchText);
                
                if (!shouldShow)
                {
                    _shortcutList.Items.Remove(item);
                }
            }
            
            // 如果搜索框为空，重新加载所有项
            if (string.IsNullOrEmpty(searchText))
            {
                LoadShortcuts();
            }
        }
    }
}
