using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core;
using WindFromCanvas.Core.Objects;
using WindFromCanvas.Core.Styles;
using WindFromCanvas.Core.Events;

namespace WindFromCanvas
{
    public partial class Form1 : Form
    {
        private RectangleObject _draggableRect;
        private EllipseObject _animatedCircle;
        private float _animationAngle = 0f;

        public Form1()
        {
            InitializeComponent();
            SetupDemo();
            SetupFlowDesigner();
        }

        private void SetupFlowDesigner()
        {
            // 设置工具箱和属性面板
            flowDesignerCanvas1.Toolbox = toolboxPanel1;
            flowDesignerCanvas1.PropertiesPanel = propertiesPanel1;

            // 创建示例流程
            var startNodeData = new WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeData
            {
                Name = "start",
                DisplayName = "开始",
                Type = WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeType.Start,
                Position = new PointF(100, 100)
            };
            var startNode = new WindFromCanvas.Core.Applications.FlowDesigner.Nodes.StartNode(startNodeData);

            var processNodeData = new WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeData
            {
                Name = "process1",
                DisplayName = "处理步骤1",
                Type = WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeType.Process,
                Position = new PointF(300, 100)
            };
            var processNode = new WindFromCanvas.Core.Applications.FlowDesigner.Nodes.ProcessNode(processNodeData);

            var decisionNodeData = new WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeData
            {
                Name = "decision1",
                DisplayName = "判断条件",
                Type = WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeType.Decision,
                Position = new PointF(500, 100)
            };
            var decisionNode = new WindFromCanvas.Core.Applications.FlowDesigner.Nodes.DecisionNode(decisionNodeData);

            var endNodeData = new WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeData
            {
                Name = "end",
                DisplayName = "结束",
                Type = WindFromCanvas.Core.Applications.FlowDesigner.Models.FlowNodeType.End,
                Position = new PointF(700, 100)
            };
            var endNode = new WindFromCanvas.Core.Applications.FlowDesigner.Nodes.EndNode(endNodeData);

            // 添加节点到画布
            flowDesignerCanvas1.AddNode(startNode);
            flowDesignerCanvas1.AddNode(processNode);
            flowDesignerCanvas1.AddNode(decisionNode);
            flowDesignerCanvas1.AddNode(endNode);

            // 创建连接
            flowDesignerCanvas1.CreateConnection(startNode, processNode);
            flowDesignerCanvas1.CreateConnection(processNode, decisionNode);
            flowDesignerCanvas1.CreateConnection(decisionNode, endNode);
        }

        private void SetupDemo()
        {
            var ctx = canvas1.GetContext2D();

            // ========== 演示1: 基本图形绘制 ==========
            DemoBasicShapes(ctx);

            // ========== 演示2: 路径绘制 ==========
            DemoPathDrawing(ctx);

            // ========== 演示3: 文本绘制 ==========
            DemoTextDrawing(ctx);

            // ========== 演示4: 渐变效果 ==========
            DemoGradient(ctx);

            // ========== 演示5: 可拖拽对象 ==========
            DemoDraggableObject();

            // ========== 演示6: 鼠标事件 ==========
            DemoMouseEvents();

            // ========== 演示7: 动画效果 ==========
            DemoAnimation();
        }

        // 演示1: 基本图形绘制
        private void DemoBasicShapes(CanvasRenderingContext2D ctx)
        {
            // 填充矩形
            ctx.FillStyle = new SolidColorStyle(Color.FromArgb(255, 100, 150));
            ctx.FillRect(20, 20, 100, 80);

            // 描边矩形
            ctx.StrokeStyle = new SolidColorStyle(Color.Blue);
            ctx.LineWidth = 3f;
            ctx.StrokeRect(140, 20, 100, 80);

            // 填充圆形
            ctx.FillStyle = new SolidColorStyle(Color.Green);
            ctx.FillCircle(320, 60, 40);

            // 描边椭圆
            ctx.StrokeStyle = new SolidColorStyle(Color.Purple);
            ctx.LineWidth = 2f;
            ctx.StrokeEllipse(400, 40, 60, 40);

            // 带透明度的矩形
            ctx.GlobalAlpha = 0.5f;
            ctx.FillStyle = new SolidColorStyle(Color.Orange);
            ctx.FillRect(480, 20, 100, 80);
            ctx.GlobalAlpha = 1f;
        }

        // 演示2: 路径绘制
        private void DemoPathDrawing(CanvasRenderingContext2D ctx)
        {
            // 绘制三角形
            ctx.BeginPath();
            ctx.MoveTo(20, 150);
            ctx.LineTo(70, 120);
            ctx.LineTo(120, 150);
            ctx.ClosePath();
            ctx.FillStyle = new SolidColorStyle(Color.Cyan);
            ctx.Fill();

            // 绘制星形
            ctx.BeginPath();
            float centerX = 200;
            float centerY = 135;
            float radius = 30;
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)(i * 4 * Math.PI / 5 - Math.PI / 2);
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = centerY + radius * (float)Math.Sin(angle);
                if (i == 0)
                    ctx.MoveTo(x, y);
                else
                    ctx.LineTo(x, y);
            }
            ctx.ClosePath();
            ctx.StrokeStyle = new SolidColorStyle(Color.Red);
            ctx.LineWidth = 2f;
            ctx.Stroke();

            // 绘制圆弧
            ctx.BeginPath();
            ctx.Arc(320, 135, 30, 0, (float)(Math.PI * 1.5), false);
            ctx.LineTo(320, 135);
            ctx.ClosePath();
            ctx.FillStyle = new SolidColorStyle(Color.Yellow);
            ctx.Fill();

