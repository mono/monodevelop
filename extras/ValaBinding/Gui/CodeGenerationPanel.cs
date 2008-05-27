//
// CodeGenerationPanel.cs: Code generation panel to configure project
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
using System.IO;
using System.Collections;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Dialogs;

namespace MonoDevelop.ValaBinding
{
	public partial class CodeGenerationPanel : Gtk.Bin
	{
		private ValaProjectConfiguration configuration;
		private ValaCompilationParameters compilationParameters;
		private Gtk.ListStore libStore = new Gtk.ListStore (typeof(string));
		private Gtk.ListStore includePathStore = new Gtk.ListStore (typeof(string));
		
		public CodeGenerationPanel () 
		{
			this.Build ();
			
			Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
			
			libTreeView.Model = libStore;
			libTreeView.HeadersVisible = false;
			libTreeView.AppendColumn ("Library", textRenderer, "text", 0);
			
			includePathTreeView.Model = includePathStore;
			includePathTreeView.HeadersVisible = false;
			includePathTreeView.AppendColumn ("Include", textRenderer, "text", 0);
		}
		
		public void Load (ValaProjectConfiguration config)
		{
			configuration = config;
			compilationParameters = (ValaCompilationParameters)configuration.CompilationParameters;
			
			switch (compilationParameters.WarningLevel)
			{
			case WarningLevel.None:
				noWarningRadio.Active = true;
				break;
			case WarningLevel.Normal:
				normalWarningRadio.Active = true;
				break;
			case WarningLevel.All:
				allWarningRadio.Active = true;
				break;
			}
			
			warningsAsErrorsCheckBox.Active = compilationParameters.WarningsAsErrors;
			
			optimizationSpinButton.Value = compilationParameters.OptimizationLevel;
			
			switch (configuration.CompileTarget)
			{
			case ValaBinding.CompileTarget.Bin:
				targetComboBox.Active = 0;
				break;
			case ValaBinding.CompileTarget.StaticLibrary:
				targetComboBox.Active = 1;
				break;
			case ValaBinding.CompileTarget.SharedLibrary:
				targetComboBox.Active = 2;
				break;
			}
			
			extraCompilerTextView.Buffer.Text = compilationParameters.ExtraCompilerArguments;
			
			defineSymbolsTextEntry.Text = compilationParameters.DefineSymbols;
			
			foreach (string lib in configuration.Libs)
				libStore.AppendValues (lib);
			
			foreach (string includePath in configuration.Includes)
				includePathStore.AppendValues (includePath);
		}
		
		private void OnIncludePathAdded (object sender, EventArgs e)
		{
			if (includePathEntry.Text.Length > 0) {				
				includePathStore.AppendValues (includePathEntry.Text);
				includePathEntry.Text = string.Empty;
			}
		}
		
		private void OnIncludePathRemoved (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			includePathTreeView.Selection.GetSelected (out iter);
			includePathStore.Remove (ref iter);
		}
		
		private void OnLibAdded (object sender, EventArgs e)
		{
			if (libAddEntry.Text.Length > 0) {				
				libStore.AppendValues (libAddEntry.Text);
				libAddEntry.Text = string.Empty;
			}
		}
		
		private void OnLibRemoved (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			libTreeView.Selection.GetSelected (out iter);
			libStore.Remove (ref iter);
		}
		
		private void OnBrowseButtonClick (object sender, EventArgs e)
		{
			AddLibraryDialog dialog = new AddLibraryDialog ();
			dialog.Run ();
			libAddEntry.Text = dialog.Library;
		}
		
		private void OnIncludePathBrowseButtonClick (object sender, EventArgs e)
		{
			AddPathDialog dialog = new AddPathDialog (configuration.SourceDirectory);
			dialog.Run ();
			includePathEntry.Text = dialog.SelectedPath;
		}
		
		public bool Store ()
		{
			if (compilationParameters == null || configuration == null)
				return false;
			
			string line;
			Gtk.TreeIter iter;
			
			if (noWarningRadio.Active)
				compilationParameters.WarningLevel = WarningLevel.None;
			else if (normalWarningRadio.Active)
				compilationParameters.WarningLevel = WarningLevel.Normal;
			else
				compilationParameters.WarningLevel = WarningLevel.All;
			
			compilationParameters.WarningsAsErrors = warningsAsErrorsCheckBox.Active;
			
			compilationParameters.OptimizationLevel = (int)optimizationSpinButton.Value;
			
			switch (targetComboBox.ActiveText)
			{
			case "Executable":
				configuration.CompileTarget = ValaBinding.CompileTarget.Bin;
				break;
			case "Static Library":
				configuration.CompileTarget = ValaBinding.CompileTarget.StaticLibrary;
				break;
			case "Shared Object":
				configuration.CompileTarget = ValaBinding.CompileTarget.SharedLibrary;
				break;
			}
			
			compilationParameters.ExtraCompilerArguments = extraCompilerTextView.Buffer.Text;
			
			compilationParameters.DefineSymbols = defineSymbolsTextEntry.Text;
			
			libStore.GetIterFirst (out iter);
			configuration.Libs.Clear ();
			while (libStore.IterIsValid (iter)) {
				line = (string)libStore.GetValue (iter, 0);
				configuration.Libs.Add (line);
				libStore.IterNext (ref iter);
			}
			
			includePathStore.GetIterFirst (out iter);
			configuration.Includes.Clear ();
			while (includePathStore.IterIsValid (iter)) {
				line = (string)includePathStore.GetValue (iter, 0);
				configuration.Includes.Add (line);
				includePathStore.IterNext (ref iter);
			}
			
			return true;
		}

		protected virtual void OnLibAddEntryChanged (object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty (libAddEntry.Text))
				addLibButton.Sensitive = false;
			else
				addLibButton.Sensitive = true;
		}

		protected virtual void OnLibTreeViewCursorChanged (object sender, System.EventArgs e)
		{
			removeLibButton.Sensitive = true;
		}

		protected virtual void OnRemoveLibButtonClicked (object sender, System.EventArgs e)
		{
			removeLibButton.Sensitive = false;
		}

		protected virtual void OnIncludePathEntryChanged (object sender, System.EventArgs e)
		{
			if (string.IsNullOrEmpty (includePathEntry.Text))
				includePathAddButton.Sensitive = false;
			else
				includePathAddButton.Sensitive = true;
		}

		protected virtual void OnIncludePathTreeViewCursorChanged (object sender, System.EventArgs e)
		{
			includePathRemoveButton.Sensitive = true;
		}

		protected virtual void OnIncludePathRemoveButtonClicked (object sender, System.EventArgs e)
		{
			includePathRemoveButton.Sensitive = false;
		}
		
		protected virtual void OnLibAddEntryActivated (object sender, System.EventArgs e)
		{
			OnLibAdded (this, new EventArgs ());
		}

		protected virtual void OnIncludePathEntryActivated (object sender, System.EventArgs e)
		{
			OnIncludePathAdded (this, new EventArgs ());
		}
	}
	
	public class CodeGenerationPanelBinding : MultiConfigItemOptionsPanel
	{
		private CodeGenerationPanel panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return panel = new CodeGenerationPanel ();
		}
		
		public override void LoadConfigData ()
		{
			panel.Load((ValaProjectConfiguration) CurrentConfiguration);
//			panel = new CodeGenerationPanel ((Properties)CustomizationObject);
//			Add (panel);
		}

		
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
}
