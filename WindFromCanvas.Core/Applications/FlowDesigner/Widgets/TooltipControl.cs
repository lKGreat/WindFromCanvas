using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 工具提示控件（用于节点悬停显示详细信息）
    /// </summary>
    public class TooltipControl : Form
    {
        private Label _titleLabel;
        private Label _descriptionLabel;
        private Label _typeLabel;
        private Timer _showTimer;
        private const int ShowDelay = 500; // 延迟500ms显示

        public TooltipControl()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(15, 23, 42); // Activepieces深色背景
            this.ForeColor = Color.FromArgb(248, 250, 252);
            this.Padding = new Padding(12, 8, 12, 8);
            this.AutoSize = true;
            this.StartPosition = FormStartPosition.Manual;

            _showTimer = new Timer();
            _showTimer.Interval = ShowDelay;
            _showTimer.Tick += ShowTimer_Tick;
        }

        private void InitializeComponent()
        {
            _titleLabel = new Label
            {
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                AutoSize = true,
                Location = new Point(12, 8)
            };

            _typeLabel = new Label
            {
                Font = new Font("Microsoft YaHei UI", 8),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(12, 28)
            };

            _descriptionLabel = new Label
            {
                Font = new Font("Microsoft YaHei UI", 8),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Location = new Point(12, 48),
                MaximumSize = new Size(300, 0)
            };

            this.Controls.Add(_titleLabel);
            this.Controls.Add(_typeLabel);
            this.Controls.Add(_descriptionLabel);
        }

        /// <summary>
        /// 显示节点信息
        /// </summary>
        public void ShowNodeInfo(FlowNode node, Point screenLocation)
        {
            if (node?.Data == null)
            {
                Hide();
                return;
            }

            _titleLabel.Text = node.Data.DisplayName ?? node.Data.Name ?? "节点";
            _typeLabel.Text = $"类型: {node.Data.Type}";
            _descriptionLabel.Text = node.Data.Description ?? "无描述";

            // 调整位置（在鼠标右下方）
            this.Location = new Point(screenLocation.X + 10, screenLocation.Y + 10);

            // 延迟显示
            _showTimer.Start();
        }

        private void ShowTimer_Tick(object sender, EventArgs e)
        {
            _showTimer.Stop();
            if (!this.Visible)
            {
                this.Show();
            }
        }

        /// <summary>
        /// 隐藏工具提示
        /// </summary>
        public new void Hide()
        {
            _showTimer.Stop();
            base.Hide();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // 绘制圆角边框
            using (var pen = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}
