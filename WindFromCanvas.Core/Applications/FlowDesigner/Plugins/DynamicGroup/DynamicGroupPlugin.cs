using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.DynamicGroup
{
    /// <summary>
    /// 6.5 动态分组插件
    /// 提供可折叠的容器节点功能
    /// </summary>
    public class DynamicGroupPlugin : FlowPluginBase
    {
        public override string PluginName => "DynamicGroup";
        public override string DisplayName => "Dynamic Group";
        public override string Description => "Provides collapsible container nodes for grouping other nodes";
        public override Version Version => new Version(1, 0, 0);

        private readonly Dictionary<string, GroupNode> _groups = new Dictionary<string, GroupNode>();
        private IPluginContext _context;

        // 6.5.6 子流程事件
        public event EventHandler<GroupCreatedEventArgs> GroupCreated;
        public event EventHandler<GroupDeletedEventArgs> GroupDeleted;
        public event EventHandler<NodeGroupChangedEventArgs> NodeAddedToGroup;
        public event EventHandler<NodeGroupChangedEventArgs> NodeRemovedFromGroup;

        protected override void OnInitialize()
        {
            _context = Context;
            
            // 注册组节点类型
            Context.RegisterNodeType("group:container", typeof(GroupNodeData), typeof(GroupNode));

            // 订阅相关事件
            Context.EventBus.Subscribe("node:dragend", OnNodeDragEnd);
            Context.EventBus.Subscribe("node:delete", OnNodeDelete);
            Context.EventBus.Subscribe("selection:changed", OnSelectionChanged);
        }

        protected override void OnDestroy()
        {
            _groups.Clear();
        }

        #region 组管理

        /// <summary>
        /// 创建新组
        /// </summary>
        public GroupNode CreateGroup(string name = null, RectangleF? bounds = null)
        {
            var groupData = new GroupNodeData
            {
                Name = name ?? string.Format("Group_{0}", _groups.Count + 1),
                Type = FlowNodeType.Group,
                PositionX = bounds?.X ?? 100,
                PositionY = bounds?.Y ?? 100,
                GroupWidth = bounds?.Width ?? 300,
                GroupHeight = bounds?.Height ?? 200
            };

            var group = new GroupNode(groupData);
            _groups[groupData.Name] = group;

            GroupCreated?.Invoke(this, new GroupCreatedEventArgs { Group = group });
            Context.EventBus.Publish("group:created", group);

            return group;
        }

        /// <summary>
        /// 从选中的节点创建组
        /// </summary>
        public GroupNode CreateGroupFromNodes(IEnumerable<FlowNode> nodes, string name = null)
        {
            var nodeList = nodes?.Where(n => !(n is GroupNode)).ToList();
            if (nodeList == null || nodeList.Count == 0) return null;

            // 计算边界
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var node in nodeList)
            {
                minX = Math.Min(minX, node.X);
                minY = Math.Min(minY, node.Y);
                maxX = Math.Max(maxX, node.X + node.Width);
                maxY = Math.Max(maxY, node.Y + node.Height);
            }

            // 添加边距
            const float padding = 40;
            const float headerHeight = 50;

            var bounds = new RectangleF(
                minX - padding,
                minY - headerHeight - padding,
                maxX - minX + padding * 2,
                maxY - minY + headerHeight + padding * 2
            );

            var group = CreateGroup(name, bounds);

            // 添加节点到组
            foreach (var node in nodeList)
            {
                group.AddChild(node);
            }

            return group;
        }

        /// <summary>
        /// 删除组
        /// </summary>
        public void DeleteGroup(string groupName, bool releaseChildren = true)
        {
            if (!_groups.TryGetValue(groupName, out var group))
                return;

            if (releaseChildren)
            {
                // 释放所有子节点
                foreach (var child in group.ChildNodes.ToList())
                {
                    group.RemoveChild(child);
                }
            }

            _groups.Remove(groupName);
            GroupDeleted?.Invoke(this, new GroupDeletedEventArgs { GroupName = groupName });
            Context.EventBus.Publish("group:deleted", groupName);
        }

        /// <summary>
        /// 获取组
        /// </summary>
        public GroupNode GetGroup(string groupName)
        {
            _groups.TryGetValue(groupName, out var group);
            return group;
        }

        /// <summary>
        /// 获取所有组
        /// </summary>
        public IEnumerable<GroupNode> GetAllGroups()
        {
            return _groups.Values;
        }

        /// <summary>
        /// 获取节点所属的组
        /// </summary>
        public GroupNode GetNodeGroup(FlowNode node)
        {
            if (node == null) return null;

            // 遍历所有组查找包含该节点的组
            return _groups.Values.FirstOrDefault(g => g.ChildNodes.Contains(node));
        }

        #endregion

        #region 6.5.5 拖入/拖出逻辑

        /// <summary>
        /// 将节点添加到组
        /// </summary>
        public bool AddNodeToGroup(FlowNode node, GroupNode group)
        {
            if (node == null || group == null) return false;
            if (node is GroupNode) return false; // 不能嵌套组
            if (group.ChildNodes.Contains(node)) return false;

            // 从旧组中移除
            var oldGroup = GetNodeGroup(node);
            if (oldGroup != null)
            {
                oldGroup.RemoveChild(node);
            }

            // 添加到新组
            group.AddChild(node);

            NodeAddedToGroup?.Invoke(this, new NodeGroupChangedEventArgs
            {
                Node = node,
                Group = group
            });
            Context.EventBus.Publish("group:nodeadded", new { Node = node, Group = group });

            return true;
        }

        /// <summary>
        /// 将节点从组中移除
        /// </summary>
        public bool RemoveNodeFromGroup(FlowNode node)
        {
            var group = GetNodeGroup(node);
            if (group == null) return false;

            group.RemoveChild(node);

            NodeRemovedFromGroup?.Invoke(this, new NodeGroupChangedEventArgs
            {
                Node = node,
                Group = group
            });
            Context.EventBus.Publish("group:noderemoved", new { Node = node, Group = group });

            return true;
        }

        /// <summary>
        /// 检测节点拖拽结束时是否应该加入/离开组
        /// </summary>
        private void OnNodeDragEnd(object eventData)
        {
            var node = eventData as FlowNode;
            if (node == null) return;
            if (node is GroupNode) return;

            // 检查节点是否在某个组的边界内
            GroupNode targetGroup = null;
            foreach (var group in _groups.Values)
            {
                if (group.ContainsNode(node))
                {
                    targetGroup = group;
                    break;
                }
            }

            var currentGroup = GetNodeGroup(node);

            if (targetGroup != null && targetGroup != currentGroup)
            {
                // 拖入新组
                AddNodeToGroup(node, targetGroup);
            }
            else if (targetGroup == null && currentGroup != null)
            {
                // 拖出组
                RemoveNodeFromGroup(node);
            }
        }

        /// <summary>
        /// 节点删除时从组中移除
        /// </summary>
        private void OnNodeDelete(object eventData)
        {
            var node = eventData as FlowNode;
            if (node != null)
            {
                var group = GetNodeGroup(node);
                if (group != null)
                {
                    group.RemoveChild(node);
                }
            }
        }

        /// <summary>
        /// 选择变更时的处理
        /// </summary>
        private void OnSelectionChanged(object eventData)
        {
            // 可以在这里添加选择组时的特殊处理
        }

        #endregion

        #region 组操作

        /// <summary>
        /// 折叠所有组
        /// </summary>
        public void CollapseAllGroups()
        {
            foreach (var group in _groups.Values)
            {
                group.IsCollapsed = true;
            }
        }

        /// <summary>
        /// 展开所有组
        /// </summary>
        public void ExpandAllGroups()
        {
            foreach (var group in _groups.Values)
            {
                group.IsCollapsed = false;
            }
        }

        /// <summary>
        /// 解散组（删除组但保留子节点）
        /// </summary>
        public void UngroupNodes(GroupNode group)
        {
            if (group == null) return;
            DeleteGroup(group.Data?.Name, releaseChildren: true);
        }

        /// <summary>
        /// 自动调整所有组大小
        /// </summary>
        public void AutoFitAllGroups()
        {
            foreach (var group in _groups.Values)
            {
                if (!group.IsCollapsed)
                {
                    group.AdjustSizeToFitChildren();
                }
            }
        }

        #endregion

        public override void Render(Graphics g, RectangleF viewport)
        {
            // 组节点由画布统一渲染，这里可以添加额外的装饰
        }

        protected override void OnConfigurationChanged()
        {
            // 应用配置变更
            var config = GetConfiguration();
            // 可以在这里添加配置处理
        }
    }

    #region 事件参数

    public class GroupCreatedEventArgs : EventArgs
    {
        public GroupNode Group { get; set; }
    }

    public class GroupDeletedEventArgs : EventArgs
    {
        public string GroupName { get; set; }
    }

    public class NodeGroupChangedEventArgs : EventArgs
    {
        public FlowNode Node { get; set; }
        public GroupNode Group { get; set; }
    }

    #endregion
}
