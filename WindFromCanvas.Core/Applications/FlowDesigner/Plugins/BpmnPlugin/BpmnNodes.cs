using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Nodes;
using WindFromCanvas.Core.Applications.FlowDesigner.Themes;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Plugins.BpmnPlugin
{
    /// <summary>
    /// BPMN节点类型枚举
    /// </summary>
    public enum BpmnNodeType
    {
        StartEvent,
        EndEvent,
        IntermediateEvent,
        UserTask,
        ServiceTask,
        ScriptTask,
        ManualTask,
        ExclusiveGateway,
        ParallelGateway,
        InclusiveGateway,
        EventBasedGateway,
        SubProcess,
        CallActivity
    }

    /// <summary>
    /// BPMN节点数据
    /// </summary>
    public class BpmnNodeData : FlowNodeData
    {
        public BpmnNodeType BpmnType { get; set; }
        public string BpmnId { get; set; }
        public string Documentation { get; set; }
        public Dictionary<string, string> Extensions { get; set; } = new Dictionary<string, string>();
        
        // 任务特定属性
        public string Assignee { get; set; }
        public string CandidateUsers { get; set; }
        public string CandidateGroups { get; set; }
        public string FormKey { get; set; }
        
        // 服务任务特定属性
        public string Implementation { get; set; }
        public string OperationRef { get; set; }
        
        // 脚本任务特定属性
        public string ScriptFormat { get; set; }
        public string Script { get; set; }
        
        // 网关特定属性
        public string DefaultFlow { get; set; }
    }

    /// <summary>
    /// BPMN节点基类
    /// </summary>
    public abstract class BpmnNode : FlowNode
    {
        protected BpmnNodeData BpmnData => Data as BpmnNodeData;

        protected BpmnNode() : base() { }
        protected BpmnNode(BpmnNodeData data) : base(data) { }

        public abstract BpmnNodeType BpmnType { get; }
    }

    #region 事件节点

    /// <summary>
    /// 6.2.1 开始事件节点
    /// </summary>
    public class StartEventNode : BpmnNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.StartEvent;
        public override float Width { get; set; } = 36;
        public override float Height { get; set; } = 36;

        public StartEventNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.StartEvent, Type = FlowNodeType.Start };
        }

        public StartEventNode(BpmnNodeData data) : base(data) { }

        public override void Draw(Graphics g)
        {
            if (!Visible) return;

            var theme = ThemeManager.Instance.CurrentTheme;
            var rect = new RectangleF(X, Y, Width, Height);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制圆形（开始事件使用细边框）
            using (var fillBrush = new SolidBrush(Color.FromArgb(200, 230, 201))) // 淡绿色
            using (var borderPen = new Pen(Color.FromArgb(67, 160, 71), IsSelected ? 3 : 2))
            {
                g.FillEllipse(fillBrush, rect);
                g.DrawEllipse(borderPen, rect);
            }

            // 如果选中，绘制选中效果
            if (IsSelected)
            {
                using (var selectPen = new Pen(theme.Primary, 1) { DashStyle = DashStyle.Dash })
                {
                    var selectRect = rect;
                    selectRect.Inflate(4, 4);
                    g.DrawEllipse(selectPen, selectRect);
                }
            }

            DrawPorts(g);
        }

        /// <summary>
        /// 开始事件只能作为源，可以连接到任何节点
        /// </summary>
        public List<FlowNodeType> GetConnectedSourceRules()
        {
            return null;
        }

        /// <summary>
        /// 开始事件不能作为目标
        /// </summary>
        public List<FlowNodeType> GetConnectedTargetRules()
        {
            return new List<FlowNodeType>();
        }
    }

    /// <summary>
    /// 6.2.2 结束事件节点
    /// </summary>
    public class EndEventNode : BpmnNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.EndEvent;
        public override float Width { get; set; } = 36;
        public override float Height { get; set; } = 36;

        public EndEventNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.EndEvent, Type = FlowNodeType.End };
        }

        public EndEventNode(BpmnNodeData data) : base(data) { }

        public override void Draw(Graphics g)
        {
            if (!Visible) return;

            var theme = ThemeManager.Instance.CurrentTheme;
            var rect = new RectangleF(X, Y, Width, Height);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制圆形（结束事件使用粗边框）
            using (var fillBrush = new SolidBrush(Color.FromArgb(255, 205, 210))) // 淡红色
            using (var borderPen = new Pen(Color.FromArgb(229, 57, 53), IsSelected ? 5 : 4))
            {
                g.FillEllipse(fillBrush, rect);
                g.DrawEllipse(borderPen, rect);
            }

            if (IsSelected)
            {
                using (var selectPen = new Pen(theme.Primary, 1) { DashStyle = DashStyle.Dash })
                {
                    var selectRect = rect;
                    selectRect.Inflate(4, 4);
                    g.DrawEllipse(selectPen, selectRect);
                }
            }

            DrawPorts(g);
        }

        /// <summary>
        /// 结束事件不能作为源
        /// </summary>
        public List<FlowNodeType> GetConnectedSourceRules()
        {
            return new List<FlowNodeType>();
        }

        /// <summary>
        /// 结束事件可以从任何节点连入
        /// </summary>
        public List<FlowNodeType> GetConnectedTargetRules()
        {
            return null;
        }
    }

    #endregion

    #region 任务节点

    /// <summary>
    /// BPMN任务节点基类
    /// </summary>
    public abstract class BpmnTaskNode : BpmnNode
    {
        public override float Width { get; set; } = 100;
        public override float Height { get; set; } = 80;
        public override float CornerRadius { get; set; } = 10;

        protected BpmnTaskNode() : base() { }
        protected BpmnTaskNode(BpmnNodeData data) : base(data) { }

        protected abstract Color TaskColor { get; }
        protected abstract string TaskIconText { get; }

        public override void Draw(Graphics g)
        {
            if (!Visible) return;

            var theme = ThemeManager.Instance.CurrentTheme;
            var rect = new RectangleF(X, Y, Width, Height);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = CreateRoundedRectangle(rect, CornerRadius))
            {
                // 填充背景
                using (var fillBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(fillBrush, path);
                }

                // 绘制边框
                var borderColor = IsSelected ? theme.Primary : TaskColor;
                using (var borderPen = new Pen(borderColor, IsSelected ? 2.5f : 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 绘制任务图标（左上角）
            DrawTaskIcon(g, new RectangleF(X + 5, Y + 5, 16, 16));

            // 绘制任务名称
            DrawTaskName(g, rect);

            DrawPorts(g);
        }

        protected virtual void DrawTaskIcon(Graphics g, RectangleF iconRect)
        {
            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(TaskColor))
            {
                g.DrawString(TaskIconText, font, brush, iconRect.X, iconRect.Y);
            }
        }

        protected virtual void DrawTaskName(Graphics g, RectangleF rect)
        {
            var name = Data?.DisplayName ?? Data?.Name ?? "Task";
            using (var font = new Font("Segoe UI", 9))
            using (var brush = new SolidBrush(Color.Black))
            {
                var textRect = new RectangleF(rect.X + 5, rect.Y + 25, rect.Width - 10, rect.Height - 30);
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(name, font, brush, textRect, format);
            }
        }
    }

    /// <summary>
    /// 6.2.3 用户任务节点
    /// </summary>
    public class UserTaskNode : BpmnTaskNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.UserTask;
        protected override Color TaskColor => Color.FromArgb(255, 152, 0); // 橙色
        protected override string TaskIconText => "👤";

        public UserTaskNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.UserTask, Type = FlowNodeType.Process };
        }

        public UserTaskNode(BpmnNodeData data) : base(data) { }

        protected override void DrawTaskIcon(Graphics g, RectangleF iconRect)
        {
            // 绘制用户图标（简化版头像）
            using (var pen = new Pen(TaskColor, 1.5f))
            {
                // 头部
                g.DrawEllipse(pen, iconRect.X + 4, iconRect.Y, 8, 8);
                // 身体
                g.DrawArc(pen, iconRect.X, iconRect.Y + 8, 16, 12, 0, -180);
            }
        }
    }

    /// <summary>
    /// 6.2.4 服务任务节点
    /// </summary>
    public class ServiceTaskNode : BpmnTaskNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.ServiceTask;
        protected override Color TaskColor => Color.FromArgb(33, 150, 243); // 蓝色
        protected override string TaskIconText => "⚙";

        public ServiceTaskNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.ServiceTask, Type = FlowNodeType.Process };
        }

        public ServiceTaskNode(BpmnNodeData data) : base(data) { }

        protected override void DrawTaskIcon(Graphics g, RectangleF iconRect)
        {
            // 绘制齿轮图标
            using (var pen = new Pen(TaskColor, 1.5f))
            using (var brush = new SolidBrush(TaskColor))
            {
                var centerX = iconRect.X + iconRect.Width / 2;
                var centerY = iconRect.Y + iconRect.Height / 2;
                
                // 外圆
                g.DrawEllipse(pen, iconRect.X + 2, iconRect.Y + 2, 12, 12);
                // 内圆
                g.FillEllipse(brush, iconRect.X + 5, iconRect.Y + 5, 6, 6);
            }
        }
    }

    /// <summary>
    /// 脚本任务节点
    /// </summary>
    public class ScriptTaskNode : BpmnTaskNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.ScriptTask;
        protected override Color TaskColor => Color.FromArgb(156, 39, 176); // 紫色
        protected override string TaskIconText => "📜";

        public ScriptTaskNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.ScriptTask, Type = FlowNodeType.Code };
        }

        public ScriptTaskNode(BpmnNodeData data) : base(data) { }
    }

    #endregion

    #region 网关节点

    /// <summary>
    /// BPMN网关节点基类
    /// </summary>
    public abstract class BpmnGatewayNode : BpmnNode
    {
        public override float Width { get; set; } = 50;
        public override float Height { get; set; } = 50;

        protected BpmnGatewayNode() : base() { }
        protected BpmnGatewayNode(BpmnNodeData data) : base(data) { }

        protected abstract Color GatewayColor { get; }

        public override void Draw(Graphics g)
        {
            if (!Visible) return;

            var theme = ThemeManager.Instance.CurrentTheme;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制菱形
            var centerX = X + Width / 2;
            var centerY = Y + Height / 2;
            var halfWidth = Width / 2;
            var halfHeight = Height / 2;

            var points = new PointF[]
            {
                new PointF(centerX, Y),           // 上
                new PointF(X + Width, centerY),   // 右
                new PointF(centerX, Y + Height),  // 下
                new PointF(X, centerY)            // 左
            };

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(points);

                using (var fillBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(fillBrush, path);
                }

                var borderColor = IsSelected ? theme.Primary : GatewayColor;
                using (var borderPen = new Pen(borderColor, IsSelected ? 2.5f : 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 绘制网关标识
            DrawGatewayMarker(g, centerX, centerY);

            DrawPorts(g);
        }

        protected abstract void DrawGatewayMarker(Graphics g, float centerX, float centerY);
    }

    /// <summary>
    /// 6.2.5 排他网关节点
    /// </summary>
    public class ExclusiveGatewayNode : BpmnGatewayNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.ExclusiveGateway;
        protected override Color GatewayColor => Color.FromArgb(255, 193, 7); // 黄色

        public ExclusiveGatewayNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.ExclusiveGateway, Type = FlowNodeType.Decision };
        }

        public ExclusiveGatewayNode(BpmnNodeData data) : base(data) { }

        protected override void DrawGatewayMarker(Graphics g, float centerX, float centerY)
        {
            // 绘制X标记
            using (var pen = new Pen(GatewayColor, 3))
            {
                var size = 10;
                g.DrawLine(pen, centerX - size, centerY - size, centerX + size, centerY + size);
                g.DrawLine(pen, centerX + size, centerY - size, centerX - size, centerY + size);
            }
        }
    }

    /// <summary>
    /// 6.2.6 并行网关节点
    /// </summary>
    public class ParallelGatewayNode : BpmnGatewayNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.ParallelGateway;
        protected override Color GatewayColor => Color.FromArgb(76, 175, 80); // 绿色

        public ParallelGatewayNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.ParallelGateway, Type = FlowNodeType.Decision };
        }

        public ParallelGatewayNode(BpmnNodeData data) : base(data) { }

        protected override void DrawGatewayMarker(Graphics g, float centerX, float centerY)
        {
            // 绘制+标记
            using (var pen = new Pen(GatewayColor, 3))
            {
                var size = 12;
                g.DrawLine(pen, centerX, centerY - size, centerX, centerY + size);
                g.DrawLine(pen, centerX - size, centerY, centerX + size, centerY);
            }
        }
    }

    /// <summary>
    /// 包容网关节点
    /// </summary>
    public class InclusiveGatewayNode : BpmnGatewayNode
    {
        public override BpmnNodeType BpmnType => BpmnNodeType.InclusiveGateway;
        protected override Color GatewayColor => Color.FromArgb(255, 152, 0); // 橙色

        public InclusiveGatewayNode() : base()
        {
            Data = new BpmnNodeData { BpmnType = BpmnNodeType.InclusiveGateway, Type = FlowNodeType.Decision };
        }

        public InclusiveGatewayNode(BpmnNodeData data) : base(data) { }

        protected override void DrawGatewayMarker(Graphics g, float centerX, float centerY)
        {
            // 绘制圆圈标记
            using (var pen = new Pen(GatewayColor, 3))
            {
                var size = 10;
                g.DrawEllipse(pen, centerX - size, centerY - size, size * 2, size * 2);
            }
        }
    }

    #endregion

    #region 连线数据

    /// <summary>
    /// BPMN顺序流数据
    /// </summary>
    public class BpmnSequenceFlowData : FlowConnectionData
    {
        public string BpmnId { get; set; }
        public string ConditionExpression { get; set; }
        public bool IsDefault { get; set; }
        public List<PointF> WayPoints { get; set; } = new List<PointF>();
        
        /// <summary>
        /// BPMN名称（用于条件显示）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 源节点名称（BPMN XML中的sourceRef）
        /// </summary>
        public string SourceNodeName
        {
            get => SourceNode;
            set => SourceNode = value;
        }

        /// <summary>
        /// 目标节点名称（BPMN XML中的targetRef）
        /// </summary>
        public string TargetNodeName
        {
            get => TargetNode;
            set => TargetNode = value;
        }
    }

    #endregion
}
