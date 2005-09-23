// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using Gtk;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Services;

namespace ILAsmBinding
{
	public class CompilerParametersPanel : AbstractOptionPanel
	{
		ILAsmCompilerParameters compilerParameters = null;
		DotNetProjectConfiguration configuration;
		
		Entry outputPath = new Entry ();
		Entry assemblyName = new Entry ();
		RadioButton exeTarget = new RadioButton ("exe");
		RadioButton dllTarget;
		CheckButton debug = new CheckButton (GettextCatalog.GetString ("Include debug information"));
		
		public override void LoadPanelContents()
		{
			configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
			compilerParameters = (ILAsmCompilerParameters) configuration.CompilationParameters;
			
			dllTarget = new RadioButton (exeTarget, "dll");
			SetupUI ();
			RestoreValues ();
			this.ShowAll ();
		}
		
		public override bool StorePanelContents()
		{
			configuration.OutputAssembly = assemblyName.Text;
			configuration.OutputDirectory = outputPath.Text;
			configuration.DebugMode = debug.Active;
			if (exeTarget.Active)
				configuration.CompileTarget = CompileTarget.Exe;
			else
				configuration.CompileTarget = CompileTarget.Library;
				
			return true;
		}

		void SetupUI ()
		{
			VBox vbox = new VBox (false, 6);
			Label outputLabel = new Label ();
			outputLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Output path"));
			vbox.PackStart (outputLabel, false, true, 0);
			vbox.PackStart (outputPath, false, true, 0);
			Label assemblyLabel = new Label ();
			assemblyLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Assembly name"));
			vbox.PackStart (assemblyLabel, false, true, 0);
			vbox.PackStart (assemblyName, false, true, 0);
			Label targetLabel = new Label ();
			targetLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Target options"));
			vbox.PackStart (targetLabel, false, true, 0);
			vbox.PackStart (exeTarget, false, true, 0);
			vbox.PackStart (dllTarget, false, true, 0);
			vbox.PackStart (debug, false, true, 0);
			this.Add (vbox);
		}

		void RestoreValues ()
		{
			assemblyName.Text = configuration.OutputAssembly;
			outputPath.Text = configuration.OutputDirectory;
			if (configuration.CompileTarget == CompileTarget.Exe)
				exeTarget.Active = true;
			else
				dllTarget.Active = true;
			debug.Active = configuration.DebugMode;
		}
	}
}
