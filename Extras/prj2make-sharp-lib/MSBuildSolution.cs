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
			FileFormat = new SlnFileFormat ();
		}

		static MSBuildSolution ()
		{
			IdeApp.ProjectOperations.AddingEntryToCombine += new AddEntryEventHandler (HandleAddEntry);
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

			//FIXME: Use IFileFormat.CanReadFile 
			if (String.Compare (extn, ".mdp", true) == 0 || String.Compare (extn, ".mds", true) == 0) {
				if (!IdeApp.Services.MessageService.AskQuestion (GettextCatalog.GetString (
					"The project file {0} must be converted to msbuild format to be added " + 
					"to a msbuild solution. Convert?", args.FileName), "Conversion required")) {
					args.Cancel = true;
					return;
				}
			}

			IProgressMonitor monitor = new NullProgressMonitor ();
			CombineEntry ce = Services.ProjectService.ReadCombineEntry (args.FileName, monitor);
			ce = ConvertToMSBuild (ce, false);
			args.FileName = ce.FileName;

			if (String.Compare (extn, ".mds", true) == 0)
				UpdateProjectReferences (((Combine) ce), true);

			ce.FileFormat.WriteFile (ce.FileName, ce, monitor);
		}

		static void HandleCombineEntryAdded (object sender, CombineEntryEventArgs e)
		{
			try {
				ConvertToMSBuild (e.CombineEntry, true);

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

			combine.EntryAdded += new CombineEntryEventHandler (HandleCombineEntryAdded);
		}

		static CombineEntry ConvertToMSBuild (CombineEntry ce, bool prompt)
		{
			Combine newCombine = ce as Combine;
			CombineEntry ret = ce;

			if (newCombine == null) {
				//FIXME: Use MSBuildFileFormat.CanReadFile instead
				if (String.Compare (Path.GetExtension (ce.FileName), ".mdp", true) == 0) {
					DotNetProject project = (DotNetProject) ce;
					MSBuildFileFormat fileFormat = new MSBuildFileFormat (project.LanguageName);
					project.FileFormat = fileFormat;

					string newname = fileFormat.GetValidFormatName (project.FileName);
					project.FileName = newname;
					fileFormat.SaveProject (project, new NullProgressMonitor ());
				}
			} else {
				SlnData slnData = (SlnData) newCombine.ExtendedProperties [typeof (SlnFileFormat)];
				if (slnData == null) {
					slnData = new SlnData ();
					newCombine.ExtendedProperties [typeof (SlnFileFormat)] = slnData;
				}

			 	slnData.Guid = Guid.NewGuid ().ToString ().ToUpper ();

				if (String.Compare (Path.GetExtension (newCombine.FileName), ".mds", true) == 0) {
					foreach (CombineEntry e in newCombine.Entries)
						ConvertToMSBuild (e, false);

					newCombine.FileFormat = new SlnFileFormat ();
					newCombine.FileName = newCombine.FileFormat.GetValidFormatName (newCombine.FileName);
					SetHandlers (newCombine, false);
				}

				//This is set to ensure that the solution folder's BaseDirectory
				//(which is derived from .FileName) matches that of the root
				//combine
				//newCombine.FileName = newCombine.RootCombine.FileName;
			}

			return ret;
		}

		static void UpdateProjectReferences (Combine c, bool saveProjects)
		{
			CombineEntryCollection allProjects = c.GetAllProjects ();

			foreach (Project proj in allProjects) {
				foreach (ProjectReference pref in proj.ProjectReferences) {
					if (pref.ReferenceType != ReferenceType.Project)
						continue;

					Project p = (Project) allProjects [pref.Reference];

					//FIXME: Move this to MSBuildFileFormat ?
					MSBuildData data = (MSBuildData) proj.ExtendedProperties [typeof (MSBuildFileFormat)];
					XmlElement elem = data.ProjectReferenceElements [pref];
					elem.SetAttribute ("Include", 
						Runtime.FileService.AbsoluteToRelativePath (
							proj.BaseDirectory, p.FileName));

					//Set guid of the ProjectReference
					MSBuildData prefData = (MSBuildData) p.ExtendedProperties [typeof (MSBuildFileFormat)];
					MSBuildFileFormat.EnsureChildValue (elem, "Project", MSBuildFileFormat.ns,
						String.Concat ("{", prefData.Guid, "}"));

				}
				if (saveProjects)
					proj.FileFormat.WriteFile (proj.FileName, proj, new NullProgressMonitor ());
			}
		}

	}
}
