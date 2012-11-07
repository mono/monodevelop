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
		public Gtk.Entry EntryDefines { get { return this.entryDefines; } }
		public Gtk.CheckButton CheckDebugInfo { get { return this.checkDebugInfo; } }
		public Gtk.CheckButton CheckOptimize { get { return this.checkOptimize; } }
		public Gtk.CheckButton CheckTailCalls { get { return this.checkTailCalls; } }
		public Gtk.CheckButton CheckXmlDocumentation { get { return this.checkXmlDocumentation; } }
	}
}

