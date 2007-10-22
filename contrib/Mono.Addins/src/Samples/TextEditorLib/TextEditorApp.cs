
using System;
using System.IO;

namespace TextEditor
{
	public class TextEditorApp
	{
		static string openFile = "";
		
		private TextEditorApp()
		{
		}
		
		public static string OpenFileName {
			get { return openFile; }
		}
		
		public static MainWindow MainWindow {
			get { return MainWindow.Instance; }
		}
		
		public static void OpenFile (string file)
		{
			using (StreamReader sr = new StreamReader (file)) {
				MainWindow.View.Buffer.Text = sr.ReadToEnd ();
			}
			SetOpenFile (file);
		}
		
		public static void SaveFile ()
		{
			if (openFile == "") {
				Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog ("Save File", null, Gtk.FileChooserAction.Save);
				fcd.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
				fcd.AddButton (Gtk.Stock.Open, Gtk.ResponseType.Ok);
				fcd.DefaultResponse = Gtk.ResponseType.Ok;
				fcd.SelectMultiple = false;

				Gtk.ResponseType response = (Gtk.ResponseType) fcd.Run ();
				if (response != Gtk.ResponseType.Ok) {
					fcd.Destroy ();
					return;
				}
				
				SetOpenFile (fcd.Filename);
				fcd.Destroy ();
			}
			using (StreamWriter sr = new StreamWriter (openFile)) {
				sr.Write (TextEditorApp.MainWindow.View.Buffer.Text);
			}
		}
		
		public static void NewFile (string content)
		{
			SetOpenFile ("");
			MainWindow.View.Buffer.Text = content;
		}
		
		static void SetOpenFile (string file)
		{
			openFile = file;
			if (file.Length > 0)
				MainWindow.Title = Path.GetFileName (file);
			else
				MainWindow.Title = "New File";
			
			if (OpenFileChanged != null)
				OpenFileChanged (null, EventArgs.Empty);
		}
		
		public static event EventHandler OpenFileChanged;
	}
}
