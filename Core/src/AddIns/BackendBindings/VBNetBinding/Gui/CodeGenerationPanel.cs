// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
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

using Gtk;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Services;

namespace VBBinding
{
	public class CodeGenerationPanel : AbstractOptionPanel
	{
		VBCompilerParameters compilerParameters = null;
		DotNetProjectConfiguration configuration;
	
		/*
		
		ResourceService resourceService = (ResourceService)ServiceManager.GetService(typeof(IResourceService));
		static FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
		
		public override bool ReceiveDialogMessage(DialogMessage message)
		{
			if (message == DialogMessage.OK) {
				if (compilerParameters == null) {
					return true;
				}
				FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
				
				
				compilerParameters.DefineSymbols = ControlDictionary["symbolsTextBox"].Text;
				compilerParameters.MainClass     = ControlDictionary["mainClassTextBox"].Text;
				compilerParameters.Imports       = ControlDictionary["importsTextBox"].Text;
				compilerParameters.RootNamespace = ControlDictionary["RootNamespaceTextBox"].Text;
				
				compilerParameters.Debugmode = ((CheckBox)ControlDictionary["generateDebugInformationCheckBox"]).Checked;
				compilerParameters.Optimize = ((CheckBox)ControlDictionary["enableOptimizationCheckBox"]).Checked;
				compilerParameters.GenerateOverflowChecks = ((CheckBox)ControlDictionary["generateOverflowChecksCheckBox"]).Checked;
				compilerParameters.TreatWarningsAsErrors  = ((CheckBox)ControlDictionary["warningsAsErrorsCheckBox"]).Checked;
				
				compilerParameters.OptionExplicit = ((CheckBox)ControlDictionary["optionExplicitCheckBox"]).Checked ;
				compilerParameters.OptionStrict = ((CheckBox)ControlDictionary["optionStrictCheckBox"]).Checked;
			}
			return true;
		}
		
		void SetValues(object sender, EventArgs e)
		{
			this.compilerParameters = (VBCompilerParameters)((IProperties)CustomizationObject).GetProperty("Config");
			
			ControlDictionary["symbolsTextBox"].Text = compilerParameters.DefineSymbols;
			ControlDictionary["mainClassTextBox"].Text = compilerParameters.MainClass;
			ControlDictionary["importsTextBox"].Text = compilerParameters.Imports;
			ControlDictionary["RootNamespaceTextBox"].Text = compilerParameters.RootNamespace;
			
			
			((CheckBox)ControlDictionary["generateDebugInformationCheckBox"]).Checked = compilerParameters.Debugmode;
			((CheckBox)ControlDictionary["enableOptimizationCheckBox"]).Checked = compilerParameters.Optimize;
			((CheckBox)ControlDictionary["generateOverflowChecksCheckBox"]).Checked = compilerParameters.GenerateOverflowChecks;
			((CheckBox)ControlDictionary["warningsAsErrorsCheckBox"]).Checked = compilerParameters.TreatWarningsAsErrors;
			
			((CheckBox)ControlDictionary["optionExplicitCheckBox"]).Checked = compilerParameters.OptionExplicit;
			((CheckBox)ControlDictionary["optionStrictCheckBox"]).Checked = compilerParameters.OptionStrict;
		}
		
		static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		public CodeGenerationPanel() : base(propertyService.DataDirectory + @"\resources\panels\ProjectOptions\VBNetCodeGenerationPanel.xfrm")
		{
			CustomizationObjectChanged += new EventHandler(SetValues);
			
		}
		*/
		
