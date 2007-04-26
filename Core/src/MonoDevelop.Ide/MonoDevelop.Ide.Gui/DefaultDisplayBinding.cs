
using System;
using MonoDevelop.Ide.Codons;
using System.IO;

namespace MonoDevelop.Ide.Gui
{
	class DefaultDisplayBinding: IDisplayBinding
	{
		public bool CanCreateContentForFile (string fileName)
		{
			return false;
		}
		
		public bool CanCreateContentForMimeType (string mimetype)
		{
			return false;
		}

		public IViewContent CreateContentForFile (string fileName)
		{
			throw new NotSupportedException ();
		}
		
		public IViewContent CreateContentForMimeType (string mimeType, Stream content)
		{
			throw new NotSupportedException ();
		}

		public string DisplayName {
			get { return ""; }
		}

	}
}
