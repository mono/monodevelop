
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
	public class GlobalOptionsPanelPanel : AbstractOptionPanel
	{
		class GlobalOptionsPanelWidget : GladeWidgetExtract 
		{
			//
			// Gtk Controls	
			//
			[Glade.Widget] Gnome.FileEntry ikvmPathEntry;
			[Glade.Widget] Entry compilerPathEntry;
			[Glade.Widget] Entry classpathEntry;
			[Glade.Widget] ComboBox compilerCombo;
			
			// compiler chooser
			
 			public GlobalOptionsPanelWidget () : base ("Java.glade", "GlobalOptionsPanel")
 			{
				ListStore store = new ListStore (typeof (string));
				store.AppendValues (GettextCatalog.GetString ("Javac"));
				store.AppendValues (GettextCatalog.GetString ("Gcj"));
				compilerCombo.Model = store;
				CellRendererText cr = new CellRendererText ();
				compilerCombo.PackStart (cr, true);
				compilerCombo.AddAttribute (cr, "text", 0);
				compilerCombo.Active = (int) JavaLanguageBinding.Properties.CompilerType;
				
				ikvmPathEntry.Filename = JavaLanguageBinding.Properties.IkvmPath;
				compilerPathEntry.Text = JavaLanguageBinding.Properties.CompilerCommand;
				classpathEntry.Text = JavaLanguageBinding.Properties.Classpath;
			}
			
			public bool Store ()
			{
				JavaLanguageBinding.Properties.IkvmPath = ikvmPathEntry.Filename;
				JavaLanguageBinding.Properties.CompilerCommand = compilerPathEntry.Text;
				JavaLanguageBinding.Properties.Classpath = classpathEntry.Text;
				
				JavaLanguageBinding.Properties.CompilerType = (JavaCompiler) compilerCombo.Active;
				return true;
			}
		}

		GlobalOptionsPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  GlobalOptionsPanelWidget ());
		}
		
		public override bool StorePanelContents()
		{
			bool result = true;
			result = widget.Store ();
 			return result;
		}
	}
}
