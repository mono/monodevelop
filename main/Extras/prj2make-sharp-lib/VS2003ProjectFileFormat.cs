//
// VS2003ProjectFileFormat.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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

		public string GetValidFormatName (object obj, string fileName)
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
					xr.MoveToContent ();
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
			return false;
		}

		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
		}

		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
		{
			return null;
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
			SlnFileFormat.ConvertToMSBuild (project);
			return project as DotNetProject;
		}
	}
}
