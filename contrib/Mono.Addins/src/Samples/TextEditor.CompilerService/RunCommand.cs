
using System;
using TextEditor;
using Mono.Addins;

namespace TextEditor.CompilerService
{
	public class RunCommand: ICommand
	{
		public void Run ()
		{
			CompilerManager.Run (TextEditorApp.OpenFileName);
		}
	}
}
