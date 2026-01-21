using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Events;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Connections
{
    /// <summary>
    /// 流程连接线
    /// </summary>
    public class FlowConnection : CanvasObject
    {
        /// <summary>
        /// 连接数据
        /// </summary>
        public FlowConnectionData Data { get; set; }

        /// <summary>
        /// 源节点
        /// </summary>
        public FlowNode SourceNode { get; set; }

        /// <summary>
        /// 目标节点
        /// </summary>
        public FlowNode TargetNode { get; set; }

        /// <summary>
        /// 是否为循环返回连接
        /// </summary>
        public bool IsLoopReturn { get; set; }

        /// <summary>
        /// 连接线颜色（Activepieces标准）
        /// </summary>
        public Color LineColor { get; set; } = Color.FromArgb(148, 163, 184);

        /// <summary>
        /// 连接线宽度（Activepieces标准：1.5px）
        /// </summary>
        public float LineWidth { get; set; } = 1.5f;

        /// <summary>
        /// 箭头大小
        /// </summary>
        public float ArrowSize { get; set; } = 6f;

        /// <summary>
        /// 圆角半径（Activepieces ARC_LENGTH：15px）
        /// </summary>
        public float ArcLength { get; set; } = 15f;

        /// <summary>
        /// 是否为虚线（用于跳过的连接）
        /// </summary>
        public bool IsDashed { get; set; } = false;

        /// <summary>
        /// 悬停时的连接线颜色
        /// </summary>
        public Color HoverLineColor { get; set; } = Color.FromArgb(59, 130, 246);

        /// <summary>
        /// 选中时的连接线颜色
        /// </summary>
        public Color SelectedLineColor { get; set; } = Color.FromArgb(59, 130, 246);

        /// <summary>
        /// 是否显示添加按钮（连接线中点）
        /// </summary>
        public bool ShowAddButton { get; set; } = true;

        /// <summary>
        /// 添加按钮大小（Activepieces标准：20x20）
        /// </summary>
        public float AddButtonSize { get; set; } = 20f;

        /// <summary>
        /// 添加按钮是否悬停
        /// </summary>
        public bool IsAddButtonHovered { get; set; }

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否悬停
        /// </summary>
        public bool IsHovered { get; set; }

        public FlowConnection()
        {
            Draggable = false;
            ZIndex = 1; // 连接线在节点下方
        }

        public FlowConnection(FlowConnectionData data, FlowNode sourceNode, FlowNode targetNode) : this()
        {
            Data = data;
            SourceNode = sourceNode;
            TargetNode = targetNode;
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || SourceNode == null || TargetNode == null) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty) return;

            // 确定连接线颜色
            var color = IsSelected ? SelectedLineColor : 
                       (IsHovered ? HoverLineColor : LineColor);
            
            // 创建画笔
            using (var pen = new Pen(color, LineWidth))
            {
                pen.EndCap = LineCap.Round;
                pen.StartCap = LineCap.Round;
                
                // 如果是虚线，设置虚线样式
                if (IsDashed)
                {
                    pen.DashStyle = DashStyle.Dash;
                    pen.DashPattern = new float[] { 4f, 4f };
                }

                if (IsLoopReturn)
                {
                    // 循环返回连接：绘制带圆角的曲线
                    DrawLoopReturnLine(g, startPoint, endPoint, pen);
                }
                else
                {
                    // 普通连接：绘制直线（带圆角转角）
                    DrawStraightLine(g, startPoint, endPoint, pen);
                }
            }

            // 绘制箭头（只在非虚线时绘制）
            if (!IsDashed)
            {
                DrawArrow(g, startPoint, endPoint, color);
            }

            // 绘制添加按钮（连接线中点）
            if (ShowAddButton && !IsLoopReturn)
            {
                DrawAddButton(g, startPoint, endPoint);
            }

            // 绘制连接线标签（如果有）
            if (Data != null && !string.IsNullOrEmpty(Data.Label))
            {
                DrawLabel(g, startPoint, endPoint, color);
            }
        }

        /// <summary>
        /// 绘制连接线标签（中点位置）
        /// </summary>
        private void DrawLabel(Graphics g, PointF start, PointF end, Color lineColor)
        {
            var midPoint = new PointF((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            var labelText = Data.Label;
            
            // 测量文本大小
            var textSize = g.MeasureString(labelText, SystemFonts.DefaultFont);
            var labelRect = new RectangleF(
                midPoint.X - textSize.Width / 2 - 5,
                midPoint.Y - textSize.Height / 2 - 3,
                textSize.Width + 10,
                textSize.Height + 6
            );

            // 绘制标签背景（白色，半透明）
            using (var brush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
            {
                g.FillRectangle(brush, labelRect);
            }

            // 绘制标签边框
            using (var pen = new Pen(lineColor, 1f))
            {
                g.DrawRectangle(pen, labelRect.X, labelRect.Y, labelRect.Width, labelRect.Height);
            }

            // 绘制文本
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(labelText, SystemFonts.DefaultFont, brush, labelRect, sf);
            }
        }

        /// <summary>
        /// 绘制添加按钮（连接线中点，Activepieces风格）
        /// </summary>
        private void DrawAddButton(Graphics g, PointF start, PointF end)
        {
            // 计算中点
            var midPoint = new PointF((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            var buttonRect = new RectangleF(
                midPoint.X - AddButtonSize / 2,
                midPoint.Y - AddButtonSize / 2,
                AddButtonSize,
                AddButtonSize
            );

            // 悬停时放大1.2倍
            if (IsAddButtonHovered)
            {
                var scale = 1.2f;
                buttonRect = new RectangleF(
                    midPoint.X - AddButtonSize * scale / 2,
                    midPoint.Y - AddButtonSize * scale / 2,
                    AddButtonSize * scale,
                    AddButtonSize * scale
                );
            }

            // 绘制按钮背景（圆形，白色）
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, buttonRect);
            }

            // 绘制边框
            var borderColor = IsAddButtonHovered ? Color.FromArgb(59, 130, 246) : Color.FromArgb(226, 232, 240);
            using (var pen = new Pen(borderColor, 1.5f))
            {
                g.DrawEllipse(pen, buttonRect);
            }

            // 绘制+号
            using (var pen = new Pen(borderColor, 2f))
            {
                var centerX = buttonRect.X + buttonRect.Width / 2;
                var centerY = buttonRect.Y + buttonRect.Height / 2;
                var crossSize = buttonRect.Width * 0.3f;
                
                // 横线
                g.DrawLine(pen, 
                    centerX - crossSize / 2, centerY,
                    centerX + crossSize / 2, centerY);
                // 竖线
                g.DrawLine(pen,
                    centerX, centerY - crossSize / 2,
                    centerX, centerY + crossSize / 2);
            }
        }

        /// <summary>
        /// 检测添加按钮点击
        /// </summary>
        public bool HitTestAddButton(PointF point)
        {
            if (!ShowAddButton || SourceNode == null || TargetNode == null || IsLoopReturn)
                return false;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);
            var midPoint = new PointF((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
            
            var buttonSize = IsAddButtonHovered ? AddButtonSize * 1.2f : AddButtonSize;
            var dx = point.X - midPoint.X;
            var dy = point.Y - midPoint.Y;
            var distanceSquared = dx * dx + dy * dy;
            
            return distanceSquared <= (buttonSize / 2) * (buttonSize / 2);
        }

        /// <summary>
        /// 绘制直线连接（带圆角转角）
        /// </summary>
        private void DrawStraightLine(Graphics g, PointF start, PointF end, Pen pen)
        {
            // 如果连接是垂直的，直接绘制直线
            if (Math.Abs(start.X - end.X) < 1)
            {
                g.DrawLine(pen, start, end);
                return;
            }

            // 水平连接：添加圆角转角
            var midY = (start.Y + end.Y) / 2;
            var verticalSpace = Math.Abs(end.Y - start.Y);
            
            if (verticalSpace > ArcLength * 2)
            {
                // 使用圆角路径
                using (var path = new GraphicsPath())
                {
                    // 从起点向下
                    path.AddLine(start, new PointF(start.X, start.Y + ArcLength));
                    
                    // 圆角转角
                    path.AddArc(start.X - ArcLength, start.Y + ArcLength - ArcLength, 
                        ArcLength * 2, ArcLength * 2, 90, 90);
                    
                    // 水平线
                    path.AddLine(new PointF(start.X + ArcLength, start.Y + ArcLength), 
                        new PointF(end.X - ArcLength, end.Y - ArcLength));
                    
                    // 圆角转角
                    path.AddArc(end.X - ArcLength, end.Y - ArcLength - ArcLength, 
                        ArcLength * 2, ArcLength * 2, 0, 90);
                    
                    // 到终点
                    path.AddLine(new PointF(end.X, end.Y - ArcLength), end);
                    
                    g.DrawPath(pen, path);
                }
            }
            else
            {
                // 距离太近，直接绘制直线
                g.DrawLine(pen, start, end);
            }
        }

        /// <summary>
        /// 绘制循环返回线（带圆角的曲线，参考Activepieces实现）
        /// </summary>
        private void DrawLoopReturnLine(Graphics g, PointF start, PointF end, Pen pen)
        {
            // 参考Activepieces的loop-return-edge实现
            // 使用多段圆弧和直线创建循环返回路径
            
            var horizontalLineLength = Math.Abs(start.X - end.X) - 2 * ArcLength;
            var verticalLineLength = Math.Abs(end.Y - start.Y);
            
            using (var path = new GraphicsPath())
            {
                // 从起点向上
                var lineStartY = start.Y - 7; // VERTICAL_SPACE_BETWEEN_STEP_AND_LINE
                path.AddLine(start.X, start.Y, start.X, lineStartY);
                
                // 左下方圆弧
                path.AddArc(start.X - ArcLength * 2, lineStartY, ArcLength * 2, ArcLength * 2, 180, 90);
                
                // 水平线（向左）
                path.AddLine(new PointF(start.X - ArcLength, lineStartY + ArcLength),
                    new PointF(start.X - horizontalLineLength - ArcLength, lineStartY + ArcLength));
                
                // 右上方圆弧
                path.AddArc(start.X - horizontalLineLength - ArcLength * 2, 
                    lineStartY + ArcLength - verticalLineLength, 
                    ArcLength * 2, ArcLength * 2, 270, 90);
                
                // 向上到循环起点
                path.AddLine(new PointF(start.X - horizontalLineLength / 2 - ArcLength, 
                    lineStartY + ArcLength - verticalLineLength),
                    new PointF(start.X - horizontalLineLength / 2 - ArcLength, 
                    start.Y + 7 + ArcLength / 2));
                
                // 向下到终点
                var endLineLength = 60 - 2 * 7 + 8; // VERTICAL_SPACE_BETWEEN_STEPS
                path.AddLine(new PointF(start.X - horizontalLineLength / 2 - ArcLength,
                    start.Y + 7 + ArcLength / 2),
                    new PointF(start.X - horizontalLineLength / 2 - ArcLength,
                    start.Y + 7 + ArcLength / 2 + endLineLength));
                
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// 获取连接点（节点边缘的中点）
        /// </summary>
        private PointF GetConnectionPoint(FlowNode node, bool isSource)
        {
            if (node == null) return PointF.Empty;

            var bounds = node.GetBounds();
            if (isSource)
            {
                // 源节点：右边缘中点
                return new PointF(bounds.Right, bounds.Y + bounds.Height / 2);
            }
            else
            {
                // 目标节点：左边缘中点
                return new PointF(bounds.Left, bounds.Y + bounds.Height / 2);
            }
        }

        /// <summary>
        /// 绘制箭头（Activepieces风格：更清晰的三角形）
        /// </summary>
        private void DrawArrow(Graphics g, PointF start, PointF end, Color color)
        {
            // 计算箭头方向
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = (float)Math.Sqrt(dx * dx + dy * dy);

            if (length < ArrowSize) return;

            // 归一化方向向量
            var nx = dx / length;
            var ny = dy / length;

            // 箭头尖端位置（稍微向内，避免重叠）
            var arrowTip = new PointF(
                end.X - nx * (TargetNode.Width / 2 + 5),
                end.Y - ny * (TargetNode.Height / 2 + 5)
            );

            // 箭头两边的点（Activepieces风格：更小的角度）
            var arrowAngle = 0.5f; // 箭头角度（弧度）
            var sinAngle = (float)Math.Sin(arrowAngle);
            var arrowLeft = new PointF(
                arrowTip.X - ArrowSize * nx - ArrowSize * sinAngle * ny,
                arrowTip.Y - ArrowSize * ny + ArrowSize * sinAngle * nx
            );
            var arrowRight = new PointF(
                arrowTip.X - ArrowSize * nx + ArrowSize * sinAngle * ny,
                arrowTip.Y - ArrowSize * ny - ArrowSize * sinAngle * nx
            );

            // 绘制箭头（填充三角形）
            using (var brush = new SolidBrush(color))
            {
                var points = new[] { arrowTip, arrowLeft, arrowRight };
                g.FillPolygon(brush, points);
            }
        }

        public override bool HitTest(PointF point)
        {
            if (SourceNode == null || TargetNode == null) return false;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty) return false;

            // 检查点是否在连接线附近（容差范围内）
            const float tolerance = 5f;
            return IsPointNearLine(point, startPoint, endPoint, tolerance);
        }

        /// <summary>
        /// 检查点是否在直线附近
        /// </summary>
        private bool IsPointNearLine(PointF point, PointF lineStart, PointF lineEnd, float tolerance)
        {
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var lengthSquared = dx * dx + dy * dy;

            if (lengthSquared < 0.01f) return false;

            // 计算点到直线的距离
            var t = Math.Max(0, Math.Min(1, 
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared));
            
            var projection = new PointF(
                lineStart.X + t * dx,
                lineStart.Y + t * dy
            );

            var distanceSquared = (point.X - projection.X) * (point.X - projection.X) +
                                 (point.Y - projection.Y) * (point.Y - projection.Y);

            return distanceSquared <= tolerance * tolerance;
        }

        public override RectangleF GetBounds()
        {
            if (SourceNode == null || TargetNode == null)
                return RectangleF.Empty;

            var startPoint = GetConnectionPoint(SourceNode, true);
            var endPoint = GetConnectionPoint(TargetNode, false);

            if (startPoint.IsEmpty || endPoint.IsEmpty)
                return RectangleF.Empty;

            var minX = Math.Min(startPoint.X, endPoint.X);
            var minY = Math.Min(startPoint.Y, endPoint.Y);
            var maxX = Math.Max(startPoint.X, endPoint.X);
            var maxY = Math.Max(startPoint.Y, endPoint.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 更新连接（当节点移动时调用）
        /// </summary>
        public void Update()
        {
            // 连接线位置由源节点和目标节点决定，不需要单独的位置属性
            // 但可以触发重绘
        }
    }
}
