using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Rendering
{
    /// <summary>
    /// 渲染层管理器（管理分层渲染和局部重绘）
    /// </summary>
    public class RenderLayerManager
    {
        private readonly Dictionary<RenderLayerType, Bitmap> _layerBuffers = new Dictionary<RenderLayerType, Bitmap>();
        private readonly HashSet<RenderLayerType> _dirtyLayers = new HashSet<RenderLayerType>();
        private readonly Dictionary<RenderLayerType, Action<Graphics, RectangleF>> _renderCallbacks = new Dictionary<RenderLayerType, Action<Graphics, RectangleF>>();
        private Size _bufferSize = Size.Empty;

        /// <summary>
        /// 注册渲染回调
        /// </summary>
        public void RegisterRenderCallback(RenderLayerType layer, Action<Graphics, RectangleF> renderCallback)
        {
            _renderCallbacks[layer] = renderCallback;
        }

        /// <summary>
        /// 标记层为脏（需要重绘）
        /// </summary>
        public void MarkLayerDirty(RenderLayerType layer)
        {
            _dirtyLayers.Add(layer);
        }

        /// <summary>
        /// 标记多个层为脏
        /// </summary>
        public void MarkLayersDirty(params RenderLayerType[] layers)
        {
            foreach (var layer in layers)
            {
                _dirtyLayers.Add(layer);
            }
        }

        /// <summary>
        /// 标记所有层为脏
        /// </summary>
        public void MarkAllLayersDirty()
        {
            foreach (RenderLayerType layer in Enum.GetValues(typeof(RenderLayerType)))
            {
                _dirtyLayers.Add(layer);
            }
        }

        /// <summary>
        /// 渲染所有层到目标Graphics
        /// </summary>
        public void Render(Graphics targetGraphics, RectangleF viewport)
        {
            if (targetGraphics == null)
            {
                return;
            }

            // 检查是否需要调整缓冲区大小
            var requiredSize = new Size((int)Math.Ceiling(viewport.Width), (int)Math.Ceiling(viewport.Height));
            if (_bufferSize != requiredSize)
            {
                ResizeBuffers(requiredSize);
            }

            // 按顺序渲染每一层
            foreach (RenderLayerType layer in Enum.GetValues(typeof(RenderLayerType)).Cast<RenderLayerType>().OrderBy(l => (int)l))
            {
                // 如果层是脏的，重新渲染
                if (_dirtyLayers.Contains(layer))
                {
                    RenderLayer(layer, viewport);
                    _dirtyLayers.Remove(layer);
                }

                // 将层缓冲区绘制到目标Graphics
                if (_layerBuffers.TryGetValue(layer, out var buffer) && buffer != null)
                {
                    targetGraphics.DrawImage(buffer, viewport.X, viewport.Y);
                }
            }
        }

        /// <summary>
        /// 渲染指定层
        /// </summary>
        private void RenderLayer(RenderLayerType layer, RectangleF viewport)
        {
            if (!_layerBuffers.TryGetValue(layer, out var buffer) || buffer == null)
            {
                return;
            }

            using (var g = Graphics.FromImage(buffer))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 调用注册的渲染回调
                if (_renderCallbacks.TryGetValue(layer, out var callback))
                {
                    callback?.Invoke(g, viewport);
                }
            }
        }

        /// <summary>
        /// 调整缓冲区大小
        /// </summary>
        private void ResizeBuffers(Size newSize)
        {
            // 释放旧的缓冲区
            foreach (var buffer in _layerBuffers.Values)
            {
                buffer?.Dispose();
            }
            _layerBuffers.Clear();

            // 创建新的缓冲区
            foreach (RenderLayerType layer in Enum.GetValues(typeof(RenderLayerType)))
            {
                var buffer = new Bitmap(newSize.Width, newSize.Height);
                _layerBuffers[layer] = buffer;
            }

            _bufferSize = newSize;
            MarkAllLayersDirty();
        }

        /// <summary>
        /// 获取指定层的缓冲区（用于直接绘制）
        /// </summary>
        public Bitmap GetLayerBuffer(RenderLayerType layer)
        {
            if (_layerBuffers.TryGetValue(layer, out var buffer))
            {
                return buffer;
            }
            return null;
        }

        /// <summary>
        /// 清除所有层
        /// </summary>
        public void Clear()
        {
            foreach (var buffer in _layerBuffers.Values)
            {
                buffer?.Dispose();
            }
            _layerBuffers.Clear();
            _dirtyLayers.Clear();
            _renderCallbacks.Clear();
            _bufferSize = Size.Empty;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
