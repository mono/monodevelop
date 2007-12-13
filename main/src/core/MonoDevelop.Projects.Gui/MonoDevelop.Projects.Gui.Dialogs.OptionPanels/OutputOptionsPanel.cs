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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;

using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class OutputOptionsPanel : AbstractOptionPanel
	{
		OutputOptionsPanelWidget  widget;
		public override void LoadPanelContents()
		{
			Add (widget = new  OutputOptionsPanelWidget ((Properties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			bool result = true;
			result = widget.Store ();
 			return result;
		}
	}


	partial class OutputOptionsPanelWidget : Gtk.Bin 
	{
		DotNetProjectConfiguration configuration;
		Project project;

		public  OutputOptionsPanelWidget(Properties CustomizationObject)
		{	
			Build ();
			configuration = ((Properties)CustomizationObject).Get<DotNetProjectConfiguration>("Config");
			project = ((Properties)CustomizationObject).Get<Project>("Project");
			externalConsoleCheckButton.Toggled += new EventHandler (ExternalConsoleToggle);
			
			assemblyNameEntry.Text = configuration.OutputAssembly;
			parametersEntry.Text = configuration.CommandLineParameters;
			
			outputPathEntry.DefaultPath = project.BaseDirectory;
			outputPathEntry.Path = configuration.OutputDirectory;
			
			externalConsoleCheckButton.Active = configuration.ExternalConsole;
			pauseConsoleOutputCheckButton.Active = configuration.PauseConsoleOutput;
			
			if (!externalConsoleCheckButton.Active)
				pauseConsoleOutputCheckButton.Sensitive = false;
		}

		public bool Store ()
		{	
			if (configuration == null) {
				return true;
			}
			
			if (!FileService.IsValidFileName (assemblyNameEntry.Text)) {
				Services.MessageService.ShowError (null, GettextCatalog.GetString ("Invalid assembly name specified"), (Gtk.Window) Toplevel, true);
				return false;
			}

			if (!FileService.IsValidFileName (outputPathEntry.Path)) {
				Services.MessageService.ShowError (null, GettextCatalog.GetString ("Invalid output directory specified"), (Gtk.Window) Toplevel, true);
				return false;
			}
			
			configuration.OutputAssembly = assemblyNameEntry.Text;
			configuration.OutputDirectory = outputPathEntry.Path;
			configuration.CommandLineParameters = parametersEntry.Text;
				configuration.ExternalConsole = externalConsoleCheckButton.Active;
			configuration.PauseConsoleOutput = pauseConsoleOutputCheckButton.Active;
			return true;
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

