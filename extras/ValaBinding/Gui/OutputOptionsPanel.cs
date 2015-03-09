//
// OutputOptionsPanel.cs: configure output options
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.ValaBinding.Utils;
using System.IO;
using MonoDevelop.Ide;

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
            outputPathTextEntry.Text = FileService.AbsoluteToRelativePath(
                    configuration.SourceDirectory, configuration.OutputDirectory);
			parametersTextEntry.Text = configuration.CommandLineParameters;
			
			externalConsoleCheckbox.Active = configuration.ExternalConsole;
			pauseCheckbox.Active = configuration.PauseConsoleOutput;
		}
		
		private void OnBrowseButtonClick (object sender, EventArgs e)
		{
			AddPathDialog dialog = new AddPathDialog(
                FileService.RelativeToAbsolutePath(configuration.SourceDirectory, configuration.OutputDirectory));
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
            {
                var baseDirectory = FileUtils.GetExactPathName(configuration.SourceDirectory);
                var fullPath = FileService.RelativeToAbsolutePath(configuration.SourceDirectory,
                    outputPathTextEntry.Text.Trim());

                if (!Directory.Exists(fullPath))
                {
                    try
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageService.ShowException(ex, "Cannot create output folder.");
                    }
                }

                var path = FileUtils.GetExactPathName(fullPath);
                configuration.OutputDirectory = FileService.AbsoluteToRelativePath(
                    baseDirectory, path);
            }
			
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
