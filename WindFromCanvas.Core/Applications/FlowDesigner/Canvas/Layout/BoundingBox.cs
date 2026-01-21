using System;
using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout
{
    /// <summary>
    /// 边界框（用于布局计算）
    /// </summary>
    public class BoundingBox
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Right { get; set; }
        public float Bottom { get; set; }

        public float Width => Right - Left;
        public float Height => Bottom - Top;

        public PointF Center => new PointF((Left + Right) / 2, (Top + Bottom) / 2);

        public BoundingBox()
        {
            Left = float.MaxValue;
            Top = float.MaxValue;
            Right = float.MinValue;
            Bottom = float.MinValue;
        }

        public BoundingBox(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// 扩展边界框以包含点
        /// </summary>
        public void ExpandToInclude(PointF point)
        {
            Left = Math.Min(Left, point.X);
            Top = Math.Min(Top, point.Y);
            Right = Math.Max(Right, point.X);
            Bottom = Math.Max(Bottom, point.Y);
        }

        /// <summary>
        /// 扩展边界框以包含矩形
        /// </summary>
        public void ExpandToInclude(RectangleF rect)
        {
            Left = Math.Min(Left, rect.Left);
            Top = Math.Min(Top, rect.Top);
            Right = Math.Max(Right, rect.Right);
            Bottom = Math.Max(Bottom, rect.Bottom);
        }

        /// <summary>
        /// 扩展边界框以包含另一个边界框
        /// </summary>
        public void ExpandToInclude(BoundingBox other)
        {
            Left = Math.Min(Left, other.Left);
            Top = Math.Min(Top, other.Top);
            Right = Math.Max(Right, other.Right);
            Bottom = Math.Max(Bottom, other.Bottom);
        }

        /// <summary>
        /// 应用偏移
        /// </summary>
        public BoundingBox Offset(float deltaX, float deltaY)
        {
            return new BoundingBox(
                Left + deltaX,
                Top + deltaY,
                Right + deltaX,
                Bottom + deltaY
            );
        }

        /// <summary>
        /// 转换为矩形
        /// </summary>
        public RectangleF ToRectangle()
        {
            return new RectangleF(Left, Top, Width, Height);
        }
    }
}
