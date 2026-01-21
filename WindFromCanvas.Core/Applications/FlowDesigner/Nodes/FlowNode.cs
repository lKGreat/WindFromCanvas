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
    /// 流程节点基类
    /// </summary>
    public abstract class FlowNode : CanvasObject
    {
        /// <summary>
        /// 节点数据
        /// </summary>
        public FlowNodeData Data { get; set; }

        /// <summary>
        /// 节点宽度
        /// </summary>
        public virtual float Width { get; set; } = 150f;

        /// <summary>
        /// 节点高度
        /// </summary>
        public virtual float Height { get; set; } = 60f;

        /// <summary>
        /// 圆角半径
        /// </summary>
        public virtual float CornerRadius { get; set; } = 8f;

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否悬停
        /// </summary>
        public bool IsHovered { get; set; }

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
        /// 选中时的边框颜色
        /// </summary>
        public virtual Color SelectedBorderColor { get; set; } = Color.FromArgb(0, 120, 215);

        /// <summary>
        /// 悬停时的边框颜色
        /// </summary>
        public virtual Color HoverBorderColor { get; set; } = Color.FromArgb(100, 100, 100);

        /// <summary>
        /// 边框宽度
        /// </summary>
        public virtual float BorderWidth { get; set; } = 2f;

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

            // 绘制圆角矩形背景
            using (var path = CreateRoundedRectangle(rect, CornerRadius))
            {
                // 填充背景
                using (var brush = new SolidBrush(BackgroundColor))
                {
                    g.FillPath(brush, path);
                }

                // 绘制边框
                var borderColor = IsSelected ? SelectedBorderColor : (IsHovered ? HoverBorderColor : BorderColor);
                using (var pen = new Pen(borderColor, BorderWidth))
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
            foreach (var port in InputPorts)
            {
                DrawPort(g, port, true);
            }

            // 绘制输出端口
            foreach (var port in OutputPorts)
            {
                DrawPort(g, port, false);
            }
        }

        /// <summary>
        /// 绘制单个端口
        /// </summary>
        protected virtual void DrawPort(Graphics g, PointF port, bool isInput)
        {
            var rect = new RectangleF(
                port.X - PortSize / 2,
                port.Y - PortSize / 2,
                PortSize,
                PortSize
            );

            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, rect);
            }
            using (var pen = new Pen(BorderColor, 1.5f))
            {
                g.DrawEllipse(pen, rect);
            }
        }

        /// <summary>
        /// 检测端口点击
        /// </summary>
        public virtual PointF? HitTestPort(PointF point, bool isOutput)
        {
            var ports = isOutput ? OutputPorts : InputPorts;
            foreach (var port in ports)
            {
                var dx = point.X - port.X;
                var dy = point.Y - port.Y;
                if (dx * dx + dy * dy <= PortSize * PortSize)
                {
                    return port;
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
            // 占位：绘制一个小矩形作为图标
            var iconSize = 20f;
            var iconRect = new RectangleF(
                rect.X + 10,
                rect.Y + (rect.Height - iconSize) / 2,
                iconSize,
                iconSize
            );

            using (var brush = new SolidBrush(Color.Gray))
            {
                g.FillRectangle(brush, iconRect);
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
