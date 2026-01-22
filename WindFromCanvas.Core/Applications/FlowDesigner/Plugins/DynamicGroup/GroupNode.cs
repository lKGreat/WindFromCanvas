using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.DynamicGroup
{
    /// <summary>
    /// 6.5.1 分组节点（可折叠的容器节点）
    /// </summary>
    public class GroupNode : FlowNode
    {
        // 6.5.2 子节点容器
        private readonly List<FlowNode> _childNodes = new List<FlowNode>();
        private readonly List<string> _childNodeIds = new List<string>();
        
        // 子节点位置信息存储
        private readonly Dictionary<FlowNode, GroupChildInfo> _childInfoMap = new Dictionary<FlowNode, GroupChildInfo>();
        
        // 6.5.3 折叠状态
        private bool _isCollapsed = false;
        private SizeF _expandedSize;
        private SizeF _collapsedSize = new SizeF(200, 50);

        // 边界渲染
        private const float HeaderHeight = 40f;
        private const float Padding = 20f;
        private const float BorderRadius = 8f;
        private const float ChildPadding = 10f;

        // 交互状态
        private bool _isResizing = false;
        private ResizeHandle _activeResizeHandle = ResizeHandle.None;
        private PointF _resizeStartPoint;
        private SizeF _resizeStartSize;

        // 事件
        public event EventHandler<GroupCollapsedEventArgs> Collapsed;
        public event EventHandler<GroupCollapsedEventArgs> Expanded;
        public event EventHandler<GroupChildEventArgs> ChildAdded;
        public event EventHandler<GroupChildEventArgs> ChildRemoved;
        public event EventHandler<GroupResizedEventArgs> Resized;

        public GroupNode() : base()
        {
            // 设置默认尺寸
            Width = 300;
            Height = 200;
            _expandedSize = new SizeF(Width, Height);
        }

        public GroupNode(GroupNodeData data) : base(data)
        {
            if (data != null)
            {
                _isCollapsed = data.IsCollapsed;
                _childNodeIds.AddRange(data.ChildNodeIds ?? new List<string>());
                _expandedSize = new SizeF(data.ExpandedWidth, data.ExpandedHeight);
                
                if (_isCollapsed)
                {
                    Width = _collapsedSize.Width;
                    Height = _collapsedSize.Height;
                }
            }
            else
            {
                Width = 300;
                Height = 200;
                _expandedSize = new SizeF(Width, Height);
            }
        }

        /// <summary>
        /// 是否折叠
        /// </summary>
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed != value)
                {
                    _isCollapsed = value;
                    if (value)
                    {
                        Collapse();
                    }
                    else
                    {
                        Expand();
                    }
                }
            }
        }

        /// <summary>
        /// 子节点列表
        /// </summary>
        public IReadOnlyList<FlowNode> ChildNodes => _childNodes.AsReadOnly();

        /// <summary>
        /// 子节点ID列表
        /// </summary>
        public IReadOnlyList<string> ChildNodeIds => _childNodeIds.AsReadOnly();

        /// <summary>
        /// 展开尺寸
        /// </summary>
        public SizeF ExpandedSize
        {
            get => _expandedSize;
            set
            {
                _expandedSize = value;
                if (!_isCollapsed)
                {
                    Width = value.Width;
                    Height = value.Height;
                }
            }
        }

        /// <summary>
        /// 6.5.2 添加子节点
        /// </summary>
        public void AddChild(FlowNode node)
        {
            if (node == null || node == this) return;
            if (_childNodes.Contains(node)) return;

            _childNodes.Add(node);
            if (!string.IsNullOrEmpty(node.Data?.Name) && !_childNodeIds.Contains(node.Data.Name))
            {
                _childNodeIds.Add(node.Data.Name);
            }

            // 更新子节点位置（相对于组）
            var childInfo = new GroupChildInfo
            {
                ParentGroup = this,
                RelativeX = node.X - X - Padding,
                RelativeY = node.Y - Y - HeaderHeight
            };
            _childInfoMap[node] = childInfo;

            // 调整组大小以容纳子节点
            AdjustSizeToFitChildren();

            ChildAdded?.Invoke(this, new GroupChildEventArgs { Node = node, Group = this });
        }

        /// <summary>
        /// 6.5.2 移除子节点
        /// </summary>
        public void RemoveChild(FlowNode node)
        {
            if (node == null) return;

            if (_childNodes.Remove(node))
            {
                if (!string.IsNullOrEmpty(node.Data?.Name))
                {
                    _childNodeIds.Remove(node.Data.Name);
                }

                // 清除子节点的组信息
                _childInfoMap.Remove(node);

                ChildRemoved?.Invoke(this, new GroupChildEventArgs { Node = node, Group = this });
            }
        }

        /// <summary>
        /// 获取子节点的组信息
        /// </summary>
        public GroupChildInfo GetChildInfo(FlowNode node)
        {
            if (node == null) return null;
            _childInfoMap.TryGetValue(node, out var info);
            return info;
        }

        /// <summary>
        /// 6.5.5 检查节点是否在组边界内
        /// </summary>
        public bool ContainsPoint(PointF point)
        {
            var bounds = GetContentBounds();
            return bounds.Contains(point);
        }

        /// <summary>
        /// 检查节点是否完全在组内
        /// </summary>
        public bool ContainsNode(FlowNode node)
        {
            if (node == null) return false;
            
            var contentBounds = GetContentBounds();
            var nodeBounds = new RectangleF(node.X, node.Y, node.Width, node.Height);
            
            return contentBounds.Contains(nodeBounds);
        }

        /// <summary>
        /// 获取内容区域边界
        /// </summary>
        public RectangleF GetContentBounds()
        {
            return new RectangleF(
                X + Padding,
                Y + HeaderHeight,
                Width - Padding * 2,
                Height - HeaderHeight - Padding
            );
        }

        /// <summary>
        /// 6.5.3 折叠组
        /// </summary>
        private void Collapse()
        {
            // 保存当前尺寸
            _expandedSize = new SizeF(Width, Height);

            // 设置折叠尺寸
            Width = _collapsedSize.Width;
            Height = _collapsedSize.Height;

            // 隐藏子节点
            foreach (var child in _childNodes)
            {
                child.Visible = false;
            }

            Collapsed?.Invoke(this, new GroupCollapsedEventArgs { Group = this });
        }

        /// <summary>
        /// 6.5.3 展开组
        /// </summary>
        private void Expand()
        {
            // 恢复展开尺寸
            Width = _expandedSize.Width;
            Height = _expandedSize.Height;

            // 显示子节点并更新位置
            foreach (var child in _childNodes)
            {
                child.Visible = true;
                
                // 恢复相对位置
                if (_childInfoMap.TryGetValue(child, out var info))
                {
                    child.X = X + Padding + info.RelativeX;
                    child.Y = Y + HeaderHeight + info.RelativeY;
                }
            }

            Expanded?.Invoke(this, new GroupCollapsedEventArgs { Group = this });
        }

        /// <summary>
        /// 切换折叠状态
        /// </summary>
        public void ToggleCollapse()
        {
            IsCollapsed = !IsCollapsed;
        }

        /// <summary>
        /// 调整组大小以适应子节点
        /// </summary>
        public void AdjustSizeToFitChildren()
        {
            if (_childNodes.Count == 0 || _isCollapsed) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var child in _childNodes)
            {
                minX = Math.Min(minX, child.X);
                minY = Math.Min(minY, child.Y);
                maxX = Math.Max(maxX, child.X + child.Width);
                maxY = Math.Max(maxY, child.Y + child.Height);
            }

            // 计算新尺寸
            float newWidth = maxX - minX + Padding * 2 + ChildPadding * 2;
            float newHeight = maxY - minY + HeaderHeight + Padding + ChildPadding * 2;

            // 设置最小尺寸
            newWidth = Math.Max(newWidth, 200);
            newHeight = Math.Max(newHeight, 100);

            Width = newWidth;
            Height = newHeight;
            _expandedSize = new SizeF(Width, Height);

            // 更新组位置
            X = minX - Padding - ChildPadding;
            Y = minY - HeaderHeight - ChildPadding;
        }

        /// <summary>
        /// 移动组（同时移动所有子节点）
        /// </summary>
        public void Move(float deltaX, float deltaY)
        {
            // 移动组本身
            X += deltaX;
            Y += deltaY;

            // 移动所有子节点
            if (!_isCollapsed)
            {
                foreach (var child in _childNodes)
                {
                    child.X += deltaX;
                    child.Y += deltaY;
                }
            }
        }

        /// <summary>
        /// 6.5.4 渲染组边界
        /// </summary>
        public override void Draw(Graphics g)
        {
            var theme = ThemeManager.Instance.CurrentTheme;

            // 绘制组背景
            var bounds = new RectangleF(X, Y, Width, Height);
            
            using (var path = CreateRoundedRectPath(bounds, BorderRadius))
            {
                // 背景填充（半透明）
                using (var bgBrush = new SolidBrush(Color.FromArgb(40, theme.Primary)))
                {
                    g.FillPath(bgBrush, path);
                }

                // 边框
                using (var borderPen = new Pen(theme.Primary, 2) { DashStyle = DashStyle.Dash })
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 绘制头部
            DrawHeader(g, theme);

            // 如果展开，绘制调整大小手柄
            if (!_isCollapsed)
            {
                DrawResizeHandles(g, theme);
            }
        }

        /// <summary>
        /// 绘制头部
        /// </summary>
        private void DrawHeader(Graphics g, ThemeConfig theme)
        {
            var headerBounds = new RectangleF(X, Y, Width, HeaderHeight);
            
            // 头部背景
            using (var headerPath = CreateRoundedRectPath(headerBounds, BorderRadius, true, true, false, false))
            using (var headerBrush = new SolidBrush(Color.FromArgb(80, theme.Primary)))
            {
                g.FillPath(headerBrush, headerPath);
            }

            // 折叠/展开图标
            var iconRect = new RectangleF(X + 10, Y + (HeaderHeight - 16) / 2, 16, 16);
            DrawCollapseIcon(g, iconRect, theme);

            // 组名称
            using (var font = new Font("Segoe UI Semibold", 10))
            using (var textBrush = new SolidBrush(theme.TextPrimary))
            {
                var textRect = new RectangleF(X + 32, Y, Width - 42, HeaderHeight);
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(Data?.Name ?? "Group", font, textBrush, textRect, format);
            }

            // 子节点数量
            using (var countFont = new Font("Segoe UI", 8))
            using (var countBrush = new SolidBrush(theme.TextSecondary))
            {
                var countText = string.Format("({0})", _childNodes.Count);
                var countSize = g.MeasureString(countText, countFont);
                g.DrawString(countText, countFont, countBrush, 
                    X + Width - countSize.Width - 10, 
                    Y + (HeaderHeight - countSize.Height) / 2);
            }
        }

        /// <summary>
        /// 绘制折叠/展开图标
        /// </summary>
        private void DrawCollapseIcon(Graphics g, RectangleF rect, ThemeConfig theme)
        {
            using (var pen = new Pen(theme.TextPrimary, 2))
            {
                var centerX = rect.X + rect.Width / 2;
                var centerY = rect.Y + rect.Height / 2;

                if (_isCollapsed)
                {
                    // 展开箭头 (>)
                    g.DrawLine(pen, centerX - 3, centerY - 4, centerX + 3, centerY);
                    g.DrawLine(pen, centerX + 3, centerY, centerX - 3, centerY + 4);
                }
                else
                {
                    // 折叠箭头 (v)
                    g.DrawLine(pen, centerX - 4, centerY - 3, centerX, centerY + 3);
                    g.DrawLine(pen, centerX, centerY + 3, centerX + 4, centerY - 3);
                }
            }
        }

        /// <summary>
        /// 绘制调整大小手柄
        /// </summary>
        private void DrawResizeHandles(Graphics g, ThemeConfig theme)
        {
            const float handleSize = 8;
            using (var handleBrush = new SolidBrush(theme.Primary))
            {
                // 右下角手柄
                g.FillRectangle(handleBrush,
                    X + Width - handleSize,
                    Y + Height - handleSize,
                    handleSize, handleSize);
            }
        }

        /// <summary>
        /// 检查点击是否在头部区域
        /// </summary>
        public bool IsPointInHeader(PointF point)
        {
            var headerBounds = new RectangleF(X, Y, Width, HeaderHeight);
            return headerBounds.Contains(point);
        }

        /// <summary>
        /// 检查点击是否在调整大小手柄上
        /// </summary>
        public ResizeHandle GetResizeHandleAt(PointF point)
        {
            if (_isCollapsed) return ResizeHandle.None;

            const float handleSize = 12;
            
            // 右下角
            if (point.X >= X + Width - handleSize && point.Y >= Y + Height - handleSize)
                return ResizeHandle.BottomRight;

            return ResizeHandle.None;
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius, 
            bool topLeft = true, bool topRight = true, bool bottomRight = true, bool bottomLeft = true)
        {
            var path = new GraphicsPath();
            
            if (topLeft)
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            else
                path.AddLine(rect.X, rect.Y + radius, rect.X, rect.Y);
            
            if (topRight)
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            else
                path.AddLine(rect.Right - radius, rect.Y, rect.Right, rect.Y);
            
            if (bottomRight)
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            else
                path.AddLine(rect.Right, rect.Bottom - radius, rect.Right, rect.Bottom);
            
            if (bottomLeft)
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            else
                path.AddLine(rect.X + radius, rect.Bottom, rect.X, rect.Bottom);
            
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        public GroupNodeData GetGroupData()
        {
            return new GroupNodeData
            {
                Name = Data?.Name,
                PositionX = X,
                PositionY = Y,
                GroupWidth = Width,
                GroupHeight = Height,
                IsCollapsed = _isCollapsed,
                ChildNodeIds = new List<string>(_childNodeIds),
                ExpandedWidth = _expandedSize.Width,
                ExpandedHeight = _expandedSize.Height,
                Type = FlowNodeType.Group
            };
        }
    }

    /// <summary>
    /// 调整大小手柄类型
    /// </summary>
    public enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom,
        Left,
        Right
    }

    /// <summary>
    /// 组子节点信息
    /// </summary>
    public class GroupChildInfo
    {
        public GroupNode ParentGroup { get; set; }
        public float RelativeX { get; set; }
        public float RelativeY { get; set; }
    }

    /// <summary>
    /// 组节点数据
    /// </summary>
    public class GroupNodeData : FlowNodeData
    {
        public bool IsCollapsed { get; set; }
        public List<string> ChildNodeIds { get; set; } = new List<string>();
        public float ExpandedWidth { get; set; } = 300;
        public float ExpandedHeight { get; set; } = 200;
        public float GroupWidth { get; set; } = 300;
        public float GroupHeight { get; set; } = 200;
    }

    #region 事件参数

    public class GroupCollapsedEventArgs : EventArgs
    {
        public GroupNode Group { get; set; }
    }

    public class GroupChildEventArgs : EventArgs
    {
        public GroupNode Group { get; set; }
        public FlowNode Node { get; set; }
    }

    public class GroupResizedEventArgs : EventArgs
    {
        public GroupNode Group { get; set; }
        public SizeF OldSize { get; set; }
        public SizeF NewSize { get; set; }
    }

    #endregion
}
