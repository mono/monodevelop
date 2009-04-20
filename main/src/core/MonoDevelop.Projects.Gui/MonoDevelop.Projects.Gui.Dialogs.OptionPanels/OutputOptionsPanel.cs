// OutputOptionsPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

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
		}
		
		public void Load (Project project, DotNetProjectConfiguration config)
		{	
			this.configuration = config;
			assemblyNameEntry.Text = configuration.OutputAssembly;
			
			outputPathEntry.DefaultPath = project.BaseDirectory;
			outputPathEntry.Path = configuration.OutputDirectory;
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

			if (!FileService.IsValidPath (outputPathEntry.Path)) {
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
		}
	}
}

