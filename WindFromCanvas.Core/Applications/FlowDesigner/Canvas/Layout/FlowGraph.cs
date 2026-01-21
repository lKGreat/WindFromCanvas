using System.Collections.Generic;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Edges;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Canvas.Layout
{
    /// <summary>
    /// 流程图（节点和边缘的集合）
    /// </summary>
    public class FlowGraph
    {
        public List<ICanvasNode> Nodes { get; set; }
        public List<ICanvasEdge> Edges { get; set; }

        public FlowGraph()
        {
            Nodes = new List<ICanvasNode>();
            Edges = new List<ICanvasEdge>();
        }

        /// <summary>
        /// 合并另一个图
        /// </summary>
        public void Merge(FlowGraph other)
        {
            Nodes.AddRange(other.Nodes);
            Edges.AddRange(other.Edges);
        }

        /// <summary>
        /// 计算边界框
        /// </summary>
        public BoundingBox CalculateBoundingBox()
        {
            var bbox = new BoundingBox();
            
            foreach (var node in Nodes)
            {
                bbox.ExpandToInclude(node.Bounds);
            }

            return bbox;
        }
    }
}
