using System;
using System.Collections.Generic;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Commands
{
    /// <summary>
    /// 命令管理器 - 管理命令栈，支持撤销/重做和命令合并
    /// </summary>
    public class CommandManager
    {
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();
        
        /// <summary>
        /// 4.1.6 历史栈容量限制（默认100）
        /// </summary>
        private int _maxStackSize = 100;

        /// <summary>
        /// 获取或设置历史栈最大容量
        /// </summary>
        public int MaxStackSize
        {
            get => _maxStackSize;
            set
            {
                if (value > 0)
                {
                    _maxStackSize = value;
                    TrimStack();
                }
            }
        }

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// 执行命令（支持命令合并）
        /// </summary>
        public void Execute(ICommand command)
        {
            if (command == null) return;

            // 4.1.5 尝试命令合并（防抖优化）
            if (TryMergeCommand(command))
            {
                return; // 已合并，不需要新增命令
            }

            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // 执行新命令后清空重做栈

            // 4.1.6 限制栈大小
            TrimStack();
        }

        /// <summary>
        /// 4.1.5 尝试合并命令（防抖）
        /// </summary>
        private bool TryMergeCommand(ICommand newCommand)
        {
            if (_undoStack.Count == 0)
                return false;

            var lastCommand = _undoStack.Peek();
            
            // 检查是否可以合并（需要ICommand支持合并接口）
            // 这里简化实现，只检查FlowOperationCommand
            if (lastCommand is FlowOperationCommand lastFlowCmd && 
                newCommand is FlowOperationCommand newFlowCmd)
            {
                // TODO: 实现FlowOperationCommand的合并逻辑
                // 现在暂时不支持合并
                return false;
            }

            return false;
        }

        /// <summary>
        /// 4.1.6 修剪栈大小（保持在容量限制内）
        /// </summary>
        private void TrimStack()
        {
            if (_undoStack.Count > _maxStackSize)
            {
                var commands = _undoStack.ToList();
                commands.Reverse(); // 反转以保持正确顺序
                
                // 移除最旧的命令
                commands.RemoveRange(0, commands.Count - _maxStackSize);
                
                _undoStack.Clear();
                commands.Reverse(); // 再次反转
                foreach (var cmd in commands)
                {
                    _undoStack.Push(cmd);
                }
            }
        }

        /// <summary>
        /// 撤销
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }

        /// <summary>
        /// 重做
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }

        /// <summary>
        /// 清空所有命令栈
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// 获取下一个可撤销的命令描述
        /// </summary>
        public string GetUndoDescription()
        {
            return CanUndo ? _undoStack.Peek().Description : null;
        }

        /// <summary>
        /// 获取下一个可重做的命令描述
        /// </summary>
        public string GetRedoDescription()
        {
            return CanRedo ? _redoStack.Peek().Description : null;
        }
    }
}
