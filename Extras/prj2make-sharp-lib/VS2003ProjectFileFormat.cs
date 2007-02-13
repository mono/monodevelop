using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace MonoDevelop.Prj2Make
{
	public class VS2003ProjectFileFormat : IFileFormat
	{
		public VS2003ProjectFileFormat ()
		{
		}

		public string Name {
			get { return "VS2003 project"; }
		}

		public string GetValidFormatName (string fileName)
		{
			return fileName;
		}

		public bool CanReadFile (string file)
		{
			if (String.Compare (Path.GetExtension (file), ".csproj", true) != 0 &&
				String.Compare (Path.GetExtension (file), ".vbproj", true) != 0)
				return false;

			//FIXME: Need a better way to check the rootelement
			try {
				using (XmlReader xr = XmlReader.Create (file)) {
					xr.Read ();
					if (xr.NodeType == XmlNodeType.Element && String.Compare (xr.LocalName, "VisualStudioProject") == 0)
						return true;
				}
			} catch {
				return false;
			}

			return false;
		}

		public bool CanWriteFile (object obj)
		{
			return obj is DotNetProject;
		}

		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
		}

		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			//if (!CanReadFile (fileName))

			int choice = IdeApp.Services.MessageService.ShowCustomDialog (GettextCatalog.GetString ("Conversion required"),
					GettextCatalog.GetString (
						"The project file {0} is a VS2003 project. It must be converted to either a MonoDevelop " + 
						"or a VS2005 project. Converting to VS2005 format will overwrite existing files. Convert ?",
						fileName), "MonoDevelop", "VS2005", "Cancel");

			if (choice == 2)
				throw new InvalidOperationException ("VS2003 projects are not supported natively.");

			DotNetProject project = null;
			if (choice == 0) {
				// Convert to MD project
				project = ImportCsproj (fileName, true);
			} else if (choice == 1) {
				// Convert to VS2005 project
				project = ImportCsprojAsMSBuild (fileName);
				// Re-read to get a MSBuildProject object
				project = IdeApp.Services.ProjectService.ReadCombineEntry (project.FileName, monitor) as DotNetProject;
			}

			return project;
		}
		
		internal static DotNetProject ImportCsproj (string fileName, bool save)
		{
			DotNetProject project = null;
			SlnMaker slnmaker = new SlnMaker ();
			try { 
				using (IProgressMonitor m = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (
							true, false, true, false)) {
					project = slnmaker.CreatePrjxFromCsproj (fileName, m, save);
				}
			} catch (Exception e) {
				Console.WriteLine ("exception while converting : " + e.ToString ());
				throw;
			}

			return project;
		}

		internal static DotNetProject ImportCsprojAsMSBuild (string fileName)
		{
			DotNetProject project = ImportCsproj (fileName, false);

			// ConvertToMSBuild saves the project for mdp
			return MSBuildSolution.ConvertToMSBuild (project, false) as DotNetProject;
		}
	}
}
