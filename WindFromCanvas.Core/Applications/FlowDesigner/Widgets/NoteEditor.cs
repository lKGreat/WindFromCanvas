using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 笔记编辑器（内嵌富文本编辑）
    /// </summary>
    public class NoteEditor : TextBox
    {
        private NoteNode _noteNode;
        private bool _isEditing;

        public NoteEditor(NoteNode noteNode)
        {
            _noteNode = noteNode;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Multiline = true;
            this.WordWrap = true;
            this.ScrollBars = ScrollBars.Vertical;
            this.BorderStyle = BorderStyle.None;
            this.BackColor = Color.Transparent;
            this.ForeColor = ThemeManager.Instance.CurrentTheme.Foreground;
            this.Font = new Font("Microsoft YaHei UI", 9);
            
            if (_noteNode?.Data != null)
            {
                this.Text = _noteNode.Data.Content ?? "";
            }

            this.KeyDown += NoteEditor_KeyDown;
            this.LostFocus += NoteEditor_LostFocus;
            this.TextChanged += NoteEditor_TextChanged;
        }

        private void NoteEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelEdit();
            }
            else if (e.KeyCode == Keys.Enter && e.Control)
            {
                // Ctrl+Enter 保存
                FinishEdit();
            }
        }

        private void NoteEditor_LostFocus(object sender, EventArgs e)
        {
            FinishEdit();
        }

        private void NoteEditor_TextChanged(object sender, EventArgs e)
        {
            // 自动保存（实时更新）
            if (_noteNode?.Data != null && _isEditing)
            {
                _noteNode.Data.Content = this.Text;
                // 触发重绘（通过父控件）
                this.Parent?.Invalidate();
            }
        }

        public void StartEdit()
        {
            _isEditing = true;
            this.Focus();
            this.SelectAll();
        }

        private void FinishEdit()
        {
            if (_noteNode?.Data != null)
            {
                _noteNode.Data.Content = this.Text;
            }
            _isEditing = false;
        }

        private void CancelEdit()
        {
            if (_noteNode?.Data != null)
            {
                this.Text = _noteNode.Data.Content ?? "";
            }
            _isEditing = false;
            this.Parent?.Focus();
        }
    }
}
