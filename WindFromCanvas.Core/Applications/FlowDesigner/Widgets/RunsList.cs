using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 运行列表（匹配 Activepieces RunsList）
    /// </summary>
    public class RunsList : Panel
    {
        private BuilderStateStore _stateStore;
        private ListView _runsList;
        private List<FlowRun> _runs;

        public event EventHandler<FlowRun> RunSelected;

        public RunsList(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _runs = new List<FlowRun>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(10);

            // 标题
            var lblTitle = new Label
            {
                Text = "运行历史",
                Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 25),
                Parent = this
            };

            // 运行列表
            _runsList = new ListView
            {
                Location = new Point(10, 40),
                Size = new Size(this.Width - 20, this.Height - 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Parent = this
            };

            _runsList.Columns.Add("状态", 80);
            _runsList.Columns.Add("开始时间", 150);
            _runsList.Columns.Add("持续时间", 100);
            _runsList.Columns.Add("步骤数", 80);

            _runsList.MouseDoubleClick += RunsList_MouseDoubleClick;

            RefreshRuns();
        }

        /// <summary>
        /// 刷新运行列表
        /// </summary>
        public void RefreshRuns()
        {
            _runsList.Items.Clear();
            
            foreach (var run in _runs.OrderByDescending(r => r.StartTime))
            {
                var item = new ListViewItem(GetStatusDisplayName(run.Status));
                item.SubItems.Add(run.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(GetDurationDisplay(run.Duration));
                item.SubItems.Add(run.StepsExecuted.ToString());
                item.Tag = run;
                _runsList.Items.Add(item);
            }
        }

        /// <summary>
        /// 添加运行记录
        /// </summary>
        public void AddRun(FlowRun run)
        {
            _runs.Add(run);
            RefreshRuns();
        }

        /// <summary>
        /// 获取状态显示名称
        /// </summary>
        private string GetStatusDisplayName(FlowRunStatus status)
        {
            switch (status)
            {
                case FlowRunStatus.SUCCESS:
                    return "成功";
                case FlowRunStatus.FAILED:
                    return "失败";
                case FlowRunStatus.RUNNING:
                    return "运行中";
                default:
                    return status.ToString();
            }
        }

        /// <summary>
        /// 获取持续时间显示
        /// </summary>
        private string GetDurationDisplay(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
            {
                return $"{(int)(duration.TotalMilliseconds)}ms";
            }
            else if (duration.TotalMinutes < 1)
            {
                return $"{duration.TotalSeconds:F1}s";
            }
            else
            {
                return $"{duration.TotalMinutes:F1}m";
            }
        }

        /// <summary>
        /// 运行列表双击
        /// </summary>
        private void RunsList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_runsList.SelectedItems.Count > 0)
            {
                var run = _runsList.SelectedItems[0].Tag as FlowRun;
                if (run != null)
                {
                    RunSelected?.Invoke(this, run);
                }
            }
        }
    }

    /// <summary>
    /// 流程运行
    /// </summary>
    public class FlowRun
    {
        public string Id { get; set; }
        public string FlowId { get; set; }
        public string FlowVersionId { get; set; }
        public FlowRunStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int StepsExecuted { get; set; }
        public Dictionary<string, StepOutput> StepOutputs { get; set; }

        public FlowRun()
        {
            Id = Guid.NewGuid().ToString();
            Status = FlowRunStatus.RUNNING;
            StartTime = DateTime.Now;
            StepOutputs = new Dictionary<string, StepOutput>();
        }
    }

    /// <summary>
    /// 运行状态
    /// </summary>
    public enum FlowRunStatus
    {
        RUNNING,
        SUCCESS,
        FAILED
    }

    /// <summary>
    /// 步骤输出
    /// </summary>
    public class StepOutput
    {
        public string StepName { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
