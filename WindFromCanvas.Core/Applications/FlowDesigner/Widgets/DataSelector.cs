using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;
using WindFromCanvas.Core.Applications.FlowDesigner.State;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 数据选择器（匹配 Activepieces DataSelector）
    /// 用于选择前序步骤的输出数据作为输入
    /// </summary>
    public class DataSelector : Form
    {
        private BuilderStateStore _stateStore;
        private ListBox _stepsList;
        private TreeView _dataTree;
        private Button _btnSelect;
        private Button _btnCancel;
        private IStep _currentStep;
        private string _selectedPath;

        public event EventHandler<string> DataSelected;

        public DataSelector(BuilderStateStore stateStore, IStep currentStep)
        {
            _stateStore = stateStore;
            _currentStep = currentStep;
            InitializeComponent();
            LoadAvailableSteps();
        }

        private void InitializeComponent()
        {
            this.Text = "选择数据";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 左侧：步骤列表
            var lblSteps = new Label
            {
                Text = "步骤:",
                Location = new Point(10, 10),
                Size = new Size(100, 20),
                Parent = this
            };

            _stepsList = new ListBox
            {
                Location = new Point(10, 35),
                Size = new Size(200, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                Parent = this
            };
            _stepsList.SelectedIndexChanged += StepsList_SelectedIndexChanged;

            // 右侧：数据树
            var lblData = new Label
            {
                Text = "输出数据:",
                Location = new Point(220, 10),
                Size = new Size(200, 20),
                Parent = this
            };

            _dataTree = new TreeView
            {
                Location = new Point(220, 35),
                Size = new Size(360, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Parent = this
            };
            _dataTree.NodeMouseDoubleClick += DataTree_NodeMouseDoubleClick;

            // 底部按钮
            _btnSelect = new Button
            {
                Text = "选择",
                Location = new Point(420, 445),
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                Enabled = false,
                Parent = this
            };
            _btnSelect.Click += BtnSelect_Click;

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(505, 445),
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                Parent = this
            };

            this.AcceptButton = _btnSelect;
            this.CancelButton = _btnCancel;
        }

        /// <summary>
        /// 加载可用步骤
        /// </summary>
        private void LoadAvailableSteps()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;
            var allSteps = FlowStructureUtil.GetAllSteps(trigger);

            // 只显示当前步骤之前的步骤
            var currentStepIndex = -1;
            if (_currentStep != null)
            {
                var stepsList = allSteps.ToList();
                currentStepIndex = stepsList.FindIndex(s => s.Name == _currentStep.Name);
            }

            _stepsList.Items.Clear();
            foreach (var step in allSteps)
            {
                if (currentStepIndex >= 0)
                {
                    var stepIndex = allSteps.ToList().FindIndex(s => s.Name == step.Name);
                    if (stepIndex >= currentStepIndex)
                    {
                        continue; // 跳过当前步骤及之后的步骤
                    }
                }

                var displayName = step.DisplayName ?? step.Name;
                _stepsList.Items.Add(new StepListItem { Step = step, DisplayText = displayName });
            }

            if (_stepsList.Items.Count > 0)
            {
                _stepsList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 步骤列表选择变化
        /// </summary>
        private void StepsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_stepsList.SelectedItem is StepListItem item)
            {
                LoadStepData(item.Step);
            }
        }

        /// <summary>
        /// 加载步骤数据
        /// </summary>
        private void LoadStepData(IStep step)
        {
            _dataTree.Nodes.Clear();
            _selectedPath = null;
            _btnSelect.Enabled = false;

            // 创建模拟的输出数据结构
            // 实际应用中应该从步骤的实际输出数据构建
            var rootNode = new TreeNode("输出数据")
            {
                Tag = "output"
            };

            // 添加常见的数据字段
            var commonFields = new[]
            {
                "body",
                "headers",
                "queryParams",
                "statusCode"
            };

            foreach (var field in commonFields)
            {
                var fieldNode = new TreeNode(field)
                {
                    Tag = $"output.{field}"
                };
                rootNode.Nodes.Add(fieldNode);

                // 添加子字段示例
                if (field == "body")
                {
                    var bodyFields = new[] { "id", "name", "email", "data" };
                    foreach (var bodyField in bodyFields)
                    {
                        var subNode = new TreeNode(bodyField)
                        {
                            Tag = $"output.body.{bodyField}"
                        };
                        fieldNode.Nodes.Add(subNode);
                    }
                }
            }

            _dataTree.Nodes.Add(rootNode);
            rootNode.Expand();

            // 选择变化事件
            _dataTree.AfterSelect += DataTree_AfterSelect;
        }

        /// <summary>
        /// 数据树选择变化
        /// </summary>
        private void DataTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag != null)
            {
                _selectedPath = e.Node.Tag.ToString();
                _btnSelect.Enabled = true;
            }
        }

        /// <summary>
        /// 数据树双击
        /// </summary>
        private void DataTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag != null)
            {
                _selectedPath = e.Node.Tag.ToString();
                SelectData();
            }
        }

        /// <summary>
        /// 选择按钮点击
        /// </summary>
        private void BtnSelect_Click(object sender, EventArgs e)
        {
            SelectData();
        }

        /// <summary>
        /// 选择数据
        /// </summary>
        private void SelectData()
        {
            if (!string.IsNullOrEmpty(_selectedPath))
            {
                // 转换为模板表达式格式 {{stepName.output.field}}
                var stepItem = _stepsList.SelectedItem as StepListItem;
                if (stepItem != null)
                {
                    var templateExpression = $"{{{{{stepItem.Step.Name}.{_selectedPath}}}}}";
                    DataSelected?.Invoke(this, templateExpression);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        /// <summary>
        /// 步骤列表项
        /// </summary>
        private class StepListItem
        {
            public IStep Step { get; set; }
            public string DisplayText { get; set; }

            public override string ToString()
            {
                return DisplayText;
            }
        }
    }
}
