using System;
using System.Collections.Generic;
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
        /// 连接线颜色（从主题获取）
        /// </summary>
        public Color LineColor 
        { 
            get => Themes.ThemeManager.Instance.CurrentTheme.ConnectionLine;
        }

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
        /// 悬停时的连接线颜色（从主题获取）
        /// </summary>
        public Color HoverLineColor 
        { 
            get => Themes.ThemeManager.Instance.CurrentTheme.ConnectionHover;
        }

        /// <summary>
        /// 选中时的连接线颜色（从主题获取）
        /// </summary>
        public Color SelectedLineColor 
        { 
            get => Themes.ThemeManager.Instance.CurrentTheme.ConnectionSelected;
        }

        /// <summary>
        /// 是否启用连接线渐变
        /// </summary>
        public bool EnableGradient { get; set; } = true;

        /// <summary>
        /// 是否启用连接线阴影
        /// </summary>
        public bool EnableShadow { get; set; } = true;

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

        /// <summary>
        /// 连线流动动画偏移（用于虚线流动效果）
        /// </summary>
        public float FlowAnimationOffset { get; set; }

        /// <summary>
        /// 连接建立动画进度（0-1，用于曲线生长效果）
        /// </summary>
        public float BuildProgress { get; set; } = 1f;

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
            
            // 绘制阴影（如果启用）
            if (EnableShadow && !IsDashed)
            {
                DrawConnectionShadow(g, startPoint, endPoint, color);
            }
            
            // 创建画笔
            Pen pen;
            if (EnableGradient && !IsDashed && !IsLoopReturn)
            {
                // 使用渐变画笔
                pen = CreateGradientPen(startPoint, endPoint, color);
            }
            else
            {
                pen = new Pen(color, LineWidth);
            }
            
            using (pen)
            {
                pen.EndCap = LineCap.Round;
                pen.StartCap = LineCap.Round;
                
                // 如果是虚线，设置虚线样式
                if (IsDashed)
                {
                    pen.DashStyle = DashStyle.Dash;
                    pen.DashPattern = new float[] { 4f, 4f };
                }
                else if (FlowAnimationOffset > 0)
                {
                    // 连线流动动画：虚线流动效果
                    pen.DashStyle = DashStyle.Dash;
                    pen.DashPattern = new float[] { 8f, 4f };
                    pen.DashOffset = FlowAnimationOffset;
                }

                // 如果正在建立连接，使用生长动画
                if (BuildProgress < 1f && !IsLoopReturn)
                {
                    DrawConnectionBuild(g, startPoint, endPoint, pen);
                }
                else if (IsLoopReturn)
                {
                    // 循环返回连接：绘制带圆角的曲线
                    DrawLoopReturnLine(g, startPoint, endPoint, pen);
                }
                else
                {
                    // 普通连接：绘制贝塞尔曲线
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
        /// 绘制连接建立动画（曲线从起点生长到终点）
        /// </summary>
        private void DrawConnectionBuild(Graphics g, PointF start, PointF end, Pen pen)
        {
            // 计算贝塞尔曲线控制点
            var dx = Math.Abs(end.X - start.X);
            var controlOffset = Math.Max(50f, dx * 0.4f);
            var cp1 = new PointF(start.X + controlOffset, start.Y);
            var cp2 = new PointF(end.X - controlOffset, end.Y);

            // 根据 BuildProgress 计算当前终点
            var currentEnd = GetBezierPoint(start, cp1, cp2, end, BuildProgress);

            // 绘制部分曲线
            using (var path = new GraphicsPath())
            {
                path.AddBezier(start, cp1, cp2, currentEnd);
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// 绘制贝塞尔曲线连接（平滑美观的曲线）
        /// </summary>
        private void DrawStraightLine(Graphics g, PointF start, PointF end, Pen pen)
        {
            // 如果连接是垂直的，直接绘制直线
            if (Math.Abs(start.X - end.X) < 1)
            {
                g.DrawLine(pen, start, end);
                return;
            }

            // 使用三次贝塞尔曲线绘制平滑连接
            using (var path = new GraphicsPath())
            {
                // 计算控制点偏移量：基于水平距离，最小50px
                var dx = Math.Abs(end.X - start.X);
                var controlOffset = Math.Max(50f, dx * 0.4f);
                
                // 控制点1：从起点向右延伸
                var cp1 = new PointF(start.X + controlOffset, start.Y);
                
                // 控制点2：从终点向左延伸
                var cp2 = new PointF(end.X - controlOffset, end.Y);
                
                // 添加贝塞尔曲线
                path.AddBezier(start, cp1, cp2, end);
                
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// 需要避让的节点列表（用于循环返回连接）
        /// </summary>
        public List<FlowNode> AvoidanceNodes { get; set; } = new List<FlowNode>();

        /// <summary>
        /// 绘制循环返回线（带圆角的曲线，参考Activepieces实现，包含避让算法）
        /// </summary>
        private void DrawLoopReturnLine(Graphics g, PointF start, PointF end, Pen pen)
        {
            // 参考Activepieces的loop-return-edge实现
            // 使用多段圆弧和直线创建循环返回路径，自动避让中间节点
            
            var horizontalLineLength = Math.Abs(start.X - end.X) - 2 * ArcLength;
            var verticalLineLength = Math.Abs(end.Y - start.Y);
            
            // 计算避让高度（如果有需要避让的节点）
            float avoidanceHeight = 0f;
            if (AvoidanceNodes != null && AvoidanceNodes.Count > 0)
            {
                foreach (var node in AvoidanceNodes)
                {
                    var bounds = node.GetBounds();
                    // 计算节点最高点
                    var nodeTop = bounds.Top;
                    var currentAvoidance = start.Y - nodeTop + 20f; // 额外20px间距
                    if (currentAvoidance > avoidanceHeight)
                    {
                        avoidanceHeight = currentAvoidance;
                    }
                }
            }
            
            // 最小避让高度
            var minAvoidanceHeight = 50f;
            avoidanceHeight = Math.Max(avoidanceHeight, minAvoidanceHeight);
            
            using (var path = new GraphicsPath())
            {
                // 从起点向上（避让高度）
                var lineStartY = start.Y - avoidanceHeight;
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
        protected PointF GetConnectionPoint(FlowNode node, bool isSource)
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
        /// 计算贝塞尔曲线在参数t处的点
        /// </summary>
        private PointF GetBezierPoint(PointF p0, PointF p1, PointF p2, PointF p3, float t)
        {
            var u = 1f - t;
            var tt = t * t;
            var uu = u * u;
            var uuu = uu * u;
            var ttt = tt * t;

            var x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
            var y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;

            return new PointF(x, y);
        }

        /// <summary>
        /// 计算贝塞尔曲线在参数t处的切线方向
        /// </summary>
        private PointF GetBezierTangent(PointF p0, PointF p1, PointF p2, PointF p3, float t)
        {
            var u = 1f - t;
            var uu = u * u;
            var tt = t * t;

            var dx = 3 * uu * (p1.X - p0.X) + 6 * u * t * (p2.X - p1.X) + 3 * tt * (p3.X - p2.X);
            var dy = 3 * uu * (p1.Y - p0.Y) + 6 * u * t * (p2.Y - p1.Y) + 3 * tt * (p3.Y - p2.Y);

            var length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001f) return new PointF(1, 0);

            return new PointF(dx / length, dy / length);
        }

        /// <summary>
        /// 绘制箭头（贴合贝塞尔曲线末端切线方向）
        /// </summary>
        protected void DrawArrow(Graphics g, PointF start, PointF end, Color color)
        {
            // 计算箭头方向（使用贝塞尔曲线切线）
            PointF tangent;
            PointF arrowTip;

            // 如果不是循环返回连接，使用贝塞尔曲线计算切线
            if (!IsLoopReturn)
            {
                var dx = Math.Abs(end.X - start.X);
                var controlOffset = Math.Max(50f, dx * 0.4f);
                var cp1 = new PointF(start.X + controlOffset, start.Y);
                var cp2 = new PointF(end.X - controlOffset, end.Y);

                // 在曲线末端（t=1）计算切线
                tangent = GetBezierTangent(start, cp1, cp2, end, 1f);
                
                // 计算箭头尖端位置（稍微向内，避免重叠）
                var tipOffset = TargetNode.Width / 2 + 5;
                arrowTip = new PointF(
                    end.X - tangent.X * tipOffset,
                    end.Y - tangent.Y * tipOffset
                );
            }
            else
            {
                // 循环返回连接使用直线方向
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var length = (float)Math.Sqrt(dx * dx + dy * dy);

                if (length < ArrowSize) return;

                var nx = dx / length;
                var ny = dy / length;
                tangent = new PointF(nx, ny);

                arrowTip = new PointF(
                    end.X - nx * (TargetNode.Width / 2 + 5),
                    end.Y - ny * (TargetNode.Height / 2 + 5)
                );
            }

            // 箭头两边的点（Activepieces风格：更小的角度）
            var arrowAngle = 0.5f; // 箭头角度（弧度）
            var sinAngle = (float)Math.Sin(arrowAngle);
            var perpX = -tangent.Y; // 垂直向量
            var perpY = tangent.X;

            var arrowLeft = new PointF(
                arrowTip.X - ArrowSize * tangent.X - ArrowSize * sinAngle * perpX,
                arrowTip.Y - ArrowSize * tangent.Y - ArrowSize * sinAngle * perpY
            );
            var arrowRight = new PointF(
                arrowTip.X - ArrowSize * tangent.X + ArrowSize * sinAngle * perpX,
                arrowTip.Y - ArrowSize * tangent.Y + ArrowSize * sinAngle * perpY
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
        /// 检查点是否在贝塞尔曲线附近
        /// </summary>
        private bool IsPointNearLine(PointF point, PointF lineStart, PointF lineEnd, float tolerance)
        {
            if (IsLoopReturn)
            {
                // 循环返回连接使用简单的直线检测
                return IsPointNearStraightLine(point, lineStart, lineEnd, tolerance);
            }

            // 计算贝塞尔曲线控制点
            var dx = Math.Abs(lineEnd.X - lineStart.X);
            var controlOffset = Math.Max(50f, dx * 0.4f);
            var cp1 = new PointF(lineStart.X + controlOffset, lineStart.Y);
            var cp2 = new PointF(lineEnd.X - controlOffset, lineEnd.Y);

            // 在曲线上采样多个点，检查最近距离
            float minDistanceSquared = float.MaxValue;
            const int samples = 20; // 采样点数

            for (int i = 0; i <= samples; i++)
            {
                var t = i / (float)samples;
                var curvePoint = GetBezierPoint(lineStart, cp1, cp2, lineEnd, t);
                
                var dx2 = point.X - curvePoint.X;
                var dy2 = point.Y - curvePoint.Y;
                var distSq = dx2 * dx2 + dy2 * dy2;
                
                if (distSq < minDistanceSquared)
                {
                    minDistanceSquared = distSq;
                }
            }

            return minDistanceSquared <= tolerance * tolerance;
        }

        /// <summary>
        /// 检查点是否在直线附近（用于循环返回连接）
        /// </summary>
        private bool IsPointNearStraightLine(PointF point, PointF lineStart, PointF lineEnd, float tolerance)
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
        /// 绘制连接线阴影
        /// </summary>
        private void DrawConnectionShadow(Graphics g, PointF start, PointF end, Color baseColor)
        {
            var shadowColor = Themes.ThemeManager.Instance.CurrentTheme.ConnectionShadow;
            var shadowOffset = 2f;
            
            using (var shadowPath = new GraphicsPath())
            {
                // 计算贝塞尔曲线控制点
                var dx = Math.Abs(end.X - start.X);
                var controlOffset = Math.Max(50f, dx * 0.4f);
                var cp1 = new PointF(start.X + controlOffset, start.Y);
                var cp2 = new PointF(end.X - controlOffset, end.Y);
                
                shadowPath.AddBezier(
                    new PointF(start.X + shadowOffset, start.Y + shadowOffset),
                    new PointF(cp1.X + shadowOffset, cp1.Y + shadowOffset),
                    new PointF(cp2.X + shadowOffset, cp2.Y + shadowOffset),
                    new PointF(end.X + shadowOffset, end.Y + shadowOffset)
                );
                
                using (var shadowPen = new Pen(shadowColor, LineWidth + 2f))
                {
                    shadowPen.LineJoin = LineJoin.Round;
                    g.DrawPath(shadowPen, shadowPath);
                }
            }
        }

        /// <summary>
        /// 创建渐变画笔（从源节点颜色渐变到目标节点颜色）
        /// </summary>
        private Pen CreateGradientPen(PointF start, PointF end, Color baseColor)
        {
            // 创建线性渐变画笔
            var gradientRect = new RectangleF(
                Math.Min(start.X, end.X) - 50,
                Math.Min(start.Y, end.Y) - 10,
                Math.Abs(end.X - start.X) + 100,
                Math.Abs(end.Y - start.Y) + 20
            );
            
            // 源节点颜色（稍微浅一点）
            var sourceColor = Color.FromArgb(
                Math.Min(255, baseColor.R + 20),
                Math.Min(255, baseColor.G + 20),
                Math.Min(255, baseColor.B + 20)
            );
            
            // 目标节点颜色（稍微深一点）
            var targetColor = Color.FromArgb(
                Math.Max(0, baseColor.R - 20),
                Math.Max(0, baseColor.G - 20),
                Math.Max(0, baseColor.B - 20)
            );
            
            var brush = new LinearGradientBrush(gradientRect, sourceColor, targetColor, 0f);
            return new Pen(brush, LineWidth);
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
