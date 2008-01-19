//
// SlnFileFormat.cs
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Prj2Make
{
	internal class SlnFileFormat
	{
		static string folderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

		static SlnFileFormat ()
		{
			IdeApp.Initialized += delegate (object sender, EventArgs args) {
				IdeApp.ProjectOperations.AddingEntryToCombine += new AddEntryEventHandler (HandleAddEntry);
			};
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			return Path.ChangeExtension (fileName, ".sln");
		}
		
		public bool CanReadFile (string file)
		{
			if (String.Compare (Path.GetExtension (file), ".sln", true) != 0)
				return false;
			string version = GetSlnFileVersion (file);
			return version == "9.00" || version == "10.00";
		}
		
		public bool CanWriteFile (object obj)
		{
			return obj is Combine;
		}
		
		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
		{
			Combine c = obj as Combine;
			if (c != null && !c.IsRoot && c.ParentCombine.FileFormat is MSBuildFileFormat)
				// Solution folder
				return new System.Collections.Specialized.StringCollection ();

			return null;
		}
		
		public void WriteFile (string file, object obj, IProgressMonitor monitor)
		{
			Combine c = obj as Combine;
			if (c == null)
				return;

			if (!c.IsRoot && c.ParentCombine.FileFormat is MSBuildFileFormat)
				// Ignore a non-root combine if its parent is a msbuild solution
				// Eg. if parent is a mds, then this should get emitted as the
				// top level solution
				return;

			string tmpfilename = String.Empty;
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", file), 1);
				try {
					if (File.Exists (file))
						tmpfilename = Path.GetTempFileName ();
				} catch (IOException) {
				}

				if (tmpfilename == String.Empty) {
					WriteFileInternal (file, c, monitor);
				} else {
					WriteFileInternal (tmpfilename, c, monitor);
					File.Delete (file);
					File.Move (tmpfilename, file);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save solution: {0}", file), ex);
				LoggingService.LogError (GettextCatalog.GetString ("Could not save solution: {0}", file), ex);

				if (!String.IsNullOrEmpty (tmpfilename))
					File.Delete (tmpfilename);
				throw;
			} finally {
				monitor.EndTask ();
			}
		}

		void WriteFileInternal (string file, Combine c, IProgressMonitor monitor)
		{
			using (StreamWriter sw = new StreamWriter (file, false, Encoding.UTF8)) {
				sw.NewLine = "\r\n";

				sw.WriteLine ();
				//Write Header
				sw.WriteLine ("Microsoft Visual Studio Solution File, Format Version 9.00");

				sw.WriteLine ("# Visual Studio 2005");

				//Write the projects
				monitor.BeginTask (GettextCatalog.GetString ("Saving projects"), 1);
				WriteProjects (c, c.BaseDirectory, sw, monitor);
				monitor.EndTask ();

				SlnData slnData = GetSlnData (c);
				if (slnData == null) {
					// If a non-msbuild project is being converted by just
					// changing the fileformat, then create the SlnData for it
					slnData = new SlnData ();
					c.ExtendedProperties [typeof (SlnFileFormat)] = slnData;
				} else {
					//Write the lines for unknownProjects
					foreach (string l in slnData.UnknownProjects)
						sw.WriteLine (l);
				}

				//Write the Globals
				sw.WriteLine ("Global");

				//Write SolutionConfigurationPlatforms
				//FIXME: SolutionConfigurations?
				sw.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

				foreach (CombineConfiguration config in c.Configurations)
					sw.WriteLine ("\t\t{0} = {0}", config.Name);

				sw.WriteLine ("\tEndGlobalSection");

				//Write ProjectConfigurationPlatforms
				sw.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

				List<string> list = new List<string> ();
				WriteProjectConfigurations (c, list, 0, null);

				list.Sort (StringComparer.Create (CultureInfo.InvariantCulture, true));
				foreach (string s in list)
					sw.WriteLine (s);

				//Write lines for projects we couldn't load
				if (slnData.SectionExtras.ContainsKey ("ProjectConfigurationPlatforms")) {
					foreach (string s in slnData.SectionExtras ["ProjectConfigurationPlatforms"])
						sw.WriteLine ("\t\t{0}", s);
				}

				sw.WriteLine ("\tEndGlobalSection");

				//Write Nested Projects
				sw.WriteLine ("\tGlobalSection(NestedProjects) = preSolution");
				WriteNestedProjects (c, c, sw);
				sw.WriteLine ("\tEndGlobalSection");

				//Write 'others'
				if (slnData.GlobalExtra != null) {
					foreach (string s in slnData.GlobalExtra)
						sw.WriteLine (s);
				}
				
				sw.WriteLine ("EndGlobal");
			}
		}

		void WriteProjects (Combine combine, string baseDirectory, StreamWriter writer, IProgressMonitor monitor)
		{
			monitor.BeginStepTask (GettextCatalog.GetString ("Saving projects"), combine.Entries.Count, 1); 
			foreach (CombineEntry ce in combine.Entries) {
				Combine c = ce as Combine;

				List<string> l = null;
				if (c == null) {
					//Project
					DotNetProject project = ce as DotNetProject;
					if (project == null) {
						monitor.ReportWarning (GettextCatalog.GetString (
							"Error saving project ({0}) : Only DotNetProjects can be part of a MSBuild solution. Ignoring.", ce.Name));
						monitor.Step (1);
						continue;
					}

					if (!MSBuildFileFormat.LanguageTypeGuids.ContainsKey (project.LanguageName)) {
						// FIXME: Should not happen, temp
						monitor.ReportWarning (GettextCatalog.GetString ("Saving for project {0} not supported. Ignoring.",
							ce.FileName));
						monitor.Step (1);
						continue;
					}

					IFileFormat ff = project.FileFormat;
					if (! (ff is MSBuildFileFormat)) {
						// Convert to a msbuild file format
						project.FileFormat = new MSBuildFileFormat ();
						project.FileName = project.FileFormat.GetValidFormatName (project, project.FileName);
					}

					project.Save (monitor);

					MSBuildData msbData = Utils.GetMSBuildData (project);
					if (msbData == null)
						//This should not happen as project.Save would've added this
						throw new Exception (String.Format (
							"INTERNAL ERROR: Project named '{0}', filename = {1}, does not have a 'data' object.", 
							project.Name, project.FileName));

					l = msbData.Extra;

					writer.WriteLine (@"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""",
						MSBuildFileFormat.LanguageTypeGuids [project.LanguageName],
						project.Name, 
						FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (
							baseDirectory, project.FileName)).Replace ('/', '\\'),
						msbData.Guid);
				} else {
					//Solution
					SlnData slnData = GetSlnData (c);
					if (slnData == null) {
						// Solution folder
						slnData = new SlnData ();
						c.ExtendedProperties [typeof (SlnFileFormat)] = slnData;
					}

					l = slnData.Extra;
					
					writer.WriteLine (@"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""",
						folderTypeGuid,
						ce.Name, 
						ce.Name,
						slnData.Guid);
				}

				if (l != null) {
					foreach (string s in l)
						writer.WriteLine (s);
				}

				writer.WriteLine ("EndProject");
				if (c != null)
					WriteProjects (c, baseDirectory, writer, monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}

		void WriteProjectConfigurations (Combine c, List<string> list, int ind, string config)
		{
			foreach (CombineConfiguration cc in c.Configurations) {
				string rootConfigName = config ?? cc.Name;
				if (cc.Name != rootConfigName)
					continue;

				foreach (CombineConfigurationEntry cce in cc.Entries) {
					DotNetProject p = cce.Entry as DotNetProject;
					if (p == null) {
						Combine combine = cce.Entry as Combine;
						if (combine == null)
							continue;

						//FIXME: Bug in md :/ Workaround, setting the config name explicitly
						//Solution folder's cce.ConfigurationName doesn't get set
						if (String.IsNullOrEmpty (cce.ConfigurationName)) {
							if (combine.GetConfiguration (rootConfigName) != null)
								cce.ConfigurationName = rootConfigName;
						}

						if (cce.ConfigurationName != rootConfigName) {
							//Sln folder's config must match the root,
							//so that its the same throughout the tree
							//this ensures that _all_ the projects are
							//relative to rootconfigname
							//FIXME: Could be either:
							//	1. Invalid setting
							//	2. New imported project, which doesn't yet have
							//	   a config named rootConfigName
							LoggingService.LogDebug ("Known Problem: Invalid setting. Ignoring.");
							continue;
						}

						WriteProjectConfigurations (combine, list, ind + 1, cc.Name);

						continue;
					}

					/* Project */

					MSBuildData data = Utils.GetMSBuildData (p);
					list.Add (String.Format (
						"\t\t{0}.{1}.ActiveCfg = {2}", data.Guid, cc.Name, cce.ConfigurationName));

					if (cce.Build)
						list.Add (String.Format (
							"\t\t{0}.{1}.Build.0 = {2}", data.Guid, cc.Name, cce.ConfigurationName));
				}
			}
		}

		void WriteNestedProjects (Combine combine, Combine root, StreamWriter writer)
		{
			foreach (CombineEntry ce in combine.Entries) {
				Combine c = ce as Combine;
				if (c == null || c.ParentCombine == null)
					continue;

				WriteNestedProjects (c, root, writer);
			}

			SlnData data = GetSlnData (combine);
			if (data == null)
				throw new Exception (String.Format (
					"INTERNAL ERROR: Solution named '{0}', filename = {1}, does not have a 'data' object.", 
					combine.Name, combine.FileName));

			string containerGuid = data.Guid;
			foreach (CombineEntry ce in combine.Entries) {
				if (ce.ParentCombine == root)
					continue;

				string containeeGuid = null;
				if (ce is Combine) {
					SlnData slnData = GetSlnData (ce);
					containeeGuid = slnData.Guid;
				} else {
					MSBuildData msbData = Utils.GetMSBuildData (ce);
					containeeGuid = msbData.Guid;
				}

				writer.WriteLine (@"{0}{1} = {2}", "\t\t", containeeGuid, containerGuid);
			}
		}

		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			Combine combine = null;
			if (fileName == null || monitor == null)
				return null;

			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution: {0}"), fileName), 1);
				combine = LoadSolution (fileName, monitor);
				SetHandlers (combine, true);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load solution: {0}", fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
			}

			return combine;
		}

		//ExtendedProperties
		//	Per config
		//		Platform : Eg. Any CPU
		//		SolutionConfigurationPlatforms
		//
		Combine LoadSolution (string fileName, IProgressMonitor monitor)
		{
			string version = GetSlnFileVersion (fileName);
			if (version != "9.00" && version != "10.00")
				throw new UnknownProjectVersionException (fileName, version);

			ListDictionary globals = null;
			Combine combine = null;
			SlnData data = null;
			List<Section> projectSections = null;
			List<string> lines = null;

			monitor.BeginTask (GettextCatalog.GetString ("Loading solution: {0}", fileName), 1);
			//Parse the .sln file
			using (StreamReader reader = new StreamReader(fileName)) {
				combine = new Combine ();
				combine.Name = Path.GetFileNameWithoutExtension (fileName);
				combine.FileName = fileName;
				combine.Version = "0.1"; //FIXME:
				combine.FileFormat = new MSBuildFileFormat ();
				data = new SlnData ();
				combine.ExtendedProperties [typeof (SlnFileFormat)] = data;

				string s = null;
				projectSections = new List<Section> ();
				lines = new List<string> ();
				globals = new ListDictionary ();
				//Parse
				while (reader.Peek () >= 0) {
					s = GetNextLine (reader, lines).Trim ();

					if (String.Compare (s, "Global", true) == 0) {
						ParseGlobal (reader, lines, globals);
						continue;
					}

					if (s.StartsWith ("Project")) {
						Section sec = new Section ();
						projectSections.Add (sec);

						sec.Start = lines.Count - 1;

						int e = ReadUntil ("EndProject", reader, lines);
						sec.Count = (e < 0) ? 1 : (e - sec.Start + 1);

						continue;
					}
				}
			}

			monitor.BeginTask("Loading projects ..", projectSections.Count + 1);
			Dictionary<string, CombineEntry> entries = new Dictionary<string, CombineEntry> ();
			foreach (Section sec in projectSections) {
				monitor.Step (1);
				Match match = ProjectRegex.Match (lines [sec.Start]);
				if (!match.Success) {
					LoggingService.LogDebug (GettextCatalog.GetString (
						"Invalid Project definition on line number #{0} in file '{1}'. Ignoring.",
						sec.Start + 1,
						fileName));

					continue;
				}

				try {
					// Valid guid?
					new Guid (match.Groups [1].Value);
				} catch (FormatException) {
					//Use default guid as projectGuid
					LoggingService.LogDebug (GettextCatalog.GetString (
						"Invalid Project type guid '{0}' on line #{1}. Ignoring.",
						match.Groups [1].Value,
						sec.Start + 1));
					continue;
				}

				string projTypeGuid = match.Groups [1].Value.ToUpper ();
				string projectName = match.Groups [2].Value;
				string projectPath = match.Groups [3].Value;
				string projectGuid = match.Groups [4].Value;

				if (projTypeGuid == folderTypeGuid) {
					//Solution folder
					SolutionFolder folder = new SolutionFolder ();
					folder.Name = projectName;
					folder.FileName = projectPath;
					folder.Version = "0.1"; //FIXME:
					folder.FileFormat = new MSBuildFileFormat ();

					SlnData slnData = new SlnData (projectGuid);
					folder.ExtendedProperties [typeof (SlnFileFormat)] = slnData;

					slnData.Extra = lines.GetRange (sec.Start + 1, sec.Count - 2);

					entries [projectGuid] = folder;
					
					continue;
				}

				if (!MSBuildFileFormat.LanguageTypeGuids.ContainsValue (projTypeGuid)) {
					LoggingService.LogWarning (GettextCatalog.GetString (
						"Unknown project type guid '{0}' on line #{1}. Ignoring.",
						projTypeGuid,
						sec.Start + 1));
					monitor.ReportWarning (GettextCatalog.GetString (
						"{0}({1}): Unsupported or unrecognized project : '{2}'. See logs.", 
						fileName, sec.Start + 1, projectPath));
					continue;
				}

				if (projectPath.StartsWith("http://")) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"{0}({1}): Projects with non-local source (http://...) not supported. '{2}'.",
						fileName, sec.Start + 1, projectPath));
					data.UnknownProjects.AddRange (lines.GetRange (sec.Start, sec.Count));
					continue;
				}

				DotNetProject project = null;
				string path = SlnMaker.MapPath (Path.GetDirectoryName (fileName), projectPath);
				if (String.IsNullOrEmpty (path)) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", fileName, projectPath));
					LoggingService.LogWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", fileName, projectPath));

					continue;
				}

				projectPath = Path.GetFullPath (path);
				try {
					project = Services.ProjectService.ReadCombineEntry (projectPath, monitor) as DotNetProject;
					if (project == null) {
						LoggingService.LogError ("Internal Error: Didn't get the expected DotNetProject for {0} project.",
							projectPath);
						continue;
					}

					MSBuildData msdata = Utils.GetMSBuildData (project);
					entries [projectGuid] = project;
					data.ProjectsByGuid [msdata.Guid] = project;

					msdata.Extra = lines.GetRange (sec.Start + 1, sec.Count - 2);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString (
								"Error while trying to load the project {0}. Exception : {1}",
								projectPath, e.ToString ()));
					monitor.ReportWarning (GettextCatalog.GetString (
						"Error while trying to load the project {0}. Exception : {1}", projectPath, e.Message));

					if (project == null)
						data.UnknownProjects.AddRange (lines.GetRange (sec.Start, sec.Count));
				}
			}
			monitor.EndTask ();

			if (globals != null && globals.Contains ("NestedProjects")) {
				LoadNestedProjects (globals ["NestedProjects"] as Section, lines, entries, monitor);
				globals.Remove ("NestedProjects");
			}

			//Add top level folders and projects to the main combine
			foreach (CombineEntry ce in entries.Values) {
				if (ce.ParentCombine == null)
					combine.Entries.Add (ce);
			}

			//Resolve project references
			List<ProjectReference> toRemove = new List<ProjectReference> ();
			List<ProjectReference> toAdd = new List<ProjectReference> ();
			foreach (Project p in combine.GetAllProjects ()) {
				toRemove.Clear ();
				toAdd.Clear ();
				MSBuildData msbuildData = Utils.GetMSBuildData (p);
				if (msbuildData == null)
					continue;

				foreach (ProjectReference pref in p.ProjectReferences) {
					if (pref.ReferenceType != ReferenceType.Project)
						continue;

					Project rp = combine.FindProject (pref.Reference);
					if (rp != null)
						continue;

					//Unresolved
					XmlElement elem = msbuildData.ProjectReferenceElements [pref];
					if (elem == null)
						//Should not happen
						continue;

					if (elem ["Project"] == null)
						continue;

					string guid = elem ["Project"].InnerText.Trim ();
					if (!data.ProjectsByGuid.ContainsKey (guid))
						continue;

					toRemove.Add (pref);
					rp = data.ProjectsByGuid [guid];
					ProjectReference newRef = new ProjectReference (ReferenceType.Project, rp.Name);
					toAdd.Add (newRef);

					XmlElement clonedElement = (XmlElement) elem.Clone ();
					elem.ParentNode.InsertAfter (clonedElement, elem);
					msbuildData.ProjectReferenceElements [newRef] = clonedElement;
				}

				foreach (ProjectReference pref in toRemove) {
					p.ProjectReferences.Remove (pref);
					msbuildData.ProjectReferenceElements.Remove (pref);
				}

				foreach (ProjectReference pref in toAdd)
					p.ProjectReferences.Add (pref);
			}

			//FIXME: This can be just SolutionConfiguration also!
			if (globals != null) {
				if (globals.Contains ("SolutionConfigurationPlatforms")) {
					LoadSolutionConfigurations (globals ["SolutionConfigurationPlatforms"] as Section, lines,
						combine, monitor);
					globals.Remove ("SolutionConfigurationPlatforms");
				}

				if (globals.Contains ("ProjectConfigurationPlatforms")) {
					LoadProjectConfigurationMappings (globals ["ProjectConfigurationPlatforms"] as Section, lines,
						combine, monitor);
					globals.Remove ("ProjectConfigurationPlatforms");
				}
			}

			//Save the global sections that we dont use
			List<string> globalLines = new List<string> ();
			foreach (Section sec in globals.Values)
				globalLines.InsertRange (globalLines.Count, lines.GetRange (sec.Start, sec.Count));

			data.GlobalExtra = globalLines;
			monitor.EndTask ();
			return combine;
		}

		void ParseGlobal (StreamReader reader, List<string> lines, ListDictionary dict)
		{
			//Process GlobalSection-s
			while (reader.Peek () >= 0) {
				string s = GetNextLine (reader, lines).Trim ();
				if (s.Length == 0)
					//Skip blank lines
					continue;

				Match m = GlobalSectionRegex.Match (s);
				if (!m.Success) {
					if (String.Compare (s, "EndGlobal", true) == 0)
						return;

					continue;
				}

				Section sec = new Section (m.Groups [1].Value, m.Groups [2].Value, lines.Count - 1, 1);
				dict [sec.Key] = sec;

				sec.Count = ReadUntil ("EndGlobalSection", reader, lines) - sec.Start + 1;
				//FIXME: sec.Count == -1 : No EndGlobalSection found, ignore entry?
			}
		}

		void LoadProjectConfigurationMappings (Section sec, List<string> lines, Combine sln, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "postSolution", true) != 0)
				return;

			List<CombineConfigurationEntry> noBuildList = new List<CombineConfigurationEntry> ();
			Dictionary<string, CombineConfigurationEntry> cache = new Dictionary<string, CombineConfigurationEntry> ();
			Dictionary<string, string> ignoredProjects = new Dictionary<string, string> ();
			SlnData slnData = GetSlnData (sln);
			
			List<string> extras = new List<string> ();

			for (int i = 0; i < sec.Count - 2; i ++) {
				int lineNum = i + sec.Start + 1;
				string s = lines [lineNum].Trim ();
				extras.Add (s);
				
				//Format:
				// {projectGuid}.SolutionConfigName|SolutionPlatform.ActiveCfg = ProjConfigName|ProjPlatform
				// {projectGuid}.SolutionConfigName|SolutionPlatform.Build.0 = ProjConfigName|ProjPlatform

				string [] parts = s.Split (new char [] {'='}, 2);
				if (parts.Length < 2) {
					LoggingService.LogDebug ("{0} ({1}) : Invalid format. Ignoring", sln.FileName, lineNum + 1);
					continue;
				}

				string action;
				string projConfig = parts [1].Trim ();

				string left = parts [0].Trim ();
				if (left.EndsWith (".ActiveCfg")) {
					action = "ActiveCfg";
					left = left.Substring (0, left.Length - 10);
				} else if (left.EndsWith (".Build.0")) {
					action = "Build.0";
					left = left.Substring (0, left.Length - 8);
				} else { 
					LoggingService.LogWarning (GettextCatalog.GetString ("{0} ({1}) : Unknown action. Only ActiveCfg & Build.0 supported.",
						sln.FileName, lineNum + 1));
					continue;
				}

				string [] t = left.Split (new char [] {'.'}, 2);
				if (t.Length < 2) {
					LoggingService.LogDebug ("{0} ({1}) : Invalid format of the left side. Ignoring",
						sln.FileName, lineNum + 1);
					continue;
				}

				string projGuid = t [0];
				string slnConfig = t [1];

				if (!slnData.ProjectsByGuid.ContainsKey (projGuid)) {
					if (ignoredProjects.ContainsKey (projGuid))
						// already warned
						continue;

					LoggingService.LogWarning (GettextCatalog.GetString ("{0} ({1}) : Project with guid = '{2}' not found or not loaded. Ignoring", 
						sln.FileName, lineNum + 1, projGuid));
					ignoredProjects [projGuid] = projGuid;
					continue;
				}

				DotNetProject project = slnData.ProjectsByGuid [projGuid];

				string key = projGuid + "." + slnConfig;
				CombineConfigurationEntry combineConfigEntry = null;
				if (cache.ContainsKey (key)) {
					combineConfigEntry = cache [key];
				} else {
					combineConfigEntry = GetConfigEntryForProject (sln, slnConfig, project);
					cache [key] = combineConfigEntry;
				}

				/* If both ActiveCfg & Build.0 entries are missing
				 * for a project, then default values :
				 *	ActiveCfg : same as the solution config
				 *	Build : true
				 *
				 * ELSE
				 * if Build (true/false) for the project will 
				 * will depend on presence/absence of Build.0 entry
				 */
				if (String.Compare (action, "ActiveCfg", false) == 0) {
					combineConfigEntry.ConfigurationName = projConfig;
					noBuildList.Add (combineConfigEntry);
				} else if (String.Compare (action, "Build.0", false) == 0) {
					noBuildList.Remove (combineConfigEntry);
				}

				extras.RemoveAt (extras.Count - 1);
			}

			slnData.SectionExtras ["ProjectConfigurationPlatforms"] = extras;

			foreach (CombineConfigurationEntry e in noBuildList) {
				//Mark (build=false) of all projects for which 
				//ActiveCfg was found but no Build.0
				e.Build = false;
			}
		}

		/* Finds a CombineConfigurationEntry corresponding to the @configName for a project (@projectName) 
		 * in @combine */
		CombineConfigurationEntry GetConfigEntryForProject (Combine combine, string configName, Project project)
		{
			if (project.ParentCombine == null)
				throw new Exception (String.Format (
					"INTERNAL ERROR: project {0} is not part of any combine", project.Name));

			CombineConfigurationEntry ret = GetConfigEntry (project, configName);

			//Ensure the corresponding entries exist all the way
			//upto the RootCombine
			Combine parent = project.ParentCombine;
			while (!parent.IsRoot && parent.ParentCombine != null) {
				CombineConfigurationEntry p = GetConfigEntry (parent, configName);
				if (p.ConfigurationName != configName)
					p.ConfigurationName = configName;
				parent = parent.ParentCombine;
			}

			return ret;
		}

		/* Gets the CombineConfigurationEntry corresponding to the @entry in its parentCombine's 
		 * CombineConfiguration. Creates the required bits if not present */
		CombineConfigurationEntry GetConfigEntry (CombineEntry entry, string configName)
		{
			Combine parent = entry.ParentCombine;
			CombineConfiguration combineConfig = parent.GetConfiguration (configName) as CombineConfiguration;
			if (combineConfig == null) {
				combineConfig = (CombineConfiguration) parent.CreateConfiguration (configName);
				parent.Configurations.Add (combineConfig);
			}

			foreach (CombineConfigurationEntry cce in combineConfig.Entries) {
				if (cce.Entry == entry)
					return cce;
			}

			return combineConfig.AddEntry (entry);
		}

		void LoadSolutionConfigurations (Section sec, List<string> lines, Combine combine, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "preSolution", true) != 0)
				return;

			for (int i = 0; i < sec.Count - 2; i ++) {
				//FIXME: expects both key and val to be on the same line
				int lineNum = i + sec.Start + 1;
				string s = lines [lineNum].Trim ();
				if (s.Length == 0)
					//Skip blank lines
					continue;

				KeyValuePair<string, string> pair = SplitKeyValue (s);

				if (pair.Key.IndexOf ('|') < 0) {
					//Config must of the form ConfigName|Platform
					LoggingService.LogError (GettextCatalog.GetString ("{0} ({1}) : Invalid config name '{2}'", combine.FileName, lineNum + 1, pair.Key));
					continue;
				}
				
				CombineConfiguration config = (CombineConfiguration) 
					combine.GetConfiguration (pair.Key);
				
				if (config == null) {
					config = (CombineConfiguration) 
						combine.CreateConfiguration (pair.Key);
					combine.Configurations.Add (config);
				}
			}
		}

		void LoadNestedProjects (Section sec, List<string> lines,
			Dictionary<string, CombineEntry> entries, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "preSolution", true) != 0)
				return;

			for (int i = 0; i < sec.Count - 2; i ++) {
				KeyValuePair<string, string> pair = SplitKeyValue (lines [i + sec.Start + 1].Trim ());

				if (!entries.ContainsKey (pair.Value)) {
					//Container not found
					LoggingService.LogWarning (GettextCatalog.GetString ("Project with guid '{0}' not found.", pair.Value));
					continue;
				}

				if (!entries.ContainsKey (pair.Key)) {
					//Containee not found
					LoggingService.LogWarning (GettextCatalog.GetString ("Project with guid '{0}' not found.", pair.Key));
					continue;
				}

				Combine folder = entries [pair.Value] as Combine;
				if (folder == null)
					continue;

				folder.Entries.Add (entries [pair.Key]);
			}
		}

		string GetNextLine (StreamReader reader, List<string> list)
		{
			if (reader.Peek () < 0)
				return null;

			string ret = reader.ReadLine ();
			list.Add (ret);
			return ret;
		}

		int ReadUntil (string end, StreamReader reader, List<string> lines)
		{
			int ret = -1;
			while (reader.Peek () >= 0) {
				string s = GetNextLine (reader, lines);

				if (String.Compare (s.Trim (), end, true) == 0)
					return (lines.Count - 1);
			}

			return ret;
		}


		KeyValuePair<string, string> SplitKeyValue (string s)
		{
			string [] pair = s.Split (new char [] {'='}, 2);
			string key = pair [0].Trim ();
			string val = String.Empty;
			if (pair.Length == 2)
				val = pair [1].Trim ();

			return new KeyValuePair<string, string> (key, val);
		}

		// Utility function to determine the sln file version
		string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			StreamReader reader = new StreamReader(strInSlnFile);
			
			strInput = reader.ReadLine();
			if (strInput == null)
				return null;

			match = SlnVersionRegex.Match(strInput);
			if (!match.Success) {
				strInput = reader.ReadLine();
				if (strInput == null)
					return null;
				match = SlnVersionRegex.Match (strInput);
			}

			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}

		static SlnData GetSlnData (CombineEntry entry)
		{
			if (entry.ExtendedProperties.Contains (typeof (SlnFileFormat)))
				return entry.ExtendedProperties [typeof (SlnFileFormat)] as SlnData;
			return null;
		}

		// Event handlers
		public static void HandleAddEntry (object s, AddEntryEventArgs args)
		{
			if (GetSlnData (args.Combine) == null)
				return;

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

		static void HandleCombineEntryAdded (object sender, CombineEntryEventArgs e)
		{
			try {
				// ReadFile for Sln/MSBuildFileFormat set the handlers
				ConvertToMSBuild (e.CombineEntry);

				Combine rootSln = e.CombineEntry.RootCombine;
				SlnData rootSlnData = GetSlnData (rootSln);
				SlnData slnData = GetSlnData (e.CombineEntry);
				if (slnData != null) {
					foreach (KeyValuePair<string, DotNetProject> pair in slnData.ProjectsByGuid)
						rootSlnData.ProjectsByGuid [pair.Key] = pair.Value;
				} else {
					//Add guid for the new project
					MSBuildData msdata = Utils.GetMSBuildData (e.CombineEntry);
					DotNetProject project = e.CombineEntry as DotNetProject;
					if (project != null && msdata != null)
						//msbuild project
						rootSlnData.ProjectsByGuid [msdata.Guid] = project;
				}

				//FIXME: Can't call this now as we don't extend Combine :/
				//rootSln.NotifyModified ();
				//Need some other solution, for now, hack -
				rootSln.FileName = rootSln.FileName;
			} catch (Exception ex) {
				LoggingService.LogDebug ("HandleCombineEntryAdded : {0}", ex);
			}
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

		// static regexes
		static Regex projectRegex = null;
		internal static Regex ProjectRegex {
			get {
				if (projectRegex == null)
					projectRegex = new Regex(@"Project\(""(\{[^}]*\})""\) = ""(.*)"", ""(.*)"", ""(\{[^{]*\})""");
				return projectRegex;
			}
		}

		static Regex globalSectionRegex = null;
		static Regex GlobalSectionRegex {
			get {
				if (globalSectionRegex == null)
					globalSectionRegex = new Regex (@"GlobalSection\s*\(([^)]*)\)\s*=\s*(\w*)"); 
				return globalSectionRegex;
			}
		}

		static Regex slnVersionRegex = null;
		internal static Regex SlnVersionRegex {
			get {
				if (slnVersionRegex == null)
					slnVersionRegex = new Regex (@"Microsoft Visual Studio Solution File, Format Version (\d?\d.\d\d)");
				return slnVersionRegex;
			}
		}

	}

	class Section {
		public string Key;
		public string Val;

		public int Start = -1; //Line number
		public int Count = 0;

		public Section ()
		{
		}

		public Section (string Key, string Val, int Start, int Count)
		{
			this.Key = Key;
			this.Val = Val;
			this.Start = Start;
			this.Count = Count;
		}
	}

	class SolutionFolder : Combine {
		public override bool NeedsReload {
			get { return false; }
		}
	}

}
