using System;
using System.Collections.Generic;
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
    /// 快捷键管理器（匹配 Activepieces shortcuts.ts）
    /// </summary>
    public class ShortcutManager
    {
        private readonly BuilderStateStore _stateStore;
        private readonly Dictionary<Keys, Action> _shortcuts;
        private readonly ClipboardManager _clipboardManager;

        public ShortcutManager(BuilderStateStore stateStore)
        {
            _stateStore = stateStore;
            _shortcuts = new Dictionary<Keys, Action>();
            _clipboardManager = new ClipboardManager();
            InitializeShortcuts();
        }

        private void InitializeShortcuts()
        {
            // Ctrl+C - 复制
            RegisterShortcut(Keys.Control | Keys.C, () =>
            {
                CopySelectedActions();
            });

            // Ctrl+V - 粘贴
            RegisterShortcut(Keys.Control | Keys.V, () =>
            {
                PasteActions();
            });

            // Shift+Delete 或 Delete - 删除
            RegisterShortcut(Keys.Shift | Keys.Delete, () =>
            {
                DeleteSelectedActions();
            });
            RegisterShortcut(Keys.Delete, () =>
            {
                DeleteSelectedActions();
            });

            // Ctrl+E - 跳过/取消跳过
            RegisterShortcut(Keys.Control | Keys.E, () =>
            {
                ToggleSkipSelectedActions();
            });

            // Ctrl+M - 小地图
            RegisterShortcut(Keys.Control | Keys.M, () =>
            {
                _stateStore.Canvas.ShowMinimap = !_stateStore.Canvas.ShowMinimap;
            });

            // 3.5.5 Ctrl+A - 全选
            RegisterShortcut(Keys.Control | Keys.A, () =>
            {
                SelectAllActions();
            });

            // 3.5.6 方向键 - 微调位置
            RegisterShortcut(Keys.Left, () => MoveSelectedNodes(-1, 0));
            RegisterShortcut(Keys.Right, () => MoveSelectedNodes(1, 0));
            RegisterShortcut(Keys.Up, () => MoveSelectedNodes(0, -1));
            RegisterShortcut(Keys.Down, () => MoveSelectedNodes(0, 1));

            // Shift+方向键 - 快速移动（10像素）
            RegisterShortcut(Keys.Shift | Keys.Left, () => MoveSelectedNodes(-10, 0));
            RegisterShortcut(Keys.Shift | Keys.Right, () => MoveSelectedNodes(10, 0));
            RegisterShortcut(Keys.Shift | Keys.Up, () => MoveSelectedNodes(0, -10));
            RegisterShortcut(Keys.Shift | Keys.Down, () => MoveSelectedNodes(0, 10));

            // Escape - 退出拖拽或清除选择
            RegisterShortcut(Keys.Escape, () =>
            {
                if (_stateStore.Drag.IsDragging)
                {
                    _stateStore.EndDrag();
                }
                else if (_stateStore.Selection.SelectedNodeIds.Count > 0)
                {
                    _stateStore.Selection.ClearSelection();
                }
            });
        }

        /// <summary>
        /// 注册快捷键
        /// </summary>
        public void RegisterShortcut(Keys keys, Action action)
        {
            _shortcuts[keys] = action;
        }

        /// <summary>
        /// 处理按键
        /// </summary>
        public bool HandleKeyPress(Keys keys)
        {
            if (_shortcuts.TryGetValue(keys, out var action))
            {
                action();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 复制选中的动作
        /// </summary>
        private void CopySelectedActions()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;
            if (_stateStore.Selection?.SelectedNodeIds == null || _stateStore.Selection.SelectedNodeIds.Count == 0) return;

            var actions = new List<FlowAction>();
            var trigger = _stateStore.Flow.FlowVersion.Trigger;

            foreach (var nodeId in _stateStore.Selection.SelectedNodeIds)
            {
                var step = FlowStructureUtil.GetStep(nodeId, trigger);
                if (step is FlowAction action)
                {
                    actions.Add(action);
                }
            }

            if (actions.Count > 0)
            {
                _clipboardManager.CopyActions(actions);
            }
        }

        /// <summary>
        /// 粘贴动作
        /// </summary>
        private void PasteActions()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;

            var actions = _clipboardManager.GetActionsFromClipboard();
            if (actions.Count == 0) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;
            
            // 找到最后一个选中的节点，在其后粘贴
            string parentStepName = null;
            if (_stateStore.Selection?.SelectedNodeIds != null && _stateStore.Selection.SelectedNodeIds.Count > 0)
            {
                parentStepName = _stateStore.Selection.SelectedNodeIds.Last();
            }
            else
            {
                // 如果没有选中节点，找到最后一个动作
                var allSteps = FlowStructureUtil.GetAllSteps(trigger);
                var lastAction = allSteps.OfType<FlowAction>().LastOrDefault();
                if (lastAction != null)
                {
                    parentStepName = lastAction.Name;
                }
            }

            if (string.IsNullOrEmpty(parentStepName))
            {
                // 如果没有找到父步骤，添加到触发器之后
                if (trigger.NextAction == null)
                {
                    parentStepName = trigger.Name;
                }
                else
                {
                    parentStepName = trigger.NextAction.Name;
                }
            }

            // 粘贴第一个动作
            if (actions.Count > 0)
            {
                var firstAction = actions[0];
                // 生成新名称
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

                // 粘贴后续动作（链式连接）
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

        /// <summary>
        /// 删除选中的动作
        /// </summary>
        private void DeleteSelectedActions()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;
            if (_stateStore.Selection?.SelectedNodeIds == null || _stateStore.Selection.SelectedNodeIds.Count == 0) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;

            // 从后往前删除，避免索引问题
            var nodeIds = _stateStore.Selection.SelectedNodeIds.ToList();
            nodeIds.Reverse();

            foreach (var nodeId in nodeIds)
            {
                var step = FlowStructureUtil.GetStep(nodeId, trigger);
                if (step is FlowAction) // 不能删除触发器
                {
                    var operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.DELETE_ACTION,
                        Request = new DeleteActionRequest { StepName = nodeId }
                    };
                    _stateStore.ApplyOperation(operation);
                }
            }

            // 清除选择
            _stateStore.Selection.ClearSelection();
        }

        /// <summary>
        /// 切换选中动作的跳过状态
        /// </summary>
        private void ToggleSkipSelectedActions()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;
            if (_stateStore.Selection?.SelectedNodeIds == null || _stateStore.Selection.SelectedNodeIds.Count == 0) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;

            foreach (var nodeId in _stateStore.Selection.SelectedNodeIds)
            {
                var step = FlowStructureUtil.GetStep(nodeId, trigger);
                if (step is FlowAction action)
                {
                    action.Skip = !action.Skip;
                    
                    var operation = new FlowOperationRequest
                    {
                        Type = FlowOperationType.UPDATE_ACTION,
                        Request = new UpdateActionRequest
                        {
                            StepName = nodeId,
                            UpdatedAction = action
                        }
                    };
                    _stateStore.ApplyOperation(operation);
                }
            }
        }

        /// <summary>
        /// 3.5.5 全选所有动作
        /// </summary>
        private void SelectAllActions()
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;

            var trigger = _stateStore.Flow.FlowVersion.Trigger;
            var allSteps = FlowStructureUtil.GetAllSteps(trigger);
            
            // 选择所有动作（不包括触发器）
            var actionIds = allSteps
                .Where(s => s is FlowAction)
                .Select(s => s.Name)
                .ToArray();

            _stateStore.SetSelectedNodes(actionIds);
        }

        /// <summary>
        /// 3.5.6 移动选中的节点（方向键微调）
        /// </summary>
        private void MoveSelectedNodes(float deltaX, float deltaY)
        {
            if (_stateStore?.Flow?.FlowVersion == null) return;
            if (_stateStore.Selection?.SelectedNodeIds == null || _stateStore.Selection.SelectedNodeIds.Count == 0) return;

            // 通过GraphModel移动节点
            if (_stateStore.Graph != null)
            {
                foreach (var nodeId in _stateStore.Selection.SelectedNodeIds)
                {
                    var node = _stateStore.Graph.GetNode(nodeId);
                    if (node != null)
                    {
                        _stateStore.Graph.UpdateNodePosition(
                            nodeId,
                            node.PositionX + deltaX,
                            node.PositionY + deltaY
                        );
                    }
                }
            }
        }
    }
}
