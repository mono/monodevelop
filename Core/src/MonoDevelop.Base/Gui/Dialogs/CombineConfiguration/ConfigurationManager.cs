// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	/// <summary>
	/// Summary description for Form2.
	/// </summary>
	public class ConfigurationManager //: System.Windows.Forms.Form
	{/*
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ComboBox comboBox2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button createButton;
		private System.Windows.Forms.Button removeButton;
		private System.Windows.Forms.Button renameButton;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		
		PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		public ConfigurationManager()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			
			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null){
					components.Dispose();
				}
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
			bool flat = Crownwood.Magic.Common.VisualStyle.IDE == (Crownwood.Magic.Common.VisualStyle)propertyService.GetProperty("MonoDevelop.Gui.VisualStyle", Crownwood.Magic.Common.VisualStyle.IDE);
			
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.createButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.renameButton = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// treeView1
			// 
			this.treeView1.ImageIndex = -1;
			this.treeView1.Location = new System.Drawing.Point(8, 24);
			this.treeView1.Name = "treeView1";
			this.treeView1.SelectedImageIndex = -1;
			this.treeView1.Size = new System.Drawing.Size(200, 240);
			this.treeView1.TabIndex = 0;
			treeView1.BorderStyle  = flat ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
			
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(88, 16);
			this.label1.TabIndex = 1;
			this.label1.Text = "Entries:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(216, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(88, 16);
			this.label2.TabIndex = 2;
			this.label2.Text = "Configuration";
			// 
			// comboBox1
			// 
			this.comboBox1.DropDownWidth = 264;
			this.comboBox1.Location = new System.Drawing.Point(216, 40);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(264, 21);
			this.comboBox1.TabIndex = 3;
			// 
			// label3
			// 
			label3.BorderStyle  = flat ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
			this.label3.Location = new System.Drawing.Point(216, 264);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(272, 2);
			this.label3.TabIndex = 4;
			// 
			// okButton
			// 
			this.okButton.Location = new System.Drawing.Point(256, 272);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(74, 23);
			this.okButton.TabIndex = 5;
			this.okButton.Text = "OK";
			this.okButton.Click += new System.EventHandler(this.button1_Click);
			okButton.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(336, 272);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(74, 23);
			this.cancelButton.TabIndex = 6;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.Click += new System.EventHandler(this.button2_Click);
			cancelButton.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			
			// 
			// helpButton
			// 
			this.helpButton.Location = new System.Drawing.Point(416, 272);
			this.helpButton.Name = "helpButton";
			this.helpButton.Size = new System.Drawing.Size(74, 23);
			this.helpButton.TabIndex = 7;
			this.helpButton.Text = "Help";
			this.helpButton.Click += new System.EventHandler(this.button3_Click);
			helpButton.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			
			//
			// groupBox1
			// 
			this.groupBox1.Controls.AddRange(new System.Windows.Forms.Control[] {
																					this.createButton,
																					this.textBox1,
																					this.label5,
																					this.comboBox2,
																					this.label4});
			this.groupBox1.Location = new System.Drawing.Point(216, 104);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(264, 144);
			this.groupBox1.TabIndex = 8;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Create new Configuration";
			groupBox1.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			
			// 
			// comboBox2
			// 
			this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox2.DropDownWidth = 264;
			this.comboBox2.Location = new System.Drawing.Point(8, 80);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(248, 21);
			this.comboBox2.TabIndex = 3;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 64);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(128, 16);
			this.label4.TabIndex = 2;
			this.label4.Text = "Copy settings from";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(8, 16);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(40, 16);
			this.label5.TabIndex = 4;
			this.label5.Text = "Name";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(8, 32);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(248, 20);
			this.textBox1.TabIndex = 5;
			this.textBox1.Text = "";
			textBox1.BorderStyle  = flat ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
			
			//
			// createButton
			// 
			this.createButton.Location = new System.Drawing.Point(8, 112);
			this.createButton.Name = "createButton";
			this.createButton.TabIndex = 6;
			this.createButton.Text = "Create";
			createButton.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			
			// 
			// removeButton
			// 
			this.removeButton.Location = new System.Drawing.Point(216, 72);
			this.removeButton.Name = "removeButton";
			this.removeButton.TabIndex = 9;
			this.removeButton.Text = "Remove";
			removeButton.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			
			//
			// renameButton
			// 
			this.renameButton.Location = new System.Drawing.Point(296, 72);
			this.renameButton.Name = "renameButton";
			this.renameButton.TabIndex = 10;
			this.renameButton.Text = "Rename";
			renameButton.FlatStyle = flat ? FlatStyle.Flat : FlatStyle.Standard;
			
			// 
			// Form2
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(498, 303);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {this.renameButton,
			                       this.removeButton,
			                       this.groupBox1,
			                       this.helpButton,
			                       this.cancelButton,
			                       this.okButton,
			                       this.label3,
			                       this.comboBox1,
			                       this.label2,
			                       this.label1,
			                       this.treeView1});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form2";
			this.ShowInTaskbar = false;
			this.Text = "Configuration Manager";
			this.Load += new System.EventHandler(this.Form2_Load);
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		#endregion

		private void button3_Click(object sender, System.EventArgs e)
		{

		}

		private void button1_Click(object sender, System.EventArgs e)
		{

		}

		private void Form2_Load(object sender, System.EventArgs e)
		{

		}

		private void button2_Click(object sender, System.EventArgs e)
		{

		}*/
	}
}
