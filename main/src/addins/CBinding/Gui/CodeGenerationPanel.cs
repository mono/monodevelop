//
// CodeGenerationPanel.cs: Code generation panel to configure project
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
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
using System.IO;
using System.Collections;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Core.Gui.Components;

namespace CBinding
{
	public partial class CodeGenerationPanel : Gtk.Bin
	{
		private CProjectConfiguration configuration;
		private CCompilationParameters compilationParameters;
		private Gtk.ListStore libStore = new Gtk.ListStore (typeof(string));
		private Gtk.ListStore libPathStore = new Gtk.ListStore (typeof(string));
		private Gtk.ListStore includePathStore = new Gtk.ListStore (typeof(string));
		
		static string[,] quickPathInsertMenu = new string[,] {
			{GettextCatalog.GetString ("_Project Directory"), "${ProjectDir}"},
			{GettextCatalog.GetString ("_Root Solution Directory"), "${CombineDir}"},
		};
		
		public CodeGenerationPanel ()
		{
			this.Build ();
			
			Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
			
			libTreeView.Model = libStore;
			libTreeView.HeadersVisible = false;
			libTreeView.AppendColumn ("Library", textRenderer, "text", 0);
			
			libPathTreeView.Model = libPathStore;
			libPathTreeView.HeadersVisible = false;
			libPathTreeView.AppendColumn ("Library", textRenderer, "text", 0);
			
			includePathTreeView.Model = includePathStore;
			includePathTreeView.HeadersVisible = false;
			includePathTreeView.AppendColumn ("Include", textRenderer, "text", 0);
			
			new MenuButtonEntry (libPathEntry, quickInsertLibButton, quickPathInsertMenu);
			new MenuButtonEntry (includePathEntry, quickInsertIncludeButton, quickPathInsertMenu);
		}
		
		public void Load (CProjectConfiguration config)
		{
			configuration = config;
			compilationParameters = (CCompilationParameters)configuration.CompilationParameters;
			
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
			case CBinding.CompileTarget.Bin:
				targetComboBox.Active = 0;
				break;
			case CBinding.CompileTarget.StaticLibrary:
				targetComboBox.Active = 1;
				break;
			case CBinding.CompileTarget.SharedLibrary:
				targetComboBox.Active = 2;
				break;
			}
			
			extraCompilerTextView.Buffer.Text = compilationParameters.ExtraCompilerArguments;
			
			extraLinkerTextView.Buffer.Text = compilationParameters.ExtraLinkerArguments;
			
			defineSymbolsTextEntry.Text = compilationParameters.DefineSymbols;
			
			libStore.Clear ();
			foreach (string lib in configuration.Libs)
				libStore.AppendValues (lib);
			
			libPathStore.Clear ();
			foreach (string libPath in configuration.LibPaths)
				libPathStore.AppendValues (libPath);
			
			libPathStore.Clear ();
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
		
		private void OnLibPathAdded (object sender, EventArgs e)
		{
			if (libPathEntry.Text.Length > 0) {
				libPathStore.AppendValues (libPathEntry.Text);
				libPathEntry.Text = string.Empty;
			}
		}
		
		private void OnLibPathRemoved (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			libPathTreeView.Selection.GetSelected (out iter);
			libPathStore.Remove (ref iter);
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
			AddPathDialog dialog = new AddPathDialog ("/usr/include");
			dialog.Run ();
			includePathEntry.Text = dialog.SelectedPath;
		}
		
		private void OnLibPathBrowseButtonClick (object sender, EventArgs e)
		{
			AddPathDialog dialog = new AddPathDialog ("/usr/lib");
			dialog.Run ();
			libPathEntry.Text = dialog.SelectedPath;
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
				configuration.CompileTarget = CBinding.CompileTarget.Bin;
				break;
			case "Static Library":
				configuration.CompileTarget = CBinding.CompileTarget.StaticLibrary;
				break;
			case "Shared Object":
				configuration.CompileTarget = CBinding.CompileTarget.SharedLibrary;
				break;
			}
			
			compilationParameters.ExtraCompilerArguments = extraCompilerTextView.Buffer.Text;
			
			compilationParameters.ExtraLinkerArguments = extraLinkerTextView.Buffer.Text;
			
			compilationParameters.DefineSymbols = defineSymbolsTextEntry.Text;
			
			libStore.GetIterFirst (out iter);
			configuration.Libs.Clear ();
			while (libStore.IterIsValid (iter)) {
				line = (string)libStore.GetValue (iter, 0);
				configuration.Libs.Add (line);
				libStore.IterNext (ref iter);
			}
			
			libPathStore.GetIterFirst (out iter);
			configuration.LibPaths.Clear ();
			while (libPathStore.IterIsValid (iter)) {
				line = (string)libPathStore.GetValue (iter, 0);
				configuration.LibPaths.Add (line);
				libPathStore.IterNext (ref iter);
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

		protected virtual void OnLibPathEntryChanged (object sender, System.EventArgs e)
		{
			if (string.IsNullOrEmpty (libPathEntry.Text))
			    libPathAddButton.Sensitive = false;
			else
			    libPathAddButton.Sensitive = true;
		}

		protected virtual void OnIncludePathTreeViewCursorChanged (object sender, System.EventArgs e)
		{
			includePathRemoveButton.Sensitive = true;
		}

		protected virtual void OnIncludePathRemoveButtonClicked (object sender, System.EventArgs e)
		{
			includePathRemoveButton.Sensitive = false;
		}
		
		protected virtual void OnLibPathTreeViewCursorChanged (object sender, System.EventArgs e)
		{
			libPathRemoveButton.Sensitive = true;
		}

		protected virtual void OnLibPathRemoveButtonClicked (object sender, System.EventArgs e)
		{
			libPathRemoveButton.Sensitive = false;
		}

		protected virtual void OnLibAddEntryActivated (object sender, System.EventArgs e)
		{
			OnLibAdded (this, new EventArgs ());
		}

		protected virtual void OnIncludePathEntryActivated (object sender, System.EventArgs e)
		{
			OnIncludePathAdded (this, new EventArgs ());
		}

		protected virtual void OnLibPathEntryActivated (object sender, System.EventArgs e)
		{
			OnLibPathAdded (this, new EventArgs ());
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
			panel.Load ((CProjectConfiguration) CurrentConfiguration);
		}
		
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
}
