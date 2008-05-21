//  OutputOptionsPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
	internal class OutputOptionsPanel : MultiConfigItemOptionsPanel
	{
		OutputOptionsPanelWidget  widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is DotNetProject;
		}

		public override Widget CreatePanelWidget()
		{
			return (widget = new OutputOptionsPanelWidget ());
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}
		
		public override void LoadConfigData ()
		{
			widget.Load (ConfiguredProject, (DotNetProjectConfiguration) CurrentConfiguration);
		}

		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}


	partial class OutputOptionsPanelWidget : Gtk.Bin 
	{
		DotNetProjectConfiguration configuration;

		public OutputOptionsPanelWidget ()
		{
			Build ();
			externalConsoleCheckButton.Toggled += new EventHandler (ExternalConsoleToggle);
		}
		
		public void Load (Project project, DotNetProjectConfiguration config)
		{	
			this.configuration = config;
			assemblyNameEntry.Text = configuration.OutputAssembly;
			parametersEntry.Text = configuration.CommandLineParameters;
			
			outputPathEntry.DefaultPath = project.BaseDirectory;
			outputPathEntry.Path = configuration.OutputDirectory;
			
			externalConsoleCheckButton.Active = configuration.ExternalConsole;
			pauseConsoleOutputCheckButton.Active = configuration.PauseConsoleOutput;
			
			pauseConsoleOutputCheckButton.Sensitive = externalConsoleCheckButton.Active;
		}

		public bool ValidateChanges ()
		{
			if (configuration == null) {
				return true;
			}
			
			if (!FileService.IsValidFileName (assemblyNameEntry.Text)) {
				MessageService.ShowError (GettextCatalog.GetString ("Invalid assembly name specified"));
				return false;
			}

			if (!FileService.IsValidFileName (outputPathEntry.Path)) {
				MessageService.ShowError (GettextCatalog.GetString ("Invalid output directory specified"));
				return false;
			}
			
			return true;
		}
		
		public void Store ()
		{	
			if (configuration == null)
				return;
			
			configuration.OutputAssembly = assemblyNameEntry.Text;
			configuration.OutputDirectory = outputPathEntry.Path;
			configuration.CommandLineParameters = parametersEntry.Text;
				configuration.ExternalConsole = externalConsoleCheckButton.Active;
			configuration.PauseConsoleOutput = pauseConsoleOutputCheckButton.Active;
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

