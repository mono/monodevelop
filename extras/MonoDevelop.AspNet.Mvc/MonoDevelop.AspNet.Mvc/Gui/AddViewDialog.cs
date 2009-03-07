// 
// AddViewDialog.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.AspNet.Gui;
using PP = System.IO.Path;

namespace MonoDevelop.AspNet.Mvc.Gui
{
	
	
	public partial class AddViewDialog : Gtk.Dialog
	{	
		AspMvcProject project;
		IList<string> loadedTemplateList;
		
		public AddViewDialog (AspMvcProject project)
		{
			this.project = project;
			this.Build ();
			
			string siteMaster = PP.Combine (PP.Combine (PP.Combine (project.BaseDirectory, "Views"), "Shared"), "Site.master");
			if (project.Files.GetFile (siteMaster) != null)
				masterEntry.Text = "~/Views/Shared/Site.master";
			
			loadedTemplateList = project.GetCodeTemplates ("AddView");
			bool foundEmptyTemplate = false;
			int templateIndex = 0;
			foreach (string file in loadedTemplateList) {
				string name = PP.GetFileNameWithoutExtension (file);
				templateCombo.AppendText (name);
				if (!foundEmptyTemplate){
					if (name == "Empty") {
						templateCombo.Active = templateIndex;
						foundEmptyTemplate = true;
					} else
						templateIndex++;
				}
			}
			
			if (!foundEmptyTemplate)
				throw new Exception ("The Empty.tt template is missing.");
			
			UpdateTypePanelSensitivity (null, null);
			UpdateMasterPanelSensitivity (null, null);
			Validate (null, null);
		}
	
		protected virtual void Validate (object sender, EventArgs e)
		{
			buttonOk.Sensitive = IsValid ();
		}
	
		protected virtual void UpdateMasterPanelSensitivity (object sender, EventArgs e)
		{
			bool canHaveMaster = !IsPartialView;
			masterCheck.Sensitive = canHaveMaster;
			masterPanel.Sensitive = canHaveMaster && HasMaster;
		}
		
		protected virtual void UpdateTypePanelSensitivity (object sender, EventArgs e)
		{
			typePanel.Sensitive = stronglyTypedCheck.Active;
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
		
		public bool IsValid ()
		{
			if (String.IsNullOrEmpty (ViewName))
				return false;
			
			if (!IsPartialView && HasMaster && String.IsNullOrEmpty (MasterFile)) //PrimaryPlaceHolder can be empty
				return false;
			
			if (IsStronglyTyped && (ViewDataType == null || String.IsNullOrEmpty (TemplateFile)))
			    return false;
			
			return true;
		}
	
		protected virtual void ShowMasterSelectionDialog (object sender, System.EventArgs e)
		{
			//MonoDevelop.AspNet.Gui.
			using (AspNetFileSelector dialog = new AspNetFileSelector (project, null, "*.master")) {
				dialog.Title = MonoDevelop.Core.GettextCatalog.GetString ("Select a Master Page...");
				dialog.Modal = true;
				dialog.TransientFor = this;
				dialog.DestroyWithParent = true;
				int response = dialog.Run ();
				if (response == (int)Gtk.ResponseType.Ok)
					masterEntry.Text = project.LocalToVirtualPath (dialog.SelectedFile.FilePath);
				dialog.Destroy ();
			}
		}
		
		#region Public properties
		
		public MonoDevelop.Projects.Dom.IType ViewDataType {
			get {
				return null;
			}
		}
		
		public string MasterFile {
			get {
				return masterEntry.Text;
			}
		}
		
		public bool HasMaster {
			get {
				return masterCheck.Active;
			}
		}
		
		public string PrimaryPlaceHolder {
			get {
				return primaryPlaceholderCombo.ActiveText;
			}
		}
		
		public List<string> ContentPlaceHolders {
			get; private set;
		}
		
		public string TemplateFile {
			get {
				return loadedTemplateList[templateCombo.Active];
			}
		}
		
		public string ViewName {
			get {
				return nameEntry.Text;
			}
			set {
				nameEntry.Text = value ?? "";
			}
		}
		
		public bool IsPartialView {
			get { return partialCheck.Active; }
		}
		
		public bool IsStronglyTyped {
			get { return stronglyTypedCheck.Active; }
		}
		
		#endregion
	}
}

		