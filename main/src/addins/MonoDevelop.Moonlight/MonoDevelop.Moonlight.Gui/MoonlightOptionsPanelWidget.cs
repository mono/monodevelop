// 
// MoonlightOptionsPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Moonlight.Gui
{
	
	public partial class MoonlightOptionsPanelWidget : Gtk.Bin
	{
		Gtk.ListStore classListStore;
		bool classesFilled = false;
		
		public MoonlightOptionsPanelWidget ()
		{
			this.Build();
			
			this.validateXamlCheck.Toggled += delegate {
				this.throwXamlErrorsCheck.Sensitive = this.validateXamlCheck.Active;
			};
			
			this.generateManifestCheck.Toggled += delegate {
				this.manifestTable.Sensitive = this.generateManifestCheck.Active;
			};
			
			this.generateTestPageCheck.Toggled += delegate {
				this.testPageBox.Sensitive = this.generateTestPageCheck.Active;
			};
		}
		
		public void Load (MoonlightProject project)
		{
			
			this.validateXamlCheck.Active = project.ValidateXaml;
			this.throwXamlErrorsCheck.Active = project.ThrowErrorsInValidation;
			
			//TODO: enable after implementing xaml validation
//			this.throwXamlErrorsCheck.Sensitive = this.validateXamlCheck.Active;
			this.xamlAlignment.Sensitive = false;
			
			this.applicationOptionsBox.Visible = project.SilverlightApplication;
			
			if (!project.SilverlightApplication)
				return;
			
			this.xapFilenameEntry.Text = project.XapFilename ?? "";
			
			this.generateManifestCheck.Active = project.GenerateSilverlightManifest;
			this.manifestTemplateFilenameEntry.Text = project.SilverlightManifestTemplate ?? "";
			this.manifestTemplateFilenameEntry.Sensitive = this.generateManifestCheck.Active;
			
			this.generateTestPageCheck.Active = project.CreateTestPage;
			this.testPageFilenameEntry.Text = project.TestPageFileName ?? "";
			this.testPageFilenameEntry.Sensitive = this.generateTestPageCheck.Active;
			
			this.entryPointCombo.Entry.Text = project.SilverlightAppEntry;
			classListStore = new Gtk.ListStore (typeof(string));
			entryPointCombo.Model = classListStore;
			entryPointCombo.TextColumn = 0;
			
			FillClasses (project);
		}
		
		void FillClasses (MoonlightProject project)
		{
			if (classesFilled)
				return;
			classesFilled = true;
			try {
				ProjectDom dom = ProjectDomService.GetProjectDom (project);
				IType appType = dom.GetType ("System.Windows.Application", true);
				if (appType == null)
					return;
				foreach (IType type in dom.GetSubclasses (appType, false))
					classListStore.AppendValues (type.FullName);
			} catch (InvalidOperationException) {
				// Project not found in parser database
			}
		}
		
		public void Store (MoonlightProject project)
		{
			project.ValidateXaml = this.throwXamlErrorsCheck.Active;
			project.ThrowErrorsInValidation = this.throwXamlErrorsCheck.Active;
			
			if (!project.SilverlightApplication)
				return;
			
			if (!string.IsNullOrEmpty (this.xapFilenameEntry.Text))
				project.XapFilename = this.xapFilenameEntry.Text;
			
			project.GenerateSilverlightManifest = this.generateManifestCheck.Active;
			project.SilverlightManifestTemplate = this.manifestTemplateFilenameEntry.Text;
			
			project.CreateTestPage = this.generateTestPageCheck.Active;
			project.TestPageFileName = this.testPageFilenameEntry.Text;
			
			project.SilverlightAppEntry = this.entryPointCombo.Entry.Text;
		}
	}
}
