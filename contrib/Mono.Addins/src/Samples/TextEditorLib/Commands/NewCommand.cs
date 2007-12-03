
using System;

namespace TextEditor
{
	public class NewCommand: ICommand
	{
		public void Run ()
		{
			TextEditorApp.NewFile ("");
		}
	}
}
