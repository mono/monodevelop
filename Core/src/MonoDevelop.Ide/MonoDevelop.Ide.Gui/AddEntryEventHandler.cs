
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	public delegate void AddEntryEventHandler (object s, AddEntryEventArgs args);
	
	public class AddEntryEventArgs
	{
		string fileName;
		bool cancel;
		Combine combine;
		
		public AddEntryEventArgs (Combine combine, string fileName)
		{
			this.combine = combine;
			this.fileName = fileName;
		}
		
		public Combine Combine {
			get { return combine; }
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
