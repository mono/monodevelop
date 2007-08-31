using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core;
using Mono.Addins;

using Gtk;
using MonoDevelop.Components;

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
 			[Glade.Widget] CheckButton greedy;
 			[Glade.Widget] CheckButton pedantic;
 			
			NemerleParameters compilerParameters = null;
			DotNetProjectConfiguration configuration;

 			public  CodeGenerationPanelWidget(Properties CustomizationObject) : base ("Nemerle.glade", "CodeGenerationPanel")
 			{
				configuration = (DotNetProjectConfiguration) ((Properties)CustomizationObject).Get("Config");
				compilerParameters = (NemerleParameters) configuration.CompilationParameters;
				
				target.Active = (int) configuration.CompileTarget;
				
				nostdmacros.Active = compilerParameters.Nostdmacros;
				nostdlib.Active    = compilerParameters.Nostdlib;
				ignorewarnings.Active = configuration.RunWithWarnings;
				ot.Active          = compilerParameters.Ot;
				greedy.Active      = compilerParameters.Greedy;
				pedantic.Active    = compilerParameters.Pedantic;
 			}

			public bool Store ()
			{	
				configuration.CompileTarget = (CompileTarget) target.Active;
				compilerParameters.Nostdmacros = nostdmacros.Active;
				compilerParameters.Nostdlib = nostdlib.Active;
				configuration.RunWithWarnings = ignorewarnings.Active;
				compilerParameters.Ot = ot.Active;
				compilerParameters.Greedy = greedy.Active;
				compilerParameters.Pedantic = pedantic.Active;
				return true;
			}
		}

		CodeGenerationPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  CodeGenerationPanelWidget ((Properties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
 			return  widget.Store ();
		}
	}
}
