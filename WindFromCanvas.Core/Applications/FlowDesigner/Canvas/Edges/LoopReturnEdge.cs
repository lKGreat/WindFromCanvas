using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges
{
    /// <summary>
    /// 循环返回边缘（匹配 Activepieces LoopReturnEdge）
    /// </summary>
    public class LoopReturnEdge : ICanvasEdge
    {
        public string Id { get; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public string ParentStepName { get; set; }
        public bool IsLoopEmpty { get; set; }
        public bool DrawArrowHeadAfterEnd { get; set; }
        public float VerticalSpaceBetweenReturnNodeStartAndEnd { get; set; }

        public LoopReturnEdge(string id)
        {
            Id = id;
        }

        public void Draw(Graphics g, float zoom, PointF sourcePos, PointF targetPos)
        {
            // 绘制从循环子节点返回到循环节点的弧线
            var arcLength = LayoutConstants.ARC_LENGTH / zoom;
            
            using (var path = new GraphicsPath())
            {
                // 创建返回路径（从子节点底部返回到循环节点）
                path.AddBezier(
                    sourcePos.X, sourcePos.Y,
                    sourcePos.X, sourcePos.Y + arcLength,
                    targetPos.X, sourcePos.Y + VerticalSpaceBetweenReturnNodeStartAndEnd / zoom - arcLength,
                    targetPos.X, targetPos.Y
                );

                using (var pen = new Pen(Color.FromArgb(200, 200, 200), LayoutConstants.LINE_WIDTH / zoom))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 如果需要，绘制箭头
            if (DrawArrowHeadAfterEnd)
            {
                DrawArrowHead(g, sourcePos, targetPos, zoom);
            }
        }

        private void DrawArrowHead(Graphics g, PointF source, PointF target, float zoom)
        {
            var angle = Math.Atan2(target.Y - source.Y, target.X - source.X);
            var arrowLength = 8f / zoom;
            var arrowWidth = 6f / zoom;

            var arrowPoint1 = new PointF(
                (float)(target.X - arrowLength * Math.Cos(angle - Math.PI / 6)),
                (float)(target.Y - arrowLength * Math.Sin(angle - Math.PI / 6))
            );
            var arrowPoint2 = new PointF(
                (float)(target.X - arrowLength * Math.Cos(angle + Math.PI / 6)),
                (float)(target.Y - arrowLength * Math.Sin(angle + Math.PI / 6))
            );

            using (var brush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                var points = new[] { target, arrowPoint1, arrowPoint2 };
                g.FillPolygon(brush, points);
            }
        }
    }
}
