// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using Gtk;
using GLib;

namespace VBBinding
{
	public class ChooseRuntimePanel : AbstractOptionPanel
	{
		VBCompilerParameters parameters;
		DotNetProjectConfiguration configuration;
		
		// FIXME: set the right rb groups
		RadioButton monoRadioButton;
		RadioButton mintRadioButton;
		RadioButton msnetRadioButton;
	
		RadioButton mbasRadioButton;
		RadioButton vbcRadioButton;
		
		Label labelCompiler=new Label(GettextCatalog.GetString("Compiler:"));
		Label labelRuntime=new Label(GettextCatalog.GetString("Runtime:"));

		//For grouping
		//SList compilers=new SList(null);
		//SList runtimes=new SList(null);
		
		public ChooseRuntimePanel(): base(){
			InitializeComponent ();						
			VBox vbox = new VBox ();
			//HBox hboxTitle = new HBox ();
			//hboxTitle.PackStart (titleLabel, false, false, 0);
			//vbox.PackStart (hboxTitle);
			//vbox.PackStart (outputAssembly);
			
			HBox hboxCompiler = new HBox ();
			hboxCompiler.PackStart (labelCompiler, false, false, 0);
			vbox.PackStart (hboxCompiler);
			VBox comps = new VBox ();
			comps.PackStart (mbasRadioButton);
			comps.PackStart (vbcRadioButton);
			vbox.PackStart (comps);
			//vbox.PackStart (compilerPath);
			HBox hboxRuntime = new HBox ();
			hboxRuntime.PackStart (labelRuntime, false, false, 0);
			VBox runs=new VBox();
			runs.PackStart(monoRadioButton);
			runs.PackStart(mintRadioButton);
			runs.PackStart(msnetRadioButton);
			vbox.PackStart (hboxRuntime);
			vbox.PackStart(runs);
			/* HBox hboxClasspath = new HBox ();
			hboxClasspath.PackStart (labelClasspath, false, false, 0);
			vbox.PackStart (hboxClasspath);
			vbox.PackStart (classPath);
			HBox hboxMainClass = new HBox ();
			hboxMainClass.PackStart (labelMainClass, false, false, 0);
			vbox.PackStart (hboxMainClass);
			vbox.PackStart (mainClass);
			HBox hboxWarnings = new HBox ();
			hboxWarnings.PackStart (labelWarnings, false, false, 0);
			vbox.PackStart (hboxWarnings);
			HBox hbox = new HBox ();
			hbox.PackStart (checkDeprecation);
			hbox.PackStart (checkDebug);
			hbox.PackStart (checkOptimize);
			vbox.PackStart (hbox);
			HBox hboxOutput = new HBox ();
			hboxOutput.PackStart (labelOutput, false, false, 0);
			vbox.PackStart (hboxOutput);
			vbox.PackStart (outputDirectory); */
			this.Add (vbox);
		}
		
		private void InitializeComponent(){
		/*	runtimes.Append(monoRadioButton);
			runtimes.Append(mintRadioButton);
			runtimes.Append(msnetRadioButton);
			compilers.Append(mbasRadioButton);
			compilers.Append(vbcRadioButton);
			
			msnetRadioButton.Group=runtimes;
			monoRadioButton.Group=runtimes;
			mintRadioButton.Group=runtimes;
			vbcRadioButton.Group=compilers;
			mbasRadioButton.Group=compilers; */
			
			monoRadioButton = new RadioButton ("Mono");
			mintRadioButton = new RadioButton (monoRadioButton,"Mint");
			msnetRadioButton = new RadioButton (monoRadioButton,"Msnet");
		
			mbasRadioButton = new RadioButton ("MBAS");
			vbcRadioButton = new RadioButton (mbasRadioButton,"VBC");
		}
		
		public override void LoadPanelContents()
		{
			configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
			parameters = (VBCompilerParameters) configuration.CompilationParameters;
			
			msnetRadioButton.Active = config.NetRuntime == NetRuntime.MsNet;
			monoRadioButton.Active  = config.NetRuntime == NetRuntime.Mono;
			mintRadioButton.Active  = config.NetRuntime == NetRuntime.MonoInterpreter;
			
			vbcRadioButton.Active = parameters.VBCompiler == VBCompiler.Vbc;
			mbasRadioButton.Active = parameters.VBCompiler == VBCompiler.Mbas;
		}
		
		public override bool StorePanelContents()
		{
			if (msnetRadioButton.Active) {
				config.NetRuntime =  NetRuntime.MsNet;
			} else if (monoRadioButton.Active) {
				config.NetRuntime =  NetRuntime.Mono;
			} else {
				config.NetRuntime =  NetRuntime.MonoInterpreter;
			}
			parameters.VBCompiler = vbcRadioButton.Active ? VBCompiler.Vbc : VBCompiler.Mbas;
			
			return true;
		}
	}
}
