//
// MSBuildSolution.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MonoDevelop.Prj2Make
{
	class MSBuildSolution : Combine
	{
		Dictionary<string, DotNetProject> projectsByGuidTable;

		public MSBuildSolution () : base ()
		{
			FileFormat = new MSBuildFileFormat ();
		}

		static MSBuildSolution ()
		{
			if (IdeApp.ProjectOperations != null)
				// this is when invoked w/o the gui
				//FIXME: Replace with a better way to check this
				IdeApp.ProjectOperations.AddingEntryToCombine += new AddEntryEventHandler (HandleAddEntry);
		}

		public override bool NeedsReload {
			get {
				if (ParentCombine != null && ParentCombine is MSBuildSolution)
					// Solution folder
					return false;
				else
					return base.NeedsReload;
			}
			set { base.NeedsReload = value; }
		}

		public SlnData Data {
			get {
				if (!ExtendedProperties.Contains (typeof (SlnFileFormat)))
					return null;
				return (SlnData) ExtendedProperties [typeof (SlnFileFormat)];
			}
			set {
				ExtendedProperties [typeof (SlnFileFormat)] = value;
			}
		}

		public Dictionary<string, DotNetProject> ProjectsByGuid {
			get {
				if (projectsByGuidTable == null)
					projectsByGuidTable = new Dictionary<string, DotNetProject> ();
				return projectsByGuidTable;
			}
		}

		public static void HandleAddEntry (object s, AddEntryEventArgs args)
		{
			if (args.Combine.GetType () != typeof (MSBuildSolution))
				return;

			string extn = Path.GetExtension (args.FileName);

			IFileFormat msformat = new MSBuildFileFormat ();

			if (!msformat.CanReadFile (args.FileName)) {
				if (!IdeApp.Services.MessageService.AskQuestion (GettextCatalog.GetString (
					"The project file {0} must be converted to msbuild format to be added " + 
					"to a msbuild solution. Convert?", args.FileName), "Conversion required")) {
					args.Cancel = true;
					return;
				}
			} else {
				// vs2005 solution/project
				return;
			}

			IProgressMonitor monitor = new NullProgressMonitor ();
			IFileFormat slnff = new VS2003SlnFileFormat ();
			IFileFormat prjff = new VS2003ProjectFileFormat ();

			if (slnff.CanReadFile (args.FileName)) {
				// VS2003 solution
				Combine c = VS2003SlnFileFormat.ImportSlnAsMSBuild (args.FileName);
				c.Save (monitor);

				args.FileName = c.FileName;
			} else if (prjff.CanReadFile (args.FileName)) {
				// VS2003 project

				DotNetProject proj = VS2003ProjectFileFormat.ImportCsprojAsMSBuild (args.FileName);
				args.FileName = proj.FileName;
			} else {
				CombineEntry ce = Services.ProjectService.ReadCombineEntry (args.FileName, monitor);
				ConvertToMSBuild (ce);
				args.FileName = ce.FileName;
				ce.Save (monitor);
			}
		}

		static void HandleCombineEntryAdded (object sender, CombineEntryEventArgs e)
		{
			try {
				// ReadFile for Sln/MSBuildFileFormat set the handlers
				ConvertToMSBuild (e.CombineEntry);

				MSBuildSolution rootSln = (MSBuildSolution) e.CombineEntry.RootCombine;
				MSBuildSolution sln = e.CombineEntry as MSBuildSolution;
				if (sln != null) {
					foreach (KeyValuePair<string, DotNetProject> pair in sln.ProjectsByGuid)
						rootSln.ProjectsByGuid [pair.Key] = pair.Value;
				} else {
					//Add guid for the new project
					MSBuildProject project = e.CombineEntry as MSBuildProject;
					if (project != null)
						rootSln.ProjectsByGuid [project.Data.Guid] = project;
				}

				rootSln.NotifyModified ();
			} catch (Exception ex) {
				Runtime.LoggingService.DebugFormat ("{0}", ex.Message);
				Console.WriteLine ("HandleCombineEntryAdded : {0}", ex.ToString ());
			}
		}

		internal static void SetHandlers (Combine combine, bool setEntries)
		{
			if (setEntries) {
				foreach (CombineEntry ce in combine.Entries) {
					Combine c = ce as Combine;
					if (c == null)
						continue;
	 
					SetHandlers (c, setEntries);
				}
			}

			combine.EntryAdded += HandleCombineEntryAdded;
		}

		internal static void ConvertToMSBuild (CombineEntry ce)
		{
			MSBuildFileFormat msformat = new MSBuildFileFormat ();
			if (!msformat.CanReadFile (ce.FileName)) {
				// Convert
				ce.FileFormat = msformat;
				ce.FileName = msformat.GetValidFormatName (ce, ce.FileName);

				// Save will create the required SlnData, MSBuildData
				// objects, create the new guids for the projects _and_ the
				// solution folders
				ce.Save (new NullProgressMonitor ());
				// Writing out again might be required to fix
				// project references which have changed (filenames
				// changed due to the conversion)
			}
		}

	}
}
