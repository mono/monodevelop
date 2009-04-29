
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;


namespace MonoDevelop.Deployment.Linux
{
	public class DotDesktopDisplayBinding: IDisplayBinding
	{
		public bool CanCreateContentForFile (string fileName)
		{
			return fileName.EndsWith (".desktop");
		}
		
		public bool CanCreateContentForMimeType (string mimetype)
		{
			return mimetype == "application/x-desktop";
		}

		public IViewContent CreateContentForUri (string fileName)
		{
			return new DotDesktopView ();
		}
		
		public IViewContent CreateContentForMimeType (string mimeType, Stream content)
		{
			return new DotDesktopView ();
		}

		public string Name {
			get { return "Desktop Entry"; }
		}
	}
}
