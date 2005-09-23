// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using Gtk;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;

namespace JavaBinding
{
	public class ProjectConfigurationPropertyPanel : AbstractOptionPanel
	{
		class CodeGenerationPanelWidget : GladeWidgetExtract 
		{
			//
			// Gtk Controls	
			//
			[Glade.Widget] Entry symbolsEntry;
			[Glade.Widget] Entry mainClassEntry;
			[Glade.Widget] Entry compilerEntry;
			[Glade.Widget] Entry classPathEntry;
			[Glade.Widget] ComboBox compileTargetCombo;
			[Glade.Widget] CheckButton generateWarningsCheckButton;
			[Glade.Widget] CheckButton deprecationCheckButton;
			[Glade.Widget] CheckButton enableOptimizationCheckButton;
			[Glade.Widget] CheckButton warningsAsErrorsCheckButton;
			[Glade.Widget] CheckButton generateDebugInformationCheckButton;
			[Glade.Widget] RadioButton compilerGcjButton;
			[Glade.Widget] RadioButton compilerJavacButton;
			
			// compiler chooser
			
			JavaCompilerParameters compilerParameters = null;
			DotNetProjectConfiguration configuration;
			
 			public CodeGenerationPanelWidget (IProperties CustomizationObject) : base ("Java.glade", "CodeGenerationPanel")
 			{	
				configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				compilerParameters = (JavaCompilerParameters) configuration.CompilationParameters;
				
				ListStore store = new ListStore (typeof (string));
				store.AppendValues (GettextCatalog.GetString ("Executable"));
				store.AppendValues (GettextCatalog.GetString ("Library"));
				compileTargetCombo.Model = store;
				CellRendererText cr = new CellRendererText ();
				compileTargetCombo.PackStart (cr, true);
				compileTargetCombo.AddAttribute (cr, "text", 0);
				compileTargetCombo.Active = (int) configuration.CompileTarget;

				if (compilerParameters.Compiler == JavaCompiler.Javac)
					compilerJavacButton.Active = true;
				else
					compilerGcjButton.Active = true;
					
				compilerJavacButton.Toggled += new EventHandler (OnCompilerToggled);
				compilerGcjButton.Toggled += new EventHandler (OnCompilerToggled);
	
				enableOptimizationCheckButton.Active = compilerParameters.Optimize;
				generateDebugInformationCheckButton.Active = configuration.DebugMode;
				deprecationCheckButton.Active = compilerParameters.Deprecation;
				generateWarningsCheckButton.Active = compilerParameters.GenWarnings;
				warningsAsErrorsCheckButton.Active = !configuration.RunWithWarnings;
				
				compilerEntry.Text = compilerParameters.CompilerPath;
				classPathEntry.Text = compilerParameters.ClassPath;				
				mainClassEntry.Text = compilerParameters.MainClass;				
				symbolsEntry.Text = compilerParameters.DefineSymbols;				
			}
			
			void OnCompilerToggled (object o, EventArgs args)
			{
				if (compilerJavacButton.Active)
					compilerEntry.Text = "javac";
				else
					compilerEntry.Text = "gcj";
			}
			
			public bool Store ()
			{
				if (compilerParameters == null)
					return true;

				if (compilerJavacButton.Active)
					compilerParameters.Compiler = JavaCompiler.Javac;
				else
					compilerParameters.Compiler = JavaCompiler.Gcj;

				configuration.CompileTarget = (CompileTarget) compileTargetCombo.Active;
				compilerParameters.GenWarnings = generateWarningsCheckButton.Active;			
				compilerParameters.Deprecation = deprecationCheckButton.Active;			
				configuration.DebugMode = generateDebugInformationCheckButton.Active;			
				compilerParameters.Optimize = enableOptimizationCheckButton.Active;
				configuration.RunWithWarnings = !warningsAsErrorsCheckButton.Active;
				
				compilerParameters.CompilerPath = compilerEntry.Text;
				compilerParameters.ClassPath = classPathEntry.Text;
				compilerParameters.MainClass = mainClassEntry.Text;
				compilerParameters.DefineSymbols = symbolsEntry.Text;
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
