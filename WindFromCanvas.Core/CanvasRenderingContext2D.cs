using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Rendering;
using WindFromCanvas.Core.Styles;

namespace WindFromCanvas.Core
{
    public class CanvasRenderingContext2D
    {
        private readonly Canvas _canvas;
        private GraphicsPath _currentPath;
        private PointF _currentPoint;
        private Stack<DrawingState> _stateStack = new Stack<DrawingState>();
        private Matrix _transform = new Matrix();

        public IFillStyle FillStyle { get; set; } = new SolidColorStyle(Color.Black);
        public IFillStyle StrokeStyle { get; set; } = new SolidColorStyle(Color.Black);
        public float LineWidth { get; set; } = 1f;
        public float GlobalAlpha { get; set; } = 1f;
        public string Font { get; set; } = "12px Arial";
        public TextAlign TextAlign { get; set; } = TextAlign.Left;
        public TextBaseline TextBaseline { get; set; } = TextBaseline.Top;

        private DrawingState CurrentState
        {
            get
            {
                return new DrawingState
                {
                    Transform = _transform.Clone() as Matrix,
                    FillStyle = FillStyle,
                    StrokeStyle = StrokeStyle,
                    LineWidth = LineWidth,
                    GlobalAlpha = GlobalAlpha,
                    Font = ParseFont(Font),
                    TextAlign = TextAlign,
                    TextBaseline = TextBaseline
                };
            }
        }

