using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 步骤设置面板（匹配 Activepieces StepSettingsContainer）
    /// </summary>
    public class StepSettingsPanel : Panel
    {
        private BuilderStateStore _stateStore;
        private TabControl _settingsTabs;
        private Panel _currentSettingsPanel;
        private IStep _currentStep;

        public StepSettingsPanel(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            InitializeComponent();
            
            // 订阅状态变化
            _stateStore.PropertyChanged += StateStore_PropertyChanged;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.AutoScroll = true;
            this.Padding = new Padding(10);

            _settingsTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9)
            };

            this.Controls.Add(_settingsTabs);
        }

        private void StateStore_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BuilderStateStore.Canvas) || 
                e.PropertyName == "Canvas.SelectedStep")
            {
                UpdateSettingsPanel();
            }
        }

        /// <summary>
        /// 更新设置面板
        /// </summary>
        private void UpdateSettingsPanel()
        {
            var selectedStepName = _stateStore.Canvas?.SelectedStep;
            if (string.IsNullOrEmpty(selectedStepName))
            {
                ClearSettings();
                return;
            }

            var flowVersion = _stateStore.Flow?.FlowVersion;
            if (flowVersion == null) return;

            var step = FlowStructureUtil.GetStep(selectedStepName, flowVersion.Trigger);
            if (step == null) return;

            _currentStep = step;
            LoadSettingsForStep(step);
        }

        /// <summary>
        /// 为步骤加载设置
        /// </summary>
        private void LoadSettingsForStep(IStep step)
        {
            _settingsTabs.TabPages.Clear();

            if (step is FlowTrigger trigger)
            {
                LoadTriggerSettings(trigger);
            }
            else if (step is FlowAction action)
            {
                switch (action.Type)
                {
                    case FlowActionType.CODE:
                        LoadCodeSettings((CodeAction)action);
                        break;
                    case FlowActionType.PIECE:
                        LoadPieceSettings((PieceAction)action);
                        break;
                    case FlowActionType.LOOP_ON_ITEMS:
                        LoadLoopSettings((LoopOnItemsAction)action);
                        break;
                    case FlowActionType.ROUTER:
                        LoadRouterSettings((RouterAction)action);
                        break;
                }
            }
        }

        /// <summary>
        /// 加载触发器设置
        /// </summary>
        private void LoadTriggerSettings(FlowTrigger trigger)
        {
            var tabPage = new TabPage("触发器设置");
            var panel = CreateBasicSettingsPanel(trigger);
            tabPage.Controls.Add(panel);
            _settingsTabs.TabPages.Add(tabPage);
        }

        /// <summary>
        /// 加载代码动作设置
        /// </summary>
        private void LoadCodeSettings(CodeAction action)
        {
            // 基本信息标签页
            var basicTab = new TabPage("基本信息");
            var basicPanel = CreateBasicSettingsPanel(action);
            basicTab.Controls.Add(basicPanel);
            _settingsTabs.TabPages.Add(basicTab);

            // 代码设置标签页
            var codeTab = new TabPage("代码");
            var codePanel = CreateCodeSettingsPanel(action);
            codeTab.Controls.Add(codePanel);
            _settingsTabs.TabPages.Add(codeTab);
        }

        /// <summary>
        /// 加载组件动作设置
        /// </summary>
        private void LoadPieceSettings(PieceAction action)
        {
            // 基本信息标签页
            var basicTab = new TabPage("基本信息");
            var basicPanel = CreateBasicSettingsPanel(action);
            basicTab.Controls.Add(basicPanel);
            _settingsTabs.TabPages.Add(basicTab);

            // 组件设置标签页
            var pieceTab = new TabPage("组件设置");
            var piecePanel = CreatePieceSettingsPanel(action);
            pieceTab.Controls.Add(piecePanel);
            _settingsTabs.TabPages.Add(pieceTab);
        }

        /// <summary>
        /// 加载循环设置
        /// </summary>
        private void LoadLoopSettings(LoopOnItemsAction action)
        {
            // 基本信息标签页
            var basicTab = new TabPage("基本信息");
            var basicPanel = CreateBasicSettingsPanel(action);
            basicTab.Controls.Add(basicPanel);
            _settingsTabs.TabPages.Add(basicTab);

            // 循环设置标签页
            var loopTab = new TabPage("循环设置");
            var loopPanel = CreateLoopSettingsPanel(action);
            loopTab.Controls.Add(loopPanel);
            _settingsTabs.TabPages.Add(loopTab);
        }

        /// <summary>
        /// 加载路由设置
        /// </summary>
        private void LoadRouterSettings(RouterAction action)
        {
            // 基本信息标签页
            var basicTab = new TabPage("基本信息");
            var basicPanel = CreateBasicSettingsPanel(action);
            basicTab.Controls.Add(basicPanel);
            _settingsTabs.TabPages.Add(basicTab);

            // 路由设置标签页
            var routerTab = new TabPage("路由设置");
            var routerPanel = CreateRouterSettingsPanel(action);
            routerTab.Controls.Add(routerPanel);
            _settingsTabs.TabPages.Add(routerTab);
        }

        /// <summary>
        /// 创建基本设置面板
        /// </summary>
        private Panel CreateBasicSettingsPanel(IStep step)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            int y = 10;

            // 显示名称
            var lblDisplayName = new Label
            {
                Text = "显示名称:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            var txtDisplayName = new TextBox
            {
                Location = new Point(120, y),
                Size = new Size(panel.Width - 130, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = step.DisplayName ?? "",
                Parent = panel
            };
            txtDisplayName.TextChanged += (s, e) =>
            {
                step.DisplayName = txtDisplayName.Text;
                UpdateStep();
            };
            y += 30;

            // 跳过选项
            if (step is FlowAction)
            {
                var chkSkip = new CheckBox
                {
                    Text = "跳过此步骤",
                    Location = new Point(10, y),
                    Size = new Size(200, 20),
                    Checked = step.Skip,
                    Parent = panel
                };
                chkSkip.CheckedChanged += (s, e) =>
                {
                    step.Skip = chkSkip.Checked;
                    UpdateStep();
                };
                y += 30;
            }

            // 有效性状态
            var lblValid = new Label
            {
                Text = step.Valid ? "状态: 有效" : "状态: 无效",
                Location = new Point(10, y),
                Size = new Size(panel.Width - 20, 20),
                ForeColor = step.Valid ? Color.Green : Color.Red,
                Parent = panel
            };
            y += 30;

            return panel;
        }

        /// <summary>
        /// 创建代码设置面板
        /// </summary>
        private Panel CreateCodeSettingsPanel(CodeAction action)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            int y = 10;

            // 代码编辑器
            var lblCode = new Label
            {
                Text = "代码:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            y += 25;

            // 确保 SourceCode 已初始化
            if (action.Settings == null)
            {
                action.Settings = new Core.Models.CodeActionSettings();
            }
            if (action.Settings.SourceCode == null)
            {
                action.Settings.SourceCode = new Core.Models.SourceCode
                {
                    Code = "",
                    PackageJson = "{}"
                };
            }

            var txtCode = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(panel.Width - 20, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = action.Settings.SourceCode.Code ?? "",
                Parent = panel
            };
            txtCode.TextChanged += (s, e) =>
            {
                if (action.Settings?.SourceCode != null)
                {
                    action.Settings.SourceCode.Code = txtCode.Text;
                    UpdateStep();
                }
            };
            y += 210;

            // NPM 依赖
            var lblPackageJson = new Label
            {
                Text = "NPM 依赖 (package.json):",
                Location = new Point(10, y),
                Size = new Size(200, 20),
                Parent = panel
            };
            y += 25;

            var txtPackageJson = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(panel.Width - 20, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = action.Settings.SourceCode.PackageJson ?? "{}",
                Parent = panel
            };
            txtPackageJson.TextChanged += (s, e) =>
            {
                if (action.Settings?.SourceCode != null)
                {
                    action.Settings.SourceCode.PackageJson = txtPackageJson.Text;
                    UpdateStep();
                }
            };

            return panel;
        }

        /// <summary>
        /// 创建组件设置面板
        /// </summary>
        private Panel CreatePieceSettingsPanel(PieceAction action)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            int y = 10;

            // 组件名称
            var lblPieceName = new Label
            {
                Text = "组件名称:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            var lblPieceNameValue = new Label
            {
                Location = new Point(120, y),
                Size = new Size(panel.Width - 130, 20),
                Text = action.Settings?.PieceName ?? "",
                Parent = panel
            };
            y += 30;

            // 动作名称
            var lblActionName = new Label
            {
                Text = "动作名称:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            var lblActionNameValue = new Label
            {
                Location = new Point(120, y),
                Size = new Size(panel.Width - 130, 20),
                Text = action.Settings?.ActionName ?? "",
                Parent = panel
            };
            y += 30;

            // 属性设置（简化实现）
            var lblProperties = new Label
            {
                Text = "属性设置:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            y += 25;

            var txtProperties = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(panel.Width - 20, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "属性配置（JSON格式）",
                ReadOnly = true,
                Parent = panel
            };

            return panel;
        }

        /// <summary>
        /// 创建循环设置面板
        /// </summary>
        private Panel CreateLoopSettingsPanel(LoopOnItemsAction action)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            int y = 10;

            // 确保 Settings 已初始化
            if (action.Settings == null)
            {
                action.Settings = new Core.Models.LoopOnItemsActionSettings();
            }

            // 循环项表达式
            var lblItems = new Label
            {
                Text = "循环项:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            var txtItems = new TextBox
            {
                Location = new Point(120, y),
                Size = new Size(panel.Width - 130, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = action.Settings.Items ?? "",
                Parent = panel
            };
            txtItems.TextChanged += (s, e) =>
            {
                if (action.Settings != null)
                {
                    action.Settings.Items = txtItems.Text;
                    UpdateStep();
                }
            };
            y += 30;

            // 说明
            var lblDescription = new Label
            {
                Text = "输入要循环的数据表达式，例如：{{trigger.body.items}}",
                Location = new Point(10, y),
                Size = new Size(panel.Width - 20, 40),
                ForeColor = Color.Gray,
                Parent = panel
            };

            return panel;
        }

        /// <summary>
        /// 创建路由设置面板
        /// </summary>
        private Panel CreateRouterSettingsPanel(RouterAction action)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            int y = 10;

            // 确保 Settings 已初始化
            if (action.Settings == null)
            {
                action.Settings = new Core.Models.RouterActionSettings();
            }

            // 执行类型
            var lblExecutionType = new Label
            {
                Text = "执行类型:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            var cmbExecutionType = new ComboBox
            {
                Location = new Point(120, y),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Parent = panel
            };
            cmbExecutionType.Items.Add("执行所有匹配的分支");
            cmbExecutionType.Items.Add("执行第一个匹配的分支");
            cmbExecutionType.SelectedIndex = action.Settings.ExecutionType == RouterExecutionType.EXECUTE_ALL_MATCH ? 0 : 1;
            cmbExecutionType.SelectedIndexChanged += (s, e) =>
            {
                if (action.Settings != null)
                {
                    action.Settings.ExecutionType = cmbExecutionType.SelectedIndex == 0 
                        ? RouterExecutionType.EXECUTE_ALL_MATCH 
                        : RouterExecutionType.EXECUTE_FIRST_MATCH;
                    UpdateStep();
                }
            };
            y += 30;

            // 分支列表
            var lblBranches = new Label
            {
                Text = "分支:",
                Location = new Point(10, y),
                Size = new Size(100, 20),
                Parent = panel
            };
            y += 25;

            var branchesPanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(panel.Width - 20, 300),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Parent = panel
            };

            LoadBranchesList(branchesPanel, action);

            // 添加分支按钮
            y += 310;
            var btnAddBranch = new Button
            {
                Text = "添加分支",
                Location = new Point(10, y),
                Size = new Size(100, 30),
                Parent = panel
            };
            btnAddBranch.Click += (s, e) =>
            {
                AddBranch(action);
                LoadBranchesList(branchesPanel, action);
            };

            return panel;
        }

        /// <summary>
        /// 加载分支列表
        /// </summary>
        private void LoadBranchesList(Panel container, RouterAction action)
        {
            container.Controls.Clear();

            if (action.Settings?.Branches == null) return;

            int y = 5;
            for (int i = 0; i < action.Settings.Branches.Count; i++)
            {
                var branch = action.Settings.Branches[i];
                var branchPanel = CreateBranchItemPanel(branch, i, action);
                branchPanel.Location = new Point(5, y);
                branchPanel.Width = container.Width - 10;
                branchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                container.Controls.Add(branchPanel);
                y += branchPanel.Height + 5;
            }
        }

        /// <summary>
        /// 创建分支项面板
        /// </summary>
        private Panel CreateBranchItemPanel(RouterBranch branch, int index, RouterAction routerAction)
        {
            var panel = new Panel
            {
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            int y = 5;

            // 分支名称
            var lblBranchName = new Label
            {
                Text = $"分支名称:",
                Location = new Point(5, y),
                Size = new Size(80, 20),
                Parent = panel
            };
            var txtBranchName = new TextBox
            {
                Location = new Point(90, y),
                Size = new Size(panel.Width - 150, 20),
                Text = branch.BranchName ?? "",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = panel
            };
            txtBranchName.TextChanged += (s, e) =>
            {
                branch.BranchName = txtBranchName.Text;
                UpdateStep();
            };
            y += 25;

            // 分支类型
            var lblBranchType = new Label
            {
                Text = $"类型: {(branch.BranchType == BranchExecutionType.CONDITION ? "条件" : "回退")}",
                Location = new Point(5, y),
                Size = new Size(200, 20),
                Parent = panel
            };
            y += 25;

            // 删除按钮
            var btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(panel.Width - 60, 5),
                Size = new Size(50, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Parent = panel
            };
            btnDelete.Click += (s, e) =>
            {
                var operation = new FlowOperationRequest
                {
                    Type = FlowOperationType.DELETE_BRANCH,
                    Request = new DeleteBranchRequest
                    {
                        StepName = routerAction.Name,
                        BranchIndex = index
                    }
                };
                _stateStore.ApplyOperation(operation);
                LoadBranchesList(panel.Parent as Panel, routerAction);
            };

            return panel;
        }

        /// <summary>
        /// 添加分支
        /// </summary>
        private void AddBranch(RouterAction action)
        {
            var branchIndex = action.Settings?.Branches?.Count ?? 0;
            var branchName = $"Branch {branchIndex + 1}";

            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_BRANCH,
                Request = new AddBranchRequest
                {
                    StepName = action.Name,
                    BranchIndex = branchIndex,
                    BranchName = branchName
                }
            };

            _stateStore.ApplyOperation(operation);
        }

        /// <summary>
        /// 更新步骤
        /// </summary>
        private void UpdateStep()
        {
            if (_currentStep == null) return;

            var operation = new FlowOperationRequest
            {
                Type = _currentStep is FlowAction ? FlowOperationType.UPDATE_ACTION : FlowOperationType.UPDATE_TRIGGER,
                Request = CreateUpdateRequest(_currentStep)
            };

            _stateStore.ApplyOperation(operation);
        }

        /// <summary>
        /// 创建更新请求
        /// </summary>
        private object CreateUpdateRequest(IStep step)
        {
            if (step is FlowAction action)
            {
                return new UpdateActionRequest
                {
                    StepName = action.Name,
                    UpdatedAction = action
                };
            }
            else if (step is FlowTrigger trigger)
            {
                return new UpdateTriggerRequest
                {
                    Trigger = trigger
                };
            }

            return null;
        }

        /// <summary>
        /// 清除设置
        /// </summary>
        private void ClearSettings()
        {
            _settingsTabs.TabPages.Clear();
            _currentStep = null;
        }
    }
}
