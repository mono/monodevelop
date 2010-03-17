
using System;
using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace JavaBinding
{
	public class GlobalOptionsPanelPanel : OptionsPanel
	{
		GlobalOptionsPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new GlobalOptionsPanelWidget();
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}

	partial class GlobalOptionsPanelWidget : Gtk.Bin 
	{
		public GlobalOptionsPanelWidget ()
		{
			Build ();
			
			ListStore store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Javac"));
			store.AppendValues (GettextCatalog.GetString ("Gcj"));
			compilerCombo.Model = store;
			CellRendererText cr = new CellRendererText ();
			compilerCombo.PackStart (cr, true);
			compilerCombo.AddAttribute (cr, "text", 0);
			compilerCombo.Active = (int) JavaLanguageBinding.Properties.CompilerType;
			
			ikvmPathEntry.Path = JavaLanguageBinding.Properties.IkvmPath;
			compilerPathEntry.Text = JavaLanguageBinding.Properties.CompilerCommand;
			classpathEntry.Text = JavaLanguageBinding.Properties.Classpath;
		}
		
		public bool Store ()
		{
			JavaLanguageBinding.Properties.IkvmPath = ikvmPathEntry.Path;
			JavaLanguageBinding.Properties.CompilerCommand = compilerPathEntry.Text;
			JavaLanguageBinding.Properties.Classpath = classpathEntry.Text;
			
			JavaLanguageBinding.Properties.CompilerType = (JavaCompiler) compilerCombo.Active;
			return true;
		}
	}
}
