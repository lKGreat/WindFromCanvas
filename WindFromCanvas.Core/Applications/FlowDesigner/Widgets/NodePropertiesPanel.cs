using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 节点属性面板 - 显示和编辑节点属性
    /// </summary>
    public class NodePropertiesPanel : Panel
    {
        private FlowNode _selectedNode;
        private Label _lblName;
        private TextBox _txtName;
        private Label _lblDisplayName;
        private TextBox _txtDisplayName;
        private Label _lblType;
        private Label _lblTypeValue;
        private Label _lblDescription;
        private TextBox _txtDescription;
        private CheckBox _chkSkip;
        private Label _lblValid;

        public event EventHandler<FlowNode> NodePropertyChanged;

        public NodePropertiesPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.AutoScroll = true;
            this.Padding = new Padding(10);

            int y = 10;

            // 名称标签和文本框
            _lblName = new Label
            {
                Text = "名称:",
                Location = new Point(10, y),
                Size = new Size(80, 20),
                Parent = this
            };
            _txtName = new TextBox
            {
                Location = new Point(100, y),
                Size = new Size(this.Width - 110, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = this
            };
            _txtName.TextChanged += TxtName_TextChanged;
            y += 30;

            // 显示名称标签和文本框
            _lblDisplayName = new Label
            {
                Text = "显示名称:",
                Location = new Point(10, y),
                Size = new Size(80, 20),
                Parent = this
            };
            _txtDisplayName = new TextBox
            {
                Location = new Point(100, y),
                Size = new Size(this.Width - 110, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = this
            };
            _txtDisplayName.TextChanged += TxtDisplayName_TextChanged;
            y += 30;

            // 类型标签
            _lblType = new Label
            {
                Text = "类型:",
                Location = new Point(10, y),
                Size = new Size(80, 20),
                Parent = this
            };
            _lblTypeValue = new Label
            {
                Location = new Point(100, y),
                Size = new Size(this.Width - 110, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = this
            };
            y += 30;

            // 描述标签和文本框
            _lblDescription = new Label
            {
                Text = "描述:",
                Location = new Point(10, y),
                Size = new Size(80, 20),
                Parent = this
            };
            _txtDescription = new TextBox
            {
                Location = new Point(100, y),
                Size = new Size(this.Width - 110, 60),
                Multiline = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = this
            };
            _txtDescription.TextChanged += TxtDescription_TextChanged;
            y += 70;

            // 跳过复选框
            _chkSkip = new CheckBox
            {
                Text = "跳过此节点",
                Location = new Point(10, y),
                Size = new Size(150, 20),
                Parent = this
            };
            _chkSkip.CheckedChanged += ChkSkip_CheckedChanged;
            y += 30;

            // 有效性标签
            _lblValid = new Label
            {
                Text = "状态: 有效",
                Location = new Point(10, y),
                Size = new Size(this.Width - 20, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.Green,
                Parent = this
            };
        }

        /// <summary>
        /// 设置选中的节点
        /// </summary>
        public void SetSelectedNode(FlowNode node)
        {
            _selectedNode = node;
            UpdateUI();
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            if (_selectedNode == null || _selectedNode.Data == null)
            {
                _txtName.Text = "";
                _txtDisplayName.Text = "";
                _lblTypeValue.Text = "";
                _txtDescription.Text = "";
                _chkSkip.Checked = false;
                _lblValid.Text = "状态: 无选中";
                _lblValid.ForeColor = Color.Gray;
                return;
            }

            var data = _selectedNode.Data;
            _txtName.Text = data.Name ?? "";
            _txtDisplayName.Text = data.DisplayName ?? "";
            _lblTypeValue.Text = GetTypeDisplayName(data.Type);
            _txtDescription.Text = data.Description ?? "";
            _chkSkip.Checked = data.Skip;
            _lblValid.Text = data.Valid ? "状态: 有效" : "状态: 无效";
            _lblValid.ForeColor = data.Valid ? Color.Green : Color.Red;
        }

        private string GetTypeDisplayName(FlowNodeType type)
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
                    return type.ToString();
            }
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedNode?.Data != null)
            {
                _selectedNode.Data.Name = _txtName.Text;
                NodePropertyChanged?.Invoke(this, _selectedNode);
            }
        }

        private void TxtDisplayName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedNode?.Data != null)
            {
                _selectedNode.Data.DisplayName = _txtDisplayName.Text;
                NodePropertyChanged?.Invoke(this, _selectedNode);
            }
        }

        private void TxtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_selectedNode?.Data != null)
            {
                _selectedNode.Data.Description = _txtDescription.Text;
                NodePropertyChanged?.Invoke(this, _selectedNode);
            }
        }

        private void ChkSkip_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedNode?.Data != null)
            {
                _selectedNode.Data.Skip = _chkSkip.Checked;
                NodePropertyChanged?.Invoke(this, _selectedNode);
            }
        }
    }
}
