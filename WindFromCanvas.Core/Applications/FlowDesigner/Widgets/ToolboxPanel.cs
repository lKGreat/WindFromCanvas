using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 7.1 工具箱面板 - 显示可拖拽的节点类型
    /// 支持分类折叠、搜索过滤、最近使用、拖拽预览、工具提示
    /// </summary>
    public class ToolboxPanel : Panel
    {
        #region 数据结构

        /// <summary>
        /// 节点类型定义
        /// </summary>
        public class NodeTypeItem
        {
            public string Id { get; set; }
            public FlowNodeType Type { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public Color Color { get; set; }
            public Image Icon { get; set; }
            public string IconText { get; set; }
            public int UsageCount { get; set; }
            public DateTime LastUsed { get; set; }
        }

        /// <summary>
        /// 分类定义
        /// </summary>
        public class CategoryItem
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public bool IsExpanded { get; set; } = true;
            public List<NodeTypeItem> Items { get; set; } = new List<NodeTypeItem>();
            public int Order { get; set; }
        }

        #endregion

        #region 字段

        private readonly List<NodeTypeItem> _allNodeTypes = new List<NodeTypeItem>();
        private readonly List<CategoryItem> _categories = new List<CategoryItem>();
        private readonly List<NodeTypeItem> _recentItems = new List<NodeTypeItem>();
        private readonly Dictionary<string, Rectangle> _itemRects = new Dictionary<string, Rectangle>();
        private readonly Dictionary<string, Rectangle> _categoryRects = new Dictionary<string, Rectangle>();

        private TextBox _searchBox;
        private string _searchText = "";
        private FlowNodeType? _draggingNodeType;
        private NodeTypeItem _hoveredItem;
        private string _hoveredCategory;
        private ToolTip _toolTip;
        private Timer _tooltipTimer;
        private Point _lastMousePosition;

        // 布局常量
        private const int SearchBoxHeight = 30;
        private const int CategoryHeaderHeight = 32;
        private const int ItemHeight = 44;
        private const int ItemPadding = 8;
        private const int IconSize = 28;
        private const int MaxRecentItems = 5;

        #endregion

        #region 事件

        public event EventHandler<NodeTypeItem> NodeTypeSelected;
        public event EventHandler<NodeTypeItem> NodeDragStarted;

        #endregion

        #region 构造

        public ToolboxPanel()
        {
            InitializeComponent();
            InitializeNodeTypes();
            SetupUI();
        }

        private void InitializeComponent()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);
        }

        #endregion

        #region 7.1.1 节点类型初始化

        /// <summary>
        /// 初始化节点类型
        /// </summary>
        private void InitializeNodeTypes()
        {
            // 基础类别
            AddCategory("recent", "最近使用", 0);
            AddCategory("basic", "基础节点", 1);
            AddCategory("control", "控制节点", 2);
            AddCategory("data", "数据节点", 3);

            // 基础节点
            AddNodeType(new NodeTypeItem
            {
                Id = "start",
                Type = FlowNodeType.Start,
                DisplayName = "开始",
                Description = "流程的起始点",
                Category = "basic",
                Color = Color.FromArgb(67, 160, 71),
                IconText = "▶"
            });

            AddNodeType(new NodeTypeItem
            {
                Id = "end",
                Type = FlowNodeType.End,
                DisplayName = "结束",
                Description = "流程的终止点",
                Category = "basic",
                Color = Color.FromArgb(229, 57, 53),
                IconText = "■"
            });

            AddNodeType(new NodeTypeItem
            {
                Id = "process",
                Type = FlowNodeType.Process,
                DisplayName = "处理",
                Description = "执行一个操作或任务",
                Category = "basic",
                Color = Color.FromArgb(33, 150, 243),
                IconText = "□"
            });

            // 控制节点
            AddNodeType(new NodeTypeItem
            {
                Id = "decision",
                Type = FlowNodeType.Decision,
                DisplayName = "判断",
                Description = "根据条件分支执行",
                Category = "control",
                Color = Color.FromArgb(255, 193, 7),
                IconText = "◇"
            });

            AddNodeType(new NodeTypeItem
            {
                Id = "loop",
                Type = FlowNodeType.Loop,
                DisplayName = "循环",
                Description = "重复执行一组操作",
                Category = "control",
                Color = Color.FromArgb(156, 39, 176),
                IconText = "↻"
            });

            // 数据节点
            AddNodeType(new NodeTypeItem
            {
                Id = "code",
                Type = FlowNodeType.Code,
                DisplayName = "代码",
                Description = "执行自定义代码脚本",
                Category = "data",
                Color = Color.FromArgb(0, 150, 136),
                IconText = "<>"
            });

            AddNodeType(new NodeTypeItem
            {
                Id = "piece",
                Type = FlowNodeType.Piece,
                DisplayName = "组件",
                Description = "引用可复用的组件",
                Category = "data",
                Color = Color.FromArgb(121, 85, 72),
                IconText = "◈"
            });
        }

        private void AddCategory(string name, string displayName, int order)
        {
            _categories.Add(new CategoryItem
            {
                Name = name,
                DisplayName = displayName,
                Order = order
            });
        }

        private void AddNodeType(NodeTypeItem item)
        {
            _allNodeTypes.Add(item);
            var category = _categories.FirstOrDefault(c => c.Name == item.Category);
            if (category != null)
            {
                category.Items.Add(item);
            }
        }

        /// <summary>
        /// 注册自定义节点类型
        /// </summary>
        public void RegisterNodeType(NodeTypeItem item)
        {
            if (item == null) return;

            // 检查是否已存在
            var existing = _allNodeTypes.FirstOrDefault(n => n.Id == item.Id);
            if (existing != null)
            {
                _allNodeTypes.Remove(existing);
            }

            AddNodeType(item);
            Invalidate();
        }

        #endregion

        #region UI 设置

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            var theme = ThemeManager.Instance.CurrentTheme;

            this.BackColor = theme.Background;
            this.AutoScroll = true;
            this.Padding = new Padding(0, SearchBoxHeight + 8, 0, 0);

            // 7.1.2 搜索框
            _searchBox = new TextBox
            {
                Location = new Point(8, 8),
                Width = 200,
                Height = SearchBoxHeight,
                Font = new Font("Segoe UI", 9),
                Text = ""
            };
            _searchBox.GotFocus += (s, e) => {
                if (_searchBox.Text == "搜索节点...")
                {
                    _searchBox.Text = "";
                    _searchBox.ForeColor = SystemColors.WindowText;
                }
            };
            _searchBox.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(_searchBox.Text))
                {
                    _searchBox.Text = "搜索节点...";
                    _searchBox.ForeColor = SystemColors.GrayText;
                }
            };
            _searchBox.Text = "搜索节点...";
            _searchBox.ForeColor = SystemColors.GrayText;
            _searchBox.TextChanged += SearchBox_TextChanged;
            this.Controls.Add(_searchBox);

            // 工具提示
            _toolTip = new ToolTip
            {
                InitialDelay = 500,
                AutoPopDelay = 5000,
                ReshowDelay = 200
            };

            _tooltipTimer = new Timer { Interval = 500 };
            _tooltipTimer.Tick += TooltipTimer_Tick;

            // 事件绑定
            this.Paint += ToolboxPanel_Paint;
            this.MouseDown += ToolboxPanel_MouseDown;
            this.MouseMove += ToolboxPanel_MouseMove;
            this.MouseUp += ToolboxPanel_MouseUp;
            this.MouseLeave += ToolboxPanel_MouseLeave;
            this.Resize += ToolboxPanel_Resize;

            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        }

        private void ToolboxPanel_Resize(object sender, EventArgs e)
        {
            if (_searchBox != null)
            {
                _searchBox.Width = this.Width - 16;
            }
            Invalidate();
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            this.BackColor = e.NewTheme.Background;
            Invalidate();
        }

        #endregion

        #region 7.1.2 搜索过滤

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var text = _searchBox.Text;
            if (text == "搜索节点..." || _searchBox.ForeColor == SystemColors.GrayText)
            {
                _searchText = "";
            }
            else
            {
                _searchText = text.Trim().ToLower();
            }
            Invalidate();
        }

        /// <summary>
        /// 获取过滤后的节点类型
        /// </summary>
        private IEnumerable<NodeTypeItem> GetFilteredItems(CategoryItem category)
        {
            var items = category.Items.AsEnumerable();

            if (!string.IsNullOrEmpty(_searchText))
            {
                items = items.Where(item =>
                    item.DisplayName.ToLower().Contains(_searchText) ||
                    (item.Description?.ToLower().Contains(_searchText) ?? false));
            }

            return items;
        }

        #endregion

        #region 7.1.3 最近使用

        /// <summary>
        /// 记录节点使用
        /// </summary>
        public void RecordUsage(FlowNodeType type)
        {
            var item = _allNodeTypes.FirstOrDefault(n => n.Type == type);
            if (item == null) return;

            item.UsageCount++;
            item.LastUsed = DateTime.Now;

            // 更新最近使用列表
            _recentItems.Remove(item);
            _recentItems.Insert(0, item);

            // 保持列表大小
            while (_recentItems.Count > MaxRecentItems)
            {
                _recentItems.RemoveAt(_recentItems.Count - 1);
            }

            // 更新最近使用分类
            var recentCategory = _categories.FirstOrDefault(c => c.Name == "recent");
            if (recentCategory != null)
            {
                recentCategory.Items.Clear();
                recentCategory.Items.AddRange(_recentItems);
            }

            Invalidate();
        }

        #endregion

        #region 绘制

        private void ToolboxPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var theme = ThemeManager.Instance.CurrentTheme;

            _itemRects.Clear();
            _categoryRects.Clear();

            int y = SearchBoxHeight + 16;

            // 按顺序绘制分类
            foreach (var category in _categories.OrderBy(c => c.Order))
            {
                var filteredItems = GetFilteredItems(category).ToList();
                
                // 跳过空分类（但最近使用只在有数据时显示）
                if (category.Name == "recent" && filteredItems.Count == 0)
                    continue;

                // 7.1.1 绘制分类头（可折叠）
                var categoryRect = new Rectangle(0, y, this.Width, CategoryHeaderHeight);
                _categoryRects[category.Name] = categoryRect;

                DrawCategoryHeader(g, category, categoryRect, theme);
                y += CategoryHeaderHeight;

                // 如果分类展开且有内容，绘制节点项
                if (category.IsExpanded && filteredItems.Count > 0)
                {
                    foreach (var item in filteredItems)
                    {
                        var itemRect = new Rectangle(ItemPadding, y, this.Width - ItemPadding * 2 - SystemInformation.VerticalScrollBarWidth, ItemHeight);
                        _itemRects[item.Id] = itemRect;

                        DrawNodeItem(g, item, itemRect, theme);
                        y += ItemHeight + 4;
                    }
                }

                y += 8; // 分类间距
            }

            // 更新滚动区域
            this.AutoScrollMinSize = new Size(0, y + 20);
        }

        /// <summary>
        /// 绘制分类头
        /// </summary>
        private void DrawCategoryHeader(Graphics g, CategoryItem category, Rectangle rect, ThemeConfig theme)
        {
            var isHovered = _hoveredCategory == category.Name;
            var bgColor = isHovered ? Color.FromArgb(20, theme.Primary) : Color.Transparent;

            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, rect);
            }

            // 折叠箭头
            var arrowRect = new RectangleF(rect.X + 12, rect.Y + 10, 12, 12);
            using (var pen = new Pen(theme.TextSecondary, 2))
            {
                if (category.IsExpanded)
                {
                    // 向下箭头
                    g.DrawLine(pen, arrowRect.X, arrowRect.Y + 3, arrowRect.X + 6, arrowRect.Y + 9);
                    g.DrawLine(pen, arrowRect.X + 6, arrowRect.Y + 9, arrowRect.X + 12, arrowRect.Y + 3);
                }
                else
                {
                    // 向右箭头
                    g.DrawLine(pen, arrowRect.X + 3, arrowRect.Y, arrowRect.X + 9, arrowRect.Y + 6);
                    g.DrawLine(pen, arrowRect.X + 9, arrowRect.Y + 6, arrowRect.X + 3, arrowRect.Y + 12);
                }
            }

            // 分类名称
            using (var font = new Font("Segoe UI Semibold", 10))
            using (var brush = new SolidBrush(theme.TextPrimary))
            {
                var textRect = new RectangleF(rect.X + 32, rect.Y, rect.Width - 40, rect.Height);
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(category.DisplayName, font, brush, textRect, format);
            }

            // 项目数量
            var itemCount = GetFilteredItems(category).Count();
            if (itemCount > 0)
            {
                using (var font = new Font("Segoe UI", 8))
                using (var brush = new SolidBrush(theme.TextSecondary))
                {
                    var countText = string.Format("({0})", itemCount);
                    var countSize = g.MeasureString(countText, font);
                    g.DrawString(countText, font, brush, rect.Right - countSize.Width - 16, rect.Y + (rect.Height - countSize.Height) / 2);
                }
            }
        }

        /// <summary>
        /// 7.1.4 绘制节点项（支持拖拽预览样式）
        /// </summary>
        private void DrawNodeItem(Graphics g, NodeTypeItem item, Rectangle rect, ThemeConfig theme)
        {
            var isHovered = _hoveredItem == item;
            var bgColor = isHovered ? Color.FromArgb(40, item.Color) : theme.NodeBackground;
            var borderColor = isHovered ? item.Color : theme.Border;

            // 背景
            using (var path = CreateRoundedRectPath(rect, 6))
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            // 边框
            using (var path = CreateRoundedRectPath(rect, 6))
            using (var pen = new Pen(borderColor, isHovered ? 2 : 1))
            {
                g.DrawPath(pen, path);
            }

            // 颜色图标
            var iconRect = new RectangleF(rect.X + 10, rect.Y + (rect.Height - IconSize) / 2, IconSize, IconSize);
            using (var brush = new SolidBrush(item.Color))
            using (var path = CreateRoundedRectPath(Rectangle.Round(iconRect), 4))
            {
                g.FillPath(brush, path);
            }

            // 图标文字
            if (!string.IsNullOrEmpty(item.IconText))
            {
                using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(item.IconText, font, brush, iconRect, format);
                }
            }

            // 名称
            using (var font = new Font("Segoe UI", 10))
            using (var brush = new SolidBrush(theme.TextPrimary))
            {
                var textX = iconRect.Right + 10;
                g.DrawString(item.DisplayName, font, brush, textX, rect.Y + 8);
            }

            // 描述（如果有）
            if (!string.IsNullOrEmpty(item.Description))
            {
                using (var font = new Font("Segoe UI", 8))
                using (var brush = new SolidBrush(theme.TextSecondary))
                {
                    var textX = iconRect.Right + 10;
                    var maxWidth = rect.Width - textX - 10;
                    var descRect = new RectangleF(textX, rect.Y + 24, maxWidth, 16);
                    var format = new StringFormat
                    {
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(item.Description, font, brush, descRect, format);
                }
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, float radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            return CreateRoundedRectPath(Rectangle.Round(rect), radius);
        }

        #endregion

        #region 交互

        private void ToolboxPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // 检查分类点击
            var scrollOffset = this.AutoScrollPosition;
            var adjustedPoint = new Point(e.X - scrollOffset.X, e.Y - scrollOffset.Y);

            foreach (var kvp in _categoryRects)
            {
                if (kvp.Value.Contains(adjustedPoint))
                {
                    var category = _categories.FirstOrDefault(c => c.Name == kvp.Key);
                    if (category != null)
                    {
                        category.IsExpanded = !category.IsExpanded;
                        Invalidate();
                        return;
                    }
                }
            }

            // 检查节点项点击
            var item = GetItemAt(e.Location);
            if (item != null && e.Button == MouseButtons.Left)
            {
                _draggingNodeType = item.Type;
                NodeDragStarted?.Invoke(this, item);
                this.DoDragDrop(item, DragDropEffects.Copy);
            }
        }

        private void ToolboxPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var scrollOffset = this.AutoScrollPosition;
            var adjustedPoint = new Point(e.X - scrollOffset.X, e.Y - scrollOffset.Y);

            // 检查分类悬停
            string hoveredCat = null;
            foreach (var kvp in _categoryRects)
            {
                if (kvp.Value.Contains(adjustedPoint))
                {
                    hoveredCat = kvp.Key;
                    break;
                }
            }

            // 检查节点项悬停
            var item = GetItemAt(e.Location);
            
            if (_hoveredItem != item || _hoveredCategory != hoveredCat)
            {
                _hoveredItem = item;
                _hoveredCategory = hoveredCat;
                _lastMousePosition = e.Location;
                
                // 7.1.5 重置工具提示计时器
                _tooltipTimer.Stop();
                _toolTip.Hide(this);
                if (item != null)
                {
                    _tooltipTimer.Start();
                }

                Invalidate();
            }
        }

        private void ToolboxPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _draggingNodeType = null;
        }

        private void ToolboxPanel_MouseLeave(object sender, EventArgs e)
        {
            _hoveredItem = null;
            _hoveredCategory = null;
            _tooltipTimer.Stop();
            _toolTip.Hide(this);
            Invalidate();
        }

        /// <summary>
        /// 7.1.5 显示工具提示
        /// </summary>
        private void TooltipTimer_Tick(object sender, EventArgs e)
        {
            _tooltipTimer.Stop();

            if (_hoveredItem != null)
            {
                var tipText = string.Format("{0}\n{1}", _hoveredItem.DisplayName, _hoveredItem.Description ?? "");
                _toolTip.Show(tipText, this, _lastMousePosition.X + 10, _lastMousePosition.Y + 20, 3000);
            }
        }

        /// <summary>
        /// 获取指定位置的节点类型项
        /// </summary>
        private NodeTypeItem GetItemAt(Point location)
        {
            var scrollOffset = this.AutoScrollPosition;
            var adjustedPoint = new Point(location.X - scrollOffset.X, location.Y - scrollOffset.Y);

            foreach (var kvp in _itemRects)
            {
                if (kvp.Value.Contains(adjustedPoint))
                {
                    return _allNodeTypes.FirstOrDefault(n => n.Id == kvp.Key);
                }
            }
            return null;
        }

        #endregion

        #region 节点创建

        /// <summary>
        /// 创建节点实例
        /// </summary>
        public FlowNode CreateNode(FlowNodeType type, PointF position)
        {
            var nodeData = new FlowNodeData
            {
                Name = Guid.NewGuid().ToString(),
                DisplayName = GetDisplayName(type),
                Type = type,
                Position = position
            };

            // 记录使用
            RecordUsage(type);

            return CreateNodeFromData(nodeData);
        }

        /// <summary>
        /// 从数据创建节点
        /// </summary>
        public FlowNode CreateNodeFromData(FlowNodeData data)
        {
            switch (data.Type)
            {
                case FlowNodeType.Start:
                    return new StartNode(data);
                case FlowNodeType.Process:
                    return new ProcessNode(data);
                case FlowNodeType.Decision:
                    return new DecisionNode(data);
                case FlowNodeType.Loop:
                    return new LoopNode(data);
                case FlowNodeType.End:
                    return new EndNode(data);
                default:
                    return new ProcessNode(data);
            }
        }

        private string GetDisplayName(FlowNodeType type)
        {
            var item = _allNodeTypes.FirstOrDefault(n => n.Type == type);
            return item?.DisplayName ?? type.ToString();
        }

        #endregion
    }
}