        public CanvasRenderingContext2D(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void FillRect(float x, float y, float width, float height)
        {
            var fillStyle = FillStyle as SolidColorStyle;
            var color = fillStyle != null ? fillStyle.Color : Color.Black;
            
            var rect = new RectangleObject
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FillColor = ApplyAlpha(color),
                IsFilled = true,
                IsStroked = false,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(rect);
        }

        public void StrokeRect(float x, float y, float width, float height)
        {
            var strokeStyle = StrokeStyle as SolidColorStyle;
            var color = strokeStyle != null ? strokeStyle.Color : Color.Black;
            
            var rect = new RectangleObject
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                StrokeColor = ApplyAlpha(color),
                StrokeWidth = LineWidth,
                IsFilled = false,
                IsStroked = true,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(rect);
        }

        public void ClearRect(float x, float y, float width, float height)
        {
            var rect = new RectangleObject
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FillColor = _canvas.BackgroundColor,
                IsFilled = true,
                IsStroked = false,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(rect);
        }

        public void FillEllipse(float cx, float cy, float rx, float ry)
        {
            var fillStyle = FillStyle as SolidColorStyle;
            var color = fillStyle != null ? fillStyle.Color : Color.Black;
            
            var ellipse = new EllipseObject
            {
                X = cx,
                Y = cy,
                RadiusX = rx,
                RadiusY = ry,
                FillColor = ApplyAlpha(color),
                IsFilled = true,
                IsStroked = false,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(ellipse);
        }

        public void StrokeEllipse(float cx, float cy, float rx, float ry)
        {
            var strokeStyle = StrokeStyle as SolidColorStyle;
            var color = strokeStyle != null ? strokeStyle.Color : Color.Black;
            
            var ellipse = new EllipseObject
            {
                X = cx,
                Y = cy,
                RadiusX = rx,
                RadiusY = ry,
                StrokeColor = ApplyAlpha(color),
                StrokeWidth = LineWidth,
                IsFilled = false,
                IsStroked = true,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(ellipse);
        }

        public void FillCircle(float cx, float cy, float radius)
        {
            FillEllipse(cx, cy, radius, radius);
        }

        public void StrokeCircle(float cx, float cy, float radius)
        {
            StrokeEllipse(cx, cy, radius, radius);
        }

        public void BeginPath()
        {
            _currentPath = new GraphicsPath();
        }

        public void MoveTo(float x, float y)
        {
            _currentPoint = new PointF(x, y);
        }

        public void LineTo(float x, float y)
        {
            if (_currentPath == null)
                BeginPath();
            
            _currentPath.AddLine(_currentPoint, new PointF(x, y));
            _currentPoint = new PointF(x, y);
        }

        public void Arc(float x, float y, float radius, float startAngle, float endAngle, bool anticlockwise = false)
        {
            if (_currentPath == null)
                BeginPath();
            
            float sweepAngle = endAngle - startAngle;
            if (anticlockwise && sweepAngle > 0)
                sweepAngle -= (float)(2 * Math.PI);
            else if (!anticlockwise && sweepAngle < 0)
                sweepAngle += (float)(2 * Math.PI);
            
            float startDegrees = startAngle * 180f / (float)Math.PI;
            float sweepDegrees = sweepAngle * 180f / (float)Math.PI;
            
            var rect = new RectangleF(x - radius, y - radius, radius * 2, radius * 2);
            _currentPath.AddArc(rect, startDegrees, sweepDegrees);
            
            // 更新当前点
            float endX = x + radius * (float)Math.Cos(endAngle);
            float endY = y + radius * (float)Math.Sin(endAngle);
            _currentPoint = new PointF(endX, endY);
        }

        public void QuadraticCurveTo(float cpx, float cpy, float x, float y)
        {
            if (_currentPath == null)
                BeginPath();
            
            // 二次贝塞尔曲线转换为三次贝塞尔曲线
            PointF cp1 = new PointF(
                _currentPoint.X + 2f / 3f * (cpx - _currentPoint.X),
                _currentPoint.Y + 2f / 3f * (cpy - _currentPoint.Y)
            );
            PointF cp2 = new PointF(
                x + 2f / 3f * (cpx - x),
                y + 2f / 3f * (cpy - y)
            );
            
            _currentPath.AddBezier(_currentPoint, cp1, cp2, new PointF(x, y));
            _currentPoint = new PointF(x, y);
        }

        public void BezierCurveTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y)
        {
            if (_currentPath == null)
                BeginPath();
            
            _currentPath.AddBezier(_currentPoint, new PointF(cp1x, cp1y), new PointF(cp2x, cp2y), new PointF(x, y));
            _currentPoint = new PointF(x, y);
        }

        public void ClosePath()
        {
            if (_currentPath != null && _currentPath.PointCount > 0)
            {
                _currentPath.CloseFigure();
            }
        }

        public void Stroke()
        {
            if (_currentPath == null || _currentPath.PointCount == 0) return;
            
            var strokeStyle = StrokeStyle as SolidColorStyle;
            var color = strokeStyle != null ? strokeStyle.Color : Color.Black;
            
            var pathObj = new PathObject
            {
                Path = (GraphicsPath)_currentPath.Clone(),
                StrokeColor = ApplyAlpha(color),
                StrokeWidth = LineWidth,
                IsFilled = false,
                IsStroked = true,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(pathObj);
            _currentPath = null;
        }

        public void Fill()
        {
            if (_currentPath == null || _currentPath.PointCount == 0) return;
            
            var fillStyle = FillStyle as SolidColorStyle;
            var color = fillStyle != null ? fillStyle.Color : Color.Black;
            
            var pathObj = new PathObject
            {
                Path = (GraphicsPath)_currentPath.Clone(),
                FillColor = ApplyAlpha(color),
                IsFilled = true,
                IsStroked = false,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(pathObj);
            _currentPath = null;
        }

        public void FillText(string text, float x, float y)
        {
            var fillStyle = FillStyle as SolidColorStyle;
            var color = fillStyle != null ? fillStyle.Color : Color.Black;
            
            var font = ParseFont(Font);
            var textObj = new TextObject
            {
                Text = text,
                X = x,
                Y = y,
                Font = font,
                TextAlign = TextAlign,
                TextBaseline = TextBaseline,
                FillColor = ApplyAlpha(color),
                IsFilled = true,
                IsStroked = false,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(textObj);
        }

        public void StrokeText(string text, float x, float y)
        {
            var strokeStyle = StrokeStyle as SolidColorStyle;
            var color = strokeStyle != null ? strokeStyle.Color : Color.Black;
            
            var font = ParseFont(Font);
            var textObj = new TextObject
            {
                Text = text,
                X = x,
                Y = y,
                Font = font,
                TextAlign = TextAlign,
                TextBaseline = TextBaseline,
                StrokeColor = ApplyAlpha(color),
                StrokeWidth = LineWidth,
                IsFilled = false,
                IsStroked = true,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(textObj);
        }

        public SizeF MeasureText(string text)
        {
            var font = ParseFont(Font);
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                return g.MeasureString(text, font);
            }
        }

        public void DrawImage(Image image, float x, float y)
        {
            if (image == null) return;
            
            var imgObj = new ImageObject
            {
                Image = image,
                X = x,
                Y = y,
                Width = 0,  // 使用原始大小
                Height = 0,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(imgObj);
        }

        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            if (image == null) return;
            
            var imgObj = new ImageObject
            {
                Image = image,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(imgObj);
        }

        public void DrawImage(Image image, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh)
        {
            if (image == null) return;
            
            var imgObj = new ImageObject
            {
                Image = image,
                X = dx,
                Y = dy,
                Width = dw,
                Height = dh,
                SourceRect = new RectangleF(sx, sy, sw, sh),
                ZIndex = _canvas.Objects.Count
            };
            
            _canvas.AddObject(imgObj);
        }

        private Font ParseFont(string fontString)
        {
            // 简单解析 "12px Arial" 格式
            if (string.IsNullOrEmpty(fontString))
                return new Font("Arial", 12f);
            
            var parts = fontString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            float size = 12f;
            string family = "Arial";
            
            foreach (var part in parts)
            {
                if (part.EndsWith("px"))
                {
                    if (float.TryParse(part.Substring(0, part.Length - 2), out float parsedSize))
                        size = parsedSize;
                }
                else
                {
                    family = part;
                }
            }
            
            return new Font(family, size);
        }

        public void Save()
        {
            _stateStack.Push(CurrentState);
        }

        public void Restore()
        {
            if (_stateStack.Count > 0)
            {
                var state = _stateStack.Pop();
                RestoreState(state);
            }
        }

        private void RestoreState(DrawingState state)
        {
            if (state.Transform != null)
            {
                _transform = state.Transform.Clone() as Matrix;
            }
            FillStyle = state.FillStyle;
            StrokeStyle = state.StrokeStyle;
            LineWidth = state.LineWidth;
            GlobalAlpha = state.GlobalAlpha;
            if (state.Font != null)
            {
                Font = $"{state.Font.Size}px {state.Font.FontFamily.Name}";
            }
            TextAlign = state.TextAlign;
            TextBaseline = state.TextBaseline;
        }

        public void Translate(float x, float y)
        {
            _transform.Translate(x, y);
        }

        public void Rotate(float angle)
        {
            _transform.Rotate(angle * 180f / (float)Math.PI);
        }

        public void Scale(float x, float y)
        {
            _transform.Scale(x, y);
        }

        public void SetTransform(float a, float b, float c, float d, float e, float f)
        {
            _transform.Reset();
            _transform.Elements[0] = a;
            _transform.Elements[1] = b;
            _transform.Elements[2] = c;
            _transform.Elements[3] = d;
            _transform.Elements[4] = e;
            _transform.Elements[5] = f;
        }

        public void ResetTransform()
        {
            _transform.Reset();
        }

        public LinearGradientStyle CreateLinearGradient(float x0, float y0, float x1, float y1)
        {
            return new LinearGradientStyle
            {
                Start = new PointF(x0, y0),
                End = new PointF(x1, y1)
            };
        }

        private Color ApplyAlpha(Color color)
        {
            if (GlobalAlpha >= 1f)
                return color;
            
            return Color.FromArgb((int)(color.A * GlobalAlpha), color.R, color.G, color.B);
        }
    }
}
