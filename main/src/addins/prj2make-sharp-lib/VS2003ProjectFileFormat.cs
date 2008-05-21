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
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Prj2Make
{
	public class VS2003ProjectFileFormat : IFileFormat
	{
		public VS2003ProjectFileFormat ()
		{
		}

		public string Name {
			get { return "Visual Studio 2003"; }
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			return fileName;
		}

		public bool CanReadFile (string file, Type expectedObjectType)
		{
			if (expectedObjectType.IsAssignableFrom (typeof(Solution)) && String.Compare (Path.GetExtension (file), ".sln", true) == 0) {
				string ver = GetSlnFileVersion (file);
				if (ver == "7.00" || ver == "8.00")
					return true;
			}
			
			if (!expectedObjectType.IsAssignableFrom (typeof(DotNetProject)))
				return false;
			
			if (String.Compare (Path.GetExtension (file), ".csproj", true) != 0 &&
				String.Compare (Path.GetExtension (file), ".vbproj", true) != 0)
				return false;

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

		public object ReadFile (string file, Type expectedType, IProgressMonitor monitor)
		{
			if (expectedType.IsAssignableFrom (typeof(DotNetProject)))
				return ReadProjectFile (file, monitor);
			else
				return ReadSolutionFile (file, monitor);
		}

		public object ReadProjectFile (string fileName, IProgressMonitor monitor)
		{
			//if (!CanReadFile (fileName))
			
			AlertButton monodevelop = new AlertButton ("MonoDevelop");
			AlertButton vs2005      = new AlertButton ("VS2005");

			AlertButton choice = MessageService.AskQuestion (GettextCatalog.GetString ("The project file {0} is a VS2003 project. It must be converted to either a MonoDevelop or a VS2005 project.", fileName),
			                                                 GettextCatalog.GetString ("Converting to VS2005 format will overwrite existing files."),
			                                                 AlertButton.Cancel, vs2005, monodevelop);

			DotNetProject project = null;
			if (choice == monodevelop) {
				// Convert to MD project
				project = ImportCsproj (fileName, true);
			} else if (choice == vs2005) {
				// Convert to VS2005 project
				project = ImportCsprojAsMSBuild (fileName);
				// Re-read to get a MSBuildProject object
				project = IdeApp.Services.ProjectService.ReadSolutionItem (monitor, project.FileName) as DotNetProject;
			} else {
				throw new InvalidOperationException ("VS2003 projects are not supported natively.");
			}

			return project;
		}
		
		public object ReadSolutionFile (string fileName, IProgressMonitor monitor)
		{
			//if (!CanReadFile (fileName))

			AlertButton choice;
			AlertButton monodevelop = new AlertButton ("MonoDevelop");
			AlertButton vs2005      = new AlertButton ("VS2005");
			if (IdeApp.Services == null) {
				// HACK, for mdtool
				choice = null;
			} else {
				choice = MessageService.AskQuestion (GettextCatalog.GetString ("The solution file {0} is a VS2003 solution. It must be converted to either a MonoDevelop or a VS2005 solution", fileName),
				                                     GettextCatalog.GetString ("Converting to VS2005 format will overwrite existing files."),
				                                     AlertButton.Cancel, vs2005, monodevelop);
			}
			

			Solution solution = null;
			if (choice == monodevelop) {
				// Convert to MD solution
				solution = ImportSln (fileName, true);
			} else if (choice == vs2005) {
				// Convert to vs2005 solution
				solution = ImportSlnAsMSBuild (fileName);
				solution.Save (monitor);

				// Re-read to get a MSBuildSolution object
				solution = MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (monitor, solution.FileName) as Solution;
			} else {
				throw new InvalidOperationException ("VS2003 solutions are not supported natively.");
			}				

			return solution;
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
				LoggingService.LogError ("exception while converting : " + e.ToString ());
				throw;
			}

			return project;
		}

		internal static DotNetProject ImportCsprojAsMSBuild (string fileName)
		{
			DotNetProject project = ImportCsproj (fileName, false);

			// ConvertToMSBuild saves the project for mdp
//			SlnFileFormat.ConvertToMSBuild (project);
			return project as DotNetProject;
		}

		public void ConvertToFormat (object obj)
		{
			throw new NotSupportedException ();
		}

		public List<string> GetItemFiles (object obj)
		{
			return new List<string> ();
		}

		// Converts a vs2003 solution to a Combine object
		internal static Solution ImportSln (string fileName, bool save)
		{
			SlnMaker slnmaker = new SlnMaker ();
			Solution solution = null;
			IProgressMonitor m;
			if (IdeApp.Services == null)
				m = new ConsoleProgressMonitor ();
			else
				m = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (
							true, false, true, false);

			try { 
				solution = slnmaker.MsSlnToCmbxHelper (fileName, m, save);
			} catch (Exception e) {
				LoggingService.LogError ("exception while converting : " + e.ToString ());
				throw;
			} finally {
				if (m != null)
					m.Dispose ();
			}

			return solution;
		}

		// Does not save the final combine, useful when this converted
		// combine will be used as a solution folder (saves an extra .sln from being created)
		internal static Solution ImportSlnAsMSBuild (string fileName)
		{
			Solution solution = ImportSln (fileName, false);
//			SlnFileFormat.ConvertToMSBuild (solution);
			solution.Save (new NullProgressMonitor ());

			return solution;
		}
		
		// Utility function to determine the sln file version
		string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			StreamReader reader = new StreamReader(strInSlnFile);
			strInput = reader.ReadLine();

			match = SlnMaker.SlnVersionRegex.Match(strInput);
			if (!match.Success) {
				match = SlnMaker.SlnVersionRegex.Match (reader.ReadLine ());
			}

			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}
	}
}
