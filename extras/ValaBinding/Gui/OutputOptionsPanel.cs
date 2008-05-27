//
// OutputOptionsPanel.cs: configure output options
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//



using System;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Dialogs;

namespace MonoDevelop.ValaBinding
{
	public partial class OutputOptionsPanel : Gtk.Bin
	{
		private ValaProjectConfiguration configuration;
		
		public OutputOptionsPanel ()
		{
			this.Build ();
			
			table1.RowSpacing = 3;
		}
		
		public void Load (ValaProjectConfiguration config)
		{
			configuration = config;
			
			outputNameTextEntry.Text = configuration.Output;
			outputPathTextEntry.Text = configuration.OutputDirectory;
			parametersTextEntry.Text = configuration.CommandLineParameters;
			
			externalConsoleCheckbox.Active = configuration.ExternalConsole;
			pauseCheckbox.Active = configuration.PauseConsoleOutput;
		}
		
		private void OnBrowseButtonClick (object sender, EventArgs e)
		{
			AddPathDialog dialog = new AddPathDialog (configuration.OutputDirectory);
			dialog.Run ();
			outputPathTextEntry.Text = dialog.SelectedPath;
		}
		
		public bool Store ()
		{
			if (configuration == null)
				return false;
			
			if (outputNameTextEntry != null && outputNameTextEntry.Text.Length > 0)
				configuration.Output = outputNameTextEntry.Text.Trim ();
			
			if (outputPathTextEntry.Text != null && outputPathTextEntry.Text.Length > 0)
				configuration.OutputDirectory = outputPathTextEntry.Text.Trim ();
			
			if (parametersTextEntry.Text != null && parametersTextEntry.Text.Length > 0)
				configuration.CommandLineParameters = parametersTextEntry.Text.Trim ();
			
			configuration.ExternalConsole = externalConsoleCheckbox.Active;
			configuration.PauseConsoleOutput = pauseCheckbox.Active;
			
			return true;
		}

		protected virtual void OnExternalConsoleCheckboxClicked (object sender, System.EventArgs e)
		{
			pauseCheckbox.Sensitive = externalConsoleCheckbox.Active;
		}
	}
	
	public class OutputOptionsPanelBinding : MultiConfigItemOptionsPanel
	{
		private OutputOptionsPanel panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return panel = new OutputOptionsPanel ();
		}
		
		public override void LoadConfigData ()
		{
			panel.Load((ValaProjectConfiguration) CurrentConfiguration);
//			panel = new OutputOptionsPanel ((Properties)CustomizationObject);
//			Add (panel);
		}
		
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
}
