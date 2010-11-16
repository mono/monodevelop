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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	internal class SlnFileFormat
	{
		public string GetValidFormatName (object obj, string fileName, MSBuildFileFormat format)
		{
			return Path.ChangeExtension (fileName, ".sln");
		}
		
		public bool CanReadFile (string file, MSBuildFileFormat format)
		{
			if (String.Compare (Path.GetExtension (file), ".sln", true) == 0) {
				string tmp;
				string version = GetSlnFileVersion (file, out tmp);
				return version == format.SlnVersion;
			}
			return false;
		}
		
		public bool CanWriteFile (object obj, MSBuildFileFormat format)
		{
			return obj is Solution;
		}
		
		public List<string> GetItemFiles (object obj)
		{
			return null;
		}
		
		public void WriteFile (string file, object obj, MSBuildFileFormat format, bool saveProjects, IProgressMonitor monitor)
		{
			Solution sol = (Solution) obj;

			string tmpfilename = String.Empty;
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", file), 1);
				try {
					if (File.Exists (file))
						tmpfilename = Path.GetTempFileName ();
				} catch (IOException) {
				}

				string baseDir = Path.GetDirectoryName (file);
				if (tmpfilename == String.Empty) {
					WriteFileInternal (file, sol, baseDir, format, saveProjects, monitor);
				} else {
					WriteFileInternal (tmpfilename, sol, baseDir, format, saveProjects, monitor);
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

		void WriteFileInternal (string file, Solution solution, string baseDir, MSBuildFileFormat format, bool saveProjects, IProgressMonitor monitor)
		{
			SolutionFolder c = solution.RootFolder;
			
			using (StreamWriter sw = new StreamWriter (file, false, Encoding.UTF8)) {
				sw.NewLine = "\r\n";

				SlnData slnData = GetSlnData (c);
				if (slnData == null) {
					// If a non-msbuild project is being converted by just
					// changing the fileformat, then create the SlnData for it
					slnData = new SlnData ();
					c.ExtendedProperties [typeof (SlnFileFormat)] = slnData;
				}

				slnData.UpdateVersion (format);

				sw.WriteLine ();
				//Write Header
				sw.WriteLine ("Microsoft Visual Studio Solution File, Format Version " + slnData.VersionString);
				sw.WriteLine (slnData.HeaderComment);

				//Write the projects
				monitor.BeginTask (GettextCatalog.GetString ("Saving projects"), 1);
				WriteProjects (c, baseDir, sw, saveProjects, monitor);
				monitor.EndTask ();

				//Write the lines for unknownProjects
				foreach (string l in slnData.UnknownProjects)
					sw.WriteLine (l);

				//Write the Globals
				sw.WriteLine ("Global");

				//Write SolutionConfigurationPlatforms
				//FIXME: SolutionConfigurations?
				sw.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

				foreach (SolutionConfiguration config in solution.Configurations)
					sw.WriteLine ("\t\t{0} = {0}", ToSlnConfigurationId (config));

				sw.WriteLine ("\tEndGlobalSection");

				//Write ProjectConfigurationPlatforms
				sw.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

				List<string> list = new List<string> ();
				WriteProjectConfigurations (solution, list);

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
				ICollection<SolutionFolder> folders = solution.RootFolder.GetAllItems<SolutionFolder> ();
				if (folders.Count > 1) {
					// If folders ==1, that's the root folder
					sw.WriteLine ("\tGlobalSection(NestedProjects) = preSolution");
					foreach (SolutionFolder folder in folders) {
						if (folder.IsRoot)
							continue;
						WriteNestedProjects (folder, solution.RootFolder, sw);
					}
					sw.WriteLine ("\tEndGlobalSection");
				}
				
				//Write custom properties
				MSBuildSerializer ser = new MSBuildSerializer (solution.FileName);
				DataItem data = (DataItem) ser.Serialize (solution, typeof(Solution));
				if (data.HasItemData) {
					sw.WriteLine ("\tGlobalSection(MonoDevelopProperties) = preSolution");
					WriteDataItem (sw, data);
					sw.WriteLine ("\tEndGlobalSection");
				}
				
				// Write custom properties for configurations
				foreach (SolutionConfiguration conf in solution.Configurations) {
					data = (DataItem) ser.Serialize (conf);
					if (data.HasItemData) {
						sw.WriteLine ("\tGlobalSection(MonoDevelopProperties." + conf.Id + ") = preSolution");
						WriteDataItem (sw, data);
						sw.WriteLine ("\tEndGlobalSection");
					}
				}

				//Write 'others'
				if (slnData.GlobalExtra != null) {
					foreach (string s in slnData.GlobalExtra)
						sw.WriteLine (s);
				}
				
				sw.WriteLine ("EndGlobal");
			}
		}

		void WriteProjects (SolutionFolder folder, string baseDirectory, StreamWriter writer, bool saveProjects, IProgressMonitor monitor)
		{
			monitor.BeginStepTask (GettextCatalog.GetString ("Saving projects"), folder.Items.Count, 1); 
			foreach (SolutionItem ce in folder.Items)
			{
				string[] l = null;
				if (ce is SolutionEntityItem) {
					
					SolutionEntityItem item = (SolutionEntityItem) ce;
					MSBuildHandler handler = MSBuildProjectService.GetItemHandler (item);
					
					if (saveProjects) {
						try {
							handler.SavingSolution = true;
							item.Save (monitor);
						} finally {
							handler.SavingSolution = false;
						}
					}

					l = handler.SlnProjectContent;

					writer.WriteLine (@"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""",
					    handler.TypeGuid,
						item.Name, 
						FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (
							baseDirectory, item.FileName)).Replace ('/', '\\'),
						ce.ItemId);
					DataItem data = handler.WriteSlnData ();
					if (data != null && data.HasItemData) {
						writer.WriteLine ("\tProjectSection(MonoDevelopProperties) = preProject");
						WriteDataItem (writer, data);
						writer.WriteLine ("\tEndProjectSection");
					}
				} else if (ce is SolutionFolder) {
					//Solution
					SlnData slnData = GetSlnData (ce);
					if (slnData == null) {
						// Solution folder
						slnData = new SlnData ();
						ce.ExtendedProperties [typeof (SlnFileFormat)] = slnData;
					}

					l = slnData.Extra;
					
					writer.WriteLine (@"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""",
						MSBuildProjectService.FolderTypeGuid,
						ce.Name, 
						ce.Name,
						ce.ItemId);
					
					// Folder files
					WriteFolderFiles (writer, (SolutionFolder) ce);
					
					//Write custom properties
					MSBuildSerializer ser = new MSBuildSerializer (folder.ParentSolution.FileName);
					DataItem data = (DataItem) ser.Serialize (ce, typeof(SolutionFolder));
					if (data.HasItemData) {
						writer.WriteLine ("\tProjectSection(MonoDevelopProperties) = preProject");
						WriteDataItem (writer, data);
						writer.WriteLine ("\tEndProjectSection");
					}
				}

				if (l != null) {
					foreach (string s in l)
						writer.WriteLine (s);
				}

				writer.WriteLine ("EndProject");
				if (ce is SolutionFolder)
					WriteProjects (ce as SolutionFolder, baseDirectory, writer, saveProjects, monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}
		
		void WriteFolderFiles (StreamWriter writer, SolutionFolder folder)
		{
			if (folder.Files.Count > 0) {
				writer.WriteLine ("\tProjectSection(SolutionItems) = preProject");
				foreach (FilePath f in folder.Files) {
					string relFile = MSBuildProjectService.ToMSBuildPathRelative (folder.ParentSolution.ItemDirectory, f);
					writer.WriteLine ("\t\t" + relFile + " = " + relFile);
				}
				writer.WriteLine ("\tEndProjectSection");
			}
		}

		void WriteProjectConfigurations (Solution sol, List<string> list)
		{
			foreach (SolutionConfiguration cc in sol.Configurations) {

				foreach (SolutionConfigurationEntry cce in cc.Configurations) {
					SolutionEntityItem p = cce.Item;
					
					// Ignore unknown projects. We deal with them below
					if (p is UnknownSolutionItem)
						continue;
					
					list.Add (String.Format (
						"\t\t{0}.{1}.ActiveCfg = {2}", p.ItemId, ToSlnConfigurationId (cc), ToSlnConfigurationId (cce.ItemConfiguration)));

					if (cce.Build)
						list.Add (String.Format (
							"\t\t{0}.{1}.Build.0 = {2}", p.ItemId, ToSlnConfigurationId (cc), ToSlnConfigurationId (cce.ItemConfiguration)));
				}
			}
			
			// Dump config lines for unknown projects
			foreach (UnknownSolutionItem item in sol.GetAllSolutionItems<UnknownSolutionItem> ()) {
				ItemSlnData data = ItemSlnData.ForItem (item);
				list.AddRange (data.ConfigLines);
			}
		}

		void WriteNestedProjects (SolutionFolder folder, SolutionFolder root, StreamWriter writer)
		{
			foreach (SolutionItem ce in folder.Items)
				writer.WriteLine (@"{0}{1} = {2}", "\t\t", ce.ItemId, folder.ItemId);
		}
		
		DataItem GetSolutionItemData (List<string> lines)
		{
			// Find a project section of type MonoDevelopProperties
			int start, end;
			if (!FindSection (lines, "MonoDevelopProperties", out start, out end))
				return null;
			
			// Deserialize the object
			DataItem it = ReadDataItem (start, end - start + 1, lines);
			
			// Remove the lines, since they have already been preocessed
			lines.RemoveRange (start, end - start + 1);
			return it;
		}
		
		List<string> ReadFolderFiles (List<string> lines)
		{
			// Find a solution item section of type SolutionItems

			List<string> list = new List<string> ();
			int start, end;
			if (!FindSection (lines, "SolutionItems", out start, out end))
				return list;
			
			for (int n=start + 1; n < end; n++) {
				string file = lines [n];
				int i = file.IndexOf ('=');
				if (i == -1)
					continue;
				file = file.Substring (0, i).Trim (' ','\t');
				if (file.Length > 0)
					list.Add (file);
			}
			
			// Remove the lines, since they have already been preocessed
			lines.RemoveRange (start, end - start + 1);
			return list;
		}
		
		bool FindSection (List<string> lines, string name, out int start, out int end)
		{
			start = -1;
			end = -1;
			
			for (int n=0; n<lines.Count && start == -1; n++) {
				string line = lines [n].Replace ("\t","").Replace (" ", "");
				if (line == "ProjectSection(" + name + ")=preProject")
					start = n;
			}
			if (start == -1)
				return false;

			for (int n=start+1; n<lines.Count && end == -1; n++) {
				string line = lines [n].Replace ("\t","").Replace (" ", "");
				if (line == "EndProjectSection")
					end = n;
			}
			return end != -1;
		}
		
		void DeserializeSolutionItem (Solution sln, SolutionItem item, List<string> lines)
		{
			// Deserialize the object
			DataItem it = GetSolutionItemData (lines);
			if (it == null)
				return;
			
			MSBuildSerializer ser = new MSBuildSerializer (sln.FileName);
			ser.SerializationContext.BaseFile = sln.FileName;
			ser.Deserialize (item, it);
		}
		
		void WriteDataItem (StreamWriter sw, DataItem item)
		{
			int id = 0;
			foreach (DataNode val in item.ItemData)
				WriteDataNode (sw, "", val, ref id);
		}
		
		void WriteDataNode (StreamWriter sw, string prefix, DataNode node, ref int id)
		{
			string name = node.Name;
			string newPrefix = prefix.Length > 0 ? prefix + "." + name: name;
			
			if (node is DataValue) {
				DataValue val = (DataValue) node;
				string value = EncodeString (val.Value);
				sw.WriteLine ("\t\t" + newPrefix + " = " + value);
			}
			else {
				DataItem it = (DataItem) node;
				sw.WriteLine ("\t\t" + newPrefix + " = $" + id);
				newPrefix = "$" + id;
				id ++;
				foreach (DataNode cn in it.ItemData)
					WriteDataNode (sw, newPrefix, cn, ref id);
			}
		}
		
		string EncodeString (string val)
		{
			if (val.Length == 0)
				return val;
			
			int i = val.IndexOfAny (new char[] {'\n','\r','\t'});
			if (i != -1 || val [0] == '@') {
				StringBuilder sb = new StringBuilder ();
				if (i != -1) {
					int fi = val.IndexOf ('\\');
					if (fi != -1 && fi < i) i = fi;
					sb.Append (val.Substring (0,i));
				} else
					i = 0;
				for (int n = i; n < val.Length; n++) {
					char c = val [n];
					if (c == '\r')
						sb.Append (@"\r");
					else if (c == '\n')
						sb.Append (@"\n");
					else if (c == '\t')
						sb.Append (@"\t");
					else if (c == '\\')
						sb.Append (@"\\");
					else
						sb.Append (c);
				}
				val = "@" + sb.ToString ();
			}
			char fc = val [0];
			char lc = val [val.Length - 1];
		    if (fc == ' ' || fc == '"' || fc == '$' || lc == ' ')
				val = "\"" + val + "\"";
			return val;
		}
		
		string DecodeString (string val)
		{
			val = val.Trim (' ', '\t');
			if (val.Length == 0)
				return val;
			if (val [0] == '\"')
				val = val.Substring (1, val.Length - 2);
			if (val [0] == '@') {
				StringBuilder sb = new StringBuilder (val.Length);
				for (int n = 1; n < val.Length; n++) {
					char c = val [n];
					if (c == '\\') {
						c = val [++n];
						if (c == 'r') c = '\r';
						else if (c == 'n') c = '\n';
						else if (c == 't') c = '\t';
					}
					sb.Append (c);
				}
				return sb.ToString ();
			}
			else
				return val;
		}
		
		DataItem ReadDataItem (Section sec, List<string> lines)
		{
			return ReadDataItem (sec.Start, sec.Count, lines);
		}
		
		DataItem ReadDataItem (int start, int count, List<string> lines)
		{
			DataItem it = new DataItem ();
			int lineNum = start + 1;
			int lastLine = start + count - 2;
			while (lineNum <= lastLine) {
				if (!ReadDataNode (it, lines, lastLine, "", ref lineNum))
					lineNum++;
			}
			return it;
		}
		
		bool ReadDataNode (DataItem item, List<string> lines, int lastLine, string prefix, ref int lineNum)
		{
			string s = lines [lineNum].Trim (' ','\t');
			
			if (s.Length == 0) {
				lineNum++;
				return true;
			}
			
			// Check if the line belongs to the current item
			if (prefix.Length > 0) {
				if (!s.StartsWith (prefix + "."))
					return false;
				s = s.Substring (prefix.Length + 1);
			} else {
				if (s.StartsWith ("$"))
					return false;
			}
			
			int i = s.IndexOf ('=');
			if (i == -1) {
				lineNum++;
				return true;
			}

			string name = s.Substring (0, i).Trim (' ','\t');
			if (name.Length == 0) {
				lineNum++;
				return true;
			}
			
			string value = s.Substring (i+1).Trim (' ','\t');
			if (value.StartsWith ("$")) {
				// New item
				DataItem child = new DataItem ();
				child.Name = name;
				lineNum++;
				while (lineNum <= lastLine) {
					if (!ReadDataNode (child, lines, lastLine, value, ref lineNum))
						break;
				}
				item.ItemData.Add (child);
			}
			else {
				value = DecodeString (value);
				DataValue val = new DataValue (name, value);
				item.ItemData.Add (val);
				lineNum++;
			}
			return true;
		}
		
		string ToSlnConfigurationId (ItemConfiguration configuration)
		{
			if (configuration.Platform.Length == 0)
				return configuration.Name + "|Any CPU";
			else
				return configuration.Name + "|" + configuration.Platform;
		}
		
		string FromSlnConfigurationId (string configId)
		{
			int i = configId.IndexOf ('|');
			if (i != -1) {
				if (configId.Substring (i+1) == "Any CPU")
					return configId.Substring (0, i);
			}
			return configId;
		}

		string ToSlnConfigurationId (string configId)
		{
			if (configId.IndexOf ('|') == -1)
				return configId + "|Any CPU";
			else
				return configId;
		}

		//Reader
		public object ReadFile (string fileName, MSBuildFileFormat format, IProgressMonitor monitor)
		{
			if (fileName == null || monitor == null)
				return null;

			Solution sol;
			try {
				ProjectExtensionUtil.BeginLoadOperation ();
				sol = new Solution ();
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution: {0}"), fileName), 1);
				LoadSolution (sol, fileName, format, monitor);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load solution: {0}", fileName), ex);
				throw;
			} finally {
				ProjectExtensionUtil.EndLoadOperation ();
				monitor.EndTask ();
			}
			return sol;
		}

		//ExtendedProperties
		//	Per config
		//		Platform : Eg. Any CPU
		//		SolutionConfigurationPlatforms
		//
		SolutionFolder LoadSolution (Solution sol, string fileName, MSBuildFileFormat format, IProgressMonitor monitor)
		{
			string headerComment;
			string version = GetSlnFileVersion (fileName, out headerComment);

			ListDictionary globals = null;
			SolutionFolder folder = null;
			SlnData data = null;
			List<Section> projectSections = null;
			List<string> lines = null;
			
			FileFormat projectFormat = Services.ProjectService.FileFormats.GetFileFormat (format);

			monitor.BeginTask (GettextCatalog.GetString ("Loading solution: {0}", fileName), 1);
			//Parse the .sln file
			using (StreamReader reader = new StreamReader(fileName)) {
				sol.FileName = fileName;
				sol.ConvertToFormat (projectFormat, false);
				folder = sol.RootFolder;
				sol.Version = "0.1"; //FIXME:
				data = new SlnData ();
				folder.ExtendedProperties [typeof (SlnFileFormat)] = data;
				data.VersionString = version;
				data.HeaderComment = headerComment;

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
			Dictionary<string, SolutionItem> items = new Dictionary<string, SolutionItem> ();
			List<SolutionItem> sortedList = new List<SolutionItem> ();
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

				if (projTypeGuid == MSBuildProjectService.FolderTypeGuid) {
					//Solution folder
					SolutionFolder sfolder = new SolutionFolder ();
					sfolder.Name = projectName;
					MSBuildProjectService.InitializeItemHandler (sfolder);
					MSBuildProjectService.SetId (sfolder, projectGuid);

					List<string> projLines = lines.GetRange (sec.Start + 1, sec.Count - 2);
					DeserializeSolutionItem (sol, sfolder, projLines);
					
					foreach (string f in ReadFolderFiles (projLines))
						sfolder.Files.Add (MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (fileName), f));
					
					SlnData slnData = new SlnData ();
					slnData.Extra = projLines.ToArray ();
					sfolder.ExtendedProperties [typeof (SlnFileFormat)] = slnData;

					items.Add (projectGuid, sfolder);
					sortedList.Add (sfolder);
					
					continue;
				}

				if (projectPath.StartsWith("http://")) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"{0}({1}): Projects with non-local source (http://...) not supported. '{2}'.",
						fileName, sec.Start + 1, projectPath));
					data.UnknownProjects.AddRange (lines.GetRange (sec.Start, sec.Count));
					continue;
				}

				string path = MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (fileName), projectPath);
				if (String.IsNullOrEmpty (path)) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", fileName, projectPath));
					LoggingService.LogWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", fileName, projectPath));
					continue;
				}

				projectPath = Path.GetFullPath (path);
				
				SolutionEntityItem item = null;
				
				try {
					item = ProjectExtensionUtil.LoadSolutionItem (monitor, projectPath, delegate {
						return MSBuildProjectService.LoadItem (monitor, projectPath, projTypeGuid, projectGuid);
					});
					
					if (item == null) {
						LoggingService.LogWarning (GettextCatalog.GetString (
							"Unknown project type guid '{0}' on line #{1}. Ignoring.",
							projTypeGuid,
							sec.Start + 1));
						monitor.ReportWarning (GettextCatalog.GetString (
							"{0}({1}): Unsupported or unrecognized project : '{2}'.", 
							fileName, sec.Start + 1, projectPath));
						continue;
					}

					MSBuildProjectHandler handler = (MSBuildProjectHandler) item.ItemHandler;
					List<string> projLines = lines.GetRange (sec.Start + 1, sec.Count - 2);
					DataItem it = GetSolutionItemData (projLines);
					handler.SlnProjectContent = projLines.ToArray ();
					handler.ReadSlnData (it);
					
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString (
								"Error while trying to load the project {0}. Exception : {1}",
								projectPath, e.ToString ()));
					monitor.ReportWarning (GettextCatalog.GetString (
						"Error while trying to load the project '{0}': {1}", projectPath, e.Message));

					UnknownSolutionItem uitem = new UnknownSolutionItem ();
					uitem.FileName = projectPath;
					uitem.LoadError = e.Message;
					MSBuildHandler h = new MSBuildHandler (projTypeGuid, projectGuid);
					h.Item = uitem;
					uitem.SetItemHandler (h);
					item = uitem;
				}
				
				if (!items.ContainsKey (projectGuid)) {
					items.Add (projectGuid, item);
					sortedList.Add (item);
					data.ItemsByGuid [projectGuid] = item;
				} else {
					monitor.ReportError (GettextCatalog.GetString ("Invalid solution file. There are two projects with the same GUID. The project {0} will be ignored.", projectPath), null);
				}
			}
			monitor.EndTask ();

			if (globals != null && globals.Contains ("NestedProjects")) {
				LoadNestedProjects (globals ["NestedProjects"] as Section, lines, items, monitor);
				globals.Remove ("NestedProjects");
			}

			//Add top level folders and projects to the main folder
			foreach (SolutionItem ce in sortedList) {
				if (ce.ParentFolder == null)
					folder.Items.Add (ce);
			}

			//FIXME: This can be just SolutionConfiguration also!
			if (globals != null) {
				if (globals.Contains ("SolutionConfigurationPlatforms")) {
					LoadSolutionConfigurations (globals ["SolutionConfigurationPlatforms"] as Section, lines,
						sol, monitor);
					globals.Remove ("SolutionConfigurationPlatforms");
				}

				if (globals.Contains ("ProjectConfigurationPlatforms")) {
					LoadProjectConfigurationMappings (globals ["ProjectConfigurationPlatforms"] as Section, lines,
						sol, monitor);
					globals.Remove ("ProjectConfigurationPlatforms");
				}

				if (globals.Contains ("MonoDevelopProperties")) {
					LoadMonoDevelopProperties (globals ["MonoDevelopProperties"] as Section, lines,	sol, monitor);
					globals.Remove ("MonoDevelopProperties");
				}
				
				ArrayList toRemove = new ArrayList ();
				foreach (DictionaryEntry e in globals) {
					string name = (string) e.Key;
					if (name.StartsWith ("MonoDevelopProperties.")) {
						int i = name.IndexOf ('.');
						LoadMonoDevelopConfigurationProperties (name.Substring (i+1), (Section)e.Value, lines, sol, monitor);
						toRemove.Add (e.Key);
					}
				}
				foreach (object key in toRemove)
					globals.Remove (key);
			}

			//Save the global sections that we dont use
			List<string> globalLines = new List<string> ();
			foreach (Section sec in globals.Values)
				globalLines.InsertRange (globalLines.Count, lines.GetRange (sec.Start, sec.Count));

			data.GlobalExtra = globalLines;
			monitor.EndTask ();
			return folder;
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

		void LoadProjectConfigurationMappings (Section sec, List<string> lines, Solution sln, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "postSolution", true) != 0)
				return;

			List<SolutionConfigurationEntry> noBuildList = new List<SolutionConfigurationEntry> ();
			Dictionary<string, SolutionConfigurationEntry> cache = new Dictionary<string, SolutionConfigurationEntry> ();
			Dictionary<string, string> ignoredProjects = new Dictionary<string, string> ();
			SlnData slnData = GetSlnData (sln.RootFolder);
			
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

				if (!slnData.ItemsByGuid.ContainsKey (projGuid)) {
					if (ignoredProjects.ContainsKey (projGuid))
						// already warned
						continue;

					LoggingService.LogWarning (GettextCatalog.GetString ("{0} ({1}) : Project with guid = '{2}' not found or not loaded. Ignoring", 
						sln.FileName, lineNum + 1, projGuid));
					ignoredProjects [projGuid] = projGuid;
					continue;
				}

				SolutionEntityItem item;
				if (slnData.ItemsByGuid.TryGetValue (projGuid, out item)) {
					if (item is UnknownSolutionItem) {
						ItemSlnData data = ItemSlnData.ForItem (item);
						data.ConfigLines.Add (lines [lineNum]);
						extras.RemoveAt (extras.Count - 1);
						continue;
					}
					string key = projGuid + "." + slnConfig;
					SolutionConfigurationEntry combineConfigEntry = null;
					if (cache.ContainsKey (key)) {
						combineConfigEntry = cache [key];
					} else {
						combineConfigEntry = GetConfigEntry (sln, item, slnConfig);
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
					if (action == "ActiveCfg") {
						combineConfigEntry.ItemConfiguration = FromSlnConfigurationId (projConfig);
						noBuildList.Add (combineConfigEntry);
					} else if (action == "Build.0") {
						noBuildList.Remove (combineConfigEntry);
					}
				}
				extras.RemoveAt (extras.Count - 1);
			}

			slnData.SectionExtras ["ProjectConfigurationPlatforms"] = extras;

			foreach (SolutionConfigurationEntry e in noBuildList) {
				//Mark (build=false) of all projects for which 
				//ActiveCfg was found but no Build.0
				e.Build = false;
			}
		}

		/* Gets the CombineConfigurationEntry corresponding to the @entry in its parentCombine's 
		 * CombineConfiguration. Creates the required bits if not present */
		SolutionConfigurationEntry GetConfigEntry (Solution sol, SolutionEntityItem item, string configName)
		{
			configName = FromSlnConfigurationId (configName);
			
			SolutionConfiguration solutionConfig = sol.Configurations [configName];
			if (solutionConfig == null) {
				solutionConfig = CreateSolutionConfigurationFromId (configName);
				sol.Configurations.Add (solutionConfig);
			}

			SolutionConfigurationEntry conf = solutionConfig.GetEntryForItem (item);
			if (conf != null)
				return conf;
			return solutionConfig.AddItem (item);
		}

		void LoadSolutionConfigurations (Section sec, List<string> lines, Solution solution, IProgressMonitor monitor)
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
				
				string configId = FromSlnConfigurationId (pair.Key);
				SolutionConfiguration config = solution.Configurations [configId];
				
				if (config == null) {
					config = CreateSolutionConfigurationFromId (configId);
					solution.Configurations.Add (config);
				}
			}
		}
		
		SolutionConfiguration CreateSolutionConfigurationFromId (string fullId)
		{
			return new SolutionConfiguration (fullId);
		}

		void LoadMonoDevelopProperties (Section sec, List<string> lines, Solution sln, IProgressMonitor monitor)
		{
			DataItem it = ReadDataItem (sec, lines);
			MSBuildSerializer ser = new MSBuildSerializer (sln.FileName);
			ser.SerializationContext.BaseFile = sln.FileName;
			ser.Deserialize (sln, it);
		}
		
		void LoadMonoDevelopConfigurationProperties (string configName, Section sec, List<string> lines, Solution sln, IProgressMonitor monitor)
		{
			SolutionConfiguration config = sln.Configurations [configName];
			if (config == null)
				return;
			DataItem it = ReadDataItem (sec, lines);
			MSBuildSerializer ser = new MSBuildSerializer (sln.FileName);
			ser.Deserialize (config, it);
		}
		
		void LoadNestedProjects (Section sec, List<string> lines,
			IDictionary<string, SolutionItem> entries, IProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.Val, "preSolution", true) != 0)
				return;

			for (int i = 0; i < sec.Count - 2; i ++) {
				KeyValuePair<string, string> pair = SplitKeyValue (lines [i + sec.Start + 1].Trim ());

				SolutionItem folderItem;
				SolutionItem item;
				
				if (!entries.TryGetValue (pair.Value, out folderItem)) {
					//Container not found
					LoggingService.LogWarning (GettextCatalog.GetString ("Project with guid '{0}' not found.", pair.Value));
					continue;
				}
				
				SolutionFolder folder = folderItem as SolutionFolder;
				if (folder == null) {
					LoggingService.LogWarning (GettextCatalog.GetString ("Item with guid '{0}' is not a folder.", pair.Value));
					continue;
				}

				if (!entries.TryGetValue (pair.Key, out item)) {
					//Containee not found
					LoggingService.LogWarning (GettextCatalog.GetString ("Project with guid '{0}' not found.", pair.Key));
					continue;
				}

				folder.Items.Add (item);
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
		string GetSlnFileVersion(string strInSlnFile, out string headerComment)
		{
			string strVersion = null;
			string strInput = null;
			headerComment = null;
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
				headerComment = reader.ReadLine ();
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}

		static SlnData GetSlnData (SolutionItem c)
		{
			if (c.ExtendedProperties.Contains (typeof (SlnFileFormat)))
				return c.ExtendedProperties [typeof (SlnFileFormat)] as SlnData;
			return null;
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

		public string Name {
			get { return "MSBuild"; }
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
}
