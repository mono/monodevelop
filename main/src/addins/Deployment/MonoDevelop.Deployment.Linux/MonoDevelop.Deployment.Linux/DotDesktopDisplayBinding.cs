
using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Deployment.Linux
{
	public class DotDesktopDisplayBinding: IViewDisplayBinding
	{
		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			return (fileName.IsNotNull && fileName.HasExtension (".desktop"))
				|| (mimeType != null && mimeType == "application/x-desktop");
		}
		
		public IViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			return new DotDesktopView ();
		}

		public string Name {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("Desktop Entry"); }
		}

		public bool CanUseAsDefault {
			get { return true; }
		}
	}
}
