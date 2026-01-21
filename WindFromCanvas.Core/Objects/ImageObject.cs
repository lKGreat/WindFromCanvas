using System;
using System.Drawing;

namespace WindFromCanvas.Core.Objects
{
    public class ImageObject : CanvasObject
    {
        public Image Image { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public RectangleF? SourceRect { get; set; }  // 用于裁剪

        public override void Draw(Graphics g)
        {
            if (!Visible || Image == null) return;

            if (SourceRect.HasValue)
            {
                // 绘制裁剪的图像
                var srcRect = SourceRect.Value;
                var destRect = new RectangleF(X, Y, Width, Height);
                g.DrawImage(Image, destRect, srcRect, GraphicsUnit.Pixel);
            }
            else if (Width > 0 && Height > 0)
            {
                // 绘制缩放图像
                var destRect = new RectangleF(X, Y, Width, Height);
                g.DrawImage(Image, destRect);
            }
            else
            {
                // 绘制原始大小图像
                g.DrawImage(Image, X, Y);
            }
        }

        public override bool HitTest(PointF point)
        {
            if (!Visible || Image == null) return false;
            
            float width = Width > 0 ? Width : Image.Width;
            float height = Height > 0 ? Height : Image.Height;
            
            return point.X >= X && point.X <= X + width &&
                   point.Y >= Y && point.Y <= Y + height;
        }

        public override RectangleF GetBounds()
        {
            if (Image == null) return new RectangleF(X, Y, 0, 0);
            
            float width = Width > 0 ? Width : Image.Width;
            float height = Height > 0 ? Height : Image.Height;
            
            return new RectangleF(X, Y, width, height);
        }
    }
}
