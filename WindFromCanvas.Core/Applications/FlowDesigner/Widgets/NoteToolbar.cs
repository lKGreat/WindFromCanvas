using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 笔记工具栏（悬停显示，参考Activepieces）
    /// </summary>
    public class NoteToolbar : Panel
    {
        private NoteNode _noteNode;
        private Button _colorButton;
        private Button _deleteButton;
        private Button _copyButton;
        private ContextMenuStrip _colorMenu;

        public event EventHandler ColorChanged;
        public event EventHandler DeleteRequested;
        public event EventHandler CopyRequested;

        public NoteToolbar(NoteNode noteNode)
        {
            _noteNode = noteNode;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(120, 30);
            this.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            this.BorderStyle = BorderStyle.FixedSingle;

            // 颜色选择按钮
            _colorButton = new Button
            {
                Text = "🎨",
                Size = new Size(30, 25),
                Location = new Point(5, 2),
                FlatStyle = FlatStyle.Flat
            };
            _colorButton.Click += ColorButton_Click;
            this.Controls.Add(_colorButton);

            // 复制按钮
            _copyButton = new Button
            {
                Text = "📋",
                Size = new Size(30, 25),
                Location = new Point(40, 2),
                FlatStyle = FlatStyle.Flat
            };
            _copyButton.Click += (s, e) => CopyRequested?.Invoke(this, e);
            this.Controls.Add(_copyButton);

            // 删除按钮
            _deleteButton = new Button
            {
                Text = "🗑",
                Size = new Size(30, 25),
                Location = new Point(75, 2),
                FlatStyle = FlatStyle.Flat
            };
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, e);
            this.Controls.Add(_deleteButton);

            // 创建颜色菜单
            CreateColorMenu();
        }

        private void CreateColorMenu()
        {
            _colorMenu = new ContextMenuStrip();
            var colors = new[]
            {
                (NoteColorVariant.Blue, "蓝色"),
                (NoteColorVariant.Yellow, "黄色"),
                (NoteColorVariant.Green, "绿色"),
                (NoteColorVariant.Pink, "粉色"),
                (NoteColorVariant.Purple, "紫色")
            };

            foreach (var (color, name) in colors)
            {
                var item = new ToolStripMenuItem(name);
                item.Click += (s, e) =>
                {
                    if (_noteNode?.Data != null)
                    {
                        _noteNode.Data.Color = color;
                        ColorChanged?.Invoke(this, e);
                    }
                };
                _colorMenu.Items.Add(item);
            }
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            if (_colorMenu != null)
            {
                _colorMenu.Show(_colorButton, new Point(0, _colorButton.Height));
            }
        }

        /// <summary>
        /// 更新工具栏位置（跟随笔记移动）
        /// </summary>
        public void UpdatePosition(PointF notePosition)
        {
            this.Location = new Point(
                (int)(notePosition.X + _noteNode.NoteWidth - this.Width - 5),
                (int)(notePosition.Y + 5)
            );
        }
    }
}
