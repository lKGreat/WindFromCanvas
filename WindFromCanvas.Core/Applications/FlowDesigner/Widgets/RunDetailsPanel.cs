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
    /// 运行详情面板（匹配 Activepieces RunDetails）
    /// </summary>
    public class RunDetailsPanel : Panel
    {
        private BuilderStateStore _stateStore;
        private FlowRun _currentRun;
        private TreeView _stepsTree;
        private TextBox _inputOutputView;

        public RunDetailsPanel(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(10);

            // 分割面板
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200,
                Parent = this
            };

            // 左侧：步骤树
            var lblSteps = new Label
            {
                Text = "步骤执行顺序:",
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(5, 5),
                Size = new Size(200, 20),
                Parent = splitContainer.Panel1
            };

            _stepsTree = new TreeView
            {
                Location = new Point(5, 30),
                Size = new Size(splitContainer.Panel1.Width - 10, splitContainer.Panel1.Height - 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Parent = splitContainer.Panel1
            };
            _stepsTree.AfterSelect += StepsTree_AfterSelect;

            // 右侧：输入输出视图
            var lblInputOutput = new Label
            {
                Text = "输入/输出:",
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(5, 5),
                Size = new Size(200, 20),
                Parent = splitContainer.Panel2
            };

            _inputOutputView = new TextBox
            {
                Location = new Point(5, 30),
                Size = new Size(splitContainer.Panel2.Width - 10, splitContainer.Panel2.Height - 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                Parent = splitContainer.Panel2
            };
        }

        /// <summary>
        /// 显示运行详情
        /// </summary>
        public void ShowRunDetails(FlowRun run)
        {
            _currentRun = run;
            LoadStepsTree();
        }

        /// <summary>
        /// 加载步骤树
        /// </summary>
        private void LoadStepsTree()
        {
            _stepsTree.Nodes.Clear();

            if (_currentRun == null || _stateStore?.Flow?.FlowVersion == null) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;
            var allSteps = FlowStructureUtil.GetAllSteps(trigger);

            foreach (var step in allSteps)
            {
                var stepNode = new TreeNode(step.DisplayName ?? step.Name)
                {
                    Tag = step.Name
                };

                // 检查是否有输出
                if (_currentRun.StepOutputs.ContainsKey(step.Name))
                {
                    var output = _currentRun.StepOutputs[step.Name];
                    stepNode.Text += $" [{(output.Success ? "✓" : "✗")}]";
                    stepNode.ForeColor = output.Success ? Color.Green : Color.Red;
                }

                _stepsTree.Nodes.Add(stepNode);
            }
        }

        /// <summary>
        /// 步骤树选择变化
        /// </summary>
        private void StepsTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null || _currentRun == null) return;

            var stepName = e.Node.Tag.ToString();
            if (_currentRun.StepOutputs.TryGetValue(stepName, out var output))
            {
                DisplayStepInputOutput(output);
            }
            else
            {
                _inputOutputView.Text = "该步骤尚未执行或没有输出数据";
            }
        }

        /// <summary>
        /// 显示步骤输入输出
        /// </summary>
        private void DisplayStepInputOutput(StepOutput output)
        {
            var text = $"步骤: {output.StepName}\n";
            text += $"状态: {(output.Success ? "成功" : "失败")}\n";
            text += $"持续时间: {output.Duration.TotalMilliseconds}ms\n\n";

            text += "输入:\n";
            text += FormatObject(output.Input);
            text += "\n\n";

            text += "输出:\n";
            if (output.Success)
            {
                text += FormatObject(output.Output);
            }
            else
            {
                text += $"错误: {output.ErrorMessage}";
            }

            _inputOutputView.Text = text;
        }

        /// <summary>
        /// 格式化对象为字符串
        /// </summary>
        private string FormatObject(object obj)
        {
            if (obj == null)
            {
                return "null";
            }

            try
            {
                // 简单的 JSON 格式化（实际应用中应使用 JSON 序列化器）
                if (obj is Dictionary<string, object> dict)
                {
                    var lines = new List<string> { "{" };
                    foreach (var kvp in dict)
                    {
                        lines.Add($"  \"{kvp.Key}\": {FormatObject(kvp.Value)},");
                    }
                    if (lines.Count > 1)
                    {
                        lines[lines.Count - 1] = lines[lines.Count - 1].TrimEnd(',');
                    }
                    lines.Add("}");
                    return string.Join("\n", lines);
                }
                else if (obj is string str)
                {
                    return $"\"{str}\"";
                }
                else
                {
                    return obj.ToString();
                }
            }
            catch
            {
                return obj.ToString();
            }
        }
    }
}
