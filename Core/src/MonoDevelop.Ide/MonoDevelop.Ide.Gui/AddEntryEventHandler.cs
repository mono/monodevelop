
using System;

namespace MonoDevelop.Ide.Gui
{
	public delegate void AddEntryEventHandler (object s, AddEntryEventArgs args);
	
	public class AddEntryEventArgs
	{
		string fileName;
		bool cancel;
		
		public AddEntryEventArgs (string fileName)
		{
			this.fileName = fileName;
		}
		
		public string FileName {
			get { return fileName; }
			set { fileName = value; }
		}
		
		public bool Cancel {
			get { return cancel; }
			set { cancel = value; }
		}
	}
}
