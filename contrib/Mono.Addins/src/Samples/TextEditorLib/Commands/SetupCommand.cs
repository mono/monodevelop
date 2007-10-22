
using System;
using Mono.Addins.Gui;

namespace TextEditor
{
	public class SetupCommand: ICommand
	{
		public void Run ()
		{
			AddinManagerWindow.Run (TextEditorApp.MainWindow);
		}
	}
}
