namespace MonoDevelop.Platform
{
    partial class EncodingSelectionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose (bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose ();
            }
            base.Dispose (disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent ()
        {
            this.label1 = new System.Windows.Forms.Label ();
            this.addButton = new System.Windows.Forms.Button ();
            this.removeButton = new System.Windows.Forms.Button ();
            this.okButton = new System.Windows.Forms.Button ();
            this.cancelButton = new System.Windows.Forms.Button ();
            this.label2 = new System.Windows.Forms.Label ();
            this.shownListView = new MonoDevelop.Platform.EncodingListView ();
            this.availableListView = new MonoDevelop.Platform.EncodingListView ();
            this.upButton = new System.Windows.Forms.Button ();
            this.downButton = new System.Windows.Forms.Button ();
            this.SuspendLayout ();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point (10, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size (105, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Available encodings:";
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Location = new System.Drawing.Point (291, 169);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size (50, 30);
            this.addButton.TabIndex = 2;
            this.addButton.Text = ">";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler (this.addButtonClick);
            // 
            // removeButton
            // 
            this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeButton.Location = new System.Drawing.Point (291, 205);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size (50, 30);
            this.removeButton.TabIndex = 3;
            this.removeButton.Text = "<";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler (this.removeButtonClick);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point (534, 395);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size (75, 23);
            this.okButton.TabIndex = 5;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler (this.okButtonClick);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point (615, 395);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size (75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler (this.cancelButtonClick);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point (350, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size (132, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "Encodings shown in menu:";
            // 
            // shownListView
            // 
            this.shownListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.shownListView.FullRowSelect = true;
            this.shownListView.Location = new System.Drawing.Point (350, 39);
            this.shownListView.MultiSelect = false;
            this.shownListView.Name = "shownListView";
            this.shownListView.Size = new System.Drawing.Size (280, 343);
            this.shownListView.TabIndex = 4;
            this.shownListView.UseCompatibleStateImageBehavior = false;
            this.shownListView.View = System.Windows.Forms.View.Details;
            // 
            // availableListView
            // 
            this.availableListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.availableListView.FullRowSelect = true;
            this.availableListView.Location = new System.Drawing.Point (10, 40);
            this.availableListView.MultiSelect = false;
            this.availableListView.Name = "availableListView";
            this.availableListView.Size = new System.Drawing.Size (270, 342);
            this.availableListView.TabIndex = 1;
            this.availableListView.UseCompatibleStateImageBehavior = false;
            this.availableListView.View = System.Windows.Forms.View.Details;
            // 
            // upButton
            // 
            this.upButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upButton.Location = new System.Drawing.Point (640, 169);
            this.upButton.Name = "upButton";
            this.upButton.Size = new System.Drawing.Size (50, 30);
            this.upButton.TabIndex = 8;
            this.upButton.Text = "Up";
            this.upButton.UseVisualStyleBackColor = true;
            this.upButton.Click += new System.EventHandler (this.upButtonClick);
            // 
            // downButton
            // 
            this.downButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.downButton.Location = new System.Drawing.Point (640, 205);
            this.downButton.Name = "downButton";
            this.downButton.Size = new System.Drawing.Size (50, 30);
            this.downButton.TabIndex = 9;
            this.downButton.Text = "Down";
            this.downButton.UseVisualStyleBackColor = true;
            this.downButton.Click += new System.EventHandler (this.downButtonClick);
            // 
            // EncodingSelectionForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size (703, 431);
            this.Controls.Add (this.downButton);
            this.Controls.Add (this.upButton);
            this.Controls.Add (this.label2);
            this.Controls.Add (this.shownListView);
            this.Controls.Add (this.cancelButton);
            this.Controls.Add (this.okButton);
            this.Controls.Add (this.removeButton);
            this.Controls.Add (this.addButton);
            this.Controls.Add (this.availableListView);
            this.Controls.Add (this.label1);
            this.Name = "EncodingSelectionForm";
            this.ShowInTaskbar = false;
            this.Text = "Select Text Encodings";
            this.ResumeLayout (false);
            this.PerformLayout ();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private EncodingListView availableListView;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private EncodingListView shownListView;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button upButton;
        private System.Windows.Forms.Button downButton;
    }
}