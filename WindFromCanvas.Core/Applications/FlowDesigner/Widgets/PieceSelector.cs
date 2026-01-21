using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 组件选择器（匹配 Activepieces PieceSelector）
    /// </summary>
    public class PieceSelector : Form
    {
        private BuilderStateStore _stateStore;
        private TabControl _categoryTabs;
        private TextBox _searchBox;
        private FlowLayoutPanel _piecesPanel;
        private List<PieceInfo> _allPieces;
        private List<PieceInfo> _filteredPieces;
        private System.Windows.Forms.Timer _searchTimer;
        private FlowOperationType _operationType;
        private string _stepName;

        public event EventHandler<PieceSelectedEventArgs> PieceSelected;

        public PieceSelector(BuilderStateStore stateStore, FlowOperationType operationType, string stepName = null)
        {
            _stateStore = stateStore;
            _operationType = operationType;
            _stepName = stepName;
            _allPieces = new List<PieceInfo>();
            _filteredPieces = new List<PieceInfo>();
            InitializeComponent();
            LoadPieces();
        }

        private void InitializeComponent()
        {
            this.Text = _operationType == FlowOperationType.UPDATE_ACTION || 
                       _operationType == FlowOperationType.UPDATE_TRIGGER 
                       ? "替换组件" : "添加组件";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 搜索框
            _searchBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(this.ClientSize.Width - 20, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "搜索组件...",
                Font = new Font("Microsoft YaHei UI", 9),
                Parent = this
            };
            _searchBox.Enter += (s, e) => { if (_searchBox.Text == "搜索组件...") _searchBox.Text = ""; };
            _searchBox.Leave += (s, e) => { if (string.IsNullOrEmpty(_searchBox.Text)) _searchBox.Text = "搜索组件..."; };
            _searchBox.TextChanged += SearchBox_TextChanged;

            // 分类标签
            _categoryTabs = new TabControl
            {
                Location = new Point(10, 45),
                Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 85),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Microsoft YaHei UI", 9),
                Parent = this
            };

            CreateCategoryTabs();
            LoadPiecesIntoCategories();

            // 搜索防抖定时器
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 300;
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                FilterPieces();
            };
        }

        /// <summary>
        /// 创建分类标签
        /// </summary>
        private void CreateCategoryTabs()
        {
            var categories = new[]
            {
                new { Name = "探索", Type = PieceCategory.EXPLORE },
                new { Name = "应用", Type = PieceCategory.APPS },
                new { Name = "工具", Type = PieceCategory.UTILITY }
            };

            foreach (var category in categories)
            {
                var tabPage = new TabPage(category.Name);
                var piecesPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    Padding = new Padding(5)
                };
                tabPage.Controls.Add(piecesPanel);
                _categoryTabs.TabPages.Add(tabPage);
            }
        }

        /// <summary>
        /// 加载组件
        /// </summary>
        private void LoadPieces()
        {
            // 加载内置组件（简化实现）
            _allPieces.AddRange(new[]
            {
                new PieceInfo
                {
                    Name = "代码",
                    DisplayName = "代码",
                    Category = PieceCategory.UTILITY,
                    Description = "执行自定义代码",
                    Icon = "⚙"
                },
                new PieceInfo
                {
                    Name = "循环",
                    DisplayName = "循环",
                    Category = PieceCategory.UTILITY,
                    Description = "循环处理数组数据",
                    Icon = "⟲"
                },
                new PieceInfo
                {
                    Name = "路由",
                    DisplayName = "路由",
                    Category = PieceCategory.UTILITY,
                    Description = "条件分支路由",
                    Icon = "◇"
                }
            });

            _filteredPieces = new List<PieceInfo>(_allPieces);
            LoadPiecesIntoCategories();
        }

        /// <summary>
        /// 将组件加载到分类标签页
        /// </summary>
        private void LoadPiecesIntoCategories()
        {
            foreach (TabPage tabPage in _categoryTabs.TabPages)
            {
                var piecesPanel = tabPage.Controls[0] as FlowLayoutPanel;
                if (piecesPanel == null) continue;

                piecesPanel.Controls.Clear();

                var categoryName = tabPage.Text;
                var category = GetCategoryFromName(categoryName);

                foreach (var piece in _filteredPieces)
                {
                    if (piece.Category == category)
                    {
                        var pieceCard = CreatePieceCard(piece);
                        piecesPanel.Controls.Add(pieceCard);
                    }
                }
            }
        }

        /// <summary>
        /// 从名称获取分类
        /// </summary>
        private PieceCategory GetCategoryFromName(string name)
        {
            switch (name)
            {
                case "探索": return PieceCategory.EXPLORE;
                case "应用": return PieceCategory.APPS;
                case "工具": return PieceCategory.UTILITY;
                default: return PieceCategory.EXPLORE;
            }
        }

        /// <summary>
        /// 创建组件卡片
        /// </summary>
        private Panel CreatePieceCard(PieceInfo piece)
        {
            var card = new Panel
            {
                Size = new Size(550, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5),
                Cursor = Cursors.Hand
            };

            // 图标
            var iconLabel = new Label
            {
                Text = piece.Icon,
                Font = new Font("Microsoft YaHei UI", 24),
                Location = new Point(10, 15),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Parent = card
            };

            // 名称
            var nameLabel = new Label
            {
                Text = piece.DisplayName,
                Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
                Location = new Point(60, 10),
                Size = new Size(480, 25),
                ForeColor = Color.Black,
                Parent = card
            };

            // 描述
            var descLabel = new Label
            {
                Text = piece.Description,
                Font = new Font("Microsoft YaHei UI", 9),
                Location = new Point(60, 35),
                Size = new Size(480, 30),
                ForeColor = Color.Gray,
                Parent = card
            };

            // 鼠标悬停效果
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(248, 250, 252);
            card.MouseLeave += (s, e) => card.BackColor = Color.White;
            card.Click += (s, e) =>
            {
                PieceSelected?.Invoke(this, new PieceSelectedEventArgs
                {
                    PieceName = piece.Name,
                    DisplayName = piece.DisplayName
                });
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            return card;
        }

        /// <summary>
        /// 搜索框文本变化
        /// </summary>
        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        /// <summary>
        /// 过滤组件
        /// </summary>
        private void FilterPieces()
        {
            var searchText = _searchBox.Text.ToLower();
            if (searchText == "搜索组件...")
            {
                searchText = "";
            }

            if (string.IsNullOrEmpty(searchText))
            {
                _filteredPieces = new List<PieceInfo>(_allPieces);
            }
            else
            {
                _filteredPieces = _allPieces.Where(p =>
                    (p.DisplayName?.ToLower().Contains(searchText) ?? false) ||
                    (p.Description?.ToLower().Contains(searchText) ?? false)
                ).ToList();
            }

            LoadPiecesIntoCategories();
        }
    }

    /// <summary>
    /// 组件信息
    /// </summary>
    public class PieceInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public PieceCategory Category { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// 组件分类
    /// </summary>
    public enum PieceCategory
    {
        EXPLORE,
        APPS,
        UTILITY,
        AI_AND_AGENTS,
        APPROVALS
    }

    /// <summary>
    /// 组件选择事件参数
    /// </summary>
    public class PieceSelectedEventArgs : EventArgs
    {
        public string PieceName { get; set; }
        public string DisplayName { get; set; }
    }
}
