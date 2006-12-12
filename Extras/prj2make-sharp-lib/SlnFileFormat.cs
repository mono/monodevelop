//
// SlnFileFormat.cs
//
// Author:
//   Ankit Jain
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
	internal class SlnFileFormat: IFileFormat
	{
		static Guid folderTypeGuid = new Guid ("2150E333-8FDC-42A3-9474-1A3956D46DE8");
		static Guid projectTypeGuid = new Guid ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

		public string Name {
			get { return "Visual Studio .NET 2005 Solution"; }
		}
		
		public string GetValidFormatName (string fileName)
		{
			return Path.ChangeExtension (fileName, ".sln");
		}
		
		public bool CanReadFile (string file)
		{
			return String.Compare (Path.GetExtension (file), ".sln", true) == 0;
		}
		
		public bool CanWriteFile (object obj)
		{
			return obj is Combine;
		}
		
		public void WriteFile (string file, object obj, IProgressMonitor monitor)
		{
			Combine c = obj as Combine;
			if (c == null)
				return;

			if (c.ParentCombine != null)
				//Not the top most solution
				return;

			if (!c.ExtendedProperties.Contains (typeof (SlnFileFormat)))
				throw new Exception ("Writing only supported for vs2005 projects (yet)");
		
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
				Console.WriteLine ("Could not save solution: {0}, {1}", file, ex);

				if (tmpfilename != String.Empty)
					File.Delete (tmpfilename);
				throw;
			} finally {
				monitor.EndTask ();
			}
		}

		void WriteFileInternal (string file, Combine c, IProgressMonitor monitor)
		{
			using (StreamWriter sw = new StreamWriter (file, false, Encoding.UTF8)) {
				//Write Header
				sw.WriteLine ("Microsoft Visual Studio Solution File, Format Version 9.00");

				//FIXME: Write md version
				sw.WriteLine ("#MonoDevelop");

				//Write the projects
				WriteProjects (c, c.RootCombine.BaseDirectory, sw);

				//Write the lines for unknownProjects
				SlnData slnData = (SlnData) c.ExtendedProperties [typeof (SlnFileFormat)];
				if (slnData != null) {
					foreach (string l in slnData.UnknownProjects)
						sw.WriteLine (l);
				}

				//Write the Globals
				sw.WriteLine ("Global");

				//Write SolutionConfigurationPlatforms
				//FIXME: SolutionConfigurations?
				sw.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

				foreach (CombineConfiguration config in c.Configurations) {
					string s = (string) config.ExtendedProperties ["SolutionConfigurationPlatforms"];
					if (String.IsNullOrEmpty (s))
						//FIXME: Platforms
						//FIXME: Add this new string to config.ExtendedProperties
						sw.WriteLine ("\t\t{0}|{1} = {0}|{1}", config.Name, "Any CPU");
					else
						sw.WriteLine ("\t\t{0}", s);
				}

				sw.WriteLine ("\tEndGlobalSection");

				//Write Nested Projects
				sw.WriteLine ("\tGlobalSection(NestedProjects) = preSolution");
				WriteNestedProjects (c, sw);
				sw.WriteLine ("\tEndGlobalSection");

				//FIXME: ProjectConfigurationPlatforms
				//FIXME:  .ActiveCfg

				//Write 'others'

				SlnData data = (SlnData) c.ExtendedProperties [typeof (SlnFileFormat)];
				if (data.GlobalExtra != null) {
					foreach (string s in data.GlobalExtra)
						sw.WriteLine (s);
				}
				
				sw.WriteLine ("EndGlobal");
			}
		}

		void WriteProjects (Combine combine, string baseDirectory, StreamWriter writer)
		{
			foreach (CombineEntry ce in combine.Entries) {
				Combine c = ce as Combine;

				List<string> l = null;
				if (c == null) {
					//Project
					MSBuildData msbData = (MSBuildData) ce.ExtendedProperties [typeof (MSBuildFileFormat)];
					if (msbData == null)
						//This should not happen as any .mdp would've been converted to
						//.*proj and so would have this object
						//FIXME : But can happen for unsupported project types.. eg. md-unit
						throw new Exception (String.Format (
							"INTERNAL ERROR: Project named '{0}', filename = {1}, does not have a 'data' object.", 
							ce.Name, ce.FileName));

					l = msbData.Extra;

					writer.WriteLine (@"Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{{{3}}}""",
						projectTypeGuid.ToString ().ToUpper (),
						ce.Name, 
						Runtime.FileUtilityService.AbsoluteToRelativePath (baseDirectory, ce.FileName),
						msbData.Guid);
				} else {
					//Solution
					SlnData slnData = (SlnData) c.ExtendedProperties [typeof (SlnFileFormat)];
					if (slnData == null)
						throw new Exception (String.Format (
							"INTERNAL ERROR: Solution named '{0}', filename = {1}, does not have a 'data' object.", 
							ce.Name, ce.FileName));

					l = slnData.Extra;
					
					writer.WriteLine (@"Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{{{3}}}""",
						folderTypeGuid.ToString ().ToUpper (),
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
					WriteProjects (c, baseDirectory, writer);
			}
		}

		void WriteNestedProjects (Combine combine, StreamWriter writer)
		{
			foreach (CombineEntry ce in combine.Entries) {
				Combine c = ce as Combine;
				if (c == null || c.ParentCombine == null)
					continue;

				WriteNestedProjects (c, writer);
			}

			SlnData data = (SlnData) combine.ExtendedProperties [typeof (SlnFileFormat)];
			if (data == null)
				throw new Exception (String.Format (
					"INTERNAL ERROR: Solution named '{0}', filename = {1}, does not have a 'data' object.", 
					combine.Name, combine.FileName));

			string containerGuid = data.Guid;
			Combine root = combine.RootCombine;
			foreach (CombineEntry ce in combine.Entries) {
				if (ce.ParentCombine == root)
					continue;

				string containeeGuid = null;
				if (ce is Combine) {
					SlnData slnData = (SlnData) ce.ExtendedProperties [typeof (SlnFileFormat)];
					containeeGuid = slnData.Guid;
				} else {
					MSBuildData msbData = (MSBuildData) ce.ExtendedProperties [typeof (MSBuildFileFormat)];
					containeeGuid = msbData.Guid;
				}

				writer.WriteLine (@"{0}{{{1}}} = {{{2}}}", "\t\t", containeeGuid, containerGuid);
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
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not load solution: {0}"), fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
			}

			return combine;
		}

		static void SetHandlers (Combine combine, bool setEntries)
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

		static void HandleCombineEntryAdded (object sender, CombineEntryEventArgs e)
		{
			try {
				bool isMds = String.Compare (
					Path.GetExtension (e.CombineEntry.FileName), ".mds", true) == 0;

				ConvertToMSBuild (e.CombineEntry, true);

				//Update the project references
				if (isMds)
					UpdateProjectReferences ((Combine) e.CombineEntry, false);

				//Setting this so that the .sln file get rewritten with the 
				//updated project details.. 
				e.CombineEntry.RootCombine.FileName = e.CombineEntry.RootCombine.FileName;
			} catch (Exception ex) {
				Runtime.LoggingService.DebugFormat ("{0}", ex.Message);
				Console.WriteLine ("HandleCombineEntryAdded : {0}", ex.ToString ());
			}
		}

		internal static CombineEntry ConvertToMSBuild (CombineEntry ce, bool prompt)
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

		internal static void UpdateProjectReferences (Combine c, bool saveProjects)
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
						Runtime.FileUtilityService.AbsoluteToRelativePath (
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

		//ExtendedProperties
		//	Per config
		//		Platform : Eg. Any CPU
		//		SolutionConfigurationPlatforms
		//
		Combine LoadSolution (string fileName, IProgressMonitor monitor)
		{
			//string version = GetSlnFileVersion (fileName);
			//if (version != "9.00")
			//	throw new UnknownProjectVersionException (fileName, version);

			ListDictionary globals = null;
			MSBuildSolution combine = null;
			SlnData data = null;
			List<Section> projectSections = null;
			List<string> lines = null;

			//Parse the .sln file
			using (StreamReader reader = new StreamReader(fileName)) {
				combine = new MSBuildSolution ();
				combine.Name = Path.GetFileNameWithoutExtension (fileName);
				combine.FileName = fileName;
				combine.FileFormat = new SlnFileFormat ();
				data = new SlnData ();
				combine.ExtendedProperties [typeof (SlnFileFormat)] = data;

				string s = null;
				projectSections = new List<Section> ();
				lines = new List<string> ();
				globals = new ListDictionary ();
				//Parse
				while (!reader.EndOfStream) {
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

			Dictionary<string, CombineEntry> entries = new Dictionary<string, CombineEntry> ();
			Regex regex = new Regex(@"Project\(""(\{.*\})""\) = ""(.*)"", ""(.*)"", ""(\{.*\})""");
			foreach (Section sec in projectSections) {
				Match match = regex.Match (lines [sec.Start]);
				if (!match.Success) {
					Runtime.LoggingService.DebugFormat (GettextCatalog.GetString (
						"Invalid Project definition on line number #{0} in file '{1}'. Ignoring.",
						sec.Start + 1,
						fileName));

					continue;
				}

				Guid projTypeGuid = SlnFileFormat.projectTypeGuid;
				try {
					projTypeGuid = new Guid (match.Groups [1].Value);
				} catch (FormatException) {
					//Use default guid as projectGuid
					Runtime.LoggingService.Debug (GettextCatalog.GetString (
						"Invalid Project type guid '{0}' on line #{1}. Trying to read as a Project.",
						match.Groups [1].Value,
						sec.Start + 1));
				}

				string projectName = match.Groups[2].Value;
				string projectPath = match.Groups[3].Value;
				string projectGuid = match.Groups[4].Value;

				if (projTypeGuid == folderTypeGuid) {
					//Solution folder
					MSBuildSolution folder = new MSBuildSolution ();
					folder.Name = projectName;
					folder.FileName = projectPath;
					folder.FileFormat = new SlnFileFormat ();

					SlnData slnData = new SlnData ();
					folder.ExtendedProperties [typeof (SlnFileFormat)] = slnData;

					slnData.Guid = projectGuid.Trim (new char [] {'{', '}'});
					slnData.Extra = lines.GetRange (sec.Start + 1, sec.Count - 2);

					entries [projectGuid] = folder;
					
					continue;
				}

				if (projTypeGuid != projectTypeGuid)
					Runtime.LoggingService.Debug (GettextCatalog.GetString (
						"Unknown project type guid '{0}' on line #{1}. Trying to read as a project.",
						projTypeGuid,
						sec.Start + 1));

				if (!projectPath.StartsWith("http://") &&
					(projectPath.EndsWith (".csproj") || projectPath.EndsWith (".vbproj")))
				{
					projectPath = Path.GetFullPath (MapPath (Path.GetDirectoryName (fileName), projectPath));
					try {
						DotNetProject project = Services.ProjectService.ReadFile (projectPath, monitor) as DotNetProject;
						project.Name = projectName;
						entries [projectGuid] = project;
						MSBuildData msbData = (MSBuildData) project.ExtendedProperties [typeof (MSBuildFileFormat)];
						msbData.Extra = lines.GetRange (sec.Start + 1, sec.Count - 2);
					} catch {
						//
					}
					continue;
				}
				//FIXME: Non .csproj/.vbproj projects not supported (yet)
				monitor.ReportWarning (GettextCatalog.GetString (
					"{0}({1}): Unsupported or unrecognized project : '{2}'. See logs.", fileName, sec.Start + 1, projectPath));

				data.UnknownProjects.AddRange (lines.GetRange (sec.Start, sec.Count));
			}

			if (globals != null && globals.Contains ("NestedProjects")) {
				LoadNestedProjects (globals ["NestedProjects"] as Section, lines, entries, monitor);
				globals.Remove ("NestedProjects");
			}

			//Add top level folders and projects to the main combine
			foreach (CombineEntry ce in entries.Values) {
				if (ce.ParentCombine == null)
					combine.Entries.Add (ce);
			}

			//FIXME: This can be just SolutionConfiguration also!
			if (globals != null && globals.Contains ("SolutionConfigurationPlatforms")) {
				LoadConfigurations (globals ["SolutionConfigurationPlatforms"] as Section, lines,
					combine, monitor);
				globals.Remove ("SolutionConfigurationPlatforms");
			}

			//Save the global sections that we dont use
			List<string> globalLines = new List<string> ();
			foreach (Section sec in globals.Values)
				globalLines.InsertRange (globalLines.Count, lines.GetRange (sec.Start, sec.Count));

			data.GlobalExtra = globalLines;
			return combine;
		}

		void ParseGlobal (StreamReader reader, List<string> lines, ListDictionary dict)
		{
			Regex regex = new Regex (@"GlobalSection\s*\(([^)]*)\)\s*=\s*(\w*)");

			//Process GlobalSection-s
			while (!reader.EndOfStream) {
				string s = GetNextLine (reader, lines).Trim ();
				if (s.Length == 0)
					//Skip blank lines
					continue;

				Match m = regex.Match (s);
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

		void LoadConfigurations (Section sec, List<string> lines, Combine combine, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "preSolution", true) != 0)
				return;

			for (int i = 0; i < sec.Count - 2; i ++) {
				//FIXME: expects both key and val to be on the same line
				string s = lines [i + sec.Start + 1].Trim ();
				if (s.Length == 0)
					//Skip blank lines
					continue;

				KeyValuePair<string, string> pair = SplitKeyValue (s);

				//key : Debug|Any CPU
				string [] key_parts = pair.Key.Split (new char [] {'|'}, 2);
				if (key_parts.Length == 0)
					continue;
				
				CombineConfiguration config = (CombineConfiguration) 
					combine.GetConfiguration (key_parts [0]);
				
				if (config == null) {
					config = (CombineConfiguration) 
						combine.CreateConfiguration (key_parts [0]);
					combine.Configurations.Add (config);
				}
				
				if (key_parts.Length > 1) {
					if (String.Compare (key_parts [1], "Any CPU", true) != 0)
						Runtime.LoggingService.Debug (GettextCatalog.GetString (
							"Platform specific configurations not supported. Only (default) 'Any CPU' supported."));

					config.ExtendedProperties.Add ("Platform", key_parts [1]);
				}

				config.ExtendedProperties.Add ("SolutionConfigurationPlatforms", s);
			}
		}

		void LoadNestedProjects (Section sec, List<string> lines,
			Dictionary<string, CombineEntry> entries, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "preSolution", true) != 0)
				return;

			for (int i = 0; i < sec.Count; i ++) {
				KeyValuePair<string, string> pair = SplitKeyValue (lines [i + sec.Start + 1].Trim ());

				if (!entries.ContainsKey (pair.Value)) {
					//Container not found
					Runtime.LoggingService.Debug ("Project with guid '{0}' not found.", pair.Value);
					continue;
				}

				if (!entries.ContainsKey (pair.Key)) {
					//Containee not found
					Runtime.LoggingService.Debug ("Project with guid '{0}' not found.", pair.Key);
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
			if (reader.EndOfStream)
				return null;

			string ret = reader.ReadLine ();
			list.Add (ret);
			return ret;
		}

		int ReadUntil (string end, StreamReader reader, List<string> lines)
		{
			int ret = -1;
			while (!reader.EndOfStream) {
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
			Regex regex = new Regex(@"Microsoft Visual Studio Solution File, Format Version (\d.\d\d)");
			
			strInput = reader.ReadLine();

			match = regex.Match(strInput);
			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}

		internal static string MapPath (string basePath, string relPath)
		{
			if (relPath == null || relPath.Length == 0)
				return null;
			
			string path = relPath.Replace ("\\", "/");
			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':')
				return null;
			
			if (basePath != null)
				path = Path.Combine (basePath, path);
				
			if (Path.IsPathRooted (path)) {
					
				// Windows paths are case-insensitive. When mapping an absolute path
				// we can try to find the correct case for the path.
				
				string[] names = path.Substring (1).Split ('/');
				string part = "/";
				
				for (int n=0; n<names.Length; n++) {
					string[] entries;
					if (n < names.Length - 1)
						entries = Directory.GetDirectories (part);
					else
						entries = Directory.GetFiles (part);
					
					string fpath = null;
					foreach (string e in entries) {
						if (string.Compare (Path.GetFileName (e), names[n], true) == 0) {
							fpath = e;
							break;
						}
					}
					if (fpath == null) {
						// Part of the path does not exist. Can't do any more checking.
						for (; n < names.Length; n++)
							part += "/" + names[n];
						return part;
					}
					
					part = fpath;
				}
				return part;
			} else
				return path;
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

	class SlnData
	{
		string guid;
		Dictionary<CombineConfiguration, string> configStrings;
		List<string> globalExtra; // unused GlobalSections
		List<string> extra; //used by solution folders..
		List<string> unknownProjects;

		public string Guid {
			get { return guid; }
			set { guid = value; }
		}

		public Dictionary<CombineConfiguration, string> ConfigStrings {
			get {
				if (configStrings == null)
					configStrings = new Dictionary<CombineConfiguration, string> ();
				return configStrings;
			}
		}

		public List<string> GlobalExtra {
			get { return globalExtra; }
			set { globalExtra = value; }
		}

		public List<string> Extra {
			get { return extra; }
			set { extra = value; }
		}
		
		public List<string> UnknownProjects {
			get {
				if (unknownProjects == null)
					unknownProjects = new List<string> ();
				return unknownProjects;
			}
		}

	}

	class MSBuildSolution : Combine
	{
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

		public static void HandleAddEntry (object s, AddEntryEventArgs args)
		{
			if (args.Combine.GetType () != typeof (MSBuildSolution))
				return;

			string extn = Path.GetExtension (args.FileName);

			//FIXME: Use IFileFormat.CanReadFile 
			if (String.Compare (extn, ".mdp", true) == 0 || String.Compare (extn, ".mds", true) == 0) {
				if (!IdeApp.Services.MessageService.AskQuestionFormatted ( 
					"Conversion required", "The project file {0} must be converted to " + 
					"msbuild format to be added to a msbuild solution. Convert?", args.FileName)) {
					args.Cancel = true;
					return;
				}
			}

			IProgressMonitor monitor = new NullProgressMonitor ();
			CombineEntry ce = Services.ProjectService.ReadFile (args.FileName, monitor);
			ce = SlnFileFormat.ConvertToMSBuild (ce, false);
			args.FileName = ce.FileName;

			if (String.Compare (extn, ".mds", true) == 0)
				SlnFileFormat.UpdateProjectReferences ((Combine) ce, true);

			ce.FileFormat.WriteFile (ce.FileName, ce, monitor);
		}

	}

}
