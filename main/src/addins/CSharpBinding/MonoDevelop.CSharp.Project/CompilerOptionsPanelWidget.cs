// 
// CompilerOptionsPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007, 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Project
{
	
	public partial class CompilerOptionsPanelWidget : Gtk.Bin
	{
		DotNetProject project;
		ListStore classListStore;
		bool classListFilled;
		
		public CompilerOptionsPanelWidget (DotNetProject project)
		{
			this.Build();
			this.project = project;
			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			CSharpCompilerParameters compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			CSharpProjectParameters projectParameters = (CSharpProjectParameters) configuration.ProjectParameters;
			
			ListStore store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Executable"));
			store.AppendValues (GettextCatalog.GetString ("Library"));
			store.AppendValues (GettextCatalog.GetString ("Executable with GUI"));
			store.AppendValues (GettextCatalog.GetString ("Module"));
			compileTargetCombo.Model = store;
			CellRendererText cr = new CellRendererText ();
			compileTargetCombo.PackStart (cr, true);
			compileTargetCombo.AddAttribute (cr, "text", 0);
			compileTargetCombo.Active = (int) configuration.CompileTarget;
			compileTargetCombo.Changed += new EventHandler (OnTargetChanged);
			
			if (project.IsLibraryBasedProjectType) {
				//fixme: should we totally hide these?
				compileTargetCombo.Sensitive = false;
				mainClassEntry.Sensitive = false;
			} else {
				classListStore = new ListStore (typeof(string));
				mainClassEntry.Model = classListStore;
				mainClassEntry.TextColumn = 0;
				((Entry)mainClassEntry.Child).Text = projectParameters.MainClass ?? string.Empty;
			
				UpdateTarget ();
			}
			
			// Load the codepage. If it matches any of the supported encodigs, use the encoding name 			
			string foundEncoding = null;
			foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
				if (e.CodePage == -1)
					continue;
				if (e.CodePage == projectParameters.CodePage)
					foundEncoding = e.Id;
				codepageEntry.AppendText (e.Id);
			}
			if (foundEncoding != null)
				codepageEntry.Entry.Text = foundEncoding;
			else if (projectParameters.CodePage != 0)
				codepageEntry.Entry.Text = projectParameters.CodePage.ToString ();
			
			iconEntry.Path = projectParameters.Win32Icon;
			iconEntry.DefaultPath = project.BaseDirectory;
			allowUnsafeCodeCheckButton.Active = compilerParameters.UnsafeCode;
			noStdLibCheckButton.Active = compilerParameters.NoStdLib;

			ListStore langVerStore = new ListStore (typeof (string));
			langVerStore.AppendValues (GettextCatalog.GetString ("Default"));
			langVerStore.AppendValues ("ISO-1");
			langVerStore.AppendValues ("ISO-2");
			langVerStore.AppendValues ("Version 3");
			langVerStore.AppendValues ("Version 4");
			langVerStore.AppendValues ("Version 5");
			langVerCombo.Model = langVerStore;
			langVerCombo.Active = (int) compilerParameters.LangVersion;
		}
		
		protected override void OnDestroyed ()
		{
			if (classListStore != null) {
				classListStore.Dispose ();
				classListStore = null;
			}
			base.OnDestroyed ();
		}

		public bool ValidateChanges ()
		{
			if (codepageEntry.Entry.Text.Length > 0) {
				// Get the codepage. If the user specified an encoding name, find it.
				int trialCodePage = -1;
				foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
					if (e.Id == codepageEntry.Entry.Text) {
						trialCodePage = e.CodePage;
						break;
					}
				}
			
				if (trialCodePage == -1) {
					if (!int.TryParse (codepageEntry.Entry.Text, out trialCodePage)) {
						MessageService.ShowError (GettextCatalog.GetString ("Invalid code page number."));
						return false;
					}
				}
			}
			return true;
		}
		
		public void Store (ItemConfigurationCollection<ItemConfiguration> configs)
		{
			int codePage;
			CompileTarget compileTarget =  (CompileTarget) compileTargetCombo.Active;
			LangVersion langVersion = (LangVersion) langVerCombo.Active; 
			
			
			if (codepageEntry.Entry.Text.Length > 0) {
				// Get the codepage. If the user specified an encoding name, find it.
				int trialCodePage = -1;
				foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
					if (e.Id == codepageEntry.Entry.Text) {
						trialCodePage = e.CodePage;
						break;
					}
				}
			
				if (trialCodePage != -1)
					codePage = trialCodePage;
				else {
					if (!int.TryParse (codepageEntry.Entry.Text, out trialCodePage)) {
						return;
					}
					codePage = trialCodePage;
				}
			} else
				codePage = 0;
			
			project.CompileTarget = compileTarget;
			
			CSharpProjectParameters projectParameters = (CSharpProjectParameters) project.LanguageParameters; 
			
			projectParameters.CodePage = codePage;

			if (iconEntry.Sensitive)
				projectParameters.Win32Icon = iconEntry.Path;
			
			if (mainClassEntry.Sensitive)
				projectParameters.MainClass = mainClassEntry.Entry.Text;
			
			foreach (DotNetProjectConfiguration configuration in configs) {
				CSharpCompilerParameters compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters; 
				compilerParameters.UnsafeCode = allowUnsafeCodeCheckButton.Active;
				compilerParameters.NoStdLib = noStdLibCheckButton.Active;
				compilerParameters.LangVersion = langVersion;
			}
		}
		
		void OnTargetChanged (object s, EventArgs a)
		{
			UpdateTarget ();
		}
		
		void UpdateTarget ()
		{
			if ((CompileTarget) compileTargetCombo.Active == CompileTarget.Library) {
				iconEntry.Sensitive = false;
			} else {
				iconEntry.Sensitive = true;
				if (!classListFilled)
					FillClasses ();
			}
		}
		
		void FillClasses ()
		{
			try {
				var ctx = TypeSystemService.GetCompilation (project);
				if (ctx == null)
					// Project not found in parser database
					return;
				foreach (var c in ctx.GetAllTypeDefinitions ()) {
					if (c.Methods != null) {
						foreach (var m in c.Methods) {
							if (m.IsStatic && m.Name == "Main")
								classListStore.AppendValues (c.FullName);
						}
					}
				}
				classListFilled = true;
			} catch (InvalidOperationException) {
				// Project not found in parser database
			}
		}
	}
	
	public class CompilerOptionsPanel : ItemOptionsPanel
	{
		CompilerOptionsPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return (widget = new CompilerOptionsPanelWidget ((DotNetProject) ConfiguredProject));
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}
		
		public override void ApplyChanges ()
		{
			MultiConfigItemOptionsDialog dlg = (MultiConfigItemOptionsDialog) ParentDialog;
			widget.Store (dlg.Configurations);
		}
	}
}
