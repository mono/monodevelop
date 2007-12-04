
using System;

namespace TextEditor
{
	public class CopyCommand: ICommand
	{
		public void Run ()
		{
			Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			TextEditorApp.MainWindow.View.Buffer.CopyClipboard (clipboard);
		}
	}
}
