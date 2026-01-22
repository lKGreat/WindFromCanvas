using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// GraphModel中央状态管理类，管理图的所有节点和连线
    /// 类似LogicFlow的GraphModel，提供统一的数据访问接口
    /// </summary>
    public class GraphModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        private ObservableCollection<FlowNodeData> _nodes;
        private ObservableCollection<FlowConnectionData> _edges;
        private List<string> _selectedNodeIds;
        private RectangleF _viewportBounds;

        /// <summary>
        /// 节点集合
        /// </summary>
        public ObservableCollection<FlowNodeData> Nodes
        {
            get => _nodes;
            private set
            {
                if (_nodes != null)
                {
                    _nodes.CollectionChanged -= OnNodesCollectionChanged;
                }
                _nodes = value;
                if (_nodes != null)
                {
                    _nodes.CollectionChanged += OnNodesCollectionChanged;
                }
                OnPropertyChanged(nameof(Nodes));
                OnPropertyChanged(nameof(NodeCount));
            }
        }

        /// <summary>
        /// 连线集合
        /// </summary>
        public ObservableCollection<FlowConnectionData> Edges
        {
            get => _edges;
            private set
            {
                if (_edges != null)
                {
                    _edges.CollectionChanged -= OnEdgesCollectionChanged;
                }
                _edges = value;
                if (_edges != null)
                {
                    _edges.CollectionChanged += OnEdgesCollectionChanged;
                }
                OnPropertyChanged(nameof(Edges));
                OnPropertyChanged(nameof(EdgeCount));
            }
        }

        /// <summary>
        /// 选中的节点ID列表
        /// </summary>
        public List<string> SelectedNodeIds
        {
            get => _selectedNodeIds;
            set
            {
                _selectedNodeIds = value;
                OnPropertyChanged(nameof(SelectedNodeIds));
                OnPropertyChanged(nameof(SelectedElements));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        /// <summary>
        /// 视口边界（用于虚拟滚动）
        /// </summary>
        public RectangleF ViewportBounds
        {
            get => _viewportBounds;
            set
            {
                _viewportBounds = value;
                OnPropertyChanged(nameof(ViewportBounds));
                OnPropertyChanged(nameof(VisibleNodes));
            }
        }

        #region 计算属性

        /// <summary>
        /// 节点数量
        /// </summary>
        public int NodeCount => _nodes?.Count ?? 0;

        /// <summary>
        /// 连线数量
        /// </summary>
        public int EdgeCount => _edges?.Count ?? 0;

        /// <summary>
        /// 是否有选中的节点
        /// </summary>
        public bool HasSelection => _selectedNodeIds != null && _selectedNodeIds.Count > 0;

        /// <summary>
        /// 选中的元素集合（节点数据）
        /// </summary>
        public IEnumerable<FlowNodeData> SelectedElements
        {
            get
            {
                if (_selectedNodeIds == null || _nodes == null)
                {
                    return Enumerable.Empty<FlowNodeData>();
                }
                return _nodes.Where(n => _selectedNodeIds.Contains(n.Name));
            }
        }

        /// <summary>
        /// 可见节点集合（在视口范围内的节点）
        /// </summary>
        public IEnumerable<FlowNodeData> VisibleNodes
        {
            get
            {
                if (_nodes == null || _viewportBounds.IsEmpty)
                {
                    return _nodes ?? Enumerable.Empty<FlowNodeData>();
                }

                // 使用节点位置判断是否在视口内
                return _nodes.Where(n =>
                {
                    // 简单的边界检查，考虑节点大小的缓冲区
                    const float nodeBuffer = 100; // 节点大小缓冲
                    return n.PositionX + nodeBuffer >= _viewportBounds.Left &&
                           n.PositionX - nodeBuffer <= _viewportBounds.Right &&
                           n.PositionY + nodeBuffer >= _viewportBounds.Top &&
                           n.PositionY - nodeBuffer <= _viewportBounds.Bottom;
                });
            }
        }

        /// <summary>
        /// 可见连线集合（连接可见节点的连线）
        /// </summary>
        public IEnumerable<FlowConnectionData> VisibleEdges
        {
            get
            {
                if (_edges == null)
                {
                    return Enumerable.Empty<FlowConnectionData>();
                }

                var visibleNodeNames = new HashSet<string>(VisibleNodes.Select(n => n.Name));
                return _edges.Where(e =>
                    visibleNodeNames.Contains(e.SourceNode) ||
                    visibleNodeNames.Contains(e.TargetNode));
            }
        }

        #endregion

        public GraphModel()
        {
            Nodes = new ObservableCollection<FlowNodeData>();
            Edges = new ObservableCollection<FlowConnectionData>();
            _selectedNodeIds = new List<string>();
            _viewportBounds = RectangleF.Empty;
        }

        #region 节点操作

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(FlowNodeData node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (string.IsNullOrEmpty(node.Name))
                throw new ArgumentException("Node name cannot be empty");

            if (_nodes.Any(n => n.Name == node.Name))
                throw new InvalidOperationException($"Node with name '{node.Name}' already exists");

            _nodes.Add(node);
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        public bool RemoveNode(string nodeName)
        {
            var node = _nodes.FirstOrDefault(n => n.Name == nodeName);
            if (node != null)
            {
                // 移除与该节点相关的所有连线
                var relatedEdges = _edges.Where(e =>
                    e.SourceNode == nodeName || e.TargetNode == nodeName).ToList();

                foreach (var edge in relatedEdges)
                {
                    _edges.Remove(edge);
                }

                return _nodes.Remove(node);
            }
            return false;
        }

        /// <summary>
        /// 根据名称获取节点
        /// </summary>
        public FlowNodeData GetNode(string nodeName)
        {
            return _nodes.FirstOrDefault(n => n.Name == nodeName);
        }

        /// <summary>
        /// 更新节点位置
        /// </summary>
        public void UpdateNodePosition(string nodeName, float x, float y)
        {
            var node = GetNode(nodeName);
            if (node != null)
            {
                node.PositionX = x;
                node.PositionY = y;
            }
        }

        #endregion

        #region 连线操作

        /// <summary>
        /// 添加连线
        /// </summary>
        public void AddEdge(FlowConnectionData edge)
        {
            if (edge == null)
                throw new ArgumentNullException(nameof(edge));

            if (string.IsNullOrEmpty(edge.SourceNode) || string.IsNullOrEmpty(edge.TargetNode))
                throw new ArgumentException("Edge source and target cannot be empty");

            // 检查是否已存在相同的连线
            if (_edges.Any(e => e.SourceNode == edge.SourceNode &&
                               e.TargetNode == edge.TargetNode &&
                               e.SourceAnchorId == edge.SourceAnchorId &&
                               e.TargetAnchorId == edge.TargetAnchorId))
            {
                throw new InvalidOperationException("Duplicate edge already exists");
            }

            _edges.Add(edge);
        }

        /// <summary>
        /// 移除连线
        /// </summary>
        public bool RemoveEdge(FlowConnectionData edge)
        {
            return _edges.Remove(edge);
        }

        /// <summary>
        /// 移除连线（根据源和目标节点）
        /// </summary>
        public bool RemoveEdge(string sourceNode, string targetNode)
        {
            var edge = _edges.FirstOrDefault(e =>
                e.SourceNode == sourceNode && e.TargetNode == targetNode);
            return edge != null && _edges.Remove(edge);
        }

        /// <summary>
        /// 获取节点的所有输出连线
        /// </summary>
        public IEnumerable<FlowConnectionData> GetOutgoingEdges(string nodeName)
        {
            return _edges.Where(e => e.SourceNode == nodeName);
        }

        /// <summary>
        /// 获取节点的所有输入连线
        /// </summary>
        public IEnumerable<FlowConnectionData> GetIncomingEdges(string nodeName)
        {
            return _edges.Where(e => e.TargetNode == nodeName);
        }

        #endregion

        #region 选择操作

        /// <summary>
        /// 选择节点
        /// </summary>
        public void SelectNode(string nodeName, bool append = false)
        {
            if (!append)
            {
                SelectedNodeIds = new List<string> { nodeName };
            }
            else if (!_selectedNodeIds.Contains(nodeName))
            {
                _selectedNodeIds.Add(nodeName);
                OnPropertyChanged(nameof(SelectedNodeIds));
                OnPropertyChanged(nameof(SelectedElements));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        /// <summary>
        /// 取消选择节点
        /// </summary>
        public void DeselectNode(string nodeName)
        {
            if (_selectedNodeIds.Remove(nodeName))
            {
                OnPropertyChanged(nameof(SelectedNodeIds));
                OnPropertyChanged(nameof(SelectedElements));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedNodeIds.Count > 0)
            {
                SelectedNodeIds = new List<string>();
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        public void SelectAll()
        {
            SelectedNodeIds = new List<string>(_nodes.Select(n => n.Name));
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量添加节点
        /// </summary>
        public void AddNodes(IEnumerable<FlowNodeData> nodes)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }

        /// <summary>
        /// 批量添加连线
        /// </summary>
        public void AddEdges(IEnumerable<FlowConnectionData> edges)
        {
            foreach (var edge in edges)
            {
                AddEdge(edge);
            }
        }

        /// <summary>
        /// 清空图数据
        /// </summary>
        public void Clear()
        {
            _edges.Clear();
            _nodes.Clear();
            _selectedNodeIds.Clear();
            OnPropertyChanged(nameof(SelectedNodeIds));
            OnPropertyChanged(nameof(SelectedElements));
            OnPropertyChanged(nameof(HasSelection));
        }

        #endregion

        #region 事件处理

        private void OnNodesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(NodeCount));
            OnPropertyChanged(nameof(VisibleNodes));
            CollectionChanged?.Invoke(this, e);
        }

        private void OnEdgesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(EdgeCount));
            OnPropertyChanged(nameof(VisibleEdges));
            CollectionChanged?.Invoke(this, e);
        }

        #endregion

        #region INotifyPropertyChanged / INotifyCollectionChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