			/* public override bool ReceiveDialogMessage(DialogMessage message)
			{
				if (message == DialogMessage.OK) {
					return widget.Store();
				}
				return true;
			}

		
			void SetValues(object sender, EventArgs e){
				LoadPanelContents();		
			}
			
		public CodeGenerationPanel() : base()
		{
			CustomizationObjectChanged += new EventHandler(SetValues);
		} */


		
		class CodeGenerationPanelWidget : GladeWidgetExtract 
		{
			//
			// Gtk Controls	
			//
 			[Glade.Widget] Entry symbolsEntry;
 			[Glade.Widget] Entry mainClassEntry;
			[Glade.Widget] ComboBox compileTargetCombo;
 			[Glade.Widget] CheckButton generateOverflowChecksCheckButton;
			[Glade.Widget] CheckButton allowUnsafeCodeCheckButton;
 			[Glade.Widget] CheckButton enableOptimizationCheckButton;
 			[Glade.Widget] CheckButton warningsAsErrorsCheckButton;
//			[Glade.Widget] CheckButton generateDebugInformationCheckButton;
 			[Glade.Widget] CheckButton generateXmlOutputCheckButton;
 			[Glade.Widget] SpinButton warningLevelSpinButton;

			//
			// services needed
			//
			StringParserService StringParserService = (StringParserService)ServiceManager.GetService (
				typeof (StringParserService));

			DotNetProjectConfiguration configuration;
			VBCompilerParameters compilerParameters = null;
			
			//static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			

 			public  CodeGenerationPanelWidget(IProperties CustomizationObject) : base ("VB.glade", "CodeGenerationPanel")
 			{	
				configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				compilerParameters = (VBCompilerParameters) configuration.CompilationParameters;
				SetValues();
				//CustomizationObjectChanged += new EventHandler(SetValues);
 			}
 			
			public void SetValues(){
				//this.compilerParameters = (VBCompilerParameters)((IProperties)CustomizationObject).GetProperty("Config");

				// FIXME: Enable when mbas has this feature
				generateXmlOutputCheckButton.Sensitive = false;
				
				ListStore store = new ListStore (typeof (string));
				store.AppendValues (GettextCatalog.GetString ("Executable"));
				store.AppendValues (GettextCatalog.GetString ("WinEXE"));
				store.AppendValues (GettextCatalog.GetString ("Library"));
				store.AppendValues (GettextCatalog.GetString ("Module")); 

				compileTargetCombo.Model = store;
				CellRendererText cr = new CellRendererText ();
				compileTargetCombo.PackStart (cr, true);
				compileTargetCombo.AddAttribute (cr, "text", 0);
				compileTargetCombo.Active = (int) configuration.CompileTarget;

				symbolsEntry.Text = compilerParameters.DefineSymbols;
				mainClassEntry.Text = compilerParameters.MainClass;

//				generateDebugInformationCheckButton.Active = compilerParameters.Debugmode;
				generateXmlOutputCheckButton.Active        = compilerParameters.GenerateXmlDocumentation;
				enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
				allowUnsafeCodeCheckButton.Active          = compilerParameters.UnsafeCode;
				generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
				warningsAsErrorsCheckButton.Active         = ! configuration.RunWithWarnings;
				warningLevelSpinButton.Value               = compilerParameters.WarningLevel;		
			} 


			public bool Store ()
			{	
				if (compilerParameters == null) {
					System.Console.WriteLine("NULL compiler parameters for VBNet!");
					return true;
				}
				//compilerParameters.CompileTarget =  (CompileTarget)  compileTargetCombo.Active;
				configuration.CompileTarget = (CompileTarget) compileTargetCombo.Active;
				compilerParameters.DefineSymbols =  symbolsEntry.Text;
				compilerParameters.MainClass     =  mainClassEntry.Text;

//				compilerParameters.Debugmode                = generateDebugInformationCheckButton.Active;
				compilerParameters.GenerateXmlDocumentation = generateXmlOutputCheckButton.Active;
				compilerParameters.Optimize                 = enableOptimizationCheckButton.Active;
				compilerParameters.UnsafeCode               = allowUnsafeCodeCheckButton.Active;
				compilerParameters.GenerateOverflowChecks   = generateOverflowChecksCheckButton.Active;
				configuration.RunWithWarnings          = ! warningsAsErrorsCheckButton.Active;

				compilerParameters.WarningLevel = warningLevelSpinButton.ValueAsInt;

				return true;
			}
		}//CodeGenerationPanelWidget
				
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
