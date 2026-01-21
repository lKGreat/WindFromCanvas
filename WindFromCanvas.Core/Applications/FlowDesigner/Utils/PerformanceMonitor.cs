using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 性能监控器（用于大型流程优化）
    /// </summary>
    public class PerformanceMonitor
    {
        private static PerformanceMonitor _instance;
        private Queue<float> _fpsHistory = new Queue<float>();
        private Stopwatch _frameTimer = new Stopwatch();
        private int _frameCount = 0;
        private float _currentFPS = 60f;
        private const int MaxHistorySize = 60; // 保留60帧历史

        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PerformanceMonitor();
                }
                return _instance;
            }
        }

        private PerformanceMonitor()
        {
            _frameTimer.Start();
        }

        /// <summary>
        /// 记录一帧
        /// </summary>
        public void RecordFrame()
        {
            _frameCount++;
            var elapsed = _frameTimer.ElapsedMilliseconds;

            if (elapsed >= 1000) // 每秒更新一次FPS
            {
                _currentFPS = _frameCount * 1000f / elapsed;
                _fpsHistory.Enqueue(_currentFPS);

                if (_fpsHistory.Count > MaxHistorySize)
                {
                    _fpsHistory.Dequeue();
                }

                _frameCount = 0;
                _frameTimer.Restart();
            }
        }

        /// <summary>
        /// 获取当前FPS
        /// </summary>
        public float GetCurrentFPS()
        {
            return _currentFPS;
        }

        /// <summary>
        /// 获取平均FPS
        /// </summary>
        public float GetAverageFPS()
        {
            if (_fpsHistory.Count == 0) return 60f;
            return _fpsHistory.Average();
        }

        /// <summary>
        /// 获取最低FPS
        /// </summary>
        public float GetMinFPS()
        {
            if (_fpsHistory.Count == 0) return 60f;
            return _fpsHistory.Min();
        }

        /// <summary>
        /// 检查性能是否良好（FPS > 30）
        /// </summary>
        public bool IsPerformanceGood()
        {
            return _currentFPS >= 30f;
        }

        /// <summary>
        /// 建议的LOD级别（基于FPS）
        /// </summary>
        public int GetSuggestedLODLevel()
        {
            if (_currentFPS >= 50) return 0; // 高质量
            if (_currentFPS >= 30) return 1; // 中等质量
            return 2; // 低质量
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public void Reset()
        {
            _fpsHistory.Clear();
            _frameCount = 0;
            _currentFPS = 60f;
            _frameTimer.Restart();
        }
    }
}
