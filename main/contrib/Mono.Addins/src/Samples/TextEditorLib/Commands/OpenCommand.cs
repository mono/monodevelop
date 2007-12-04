
using System;
using System.IO;

namespace TextEditor
{
	public class OpenCommand: ICommand
	{
		public void Run ()
		{
			Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog ("Open File", null, Gtk.FileChooserAction.Open);
			fcd.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Open, Gtk.ResponseType.Ok);
			fcd.DefaultResponse = Gtk.ResponseType.Ok;
			fcd.SelectMultiple = false;

			Gtk.ResponseType response = (Gtk.ResponseType) fcd.Run ();
			if (response == Gtk.ResponseType.Ok)
				TextEditorApp.OpenFile (fcd.Filename);
			fcd.Destroy ();
		}
	}
}
