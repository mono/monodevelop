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

namespace PythonBinding
{
	public class CompilerParametersPanel : AbstractOptionPanel
	{
		PythonCompilerParameters compilerParameters = null;
		Entry outputPath = new Entry ();
		Entry assemblyName = new Entry ();
		RadioButton exeTarget = new RadioButton ("exe");
		RadioButton dllTarget;
		CheckButton debug = new CheckButton (GettextCatalog.GetString ("Include debug information"));
		
		public override void LoadPanelContents()
		{
			this.compilerParameters = (PythonCompilerParameters)((IProperties)CustomizationObject).GetProperty("Config");
			
			dllTarget = new RadioButton (exeTarget, "dll");
			SetupUI ();
			RestoreValues ();
			this.ShowAll ();
		}
		
		public override bool StorePanelContents()
		{
			compilerParameters.AssemblyName = assemblyName.Text;
			compilerParameters.OutputPath = outputPath.Text;
			compilerParameters.IncludeDebugInformation = debug.Active;
			if (exeTarget.Active)
				compilerParameters.CompilationTarget = CompilationTarget.Exe;
			else
				compilerParameters.CompilationTarget = CompilationTarget.Dll;
				
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
			assemblyName.Text = compilerParameters.AssemblyName;
			outputPath.Text = compilerParameters.OutputPath;
			if (compilerParameters.CompilationTarget == CompilationTarget.Exe)
				exeTarget.Active = true;
			else
				dllTarget.Active = true;
			debug.Active = compilerParameters.IncludeDebugInformation;
		}
	}
}
