using System;
using System.ComponentModel;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Commands;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 构建器状态存储（类似 Zustand Store，使用单例模式）
    /// </summary>
    public class BuilderStateStore : INotifyPropertyChanged, IBuilderState
    {
        private static BuilderStateStore _instance;
        public static BuilderStateStore Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BuilderStateStore();
                return _instance;
            }
        }

        // 状态分片
        public CanvasState Canvas { get; private set; }
        public FlowState Flow { get; private set; }
        public SelectionState Selection { get; private set; }
        public DragState Drag { get; private set; }

        // 中央图数据管理（GraphModel）
        public GraphModel Graph { get; private set; }

        // 命令管理器（用于撤销/重做）
        public CommandManager CommandManager { get; private set; }

        private BuilderStateStore()
        {
            Canvas = new CanvasState();
            Flow = new FlowState();
            Selection = new SelectionState();
            Drag = new DragState();
            Graph = new GraphModel();
            CommandManager = new CommandManager();
            
            // 订阅GraphModel变更，同步到Selection状态
            Graph.PropertyChanged += OnGraphPropertyChanged;
        }
        
        /// <summary>
        /// GraphModel属性变更处理
        /// </summary>
        private void OnGraphPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GraphModel.SelectedNodeIds))
            {
                // 同步选择状态
                Selection.SelectedNodeIds.Clear();
                Selection.SelectedNodeIds.AddRange(Graph.SelectedNodeIds);
                OnPropertyChanged(nameof(Selection));
            }
        }

        /// <summary>
        /// 初始化状态（从现有流程版本）
        /// </summary>
        public void Initialize(FlowVersion flowVersion, bool readonlyMode = false)
        {
            Flow.FlowVersion = flowVersion;
            Canvas.Readonly = readonlyMode;
            OnPropertyChanged(nameof(Flow));
            OnPropertyChanged(nameof(Canvas));
        }

        /// <summary>
        /// 应用操作（支持撤销/重做）
        /// </summary>
        public void ApplyOperation(FlowOperationRequest operation, bool recordCommand = true)
        {
            if (Flow?.FlowVersion == null)
            {
                return;
            }

            if (recordCommand)
            {
                // 创建命令并执行（支持撤销/重做）
                var command = new FlowOperationCommand(this, operation);
                CommandManager.Execute(command);
            }
            else
            {
                // 直接执行操作（不记录命令，用于撤销/重做内部调用）
                var executor = new Core.Operations.FlowOperationExecutor();
                Flow.FlowVersion = executor.Execute(Flow.FlowVersion, operation);
                
                // 通知所有监听器
                foreach (var listener in Flow.OperationListeners)
                {
                    listener(Flow.FlowVersion, operation);
                }
                
                OnPropertyChanged(nameof(Flow));
            }
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public void Undo()
        {
            CommandManager.Undo();
            OnPropertyChanged(nameof(Flow));
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        public void Redo()
        {
            CommandManager.Redo();
            OnPropertyChanged(nameof(Flow));
        }

        /// <summary>
        /// 选择步骤
        /// </summary>
        public void SelectStep(string stepName)
        {
            Canvas.SelectedStep = stepName;
            Canvas.SelectedNodes = new[] { stepName };
            Canvas.RightSidebar = RightSideBarType.PIECE_SETTINGS;
            OnPropertyChanged(nameof(Canvas));
        }

        /// <summary>
        /// 清除步骤选择
        /// </summary>
        public void ClearStepSelection()
        {
            Canvas.SelectedStep = null;
            Canvas.SelectedNodes = new string[0];
            Canvas.RightSidebar = RightSideBarType.NONE;
            Canvas.SelectedBranchIndex = null;
            OnPropertyChanged(nameof(Canvas));
        }

        /// <summary>
        /// 设置选中的节点
        /// </summary>
        public void SetSelectedNodes(string[] nodeIds)
        {
            Canvas.SelectedNodes = nodeIds;
            Selection.SelectedNodeIds.Clear();
            Selection.SelectedNodeIds.AddRange(nodeIds);
            OnPropertyChanged(nameof(Canvas));
            OnPropertyChanged(nameof(Selection));
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void StartDrag(string itemId, System.Drawing.PointF position)
        {
            Drag.StartDrag(itemId, position);
            Canvas.ActiveDraggingStep = itemId;
            OnPropertyChanged(nameof(Drag));
            OnPropertyChanged(nameof(Canvas));
        }

        /// <summary>
        /// 更新拖拽
        /// </summary>
        public void UpdateDrag(System.Drawing.PointF position, string hoveredTargetId = null)
        {
            Drag.UpdateDrag(position, hoveredTargetId);
            OnPropertyChanged(nameof(Drag));
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag()
        {
            Drag.EndDrag();
            Canvas.ActiveDraggingStep = null;
            OnPropertyChanged(nameof(Drag));
            OnPropertyChanged(nameof(Canvas));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
