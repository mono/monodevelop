using System;
namespace MonoDevelop.FSharp.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FSharpCompilerOptionsWidget : Gtk.Bin
	{
		public FSharpCompilerOptionsWidget ()
		{
			this.Build ();
		}
		
		public Gtk.Entry EntryCommandLine { get { return this.entryCustomParameters; } }
		public Gtk.Entry EntryDefines { get { return this.symbolsEntry; } }
		public Gtk.Entry EntryWarnings { get { return this.ignoreWarningsEntry; } }
		public Gtk.CheckButton CheckOptimize { get { return this.enableOptimizationCheckButton; } }
		public Gtk.CheckButton CheckTailCalls { get { return this.checkTailCalls; } }
		public Gtk.CheckButton CheckXmlDocumentation { get { return this.generateXmlOutputCheckButton; } }
		public Gtk.ComboBox ComboDebugInformation { get { return this.comboDebug; } }
		public Gtk.CheckButton CheckDebugInformation { get { return this.generateOverflowChecksCheckButton; } }
		public Gtk.ComboBox ComboPlatforms { get { return this.comboPlatforms; } }
		public Gtk.CheckButton CheckWarningsAsErrors { get { return this.warningsAsErrorsCheckButton; } }
		public Gtk.SpinButton WarningLevelSpinButton { get { return this.warningLevelSpinButton; } }
	}
}

