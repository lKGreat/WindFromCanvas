using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Events;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 端口状态
    /// </summary>
    public enum PortState
    {
        Normal,    // 正常状态
        Hovered,    // 悬停状态
        Active,     // 激活状态（正在连接）
        Connected   // 已连接状态
    }

    /// <summary>
    /// 流程节点基类
    /// </summary>
    public abstract class FlowNode : CanvasObject
    {
        /// <summary>
        /// 节点数据
        /// </summary>
        public FlowNodeData Data { get; set; }

        /// <summary>
        /// 节点宽度（Activepieces标准：232px）
        /// </summary>
        public virtual float Width { get; set; } = 232f;

        /// <summary>
        /// 节点高度（Activepieces标准：60px）
        /// </summary>
        public virtual float Height { get; set; } = 60f;

        /// <summary>
        /// 圆角半径（Activepieces标准：4px）
        /// </summary>
        public virtual float CornerRadius { get; set; } = 4f;

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否悬停
        /// </summary>
        public bool IsHovered { get; set; }

        /// <summary>
        /// 是否跳过执行
        /// </summary>
        public bool IsSkipped
        {
            get => Data?.Skip ?? false;
            set
            {
                if (Data != null)
                {
                    Data.Skip = value;
                }
            }
        }

        /// <summary>
        /// 验证错误信息
        /// </summary>
        public string ValidationError { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public virtual Color BackgroundColor { get; set; } = Color.FromArgb(255, 255, 255);

        /// <summary>
        /// 边框颜色
        /// </summary>
        public virtual Color BorderColor { get; set; } = Color.FromArgb(200, 200, 200);

        /// <summary>
        /// 选中时的边框颜色（Activepieces蓝色：#3B82F6）
        /// </summary>
        public virtual Color SelectedBorderColor { get; set; } = Color.FromArgb(59, 130, 246);

        /// <summary>
        /// 悬停时的边框颜色（Activepieces ring颜色）
        /// </summary>
        public virtual Color HoverBorderColor { get; set; } = Color.FromArgb(148, 163, 184);

        /// <summary>
        /// 边框宽度（默认1px，选中时2px）
        /// </summary>
        public virtual float BorderWidth { get; set; } = 1f;

        /// <summary>
        /// 选中时的边框宽度
        /// </summary>
        public virtual float SelectedBorderWidth { get; set; } = 2f;

        /// <summary>
        /// 是否启用阴影效果
        /// </summary>
        public virtual bool EnableShadow { get; set; } = true;

        /// <summary>
        /// 阴影偏移量
        /// </summary>
        public virtual PointF ShadowOffset { get; set; } = new PointF(0, 2);

        /// <summary>
        /// 阴影模糊半径
        /// </summary>
        public virtual float ShadowBlur { get; set; } = 4f;

        /// <summary>
        /// 阴影颜色
        /// </summary>
        public virtual Color ShadowColor { get; set; } = Color.FromArgb(30, 0, 0, 0);

        /// <summary>
        /// 选中时的阴影颜色（更深）
        /// </summary>
        public virtual Color SelectedShadowColor { get; set; } = Color.FromArgb(60, 0, 0, 0);

        /// <summary>
        /// 是否启用渐变背景
        /// </summary>
        public virtual bool EnableGradient { get; set; } = false;

        /// <summary>
        /// 渐变起始颜色
        /// </summary>
        public virtual Color GradientStartColor { get; set; }

        /// <summary>
        /// 渐变结束颜色
        /// </summary>
        public virtual Color GradientEndColor { get; set; }

        /// <summary>
        /// 渐变方向（0=水平，90=垂直）
        /// </summary>
        public virtual float GradientAngle { get; set; } = 90f;

        /// <summary>
        /// 文本颜色
        /// </summary>
        public virtual Color TextColor { get; set; } = Color.Black;

        /// <summary>
        /// 输入端口列表（位置）
        /// </summary>
        public virtual List<PointF> InputPorts { get; protected set; }

        /// <summary>
        /// 输出端口列表（位置）
        /// </summary>
        public virtual List<PointF> OutputPorts { get; protected set; }

        /// <summary>
        /// 端口大小
        /// </summary>
        public virtual float PortSize { get; set; } = 8f;

        /// <summary>
        /// 端口热区大小（用于点击检测，大于实际绘制大小）
        /// </summary>
        public virtual float PortHitSize { get; set; } = 12f;

        /// <summary>
        /// 当前悬停的输入端口索引（-1表示无）
        /// </summary>
        public int HoveredInputPortIndex { get; set; } = -1;

        /// <summary>
        /// 当前悬停的输出端口索引（-1表示无）
        /// </summary>
        public int HoveredOutputPortIndex { get; set; } = -1;

        /// <summary>
        /// 激活的输入端口索引（正在连接时）
        /// </summary>
        public int ActiveInputPortIndex { get; set; } = -1;

        /// <summary>
        /// 激活的输出端口索引（正在连接时）
        /// </summary>
        public int ActiveOutputPortIndex { get; set; } = -1;

        /// <summary>
        /// 已连接的输入端口索引集合
        /// </summary>
        public HashSet<int> ConnectedInputPorts { get; set; } = new HashSet<int>();

        /// <summary>
        /// 已连接的输出端口索引集合
        /// </summary>
        public HashSet<int> ConnectedOutputPorts { get; set; } = new HashSet<int>();

        protected FlowNode()
        {
            Draggable = true;
            ZIndex = 10;
            InputPorts = new List<PointF>();
            OutputPorts = new List<PointF>();
        }

        public FlowNode(FlowNodeData data) : this()
        {
            Data = data;
            if (data != null)
            {
                X = data.Position.X;
                Y = data.Position.Y;
            }
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            var bounds = GetBounds();
            var rect = new RectangleF(X, Y, Width, Height);

            // 创建圆角矩形路径
            using (var path = CreateRoundedRectangle(rect, CornerRadius))
            {
                // 绘制阴影（如果启用）
                if (EnableShadow)
                {
                    DrawShadow(g, path);
                }

                // 填充背景（渐变或纯色）
                if (EnableGradient && GradientStartColor != Color.Empty && GradientEndColor != Color.Empty)
                {
                    DrawGradientBackground(g, path, rect);
                }
                else
                {
                    using (var brush = new SolidBrush(BackgroundColor))
                    {
                        g.FillPath(brush, path);
                    }
                }

                // 绘制边框
                var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
                var borderWidth = IsSelected ? SelectedBorderWidth : BorderWidth;
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 绘制节点图标（占位）
            DrawIcon(g, rect);

            // 绘制节点名称
            DrawText(g, rect);

            // 绘制端口
            DrawPorts(g);

            // 绘制验证错误图标
            if (!string.IsNullOrEmpty(ValidationError) || (Data != null && !Data.Valid))
            {
                DrawValidationError(g, rect);
            }

            // 绘制状态指示器（右上角）
            if (Data != null && Data.Status != NodeStatus.None)
            {
                DrawStatusIndicator(g, rect);
            }
        }

        /// <summary>
        /// 绘制状态指示器（右上角）
        /// </summary>
        protected virtual void DrawStatusIndicator(Graphics g, RectangleF rect)
        {
            if (Data == null || Data.Status == NodeStatus.None)
                return;

            var statusIconSize = 16f;
            var statusRect = new RectangleF(
                rect.Right - statusIconSize - 5,
                rect.Top + 5,
                statusIconSize,
                statusIconSize
            );

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            switch (Data.Status)
            {
                case NodeStatus.Running:
                    DrawRunningIndicator(g, statusRect);
                    break;
                case NodeStatus.Success:
                    DrawSuccessIndicator(g, statusRect);
                    break;
                case NodeStatus.Failed:
                    DrawFailedIndicator(g, statusRect);
                    break;
                case NodeStatus.Skipped:
                    DrawSkippedIndicator(g, statusRect);
                    break;
            }
        }

        /// <summary>
        /// 绘制运行中指示器（蓝色旋转圆圈）
        /// </summary>
        protected virtual void DrawRunningIndicator(Graphics g, RectangleF rect)
        {
            // 绘制旋转的圆圈（使用动画角度，这里先绘制静态版本）
            using (var pen = new Pen(Color.FromArgb(59, 130, 246), 2f))
            {
                // 绘制圆弧（模拟旋转）
                var startAngle = (float)(System.DateTime.Now.Millisecond % 1000) / 1000f * 360f;
                g.DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, 270);
            }
        }

        /// <summary>
        /// 绘制成功指示器（绿色对勾）
        /// </summary>
        protected virtual void DrawSuccessIndicator(Graphics g, RectangleF rect)
        {
            using (var brush = new SolidBrush(Color.FromArgb(16, 185, 129))) // Activepieces绿色
            {
                g.FillEllipse(brush, rect);
            }
            using (var pen = new Pen(Color.White, 2f))
            {
                // 绘制对勾
                var centerX = rect.X + rect.Width / 2;
                var centerY = rect.Y + rect.Height / 2;
                var checkSize = rect.Width * 0.4f;
                g.DrawLine(pen, 
                    centerX - checkSize * 0.3f, centerY,
                    centerX - checkSize * 0.1f, centerY + checkSize * 0.3f);
                g.DrawLine(pen,
                    centerX - checkSize * 0.1f, centerY + checkSize * 0.3f,
                    centerX + checkSize * 0.4f, centerY - checkSize * 0.2f);
            }
        }

        /// <summary>
        /// 绘制失败指示器（红色叉号）
        /// </summary>
        protected virtual void DrawFailedIndicator(Graphics g, RectangleF rect)
        {
            using (var brush = new SolidBrush(Color.FromArgb(239, 68, 68))) // Activepieces红色
            {
                g.FillEllipse(brush, rect);
            }
            using (var pen = new Pen(Color.White, 2f))
            {
                // 绘制叉号
                var margin = rect.Width * 0.25f;
                g.DrawLine(pen, 
                    rect.X + margin, rect.Y + margin,
                    rect.Right - margin, rect.Bottom - margin);
                g.DrawLine(pen,
                    rect.Right - margin, rect.Y + margin,
                    rect.X + margin, rect.Bottom - margin);
            }
        }

        /// <summary>
        /// 绘制跳过指示器（灰色斜杠）
        /// </summary>
        protected virtual void DrawSkippedIndicator(Graphics g, RectangleF rect)
        {
            using (var brush = new SolidBrush(Color.FromArgb(148, 163, 184))) // Activepieces灰色
            {
                g.FillEllipse(brush, rect);
            }
            using (var pen = new Pen(Color.White, 2f))
            {
                // 绘制斜杠
                var margin = rect.Width * 0.25f;
                g.DrawLine(pen,
                    rect.X + margin, rect.Y + margin,
                    rect.Right - margin, rect.Bottom - margin);
            }
        }

        /// <summary>
        /// 绘制阴影效果（使用路径偏移和透明度模拟）
        /// </summary>
        protected virtual void DrawShadow(Graphics g, GraphicsPath path)
        {
            var shadowColor = IsSelected ? SelectedShadowColor : ShadowColor;
            
            // 使用GraphicsPath创建阴影效果
            // 由于GDI+不支持真正的模糊阴影，我们使用多层半透明路径来模拟
            using (var shadowPath = (GraphicsPath)path.Clone())
            {
                // 应用偏移变换
                using (var matrix = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, ShadowOffset.X, ShadowOffset.Y))
                {
                    shadowPath.Transform(matrix);
                }
                
                // 绘制多层阴影以模拟模糊效果
                for (int i = 0; i < 3; i++)
                {
                    var alpha = (int)(shadowColor.A * (1.0f - i * 0.3f));
                    if (alpha <= 0) break;
                    
                    var offset = i * 0.8f;
                    using (var tempPath = (GraphicsPath)shadowPath.Clone())
                    {
                        using (var matrix = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, offset, offset))
                        {
                            tempPath.Transform(matrix);
                        }
                        
                        using (var brush = new SolidBrush(Color.FromArgb(alpha, shadowColor.R, shadowColor.G, shadowColor.B)))
                        {
                            g.FillPath(brush, tempPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制渐变背景
        /// </summary>
        protected virtual void DrawGradientBackground(Graphics g, GraphicsPath path, RectangleF rect)
        {
            using (var brush = new LinearGradientBrush(rect, GradientStartColor, GradientEndColor, GradientAngle))
            {
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 绘制验证错误图标
        /// </summary>
        protected virtual void DrawValidationError(Graphics g, RectangleF rect)
        {
            var errorIconSize = 16f;
            var errorIconRect = new RectangleF(
                rect.Right - errorIconSize - 5,
                rect.Top + 5,
                errorIconSize,
                errorIconSize
            );

            // 绘制红色错误图标
            using (var brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush, errorIconRect);
            }
            using (var pen = new Pen(Color.White, 2))
            {
                g.DrawString("!", SystemFonts.DefaultFont, new SolidBrush(Color.White), 
                    errorIconRect.X + 4, errorIconRect.Y + 2);
            }
        }

        /// <summary>
        /// 绘制端口
        /// </summary>
        protected virtual void DrawPorts(Graphics g)
        {
            // 默认：输入端口在左侧，输出端口在右侧
            var bounds = GetBounds();
            
            // 输入端口（左侧中点）
            if (InputPorts.Count == 0)
            {
                InputPorts.Add(new PointF(bounds.Left, bounds.Y + bounds.Height / 2));
            }
            
            // 输出端口（右侧中点）
            if (OutputPorts.Count == 0)
            {
                OutputPorts.Add(new PointF(bounds.Right, bounds.Y + bounds.Height / 2));
            }

            // 绘制输入端口
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var portState = GetPortState(i, true);
                DrawPort(g, InputPorts[i], true, portState);
            }

            // 绘制输出端口
            for (int i = 0; i < OutputPorts.Count; i++)
            {
                var portState = GetPortState(i, false);
                DrawPort(g, OutputPorts[i], false, portState);
            }
        }

        /// <summary>
        /// 获取端口状态
        /// </summary>
        protected virtual PortState GetPortState(int portIndex, bool isInput)
        {
            if (isInput)
            {
                if (ActiveInputPortIndex == portIndex)
                    return PortState.Active;
                if (HoveredInputPortIndex == portIndex)
                    return PortState.Hovered;
                if (ConnectedInputPorts.Contains(portIndex))
                    return PortState.Connected;
            }
            else
            {
                if (ActiveOutputPortIndex == portIndex)
                    return PortState.Active;
                if (HoveredOutputPortIndex == portIndex)
                    return PortState.Hovered;
                if (ConnectedOutputPorts.Contains(portIndex))
                    return PortState.Connected;
            }
            return PortState.Normal;
        }

        /// <summary>
        /// 绘制单个端口（根据状态绘制不同效果）
        /// </summary>
        protected virtual void DrawPort(Graphics g, PointF port, bool isInput, PortState state)
        {
            float size = PortSize;
            float scale = 1f;
            Color fillColor = Color.White;
            Color borderColor = BorderColor;
            float borderWidth = 1.5f;
            bool showGlow = false;

            // 根据状态设置不同的视觉效果
            switch (state)
            {
                case PortState.Normal:
                    fillColor = Color.White;
                    borderColor = BorderColor;
                    borderWidth = 1.5f;
                    break;

                case PortState.Hovered:
                    fillColor = Color.White;
                    borderColor = Color.FromArgb(59, 130, 246); // 蓝色
                    borderWidth = 2f;
                    scale = 1.2f; // 放大1.2倍
                    showGlow = true;
                    break;

                case PortState.Active:
                    fillColor = Color.FromArgb(59, 130, 246); // 蓝色填充
                    borderColor = Color.FromArgb(59, 130, 246);
                    borderWidth = 2f;
                    scale = 1.3f; // 放大1.3倍
                    showGlow = true;
                    break;

                case PortState.Connected:
                    fillColor = Color.White;
                    borderColor = Color.FromArgb(16, 185, 129); // 绿色边框
                    borderWidth = 2f;
                    break;
            }

            size *= scale;
            var rect = new RectangleF(
                port.X - size / 2,
                port.Y - size / 2,
                size,
                size
            );

            // 绘制外发光效果（悬停/激活时）
            if (showGlow)
            {
                var glowRect = new RectangleF(
                    port.X - size / 2 - 3,
                    port.Y - size / 2 - 3,
                    size + 6,
                    size + 6
                );
                using (var glowBrush = new SolidBrush(Color.FromArgb(30, borderColor.R, borderColor.G, borderColor.B)))
                {
                    g.FillEllipse(glowBrush, glowRect);
                }
            }

            // 绘制端口填充
            using (var brush = new SolidBrush(fillColor))
            {
                g.FillEllipse(brush, rect);
            }

            // 绘制端口边框
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.DrawEllipse(pen, rect);
            }

            // 已连接状态：绘制内部小点
            if (state == PortState.Connected)
            {
                var dotSize = size * 0.4f;
                var dotRect = new RectangleF(
                    port.X - dotSize / 2,
                    port.Y - dotSize / 2,
                    dotSize,
                    dotSize
                );
                using (var dotBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(dotBrush, dotRect);
                }
            }
        }

        /// <summary>
        /// 检测端口点击（使用热区大小）
        /// </summary>
        public virtual PointF? HitTestPort(PointF point, bool isOutput)
        {
            var ports = isOutput ? OutputPorts : InputPorts;
            var hitRadius = PortHitSize / 2f;
            var hitRadiusSquared = hitRadius * hitRadius;

            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                var dx = point.X - port.X;
                var dy = point.Y - port.Y;
                if (dx * dx + dy * dy <= hitRadiusSquared)
                {
                    return port;
                }
            }
            return null;
        }

        /// <summary>
        /// 检测端口点击并返回端口索引
        /// </summary>
        public virtual int? HitTestPortIndex(PointF point, bool isOutput)
        {
            var ports = isOutput ? OutputPorts : InputPorts;
            var hitRadius = PortHitSize / 2f;
            var hitRadiusSquared = hitRadius * hitRadius;

            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                var dx = point.X - port.X;
                var dy = point.Y - port.Y;
                if (dx * dx + dy * dy <= hitRadiusSquared)
                {
                    return i;
                }
            }
            return null;
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        protected GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// 绘制图标（子类可重写）
        /// </summary>
        protected virtual void DrawIcon(Graphics g, RectangleF rect)
        {
            var iconSize = 24f; // Activepieces标准图标尺寸
            var iconRect = new RectangleF(
                rect.X + 10,
                rect.Y + (rect.Height - iconSize) / 2,
                iconSize,
                iconSize
            );

            Image icon = null;

            // 优先使用Data中指定的图标
            if (Data != null && !string.IsNullOrEmpty(Data.IconPath))
            {
                icon = Utils.IconCache.Instance.GetIcon(Data.IconPath, Data.IconType, new Size((int)iconSize, (int)iconSize));
            }

            // 如果没有指定图标，使用默认图标
            if (icon == null && Data != null)
            {
                icon = Utils.IconCache.Instance.GetDefaultIcon(Data.Type.ToString(), new Size((int)iconSize, (int)iconSize));
            }

            // 如果还是没有，绘制占位图标
            if (icon == null)
            {
                DrawPlaceholderIcon(g, iconRect);
            }
            else
            {
                // 绘制图标（带圆角边框和背景，类似Activepieces）
                DrawIconWithBackground(g, icon, iconRect);
            }
        }

        /// <summary>
        /// 绘制带背景的图标（Activepieces风格）
        /// </summary>
        protected virtual void DrawIconWithBackground(Graphics g, Image icon, RectangleF iconRect)
        {
            // 绘制图标背景（圆角矩形，浅灰色）
            using (var bgPath = CreateRoundedRectangle(iconRect, 4f))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPath(bgBrush, bgPath);
                }
                using (var bgPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawPath(bgPen, bgPath);
                }
            }

            // 绘制图标（居中）
            var iconDrawRect = new RectangleF(
                iconRect.X + (iconRect.Width - icon.Width) / 2,
                iconRect.Y + (iconRect.Height - icon.Height) / 2,
                icon.Width,
                icon.Height
            );
            g.DrawImage(icon, iconDrawRect);
        }

        /// <summary>
        /// 绘制占位图标
        /// </summary>
        protected virtual void DrawPlaceholderIcon(Graphics g, RectangleF iconRect)
        {
            // 绘制占位矩形
            using (var bgPath = CreateRoundedRectangle(iconRect, 4f))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPath(bgBrush, bgPath);
                }
                using (var bgPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawPath(bgPen, bgPath);
                }
            }
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        protected virtual void DrawText(Graphics g, RectangleF rect)
        {
            var text = Data?.DisplayName ?? Data?.Name ?? "节点";
            var textRect = new RectangleF(
                rect.X + 40, // 为图标留出空间
                rect.Y,
                rect.Width - 50,
                rect.Height
            );

            using (var brush = new SolidBrush(TextColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            })
            {
                g.DrawString(text, SystemFonts.DefaultFont, brush, textRect, sf);
            }
        }

        public override bool HitTest(PointF point)
        {
            var bounds = GetBounds();
            return bounds.Contains(point);
        }

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        /// <summary>
        /// 更新位置（同步到Data）
        /// </summary>
        public void UpdatePosition()
        {
            if (Data != null)
            {
                Data.Position = new PointF(X, Y);
            }
        }

        /// <summary>
        /// 从Data更新位置
        /// </summary>
        public void UpdateFromData()
        {
            if (Data != null)
            {
                X = Data.Position.X;
                Y = Data.Position.Y;
            }
        }
    }
}
