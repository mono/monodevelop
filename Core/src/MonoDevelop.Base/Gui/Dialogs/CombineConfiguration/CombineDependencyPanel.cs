// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{/*
	/// <summary>
	/// Summary description for UserControl2.
	/// </summary>
	public class CombineDependencyPanel : AbstractOptionPanel
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.CheckedListBox checkedListBox1;
		private System.Windows.Forms.Label label2;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		
		Combine combine;
		
		ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
		PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		
		public override bool ReceiveDialogMessage(DialogMessage message)
		{
			if (message == DialogMessage.OK) {
				// TODO : Project dependency accept
			}
			return true;
		}
		
		void FillCheckListBox(object sender, EventArgs e)
		{
			checkedListBox1.Items.Clear();
			
			foreach (CombineEntry entry in combine.Entries) {
				if (entry.Name != comboBox1.SelectedItem.ToString()) {
					checkedListBox1.Items.Add(entry.Name);
				}
			}
		}
		
		void SetValues(object sender, EventArgs e)
		{
			this.combine = (Combine)((IProperties)CustomizationObject).GetProperty("Combine");
			
			foreach (CombineEntry entry in combine.Entries) 
				comboBox1.Items.Add(entry.Name);
			
			if (comboBox1.Items.Count > 0) {
				comboBox1.SelectedIndex = 0;
			}
			
			FillCheckListBox(null, null);
		}
		
		public CombineDependencyPanel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			CustomizationObjectChanged += new EventHandler(SetValues);
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
			this.label1 = new System.Windows.Forms.Label();
			
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(304, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = resourceService.GetString("Dialog.Options.CombineOptions.Dependencies.EntryLabel");
			// 
			// comboBox1
			// 
			this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.comboBox1.DropDownWidth = 304;
			this.comboBox1.Location = new System.Drawing.Point(8, 24);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(304, 21);
			this.comboBox1.TabIndex = 1;
			this.comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
			comboBox1.SelectedIndexChanged += new EventHandler(FillCheckListBox);
			
			//
			// checkedListBox1
			// 
			this.checkedListBox1.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.checkedListBox1.Location = new System.Drawing.Point(8, 72);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(304, 214);
			this.checkedListBox1.TabIndex = 2;
			checkedListBox1.BorderStyle  = flat ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
			
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.label2.Location = new System.Drawing.Point(8, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(304, 16);
			this.label2.TabIndex = 3;
			this.label2.Text = resourceService.GetString("Dialog.Options.CombineOptions.Dependencies.DependsOnLabel");
			// 
			// Form10
			// 
			this.ClientSize = new System.Drawing.Size(320, 293);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {this.label2,
			                       this.checkedListBox1,
			                       this.comboBox1,
			                       this.label1});
			this.Name = "Form10";
			this.Text = "Form10";
			this.ResumeLayout(false);

		}
		#endregion
	}*/
}
