using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 7.4 画布底部控制栏 - 缩放滑块、适应视图、平移模式、全屏切换
    /// </summary>
    public class CanvasControlPanel : Panel
    {
        #region 字段

        private FlowDesignerCanvas _canvas;
        private Panel _leftPanel;
        private Panel _centerPanel;
        private Panel _rightPanel;

        // 左侧控件
        private Button _minimapButton;

        // 中间控件
        private Button _zoomOutButton;
        private TrackBar _zoomSlider;
        private Button _zoomInButton;
        private Label _zoomLabel;
        private Button _fitToViewButton;

        // 右侧控件
        private Button _grabModeButton;
        private Button _selectModeButton;
        private Button _addNoteButton;
        private Button _fullscreenButton;

        // 状态
        private bool _isGrabMode = false;
        private bool _isFullscreen = false;
        private bool _minimapVisible = true;

        // 配置
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 3.0f;
        private const float ZoomStep = 0.1f;
        private const int ControlHeight = 48;

        #endregion

        #region 事件

        public event EventHandler MinimapToggleRequested;
        public event EventHandler ZoomInRequested;
        public event EventHandler ZoomOutRequested;
        public event EventHandler<float> ZoomChanged;
        public event EventHandler FitToViewRequested;
        public event EventHandler<bool> ModeChanged; // true = grab mode, false = select mode
        public event EventHandler AddNoteRequested;
        public event EventHandler<bool> FullscreenChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        public float ZoomFactor
        {
            get => _canvas?.ZoomFactor ?? 1f;
            set => SetZoom(value);
        }

        /// <summary>
        /// 是否为抓取模式
        /// </summary>
        public bool IsGrabMode
        {
            get => _isGrabMode;
            set
            {
                if (_isGrabMode != value)
                {
                    _isGrabMode = value;
                    UpdateModeButtons();
                    ModeChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool IsFullscreen
        {
            get => _isFullscreen;
            set
            {
                if (_isFullscreen != value)
                {
                    _isFullscreen = value;
                    UpdateFullscreenButton();
                    FullscreenChanged?.Invoke(this, value);
                }
            }
        }

        #endregion

        #region 构造

        public CanvasControlPanel(FlowDesignerCanvas canvas)
        {
            _canvas = canvas;
            InitializeComponent();
            UpdateZoomControls();
        }

        public CanvasControlPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var theme = ThemeManager.Instance.CurrentTheme;

            this.Height = ControlHeight;
            this.Dock = DockStyle.Bottom;
            this.BackColor = theme.NodeBackground;
            this.Padding = new Padding(8, 4, 8, 4);

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);

            // 创建三个面板区域
            _leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 50,
                BackColor = Color.Transparent
            };

            _centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 180,
                BackColor = Color.Transparent
            };

            this.Controls.Add(_centerPanel);
            this.Controls.Add(_rightPanel);
            this.Controls.Add(_leftPanel);

            // 初始化各区域控件
            InitializeLeftPanel(theme);
            InitializeCenterPanel(theme);
            InitializeRightPanel(theme);

            // 主题变更处理
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;

            // 绘制事件
            this.Paint += CanvasControlPanel_Paint;
        }

        #endregion

        #region 面板初始化

        private void InitializeLeftPanel(ThemeConfig theme)
        {
            // 小地图切换按钮
            _minimapButton = CreateIconButton("▣", "小地图 (Ctrl+M)", theme);
            _minimapButton.Location = new Point(4, (ControlHeight - 32) / 2);
            _minimapButton.Click += (s, e) =>
            {
                _minimapVisible = !_minimapVisible;
                UpdateMinimapButton();
                MinimapToggleRequested?.Invoke(this, e);
            };
            _leftPanel.Controls.Add(_minimapButton);
        }

        private void InitializeCenterPanel(ThemeConfig theme)
        {
            // 7.4.1 缩放滑块组
            var zoomGroup = new Panel
            {
                Size = new Size(280, 36),
                Location = new Point(0, 6),
                BackColor = Color.Transparent
            };

            // 缩小按钮
            _zoomOutButton = CreateIconButton("−", "缩小 (Ctrl+-)", theme);
            _zoomOutButton.Location = new Point(0, 2);
            _zoomOutButton.Click += (s, e) =>
            {
                SetZoom(ZoomFactor - ZoomStep);
                ZoomOutRequested?.Invoke(this, e);
            };
            zoomGroup.Controls.Add(_zoomOutButton);

            // 缩放滑块
            _zoomSlider = new TrackBar
            {
                Minimum = (int)(MinZoom * 100),
                Maximum = (int)(MaxZoom * 100),
                Value = 100,
                SmallChange = (int)(ZoomStep * 100),
                LargeChange = (int)(ZoomStep * 100 * 2),
                TickStyle = TickStyle.None,
                Size = new Size(120, 32),
                Location = new Point(40, 2)
            };
            _zoomSlider.ValueChanged += (s, e) =>
            {
                if (!_isUpdatingSlider)
                {
                    SetZoom(_zoomSlider.Value / 100f);
                }
            };
            zoomGroup.Controls.Add(_zoomSlider);

            // 放大按钮
            _zoomInButton = CreateIconButton("+", "放大 (Ctrl++)", theme);
            _zoomInButton.Location = new Point(165, 2);
            _zoomInButton.Click += (s, e) =>
            {
                SetZoom(ZoomFactor + ZoomStep);
                ZoomInRequested?.Invoke(this, e);
            };
            zoomGroup.Controls.Add(_zoomInButton);

            // 7.4.3 缩放百分比显示
            _zoomLabel = new Label
            {
                Text = "100%",
                Size = new Size(50, 32),
                Location = new Point(205, 2),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9),
                ForeColor = theme.TextPrimary,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _zoomLabel.Click += (s, e) =>
            {
                // 点击重置为100%
                SetZoom(1f);
            };
            var zoomLabelTooltip = new ToolTip();
            zoomLabelTooltip.SetToolTip(_zoomLabel, "点击重置为100%");
            zoomGroup.Controls.Add(_zoomLabel);

            // 7.4.2 适应视图按钮
            _fitToViewButton = CreateIconButton("⛶", "适应视图 (Ctrl+0)", theme);
            _fitToViewButton.Location = new Point(258, 2);
            _fitToViewButton.Click += (s, e) =>
            {
                FitToViewRequested?.Invoke(this, e);
            };
            zoomGroup.Controls.Add(_fitToViewButton);

            // 居中缩放组
            _centerPanel.Resize += (s, e) =>
            {
                zoomGroup.Location = new Point((_centerPanel.Width - zoomGroup.Width) / 2, 6);
            };
            _centerPanel.Controls.Add(zoomGroup);
        }

        private void InitializeRightPanel(ThemeConfig theme)
        {
            int x = _rightPanel.Width - 40;
            int y = (ControlHeight - 32) / 2;

            // 7.4.5 全屏切换按钮
            _fullscreenButton = CreateIconButton("⛶", "全屏 (F11)", theme);
            _fullscreenButton.Location = new Point(x, y);
            _fullscreenButton.Click += (s, e) => IsFullscreen = !IsFullscreen;
            _rightPanel.Controls.Add(_fullscreenButton);
            x -= 40;

            // 添加笔记按钮
            _addNoteButton = CreateIconButton("📝", "添加笔记 (N)", theme);
            _addNoteButton.Location = new Point(x, y);
            _addNoteButton.Click += (s, e) => AddNoteRequested?.Invoke(this, e);
            _rightPanel.Controls.Add(_addNoteButton);
            x -= 44;

            // 分隔线
            var separator = new Panel
            {
                Size = new Size(1, 24),
                Location = new Point(x, y + 4),
                BackColor = theme.Border
            };
            _rightPanel.Controls.Add(separator);
            x -= 8;

            // 7.4.4 平移模式切换 - 选择模式按钮
            _selectModeButton = CreateIconButton("↖", "选择模式 (V)", theme);
            _selectModeButton.Location = new Point(x - 32, y);
            _selectModeButton.Click += (s, e) => IsGrabMode = false;
            _rightPanel.Controls.Add(_selectModeButton);

            // 抓取模式按钮
            _grabModeButton = CreateIconButton("✋", "抓取模式 (H)", theme);
            _grabModeButton.Location = new Point(x - 72, y);
            _grabModeButton.Click += (s, e) => IsGrabMode = true;
            _rightPanel.Controls.Add(_grabModeButton);

            UpdateModeButtons();
        }

        private Button CreateIconButton(string text, string tooltip, ThemeConfig theme)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(32, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = theme.TextPrimary,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, theme.Primary);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, theme.Primary);

            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, tooltip);

            return button;
        }

        #endregion

        #region 缩放控制

        private bool _isUpdatingSlider = false;

        private void SetZoom(float zoom)
        {
            zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));

            if (_canvas != null)
            {
                // 以画布中心为缩放中心
                var centerX = _canvas.Width / 2f;
                var centerY = _canvas.Height / 2f;

                _canvas.SetZoom(zoom, new PointF(centerX, centerY));
                _canvas.Invalidate();
            }

            UpdateZoomControls();
            ZoomChanged?.Invoke(this, zoom);
        }

        public void UpdateZoomControls()
        {
            var zoom = _canvas?.ZoomFactor ?? 1f;

            _isUpdatingSlider = true;
            _zoomSlider.Value = (int)(zoom * 100);
            _isUpdatingSlider = false;

            _zoomLabel.Text = string.Format("{0:F0}%", zoom * 100);

            // 更新按钮状态
            _zoomOutButton.Enabled = zoom > MinZoom;
            _zoomInButton.Enabled = zoom < MaxZoom;
        }

        /// <summary>
        /// 设置关联的画布
        /// </summary>
        public void SetCanvas(FlowDesignerCanvas canvas)
        {
            _canvas = canvas;
            UpdateZoomControls();
        }

        #endregion

        #region 状态更新

        private void UpdateModeButtons()
        {
            var theme = ThemeManager.Instance.CurrentTheme;

            if (_grabModeButton != null)
            {
                _grabModeButton.BackColor = _isGrabMode ? Color.FromArgb(50, theme.Primary) : Color.Transparent;
            }
            if (_selectModeButton != null)
            {
                _selectModeButton.BackColor = !_isGrabMode ? Color.FromArgb(50, theme.Primary) : Color.Transparent;
            }
        }

        private void UpdateMinimapButton()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            if (_minimapButton != null)
            {
                _minimapButton.BackColor = _minimapVisible ? Color.FromArgb(50, theme.Primary) : Color.Transparent;
            }
        }

        private void UpdateFullscreenButton()
        {
            if (_fullscreenButton != null)
            {
                _fullscreenButton.Text = _isFullscreen ? "⛶" : "⛶";
            }
        }

        public void SetMinimapState(bool visible)
        {
            _minimapVisible = visible;
            UpdateMinimapButton();
        }

        public void SetPanningMode(bool isGrabMode)
        {
            _isGrabMode = isGrabMode;
            UpdateModeButtons();
        }

        /// <summary>
        /// 更新缩放标签显示
        /// </summary>
        public void UpdateZoomLabel()
        {
            UpdateZoomControls();
        }

        #endregion

        #region 绘制

        private void CanvasControlPanel_Paint(object sender, PaintEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;

            // 绘制顶部边框线
            using (var pen = new Pen(theme.Border, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, this.Width, 0);
            }
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            var theme = e.NewTheme;
            this.BackColor = theme.NodeBackground;
            _zoomLabel.ForeColor = theme.TextPrimary;

            foreach (Control control in this.Controls)
            {
                UpdateControlTheme(control, theme);
            }

            Invalidate();
        }

        private void UpdateControlTheme(Control control, ThemeConfig theme)
        {
            if (control is Button button)
            {
                button.ForeColor = theme.TextPrimary;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, theme.Primary);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, theme.Primary);
            }

            foreach (Control child in control.Controls)
            {
                UpdateControlTheme(child, theme);
            }
        }

        #endregion
    }
}
