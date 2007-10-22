
using System;
using Mono.Addins;

namespace TextEditor.CompilerService
{
	public class CompilerManager
	{
		public static void Run (string file)
		{
			ICompiler[] compilers = (ICompiler[]) AddinManager.GetExtensionObjects (typeof(ICompiler));
			
			ICompiler compiler = null;
			foreach (ICompiler comp in compilers) {
				if (comp.CanCompile (file)) {
					compiler = comp;
					break;
				}
			}
			if (compiler == null) {
				string msg = "No compiler available for this kind of file.";
				Gtk.MessageDialog dlg = new Gtk.MessageDialog (TextEditorApp.MainWindow, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Close, msg);
				dlg.Run ();
				dlg.Destroy ();
				return;
			}

			string messages = compiler.Compile (file, file + ".exe");
			
			TextEditorApp.MainWindow.ConsoleWrite ("Compilation finished.\n");
			TextEditorApp.MainWindow.ConsoleWrite (messages);
		}
	}
}
