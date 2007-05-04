// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Drawing;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using Mono.Addins;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;

using Gtk;
using MonoDevelop.Components;

namespace CSharpBinding
{
	partial class CodeGenerationPanelWidget : Gtk.Bin 
	{
		Project project;
		DotNetProjectConfiguration configuration;
		CSharpCompilerParameters compilerParameters = null;
		ListStore classListStore;
		bool classListFilled;

		public 
		CodeGenerationPanelWidget (IProperties CustomizationObject)
		{
			Build ();
			
			configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
			project = (Project)((IProperties)CustomizationObject).GetProperty("Project");
			compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			
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

			symbolsEntry.Text = compilerParameters.DefineSymbols;
			
			classListStore = new ListStore (typeof(string));
			mainClassEntry.Model = classListStore;
			mainClassEntry.TextColumn = 0;
			((Entry)mainClassEntry.Child).Text = compilerParameters.MainClass;

			// Load the codepage. If it matches any of the supported encodigs, use the encoding name 			
			string foundEncoding = null;
			foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
				if (e.CodePage == -1)
					continue;
				if (e.CodePage == compilerParameters.CodePage)
					foundEncoding = e.Id;
				codepageEntry.AppendText (e.Id);
			}
			if (foundEncoding != null)
				codepageEntry.Entry.Text = foundEncoding;
			else if (compilerParameters.CodePage != 0)
				codepageEntry.Entry.Text = compilerParameters.CodePage.ToString ();

			UpdateTarget ();
			
			generateDebugInformationCheckButton.Active = configuration.DebugMode;
			generateXmlOutputCheckButton.Active        = compilerParameters.GenerateXmlDocumentation;
			enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
			allowUnsafeCodeCheckButton.Active          = compilerParameters.UnsafeCode;
			generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
			warningsAsErrorsCheckButton.Active         = ! configuration.RunWithWarnings;
			warningLevelSpinButton.Value               = compilerParameters.WarningLevel;
			
			iconEntry.Path = compilerParameters.Win32Icon;
			iconEntry.DefaultPath = project.BaseDirectory;
		}
		
		void FillClasses ()
		{
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
			foreach (IClass c in ctx.GetProjectContents ()) {
				if (c.Methods != null) {
					foreach (IMethod m in c.Methods) {
						if (m.IsStatic && m.Name == "Main")
							classListStore.AppendValues (c.FullyQualifiedName);
					}
				}
			}
		}
		
		void UpdateTarget ()
		{
			if ((CompileTarget) compileTargetCombo.Active == CompileTarget.Library) {
				mainClassEntry.Sensitive = false;
				iconEntry.Sensitive = false;
			} else {
				mainClassEntry.Sensitive = true;
				iconEntry.Sensitive = true;
				if (!classListFilled) {
					FillClasses ();
					classListFilled = true;
				}
			}
		}
		
		void OnTargetChanged (object s, EventArgs a)
		{
			UpdateTarget ();
		}

		public bool Store ()
		{	
			if (compilerParameters == null) {
				return true;
			}
			configuration.CompileTarget =  (CompileTarget) compileTargetCombo.Active;
			compilerParameters.DefineSymbols =  symbolsEntry.Text;
			compilerParameters.MainClass     =  ((Entry)mainClassEntry.Child).Text;

			configuration.DebugMode                = generateDebugInformationCheckButton.Active;
			compilerParameters.GenerateXmlDocumentation = generateXmlOutputCheckButton.Active;
			compilerParameters.Optimize                 = enableOptimizationCheckButton.Active;
			compilerParameters.UnsafeCode               = allowUnsafeCodeCheckButton.Active;
			compilerParameters.GenerateOverflowChecks   = generateOverflowChecksCheckButton.Active;
			configuration.RunWithWarnings          = ! warningsAsErrorsCheckButton.Active;

			compilerParameters.WarningLevel = warningLevelSpinButton.ValueAsInt;
			compilerParameters.Win32Icon = iconEntry.Path;

			if (codepageEntry.Entry.Text.Length > 0) {
				// Get the codepage. If the user specified an encoding name, find it.			
				int codepage = -1;
				foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
					if (e.Id == codepageEntry.Entry.Text) {
						codepage = e.CodePage;
						break;
					}
				}
				
				if (codepage != -1)
					compilerParameters.CodePage = codepage;
				else {
					if (!int.TryParse (codepageEntry.Entry.Text, out codepage)) {
						IdeApp.Services.MessageService.ShowError (GettextCatalog.GetString ("Invalid code page number."));
						return false;
					}
					compilerParameters.CodePage = codepage;
				}
			} else
				compilerParameters.CodePage = 0;

			return true;
		}
	}
	

	public class CodeGenerationPanel : AbstractOptionPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  CodeGenerationPanelWidget ((IProperties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			bool result = true;
			result = widget.Store ();
 			return result;
		}
	}
}
