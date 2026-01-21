using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Widgets
{
    /// <summary>
    /// 小地图控件（匹配 Activepieces Minimap）
    /// </summary>
    public class Minimap : Control
    {
        private FlowGraph _graph;
        private RectangleF _viewportRect;
        private float _scale;

        public Minimap()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
            
            BackColor = Color.FromArgb(240, 240, 240);
            Size = new Size(200, 150);
        }

        /// <summary>
        /// 更新小地图数据
        /// </summary>
        public void UpdateMap(FlowGraph graph, RectangleF viewportRect, float scale)
        {
            _graph = graph;
            _viewportRect = viewportRect;
            _scale = scale;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (_graph == null)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 计算缩放比例
            var graphBounds = _graph.CalculateBoundingBox();
            var graphRect = graphBounds.ToRectangle();
            
            var scaleX = Width / graphRect.Width;
            var scaleY = Height / graphRect.Height;
            var mapScale = Math.Min(scaleX, scaleY) * 0.9f; // 留出边距

            // 绘制节点（简化表示）
            foreach (var node in _graph.Nodes)
            {
                if (node is Canvas.Nodes.StepNode)
                {
                    var nodeRect = node.Bounds;
                    var mapX = (nodeRect.X - graphRect.X) * mapScale + Width * 0.05f;
                    var mapY = (nodeRect.Y - graphRect.Y) * mapScale + Height * 0.05f;
                    var mapWidth = nodeRect.Width * mapScale;
                    var mapHeight = nodeRect.Height * mapScale;

                    using (var brush = new SolidBrush(Color.FromArgb(100, 59, 130, 246)))
                    {
                        g.FillRectangle(brush, mapX, mapY, mapWidth, mapHeight);
                    }
                }
            }

            // 绘制视口矩形
            var viewportMapX = (_viewportRect.X - graphRect.X) * mapScale + Width * 0.05f;
            var viewportMapY = (_viewportRect.Y - graphRect.Y) * mapScale + Height * 0.05f;
            var viewportMapWidth = _viewportRect.Width * mapScale;
            var viewportMapHeight = _viewportRect.Height * mapScale;

            using (var pen = new Pen(Color.FromArgb(200, 59, 130, 246), 2f))
            {
                g.DrawRectangle(pen, viewportMapX, viewportMapY, viewportMapWidth, viewportMapHeight);
            }
        }
    }
}
