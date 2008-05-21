//
// CommonAssemblySigningPreferences.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal partial class CommonAssemblySigningPreferences : Gtk.Bin
	{
		ItemConfiguration[] configurations;
		string keyFile;
		
		public CommonAssemblySigningPreferences ()
		{
			this.Build();
		}
		
		public void LoadPanelContents (Project project, ItemConfiguration[] configurations)
		{
			this.configurations = configurations;
			
			int signAsm = -1;
			
			keyFile = null;
			foreach (ProjectConfiguration c in configurations) {
				int r = c.SignAssembly ? 1 : 0;
				if (signAsm == -1)
					signAsm = r;
				else if (signAsm != r)
					signAsm = 2;
				if (keyFile == null)
					keyFile = c.AssemblyKeyFile;
				else if (keyFile != c.AssemblyKeyFile)
					keyFile = "?";
			}
			
			if (signAsm == 2)
				signAssemblyCheckbutton.Inconsistent = true;
			else {
				signAssemblyCheckbutton.Inconsistent = false;
				signAssemblyCheckbutton.Active = signAsm == 1;
			}
			
			if (keyFile == null || keyFile == "?")
				this.strongNameFileEntry.Path = string.Empty;
			else
				this.strongNameFileEntry.Path = keyFile;
			
			this.strongNameFileEntry.DefaultPath = project.BaseDirectory;
			this.strongNameFileLabel.Sensitive = this.strongNameFileEntry.Sensitive = signAsm != 0;
			this.signAssemblyCheckbutton.Toggled += new EventHandler (SignAssemblyCheckbuttonActivated);
		}
		
		void SignAssemblyCheckbuttonActivated (object sender, EventArgs e)
		{
			signAssemblyCheckbutton.Inconsistent = false;
			this.strongNameFileLabel.Sensitive = this.strongNameFileEntry.Sensitive = this.signAssemblyCheckbutton.Active;
		}
		
		public void StorePanelContents ()
		{
			foreach (ProjectConfiguration c in configurations) {
				if (!signAssemblyCheckbutton.Inconsistent)
					c.SignAssembly = this.signAssemblyCheckbutton.Active;
				if (strongNameFileEntry.Path.Length > 0 || keyFile != "?")
					c.AssemblyKeyFile = this.strongNameFileEntry.Path;
			}
		}
	}
	
	class CommonAssemblySigningPreferencesPanel: MultiConfigItemOptionsPanel
	{
		CommonAssemblySigningPreferences widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is DotNetProject;
		}

		
		public override Gtk.Widget CreatePanelWidget ()
		{
			AllowMixedConfigurations = true;
			return (widget = new CommonAssemblySigningPreferences ());
		}
		
		public override void LoadConfigData ()
		{
			widget.LoadPanelContents (ConfiguredProject, CurrentConfigurations);
		}

		public override void ApplyChanges ()
		{
			widget.StorePanelContents ();
		}
	}
}
