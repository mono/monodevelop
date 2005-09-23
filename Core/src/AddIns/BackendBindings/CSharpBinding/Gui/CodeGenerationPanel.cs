// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Internal.Parser;

using Gtk;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Services;

namespace CSharpBinding
{
	//FIXME: i8n 
	public class CodeGenerationPanel : AbstractOptionPanel
	{

		class CodeGenerationPanelWidget : GladeWidgetExtract 
		{
			//
			// Gtk Controls	
			//
 			[Glade.Widget] Entry symbolsEntry;
 			[Glade.Widget] ComboBoxEntry mainClassEntry;
			[Glade.Widget] ComboBox compileTargetCombo;
 			[Glade.Widget] CheckButton generateOverflowChecksCheckButton;
			[Glade.Widget] CheckButton allowUnsafeCodeCheckButton;
 			[Glade.Widget] CheckButton enableOptimizationCheckButton;
 			[Glade.Widget] CheckButton warningsAsErrorsCheckButton;
 			[Glade.Widget] CheckButton generateDebugInformationCheckButton;
 			[Glade.Widget] CheckButton generateXmlOutputCheckButton;
 			[Glade.Widget] SpinButton warningLevelSpinButton;

			Project project;
			DotNetProjectConfiguration configuration;
			CSharpCompilerParameters compilerParameters = null;
			ListStore classListStore;
			bool classListFilled;

 			public  CodeGenerationPanelWidget(IProperties CustomizationObject) : base ("CSharp.glade", "CodeGenerationPanel")
 			{	
				configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				project = (Project)((IProperties)CustomizationObject).GetProperty("Project");
				compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters;
				
				ListStore store = new ListStore (typeof (string));
				store.AppendValues (GettextCatalog.GetString ("Executable"));
				store.AppendValues (GettextCatalog.GetString ("Library"));
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

				UpdateTarget ();
				
				generateDebugInformationCheckButton.Active = configuration.DebugMode;
				generateXmlOutputCheckButton.Active        = compilerParameters.GenerateXmlDocumentation;
				enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
				allowUnsafeCodeCheckButton.Active          = compilerParameters.UnsafeCode;
				generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
				warningsAsErrorsCheckButton.Active         = ! configuration.RunWithWarnings;
				warningLevelSpinButton.Value               = compilerParameters.WarningLevel;				
 			}
			
			void FillClasses ()
			{
				IParserContext ctx = Runtime.ProjectService.ParserDatabase.GetProjectParserContext (project);
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
				} else {
					mainClassEntry.Sensitive = true;
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

				return true;
			}
		}

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
