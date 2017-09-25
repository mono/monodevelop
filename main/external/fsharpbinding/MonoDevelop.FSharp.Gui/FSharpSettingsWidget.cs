using System;
namespace MonoDevelop.FSharp.Gui
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class FSharpSettingsWidget : Gtk.Bin
	{
		public FSharpSettingsWidget ()
		{
			this.Build ();
		}
        public Gtk.CheckButton AdvanceLine => advanceToNextLineCheckbox;
        public Gtk.CheckButton CheckCompilerUseDefault => checkCompilerUseDefault;

        public Gtk.Button ButtonCompilerBrowse => buttonCompilerBrowse;
        public Gtk.Entry EntryCompilerPath => entryCompilerPath;
        public Gtk.CheckButton CheckHighlightMutables => checkHighlightMutables;
        public Gtk.CheckButton CheckTypeSignatures => checkTypeSignatures;
        public Gtk.CheckButton CheckStatusBarTooltips => checkStatusBarTooltips;

    }
}
