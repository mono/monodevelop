// RunOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;

using Gtk;


namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class RunOptionsPanel : MultiConfigItemOptionsPanel
	{
		RunOptionsPanelWidget  widget;
		
		public override Widget CreatePanelWidget()
		{
			return (widget = new RunOptionsPanelWidget ());
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}
		
		public override void LoadConfigData ()
		{
			widget.Load (ConfiguredProject, (ProjectConfiguration) CurrentConfiguration);
		}

		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}	
	
	public partial class RunOptionsPanelWidget : Gtk.Bin
	{
		ProjectConfiguration configuration;

		public RunOptionsPanelWidget()
		{
			this.Build();
			externalConsoleCheckButton.Toggled += new EventHandler (ExternalConsoleToggle);
		}
		
		public void Load (Project project, ProjectConfiguration config)
		{	
			this.configuration = config;
			
			parametersEntry.Text = configuration.CommandLineParameters;
			externalConsoleCheckButton.Active = configuration.ExternalConsole;
			pauseConsoleOutputCheckButton.Active = configuration.PauseConsoleOutput;
			pauseConsoleOutputCheckButton.Sensitive = externalConsoleCheckButton.Active;
			
			envVarList.LoadValues (configuration.EnvironmentVariables);
		}

		public bool ValidateChanges ()
		{
			return true;
		}
		
		public void Store ()
		{	
			if (configuration == null)
				return;
			
			configuration.CommandLineParameters = parametersEntry.Text;
			configuration.ExternalConsole = externalConsoleCheckButton.Active;
			configuration.PauseConsoleOutput = pauseConsoleOutputCheckButton.Active;
			
			configuration.EnvironmentVariables.Clear ();
			envVarList.StoreValues (configuration.EnvironmentVariables);
		}
		
		void ExternalConsoleToggle (object sender, EventArgs e)
		{
			if (externalConsoleCheckButton.Active) {
 				pauseConsoleOutputCheckButton.Sensitive = true;
				pauseConsoleOutputCheckButton.Active = true;
			} else {
 				pauseConsoleOutputCheckButton.Sensitive = false;
				pauseConsoleOutputCheckButton.Active = false;
			}
		}		
	}
}
