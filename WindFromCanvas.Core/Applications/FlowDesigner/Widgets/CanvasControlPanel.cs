using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 画布底部控制栏（参考Activepieces实现）
    /// </summary>
    public class CanvasControlPanel : Panel
    {
        private FlowDesignerCanvas _canvas;
        private Button _minimapButton;
        private Button _zoomInButton;
        private Button _zoomOutButton;
        private Button _fitToViewButton;
        private Button _grabModeButton;
        private Button _selectModeButton;
        private Button _addNoteButton;
        private Label _zoomLabel;

        public event EventHandler MinimapToggleRequested;
        public event EventHandler ZoomInRequested;
        public event EventHandler ZoomOutRequested;
        public event EventHandler FitToViewRequested;
        public event EventHandler GrabModeRequested;
        public event EventHandler SelectModeRequested;
        public event EventHandler AddNoteRequested;

        public CanvasControlPanel(FlowDesignerCanvas canvas)
        {
            _canvas = canvas;
            InitializeComponent();
            UpdateZoomLabel();
        }

        private void InitializeComponent()
        {
            this.Height = 50;
            this.Dock = DockStyle.Bottom;
            this.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            this.BorderStyle = BorderStyle.FixedSingle;

            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 60 };
            var centerPanel = new Panel { Dock = DockStyle.Fill };
            var rightPanel = new Panel { Dock = DockStyle.Right, Width = 300 };

            // 左侧：小地图按钮
            _minimapButton = CreateControlButton("🗺", "小地图 (Ctrl+M)");
            _minimapButton.Click += (s, e) => MinimapToggleRequested?.Invoke(this, e);
            leftPanel.Controls.Add(_minimapButton);

            // 中间：缩放控制
            _zoomOutButton = CreateControlButton("−", "缩小");
            _zoomOutButton.Click += (s, e) => ZoomOutRequested?.Invoke(this, e);
            
            _zoomInButton = CreateControlButton("+", "放大");
            _zoomInButton.Click += (s, e) => ZoomInRequested?.Invoke(this, e);
            
            _fitToViewButton = CreateControlButton("⛶", "适应视图");
            _fitToViewButton.Click += (s, e) => FitToViewRequested?.Invoke(this, e);

            _zoomLabel = new Label
            {
                Text = "100%",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 9),
                ForeColor = ThemeManager.Instance.CurrentTheme.Foreground
            };

            var separator = new Label
            {
                Text = "|",
                AutoSize = true,
                ForeColor = ThemeManager.Instance.CurrentTheme.Border,
                Font = new Font("Microsoft YaHei UI", 12)
            };

            centerPanel.Controls.Add(_zoomOutButton);
            centerPanel.Controls.Add(_zoomInButton);
            centerPanel.Controls.Add(_fitToViewButton);
            centerPanel.Controls.Add(separator);
            centerPanel.Controls.Add(_zoomLabel);

            ArrangeControlsHorizontally(centerPanel);

            // 右侧：模式切换和笔记
            _grabModeButton = CreateControlButton("✋", "抓取模式");
            _grabModeButton.Click += (s, e) => GrabModeRequested?.Invoke(this, e);
            
            _selectModeButton = CreateControlButton("👆", "选择模式");
            _selectModeButton.Click += (s, e) => SelectModeRequested?.Invoke(this, e);
            
            _addNoteButton = CreateControlButton("📝", "添加笔记");
            _addNoteButton.Click += (s, e) => AddNoteRequested?.Invoke(this, e);

            rightPanel.Controls.Add(_grabModeButton);
            rightPanel.Controls.Add(_selectModeButton);
            rightPanel.Controls.Add(_addNoteButton);

            ArrangeControlsHorizontally(rightPanel);

            this.Controls.Add(leftPanel);
            this.Controls.Add(centerPanel);
            this.Controls.Add(rightPanel);
        }

        private Button CreateControlButton(string text, string tooltip)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(35, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.Instance.CurrentTheme.Background,
                ForeColor = ThemeManager.Instance.CurrentTheme.Foreground,
                Font = new Font("Microsoft YaHei UI", 10)
            };
            button.FlatAppearance.BorderColor = ThemeManager.Instance.CurrentTheme.Border;
            button.FlatAppearance.BorderSize = 1;
            button.MouseEnter += (s, e) => button.BackColor = Color.FromArgb(248, 250, 252);
            button.MouseLeave += (s, e) => button.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, tooltip);
            
            return button;
        }

        private void ArrangeControlsHorizontally(Panel panel)
        {
            int x = 5;
            int y = (panel.Height - 35) / 2;
            
            foreach (Control control in panel.Controls)
            {
                control.Location = new Point(x, y);
                x += control.Width + 5;
            }
        }

        public void UpdateZoomLabel()
        {
            if (_zoomLabel != null && _canvas != null)
            {
                _zoomLabel.Text = $"{(_canvas.ZoomFactor * 100):F0}%";
            }
        }

        public void SetMinimapState(bool visible)
        {
            if (_minimapButton != null)
            {
                _minimapButton.BackColor = visible ? 
                    ThemeManager.Instance.CurrentTheme.Primary : 
                    ThemeManager.Instance.CurrentTheme.Background;
            }
        }

        public void SetPanningMode(bool isGrabMode)
        {
            if (_grabModeButton != null && _selectModeButton != null)
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                _grabModeButton.BackColor = isGrabMode ? theme.Primary : theme.Background;
                _selectModeButton.BackColor = !isGrabMode ? theme.Primary : theme.Background;
            }
        }
    }
}
