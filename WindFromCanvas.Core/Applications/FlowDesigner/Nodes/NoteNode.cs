using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Objects;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Nodes
{
    /// <summary>
    /// 笔记节点（参考Activepieces实现）
    /// </summary>
    public class NoteNode : CanvasObject
    {
        public NoteData Data { get; set; }

        public float NoteWidth
        {
            get => Data?.Width ?? 200f;
            set { if (Data != null) Data.Width = value; }
        }

        public float NoteHeight
        {
            get => Data?.Height ?? 150f;
            set { if (Data != null) Data.Height = value; }
        }

        public bool IsSelected { get; set; }
        public bool IsHovered { get; set; }

        public NoteNode(NoteData data)
        {
            Data = data;
            if (data != null)
            {
                X = data.Position.X;
                Y = data.Position.Y;
            }
            Draggable = true;
            ZIndex = 5; // 笔记在节点上方
        }

        public override void Draw(Graphics g)
        {
            if (!Visible || Data == null) return;

            var rect = new RectangleF(X, Y, NoteWidth, NoteHeight);
            var color = NoteData.GetColor(Data.Color);

            // 绘制半透明背景
            using (var brush = new SolidBrush(Color.FromArgb(240, color.R, color.G, color.B)))
            {
                g.FillRectangle(brush, rect);
            }

            // 绘制边框
            var borderColor = IsSelected ? Color.FromArgb(59, 130, 246) : 
                             (IsHovered ? Color.FromArgb(148, 163, 184) : color);
            using (var pen = new Pen(borderColor, IsSelected ? 2f : 1f))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }

            // 绘制文本内容
            if (!string.IsNullOrEmpty(Data.Content))
            {
                var textRect = new RectangleF(
                    rect.X + 8,
                    rect.Y + 8,
                    rect.Width - 16,
                    rect.Height - 16
                );

                using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter
                })
                {
                    g.DrawString(Data.Content, SystemFonts.DefaultFont, brush, textRect, sf);
                }
            }
        }

        public override bool HitTest(PointF point)
        {
            return new RectangleF(X, Y, NoteWidth, NoteHeight).Contains(point);
        }

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, NoteWidth, NoteHeight);
        }
    }
}
