
using System;

namespace TextEditor
{
	
	
	public class CutCommand: ICommand
	{
		public void Run ()
		{
			Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			TextEditorApp.MainWindow.View.Buffer.CutClipboard (clipboard, true);
		}
	}
}
