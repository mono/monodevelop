
using System;

namespace TextEditor
{
	public class SaveCommand: ICommand
	{
		public void Run ()
		{
			TextEditorApp.SaveFile ();
		}
	}
}
