
using System;

namespace TextEditor
{
	public class PasteCommand: ICommand
	{
		public void Run ()
		{
			Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			TextEditorApp.MainWindow.View.Buffer.PasteClipboard (clipboard);
		}
	}
}
