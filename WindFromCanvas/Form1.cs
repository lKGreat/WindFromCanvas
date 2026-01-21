using System;
using System.Drawing;
using System.Windows.Forms;
using WindFromCanvas.Core.Objects;

namespace WindFromCanvas
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            // 测试：使用 Context2D 绘制
            var ctx = canvas1.GetContext2D();
            ctx.FillStyle = new WindFromCanvas.Core.Styles.SolidColorStyle(Color.Red);
            ctx.FillRect(50, 50, 200, 150);
            
            ctx.StrokeStyle = new WindFromCanvas.Core.Styles.SolidColorStyle(Color.Blue);
            ctx.LineWidth = 3f;
            ctx.StrokeRect(100, 100, 200, 150);
        }
    }
}
