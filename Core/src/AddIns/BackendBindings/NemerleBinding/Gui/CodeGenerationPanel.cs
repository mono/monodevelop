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

namespace NemerleBinding
{
	public class CodeGenerationPanel : AbstractOptionPanel
	{
		class CodeGenerationPanelWidget : GladeWidgetExtract 
		{
 			[Glade.Widget] ComboBox target;
 			[Glade.Widget] CheckButton nostdmacros;
			[Glade.Widget] CheckButton nostdlib;
			[Glade.Widget] CheckButton ignorewarnings;
 			[Glade.Widget] CheckButton ot;
 			[Glade.Widget] CheckButton obcm;
 			[Glade.Widget] CheckButton oocm;
 			[Glade.Widget] CheckButton oscm;
 			
			NemerleParameters compilerParameters = null;
			DotNetProjectConfiguration configuration;

 			public  CodeGenerationPanelWidget(IProperties CustomizationObject) : base ("Nemerle.glade", "CodeGenerationPanel")
 			{
				configuration = (DotNetProjectConfiguration) ((IProperties)CustomizationObject).GetProperty("Config");
				compilerParameters = (NemerleParameters) configuration.CompilationParameters;
				
				target.Active = (int) configuration.CompileTarget;
				
				nostdmacros.Active = compilerParameters.Nostdmacros;
				nostdlib.Active    = compilerParameters.Nostdlib;
				ignorewarnings.Active = configuration.RunWithWarnings;
				ot.Active          = compilerParameters.Ot;
				obcm.Active        = compilerParameters.Obcm;
				oocm.Active        = compilerParameters.Oocm;
				oscm.Active        = compilerParameters.Oscm;
 			}

			public bool Store ()
			{	
				configuration.CompileTarget = (CompileTarget) target.Active;
				compilerParameters.Nostdmacros = nostdmacros.Active;
				compilerParameters.Nostdlib = nostdlib.Active;
				configuration.RunWithWarnings = ignorewarnings.Active;
				compilerParameters.Ot = ot.Active;
				compilerParameters.Obcm = obcm.Active;
				compilerParameters.Oocm = oocm.Active;
				compilerParameters.Oscm = oscm.Active;
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
 			return  widget.Store ();
		}
	}
}
