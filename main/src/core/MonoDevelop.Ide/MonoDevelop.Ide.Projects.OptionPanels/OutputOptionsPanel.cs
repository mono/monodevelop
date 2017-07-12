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
using System.Collections;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class OutputOptionsPanel : MultiConfigItemOptionsPanel
	{
		OutputOptionsPanelWidget widget;
		
		public OutputOptionsPanel ()
		{
			AllowMixedConfigurations = true;
		}
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is DotNetProject;
		}

		public override Control CreatePanelWidget()
		{
			return (widget = new OutputOptionsPanelWidget ());
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}
		
		public override void LoadConfigData ()
		{
			widget.Load (ConfiguredProject, CurrentConfigurations);
		}

		protected override bool ConfigurationsAreEqual (IEnumerable<ItemConfiguration> configs)
		{
			string outAsm = null;
			string outDir = null;
			string outDirTemplate = null;
			OutputOptionsPanelWidget.GetCommonData (configs, out outAsm, out outDir, out outDirTemplate);
			return outAsm.Length != 0 && (outDir.Length != 0 || outDirTemplate.Length != 0);
		}

		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}


	partial class OutputOptionsPanelWidget : Gtk.Bin 
	{
		ItemConfiguration[] configurations;

		public OutputOptionsPanelWidget ()
		{
			Build ();

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			label91.Accessible.Role = Atk.Role.Filler;
			assemblyNameEntry.SetCommonAccessibilityAttributes ("OutputOptionsPanel.AssemblyEntry",
			                                                    GettextCatalog.GetString ("Assembly Name"),
			                                                    GettextCatalog.GetString ("Enter the name of the output assembly"));
			assemblyNameEntry.SetAccessibilityLabelRelationship (label98);
			outputPathEntry.EntryAccessible.SetCommonAttributes ("OutputOptionsPanel.OutputEntry",
				                                                 GettextCatalog.GetString ("Output Path"),
				                                                 GettextCatalog.GetString ("Enter the output path"));
			outputPathEntry.EntryAccessible.SetTitleUIElement (label99.Accessible);
			label99.Accessible.SetTitleFor (outputPathEntry.EntryAccessible);
		}
		
		public void Load (Project project, ItemConfiguration[] configs)
		{	
			this.configurations = configs;
			string outAsm = null;
			string outDir = null;
			string outDirTemplate = null;
			
			GetCommonData (configs, out outAsm, out outDir, out outDirTemplate);
			
			assemblyNameEntry.Text = outAsm;
			
			outputPathEntry.DefaultPath = project.BaseDirectory;
			outputPathEntry.Path = !string.IsNullOrEmpty (outDir) ? outDir : outDirTemplate;
		}
		
		internal static void GetCommonData (IEnumerable<ItemConfiguration> configs, out string outAsm, out string outDir, out string outDirTemplate)
		{
			outAsm = null;
			outDir = null;
			outDirTemplate = null;
			
			foreach (DotNetProjectConfiguration conf in configs) {
				if (outAsm == null)
					outAsm = conf.OutputAssembly;
				else if (outAsm != conf.OutputAssembly)
					outAsm = "";
				
				string dirTemplate = conf.OutputDirectory.ToString ().Replace (conf.Name, "$(Configuration)");
				if (conf.Platform.Length > 0)
					dirTemplate = dirTemplate.Replace (conf.Platform, "$(Platform)");
				
				if (outDir == null) {
					outDir = conf.OutputDirectory;
					outDirTemplate = dirTemplate;
				}
				else {
					if (outDir != conf.OutputDirectory)
						outDir = "";
					if (outDirTemplate != dirTemplate)
						outDirTemplate = "";
				}
			}
		}

		public bool ValidateChanges ()
		{
			if (configurations == null)
				return true;
			
			foreach (DotNetProjectConfiguration conf in configurations) {
				if (assemblyNameEntry.Text.Length == 0 && conf.OutputAssembly.Length == 0) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid assembly name specified"));
					return false;
				}
				string dir = outputPathEntry.Path;
				dir = dir.Replace ("$(Configuration)", conf.Name);
				dir = dir.Replace ("$(Platform)", conf.Platform);
				if (!string.IsNullOrEmpty (dir) && !FileService.IsValidPath (dir)) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid output directory: {0}", dir));
					return false;
				}
			}
			
			return true;
		}
		
		public void Store ()
		{	
			if (configurations == null)
				return;
			
			foreach (DotNetProjectConfiguration conf in configurations) {
				if (assemblyNameEntry.Text.Length > 0)
					conf.OutputAssembly = assemblyNameEntry.Text;
				string dir = outputPathEntry.Path;
				dir = dir.Replace ("$(Configuration)", conf.Name);
				dir = dir.Replace ("$(Platform)", conf.Platform);
				if (!string.IsNullOrEmpty (dir))
					conf.OutputDirectory = dir;
			}
		}
	}
}

