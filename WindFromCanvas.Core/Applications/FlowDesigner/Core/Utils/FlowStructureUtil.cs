using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils
{
    /// <summary>
    /// 流程结构工具类（匹配 Activepieces flowStructureUtil）
    /// </summary>
    public static class FlowStructureUtil
    {
        /// <summary>
        /// 判断是否为动作类型
        /// </summary>
        public static bool IsAction(FlowActionType? type)
        {
            return type.HasValue && Enum.IsDefined(typeof(FlowActionType), type.Value);
        }

        /// <summary>
        /// 判断是否为触发器类型
        /// </summary>
        public static bool IsTrigger(FlowTriggerType? type)
        {
            return type.HasValue && Enum.IsDefined(typeof(FlowTriggerType), type.Value);
        }

        /// <summary>
        /// 判断步骤是否为触发器
        /// </summary>
        public static bool IsTrigger(IStep step)
        {
            return step is FlowTrigger;
        }

        /// <summary>
        /// 判断步骤是否为动作
        /// </summary>
        public static bool IsAction(IStep step)
        {
            return step is FlowAction;
        }

        /// <summary>
        /// 获取所有步骤（深度优先遍历）
        /// </summary>
        public static List<IStep> GetAllSteps(IStep root)
        {
            var steps = new List<IStep>();
            TraverseStep(root, steps);
            return steps;
        }

        private static void TraverseStep(IStep step, List<IStep> steps)
        {
            if (step == null) return;

            steps.Add(step);

            // 处理循环动作的子步骤
            if (step is LoopOnItemsAction loop && loop.FirstLoopAction != null)
            {
                TraverseStep(loop.FirstLoopAction, steps);
            }

            // 处理路由动作的子步骤
            if (step is RouterAction router)
            {
                foreach (var child in router.Children)
                {
                    if (child != null)
                    {
                        TraverseStep(child, steps);
                    }
                }
            }

            // 处理下一个动作
            if (step is FlowAction action && action.NextAction != null)
            {
                TraverseStep(action.NextAction, steps);
            }
            else if (step is FlowTrigger trigger && trigger.NextAction != null)
            {
                TraverseStep(trigger.NextAction, steps);
            }
        }

        /// <summary>
        /// 根据名称获取步骤
        /// </summary>
        public static IStep GetStep(string stepName, IStep root)
        {
            return GetAllSteps(root).FirstOrDefault(s => s.Name == stepName);
        }

        /// <summary>
        /// 根据名称获取步骤（如果不存在则抛出异常）
        /// </summary>
        public static IStep GetStepOrThrow(string stepName, IStep root)
        {
            var step = GetStep(stepName, root);
            if (step == null)
            {
                throw new ArgumentException($"Step '{stepName}' not found");
            }
            return step;
        }

        /// <summary>
        /// 获取动作（如果不存在或不是动作则抛出异常）
        /// </summary>
        public static FlowAction GetActionOrThrow(string stepName, IStep root)
        {
            var step = GetStepOrThrow(stepName, root);
            if (!(step is FlowAction action))
            {
                throw new ArgumentException($"Step '{stepName}' is not an action");
            }
            return action;
        }

        /// <summary>
        /// 获取触发器（如果不存在或不是触发器则抛出异常）
        /// </summary>
        public static FlowTrigger GetTriggerOrThrow(string stepName, IStep root)
        {
            var step = GetStepOrThrow(stepName, root);
            if (!(step is FlowTrigger trigger))
            {
                throw new ArgumentException($"Step '{stepName}' is not a trigger");
            }
            return trigger;
        }

        /// <summary>
        /// 获取所有子步骤（不包括 nextAction）
        /// </summary>
        public static List<IStep> GetAllChildSteps(IStep step)
        {
            var children = new List<IStep>();

            if (step is LoopOnItemsAction loop)
            {
                if (loop.FirstLoopAction != null)
                {
                    TraverseStep(loop.FirstLoopAction, children);
                }
            }
            else if (step is RouterAction router)
            {
                foreach (var child in router.Children)
                {
                    if (child != null)
                    {
                        TraverseStep(child, children);
                    }
                }
            }

            return children;
        }

        /// <summary>
        /// 判断 childStepName 是否是 parent 的子步骤
        /// </summary>
        public static bool IsChildOf(IStep parent, string childStepName)
        {
            var children = GetAllChildSteps(parent);
            return children.Any(c => c.Name == childStepName);
        }

        /// <summary>
        /// 查找到目标步骤的路径（所有包含目标步骤的父步骤）
        /// </summary>
        public static List<IStep> FindPathToStep(IStep root, string targetStepName)
        {
            var allSteps = GetAllSteps(root);
            return allSteps
                .Where(step =>
                {
                    var stepChildren = GetAllSteps(step);
                    return stepChildren.Any(s => s.Name == targetStepName) && step.Name != targetStepName;
                })
                .ToList();
        }

        /// <summary>
        /// 获取所有下一个动作（不包括子步骤）
        /// </summary>
        public static List<IStep> GetAllNextActionsWithoutChildren(IStep start)
        {
            var actions = new List<IStep>();
            IStep current = start;

            while (current != null)
            {
                FlowAction nextAction = null;

                if (current is FlowAction action)
                {
                    nextAction = action.NextAction;
                }
                else if (current is FlowTrigger trigger)
                {
                    nextAction = trigger.NextAction;
                }

                if (nextAction != null)
                {
                    actions.Add(nextAction);
                    current = nextAction;
                }
                else
                {
                    break;
                }
            }

            return actions;
        }

        /// <summary>
        /// 查找未使用的步骤名称
        /// </summary>
        public static string FindUnusedName(IStep root)
        {
            var existingNames = GetAllSteps(root).Select(s => s.Name).ToArray();
            return IdGenerator.GenerateStepName(existingNames);
        }

        /// <summary>
        /// 查找多个未使用的步骤名称
        /// </summary>
        public static List<string> FindUnusedNames(IStep root, int count = 1)
        {
            var existingNames = GetAllSteps(root).Select(s => s.Name).ToArray();
            var unusedNames = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var name = IdGenerator.GenerateStepName(existingNames);
                unusedNames.Add(name);
                existingNames = existingNames.Concat(new[] { name }).ToArray();
            }

            return unusedNames;
        }
    }
}
