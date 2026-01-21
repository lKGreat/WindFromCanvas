using System;
using System.Drawing;
using System.Runtime.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Models
{
    /// <summary>
    /// 笔记颜色变体（参考Activepieces）
    /// </summary>
    public enum NoteColorVariant
    {
        Blue,
        Yellow,
        Green,
        Pink,
        Purple
    }

    /// <summary>
    /// 笔记数据（可序列化）
    /// </summary>
    [DataContract]
    [Serializable]
    public class NoteData
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public float PositionX { get; set; }

        [DataMember]
        public float PositionY { get; set; }

        [DataMember]
        public float Width { get; set; } = 200f;

        [DataMember]
        public float Height { get; set; } = 150f;

        [DataMember]
        public NoteColorVariant Color { get; set; } = NoteColorVariant.Blue;

        public PointF Position
        {
            get => new PointF(PositionX, PositionY);
            set
            {
                PositionX = value.X;
                PositionY = value.Y;
            }
        }

        public SizeF Size
        {
            get => new SizeF(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        public static System.Drawing.Color GetColor(NoteColorVariant variant)
        {
            switch (variant)
            {
                case NoteColorVariant.Blue: return System.Drawing.Color.FromArgb(59, 130, 246);
                case NoteColorVariant.Yellow: return System.Drawing.Color.FromArgb(234, 179, 8);
                case NoteColorVariant.Green: return System.Drawing.Color.FromArgb(16, 185, 129);
                case NoteColorVariant.Pink: return System.Drawing.Color.FromArgb(236, 72, 153);
                case NoteColorVariant.Purple: return System.Drawing.Color.FromArgb(147, 51, 234);
                default: return System.Drawing.Color.FromArgb(59, 130, 246);
            }
        }

        public System.Drawing.Color GetColor()
        {
            return GetColor(this.Color);
        }
    }
}