            // 绘制贝塞尔曲线
            ctx.BeginPath();
            ctx.MoveTo(400, 120);
            ctx.BezierCurveTo(420, 100, 480, 100, 500, 120);
            ctx.StrokeStyle = new SolidColorStyle(Color.Magenta);
            ctx.LineWidth = 3f;
            ctx.Stroke();
        }

        // 演示3: 文本绘制
        private void DemoTextDrawing(CanvasRenderingContext2D ctx)
        {
            ctx.Font = "16px Arial";
            ctx.FillStyle = new SolidColorStyle(Color.Black);

            // 左对齐文本
            ctx.TextAlign = TextAlign.Left;
            ctx.TextBaseline = TextBaseline.Top;
            ctx.FillText("左对齐文本", 20, 200);

            // 居中对齐文本
            ctx.TextAlign = TextAlign.Center;
            ctx.FillText("居中文本", 200, 200);

            // 右对齐文本
            ctx.TextAlign = TextAlign.Right;
            ctx.FillText("右对齐文本", 380, 200);

            // 不同字体大小
            ctx.Font = "24px Arial";
            ctx.TextAlign = TextAlign.Left;
            ctx.FillStyle = new SolidColorStyle(Color.DarkBlue);
            ctx.FillText("大字体文本", 20, 230);

            // 描边文本
            ctx.Font = "20px Arial";
            ctx.StrokeStyle = new SolidColorStyle(Color.DarkGreen);
            ctx.LineWidth = 1.5f;
            ctx.StrokeText("描边文本", 20, 270);
        }

        // 演示4: 渐变效果
        private void DemoGradient(CanvasRenderingContext2D ctx)
        {
            // 创建线性渐变
            var gradient = ctx.CreateLinearGradient(500, 200, 700, 300);
            gradient.AddColorStop(0f, Color.Red);
            gradient.AddColorStop(0.5f, Color.Yellow);
            gradient.AddColorStop(1f, Color.Green);

            ctx.FillStyle = gradient;
            ctx.FillRect(500, 200, 200, 100);

            // 渐变圆形
            var gradient2 = ctx.CreateLinearGradient(750, 120, 750, 180);
            gradient2.AddColorStop(0f, Color.Blue);
            gradient2.AddColorStop(1f, Color.Cyan);
            ctx.FillStyle = gradient2;
            ctx.FillCircle(750, 150, 30);
        }

        // 演示5: 可拖拽对象
        private void DemoDraggableObject()
        {
            _draggableRect = new RectangleObject
            {
                X = 50,
                Y = 350,
                Width = 100,
                Height = 80,
                FillColor = Color.FromArgb(200, 100, 200, 255),
                StrokeColor = Color.DarkBlue,
                StrokeWidth = 2f,
                IsFilled = true,
                IsStroked = true,
                Draggable = true,
                ZIndex = 10
            };

            canvas1.AddObject(_draggableRect);

            // 添加拖拽事件处理
            _draggableRect.DragStart += (s, e) =>
            {
                _draggableRect.FillColor = Color.FromArgb(255, 150, 200, 255);
            };

            _draggableRect.DragEnd += (s, e) =>
            {
                _draggableRect.FillColor = Color.FromArgb(200, 100, 200, 255);
            };
        }

        // 演示6: 鼠标事件
        private void DemoMouseEvents()
        {
            var interactiveRect = new RectangleObject
            {
                X = 200,
                Y = 350,
                Width = 120,
                Height = 80,
                FillColor = Color.LightGreen,
                StrokeColor = Color.DarkGreen,
                StrokeWidth = 2f,
                IsFilled = true,
                IsStroked = true,
                ZIndex = 5
            };

            canvas1.AddObject(interactiveRect);

            // 鼠标悬停效果
            Color originalColor = interactiveRect.FillColor;
            interactiveRect.MouseEnter += (s, e) =>
            {
                interactiveRect.FillColor = Color.Green;
                canvas1.Invalidate();
            };

            interactiveRect.MouseLeave += (s, e) =>
            {
                interactiveRect.FillColor = originalColor;
                canvas1.Invalidate();
            };

            // 点击事件
            interactiveRect.Click += (s, e) =>
            {
                MessageBox.Show("矩形被点击了！", "事件演示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        // 演示7: 动画效果
        private void DemoAnimation()
        {
            _animatedCircle = new EllipseObject
            {
                X = 400,
                Y = 350,
                RadiusX = 30,
                RadiusY = 30,
                FillColor = Color.Orange,
                StrokeColor = Color.Red,
                StrokeWidth = 2f,
                IsFilled = true,
                IsStroked = true,
                ZIndex = 8
            };

            canvas1.AddObject(_animatedCircle);

            // 启动动画
            canvas1.StartAnimation((deltaTime) =>
            {
                // 圆形旋转移动
                _animationAngle += (float)(deltaTime * 0.001);
                float radius = 50f;
                _animatedCircle.X = 450 + radius * (float)Math.Cos(_animationAngle);
                _animatedCircle.Y = 390 + radius * (float)Math.Sin(_animationAngle);

                // 颜色变化
                int r = (int)(128 + 127 * Math.Sin(_animationAngle));
                int g = (int)(128 + 127 * Math.Sin(_animationAngle + Math.PI * 2 / 3));
                int b = (int)(128 + 127 * Math.Sin(_animationAngle + Math.PI * 4 / 3));
                _animatedCircle.FillColor = Color.FromArgb(r, g, b);
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 停止动画
            canvas1.StopAnimation();
            base.OnFormClosed(e);
        }
    }
}
