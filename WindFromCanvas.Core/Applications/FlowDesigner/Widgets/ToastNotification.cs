using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// Toast通知控件（参考Activepieces实现）
    /// </summary>
    public class ToastNotification : Form
    {
        private Label _messageLabel;
        private Timer _autoCloseTimer;
        private const int DefaultDuration = 3000; // 3秒

        public ToastNotification(string message, ToastType type = ToastType.Info)
        {
            InitializeComponent(message, type);
        }

        private void InitializeComponent(string message, ToastType type)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = GetBackgroundColor(type);
            this.ForeColor = GetForegroundColor(type);
            this.Size = new Size(300, 60);
            this.Opacity = 0;

            _messageLabel = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 9),
                ForeColor = GetForegroundColor(type),
                Padding = new Padding(10)
            };

            this.Controls.Add(_messageLabel);

            // 淡入动画
            var fadeInTimer = new Timer { Interval = 10 };
            fadeInTimer.Tick += (s, e) =>
            {
                if (this.Opacity < 0.9)
                {
                    this.Opacity += 0.1;
                }
                else
                {
                    fadeInTimer.Stop();
                    fadeInTimer.Dispose();
                }
            };
            fadeInTimer.Start();

            // 自动关闭
            _autoCloseTimer = new Timer { Interval = DefaultDuration };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                CloseWithFadeOut();
            };
            _autoCloseTimer.Start();
        }

        private void CloseWithFadeOut()
        {
            var fadeOutTimer = new Timer { Interval = 10 };
            fadeOutTimer.Tick += (s, e) =>
            {
                if (this.Opacity > 0)
                {
                    this.Opacity -= 0.1;
                }
                else
                {
                    fadeOutTimer.Stop();
                    fadeOutTimer.Dispose();
                    this.Close();
                }
            };
            fadeOutTimer.Start();
        }

        private Color GetBackgroundColor(ToastType type)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            switch (type)
            {
                case ToastType.Success:
                    return theme.Success;
                case ToastType.Error:
                    return theme.Error;
                case ToastType.Warning:
                    return theme.Warning;
                default:
                    return theme.Primary;
            }
        }

        private Color GetForegroundColor(ToastType type)
        {
            return Color.White;
        }

        public static void Show(string message, ToastType type = ToastType.Info, Control parent = null)
        {
            var toast = new ToastNotification(message, type);
            if (parent != null)
            {
                toast.Location = new Point(
                    parent.Right - toast.Width - 20,
                    parent.Bottom - toast.Height - 20
                );
            }
            else
            {
                toast.Location = new Point(
                    Screen.PrimaryScreen.WorkingArea.Right - toast.Width - 20,
                    Screen.PrimaryScreen.WorkingArea.Bottom - toast.Height - 20
                );
            }
            toast.Show();
        }
    }

    public enum ToastType
    {
        Info,
        Success,
        Error,
        Warning
    }
}
