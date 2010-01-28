
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;


namespace MonoDevelop.Deployment.Linux
{
	public class DotDesktopDisplayBinding: DisplayBinding
	{
		public override bool CanCreateContentForUri (string fileName)
		{
			return fileName.EndsWith (".desktop");
		}
		
		public override bool CanCreateContentForMimeType (string mimetype)
		{
			return mimetype == "application/x-desktop";
		}

		public override IViewContent CreateContentForUri (string fileName)
		{
			return new DotDesktopView ();
		}
		
		public override IViewContent CreateContentForMimeType (string mimeType, Stream content)
		{
			return new DotDesktopView ();
		}

		public override string Name {
			get { return "Desktop Entry"; }
		}
	}
}
