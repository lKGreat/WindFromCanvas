namespace WindFromCanvas
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.canvas1 = new WindFromCanvas.Core.Canvas();
            this.SuspendLayout();
            // 
            // canvas1
            // 
            this.canvas1.BackgroundColor = System.Drawing.Color.LightGray;
            this.canvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas1.Location = new System.Drawing.Point(0, 0);
            this.canvas1.Name = "canvas1";
            this.canvas1.Size = new System.Drawing.Size(800, 600);
            this.canvas1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.canvas1);
            this.Name = "Form1";
            this.Text = "WindFromCanvas Test";
            this.ResumeLayout(false);
        }

        private WindFromCanvas.Core.Canvas canvas1;
    }
}
