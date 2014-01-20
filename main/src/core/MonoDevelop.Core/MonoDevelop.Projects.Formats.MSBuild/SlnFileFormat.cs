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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class SlnFileFormat
	{
		MSBuildFileFormat format;

		public SlnFileFormat (MSBuildFileFormat format)
		{
			this.format = format;
		}

		public string GetValidFormatName (object obj, string fileName, MSBuildFileFormat format)
		{
			return Path.ChangeExtension (fileName, ".sln");
		}
		
		public bool CanReadFile (string file, MSBuildFileFormat format)
		{
			if (String.Compare (Path.GetExtension (file), ".sln", StringComparison.OrdinalIgnoreCase) == 0) {
				string tmp;
				string version = GetSlnFileVersion (file, out tmp);
				return format.SupportsSlnVersion (version);
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
		
		public Task WriteFile (string file, object obj, bool saveProjects, ProgressMonitor monitor)
		{
			return Task.Factory.StartNew (delegate {
				Solution sol = (Solution)obj;

				string tmpfilename = String.Empty;
				try {
					monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", file), 1);
					try {
						if (File.Exists (file))
							tmpfilename = Path.GetTempFileName ();
					} catch (IOException) {
					}

					if (tmpfilename == String.Empty) {
						WriteFileInternal (file, file, sol, saveProjects, monitor);
					} else {
						WriteFileInternal (tmpfilename, file, sol, saveProjects, monitor);
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
			});
		}

		void WriteFileInternal (string file, string sourceFile, Solution solution, bool saveProjects, ProgressMonitor monitor)
		{
			string baseDir = Path.GetDirectoryName (sourceFile);

			if (saveProjects) {
				var items = solution.GetAllSolutionItems ().ToArray ();
				monitor.BeginTask (items.Length + 1);
				foreach (var item in items) {
					try {
						monitor.BeginStep ();
						item.SavingSolution = true;
						item.Save (monitor);
					} finally {
						item.SavingSolution = false;
					}
				}
			} else {
				monitor.BeginTask (1);
				monitor.BeginStep ();
			}

			SlnFile sln = new SlnFile ();
			sln.BaseDirectory = baseDir;
			if (File.Exists (sourceFile)) {
				try {
					sln.Read (sourceFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Existing solution can't be updated since it can't be read", ex);
				}
			}

			sln.FormatVersion = format.SlnVersion;
			sln.ProductDescription = format.ProductDescription;

			solution.WriteSolution (monitor, sln);

			sln.Write (file);
			monitor.EndTask ();
		}

		internal void WriteFileInternal (SlnFile sln, Solution solution, ProgressMonitor monitor)
		{
			SolutionFolder c = solution.RootFolder;

			// Delete data for projects that have been removed from the solution

			var currentProjects = new HashSet<string> (solution.GetAllItems<SolutionFolderItem> ().Select (it => it.ItemId));
			var removedProjects = new HashSet<string> ();
			if (solution.LoadedProjects != null)
				removedProjects.UnionWith (solution.LoadedProjects.Except (currentProjects));
			var unknownProjects = new HashSet<string> (sln.Projects.Select (p => p.Id).Except (removedProjects).Except (currentProjects));

			foreach (var p in removedProjects) {
				var ps = sln.Projects.GetProject (p);
				if (ps != null)
					sln.Projects.Remove (ps);
				var pc = sln.ProjectConfigurationsSection.GetPropertySet (p, true);
				if (pc != null)
					sln.ProjectConfigurationsSection.Remove (pc);
			}
			var secNested = sln.Sections.GetSection ("NestedProjects");
			if (secNested != null) {
				foreach (var np in secNested.Properties.ToArray ()) {
					if (removedProjects.Contains (np.Key) || removedProjects.Contains (np.Value))
						secNested.Properties.Remove (np.Key);
				}
			}
			solution.LoadedProjects = currentProjects;

			//Write the projects
			using (monitor.BeginTask (GettextCatalog.GetString ("Saving projects"), 1)) {
				monitor.BeginStep ();
				WriteProjects (c, sln, monitor, unknownProjects);
			}

			//FIXME: SolutionConfigurations?

			var pset = sln.SolutionConfigurationsSection;
			foreach (SolutionConfiguration config in solution.Configurations) {
				var cid = ToSlnConfigurationId (config);
				pset.SetValue (cid, cid);
			}

			WriteProjectConfigurations (solution, sln);

			//Write Nested Projects
			ICollection<SolutionFolder> folders = solution.RootFolder.GetAllItems<SolutionFolder> ().ToList ();
			if (folders.Count > 1) {
				// If folders ==1, that's the root folder
				var sec = sln.Sections.GetOrCreateSection ("NestedProjects", "preSolution");
				foreach (SolutionFolder folder in folders) {
					if (folder.IsRoot)
						continue;
					WriteNestedProjects (folder, solution.RootFolder, sec);
				}
				// Remove items which don't have a parent folder
				var toRemove = solution.GetAllItems<SolutionFolderItem> ().Where (it => it.ParentFolder == solution.RootFolder);
				foreach (var it in toRemove)
					sec.Properties.Remove (it.ItemId);
			}
			
			//Write custom properties
			MSBuildSerializer ser = new MSBuildSerializer (solution.FileName);
			DataItem data = (DataItem) ser.Serialize (solution, typeof(Solution));
			if (data.HasItemData) {
				var sec = sln.Sections.GetOrCreateSection ("MonoDevelopProperties", "preSolution");
				WriteDataItem (sec.Properties, data);
			} else
				sln.Sections.RemoveSection ("MonoDevelopProperties");
			
			// Write custom properties for configurations
			foreach (SolutionConfiguration conf in solution.Configurations) {
				data = (DataItem) ser.Serialize (conf);
				string secId = "MonoDevelopProperties." + conf.Id;
				if (data.HasItemData) {
					var sec = sln.Sections.GetOrCreateSection (secId, "preSolution");
					WriteDataItem (sec.Properties, data);
				} else {
					sln.Sections.RemoveSection (secId);
				}
			}
		}

		void WriteProjects (SolutionFolder folder, SlnFile sln, ProgressMonitor monitor, HashSet<string> unknownProjects)
		{
			monitor.BeginTask (folder.Items.Count); 
			foreach (SolutionFolderItem ce in folder.Items)
			{
				monitor.BeginStep ();
				if (ce is SolutionItem) {
					
					SolutionItem item = (SolutionItem) ce;

					var proj = sln.Projects.GetOrCreateProject (ce.ItemId);
					proj.TypeGuid = item.TypeGuid;
					proj.Name = item.Name;
					proj.FilePath = FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (sln.BaseDirectory, item.FileName)).Replace ('/', '\\');

					DataItem data = item.WriteSlnData ();
					if (data != null && data.HasItemData) {
						var sec = proj.Sections.GetOrCreateSection ("MonoDevelopProperties", "preProject");
						WriteDataItem (sec.Properties, data);
					} else
						proj.Sections.RemoveSection ("MonoDevelopProperties");

					if (item.ItemDependencies.Count > 0) {
						var sec = proj.Sections.GetOrCreateSection ("ProjectDependencies", "postProject");
						sec.Properties.ClearExcept (unknownProjects);
						foreach (var dep in item.ItemDependencies)
							sec.Properties.SetValue (dep.ItemId, dep.ItemId);
					} else
						proj.Sections.RemoveSection ("ProjectDependencies");
				} else if (ce is SolutionFolder) {
					var proj = sln.Projects.GetOrCreateProject (ce.ItemId);
					proj.TypeGuid = MSBuildProjectService.FolderTypeGuid;
					proj.Name = ce.Name;
					proj.FilePath = ce.Name;

					// Folder files
					WriteFolderFiles (proj, (SolutionFolder) ce);
					
					//Write custom properties
					MSBuildSerializer ser = new MSBuildSerializer (folder.ParentSolution.FileName);
					DataItem data = (DataItem) ser.Serialize (ce, typeof(SolutionFolder));
					if (data.HasItemData) {
						var sec = proj.Sections.GetOrCreateSection ("MonoDevelopProperties", "preProject");
						WriteDataItem (sec.Properties, data);
					}
				}
				if (ce is SolutionFolder)
					WriteProjects (ce as SolutionFolder, sln, monitor, unknownProjects);
			}
			monitor.EndTask ();
		}
		
		void WriteFolderFiles (SlnProject proj, SolutionFolder folder)
		{
			if (folder.Files.Count > 0) {
				var sec = proj.Sections.GetOrCreateSection ("SolutionItems", "preProject");
				sec.Properties.Clear ();
				foreach (FilePath f in folder.Files) {
					string relFile = MSBuildProjectService.ToMSBuildPathRelative (folder.ParentSolution.ItemDirectory, f);
					sec.Properties.SetValue (relFile, relFile);
				}
			} else
				proj.Sections.RemoveSection ("SolutionItems");
		}

		void WriteProjectConfigurations (Solution sol, SlnFile sln)
		{
			var col = sln.ProjectConfigurationsSection;

			foreach (var item in sol.GetAllSolutionItems ()) {
				// Don't save configurations for shared projects
				if (!item.SupportsConfigurations ())
					continue;

				// <ProjectGuid>...</ProjectGuid> in some Visual Studio generated F# project files 
				// are missing "{"..."}" in their guid. This is not generally a problem since it
				// is a valid GUID format. However the solution file format requires that these are present. 
				string itemGuid = item.ItemId;
				if (!itemGuid.StartsWith ("{") && !itemGuid.EndsWith ("}"))
					itemGuid = "{" + itemGuid + "}";

				var pset = col.GetOrCreatePropertySet (itemGuid, ignoreCase:true);
				pset.Clear ();

				foreach (SolutionConfiguration cc in sol.Configurations) {
					var cce = cc.GetEntryForItem (item);
					if (cce == null)
						continue;
					var configId = ToSlnConfigurationId (cc);
					var itemConfigId = ToSlnConfigurationId (cce.ItemConfiguration);

					pset.SetValue (configId + ".ActiveCfg", itemConfigId);

					if (cce.Build)
						pset.SetValue (configId + ".Build.0", itemConfigId);
				
					if (cce.Deploy)
						pset.SetValue (configId + ".Deploy.0", itemConfigId);
				}
			}
		}

		void WriteNestedProjects (SolutionFolder folder, SolutionFolder root, SlnSection sec)
		{
			foreach (SolutionFolderItem ce in folder.Items)
				sec.Properties.SetValue (ce.ItemId, folder.ItemId);
		}
		
		List<string> ReadSolutionItemDependencies (SlnProject proj)
		{
			// Find a project section of type ProjectDependencies
			var sec = proj.Sections.GetSection ("ProjectDependencies");
			if (sec == null)
				return null;

			return sec.Properties.Keys.ToList ();
		}

		IEnumerable<string> ReadFolderFiles (SlnProject proj)
		{
			// Find a solution item section of type SolutionItems
			var sec = proj.Sections.GetSection ("SolutionItems");
			if (sec == null)
				return new string[0];

			return sec.Properties.Keys.ToList ();
		}

		void DeserializeSolutionItem (Solution sln, SolutionFolderItem item, SlnProject proj)
		{
			// Deserialize the object
			var sec = proj.Sections.GetSection ("MonoDevelopProperties");
			if (sec == null)
				return;

			DataItem it = ReadDataItem (sec);
			if (it == null)
				return;
			
			MSBuildSerializer ser = new MSBuildSerializer (sln.FileName);
			ser.SerializationContext.BaseFile = sln.FileName;
			ser.Deserialize (item, it);
		}
		
		void WriteDataItem (SlnPropertySet pset, DataItem item)
		{
			pset.Clear ();
			int id = 0;
			foreach (DataNode val in item.ItemData)
				WriteDataNode (pset, "", val, ref id);
		}
		
		void WriteDataNode (SlnPropertySet pset, string prefix, DataNode node, ref int id)
		{
			string name = node.Name;
			string newPrefix = prefix.Length > 0 ? prefix + "." + name: name;
			
			if (node is DataValue) {
				DataValue val = (DataValue) node;
				string value = EncodeString (val.Value);
				pset.SetValue (newPrefix, value);
			}
			else {
				DataItem it = (DataItem) node;
				pset.SetValue (newPrefix, "$" + id);
				newPrefix = "$" + id;
				id ++;
				foreach (DataNode cn in it.ItemData)
					WriteDataNode (pset, newPrefix, cn, ref id);
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
		
		DataItem ReadDataItem (SlnSection sec)
		{
			DataItem it = new DataItem ();

			var lines = sec.Properties.ToArray ();

			int lineNum = 0;
			int lastLine = lines.Length - 1;
			while (lineNum <= lastLine) {
				if (!ReadDataNode (it, lines, lastLine, "", ref lineNum))
					lineNum++;
			}
			return it;
		}
		
		bool ReadDataNode (DataItem item, KeyValuePair<string,string>[] lines, int lastLine, string prefix, ref int lineNum)
		{
			var s = lines [lineNum];
			
			// Check if the line belongs to the current item
			if (prefix.Length > 0) {
				if (!s.Key.StartsWith (prefix + "."))
					return false;
			} else {
				if (s.Key.StartsWith ("$"))
					return false;
			}
			
			string name = s.Key;
			if (name.Length == 0) {
				lineNum++;
				return true;
			}
			
			string value = s.Value;
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
		public async Task<object> ReadFile (string fileName, ProgressMonitor monitor)
		{
			if (fileName == null || monitor == null)
				return null;

			Solution sol;
			try {
				ProjectExtensionUtil.BeginLoadOperation ();
				sol = new Solution ();
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution: {0}"), fileName), 1);
				var projectLoadMonitor = monitor as ProjectLoadProgressMonitor;
				if (projectLoadMonitor != null)
					projectLoadMonitor.CurrentSolution = sol;
				await Task.Factory.StartNew (() => LoadSolution (sol, fileName, monitor));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load solution: {0}", fileName), ex);
				throw;
			} finally {
				ProjectExtensionUtil.EndLoadOperation ();
				monitor.EndTask ();
			}
			return sol;
		}

		SolutionFolder LoadSolution (Solution sol, string fileName, ProgressMonitor monitor)
		{
			FileFormat projectFormat = Services.ProjectService.FileFormats.GetFileFormat (format);

			monitor.BeginTask (GettextCatalog.GetString ("Loading solution: {0}", fileName), 1);

			var sln = new SlnFile ();
			sln.Read (fileName);

			sol.FileName = fileName;
			sol.ConvertToFormat (projectFormat, false);

			sol.ReadSolution (monitor, sln);
			return sol.RootFolder;
		}

		internal void LoadSolution (Solution sol, SlnFile sln, ProgressMonitor monitor)
		{
			var version = sln.FormatVersion;

			//Parse the .sln file
			var folder = sol.RootFolder;
			sol.Version = "0.1"; //FIXME:

			monitor.BeginTask("Loading projects ..", sln.Projects.Count + 1);
			Dictionary<string, SolutionFolderItem> items = new Dictionary<string, SolutionFolderItem> ();
			List<SolutionFolderItem> sortedList = new List<SolutionFolderItem> ();
			foreach (SlnProject sec in sln.Projects) {
				monitor.Step (1);
				try {
					// Valid guid?
					new Guid (sec.TypeGuid);
				} catch (FormatException) {
					//Use default guid as projectGuid
					LoggingService.LogDebug (GettextCatalog.GetString (
						"Invalid Project type guid '{0}' on line #{1}. Ignoring.",
						sec.Id,
						sec.Line));
					continue;
				}

				string projTypeGuid = sec.TypeGuid.ToUpper ();
				string projectName = sec.Name;
				string projectPath = sec.FilePath;
				string projectGuid = sec.Id;

				if (projTypeGuid == MSBuildProjectService.FolderTypeGuid) {
					//Solution folder
					SolutionFolder sfolder = new SolutionFolder ();
					sfolder.Name = projectName;
					sfolder.ItemId = projectGuid;

					DeserializeSolutionItem (sol, sfolder, sec);
					
					foreach (string f in ReadFolderFiles (sec))
						sfolder.Files.Add (MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (sol.FileName), f));
					
					items.Add (projectGuid, sfolder);
					sortedList.Add (sfolder);
					
					continue;
				}

				if (projectPath.StartsWith("http://")) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"{0}({1}): Projects with non-local source (http://...) not supported. '{2}'.",
						sol.FileName, sec.Line, projectPath));
					continue;
				}

				string path = MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (sol.FileName), projectPath);
				if (String.IsNullOrEmpty (path)) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", sol.FileName, projectPath));
					LoggingService.LogWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", sol.FileName, projectPath));
					continue;
				}

				projectPath = Path.GetFullPath (path);
				
				SolutionItem item = null;
				
				try {
					if (sol.IsSolutionItemEnabled (projectPath)) {
						var t = ProjectExtensionUtil.LoadSolutionItem (monitor, projectPath, delegate {
							return MSBuildProjectService.LoadItem (monitor, projectPath, format, projTypeGuid, projectGuid);
						});
						t.Wait ();
						item = t.Result;
						
						if (item == null) {
							throw new UnknownSolutionItemTypeException (projTypeGuid);
						}
					} else {
						item = new UnloadedSolutionItem () {
							FileName = projectPath
						};
					}
					
				} catch (Exception e) {
					// If we get a TargetInvocationException from using Activator.CreateInstance we
					// need to unwrap the real exception
					while (e is TargetInvocationException)
						e = ((TargetInvocationException) e).InnerException;
					
					bool loadAsProject = false;

					if (e is UnknownSolutionItemTypeException) {
						var name = ((UnknownSolutionItemTypeException)e).TypeName;

						var relPath = new FilePath (path).ToRelative (sol.BaseDirectory);
						if (!string.IsNullOrEmpty (name)) {
							var guids = name.Split (';');
							var projectInfo = MSBuildProjectService.GetUnknownProjectTypeInfo (guids, sol.FileName);
							if (projectInfo != null) {
								loadAsProject = projectInfo.LoadFiles;
								LoggingService.LogWarning (string.Format ("Could not load {0} project '{1}'. {2}", projectInfo.Name, relPath, projectInfo.GetInstructions ()));
								monitor.ReportWarning (GettextCatalog.GetString ("Could not load {0} project '{1}'. {2}", projectInfo.Name, relPath, projectInfo.GetInstructions ()));
							} else {
								LoggingService.LogWarning (string.Format ("Could not load project '{0}' with unknown item type '{1}'", relPath, name));
								monitor.ReportWarning (GettextCatalog.GetString ("Could not load project '{0}' with unknown item type '{1}'", relPath, name));
							}
						} else {
							LoggingService.LogWarning (string.Format ("Could not load project '{0}' with unknown item type", relPath));
							monitor.ReportWarning (GettextCatalog.GetString ("Could not load project '{0}' with unknown item type", relPath));
						}

					} else if (e is UserException) {
						var ex = (UserException) e;
						LoggingService.LogError ("{0}: {1}", ex.Message, ex.Details);
						monitor.ReportError (string.Format ("{0}{1}{1}{2}", ex.Message, Environment.NewLine, ex.Details), null);
					} else {
						LoggingService.LogError (string.Format ("Error while trying to load the project {0}", projectPath), e);
						monitor.ReportWarning (GettextCatalog.GetString (
							"Error while trying to load the project '{0}': {1}", projectPath, e.Message));
					}

					SolutionItem uitem;
					if (loadAsProject) {
						uitem = new UnknownProject () {
							FileName = projectPath,
							UnsupportedProjectMessage = e.Message,
						};
					} else {
						uitem = new UnknownSolutionItem () {
							FileName = projectPath,
							UnsupportedProjectMessage = e.Message,
						};
					}
					item = uitem;
				}

				item.UnresolvedProjectDependencies = ReadSolutionItemDependencies (sec);

				// Deserialize the object
				var ssec = sec.Sections.GetSection ("MonoDevelopProperties");
				if (ssec != null) {
					DataItem it = ReadDataItem (ssec);
					item.ReadSlnData (it);
				}

				if (!items.ContainsKey (projectGuid)) {
					items.Add (projectGuid, item);
					sortedList.Add (item);
				} else {
					monitor.ReportError (GettextCatalog.GetString ("Invalid solution file. There are two projects with the same GUID. The project {0} will be ignored.", projectPath), null);
				}
			}
			monitor.EndTask ();

			sol.LoadedProjects = new HashSet<string> (items.Keys);

			var nested = sln.Sections.GetSection ("NestedProjects");
			if (nested != null)
				LoadNestedProjects (nested, items, monitor);

			// Resolve project dependencies
			foreach (var it in items.Values.OfType<SolutionItem> ()) {
				if (it.UnresolvedProjectDependencies != null) {
					foreach (var id in it.UnresolvedProjectDependencies.ToArray ()) {
						SolutionFolderItem dep;
						if (items.TryGetValue (id, out dep) && dep is SolutionItem) {
							it.UnresolvedProjectDependencies.Remove (id);
							it.ItemDependencies.Add ((SolutionItem)dep);
						}
					}
					if (it.UnresolvedProjectDependencies.Count == 0)
						it.UnresolvedProjectDependencies = null;
				}
			}

			//Add top level folders and projects to the main folder
			foreach (SolutionFolderItem ce in sortedList) {
				if (ce.ParentFolder == null)
					folder.Items.Add (ce);
			}

			//FIXME: This can be just SolutionConfiguration also!
			LoadSolutionConfigurations (sln.SolutionConfigurationsSection, sol, monitor);

			LoadProjectConfigurationMappings (sln.ProjectConfigurationsSection, sol, items, monitor);

			LoadMonoDevelopProperties (sln.Sections.GetSection ("MonoDevelopProperties"), sol, monitor);

			foreach (var e in sln.Sections) {
				string name = e.Id;
				if (name.StartsWith ("MonoDevelopProperties.")) {
					int i = name.IndexOf ('.');
					LoadMonoDevelopConfigurationProperties (name.Substring (i+1), e, sol, monitor);
				}
			}

			monitor.EndTask ();
		}

		void LoadProjectConfigurationMappings (SlnPropertySetCollection sets, Solution sln, Dictionary<string, SolutionFolderItem> items, ProgressMonitor monitor)
		{
			if (sets == null)
				return;

			Dictionary<string, SolutionConfigurationEntry> cache = new Dictionary<string, SolutionConfigurationEntry> ();
			Dictionary<string, string> ignoredProjects = new Dictionary<string, string> ();

			foreach (var pset in sets) {

				var projGuid = pset.Id;

				if (!items.ContainsKey (projGuid)) {
					if (ignoredProjects.ContainsKey (projGuid))
						// already warned
						continue;

					LoggingService.LogWarning (GettextCatalog.GetString ("{0} ({1}) : Project with guid = '{2}' not found or not loaded. Ignoring", 
						sln.FileName, pset.Line + 1, projGuid));
					ignoredProjects [projGuid] = projGuid;
					continue;
				}

				SolutionFolderItem it;
				if (!items.TryGetValue (projGuid, out it))
					continue;

				SolutionItem item = it as SolutionItem;

				if (item == null || !item.SupportsConfigurations ())
					continue;

				//Format:
				// {projectGuid}.SolutionConfigName|SolutionPlatform.ActiveCfg = ProjConfigName|ProjPlatform
				// {projectGuid}.SolutionConfigName|SolutionPlatform.Build.0 = ProjConfigName|ProjPlatform
				// {projectGuid}.SolutionConfigName|SolutionPlatform.Deploy.0 = ProjConfigName|ProjPlatform

				foreach (var prop in pset) {
					string action;
					string projConfig = prop.Value;

					string left = prop.Key;
					if (left.EndsWith (".ActiveCfg")) {
						action = "ActiveCfg";
						left = left.Substring (0, left.Length - 10);
					} else if (left.EndsWith (".Build.0")) {
						action = "Build.0";
						left = left.Substring (0, left.Length - 8);
					} else if (left.EndsWith (".Deploy.0")) {
						action = "Deploy.0";
						left = left.Substring (0, left.Length - 9);
					} else { 
						LoggingService.LogWarning (GettextCatalog.GetString ("{0} ({1}) : Unknown action. Only ActiveCfg, Build.0 and Deploy.0 supported.",
							sln.FileName, pset.Line));
						continue;
					}

					string slnConfig = left;

					string key = projGuid + "." + slnConfig;
					SolutionConfigurationEntry combineConfigEntry = null;
					if (cache.ContainsKey (key)) {
						combineConfigEntry = cache [key];
					} else {
						combineConfigEntry = GetConfigEntry (sln, item, slnConfig);
						combineConfigEntry.Build = false; // Not buildable by default. Build will be enabled if a Build.0 entry is found
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
					} else if (action == "Build.0") {
						combineConfigEntry.Build = true;
					} else if (action == "Deploy.0") {
						combineConfigEntry.Deploy = true;
					}
				}
			}
		}

		/* Gets the CombineConfigurationEntry corresponding to the @entry in its parentCombine's 
		 * CombineConfiguration. Creates the required bits if not present */
		SolutionConfigurationEntry GetConfigEntry (Solution sol, SolutionItem item, string configName)
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

		void LoadSolutionConfigurations (SlnPropertySet sec, Solution solution, ProgressMonitor monitor)
		{
			if (sec == null)
				return;

			foreach (var pair in sec) {

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

		void LoadMonoDevelopProperties (SlnSection sec, Solution sln, ProgressMonitor monitor)
		{
			if (sec == null)
				return;
			DataItem it = ReadDataItem (sec);
			MSBuildSerializer ser = new MSBuildSerializer (sln.FileName);
			ser.SerializationContext.BaseFile = sln.FileName;
			ser.Deserialize (sln, it);
		}
		
		void LoadMonoDevelopConfigurationProperties (string configName, SlnSection sec, Solution sln, ProgressMonitor monitor)
		{
			SolutionConfiguration config = sln.Configurations [configName];
			if (config == null)
				return;
			DataItem it = ReadDataItem (sec);
			MSBuildSerializer ser = new MSBuildSerializer (sln.FileName);
			ser.Deserialize (config, it);
		}
		
		void LoadNestedProjects (SlnSection sec, IDictionary<string, SolutionFolderItem> entries, ProgressMonitor monitor)
		{
			if (sec == null || String.Compare (sec.SectionType, "preSolution", StringComparison.OrdinalIgnoreCase) != 0)
				return;

			foreach (var kvp in sec.Properties) {
				// Guids should be upper case for VS compatibility
				var pair = new KeyValuePair<string, string> (kvp.Key.ToUpper (), kvp.Value.ToUpper ());

				SolutionFolderItem folderItem;
				SolutionFolderItem item;
				
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
