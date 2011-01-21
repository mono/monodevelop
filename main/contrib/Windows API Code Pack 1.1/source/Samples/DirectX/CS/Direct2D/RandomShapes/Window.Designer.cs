namespace RandomShapes
{
    partial class Window
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.d2DShapesControlWithButtons1 = new D2DShapes.D2DShapesControlWithButtons();
            this.SuspendLayout();
            // 
            // d2DShapesControlWithButtons1
            // 
            this.d2DShapesControlWithButtons1.BackColor = System.Drawing.Color.Bisque;
            this.d2DShapesControlWithButtons1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.d2DShapesControlWithButtons1.Location = new System.Drawing.Point(0, 0);
            this.d2DShapesControlWithButtons1.Name = "d2DShapesControlWithButtons1";
            this.d2DShapesControlWithButtons1.NumberOfShapesToAdd = 2;
            this.d2DShapesControlWithButtons1.Size = new System.Drawing.Size(728, 465);
            this.d2DShapesControlWithButtons1.TabIndex = 0;
            // 
            // Window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(728, 465);
            this.Controls.Add(this.d2DShapesControlWithButtons1);
            this.Name = "Window";
            this.Text = "Random Shapes";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Window_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private D2DShapes.D2DShapesControlWithButtons d2DShapesControlWithButtons1;
    }
}

