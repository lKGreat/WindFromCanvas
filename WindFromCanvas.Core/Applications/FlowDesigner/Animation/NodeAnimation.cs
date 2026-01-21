using System;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Animation
{
    /// <summary>
    /// 节点动画类型
    /// </summary>
    public enum AnimationType
    {
        FadeIn,          // 淡入
        FadeOut,         // 淡出
        ScalePulse,      // 缩放脉冲
        Rotate,          // 旋转
        SlideIn,         // 滑入
        ConnectionFlow,  // 连线流动动画
        ConnectionBuild, // 连接建立动画（曲线生长）
        PortPulse        // 端口脉冲动画
    }

    /// <summary>
    /// 节点动画（参考Activepieces动画效果）
    /// </summary>
    public class NodeAnimation
    {
        public AnimationType Type { get; set; }
        public float Duration { get; set; } // 动画持续时间（秒）
        public float ElapsedTime { get; set; } // 已用时间
        public bool IsCompleted { get; private set; }
        public bool IsRunning { get; private set; }

        // 淡入淡出
        public float StartOpacity { get; set; } = 0f;
        public float EndOpacity { get; set; } = 1f;
        public float CurrentOpacity { get; private set; }

        // 缩放脉冲
        public float BaseScale { get; set; } = 1f;
        public float PulseScale { get; set; } = 1.1f;
        public float CurrentScale { get; private set; }

        // 旋转
        public float RotationAngle { get; private set; }

        // 连线流动动画
        public float FlowAnimationOffset { get; set; }

        // 连接建立动画进度（0-1）
        public float BuildProgress { get; set; }

        // 端口脉冲半径
        public float PulseRadius { get; set; }

        // 缓动函数
        public Func<float, float> EasingFunction { get; set; }

        public NodeAnimation(AnimationType type, float duration = 0.3f)
        {
            Type = type;
            Duration = duration;
            ElapsedTime = 0f;
            IsCompleted = false;
            IsRunning = false;
            CurrentOpacity = 1f;
            CurrentScale = 1f;
            
            // 默认缓动函数：ease-out
            EasingFunction = EaseOutCubic;
        }

        /// <summary>
        /// 开始动画
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            IsCompleted = false;
            ElapsedTime = 0f;
            
            switch (Type)
            {
                case AnimationType.FadeIn:
                    CurrentOpacity = StartOpacity;
                    break;
                case AnimationType.FadeOut:
                    CurrentOpacity = EndOpacity;
                    break;
                case AnimationType.ScalePulse:
                    CurrentScale = BaseScale;
                    break;
            }
        }

        /// <summary>
        /// 更新动画
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsRunning || IsCompleted) return;

            ElapsedTime += deltaTime;
            float progress = Math.Min(ElapsedTime / Duration, 1f);
            float easedProgress = EasingFunction?.Invoke(progress) ?? progress;

            switch (Type)
            {
                case AnimationType.FadeIn:
                    CurrentOpacity = Lerp(StartOpacity, EndOpacity, easedProgress);
                    break;

                case AnimationType.FadeOut:
                    CurrentOpacity = Lerp(EndOpacity, StartOpacity, easedProgress);
                    break;

                case AnimationType.ScalePulse:
                    // 脉冲效果：快速放大然后恢复
                    if (progress < 0.5f)
                    {
                        CurrentScale = Lerp(BaseScale, PulseScale, progress * 2f);
                    }
                    else
                    {
                        CurrentScale = Lerp(PulseScale, BaseScale, (progress - 0.5f) * 2f);
                    }
                    break;

                case AnimationType.Rotate:
                    RotationAngle = 360f * easedProgress;
                    break;

                case AnimationType.ConnectionFlow:
                    // 连线流动：dash offset 循环变化
                    FlowAnimationOffset = (progress * 20f) % 20f;
                    // 循环动画，永不完成
                    if (progress >= 1f)
                    {
                        ElapsedTime = 0f; // 重置时间以循环
                    }
                    break;

                case AnimationType.ConnectionBuild:
                    // 连接建立：曲线从起点生长到终点
                    BuildProgress = easedProgress;
                    break;

                case AnimationType.PortPulse:
                    // 端口脉冲：半径从0增长到最大值然后消失
                    PulseRadius = Lerp(0f, 20f, progress);
                    CurrentOpacity = Lerp(0.8f, 0f, progress);
                    break;
            }

            // 连线流动动画永不完成（循环）
            if (Type == AnimationType.ConnectionFlow)
            {
                // 不设置完成状态，保持循环
            }
            else if (progress >= 1f)
            {
                IsCompleted = true;
                IsRunning = false;
            }
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            IsCompleted = true;
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        // 缓动函数
        public static float EaseOutCubic(float t)
        {
            return 1f - (float)Math.Pow(1f - t, 3);
        }

        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        }

        public static float EaseOutElastic(float t)
        {
            const float c4 = (2f * (float)Math.PI) / 3f;
            return t == 0f ? 0f :
                   t == 1f ? 1f :
                   (float)Math.Pow(2f, -10f * t) * (float)Math.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }
}
