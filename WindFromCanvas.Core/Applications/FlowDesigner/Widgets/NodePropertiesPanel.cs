using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// 7.2 节点属性面板 - 显示和编辑节点属性
    /// 支持动态属性渲染、验证、属性分组、复杂类型编辑器
    /// </summary>
    public class NodePropertiesPanel : Panel
    {
        #region 数据结构

        /// <summary>
        /// 属性定义
        /// </summary>
        public class PropertyDefinition
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string Group { get; set; }
            public Type PropertyType { get; set; }
            public bool IsReadOnly { get; set; }
            public bool IsRequired { get; set; }
            public object DefaultValue { get; set; }
            public Func<object, bool> Validator { get; set; }
            public string ValidationMessage { get; set; }
            public string[] Options { get; set; } // 用于下拉选择
            public int Order { get; set; }
        }

        /// <summary>
        /// 属性分组
        /// </summary>
        public class PropertyGroup
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public bool IsExpanded { get; set; } = true;
            public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
            public int Order { get; set; }
        }

        /// <summary>
        /// 验证结果
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public string PropertyName { get; set; }
            public string Message { get; set; }
        }

        #endregion

        #region 字段

        private FlowNode _selectedNode;
        private readonly List<PropertyGroup> _propertyGroups = new List<PropertyGroup>();
        private readonly Dictionary<string, Control> _propertyControls = new Dictionary<string, Control>();
        private readonly Dictionary<string, ValidationResult> _validationResults = new Dictionary<string, ValidationResult>();
        private readonly Dictionary<string, Rectangle> _groupHeaderRects = new Dictionary<string, Rectangle>();

        private Panel _headerPanel;
        private Label _titleLabel;
        private Label _typeLabel;
        private Panel _contentPanel;
        private Label _noSelectionLabel;

        private bool _isUpdating = false;

        // 布局常量
        private const int HeaderHeight = 60;
        private const int GroupHeaderHeight = 32;
        private const int PropertyRowHeight = 28;
        private const int LabelWidth = 100;
        private const int PanelPadding = 12;

        #endregion

        #region 事件

        public event EventHandler<PropertyChangedEventArgs> PropertyValueChanged;
        public event EventHandler<ValidationResult> PropertyValidationFailed;

        public class PropertyChangedEventArgs : EventArgs
        {
            public FlowNode Node { get; set; }
            public string PropertyName { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }
        }

        #endregion

        #region 构造

        public NodePropertiesPanel()
        {
            InitializeComponent();
            InitializeDefaultGroups();
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

        #region 初始化

        private void InitializeDefaultGroups()
        {
            // 基本属性组
            var basicGroup = new PropertyGroup
            {
                Name = "basic",
                DisplayName = "基本属性",
                Order = 0,
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition
                    {
                        Name = "Name",
                        DisplayName = "名称",
                        Description = "节点的唯一标识名称",
                        PropertyType = typeof(string),
                        IsRequired = true,
                        Validator = v => !string.IsNullOrWhiteSpace(v?.ToString()),
                        ValidationMessage = "名称不能为空",
                        Order = 0
                    },
                    new PropertyDefinition
                    {
                        Name = "DisplayName",
                        DisplayName = "显示名称",
                        Description = "节点在画布上显示的名称",
                        PropertyType = typeof(string),
                        Order = 1
                    },
                    new PropertyDefinition
                    {
                        Name = "Type",
                        DisplayName = "类型",
                        PropertyType = typeof(FlowNodeType),
                        IsReadOnly = true,
                        Order = 2
                    }
                }
            };

            // 外观属性组
            var appearanceGroup = new PropertyGroup
            {
                Name = "appearance",
                DisplayName = "外观",
                Order = 1,
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition
                    {
                        Name = "Description",
                        DisplayName = "描述",
                        Description = "节点的详细描述信息",
                        PropertyType = typeof(string),
                        Order = 0
                    },
                    new PropertyDefinition
                    {
                        Name = "IconPath",
                        DisplayName = "图标",
                        Description = "节点图标路径",
                        PropertyType = typeof(string),
                        Order = 1
                    }
                }
            };

            // 行为属性组
            var behaviorGroup = new PropertyGroup
            {
                Name = "behavior",
                DisplayName = "行为",
                Order = 2,
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition
                    {
                        Name = "Skip",
                        DisplayName = "跳过执行",
                        Description = "是否跳过此节点的执行",
                        PropertyType = typeof(bool),
                        Order = 0
                    },
                    new PropertyDefinition
                    {
                        Name = "Status",
                        DisplayName = "状态",
                        PropertyType = typeof(NodeStatus),
                        IsReadOnly = true,
                        Order = 1
                    }
                }
            };

            // 自定义属性组
            var customGroup = new PropertyGroup
            {
                Name = "custom",
                DisplayName = "自定义属性",
                Order = 3
            };

            _propertyGroups.Add(basicGroup);
            _propertyGroups.Add(appearanceGroup);
            _propertyGroups.Add(behaviorGroup);
            _propertyGroups.Add(customGroup);
        }

        private void SetupUI()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            this.BackColor = theme.Background;
            this.AutoScroll = false;

            // 头部面板
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = theme.NodeBackground,
                Padding = new Padding(PanelPadding)
            };

            _titleLabel = new Label
            {
                Text = "属性",
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = theme.TextPrimary,
                Location = new Point(PanelPadding, 8),
                AutoSize = true
            };

            _typeLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = theme.TextSecondary,
                Location = new Point(PanelPadding, 32),
                AutoSize = true
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_typeLabel);
            this.Controls.Add(_headerPanel);

            // 内容面板
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(PanelPadding)
            };
            this.Controls.Add(_contentPanel);

            // 无选中提示
            _noSelectionLabel = new Label
            {
                Text = "请选择一个节点以查看其属性",
                Font = new Font("Segoe UI", 10),
                ForeColor = theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _contentPanel.Controls.Add(_noSelectionLabel);

            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            var theme = e.NewTheme;
            this.BackColor = theme.Background;
            _headerPanel.BackColor = theme.NodeBackground;
            _titleLabel.ForeColor = theme.TextPrimary;
            _typeLabel.ForeColor = theme.TextSecondary;
            _noSelectionLabel.ForeColor = theme.TextSecondary;
            RebuildUI();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置选中的节点
        /// </summary>
        public void SetSelectedNode(FlowNode node)
        {
            _selectedNode = node;
            _validationResults.Clear();
            RebuildUI();
        }

        /// <summary>
        /// 聚焦到名称编辑字段（用于F2重命名）
        /// </summary>
        public void FocusNameField()
        {
            if (_propertyControls.TryGetValue("DisplayName", out var control))
            {
                control.Focus();
                if (control is TextBox textBox)
                {
                    textBox.SelectAll();
                }
            }
            else if (_propertyControls.TryGetValue("Name", out var nameControl))
            {
                nameControl.Focus();
                if (nameControl is TextBox textBox)
                {
                    textBox.SelectAll();
                }
            }
        }

        /// <summary>
        /// 刷新属性面板
        /// </summary>
        public void RefreshProperties()
        {
            RebuildUI();
        }

        /// <summary>
        /// 注册属性定义
        /// </summary>
        public void RegisterProperty(string groupName, PropertyDefinition property)
        {
            var group = _propertyGroups.FirstOrDefault(g => g.Name == groupName);
            if (group == null)
            {
                group = new PropertyGroup
                {
                    Name = groupName,
                    DisplayName = groupName,
                    Order = _propertyGroups.Count
                };
                _propertyGroups.Add(group);
            }

            var existing = group.Properties.FirstOrDefault(p => p.Name == property.Name);
            if (existing != null)
            {
                group.Properties.Remove(existing);
            }
            group.Properties.Add(property);
        }

        /// <summary>
        /// 验证所有属性
        /// </summary>
        public bool ValidateAll()
        {
            bool allValid = true;
            _validationResults.Clear();

            foreach (var group in _propertyGroups)
            {
                foreach (var prop in group.Properties)
                {
                    var value = GetPropertyValue(prop.Name);
                    var result = ValidateProperty(prop, value);
                    _validationResults[prop.Name] = result;
                    if (!result.IsValid)
                    {
                        allValid = false;
                        PropertyValidationFailed?.Invoke(this, result);
                    }
                }
            }

            RebuildUI();
            return allValid;
        }

        #endregion

        #region 7.2.1 动态属性渲染

        private void RebuildUI()
        {
            _isUpdating = true;
            _contentPanel.Controls.Clear();
            _propertyControls.Clear();
            _groupHeaderRects.Clear();

            if (_selectedNode == null || _selectedNode.Data == null)
            {
                _titleLabel.Text = "属性";
                _typeLabel.Text = "";
                _contentPanel.Controls.Add(_noSelectionLabel);
                _isUpdating = false;
                return;
            }

            var data = _selectedNode.Data;
            _titleLabel.Text = data.DisplayName ?? data.Name ?? "节点属性";
            _typeLabel.Text = GetTypeDisplayName(data.Type);

            var theme = ThemeManager.Instance.CurrentTheme;
            int y = PanelPadding;

            foreach (var group in _propertyGroups.OrderBy(g => g.Order))
            {
                var properties = group.Properties.OrderBy(p => p.Order).ToList();

                // 添加自定义属性到custom组
                if (group.Name == "custom" && data.Properties != null && data.Properties.Count > 0)
                {
                    foreach (var kvp in data.Properties)
                    {
                        if (!properties.Any(p => p.Name == kvp.Key))
                        {
                            properties.Add(new PropertyDefinition
                            {
                                Name = kvp.Key,
                                DisplayName = kvp.Key,
                                PropertyType = kvp.Value?.GetType() ?? typeof(string)
                            });
                        }
                    }
                }

                if (properties.Count == 0)
                    continue;

                // 7.2.5 绘制分组头
                var groupHeader = CreateGroupHeader(group, theme);
                groupHeader.Location = new Point(0, y);
                groupHeader.Width = _contentPanel.Width - SystemInformation.VerticalScrollBarWidth - PanelPadding;
                groupHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _contentPanel.Controls.Add(groupHeader);
                y += GroupHeaderHeight + 4;

                if (group.IsExpanded)
                {
                    foreach (var prop in properties)
                    {
                        var propRow = CreatePropertyRow(prop, theme);
                        propRow.Location = new Point(PanelPadding, y);
                        propRow.Width = _contentPanel.Width - PanelPadding * 2 - SystemInformation.VerticalScrollBarWidth;
                        propRow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        _contentPanel.Controls.Add(propRow);
                        y += PropertyRowHeight + 4;
                    }
                }

                y += 8;
            }

            _contentPanel.AutoScrollMinSize = new Size(0, y + 20);
            _isUpdating = false;
        }

        private Panel CreateGroupHeader(PropertyGroup group, ThemeConfig theme)
        {
            var panel = new Panel
            {
                Height = GroupHeaderHeight,
                BackColor = Color.FromArgb(20, theme.Primary),
                Cursor = Cursors.Hand,
                Tag = group.Name
            };

            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 折叠箭头
                using (var pen = new Pen(theme.TextSecondary, 2))
                {
                    var arrowX = 12;
                    var arrowY = panel.Height / 2;
                    if (group.IsExpanded)
                    {
                        g.DrawLine(pen, arrowX - 4, arrowY - 2, arrowX, arrowY + 4);
                        g.DrawLine(pen, arrowX, arrowY + 4, arrowX + 4, arrowY - 2);
                    }
                    else
                    {
                        g.DrawLine(pen, arrowX - 2, arrowY - 4, arrowX + 4, arrowY);
                        g.DrawLine(pen, arrowX + 4, arrowY, arrowX - 2, arrowY + 4);
                    }
                }

                // 组名称
                using (var font = new Font("Segoe UI Semibold", 9))
                using (var brush = new SolidBrush(theme.TextPrimary))
                {
                    g.DrawString(group.DisplayName, font, brush, 28, (panel.Height - font.Height) / 2);
                }
            };

            panel.Click += (s, e) =>
            {
                group.IsExpanded = !group.IsExpanded;
                RebuildUI();
            };

            return panel;
        }

        /// <summary>
        /// 7.2.4 创建属性行（支持复杂类型编辑器）
        /// </summary>
        private Panel CreatePropertyRow(PropertyDefinition prop, ThemeConfig theme)
        {
            var panel = new Panel
            {
                Height = PropertyRowHeight,
                BackColor = Color.Transparent
            };

            // 标签
            var label = new Label
            {
                Text = prop.DisplayName + ":",
                Font = new Font("Segoe UI", 9),
                ForeColor = theme.TextSecondary,
                Location = new Point(0, 4),
                Width = LabelWidth,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(label);

            // 获取当前值
            var value = GetPropertyValue(prop.Name);

            // 根据类型创建编辑器
            Control editor = CreateEditor(prop, value, theme);
            if (editor != null)
            {
                editor.Location = new Point(LabelWidth + 4, 2);
                editor.Width = panel.Width - LabelWidth - 8;
                editor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                panel.Controls.Add(editor);
                _propertyControls[prop.Name] = editor;
            }

            // 7.2.2 验证错误提示
            if (_validationResults.TryGetValue(prop.Name, out var validation) && !validation.IsValid)
            {
                var errorIcon = new Label
                {
                    Text = "⚠",
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 10),
                    Location = new Point(panel.Width - 24, 4),
                    Size = new Size(20, 20),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                var toolTip = new ToolTip();
                toolTip.SetToolTip(errorIcon, validation.Message);
                panel.Controls.Add(errorIcon);
            }

            return panel;
        }

        /// <summary>
        /// 创建属性编辑器
        /// </summary>
        private Control CreateEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            if (prop.IsReadOnly)
            {
                return new Label
                {
                    Text = value?.ToString() ?? "",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = theme.TextPrimary,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleLeft
                };
            }

            // 检查是否是表达式类型（属性名包含expression或condition）
            if (IsExpressionProperty(prop.Name))
            {
                return CreateExpressionEditor(prop, value, theme);
            }

            // 检查是否是代码类型
            if (prop.Name.ToLower() == "code" || prop.Name.ToLower() == "sourcecode")
            {
                return CreateCodeEditor(prop, value, theme);
            }

            // 检查是否是多行文本
            if (prop.Name.ToLower() == "description" || prop.Name.ToLower() == "notes")
            {
                return CreateMultilineTextEditor(prop, value, theme);
            }

            // 检查是否是文件路径
            if (prop.Name.ToLower().Contains("path") || prop.Name.ToLower().Contains("file"))
            {
                return CreateFilePathEditor(prop, value, theme);
            }

            // 检查是否是日期时间
            if (prop.PropertyType == typeof(DateTime))
            {
                return CreateDateTimeEditor(prop, value, theme);
            }

            // 检查是否是JSON/字典类型
            if (prop.PropertyType == typeof(Dictionary<string, object>) || 
                prop.Name.ToLower() == "settings" || prop.Name.ToLower() == "properties" ||
                prop.Name.ToLower() == "input" || prop.Name.ToLower() == "output")
            {
                return CreateJsonEditor(prop, value, theme);
            }

            // 下拉选择
            if (prop.Options != null && prop.Options.Length > 0)
            {
                var combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9),
                    Height = 24
                };
                combo.Items.AddRange(prop.Options);
                if (value != null)
                {
                    combo.SelectedItem = value.ToString();
                }
                combo.SelectedIndexChanged += (s, e) =>
                {
                    if (!_isUpdating)
                        SetPropertyValue(prop.Name, combo.SelectedItem?.ToString());
                };
                return combo;
            }

            // 枚举类型
            if (prop.PropertyType != null && prop.PropertyType.IsEnum)
            {
                var combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9),
                    Height = 24
                };
                combo.Items.AddRange(Enum.GetNames(prop.PropertyType));
                if (value != null)
                {
                    combo.SelectedItem = value.ToString();
                }
                combo.SelectedIndexChanged += (s, e) =>
                {
                    if (!_isUpdating && combo.SelectedItem != null)
                    {
                        var enumValue = Enum.Parse(prop.PropertyType, combo.SelectedItem.ToString());
                        SetPropertyValue(prop.Name, enumValue);
                    }
                };
                return combo;
            }

            // 布尔类型
            if (prop.PropertyType == typeof(bool))
            {
                var check = new CheckBox
                {
                    Checked = value is bool b && b,
                    Font = new Font("Segoe UI", 9),
                    Height = 24
                };
                check.CheckedChanged += (s, e) =>
                {
                    if (!_isUpdating)
                        SetPropertyValue(prop.Name, check.Checked);
                };
                return check;
            }

            // 数字类型
            if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
            {
                var numBox = new NumericUpDown
                {
                    Font = new Font("Segoe UI", 9),
                    Height = 24,
                    DecimalPlaces = prop.PropertyType == typeof(int) ? 0 : 2,
                    Minimum = decimal.MinValue,
                    Maximum = decimal.MaxValue
                };
                if (value != null && decimal.TryParse(value.ToString(), out var num))
                {
                    numBox.Value = num;
                }
                numBox.ValueChanged += (s, e) =>
                {
                    if (!_isUpdating)
                    {
                        object newValue = numBox.Value;
                        if (prop.PropertyType == typeof(int))
                            newValue = (int)numBox.Value;
                        else if (prop.PropertyType == typeof(float))
                            newValue = (float)numBox.Value;
                        else if (prop.PropertyType == typeof(double))
                            newValue = (double)numBox.Value;
                        SetPropertyValue(prop.Name, newValue);
                    }
                };
                return numBox;
            }

            // 颜色类型
            if (prop.PropertyType == typeof(Color))
            {
                var colorPanel = new Panel
                {
                    Height = 24,
                    BackColor = value is Color c ? c : Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };
                colorPanel.Click += (s, e) =>
                {
                    using (var dlg = new ColorDialog())
                    {
                        dlg.Color = colorPanel.BackColor;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            colorPanel.BackColor = dlg.Color;
                            SetPropertyValue(prop.Name, dlg.Color);
                        }
                    }
                };
                return colorPanel;
            }

            // 默认文本框
            var textBox = new TextBox
            {
                Text = value?.ToString() ?? "",
                Font = new Font("Segoe UI", 9),
                Height = 24
            };
            textBox.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                    SetPropertyValue(prop.Name, textBox.Text);
            };
            return textBox;
        }

        #endregion

        #region 7.2.2 属性验证

        private ValidationResult ValidateProperty(PropertyDefinition prop, object value)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                PropertyName = prop.Name
            };

            // 必填验证
            if (prop.IsRequired && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                result.IsValid = false;
                result.Message = string.Format("{0}是必填项", prop.DisplayName);
                return result;
            }

            // 自定义验证器
            if (prop.Validator != null && !prop.Validator(value))
            {
                result.IsValid = false;
                result.Message = prop.ValidationMessage ?? string.Format("{0}验证失败", prop.DisplayName);
                return result;
            }

            return result;
        }

        #endregion

        #region 7.2.3 属性变更通知

        private object GetPropertyValue(string propertyName)
        {
            if (_selectedNode?.Data == null) return null;

            var data = _selectedNode.Data;
            switch (propertyName)
            {
                case "Name": return data.Name;
                case "DisplayName": return data.DisplayName;
                case "Description": return data.Description;
                case "Type": return data.Type;
                case "Skip": return data.Skip;
                case "Status": return data.Status;
                case "IconPath": return data.IconPath;
                default:
                    // 查找自定义属性
                    if (data.Properties != null && data.Properties.TryGetValue(propertyName, out var value))
                    {
                        return value;
                    }
                    return null;
            }
        }

        private void SetPropertyValue(string propertyName, object value)
        {
            if (_selectedNode?.Data == null) return;

            var data = _selectedNode.Data;
            var oldValue = GetPropertyValue(propertyName);

            switch (propertyName)
            {
                case "Name":
                    data.Name = value?.ToString();
                    break;
                case "DisplayName":
                    data.DisplayName = value?.ToString();
                    break;
                case "Description":
                    data.Description = value?.ToString();
                    break;
                case "Skip":
                    data.Skip = value is bool b && b;
                    break;
                case "IconPath":
                    data.IconPath = value?.ToString();
                    break;
                default:
                    // 设置自定义属性
                    if (data.Properties == null)
                        data.Properties = new Dictionary<string, object>();
                    data.Properties[propertyName] = value;
                    break;
            }

            // 验证
            var prop = _propertyGroups.SelectMany(g => g.Properties).FirstOrDefault(p => p.Name == propertyName);
            if (prop != null)
            {
                var result = ValidateProperty(prop, value);
                _validationResults[propertyName] = result;
                if (!result.IsValid)
                {
                    PropertyValidationFailed?.Invoke(this, result);
                }
            }

            // 触发变更事件
            PropertyValueChanged?.Invoke(this, new PropertyChangedEventArgs
            {
                Node = _selectedNode,
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = value
            });
        }

        #endregion

        #region 高级编辑器

        /// <summary>
        /// 检查是否是表达式属性
        /// </summary>
        private bool IsExpressionProperty(string propertyName)
        {
            var lowerName = propertyName.ToLower();
            return lowerName.Contains("expression") || 
                   lowerName.Contains("condition") ||
                   lowerName.Contains("items") ||
                   lowerName.Contains("value");
        }

        /// <summary>
        /// 创建表达式编辑器
        /// 支持 {{step.output}} 等表达式语法
        /// </summary>
        private Control CreateExpressionEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            var panel = new Panel
            {
                Height = 28,
                BackColor = Color.Transparent
            };

            var textBox = new TextBox
            {
                Text = value?.ToString() ?? "",
                Font = new Font("Consolas", 9),
                ForeColor = theme.TextPrimary,
                BackColor = theme.NodeBackground,
                Location = new Point(0, 0),
                Width = panel.Width - 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            
            // 表达式高亮
            textBox.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                {
                    SetPropertyValue(prop.Name, textBox.Text);
                    // 简单的表达式验证
                    var text = textBox.Text;
                    if (text.Contains("{{") && !text.Contains("}}"))
                    {
                        textBox.BackColor = Color.FromArgb(255, 245, 245); // 浅红色表示未闭合
                    }
                    else if (text.Contains("{{") && text.Contains("}}"))
                    {
                        textBox.BackColor = Color.FromArgb(245, 255, 245); // 浅绿色表示有效表达式
                    }
                    else
                    {
                        textBox.BackColor = theme.NodeBackground;
                    }
                }
            };

            // 插入表达式按钮
            var insertBtn = new Button
            {
                Text = "fx",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = theme.Primary,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(panel.Width - 28, 0),
                Size = new Size(26, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            insertBtn.FlatAppearance.BorderColor = theme.Border;
            insertBtn.Click += (s, e) =>
            {
                // 显示表达式选择器（简化版：插入模板）
                ShowExpressionPicker(textBox);
            };

            panel.Controls.Add(textBox);
            panel.Controls.Add(insertBtn);
            return panel;
        }

        /// <summary>
        /// 显示表达式选择器
        /// </summary>
        private void ShowExpressionPicker(TextBox targetTextBox)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("插入步骤引用 {{step.output}}", null, (s, e) =>
            {
                InsertTextAtCursor(targetTextBox, "{{step.output}}");
            });
            menu.Items.Add("插入变量引用 {{variable}}", null, (s, e) =>
            {
                InsertTextAtCursor(targetTextBox, "{{variable}}");
            });
            menu.Items.Add("插入触发器数据 {{trigger.body}}", null, (s, e) =>
            {
                InsertTextAtCursor(targetTextBox, "{{trigger.body}}");
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("字符串连接 {{a + b}}", null, (s, e) =>
            {
                InsertTextAtCursor(targetTextBox, "{{a + b}}");
            });
            menu.Items.Add("条件表达式 {{a ? b : c}}", null, (s, e) =>
            {
                InsertTextAtCursor(targetTextBox, "{{condition ? trueValue : falseValue}}");
            });
            
            menu.Show(targetTextBox, new Point(0, targetTextBox.Height));
        }

        /// <summary>
        /// 在光标处插入文本
        /// </summary>
        private void InsertTextAtCursor(TextBox textBox, string text)
        {
            int selectionStart = textBox.SelectionStart;
            textBox.Text = textBox.Text.Insert(selectionStart, text);
            textBox.SelectionStart = selectionStart + text.Length;
            textBox.Focus();
        }

        /// <summary>
        /// 创建代码编辑器
        /// </summary>
        private Control CreateCodeEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            var panel = new Panel
            {
                Height = 120,
                BackColor = Color.Transparent
            };

            var textBox = new TextBox
            {
                Text = value?.ToString() ?? "",
                Font = new Font("Consolas", 9),
                ForeColor = theme.TextPrimary,
                BackColor = Color.FromArgb(30, 30, 30), // 深色代码背景
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsTab = true,
                AcceptsReturn = true,
                Dock = DockStyle.Fill
            };

            textBox.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                    SetPropertyValue(prop.Name, textBox.Text);
            };

            // 简单的语法高亮（通过RichTextBox可以实现更好的效果）
            textBox.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    textBox.SelectAll();
                    e.SuppressKeyPress = true;
                }
            };

            // 工具栏
            var toolbar = new Panel
            {
                Height = 24,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 45)
            };

            var formatBtn = new Button
            {
                Text = "格式化",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(2, 2),
                Size = new Size(50, 20),
                Cursor = Cursors.Hand
            };
            formatBtn.FlatAppearance.BorderSize = 0;
            formatBtn.Click += (s, e) =>
            {
                // 简单的JSON格式化
                try
                {
                    var formatted = FormatJson(textBox.Text);
                    textBox.Text = formatted;
                }
                catch { }
            };

            toolbar.Controls.Add(formatBtn);
            panel.Controls.Add(textBox);
            panel.Controls.Add(toolbar);

            return panel;
        }

        /// <summary>
        /// 简单的JSON格式化
        /// </summary>
        private string FormatJson(string json)
        {
            try
            {
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// 创建多行文本编辑器
        /// </summary>
        private Control CreateMultilineTextEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            var textBox = new TextBox
            {
                Text = value?.ToString() ?? "",
                Font = new Font("Segoe UI", 9),
                ForeColor = theme.TextPrimary,
                BackColor = theme.NodeBackground,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 60
            };

            textBox.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                    SetPropertyValue(prop.Name, textBox.Text);
            };

            return textBox;
        }

        /// <summary>
        /// 创建文件路径编辑器
        /// </summary>
        private Control CreateFilePathEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            var panel = new Panel
            {
                Height = 24,
                BackColor = Color.Transparent
            };

            var textBox = new TextBox
            {
                Text = value?.ToString() ?? "",
                Font = new Font("Segoe UI", 9),
                ForeColor = theme.TextPrimary,
                BackColor = theme.NodeBackground,
                Location = new Point(0, 0),
                Width = panel.Width - 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            textBox.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                    SetPropertyValue(prop.Name, textBox.Text);
            };

            var browseBtn = new Button
            {
                Text = "...",
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(panel.Width - 28, 0),
                Size = new Size(26, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            browseBtn.FlatAppearance.BorderColor = theme.Border;
            browseBtn.Click += (s, e) =>
            {
                if (prop.Name.ToLower().Contains("folder") || prop.Name.ToLower().Contains("directory"))
                {
                    using (var dlg = new FolderBrowserDialog())
                    {
                        if (!string.IsNullOrEmpty(textBox.Text))
                            dlg.SelectedPath = textBox.Text;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            textBox.Text = dlg.SelectedPath;
                        }
                    }
                }
                else
                {
                    using (var dlg = new OpenFileDialog())
                    {
                        if (!string.IsNullOrEmpty(textBox.Text))
                            dlg.FileName = textBox.Text;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            textBox.Text = dlg.FileName;
                        }
                    }
                }
            };

            panel.Controls.Add(textBox);
            panel.Controls.Add(browseBtn);
            return panel;
        }

        /// <summary>
        /// 创建日期时间编辑器
        /// </summary>
        private Control CreateDateTimeEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            var picker = new DateTimePicker
            {
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss"
            };

            if (value is DateTime dt)
            {
                picker.Value = dt;
            }

            picker.ValueChanged += (s, e) =>
            {
                if (!_isUpdating)
                    SetPropertyValue(prop.Name, picker.Value);
            };

            return picker;
        }

        /// <summary>
        /// 创建JSON编辑器
        /// </summary>
        private Control CreateJsonEditor(PropertyDefinition prop, object value, ThemeConfig theme)
        {
            var panel = new Panel
            {
                Height = 80,
                BackColor = Color.Transparent
            };

            string jsonText = "";
            if (value is Dictionary<string, object> dict)
            {
                try
                {
                    jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    jsonText = value?.ToString() ?? "{}";
                }
            }
            else
            {
                jsonText = value?.ToString() ?? "{}";
            }

            var textBox = new TextBox
            {
                Text = jsonText,
                Font = new Font("Consolas", 9),
                ForeColor = theme.TextPrimary,
                BackColor = theme.NodeBackground,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill
            };

            textBox.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                {
                    try
                    {
                        var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(textBox.Text);
                        SetPropertyValue(prop.Name, parsed);
                        textBox.BackColor = theme.NodeBackground;
                    }
                    catch
                    {
                        // JSON解析失败，显示错误颜色
                        textBox.BackColor = Color.FromArgb(255, 245, 245);
                    }
                }
            };

            panel.Controls.Add(textBox);
            return panel;
        }

        #endregion

        #region 辅助方法

        private string GetTypeDisplayName(FlowNodeType type)
        {
            switch (type)
            {
                case FlowNodeType.Start: return "开始节点";
                case FlowNodeType.Process: return "处理节点";
                case FlowNodeType.Decision: return "判断节点";
                case FlowNodeType.Loop: return "循环节点";
                case FlowNodeType.End: return "结束节点";
                case FlowNodeType.Code: return "代码节点";
                case FlowNodeType.Piece: return "组件节点";
                case FlowNodeType.Group: return "分组节点";
                default: return type.ToString();
            }
        }

        #endregion
    }
}
