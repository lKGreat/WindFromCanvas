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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageCanvas = new System.Windows.Forms.TabPage();
            this.canvas1 = new WindFromCanvas.Core.Canvas();
            this.tabPageFlowDesigner = new System.Windows.Forms.TabPage();
            this.flowDesignerCanvas1 = new WindFromCanvas.Core.Applications.FlowDesigner.FlowDesignerCanvas();
            this.toolboxPanel1 = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.ToolboxPanel();
            this.propertiesPanel1 = new WindFromCanvas.Core.Applications.FlowDesigner.Widgets.NodePropertiesPanel();
            this.tabPageCompleteDesigner = new System.Windows.Forms.TabPage();
            this.flowDesignerControl1 = new WindFromCanvas.Core.Applications.FlowDesigner.FlowDesignerControl();
            this.tabControl1.SuspendLayout();
            this.tabPageCanvas.SuspendLayout();
            this.tabPageFlowDesigner.SuspendLayout();
            this.tabPageCompleteDesigner.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageCanvas);
            this.tabControl1.Controls.Add(this.tabPageFlowDesigner);
            this.tabControl1.Controls.Add(this.tabPageCompleteDesigner);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1200, 700);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageCanvas
            // 
            this.tabPageCanvas.Controls.Add(this.canvas1);
            this.tabPageCanvas.Location = new System.Drawing.Point(4, 22);
            this.tabPageCanvas.Name = "tabPageCanvas";
            this.tabPageCanvas.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageCanvas.Size = new System.Drawing.Size(1192, 674);
            this.tabPageCanvas.TabIndex = 0;
            this.tabPageCanvas.Text = "Canvas演示";
            this.tabPageCanvas.UseVisualStyleBackColor = true;
            // 
            // canvas1
            // 
            this.canvas1.BackgroundColor = System.Drawing.Color.LightGray;
            this.canvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas1.Location = new System.Drawing.Point(3, 3);
            this.canvas1.Name = "canvas1";
            this.canvas1.Size = new System.Drawing.Size(1186, 668);
            this.canvas1.TabIndex = 0;
            // 
            // tabPageFlowDesigner
            // 
            this.tabPageFlowDesigner.Controls.Add(this.flowDesignerCanvas1);
            this.tabPageFlowDesigner.Controls.Add(this.toolboxPanel1);
            this.tabPageFlowDesigner.Controls.Add(this.propertiesPanel1);
            this.tabPageFlowDesigner.Location = new System.Drawing.Point(4, 22);
            this.tabPageFlowDesigner.Name = "tabPageFlowDesigner";
            this.tabPageFlowDesigner.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageFlowDesigner.Size = new System.Drawing.Size(1192, 674);
            this.tabPageFlowDesigner.TabIndex = 1;
            this.tabPageFlowDesigner.Text = "流程设计器";
            this.tabPageFlowDesigner.UseVisualStyleBackColor = true;
            // 
            // flowDesignerCanvas1
            // 
            this.flowDesignerCanvas1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowDesignerCanvas1.Location = new System.Drawing.Point(150, 3);
            this.flowDesignerCanvas1.Name = "flowDesignerCanvas1";
            this.flowDesignerCanvas1.Size = new System.Drawing.Size(900, 668);
            this.flowDesignerCanvas1.TabIndex = 0;
            // 
            // toolboxPanel1
            // 
            this.toolboxPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.toolboxPanel1.Location = new System.Drawing.Point(3, 3);
            this.toolboxPanel1.Name = "toolboxPanel1";
            this.toolboxPanel1.Size = new System.Drawing.Size(141, 668);
            this.toolboxPanel1.TabIndex = 1;
            // 
            // propertiesPanel1
            // 
            this.propertiesPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertiesPanel1.Location = new System.Drawing.Point(1056, 3);
            this.propertiesPanel1.Name = "propertiesPanel1";
            this.propertiesPanel1.Size = new System.Drawing.Size(133, 668);
            this.propertiesPanel1.TabIndex = 2;
            // 
            // tabPageCompleteDesigner
            // 
            this.tabPageCompleteDesigner.Controls.Add(this.flowDesignerControl1);
            this.tabPageCompleteDesigner.Location = new System.Drawing.Point(4, 22);
            this.tabPageCompleteDesigner.Name = "tabPageCompleteDesigner";
            this.tabPageCompleteDesigner.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageCompleteDesigner.Size = new System.Drawing.Size(1192, 674);
            this.tabPageCompleteDesigner.TabIndex = 2;
            this.tabPageCompleteDesigner.Text = "完整设计器";
            this.tabPageCompleteDesigner.UseVisualStyleBackColor = true;
            // 
            // flowDesignerControl1
            // 
            this.flowDesignerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowDesignerControl1.Location = new System.Drawing.Point(3, 3);
            this.flowDesignerControl1.Name = "flowDesignerControl1";
            this.flowDesignerControl1.Size = new System.Drawing.Size(1186, 668);
            this.flowDesignerControl1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.Text = "WindFromCanvas - Canvas 组件演示";
            this.tabControl1.ResumeLayout(false);
            this.tabPageCanvas.ResumeLayout(false);
            this.tabPageFlowDesigner.ResumeLayout(false);
            this.tabPageCompleteDesigner.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageCanvas;
        private WindFromCanvas.Core.Canvas canvas1;
        private System.Windows.Forms.TabPage tabPageFlowDesigner;
        private WindFromCanvas.Core.Applications.FlowDesigner.FlowDesignerCanvas flowDesignerCanvas1;
        private WindFromCanvas.Core.Applications.FlowDesigner.Widgets.ToolboxPanel toolboxPanel1;
        private WindFromCanvas.Core.Applications.FlowDesigner.Widgets.NodePropertiesPanel propertiesPanel1;
        private System.Windows.Forms.TabPage tabPageCompleteDesigner;
        private WindFromCanvas.Core.Applications.FlowDesigner.FlowDesignerControl flowDesignerControl1;
    }
}
