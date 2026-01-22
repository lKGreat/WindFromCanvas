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
        private readonly Dictionary<RenderLayerType, List<RectangleF>> _dirtyRegions = new Dictionary<RenderLayerType, List<RectangleF>>();
        private readonly Dictionary<RenderLayerType, Action<Graphics, RectangleF>> _renderCallbacks = new Dictionary<RenderLayerType, Action<Graphics, RectangleF>>();
        private Size _bufferSize = Size.Empty;
        private bool _enableDirtyRegionOptimization = true;

        /// <summary>
        /// 是否启用脏区域优化（默认启用）
        /// </summary>
        public bool EnableDirtyRegionOptimization
        {
            get => _enableDirtyRegionOptimization;
            set => _enableDirtyRegionOptimization = value;
        }

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
            // 清除该层的脏区域，因为整层都需要重绘
            if (_dirtyRegions.ContainsKey(layer))
            {
                _dirtyRegions[layer].Clear();
            }
        }

        /// <summary>
        /// 标记层的特定区域为脏（局部重绘优化）
        /// 2.3.2 / 2.3.3 节点移动和连线更新时的脏区域计算
        /// </summary>
        public void MarkRegionDirty(RenderLayerType layer, RectangleF region)
        {
            if (!_enableDirtyRegionOptimization)
            {
                // 如果未启用脏区域优化，标记整层为脏
                MarkLayerDirty(layer);
                return;
            }

            if (!_dirtyRegions.ContainsKey(layer))
            {
                _dirtyRegions[layer] = new List<RectangleF>();
            }

            // 扩展区域（添加边距以确保完全覆盖）
            const float margin = 5f;
            var expandedRegion = new RectangleF(
                region.X - margin,
                region.Y - margin,
                region.Width + margin * 2,
                region.Height + margin * 2
            );

            _dirtyRegions[layer].Add(expandedRegion);

            // 如果脏区域太多，标记整层为脏（避免过多的区域管理开销）
            if (_dirtyRegions[layer].Count > 10)
            {
                _dirtyLayers.Add(layer);
                _dirtyRegions[layer].Clear();
            }
        }

        /// <summary>
        /// 标记多个层为脏
        /// </summary>
        public void MarkLayersDirty(params RenderLayerType[] layers)
        {
            foreach (var layer in layers)
            {
                _dirtyLayers.Add(layer);
                if (_dirtyRegions.ContainsKey(layer))
                {
                    _dirtyRegions[layer].Clear();
                }
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
            _dirtyRegions.Clear();
        }

        /// <summary>
        /// 获取层的脏区域数量（用于性能监控）
        /// </summary>
        public int GetDirtyRegionCount(RenderLayerType layer)
        {
            if (_dirtyRegions.TryGetValue(layer, out var regions))
            {
                return regions.Count;
            }
            return 0;
        }

        /// <summary>
        /// 获取所有脏层数量（用于性能监控）
        /// </summary>
        public int GetDirtyLayerCount()
        {
            return _dirtyLayers.Count;
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
                // 如果层是完全脏的，重新渲染整层
                if (_dirtyLayers.Contains(layer))
                {
                    RenderLayer(layer, viewport);
                    _dirtyLayers.Remove(layer);
                }
                // 如果有脏区域，只重绘这些区域
                else if (_enableDirtyRegionOptimization && 
                         _dirtyRegions.TryGetValue(layer, out var regions) && 
                         regions.Count > 0)
                {
                    RenderLayerRegions(layer, viewport, regions);
                    regions.Clear();
                }

                // 将层缓冲区绘制到目标Graphics
                if (_layerBuffers.TryGetValue(layer, out var buffer) && buffer != null)
                {
                    targetGraphics.DrawImage(buffer, viewport.X, viewport.Y);
                }
            }
        }

        /// <summary>
        /// 渲染层的特定区域（局部重绘）
        /// </summary>
        private void RenderLayerRegions(RenderLayerType layer, RectangleF viewport, List<RectangleF> regions)
        {
            if (!_layerBuffers.TryGetValue(layer, out var buffer) || buffer == null)
            {
                return;
            }

            using (var g = Graphics.FromImage(buffer))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 只清除和重绘脏区域
                foreach (var region in regions)
                {
                    // 设置裁剪区域
                    g.SetClip(region);
                    g.Clear(Color.Transparent);

                    // 调用渲染回调（回调需要自己处理裁剪）
                    if (_renderCallbacks.TryGetValue(layer, out var callback))
                    {
                        callback?.Invoke(g, viewport);
                    }

                    g.ResetClip();
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
            _dirtyRegions.Clear();
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
