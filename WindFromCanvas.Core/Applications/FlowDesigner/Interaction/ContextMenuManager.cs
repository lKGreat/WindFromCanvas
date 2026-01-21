using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.State;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Operations;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Interaction
{
    /// <summary>
    /// 上下文菜单管理器（匹配 Activepieces CanvasContextMenu）
    /// </summary>
    public class ContextMenuManager
    {
        private readonly BuilderStateStore _stateStore;
        private readonly ClipboardManager _clipboardManager;
        private ContextMenuStrip _contextMenu;
        private string _contextStepName;

        public ContextMenuManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _clipboardManager = new ClipboardManager();
            _contextMenu = new ContextMenuStrip();
        }

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        public void ShowContextMenu(Control control, Point location, string stepName = null)
        {
            _contextStepName = stepName ?? _stateStore.Selection?.SelectedNodeIds?.FirstOrDefault();
            InitializeContextMenu();
            _contextMenu.Show(control, location);
        }

        private void InitializeContextMenu()
        {
            _contextMenu.Items.Clear();

            if (string.IsNullOrEmpty(_contextStepName))
            {
                // 空白区域菜单
                var pasteItem = new ToolStripMenuItem("粘贴");
                pasteItem.Click += (s, e) => PasteAfter(null);
                _contextMenu.Items.Add(pasteItem);
                return;
            }

            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            var step = FlowStructureUtil.GetStep(_contextStepName, trigger);
            if (step == null) return;

            var isAction = step is FlowAction;
            var isTrigger = step is FlowTrigger;
            var isSkipped = step.Skip;

            // 替换
            if (isAction)
            {
                var replaceItem = new ToolStripMenuItem("替换");
                replaceItem.Click += (s, e) => ReplaceAction();
                _contextMenu.Items.Add(replaceItem);
            }

            // 复制
            var copyItem = new ToolStripMenuItem("复制");
            copyItem.Click += (s, e) => CopyAction();
            _contextMenu.Items.Add(copyItem);

            // 跳过/取消跳过
            if (isAction)
            {
                var skipItem = new ToolStripMenuItem(isSkipped ? "取消跳过" : "跳过");
                skipItem.Click += (s, e) => ToggleSkip();
                _contextMenu.Items.Add(skipItem);
            }

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 粘贴在后面
            var pasteAfterItem = new ToolStripMenuItem("粘贴在后面");
            pasteAfterItem.Click += (s, e) => PasteAfter(_contextStepName);
            _contextMenu.Items.Add(pasteAfterItem);

            // 粘贴到循环内（如果是循环动作）
            if (step is LoopOnItemsAction)
            {
                var pasteInLoopItem = new ToolStripMenuItem("粘贴到循环内");
                pasteInLoopItem.Click += (s, e) => PasteInLoop(_contextStepName);
                _contextMenu.Items.Add(pasteInLoopItem);
            }

            // 粘贴到分支内（如果是路由动作）
            if (step is RouterAction router)
            {
                for (int i = 0; i < router.Children.Count; i++)
                {
                    var pasteInBranchItem = new ToolStripMenuItem($"粘贴到分支 {i + 1}");
                    var branchIndex = i; // 捕获变量
                    pasteInBranchItem.Click += (s, e) => PasteInBranch(_contextStepName, branchIndex);
                    _contextMenu.Items.Add(pasteInBranchItem);
                }
            }

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 删除
            if (isAction)
            {
                var deleteItem = new ToolStripMenuItem("删除");
                deleteItem.Click += (s, e) => DeleteAction();
                _contextMenu.Items.Add(deleteItem);
            }
        }

        private void ReplaceAction()
        {
            // TODO: 实现替换动作逻辑（需要打开动作选择器）
        }

        private void CopyAction()
        {
            if (string.IsNullOrEmpty(_contextStepName)) return;
            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            var step = FlowStructureUtil.GetStep(_contextStepName, trigger);
            if (step is FlowAction action)
            {
                _clipboardManager.CopyActions(new List<FlowAction> { action });
            }
        }

        private void ToggleSkip()
        {
            if (string.IsNullOrEmpty(_contextStepName)) return;
            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            var step = FlowStructureUtil.GetStep(_contextStepName, trigger);
            if (step is FlowAction action)
            {
                action.Skip = !action.Skip;
                var operation = new FlowOperationRequest
                {
                    Type = FlowOperationType.UPDATE_ACTION,
                    Request = new UpdateActionRequest
                    {
                        StepName = _contextStepName,
                        UpdatedAction = action
                    }
                };
                _stateStore.ApplyOperation(operation);
            }
        }

        private void PasteAfter(string parentStepName)
        {
            var actions = _clipboardManager.GetActionsFromClipboard();
            if (actions.Count == 0) return;

            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            if (string.IsNullOrEmpty(parentStepName))
            {
                // 如果没有指定父步骤，找到最后一个动作
                var allSteps = FlowStructureUtil.GetAllSteps(trigger);
                var lastAction = allSteps.OfType<FlowAction>().LastOrDefault();
                if (lastAction != null)
                {
                    parentStepName = lastAction.Name;
                }
                else if (trigger.NextAction != null)
                {
                    parentStepName = trigger.NextAction.Name;
                }
                else
                {
                    parentStepName = trigger.Name;
                }
            }

            // 粘贴第一个动作
            if (actions.Count > 0)
            {
                var firstAction = actions[0];
                firstAction.Name = FlowStructureUtil.FindUnusedName(trigger);

                var operation = new FlowOperationRequest
                {
                    Type = FlowOperationType.ADD_ACTION,
                    Request = new AddActionRequest
                    {
                        Action = firstAction,
                        ParentStepName = parentStepName,
                        StepLocationRelativeToParent = StepLocationRelativeToParent.AFTER
                    }
                };
                _stateStore.ApplyOperation(operation);

                // 链式粘贴后续动作
                var currentParent = firstAction.Name;
                for (int i = 1; i < actions.Count; i++)
                {
                    var action = actions[i];
                    action.Name = FlowStructureUtil.FindUnusedName(trigger);

                    operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.ADD_ACTION,
                        Request = new AddActionRequest
                        {
                            Action = action,
                            ParentStepName = currentParent,
                            StepLocationRelativeToParent = StepLocationRelativeToParent.AFTER
                        }
                    };
                    _stateStore.ApplyOperation(operation);
                    currentParent = action.Name;
                }
            }
        }

        private void PasteInLoop(string loopStepName)
        {
            var actions = _clipboardManager.GetActionsFromClipboard();
            if (actions.Count == 0) return;

            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            var loopStep = FlowStructureUtil.GetActionOrThrow(loopStepName, trigger);
            if (!(loopStep is LoopOnItemsAction loop))
            {
                return;
            }

            // 粘贴第一个动作
            if (actions.Count > 0)
            {
                var firstAction = actions[0];
                firstAction.Name = FlowStructureUtil.FindUnusedName(trigger);

                var operation = new FlowOperationRequest
                {
                    Type = FlowOperationType.ADD_ACTION,
                    Request = new AddActionRequest
                    {
                        Action = firstAction,
                        ParentStepName = loopStepName,
                        StepLocationRelativeToParent = StepLocationRelativeToParent.INSIDE_LOOP
                    }
                };
                _stateStore.ApplyOperation(operation);

                // 链式粘贴后续动作
                var currentParent = firstAction.Name;
                for (int i = 1; i < actions.Count; i++)
                {
                    var action = actions[i];
                    action.Name = FlowStructureUtil.FindUnusedName(trigger);

                    operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.ADD_ACTION,
                        Request = new AddActionRequest
                        {
                            Action = action,
                            ParentStepName = currentParent,
                            StepLocationRelativeToParent = StepLocationRelativeToParent.AFTER
                        }
                    };
                    _stateStore.ApplyOperation(operation);
                    currentParent = action.Name;
                }
            }
        }

        private void PasteInBranch(string routerStepName, int branchIndex)
        {
            var actions = _clipboardManager.GetActionsFromClipboard();
            if (actions.Count == 0) return;

            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            var routerStep = FlowStructureUtil.GetActionOrThrow(routerStepName, trigger);
            if (!(routerStep is RouterAction router))
            {
                return;
            }

            if (branchIndex < 0 || branchIndex >= router.Children.Count)
            {
                return;
            }

            // 粘贴第一个动作
            if (actions.Count > 0)
            {
                var firstAction = actions[0];
                firstAction.Name = FlowStructureUtil.FindUnusedName(trigger);

                var operation = new FlowOperationRequest
                {
                    Type = FlowOperationType.ADD_ACTION,
                    Request = new AddActionRequest
                    {
                        Action = firstAction,
                        ParentStepName = routerStepName,
                        StepLocationRelativeToParent = StepLocationRelativeToParent.INSIDE_BRANCH,
                        BranchIndex = branchIndex
                    }
                };
                _stateStore.ApplyOperation(operation);

                // 链式粘贴后续动作
                var currentParent = firstAction.Name;
                for (int i = 1; i < actions.Count; i++)
                {
                    var action = actions[i];
                    action.Name = FlowStructureUtil.FindUnusedName(trigger);

                    operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.ADD_ACTION,
                        Request = new AddActionRequest
                        {
                            Action = action,
                            ParentStepName = currentParent,
                            StepLocationRelativeToParent = StepLocationRelativeToParent.AFTER
                        }
                    };
                    _stateStore.ApplyOperation(operation);
                    currentParent = action.Name;
                }
            }
        }

        private void DeleteAction()
        {
            if (string.IsNullOrEmpty(_contextStepName)) return;

            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.DELETE_ACTION,
                Request = new DeleteActionRequest { StepName = _contextStepName }
            };
            _stateStore.ApplyOperation(operation);
        }
    }
}
