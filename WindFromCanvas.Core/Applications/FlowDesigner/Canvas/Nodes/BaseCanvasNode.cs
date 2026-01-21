using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes
{
    /// <summary>
    /// 画布节点基类
    /// </summary>
    public abstract class BaseCanvasNode : ICanvasNode
    {
        public string Id { get; protected set; }
        public PointF Position { get; set; }
        public abstract SizeF Size { get; }
        public bool Selectable { get; set; }
        public bool Draggable { get; set; }
        public bool IsSelected { get; set; }

        public RectangleF Bounds => new RectangleF(Position, Size);

        protected BaseCanvasNode(string id)
        {
            Id = id;
            Selectable = true;
            Draggable = false;
            IsSelected = false;
        }

        public abstract void Draw(Graphics g, float zoom);

        public virtual bool Contains(PointF point)
        {
            return Bounds.Contains(point);
        }

        /// <summary>
        /// 绘制选中边框
        /// </summary>
        protected void DrawSelectionBorder(Graphics g, RectangleF bounds, float zoom)
        {
            if (!IsSelected) return;

            using (var pen = new Pen(Color.FromArgb(255, 59, 130, 246), 2f / zoom))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }
    }
}
