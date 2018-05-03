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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.MSBuild
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
				string version = SlnFile.GetFileVersion (file);
				return format.SupportsSlnVersion (version);
			}
			return false;
		}
		
		public bool CanWriteFile (object obj, MSBuildFileFormat format)
		{
			return obj is Solution;
		}
		
		public Task WriteFile (string file, object obj, bool saveProjects, ProgressMonitor monitor)
		{
			return Task.Run (async delegate {
				Solution sol = (Solution)obj;

				try {
					monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", file), 1);
					await WriteFileInternal (file, file, sol, saveProjects, monitor).ConfigureAwait (false);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Could not save solution: {0}", file), ex);
					LoggingService.LogError (GettextCatalog.GetString ("Could not save solution: {0}", file), ex);
					throw;
				} finally {
					monitor.EndTask ();
				}
			});
		}

		async Task WriteFileInternal (string file, string sourceFile, Solution solution, bool saveProjects, ProgressMonitor monitor)
		{
			if (saveProjects) {
				var items = solution.GetAllSolutionItems ().ToArray ();
				monitor.BeginTask (items.Length + 1);
				foreach (var item in items) {
					try {
						monitor.BeginStep ();
						item.SavingSolution = true;
						await item.SaveAsync (monitor);
					} finally {
						item.SavingSolution = false;
					}
				}
			} else {
				monitor.BeginTask (1);
				monitor.BeginStep ();
			}

			SlnFile sln = new SlnFile ();
			sln.FileName = file;
			if (File.Exists (sourceFile)) {
				try {
					sln.Read (sourceFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Existing solution can't be updated since it can't be read", ex);
				}
			}

			sln.FormatVersion = format.SlnVersion;

			// Don't modify the product description comment if it already has a value
			if (string.IsNullOrEmpty (sln.ProductDescription))
				sln.ProductDescription = format.ProductDescriptionComment;

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
				var sec = sln.Sections.GetSection ("NestedProjects", SlnSectionType.PreProcess);
				if (sec == null) {
					sec = sln.Sections.GetOrCreateSection ("NestedProjects", SlnSectionType.PreProcess);
					sec.SkipIfEmpty = true; // don't write the section if there are no nested projects after all
				}
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

			// Write custom properties for configurations
			foreach (SolutionConfiguration conf in solution.Configurations) {
				string secId = "MonoDevelopProperties." + conf.Id;
				var sec = sln.Sections.GetOrCreateSection (secId, SlnSectionType.PreProcess);
				solution.WriteConfigurationData (monitor, sec.Properties, conf);
				if (sec.IsEmpty)
					sln.Sections.Remove (sec);
			}
		}

		void WriteProjects (SolutionFolder folder, SlnFile sln, ProgressMonitor monitor, HashSet<string> unknownProjects)
		{
			monitor.BeginTask (folder.Items.Count); 
			foreach (SolutionFolderItem ce in folder.Items.ToArray ())
			{
				monitor.BeginStep ();
				if (ce is SolutionItem) {
					
					SolutionItem item = (SolutionItem) ce;

					var proj = sln.Projects.GetOrCreateProject (ce.ItemId);
					proj.TypeGuid = item.TypeGuid;
					proj.Name = item.Name;
					proj.FilePath = FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (sln.BaseDirectory, item.FileName)).Replace ('/', '\\');

					var sec = proj.Sections.GetOrCreateSection ("MonoDevelopProperties", SlnSectionType.PreProcess);
					sec.SkipIfEmpty = true;
					folder.ParentSolution.WriteSolutionFolderItemData (monitor, sec.Properties, ce);

					if (item.ItemDependencies.Count > 0) {
						sec = proj.Sections.GetOrCreateSection ("ProjectDependencies", SlnSectionType.PostProcess);
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
					var sec = proj.Sections.GetOrCreateSection ("MonoDevelopProperties", SlnSectionType.PreProcess);
					sec.SkipIfEmpty = true;
					folder.ParentSolution.WriteSolutionFolderItemData (monitor, sec.Properties, ce);
				}
				if (ce is SolutionFolder)
					WriteProjects (ce as SolutionFolder, sln, monitor, unknownProjects);
			}
			monitor.EndTask ();
		}
		
		void WriteFolderFiles (SlnProject proj, SolutionFolder folder)
		{
			if (folder.Files.Count > 0) {
				var sec = proj.Sections.GetOrCreateSection ("SolutionItems", SlnSectionType.PreProcess);
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
				if (!itemGuid.StartsWith ("{", StringComparison.Ordinal) && !itemGuid.EndsWith ("}", StringComparison.Ordinal))
					itemGuid = "{" + itemGuid + "}";

				var pset = col.GetOrCreatePropertySet (itemGuid, ignoreCase:true);
				var existingKeys = new HashSet<string> (pset.Keys);

				foreach (SolutionConfiguration cc in sol.Configurations) {
					var cce = cc.GetEntryForItem (item);
					if (cce == null)
						continue;
					var configId = ToSlnConfigurationId (cc);
					var itemConfigId = ToSlnConfigurationId (cce.ItemConfiguration);

					string key;
					pset.SetValue (key = configId + ".ActiveCfg", itemConfigId);
					existingKeys.Remove (key);

					if (cce.Build) {
						pset.SetValue (key = configId + ".Build.0", itemConfigId);
						existingKeys.Remove (key);
					}
				
					if (cce.Deploy) {
						pset.SetValue (key = configId + ".Deploy.0", itemConfigId);
						existingKeys.Remove (key);
					}
				}
				foreach (var k in existingKeys)
					pset.Remove (k);
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

		void DeserializeSolutionItem (ProgressMonitor monitor, Solution sln, SolutionFolderItem item, SlnProject proj)
		{
			// Deserialize the object
			var sec = proj.Sections.GetSection ("MonoDevelopProperties");
			if (sec == null)
				return;

			sln.ReadSolutionFolderItemData (monitor, sec.Properties, item);
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

			var sol = new Solution (true);
			sol.FileName = fileName;
			sol.FileFormat = format;

			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution: {0}"), fileName), 1);
				monitor.BeginStep ();
				await sol.OnBeginLoad ();
				var projectLoadMonitor = monitor as ProjectLoadProgressMonitor;
				if (projectLoadMonitor != null)
					projectLoadMonitor.CurrentSolution = sol;
				await Task.Factory.StartNew (() => {
					sol.ReadSolution (monitor);
				});
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load solution: {0}", fileName), ex);
				await sol.OnEndLoad ();
				sol.NotifyItemReady ();
				monitor.EndTask ();
				throw;
			}
			await sol.OnEndLoad ();
			sol.NotifyItemReady ();
			monitor.EndTask ();
			return sol;
		}

		internal void LoadSolution (Solution sol, SlnFile sln, ProgressMonitor monitor, SolutionLoadContext ctx)
		{
			var version = sln.FormatVersion;

			//Parse the .sln file
			var folder = sol.RootFolder;
			sol.Version = "0.1"; //FIXME:

			monitor.BeginTask("Loading projects ..", sln.Projects.Count + 1);
			Dictionary<string, SolutionFolderItem> items = new Dictionary<string, SolutionFolderItem> ();
			List<string> sortedList = new List<string> ();

			List<Task> loadTasks = new List<Task> ();
			var solDirectory = Path.GetDirectoryName (sol.FileName);

			foreach (SlnProject sec in sln.Projects) {
				// Valid guid?
				if (!Guid.TryParse(sec.TypeGuid, out _)) {
					monitor.Step (1);
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

				lock (items)
					sortedList.Add (projectGuid);

				if (projTypeGuid == MSBuildProjectService.FolderTypeGuid) {
					//Solution folder
					SolutionFolder sfolder = new SolutionFolder ();
					sfolder.Name = projectName;
					sfolder.ItemId = projectGuid;

					DeserializeSolutionItem (monitor, sol, sfolder, sec);
					
					foreach (string f in ReadFolderFiles (sec))
						sfolder.Files.Add (MSBuildProjectService.FromMSBuildPath (solDirectory, f));

					lock (items)
						items.Add (projectGuid, sfolder);

					monitor.Step (1);
					continue;
				}

				if (projectPath.StartsWith("http://", StringComparison.Ordinal)) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"{0}({1}): Projects with non-local source (http://...) not supported. '{2}'.",
						sol.FileName, sec.Line, projectPath));
					monitor.Step (1);
					continue;
				}

				string path = MSBuildProjectService.FromMSBuildPath (solDirectory, projectPath);
				if (String.IsNullOrEmpty (path)) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", sol.FileName, projectPath));
					LoggingService.LogWarning (GettextCatalog.GetString (
						"Invalid project path found in {0} : {1}", sol.FileName, projectPath));
					monitor.Step (1);
					continue;
				}

				projectPath = Path.GetFullPath (path);
				
				SolutionItem item = null;
				Task<SolutionItem> loadTask;
				DateTime ti = DateTime.Now;

				if (sol.IsSolutionItemEnabled (projectPath)) {
					loadTask = Services.ProjectService.ReadSolutionItem (monitor, projectPath, format, projTypeGuid, projectGuid, ctx);
				} else {
					loadTask = Task.FromResult<SolutionItem> (new UnloadedSolutionItem () {
						FileName = projectPath
					});
				}

				var ft = loadTask.ContinueWith (ta => {
					try {
						item = ta.Result;
						if (item == null)
							throw new UnknownSolutionItemTypeException (projTypeGuid);
					} catch (Exception cex) {
						var e = UnwrapException (cex).First ();

						string unsupportedMessage = e.Message;

						if (e is UserException) {
							var ex = (UserException) e;
							LoggingService.LogError ("{0}: {1}", ex.Message, ex.Details);
							monitor.ReportError (string.Format ("{0}{1}{1}{2}", ex.Message, Environment.NewLine, ex.Details), null);
						} else {
							LoggingService.LogError (string.Format ("Error while trying to load the project {0}", projectPath), e);
							monitor.ReportWarning (GettextCatalog.GetString (
								"Error while trying to load the project '{0}': {1}", projectPath, e.Message));
						}

						SolutionItem uitem;
						uitem = new UnknownSolutionItem () {
							FileName = projectPath,
							LoadError = unsupportedMessage,
						};
						item = uitem;
						item.ItemId = projectGuid;
						item.TypeGuid = projTypeGuid;
					}

					item.UnresolvedProjectDependencies = ReadSolutionItemDependencies (sec);

					// Deserialize the object
					DeserializeSolutionItem (monitor, sol, item, sec);

					lock (items) {
						if (!items.ContainsKey (projectGuid)) {
							items.Add (projectGuid, item);
						} else {
							monitor.ReportError (GettextCatalog.GetString ("Invalid solution file. There are two projects with the same GUID. The project {0} will be ignored.", projectPath), null);
						}
					}
					monitor.Step (1);
				});
				loadTasks.Add (ft);

				// Limit the number of concurrent tasks. Por solutions with many projects, spawning one thread per
				// project makes the whole load process slower.
				loadTasks.RemoveAll (t => t.IsCompleted);
				if (loadTasks.Count > 4)
					Task.WaitAny (loadTasks.ToArray ());
			}

			Task.WaitAll (loadTasks.ToArray ());

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
			foreach (string id in sortedList) {
				SolutionFolderItem ce;
				if (items.TryGetValue (id, out ce) && ce.ParentFolder == null)
					folder.Items.Add (ce);
			}

			//FIXME: This can be just SolutionConfiguration also!
			LoadSolutionConfigurations (sln.SolutionConfigurationsSection, sol, monitor);

			LoadProjectConfigurationMappings (sln.ProjectConfigurationsSection, sol, items, monitor);

			foreach (var e in sln.Sections) {
				string name = e.Id;
				if (name.StartsWith ("MonoDevelopProperties.", StringComparison.Ordinal)) {
					int i = name.IndexOf ('.');
					LoadMonoDevelopConfigurationProperties (name.Substring (i+1), e, sol, monitor);
				}
			}

			monitor.EndTask ();
		}

		IEnumerable<Exception> UnwrapException (Exception ex)
		{
			var a = ex as AggregateException;
			if (a != null) {
				foreach (var e in a.InnerExceptions) {
					foreach (var u in UnwrapException (e))
						yield return u;
				}
			} else if (ex is TargetInvocationException) {
				// If we get a TargetInvocationException from using Activator.CreateInstance we
				// need to unwrap the real exception
				ex = ex.InnerException;
				foreach (var e in UnwrapException (ex))
					yield return e;
			}
			else
				yield return ex;
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
					if (left.EndsWith (".ActiveCfg", StringComparison.Ordinal)) {
						action = "ActiveCfg";
						left = left.Substring (0, left.Length - 10);
					} else if (left.EndsWith (".Build.0", StringComparison.Ordinal)) {
						action = "Build.0";
						left = left.Substring (0, left.Length - 8);
					} else if (left.EndsWith (".Deploy.0", StringComparison.Ordinal)) {
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

		void LoadMonoDevelopConfigurationProperties (string configName, SlnSection sec, Solution sln, ProgressMonitor monitor)
		{
			SolutionConfiguration config = sln.Configurations [configName];
			if (config == null)
				return;
			sln.ReadConfigurationData (monitor, sec.Properties, config);
		}
		
		void LoadNestedProjects (SlnSection sec, IDictionary<string, SolutionFolderItem> entries, ProgressMonitor monitor)
		{
			if (sec == null || sec.SectionType != SlnSectionType.PreProcess)
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
	}
}
