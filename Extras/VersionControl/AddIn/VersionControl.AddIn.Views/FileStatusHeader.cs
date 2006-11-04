
using System;

namespace VersionControlAddIn
{
	
	
	public class FileStatusHeader : Gtk.Bin
	{
		
		public FileStatusHeader()
		{
			Stetic.Gui.Build(this, typeof(VersionControlAddIn.FileStatusHeader));
		}
	}
}
