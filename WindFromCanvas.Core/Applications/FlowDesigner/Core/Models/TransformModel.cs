using System;
using System.ComponentModel;
using System.Drawing;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// TransformModel坐标系统管理类
    /// 统一管理画布变换（缩放、平移），提供Canvas/Client坐标互转
    /// 类似LogicFlow的TransformModel
    /// </summary>
    public class TransformModel : INotifyPropertyChanged
    {
        private float _zoom;
        private PointF _translation;
        private float _minZoom;
        private float _maxZoom;
        private SizeF _canvasSize;

        /// <summary>
        /// 缩放级别（1.0 = 100%，0.5 = 50%，2.0 = 200%）
        /// </summary>
        public float Zoom
        {
            get => _zoom;
            set
            {
                var newZoom = Math.Max(_minZoom, Math.Min(_maxZoom, value));
                if (Math.Abs(_zoom - newZoom) > 0.001f)
                {
                    _zoom = newZoom;
                    OnPropertyChanged(nameof(Zoom));
                    OnPropertyChanged(nameof(ZoomPercent));
                }
            }
        }

        /// <summary>
        /// 缩放百分比（便捷属性）
        /// </summary>
        public int ZoomPercent => (int)(_zoom * 100);

        /// <summary>
        /// 平移偏移量（画布在客户端坐标系中的偏移）
        /// </summary>
        public PointF Translation
        {
            get => _translation;
            set
            {
                if (_translation != value)
                {
                    _translation = value;
                    OnPropertyChanged(nameof(Translation));
                }
            }
        }

        /// <summary>
        /// 最小缩放级别
        /// </summary>
        public float MinZoom
        {
            get => _minZoom;
            set
            {
                _minZoom = value;
                if (_zoom < _minZoom)
                    Zoom = _minZoom;
            }
        }

        /// <summary>
        /// 最大缩放级别
        /// </summary>
        public float MaxZoom
        {
            get => _maxZoom;
            set
            {
                _maxZoom = value;
                if (_zoom > _maxZoom)
                    Zoom = _maxZoom;
            }
        }

        /// <summary>
        /// 画布尺寸
        /// </summary>
        public SizeF CanvasSize
        {
            get => _canvasSize;
            set
            {
                _canvasSize = value;
                OnPropertyChanged(nameof(CanvasSize));
            }
        }

        public TransformModel()
        {
            _zoom = 1.0f;
            _translation = PointF.Empty;
            _minZoom = 0.1f;
            _maxZoom = 5.0f;
            _canvasSize = new SizeF(2000, 2000);
        }

        #region 坐标转换

        /// <summary>
        /// Canvas坐标转Client坐标
        /// Canvas坐标：画布上的逻辑坐标（节点位置等）
        /// Client坐标：屏幕显示的物理坐标
        /// </summary>
        public PointF CanvasToClient(PointF canvasPoint)
        {
            return new PointF(
                canvasPoint.X * _zoom + _translation.X,
                canvasPoint.Y * _zoom + _translation.Y
            );
        }

        /// <summary>
        /// Canvas坐标转Client坐标（使用X,Y参数）
        /// </summary>
        public PointF CanvasToClient(float canvasX, float canvasY)
        {
            return CanvasToClient(new PointF(canvasX, canvasY));
        }

        /// <summary>
        /// Client坐标转Canvas坐标
        /// Client坐标：屏幕显示的物理坐标
        /// Canvas坐标：画布上的逻辑坐标
        /// </summary>
        public PointF ClientToCanvas(PointF clientPoint)
        {
            return new PointF(
                (clientPoint.X - _translation.X) / _zoom,
                (clientPoint.Y - _translation.Y) / _zoom
            );
        }

        /// <summary>
        /// Client坐标转Canvas坐标（使用X,Y参数）
        /// </summary>
        public PointF ClientToCanvas(float clientX, float clientY)
        {
            return ClientToCanvas(new PointF(clientX, clientY));
        }

        /// <summary>
        /// 转换矩形边界（Canvas -> Client）
        /// </summary>
        public RectangleF CanvasRectToClient(RectangleF canvasRect)
        {
            var topLeft = CanvasToClient(canvasRect.Location);
            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                canvasRect.Width * _zoom,
                canvasRect.Height * _zoom
            );
        }

        /// <summary>
        /// 转换矩形边界（Client -> Canvas）
        /// </summary>
        public RectangleF ClientRectToCanvas(RectangleF clientRect)
        {
            var topLeft = ClientToCanvas(clientRect.Location);
            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                clientRect.Width / _zoom,
                clientRect.Height / _zoom
            );
        }

        #endregion

        #region 缩放操作

        /// <summary>
        /// 在指定点进行缩放（保持该点在屏幕上的位置不变）
        /// </summary>
        /// <param name="clientPoint">Client坐标系中的缩放中心点</param>
        /// <param name="delta">缩放增量</param>
        public void ZoomAt(PointF clientPoint, float delta)
        {
            // 计算缩放前该点对应的Canvas坐标
            var canvasPoint = ClientToCanvas(clientPoint);

            // 应用缩放
            var oldZoom = _zoom;
            Zoom += delta;
            var actualDelta = _zoom - oldZoom;

            if (Math.Abs(actualDelta) > 0.001f)
            {
                // 调整平移量，使该点在屏幕上的位置保持不变
                _translation.X = clientPoint.X - canvasPoint.X * _zoom;
                _translation.Y = clientPoint.Y - canvasPoint.Y * _zoom;
                OnPropertyChanged(nameof(Translation));
            }
        }

        /// <summary>
        /// 以画布中心为基准进行缩放
        /// </summary>
        public void ZoomCenter(float delta, SizeF viewportSize)
        {
            var centerPoint = new PointF(viewportSize.Width / 2, viewportSize.Height / 2);
            ZoomAt(centerPoint, delta);
        }

        /// <summary>
        /// 设置缩放级别（到指定值）
        /// </summary>
        public void SetZoom(float zoom, PointF? centerPoint = null)
        {
            if (centerPoint.HasValue)
            {
                var canvasPoint = ClientToCanvas(centerPoint.Value);
                Zoom = zoom;
                _translation.X = centerPoint.Value.X - canvasPoint.X * _zoom;
                _translation.Y = centerPoint.Value.Y - canvasPoint.Y * _zoom;
                OnPropertyChanged(nameof(Translation));
            }
            else
            {
                Zoom = zoom;
            }
        }

        /// <summary>
        /// 重置缩放到100%
        /// </summary>
        public void ResetZoom()
        {
            Zoom = 1.0f;
        }

        #endregion

        #region 平移操作

        /// <summary>
        /// 平移画布（Client坐标系中的偏移量）
        /// </summary>
        public void Pan(float dx, float dy)
        {
            Translation = new PointF(_translation.X + dx, _translation.Y + dy);
        }

        /// <summary>
        /// 平移画布到指定位置
        /// </summary>
        public void PanTo(float x, float y)
        {
            Translation = new PointF(x, y);
        }

        /// <summary>
        /// 重置平移到原点
        /// </summary>
        public void ResetPan()
        {
            Translation = PointF.Empty;
        }

        #endregion

        #region 视图适配

        /// <summary>
        /// 适应视图（将所有内容缩放到可见范围内）
        /// </summary>
        /// <param name="contentBounds">内容边界（Canvas坐标）</param>
        /// <param name="viewportSize">视口尺寸（Client坐标）</param>
        /// <param name="padding">边距</param>
        public void FitToView(RectangleF contentBounds, SizeF viewportSize, float padding = 50)
        {
            if (contentBounds.IsEmpty || contentBounds.Width <= 0 || contentBounds.Height <= 0)
            {
                // 如果内容为空，重置到中心
                ResetZoom();
                Translation = new PointF(viewportSize.Width / 2, viewportSize.Height / 2);
                return;
            }

            // 计算适合的缩放级别
            var availableWidth = viewportSize.Width - 2 * padding;
            var availableHeight = viewportSize.Height - 2 * padding;

            var zoomX = availableWidth / contentBounds.Width;
            var zoomY = availableHeight / contentBounds.Height;

            // 取较小的缩放级别，确保完全可见
            var newZoom = Math.Min(zoomX, zoomY);
            Zoom = newZoom;

            // 计算平移量，使内容居中
            var contentCenterX = contentBounds.X + contentBounds.Width / 2;
            var contentCenterY = contentBounds.Y + contentBounds.Height / 2;

            Translation = new PointF(
                viewportSize.Width / 2 - contentCenterX * _zoom,
                viewportSize.Height / 2 - contentCenterY * _zoom
            );
        }

        /// <summary>
        /// 将指定点移动到视口中心
        /// </summary>
        public void CenterOn(PointF canvasPoint, SizeF viewportSize)
        {
            Translation = new PointF(
                viewportSize.Width / 2 - canvasPoint.X * _zoom,
                viewportSize.Height / 2 - canvasPoint.Y * _zoom
            );
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 重置所有变换
        /// </summary>
        public void Reset()
        {
            ResetZoom();
            ResetPan();
        }

        /// <summary>
        /// 获取当前可见区域（Canvas坐标系）
        /// </summary>
        public RectangleF GetVisibleBounds(SizeF viewportSize)
        {
            var topLeft = ClientToCanvas(0, 0);
            var bottomRight = ClientToCanvas(viewportSize.Width, viewportSize.Height);

            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y
            );
        }

        /// <summary>
        /// 克隆TransformModel
        /// </summary>
        public TransformModel Clone()
        {
            return new TransformModel
            {
                Zoom = this.Zoom,
                Translation = this.Translation,
                MinZoom = this.MinZoom,
                MaxZoom = this.MaxZoom,
                CanvasSize = this.CanvasSize
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
