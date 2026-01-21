using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Objects;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Animation
{
    /// <summary>
    /// 动画管理器（单例，参考Activepieces动画系统）
    /// </summary>
    public class AnimationManager
    {
        private static AnimationManager _instance;
        private Dictionary<CanvasObject, List<NodeAnimation>> _animations = new Dictionary<CanvasObject, List<NodeAnimation>>();
        private Timer _animationTimer;
        private bool _isEnabled = true;

        public static AnimationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AnimationManager();
                }
                return _instance;
            }
        }

        private AnimationManager()
        {
            _animationTimer = new Timer();
            _animationTimer.Interval = 16; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
        }

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// 添加动画
        /// </summary>
        public void AddAnimation(CanvasObject target, NodeAnimation animation)
        {
            if (!_isEnabled) return;

            if (!_animations.ContainsKey(target))
            {
                _animations[target] = new List<NodeAnimation>();
            }

            animation.Start();
            _animations[target].Add(animation);
        }

        /// <summary>
        /// 移除动画
        /// </summary>
        public void RemoveAnimation(CanvasObject target, NodeAnimation animation)
        {
            if (_animations.ContainsKey(target))
            {
                _animations[target].Remove(animation);
                if (_animations[target].Count == 0)
                {
                    _animations.Remove(target);
                }
            }
        }

        /// <summary>
        /// 清除对象的所有动画
        /// </summary>
        public void ClearAnimations(CanvasObject target)
        {
            _animations.Remove(target);
        }

        /// <summary>
        /// 获取对象的动画
        /// </summary>
        public List<NodeAnimation> GetAnimations(CanvasObject target)
        {
            return _animations.ContainsKey(target) ? _animations[target] : new List<NodeAnimation>();
        }

        /// <summary>
        /// 动画定时器更新
        /// </summary>
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            float deltaTime = _animationTimer.Interval / 1000f; // 转换为秒

            var completedAnimations = new List<(CanvasObject target, NodeAnimation animation)>();

            foreach (var kvp in _animations.ToList())
            {
                var target = kvp.Key;
                var animations = kvp.Value.ToList();

                foreach (var animation in animations)
                {
                    animation.Update(deltaTime);

                    if (animation.IsCompleted)
                    {
                        completedAnimations.Add((target, animation));
                    }
                }
            }

            // 移除已完成的动画
            foreach (var (target, animation) in completedAnimations)
            {
                RemoveAnimation(target, animation);
            }
        }

        /// <summary>
        /// 创建淡入动画
        /// </summary>
        public NodeAnimation CreateFadeInAnimation(float duration = 0.3f)
        {
            return new NodeAnimation(AnimationType.FadeIn, duration)
            {
                StartOpacity = 0f,
                EndOpacity = 1f,
                EasingFunction = NodeAnimation.EaseOutCubic
            };
        }

        /// <summary>
        /// 创建淡出动画
        /// </summary>
        public NodeAnimation CreateFadeOutAnimation(float duration = 0.3f)
        {
            return new NodeAnimation(AnimationType.FadeOut, duration)
            {
                StartOpacity = 1f,
                EndOpacity = 0f,
                EasingFunction = NodeAnimation.EaseOutCubic
            };
        }

        /// <summary>
        /// 创建缩放脉冲动画
        /// </summary>
        public NodeAnimation CreateScalePulseAnimation(float pulseScale = 1.1f, float duration = 0.4f)
        {
            return new NodeAnimation(AnimationType.ScalePulse, duration)
            {
                BaseScale = 1f,
                PulseScale = pulseScale,
                EasingFunction = NodeAnimation.EaseOutElastic
            };
        }

        /// <summary>
        /// 创建旋转动画（用于运行状态）
        /// </summary>
        public NodeAnimation CreateRotateAnimation(float duration = 1f)
        {
            return new NodeAnimation(AnimationType.Rotate, duration)
            {
                EasingFunction = null // 线性旋转
            };
        }

        /// <summary>
        /// 停止所有动画
        /// </summary>
        public void StopAllAnimations()
        {
            foreach (var animations in _animations.Values)
            {
                foreach (var animation in animations)
                {
                    animation.Stop();
                }
            }
            _animations.Clear();
        }
    }
}
