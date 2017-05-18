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
using System.Collections.Generic;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class CommonAssemblySigningPreferences : Gtk.Bin
	{
		ItemConfiguration[] configurations;
		string keyFile;
		
		public CommonAssemblySigningPreferences ()
		{
			this.Build();

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			signAssemblyCheckbutton.SetCommonAccessibilityAttributes ("SigningOptions.Sign", null,
			                                                          GettextCatalog.GetString ("Check to enable assembly signing"));
			strongNameFileEntry.EntryAccessible.Name = "SigningOptions.NameFile";
			strongNameFileEntry.EntryAccessible.SetLabel (GettextCatalog.GetString ("Strong Name File"));
			strongNameFileEntry.EntryAccessible.Description = GettextCatalog.GetString ("Enter the Strong Name File");
			strongNameFileEntry.EntryAccessible.SetTitleUIElement (strongNameFileLabel.Accessible);
			strongNameFileLabel.Accessible.SetTitleFor (strongNameFileEntry.EntryAccessible);

			delaySignCheckbutton.SetCommonAccessibilityAttributes ("SigningOptions.Delay", null,
			                                                       GettextCatalog.GetString ("Delay signing the assembly"));
		}

		public void LoadPanelContents (Project project, ItemConfiguration[] configurations)
		{
			this.configurations = configurations;
			
			int signAsm = -1;
			int delaySign = -1;
			
			keyFile = null;
			foreach (DotNetProjectConfiguration c in configurations) {
				int r = c.SignAssembly ? 1 : 0;
				if (signAsm == -1)
					signAsm = r;
				else if (signAsm != r)
					signAsm = 2;
				int d = c.DelaySign ? 1 : 0;
				if (delaySign == -1)
					delaySign = d;
				else if (delaySign != d)
					delaySign = 2;
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
			if (delaySign == 2)
				delaySignCheckbutton.Inconsistent = true;
			else {
				delaySignCheckbutton.Inconsistent = false;
				delaySignCheckbutton.Active = delaySign == 1;
			}
			
			if (keyFile == null || keyFile == "?")
				this.strongNameFileEntry.Path = string.Empty;
			else
				this.strongNameFileEntry.Path = keyFile;

			this.strongNameFileEntry.DefaultPath = project.BaseDirectory;
			delaySignCheckbutton.Sensitive = this.strongNameFileLabel.Sensitive = this.strongNameFileEntry.Sensitive = signAsm != 0;
			this.signAssemblyCheckbutton.Toggled += new EventHandler (SignAssemblyCheckbuttonActivated);
		}
		
		void SignAssemblyCheckbuttonActivated (object sender, EventArgs e)
		{
			signAssemblyCheckbutton.Inconsistent = false;
			this.delaySignCheckbutton.Sensitive = this.strongNameFileLabel.Sensitive = this.strongNameFileEntry.Sensitive = this.signAssemblyCheckbutton.Active;
		}
		
		public void StorePanelContents ()
		{
			foreach (DotNetProjectConfiguration c in configurations) {
				if (!signAssemblyCheckbutton.Inconsistent) {
					c.SignAssembly = this.signAssemblyCheckbutton.Active;
					c.DelaySign = this.delaySignCheckbutton.Active;
				}
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

		
		public override Control CreatePanelWidget ()
		{
			AllowMixedConfigurations = true;
			return (widget = new CommonAssemblySigningPreferences ());
		}
		
		protected override bool ConfigurationsAreEqual (IEnumerable<ItemConfiguration> configs)
		{
			DotNetProjectConfiguration cref = null;
			foreach (DotNetProjectConfiguration c in configs) {
				if (cref == null)
					cref = c;
				else {
					if (c.SignAssembly != cref.SignAssembly)
						return false;
					if (c.DelaySign != cref.DelaySign)
						return false;
					if (c.AssemblyKeyFile != cref.AssemblyKeyFile)
						return false;
				}
			}
			return true;
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
