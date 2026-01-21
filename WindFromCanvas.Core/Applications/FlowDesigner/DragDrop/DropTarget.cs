using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;

namespace WindFromCanvas.Core.Applications.FlowDesigner.DragDrop
{
    /// <summary>
    /// 放置目标（表示可以放置动作的位置）
    /// </summary>
    public class DropTarget : IDropTarget
    {
        public string Id { get; set; }
        public string AcceptsDragType { get; set; }
        public string ParentStepName { get; set; }
        public StepLocationRelativeToParent Location { get; set; }
        public int? BranchIndex { get; set; }

        public DropTarget(string id, string parentStepName, StepLocationRelativeToParent location, int? branchIndex = null)
        {
            Id = id;
            ParentStepName = parentStepName;
            Location = location;
            BranchIndex = branchIndex;
            AcceptsDragType = "NODE";
        }

        public bool CanAccept(IDraggable draggable)
        {
            return draggable?.DragType == AcceptsDragType;
        }
    }
}
