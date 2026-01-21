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
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using FlowOperationType = WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums.FlowOperationType;

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
                
                var addNoteItem = new ToolStripMenuItem("添加备注");
                addNoteItem.Click += (s, e) => AddNote();
                _contextMenu.Items.Add(addNoteItem);
                return;
            }

            // 检查是否是备注节点
            var flowVersion = _stateStore.Flow?.FlowVersion;
            if (flowVersion?.Notes != null)
            {
                var note = flowVersion.Notes.FirstOrDefault(n => n.Id == _contextStepName);
                if (note != null)
                {
                    InitializeNoteContextMenu(note);
                    return;
                }
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
            if (string.IsNullOrEmpty(_contextStepName)) return;
            var trigger = _stateStore.Flow?.FlowVersion?.Trigger;
            if (trigger == null) return;

            var step = FlowStructureUtil.GetStep(_contextStepName, trigger);
            if (!(step is FlowAction)) return;

            // 打开动作选择对话框
            using (var dialog = new Widgets.NodeSelectorDialog())
            {
                FlowNodeType? selectedNodeType = null;
                dialog.NodeSelected += (s, nodeType) => { selectedNodeType = nodeType; };
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && selectedNodeType.HasValue)
                {
                    // 创建新动作（根据节点类型）
                    FlowAction newAction = null;
                    switch (selectedNodeType.Value)
                    {
                        case FlowNodeType.Loop:
                            newAction = new LoopOnItemsAction();
                            break;
                        case FlowNodeType.Decision:
                            newAction = new RouterAction();
                            break;
                        case FlowNodeType.Process:
                        default:
                            // 默认创建代码动作
                            newAction = new CodeAction();
                            break;
                    }

                    if (newAction != null)
                    {
                        // 生成新名称
                        newAction.Name = FlowStructureUtil.FindUnusedName(trigger);
                        
                        // 获取原动作的属性
                        var oldAction = (FlowAction)step;
                        newAction.DisplayName = oldAction.DisplayName;
                        newAction.NextAction = oldAction.NextAction;
                        
                        // 先删除旧动作
                        var deleteOperation = new FlowOperationRequest
                        {
                            Type = FlowOperationType.DELETE_ACTION,
                            Request = new DeleteActionRequest { StepName = _contextStepName }
                        };
                        _stateStore.ApplyOperation(deleteOperation);

                        // 找到原动作的父步骤
                        var parentStepName = FindParentStepName(oldAction, trigger);
                        var location = GetCurrentLocation(oldAction, trigger);
                        int? branchIndex = null;
                        
                        if (location == StepLocationRelativeToParent.INSIDE_BRANCH)
                        {
                            branchIndex = GetBranchIndex(oldAction, trigger);
                        }

                        // 添加新动作到相同位置
                        var addOperation = new FlowOperationRequest
                        {
                            Type = FlowOperationType.ADD_ACTION,
                            Request = new AddActionRequest
                            {
                                Action = newAction,
                                ParentStepName = parentStepName,
                                StepLocationRelativeToParent = location,
                                BranchIndex = branchIndex
                            }
                        };
                        _stateStore.ApplyOperation(addOperation);
                    }
                }
            }
        }

        private string FindParentStepName(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                FlowAction nextAction = null;
                if (step is FlowAction parentAction)
                {
                    nextAction = parentAction.NextAction;
                }
                else if (step is FlowTrigger trigger)
                {
                    nextAction = trigger.NextAction;
                }

                if (nextAction != null && nextAction.Name == action.Name)
                {
                    return step.Name;
                }

                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null && loop.FirstLoopAction.Name == action.Name)
                {
                    return step.Name;
                }

                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null && router.Children[i].Name == action.Name)
                        {
                            return step.Name;
                        }
                    }
                }
            }

            return null;
        }

        private StepLocationRelativeToParent GetCurrentLocation(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null)
                {
                    var childSteps = FlowStructureUtil.GetAllChildSteps(loop);
                    if (childSteps.Any(s => s.Name == action.Name))
                    {
                        return StepLocationRelativeToParent.INSIDE_LOOP;
                    }
                }

                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null)
                        {
                            var childSteps = FlowStructureUtil.GetAllChildSteps(router.Children[i]);
                            if (childSteps.Any(s => s.Name == action.Name))
                            {
                                return StepLocationRelativeToParent.INSIDE_BRANCH;
                            }
                        }
                    }
                }
            }

            return StepLocationRelativeToParent.AFTER;
        }

        private int? GetBranchIndex(FlowAction action, IStep root)
        {
            var allSteps = FlowStructureUtil.GetAllSteps(root);
            
            foreach (var step in allSteps)
            {
                if (step is RouterAction router)
                {
                    for (int i = 0; i < router.Children.Count; i++)
                    {
                        if (router.Children[i] != null && router.Children[i].Name == action.Name)
                        {
                            return i;
                        }
                    }
                }
            }

            return null;
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

        /// <summary>
        /// 初始化备注上下文菜单
        /// </summary>
        private void InitializeNoteContextMenu(Core.Models.Note note)
        {
            // 编辑备注
            var editItem = new ToolStripMenuItem("编辑");
            editItem.Click += (s, e) => EditNote(note.Id);
            _contextMenu.Items.Add(editItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 删除备注
            var deleteItem = new ToolStripMenuItem("删除");
            deleteItem.Click += (s, e) => DeleteNote(note.Id);
            _contextMenu.Items.Add(deleteItem);
        }

        /// <summary>
        /// 添加备注
        /// </summary>
        private void AddNote()
        {
            // 这个方法需要从外部传入位置，暂时使用默认位置
            // 实际使用时应该从鼠标位置获取
            var note = new Core.Models.Note
            {
                Id = Guid.NewGuid().ToString(),
                Content = "<br>",
                Position = new System.Drawing.PointF(100, 100),
                Size = new System.Drawing.SizeF(200, 150),
                Color = Models.NoteColorVariant.Blue
            };

            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.ADD_NOTE,
                Request = new Core.Operations.AddNoteRequest
                {
                    Id = note.Id,
                    Content = note.Content,
                    Position = note.Position,
                    Size = note.Size,
                    Color = note.Color
                }
            };

            _stateStore.ApplyOperation(operation);
        }

        /// <summary>
        /// 编辑备注
        /// </summary>
        private void EditNote(string noteId)
        {
            var flowVersion = _stateStore.Flow?.FlowVersion;
            if (flowVersion?.Notes == null) return;

            var note = flowVersion.Notes.FirstOrDefault(n => n.Id == noteId);
            if (note == null) return;

            // 打开编辑对话框
            using (var form = new Form
            {
                Text = "编辑备注",
                Size = new System.Drawing.Size(400, 300),
                StartPosition = FormStartPosition.CenterParent
            })
            {
                var textBox = new TextBox
                {
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    Text = note.Content ?? "",
                    Font = new Font("Microsoft YaHei UI", 10),
                    ScrollBars = ScrollBars.Vertical
                };

                var okButton = new Button
                {
                    Text = "确定",
                    Dock = DockStyle.Bottom,
                    Height = 35,
                    DialogResult = DialogResult.OK
                };

                form.Controls.Add(textBox);
                form.Controls.Add(okButton);
                form.AcceptButton = okButton;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    var operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.UPDATE_NOTE,
                        Request = new Core.Operations.UpdateNoteRequest
                        {
                            Id = note.Id,
                            Content = textBox.Text,
                            Position = note.Position,
                            Size = note.Size,
                            Color = note.Color
                        }
                    };

                    _stateStore.ApplyOperation(operation);
                }
            }
        }

        /// <summary>
        /// 删除备注
        /// </summary>
        private void DeleteNote(string noteId)
        {
            var operation = new FlowOperationRequest
            {
                Type = FlowOperationType.DELETE_NOTE,
                Request = new Core.Operations.DeleteNoteRequest
                {
                    Id = noteId
                }
            };

            _stateStore.ApplyOperation(operation);
        }
    }
}
