using System;
using System.Collections.Generic;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Commands
{
    /// <summary>
    /// 命令管理器 - 管理命令栈，支持撤销/重做
    /// </summary>
    public class CommandManager
    {
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();
        private const int MaxStackSize = 100;

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(ICommand command)
        {
            if (command == null) return;

            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // 执行新命令后清空重做栈

            // 限制栈大小
            if (_undoStack.Count > MaxStackSize)
            {
                var commands = _undoStack.ToList();
                commands.RemoveAt(0);
                _undoStack.Clear();
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
