using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 流程设计器上下文菜单
    /// 支持节点操作、连接操作、画布操作
    /// </summary>
    public class FlowContextMenu : ContextMenuStrip
    {
        #region 事件

        /// <summary>
        /// 添加节点请求
        /// </summary>
        public event EventHandler<AddNodeEventArgs> AddNodeRequested;
        
        /// <summary>
        /// 粘贴请求
        /// </summary>
        public event EventHandler<PasteEventArgs> PasteRequested;
        
        /// <summary>
        /// 复制请求
        /// </summary>
        public event EventHandler CopyRequested;
        
        /// <summary>
        /// 剪切请求
        /// </summary>
        public event EventHandler CutRequested;
        
        /// <summary>
        /// 删除请求
        /// </summary>
        public event EventHandler DeleteRequested;
        
        /// <summary>
        /// 属性请求
        /// </summary>
        public event EventHandler PropertiesRequested;
        
        /// <summary>
        /// 跳过节点请求
        /// </summary>
        public event EventHandler SkipNodeRequested;
        
        /// <summary>
        /// 替换节点请求
        /// </summary>
        public event EventHandler<FlowNodeType> ReplaceNodeRequested;
        
        /// <summary>
        /// 复制节点请求
        /// </summary>
        public event EventHandler DuplicateRequested;
        
        /// <summary>
        /// 添加分支请求
        /// </summary>
        public event EventHandler AddBranchRequested;
        
        /// <summary>
        /// 删除分支请求
        /// </summary>
        public event EventHandler<int> DeleteBranchRequested;
        
        /// <summary>
        /// 重命名请求
        /// </summary>
        public event EventHandler RenameRequested;
        
        /// <summary>
        /// 全选请求
        /// </summary>
        public event EventHandler SelectAllRequested;
        
        /// <summary>
        /// 适应视图请求
        /// </summary>
        public event EventHandler FitToViewRequested;
        
        /// <summary>
        /// 撤销请求
        /// </summary>
        public event EventHandler UndoRequested;
        
        /// <summary>
        /// 重做请求
        /// </summary>
        public event EventHandler RedoRequested;

        #endregion

        #region 事件参数类

        public class AddNodeEventArgs : EventArgs
        {
            public FlowNodeType NodeType { get; set; }
            public PointF Position { get; set; }
            public string ParentNodeName { get; set; }
            public InsertPosition InsertPosition { get; set; }
        }

        public class PasteEventArgs : EventArgs
        {
            public PointF Position { get; set; }
            public string TargetNodeName { get; set; }
            public PasteTarget Target { get; set; }
        }

        public enum InsertPosition
        {
            After,
            Before,
            IntoLoop,
            IntoBranch
        }

        public enum PasteTarget
        {
            Canvas,
            AfterNode,
            IntoLoop,
            IntoBranch
        }

        #endregion

        #region 字段

        private FlowNode _targetNode;
        private PointF _menuPosition;
        private bool _canUndo;
        private bool _canRedo;

        #endregion

        #region 构造

        public FlowContextMenu()
        {
            this.Renderer = new FlowContextMenuRenderer();
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            // 刷新菜单样式
            this.Invalidate();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置撤销/重做状态
        /// </summary>
        public void SetUndoRedoState(bool canUndo, bool canRedo)
        {
            _canUndo = canUndo;
            _canRedo = canRedo;
        }

        /// <summary>
        /// 显示画布上下文菜单
        /// </summary>
        public void ShowCanvasMenu(Control control, Point location, PointF canvasPosition)
        {
            _targetNode = null;
            _menuPosition = canvasPosition;
            BuildCanvasMenu();
            this.Show(control, location);
        }

        /// <summary>
        /// 显示节点上下文菜单
        /// </summary>
        public void ShowNodeMenu(Control control, Point location, FlowNode node)
        {
            _targetNode = node;
            _menuPosition = node != null ? new PointF(node.X, node.Y) : PointF.Empty;
            BuildNodeMenu(node);
            this.Show(control, location);
        }

        /// <summary>
        /// 显示连接线上下文菜单
        /// </summary>
        public void ShowConnectionMenu(Control control, Point location, PointF connectionPoint)
        {
            _targetNode = null;
            _menuPosition = connectionPoint;
            BuildConnectionMenu();
            this.Show(control, location);
        }

        /// <summary>
        /// 显示多选节点上下文菜单
        /// </summary>
        public void ShowMultiSelectMenu(Control control, Point location, int selectedCount)
        {
            _targetNode = null;
            BuildMultiSelectMenu(selectedCount);
            this.Show(control, location);
        }

        #endregion

        #region 菜单构建

        /// <summary>
        /// 构建画布菜单
        /// </summary>
        private void BuildCanvasMenu()
        {
            this.Items.Clear();

            // 添加节点子菜单
            var addNodeMenu = CreateAddNodeSubmenu();
            this.Items.Add(addNodeMenu);

            this.Items.Add(new ToolStripSeparator());

            // 粘贴
            var pasteItem = new ToolStripMenuItem("粘贴", null, (s, e) =>
            {
                PasteRequested?.Invoke(this, new PasteEventArgs
                {
                    Position = _menuPosition,
                    Target = PasteTarget.Canvas
                });
            })
            {
                ShortcutKeyDisplayString = "Ctrl+V",
                Enabled = Clipboard.FlowClipboard.HasFlowData()
            };
            this.Items.Add(pasteItem);

            this.Items.Add(new ToolStripSeparator());

            // 撤销
            var undoItem = new ToolStripMenuItem("撤销", null, (s, e) => UndoRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+Z",
                Enabled = _canUndo
            };
            this.Items.Add(undoItem);

            // 重做
            var redoItem = new ToolStripMenuItem("重做", null, (s, e) => RedoRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+Y",
                Enabled = _canRedo
            };
            this.Items.Add(redoItem);

            this.Items.Add(new ToolStripSeparator());

            // 全选
            var selectAllItem = new ToolStripMenuItem("全选", null, (s, e) => SelectAllRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+A"
            };
            this.Items.Add(selectAllItem);

            // 适应视图
            var fitViewItem = new ToolStripMenuItem("适应视图", null, (s, e) => FitToViewRequested?.Invoke(this, EventArgs.Empty));
            this.Items.Add(fitViewItem);
        }

        /// <summary>
        /// 构建节点菜单
        /// </summary>
        private void BuildNodeMenu(FlowNode node)
        {
            this.Items.Clear();
            var nodeData = node?.Data;

            // 复制
            var copyItem = new ToolStripMenuItem("复制", null, (s, e) => CopyRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };
            this.Items.Add(copyItem);

            // 剪切
            var cutItem = new ToolStripMenuItem("剪切", null, (s, e) => CutRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+X"
            };
            this.Items.Add(cutItem);

            // 粘贴
            var pasteMenu = CreatePasteSubmenu(node);
            this.Items.Add(pasteMenu);

            // 复制节点
            var duplicateItem = new ToolStripMenuItem("复制节点", null, (s, e) => DuplicateRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+D"
            };
            this.Items.Add(duplicateItem);

            this.Items.Add(new ToolStripSeparator());

            // 删除
            var deleteItem = new ToolStripMenuItem("删除", null, (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Delete",
                // 开始节点不能删除
                Enabled = nodeData?.Type != FlowNodeType.Start
            };
            this.Items.Add(deleteItem);

            this.Items.Add(new ToolStripSeparator());

            // 跳过/取消跳过
            bool isSkipped = nodeData?.Skip ?? false;
            var skipItem = new ToolStripMenuItem(isSkipped ? "取消跳过" : "跳过节点", null, (s, e) => SkipNodeRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+E",
                Checked = isSkipped,
                // 开始和结束节点不能跳过
                Enabled = nodeData?.Type != FlowNodeType.Start && nodeData?.Type != FlowNodeType.End
            };
            this.Items.Add(skipItem);

            // 替换节点（子菜单）
            var replaceMenu = CreateReplaceSubmenu(node);
            if (replaceMenu != null)
            {
                this.Items.Add(replaceMenu);
            }

            this.Items.Add(new ToolStripSeparator());

            // 根据节点类型添加特殊菜单项
            if (nodeData?.Type == FlowNodeType.Decision)
            {
                // 判断节点：添加/删除分支
                var addBranchItem = new ToolStripMenuItem("添加分支", null, (s, e) => AddBranchRequested?.Invoke(this, EventArgs.Empty));
                this.Items.Add(addBranchItem);

                var deleteBranchMenu = new ToolStripMenuItem("删除分支");
                // TODO: 动态添加分支列表
                deleteBranchMenu.DropDownItems.Add(new ToolStripMenuItem("分支 1", null, (s, e) => DeleteBranchRequested?.Invoke(this, 0)));
                deleteBranchMenu.DropDownItems.Add(new ToolStripMenuItem("分支 2", null, (s, e) => DeleteBranchRequested?.Invoke(this, 1)));
                this.Items.Add(deleteBranchMenu);

                this.Items.Add(new ToolStripSeparator());
            }
            else if (nodeData?.Type == FlowNodeType.Loop)
            {
                // 循环节点：粘贴到循环内
                // 已在粘贴子菜单中处理
            }

            // 重命名
            var renameItem = new ToolStripMenuItem("重命名", null, (s, e) => RenameRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "F2"
            };
            this.Items.Add(renameItem);

            // 属性
            var propertiesItem = new ToolStripMenuItem("属性...", null, (s, e) => PropertiesRequested?.Invoke(this, EventArgs.Empty));
            this.Items.Add(propertiesItem);
        }

        /// <summary>
        /// 构建多选菜单
        /// </summary>
        private void BuildMultiSelectMenu(int selectedCount)
        {
            this.Items.Clear();

            // 显示选中数量
            var infoItem = new ToolStripMenuItem($"已选中 {selectedCount} 个节点")
            {
                Enabled = false
            };
            this.Items.Add(infoItem);

            this.Items.Add(new ToolStripSeparator());

            // 复制
            var copyItem = new ToolStripMenuItem("复制", null, (s, e) => CopyRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };
            this.Items.Add(copyItem);

            // 剪切
            var cutItem = new ToolStripMenuItem("剪切", null, (s, e) => CutRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+X"
            };
            this.Items.Add(cutItem);

            // 删除
            var deleteItem = new ToolStripMenuItem("删除", null, (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Delete"
            };
            this.Items.Add(deleteItem);

            this.Items.Add(new ToolStripSeparator());

            // 跳过所有
            var skipAllItem = new ToolStripMenuItem("跳过所有", null, (s, e) => SkipNodeRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+E"
            };
            this.Items.Add(skipAllItem);

            // 复制节点
            var duplicateItem = new ToolStripMenuItem("复制节点", null, (s, e) => DuplicateRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Ctrl+D"
            };
            this.Items.Add(duplicateItem);
        }

        /// <summary>
        /// 构建连接菜单
        /// </summary>
        private void BuildConnectionMenu()
        {
            this.Items.Clear();

            // 删除连接
            var deleteItem = new ToolStripMenuItem("删除连接", null, (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty))
            {
                ShortcutKeyDisplayString = "Delete"
            };
            this.Items.Add(deleteItem);

            this.Items.Add(new ToolStripSeparator());

            // 在连接中间添加节点
            var addNodeMenu = new ToolStripMenuItem("插入节点");
            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("处理节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Process,
                    Position = _menuPosition,
                    InsertPosition = InsertPosition.After
                });
            }));
            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("判断节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Decision,
                    Position = _menuPosition,
                    InsertPosition = InsertPosition.After
                });
            }));
            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("循环节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Loop,
                    Position = _menuPosition,
                    InsertPosition = InsertPosition.After
                });
            }));
            this.Items.Add(addNodeMenu);
        }

        /// <summary>
        /// 创建添加节点子菜单
        /// </summary>
        private ToolStripMenuItem CreateAddNodeSubmenu()
        {
            var addNodeMenu = new ToolStripMenuItem("添加节点");

            // 基础节点
            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("开始节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Start,
                    Position = _menuPosition
                });
            }));

            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("处理节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Process,
                    Position = _menuPosition
                });
            }));

            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("判断节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Decision,
                    Position = _menuPosition
                });
            }));

            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("循环节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Loop,
                    Position = _menuPosition
                });
            }));

            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("结束节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.End,
                    Position = _menuPosition
                });
            }));

            addNodeMenu.DropDownItems.Add(new ToolStripSeparator());

            // 高级节点
            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("代码节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Code,
                    Position = _menuPosition
                });
            }));

            addNodeMenu.DropDownItems.Add(new ToolStripMenuItem("组件节点", null, (s, e) =>
            {
                AddNodeRequested?.Invoke(this, new AddNodeEventArgs
                {
                    NodeType = FlowNodeType.Piece,
                    Position = _menuPosition
                });
            }));

            return addNodeMenu;
        }

        /// <summary>
        /// 创建粘贴子菜单
        /// </summary>
        private ToolStripMenuItem CreatePasteSubmenu(FlowNode node)
        {
            var pasteMenu = new ToolStripMenuItem("粘贴")
            {
                Enabled = Clipboard.FlowClipboard.HasFlowData()
            };

            // 粘贴到节点后
            pasteMenu.DropDownItems.Add(new ToolStripMenuItem("粘贴到后面", null, (s, e) =>
            {
                PasteRequested?.Invoke(this, new PasteEventArgs
                {
                    TargetNodeName = node?.Data?.Name,
                    Target = PasteTarget.AfterNode
                });
            })
            {
                ShortcutKeyDisplayString = "Ctrl+V"
            });

            // 如果是循环节点，添加"粘贴到循环内"
            if (node?.Data?.Type == FlowNodeType.Loop)
            {
                pasteMenu.DropDownItems.Add(new ToolStripMenuItem("粘贴到循环内", null, (s, e) =>
                {
                    PasteRequested?.Invoke(this, new PasteEventArgs
                    {
                        TargetNodeName = node?.Data?.Name,
                        Target = PasteTarget.IntoLoop
                    });
                }));
            }

            // 如果是判断节点，添加"粘贴到分支内"
            if (node?.Data?.Type == FlowNodeType.Decision)
            {
                var pasteToBranchMenu = new ToolStripMenuItem("粘贴到分支内");
                // TODO: 动态添加分支列表
                pasteToBranchMenu.DropDownItems.Add(new ToolStripMenuItem("分支 1", null, (s, e) =>
                {
                    PasteRequested?.Invoke(this, new PasteEventArgs
                    {
                        TargetNodeName = node?.Data?.Name,
                        Target = PasteTarget.IntoBranch
                    });
                }));
                pasteMenu.DropDownItems.Add(pasteToBranchMenu);
            }

            return pasteMenu;
        }

        /// <summary>
        /// 创建替换节点子菜单
        /// </summary>
        private ToolStripMenuItem CreateReplaceSubmenu(FlowNode node)
        {
            var nodeType = node?.Data?.Type;
            
            // 开始和结束节点不能替换
            if (nodeType == FlowNodeType.Start || nodeType == FlowNodeType.End)
                return null;

            var replaceMenu = new ToolStripMenuItem("替换为");

            // 只显示可以替换的类型
            if (nodeType != FlowNodeType.Process)
            {
                replaceMenu.DropDownItems.Add(new ToolStripMenuItem("处理节点", null, (s, e) =>
                {
                    ReplaceNodeRequested?.Invoke(this, FlowNodeType.Process);
                }));
            }

            if (nodeType != FlowNodeType.Decision)
            {
                replaceMenu.DropDownItems.Add(new ToolStripMenuItem("判断节点", null, (s, e) =>
                {
                    ReplaceNodeRequested?.Invoke(this, FlowNodeType.Decision);
                }));
            }

            if (nodeType != FlowNodeType.Loop)
            {
                replaceMenu.DropDownItems.Add(new ToolStripMenuItem("循环节点", null, (s, e) =>
                {
                    ReplaceNodeRequested?.Invoke(this, FlowNodeType.Loop);
                }));
            }

            if (nodeType != FlowNodeType.Code)
            {
                replaceMenu.DropDownItems.Add(new ToolStripMenuItem("代码节点", null, (s, e) =>
                {
                    ReplaceNodeRequested?.Invoke(this, FlowNodeType.Code);
                }));
            }

            return replaceMenu;
        }

        #endregion
    }

    #region 自定义渲染器

    /// <summary>
    /// 上下文菜单自定义渲染器
    /// </summary>
    public class FlowContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public FlowContextMenuRenderer() : base(new FlowContextMenuColorTable())
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var rect = new Rectangle(Point.Empty, e.Item.Size);

            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(Color.FromArgb(40, theme.Primary)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
            else
            {
                using (var brush = new SolidBrush(theme.NodeBackground))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var y = e.Item.Height / 2;
            using (var pen = new Pen(theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
            }
        }
    }

    /// <summary>
    /// 上下文菜单颜色表
    /// </summary>
    public class FlowContextMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder
        {
            get
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                return theme.Border;
            }
        }

        public override Color MenuItemBorder
        {
            get
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                return Color.FromArgb(60, theme.Primary);
            }
        }

        public override Color ToolStripDropDownBackground
        {
            get
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                return theme.NodeBackground;
            }
        }

        public override Color ImageMarginGradientBegin
        {
            get
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                return theme.Background;
            }
        }

        public override Color ImageMarginGradientMiddle
        {
            get
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                return theme.Background;
            }
        }

        public override Color ImageMarginGradientEnd
        {
            get
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                return theme.Background;
            }
        }
    }

    #endregion
}
