using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 流程版本列表（匹配 Activepieces FlowVersionsList）
    /// </summary>
    public class FlowVersionsList : Panel
    {
        private BuilderStateStore _stateStore;
        private ListView _versionsList;
        private VersionManager _versionManager;
        private string _currentFlowId;

        public event EventHandler<FlowVersion> VersionSelected;
        public event EventHandler<FlowVersion> VersionRestoreRequested;

        public FlowVersionsList(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _versionManager = VersionManager.Instance;
            InitializeComponent();
            
            // 订阅状态变化
            _stateStore.PropertyChanged += StateStore_PropertyChanged;
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
                Text = "流程版本",
                Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 25),
                Parent = this
            };

            // 版本列表
            _versionsList = new ListView
            {
                Location = new Point(10, 40),
                Size = new Size(this.Width - 20, this.Height - 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Parent = this
            };

            _versionsList.Columns.Add("版本名称", 150);
            _versionsList.Columns.Add("状态", 80);
            _versionsList.Columns.Add("创建时间", 150);
            _versionsList.Columns.Add("更新时间", 150);

            _versionsList.MouseDoubleClick += VersionsList_MouseDoubleClick;
            _versionsList.MouseClick += VersionsList_MouseClick;

            LoadVersions();
        }

        private void StateStore_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BuilderStateStore.Flow) || 
                e.PropertyName == "Flow.FlowVersion")
            {
                var flowVersion = _stateStore.Flow?.FlowVersion;
                if (flowVersion != null)
                {
                    _currentFlowId = flowVersion.FlowId;
                    LoadVersions();
                }
            }
        }

        /// <summary>
        /// 加载版本列表
        /// </summary>
        private void LoadVersions()
        {
            if (string.IsNullOrEmpty(_currentFlowId))
            {
                var flowVersion = _stateStore.Flow?.FlowVersion;
                if (flowVersion != null)
                {
                    _currentFlowId = flowVersion.FlowId;
                }
                else
                {
                    return;
                }
            }

            _versionsList.Items.Clear();
            var versions = _versionManager.GetVersions(_currentFlowId);

            foreach (var version in versions)
            {
                var item = new ListViewItem(version.DisplayName ?? "未命名版本");
                item.SubItems.Add(GetStateDisplayName(version.State));
                item.SubItems.Add(version.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(version.UpdatedAt.ToString("yyyy-MM-dd HH:mm"));
                item.Tag = version;
                _versionsList.Items.Add(item);
            }
        }

        /// <summary>
        /// 获取状态显示名称
        /// </summary>
        private string GetStateDisplayName(FlowVersionState state)
        {
            switch (state)
            {
                case FlowVersionState.DRAFT:
                    return "草稿";
                case FlowVersionState.LOCKED:
                    return "已锁定";
                default:
                    return state.ToString();
            }
        }

        /// <summary>
        /// 版本列表双击
        /// </summary>
        private void VersionsList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_versionsList.SelectedItems.Count > 0)
            {
                var version = _versionsList.SelectedItems[0].Tag as FlowVersion;
                if (version != null)
                {
                    VersionSelected?.Invoke(this, version);
                }
            }
        }

        /// <summary>
        /// 版本列表右键点击
        /// </summary>
        private void VersionsList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _versionsList.SelectedItems.Count > 0)
            {
                var version = _versionsList.SelectedItems[0].Tag as FlowVersion;
                if (version != null)
                {
                    ShowVersionContextMenu(version, e.Location);
                }
            }
        }

        /// <summary>
        /// 显示版本上下文菜单
        /// </summary>
        private void ShowVersionContextMenu(FlowVersion version, Point location)
        {
            var contextMenu = new ContextMenuStrip();

            // 查看版本
            var viewItem = new ToolStripMenuItem("查看");
            viewItem.Click += (s, e) => VersionSelected?.Invoke(this, version);
            contextMenu.Items.Add(viewItem);

            // 使用为草稿
            if (version.State == FlowVersionState.LOCKED)
            {
                var useAsDraftItem = new ToolStripMenuItem("使用为草稿");
                useAsDraftItem.Click += (s, e) =>
                {
                    var draftVersion = _versionManager.UseAsDraft(version.FlowId, version.Id);
                    _stateStore.Flow.FlowVersion = draftVersion;
                    _stateStore.OnPropertyChanged(nameof(BuilderStateStore.Flow));
                    LoadVersions();
                };
                contextMenu.Items.Add(useAsDraftItem);
            }

            // 恢复版本
            var restoreItem = new ToolStripMenuItem("恢复到此版本");
            restoreItem.Click += (s, e) => VersionRestoreRequested?.Invoke(this, version);
            contextMenu.Items.Add(restoreItem);

            contextMenu.Show(_versionsList, location);
        }
    }
}
