using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 节点选择器对话框（参考Activepieces实现）
    /// </summary>
    public partial class NodeSelectorDialog : Form
    {
        private TabControl _categoryTabs;
        private TextBox _searchBox;
        private FlowLayoutPanel _nodeListPanel;
        private List<FlowNodeType> _allNodeTypes;
        private List<FlowNodeType> _filteredNodeTypes;
        private System.Windows.Forms.Timer _searchTimer;

        public event EventHandler<FlowNodeType> NodeSelected;

        public NodeSelectorDialog()
        {
            InitializeComponent();
            LoadNodeTypes();
        }

        private void InitializeComponent()
        {
            this.Text = "添加节点";
            this.Size = new Size(350, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            this.ForeColor = ThemeManager.Instance.CurrentTheme.Foreground;

            // 搜索框
            _searchBox = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(this.ClientSize.Width - 24, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "搜索节点..."
            };
            _searchBox.Enter += (s, e) => { if (_searchBox.Text == "搜索节点...") _searchBox.Text = ""; };
            _searchBox.Leave += (s, e) => { if (string.IsNullOrEmpty(_searchBox.Text)) _searchBox.Text = "搜索节点..."; };
            _searchBox.TextChanged += SearchBox_TextChanged;
            this.Controls.Add(_searchBox);

            // 分类标签
            _categoryTabs = new TabControl
            {
                Location = new Point(12, 45),
                Size = new Size(this.ClientSize.Width - 24, this.ClientSize.Height - 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // 创建分类标签页
            CreateCategoryTabs();
            this.Controls.Add(_categoryTabs);

            // 搜索防抖定时器
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 300; // 300ms防抖
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                FilterNodes();
            };
        }

        private void CreateCategoryTabs()
        {
            var categories = new[]
            {
                new { Name = "基础", Types = new[] { FlowNodeType.Start, FlowNodeType.Process, FlowNodeType.Decision, FlowNodeType.Loop, FlowNodeType.End } },
                new { Name = "流程", Types = new[] { FlowNodeType.Process, FlowNodeType.Decision, FlowNodeType.Loop } },
                new { Name = "控制", Types = new[] { FlowNodeType.Decision, FlowNodeType.Loop } }
            };

            foreach (var category in categories)
            {
                var tabPage = new TabPage(category.Name);
                var nodeList = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false
                };
                tabPage.Controls.Add(nodeList);
                _categoryTabs.TabPages.Add(tabPage);
            }
        }

        private void LoadNodeTypes()
        {
            _allNodeTypes = Enum.GetValues(typeof(FlowNodeType)).Cast<FlowNodeType>().ToList();
            _filteredNodeTypes = new List<FlowNodeType>(_allNodeTypes);
            RefreshNodeList();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void FilterNodes()
        {
            var searchText = _searchBox.Text.ToLower();
            if (searchText == "搜索节点...")
            {
                searchText = "";
            }

            if (string.IsNullOrEmpty(searchText))
            {
                _filteredNodeTypes = new List<FlowNodeType>(_allNodeTypes);
            }
            else
            {
                _filteredNodeTypes = _allNodeTypes.Where(type =>
                    GetNodeTypeDisplayName(type).ToLower().Contains(searchText)
                ).ToList();
            }

            RefreshNodeList();
        }

        private void RefreshNodeList()
        {
            foreach (TabPage tabPage in _categoryTabs.TabPages)
            {
                var nodeList = tabPage.Controls[0] as FlowLayoutPanel;
                if (nodeList != null)
                {
                    nodeList.Controls.Clear();

                    var categoryName = tabPage.Text;
                    var categoryTypes = GetCategoryTypes(categoryName);

                    foreach (var nodeType in _filteredNodeTypes)
                    {
                        if (categoryTypes.Contains(nodeType))
                        {
                            var nodeCard = CreateNodeCard(nodeType);
                            nodeList.Controls.Add(nodeCard);
                        }
                    }
                }
            }
        }

        private FlowNodeType[] GetCategoryTypes(string categoryName)
        {
            switch (categoryName)
            {
                case "基础":
                    return new[] { FlowNodeType.Start, FlowNodeType.Process, FlowNodeType.Decision, FlowNodeType.Loop, FlowNodeType.End };
                case "流程":
                    return new[] { FlowNodeType.Process, FlowNodeType.Decision, FlowNodeType.Loop };
                case "控制":
                    return new[] { FlowNodeType.Decision, FlowNodeType.Loop };
                default:
                    return new FlowNodeType[0];
            }
        }

        private Panel CreateNodeCard(FlowNodeType nodeType)
        {
            var card = new Panel
            {
                Size = new Size(300, 60),
                BackColor = ThemeManager.Instance.CurrentTheme.Background,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5)
            };

            var iconLabel = new Label
            {
                Text = GetNodeTypeIcon(nodeType),
                Font = new Font("Microsoft YaHei UI", 20),
                Location = new Point(10, 15),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var nameLabel = new Label
            {
                Text = GetNodeTypeDisplayName(nodeType),
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(50, 10),
                Size = new Size(240, 20),
                ForeColor = ThemeManager.Instance.CurrentTheme.Foreground
            };

            var descLabel = new Label
            {
                Text = GetNodeTypeDescription(nodeType),
                Font = new Font("Microsoft YaHei UI", 8),
                Location = new Point(50, 30),
                Size = new Size(240, 20),
                ForeColor = ThemeManager.Instance.CurrentTheme.Ring
            };

            card.Controls.Add(iconLabel);
            card.Controls.Add(nameLabel);
            card.Controls.Add(descLabel);

            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(248, 250, 252);
            card.MouseLeave += (s, e) => card.BackColor = ThemeManager.Instance.CurrentTheme.Background;
            card.Click += (s, e) =>
            {
                NodeSelected?.Invoke(this, nodeType);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            return card;
        }

        private string GetNodeTypeIcon(FlowNodeType type)
        {
            switch (type)
            {
                case FlowNodeType.Start: return "▶";
                case FlowNodeType.Process: return "⚙";
                case FlowNodeType.Decision: return "◇";
                case FlowNodeType.Loop: return "⟲";
                case FlowNodeType.End: return "■";
                default: return "○";
            }
        }

        private string GetNodeTypeDisplayName(FlowNodeType type)
        {
            switch (type)
            {
                case FlowNodeType.Start: return "开始";
                case FlowNodeType.Process: return "处理";
                case FlowNodeType.Decision: return "判断";
                case FlowNodeType.Loop: return "循环";
                case FlowNodeType.End: return "结束";
                default: return type.ToString();
            }
        }

        private string GetNodeTypeDescription(FlowNodeType type)
        {
            switch (type)
            {
                case FlowNodeType.Start: return "流程的起始节点";
                case FlowNodeType.Process: return "执行处理操作";
                case FlowNodeType.Decision: return "条件判断分支";
                case FlowNodeType.Loop: return "循环执行";
                case FlowNodeType.End: return "流程的结束节点";
                default: return "";
            }
        }
    }
}
