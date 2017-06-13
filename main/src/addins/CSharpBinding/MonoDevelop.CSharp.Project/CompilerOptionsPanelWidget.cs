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

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.CSharp.Project
{
	
	partial class CompilerOptionsPanelWidget : Gtk.Bin
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
			var csproject = (CSharpProject)project;
			
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
				((Entry)mainClassEntry.Child).Text = csproject.MainClass ?? string.Empty;
			
				UpdateTarget ();
			}
			
			// Load the codepage. If it matches any of the supported encodigs, use the encoding name 			
			string foundEncoding = null;
			foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
				if (e.CodePage == -1)
					continue;
				if (e.CodePage == csproject.CodePage)
					foundEncoding = e.Id;
				codepageEntry.AppendText (e.Id);
			}
			if (foundEncoding != null)
				codepageEntry.Entry.Text = foundEncoding;
			else if (csproject.CodePage != 0)
				codepageEntry.Entry.Text = csproject.CodePage.ToString ();
			
			iconEntry.Path = csproject.Win32Icon;
			iconEntry.DefaultPath = project.BaseDirectory;
			allowUnsafeCodeCheckButton.Active = compilerParameters.UnsafeCode;
			noStdLibCheckButton.Active = compilerParameters.NoStdLib;

			var langVerStore = new ListStore (typeof (string), typeof(LanguageVersion));
			langVerStore.AppendValues (GettextCatalog.GetString ("Default"), LanguageVersion.Default);
			langVerStore.AppendValues ("ISO-1", LanguageVersion.CSharp1);
			langVerStore.AppendValues ("ISO-2", LanguageVersion.CSharp2);
			langVerStore.AppendValues (GettextCatalog.GetString ("Version 3"), LanguageVersion.CSharp3);
			langVerStore.AppendValues (GettextCatalog.GetString ("Version 4"), LanguageVersion.CSharp4);
			langVerStore.AppendValues (GettextCatalog.GetString ("Version 5"), LanguageVersion.CSharp5);
			langVerStore.AppendValues (GettextCatalog.GetString ("Version 6"), LanguageVersion.CSharp6);
			langVerStore.AppendValues (GettextCatalog.GetString ("Version 7"), LanguageVersion.CSharp7);
			langVerStore.AppendValues (GettextCatalog.GetString ("Version 7.1"), LanguageVersion.CSharp7_1);
			langVerStore.AppendValues (GettextCatalog.GetString ("Latest"), LanguageVersion.Latest);
			langVerCombo.Model = langVerStore;

			TreeIter iter;
			if (langVerStore.GetIterFirst (out iter)) {
				do {
					var val = (LanguageVersion)(int)langVerStore.GetValue (iter, 1);
					if (val == compilerParameters.LangVersion) {
						langVerCombo.SetActiveIter (iter);
						break;
					}
				} while (langVerStore.IterNext (ref iter));
			}

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			compileTargetCombo.SetCommonAccessibilityAttributes ("CodeGeneration.CompileTarget",
			                                                     GettextCatalog.GetString ("Compile Target"),
			                                                     GettextCatalog.GetString ("Select the compile target for the code generation"));
			compileTargetCombo.SetAccessibilityLabelRelationship (label86);

			mainClassEntry.SetCommonAccessibilityAttributes ("CodeGeneration.MainClass",
			                                                 GettextCatalog.GetString ("Main Class"),
			                                                 GettextCatalog.GetString ("Enter the main class for the code generation"));
			mainClassEntry.SetAccessibilityLabelRelationship (label88);

			iconEntry.SetEntryAccessibilityAttributes ("CodeGeneration.WinIcon", GettextCatalog.GetString ("Win32 Icon"),
			                                           GettextCatalog.GetString ("Enter the file to use as the icon on Windows"));
			iconEntry.SetAccessibilityLabelRelationship (label3);

			codepageEntry.SetCommonAccessibilityAttributes ("CodeGeneration.CodePage", GettextCatalog.GetString ("Compiler Code Page"),
			                                                GettextCatalog.GetString ("Select the compiler code page"));
			codepageEntry.SetAccessibilityLabelRelationship (label1);

			noStdLibCheckButton.SetCommonAccessibilityAttributes ("CodeGeneration.NoStdLib", null, GettextCatalog.GetString ("Whether or not to include a reference to mscorlib.dll"));

			langVerCombo.SetCommonAccessibilityAttributes ("CodeGeneration.LanguageVersion",
			                                               GettextCatalog.GetString ("C# Language Version"),
			                                               GettextCatalog.GetString ("Select the version of C# to use"));
			langVerCombo.SetAccessibilityLabelRelationship (label2);

			allowUnsafeCodeCheckButton.SetCommonAccessibilityAttributes ("CodeGeneration.AllowUnsafe", null,
			                                                             GettextCatalog.GetString ("Check to allow 'unsafe' code"));
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
			var compileTarget =  (CompileTarget) compileTargetCombo.Active;

			var langVersion = LanguageVersion.Default;
			TreeIter iter;
			if (langVerCombo.GetActiveIter (out iter)) {
				langVersion = (LanguageVersion)langVerCombo.Model.GetValue (iter, 1);
			}

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
			
			var csproject = (CSharpProject)project; 
			
			csproject.CodePage = codePage;

			if (iconEntry.Sensitive)
				csproject.Win32Icon = iconEntry.Path;
			
			if (mainClassEntry.Sensitive)
				csproject.MainClass = mainClassEntry.Entry.Text;
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
				var ctx = TypeSystemService.GetCompilationAsync (project).Result;
				if (ctx == null)
					// Project not found in parser database
					return;
				foreach (var c in ctx.Assembly.GlobalNamespace.GetTypeMembers ()) {
					foreach (var m in c.GetMembers().OfType<IMethodSymbol> ()) {
						if (m.IsStatic && m.Name == "Main")
							classListStore.AppendValues (c.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
					}
				}
				classListFilled = true;
			} catch (InvalidOperationException) {
				// Project not found in parser database
			}
		}
	}
	
	class CompilerOptionsPanel : ItemOptionsPanel
	{
		CompilerOptionsPanelWidget widget;
		
		public override Control CreatePanelWidget ()
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
