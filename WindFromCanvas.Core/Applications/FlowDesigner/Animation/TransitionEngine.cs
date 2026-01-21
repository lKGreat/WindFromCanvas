using System;
using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Animation
{
    /// <summary>
    /// 过渡动画引擎（平滑过渡、缓动效果）
    /// </summary>
    public class TransitionEngine
    {
        /// <summary>
        /// 平滑过渡到目标值
        /// </summary>
        public static float SmoothTransition(float current, float target, float speed = 0.1f)
        {
            return current + (target - current) * speed;
        }

        /// <summary>
        /// 平滑过渡到目标点
        /// </summary>
        public static PointF SmoothTransition(PointF current, PointF target, float speed = 0.1f)
        {
            return new PointF(
                SmoothTransition(current.X, target.X, speed),
                SmoothTransition(current.Y, target.Y, speed)
            );
        }

        /// <summary>
        /// 平滑过渡到目标矩形
        /// </summary>
        public static RectangleF SmoothTransition(RectangleF current, RectangleF target, float speed = 0.1f)
        {
            return new RectangleF(
                SmoothTransition(current.X, target.X, speed),
                SmoothTransition(current.Y, target.Y, speed),
                SmoothTransition(current.Width, target.Width, speed),
                SmoothTransition(current.Height, target.Height, speed)
            );
        }

        /// <summary>
        /// 使用缓动函数过渡
        /// </summary>
        public static float EasedTransition(float current, float target, float progress, Func<float, float> easing)
        {
            float eased = easing?.Invoke(progress) ?? progress;
            return current + (target - current) * eased;
        }

        /// <summary>
        /// 缩放过渡（以中心点为基准）
        /// </summary>
        public static RectangleF ScaleTransition(RectangleF rect, float scale, PointF center)
        {
            var newWidth = rect.Width * scale;
            var newHeight = rect.Height * scale;
            var newX = center.X - newWidth / 2;
            var newY = center.Y - newHeight / 2;

            return new RectangleF(newX, newY, newWidth, newHeight);
        }

        /// <summary>
        /// 检查是否接近目标值（用于判断过渡是否完成）
        /// </summary>
        public static bool IsNearTarget(float current, float target, float threshold = 0.01f)
        {
            return Math.Abs(current - target) < threshold;
        }

        /// <summary>
        /// 检查点是否接近目标点
        /// </summary>
        public static bool IsNearTarget(PointF current, PointF target, float threshold = 0.01f)
        {
            var dx = current.X - target.X;
            var dy = current.Y - target.Y;
            return Math.Sqrt(dx * dx + dy * dy) < threshold;
        }
    }
}
