
using System;
using System.IO;
using System.Xml;
using TextEditor;
using Mono.Addins;

[assembly: Addin (Namespace="TextEditor")]
[assembly: AddinDependency ("Core", "1.0")]


namespace TextEditor.Xml
{
	public class FormatXmlCommand: ICommand
	{
		public void Run ()
		{
			string text = TextEditorApp.MainWindow.View.Buffer.Text;
			XmlDocument doc = new XmlDocument ();
			try {
				doc.LoadXml (text);
				StringWriter sw = new StringWriter ();
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				doc.Save (tw);
				TextEditorApp.MainWindow.View.Buffer.Text = sw.ToString ();
			}
			catch {
				Gtk.MessageDialog dlg = new Gtk.MessageDialog (TextEditorApp.MainWindow, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Close, "Error parsing XML.");
				dlg.Run ();
				dlg.Destroy ();
			}
		}
	}
	
	class Subno: TextEditor.CopyCommand
	{
	}
}
