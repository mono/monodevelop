// MSBuildProjectHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Formats.MD1;
using MonoDevelop.Projects.Extensions;
using Mono.Addins;
using System.Linq;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProjectHandler: MSBuildHandler, IResourceHandler, IPathHandler, IAssemblyReferenceHandler
	{
		List<string> targetImports = new List<string> ();
		IResourceHandler customResourceHandler;
		List<string> subtypeGuids = new List<string> ();
		const string Unspecified = null;
		RemoteProjectBuilder projectBuilder;
		ITimeTracker timer;
		bool modifiedInMemory;
		UnknownProjectTypeNode unknownProjectTypeInfo;
		
		string lastBuildToolsVersion;
		string lastBuildRuntime;
		string lastFileName;
		string lastSlnFileName;

		struct ItemInfo {
			public MSBuildItem Item;
			public bool Added;
		}
		
		protected SolutionEntityItem EntityItem {
			get { return (SolutionEntityItem) Item; }
		}

		public string ToolsVersion { get; private set; }
		string productVersion;
		string schemaVersion;

		internal bool ProjectTypeIsUnsupported {
			get { return unknownProjectTypeInfo != null; }
		}

		internal UnknownProjectTypeNode UnknownProjectTypeInfo {
			get { return unknownProjectTypeInfo; }
		}

		public List<string> TargetImports {
			get { return targetImports; }
		}

		internal void SetUnsupportedType (UnknownProjectTypeNode typeInfo)
		{
			unknownProjectTypeInfo = typeInfo;
		}

		internal override void SetSolutionFormat (MSBuildFileFormat format, bool converting)
		{
			// when converting formats, set ToolsVersion, ProductVersion, SchemaVersion to default values written by VS 
			// this happens on creation too
			// else we leave them alone and just roundtrip them
			if (converting) {
				ToolsVersion = format.DefaultToolsVersion;
				productVersion = format.DefaultProductVersion;
				schemaVersion = format.DefaultSchemaVersion;
			}

			base.SetSolutionFormat (format, converting);
		}

		//HACK: the solution's format is irrelevant to MSBuild projects, what matters is the ToolsVersion
		// but other parts of the MD API expect a FileFormat
		MSBuildFileFormat GetToolsFormat ()
		{
			switch (ToolsVersion) {
			case "2.0":
				return new MSBuildFileFormatVS05 ();
			case "3.5":
				return new MSBuildFileFormatVS08 ();
			case "4.0":
				if (SolutionFormat != null && SolutionFormat.Id == "MSBuild10")
					return SolutionFormat;
				return new MSBuildFileFormatVS12 ();
			case "12.0":
				return new MSBuildFileFormatVS12 ();
			default:
				throw new Exception ("Unknown ToolsVersion '" + ToolsVersion + "'");
			}
		}

		public void SetCustomResourceHandler (IResourceHandler value)
		{
			customResourceHandler = value;
		}

		public List<string> SubtypeGuids {
			get {
				return subtypeGuids;
			}
		}

		public MSBuildProjectHandler ()
		{
		}
		
		public MSBuildProjectHandler (string typeGuid, string import, string itemId)
		{
			Initialize (typeGuid, import, itemId);
		}
		
		internal void Initialize (string typeGuid, string import, string itemId)
		{
			base.Initialize (typeGuid, itemId);
			if (import != null && import.Trim().Length > 0)
				this.targetImports.AddRange (import.Split (':'));
			
			Runtime.SystemAssemblyService.DefaultRuntimeChanged += OnDefaultRuntimeChanged;
		}

		public override object GetService (Type t)
		{
			foreach (var ex in GetMSBuildExtensions ()) {
				var s = ex.GetService (t);
				if (s != null)
					return s;
			}
			return null;
		}
		
		void OnDefaultRuntimeChanged (object o, EventArgs args)
		{
			// If the default runtime changes, the project builder for this project may change
			// so it has to be created again.
			CleanupProjectBuilder ();
		}

		object builderLock = new object ();
		
		RemoteProjectBuilder GetProjectBuilder ()
		{
			var item = (SolutionEntityItem) Item;

			//FIXME: we can't really have per-project runtimes, has to be per-solution
			TargetRuntime runtime = null;
			var ap = item as IAssemblyProject;
			runtime = ap != null ? ap.TargetRuntime : Runtime.SystemAssemblyService.CurrentRuntime;

			var sln = item.ParentSolution;
			var slnFile = sln != null ? sln.FileName : null;

			lock (builderLock) {
				if (projectBuilder == null || lastBuildToolsVersion != ToolsVersion || lastBuildRuntime != runtime.Id || lastFileName != item.FileName || lastSlnFileName != slnFile) {
					CleanupProjectBuilder ();
					projectBuilder = MSBuildProjectService.GetProjectBuilder (runtime, ToolsVersion, item.FileName, slnFile);
					projectBuilder.Disconnected += delegate {
						CleanupProjectBuilder ();
					};
					lastBuildToolsVersion = ToolsVersion;
					lastBuildRuntime = runtime.Id;
					lastFileName = item.FileName;
					lastSlnFileName = slnFile;
				}
				if (modifiedInMemory) {
					modifiedInMemory = false;
					var p = SaveProject (new NullProgressMonitor ());
					projectBuilder.RefreshWithContent (p.SaveToString ());
				}
			}
			return projectBuilder;
		}

		internal void CleanupProjectBuilder ()
		{
			if (projectBuilder != null) {
				projectBuilder.Dispose ();
				projectBuilder = null;
			}
		}

		internal void RefreshProjectBuilder ()
		{
			if (projectBuilder != null) {
				projectBuilder.Refresh ();
			}
		}

		public override void Dispose ()
		{
			base.Dispose ();
			CleanupProjectBuilder ();
			Runtime.SystemAssemblyService.DefaultRuntimeChanged -= OnDefaultRuntimeChanged;
		}
		
		//for some reason, MD internally handles "AnyCPU" as "", but we need to be explicit when
		//passing it to the build engine
		static string GetExplicitPlatform (SolutionItemConfiguration configObject)
		{
			if (string.IsNullOrEmpty (configObject.Platform)) {
				return "AnyCPU";
			}
			return configObject.Platform;
		}

		ProjectConfigurationInfo[] GetConfigurations (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			// Returns a list of project/configuration information for the provided item and all its references
			List<ProjectConfigurationInfo> configs = new List<ProjectConfigurationInfo> ();
			var c = item.GetConfiguration (configuration);
			configs.Add (new ProjectConfigurationInfo () {
				ProjectFile = item.FileName,
				Configuration = c.Name,
				Platform = GetExplicitPlatform (c),
				ProjectGuid = ((MSBuildProjectHandler)item.ItemHandler).ItemId
			});
			foreach (var refProject in item.GetReferencedItems (configuration).OfType<Project> ()) {
				var refConfig = refProject.GetConfiguration (configuration);
				if (refConfig != null) {
					configs.Add (new ProjectConfigurationInfo () {
						ProjectFile = refProject.FileName,
						Configuration = refConfig.Name,
						Platform = GetExplicitPlatform (refConfig),
						ProjectGuid = ((MSBuildProjectHandler)refProject.ItemHandler).ItemId
					});
				}
			}
			return configs.ToArray ();
		}
		
		IEnumerable<string> IAssemblyReferenceHandler.GetAssemblyReferences (ConfigurationSelector configuration)
		{
			if (UseMSBuildEngineForItem (Item, configuration)) {
				// Get the references list from the msbuild project
				SolutionEntityItem item = (SolutionEntityItem) Item;
				RemoteProjectBuilder builder = GetProjectBuilder ();
				var configs = GetConfigurations (item, configuration);

				string[] refs;
				using (Counters.ResolveMSBuildReferencesTimer.BeginTiming (Item.GetProjectEventMetadata ()))
					refs = builder.ResolveAssemblyReferences (configs);
				foreach (var r in refs)
					yield return r;
			}
			else {
				CleanupProjectBuilder ();
				DotNetProject item = Item as DotNetProject;
				if (item == null)
					yield break;
				foreach (ProjectReference pref in item.References.Where (pr => pr.ReferenceType != ReferenceType.Project)) {
					foreach (string asm in pref.GetReferencedFileNames (configuration))
						yield return asm;
				}
			}
		}
		
		public override BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (UseMSBuildEngineForItem (Item, configuration)) {
				SolutionEntityItem item = Item as SolutionEntityItem;
				if (item != null) {
					LogWriter logWriter = new LogWriter (monitor.Log);
					RemoteProjectBuilder builder = GetProjectBuilder ();
					var configs = GetConfigurations (item, configuration);

					TimerCounter buildTimer = null;
					switch (target) {
					case "Build": buildTimer = Counters.BuildMSBuildProjectTimer; break;
					case "Clean": buildTimer = Counters.CleanMSBuildProjectTimer; break;
					}

					var t1 = Counters.RunMSBuildTargetTimer.BeginTiming (Item.GetProjectEventMetadata ());
					var t2 = buildTimer != null ? buildTimer.BeginTiming (Item.GetProjectEventMetadata ()) : null;

					MSBuildResult result;

					try {
						result = builder.Run (configs, logWriter, MSBuildProjectService.DefaultMSBuildVerbosity, new[] { target }, null, null);
					} finally {
						t1.End ();
						if (t2 != null)
							t2.End ();
					}

					System.Runtime.Remoting.RemotingServices.Disconnect (logWriter);
					
					var br = new BuildResult ();
					foreach (var err in result.Errors) {
						FilePath file = null;
						if (err.File != null)
							file = Path.Combine (Path.GetDirectoryName (err.ProjectFile), err.File);

						br.Append (new BuildError (file, err.LineNumber, err.ColumnNumber, err.Code, err.Message) {
							Subcategory = err.Subcategory,
							EndLine = err.EndLineNumber,
							EndColumn = err.EndColumnNumber,
							IsWarning = err.IsWarning
						});
					}
					return br;
				}
			}
			else {
				CleanupProjectBuilder ();
				if (Item is DotNetProject) {
					MD1DotNetProjectHandler handler = new MD1DotNetProjectHandler ((DotNetProject)Item);
					return handler.RunTarget (monitor, target, configuration);
				}
			}
			return null;
		}
		
		public string GetDefaultResourceId (ProjectFile file)
		{
			if (customResourceHandler != null)
				return customResourceHandler.GetDefaultResourceId (file);
			else
				return MSBuildResourceHandler.Instance.GetDefaultResourceId (file);
		}
		
		public string EncodePath (string path, string oldPath)
		{
			string basePath = Path.GetDirectoryName (EntityItem.FileName);
			return FileService.RelativeToAbsolutePath (basePath, path);
		}
		
		public string DecodePath (string path)
		{
			string basePath = Path.GetDirectoryName (EntityItem.FileName);
			return FileService.AbsoluteToRelativePath (basePath, path);
		}

		public SolutionEntityItem Load (IProgressMonitor monitor, string fileName, MSBuildFileFormat format, string language, Type itemClass)
		{
			timer = Counters.ReadMSBuildProject.BeginTiming ();
			
			timer.Trace ("Reading project file");
			MSBuildProject p = new MSBuildProject ();
			p.Load (fileName);

			ToolsVersion = p.ToolsVersion;
			if (string.IsNullOrEmpty (ToolsVersion))
				ToolsVersion = "2.0";

			SetSolutionFormat (format ?? new MSBuildFileFormatVS12 (), false);

			timer.Trace ("Read project guids");
			
			MSBuildPropertySet globalGroup = p.GetGlobalPropertyGroup ();
			
			// Avoid crash if there is not global group
			if (globalGroup == null)
				globalGroup = p.AddNewPropertyGroup (false);

			productVersion = globalGroup.GetPropertyValue ("ProductVersion");
			schemaVersion = globalGroup.GetPropertyValue ("SchemaVersion");
			
			string itemGuid = globalGroup.GetPropertyValue ("ProjectGuid");
			if (itemGuid == null)
				throw new UserException ("Project file doesn't have a valid ProjectGuid");

			// Workaround for a VS issue. VS doesn't include the curly braces in the ProjectGuid
			// of shared projects.
			if (!itemGuid.StartsWith ("{", StringComparison.Ordinal))
				itemGuid = "{" + itemGuid + "}";

			itemGuid = itemGuid.ToUpper ();
			string projectTypeGuids = globalGroup.GetPropertyValue ("ProjectTypeGuids");
			string itemType = globalGroup.GetPropertyValue ("ItemType");

			subtypeGuids.Clear ();
			if (projectTypeGuids != null) {
				foreach (string guid in projectTypeGuids.Split (';')) {
					string sguid = guid.Trim ();
					if (sguid.Length > 0 && string.Compare (sguid, TypeGuid, StringComparison.OrdinalIgnoreCase) != 0)
						subtypeGuids.Add (guid);
				}
			}
			
			try {
				timer.Trace ("Create item instance");
				ProjectExtensionUtil.BeginLoadOperation ();
				Item = CreateSolutionItem (monitor, p, fileName, language, itemType, itemClass);
	
				if (subtypeGuids.Any ()) {
					string gg = string.Join (";", subtypeGuids) + ";" + TypeGuid;
					Item.ExtendedProperties ["ProjectTypeGuids"] = gg.ToUpper ();
				}

				Item.SetItemHandler (this);
				MSBuildProjectService.SetId (Item, itemGuid);
				
				SolutionEntityItem it = (SolutionEntityItem) Item;
				
				it.FileName = fileName;
				it.Name = System.IO.Path.GetFileNameWithoutExtension (fileName);
				
				RemoveDuplicateItems (p, fileName);
				
				LoadProject (monitor, p);
				return it;
				
			} finally {
				ProjectExtensionUtil.EndLoadOperation ();
				timer.End ();
			}
		}

		/// <summary>Whether to use the MSBuild engine for the specified item.</summary>
		internal bool UseMSBuildEngineForItem (SolutionItem item, ConfigurationSelector sel, bool checkReferences = true)
		{
			// if the item mandates MSBuild, always use it
			if (RequireMSBuildEngine)
				return true;
			// if the user has set the option, use the setting
			if (item.UseMSBuildEngine.HasValue)
				return item.UseMSBuildEngine.Value;

			// If the item type defaults to using MSBuild, only use MSBuild if its direct references also use MSBuild.
			// This prevents a not-uncommon common error referencing non-MSBuild projects from MSBuild projects
			// NOTE: This adds about 11ms to the load/build/etc times of the MonoDevelop solution. Doing it recursively
			// adds well over a second.
			return UseMSBuildEngineByDefault && (
				!checkReferences ||
				item.GetReferencedItems (sel).All (i => {
					var h = i.ItemHandler as MSBuildProjectHandler;
					return h != null && h.UseMSBuildEngineForItem (i, sel, false);
				})
			);
		}

		/// <summary>Whether to use the MSBuild engine by default.</summary>
		internal bool UseMSBuildEngineByDefault { get; set; }

		/// <summary>Forces the MSBuild engine to be used.</summary>
		internal bool RequireMSBuildEngine { get; set; }
		
		// All of the last 4 parameters are optional, but at least one must be provided.
		SolutionItem CreateSolutionItem (IProgressMonitor monitor, MSBuildProject p, string fileName, string language,
			string itemType, Type itemClass)
		{
			if (ProjectTypeIsUnsupported)
				return new UnknownProject (fileName, UnknownProjectTypeInfo.GetInstructions ());

			if (subtypeGuids.Any ()) {
				DotNetProjectSubtypeNode st = MSBuildProjectService.GetDotNetProjectSubtype (subtypeGuids);
				if (st != null) {
					UseMSBuildEngineByDefault = st.UseXBuild;
					RequireMSBuildEngine = st.RequireXBuild;
					Type migratedType = null;

					if (st.IsMigration && (migratedType = MigrateProject (monitor, st, p, fileName, language)) != null) {
						var oldSt = st;
						st = MSBuildProjectService.GetItemSubtypeNodes ().Last (t => t.CanHandleType (migratedType));

						for (int i = 0; i < subtypeGuids.Count; i++) {
							if (string.Equals (subtypeGuids [i], oldSt.Guid, StringComparison.OrdinalIgnoreCase)) {
								subtypeGuids [i] = st.Guid;
								oldSt = null;
								break;
							}
						}

						if (oldSt != null)
							throw new Exception ("Unable to correct flavor GUID");

						var gg = string.Join (";", subtypeGuids) + ";" + TypeGuid;
						p.GetGlobalPropertyGroup ().SetPropertyValue ("ProjectTypeGuids", gg.ToUpper (), true);
						p.Save (fileName);
					}

					var item = st.CreateInstance (language);
					st.UpdateImports ((SolutionEntityItem)item, targetImports);
					return item;
				} else {
					var projectInfo = MSBuildProjectService.GetUnknownProjectTypeInfo (subtypeGuids.ToArray (), fileName);
					if (projectInfo != null && projectInfo.LoadFiles) {
						SetUnsupportedType (projectInfo);
						return new UnknownProject (fileName, UnknownProjectTypeInfo.GetInstructions ());
					}
					throw new UnknownSolutionItemTypeException (ProjectTypeIsUnsupported ? TypeGuid : string.Join (";", subtypeGuids));
				}
			}

			if (itemClass != null)
				return (SolutionItem) Activator.CreateInstance (itemClass);
			
			if (!string.IsNullOrEmpty (language)) {
				//enable msbuild by default .NET assembly projects
				UseMSBuildEngineByDefault = true;
				RequireMSBuildEngine = false;
				return new DotNetAssemblyProject (language);
			}
			
			if (string.IsNullOrEmpty (itemType))
				throw new UnknownSolutionItemTypeException ();
				
			DataType dt = MSBuildProjectService.DataContext.GetConfigurationDataType (itemType);
			if (dt == null)
				throw new UnknownSolutionItemTypeException (itemType);
				
			return (SolutionItem) Activator.CreateInstance (dt.ValueType);
		}

		Type MigrateProject (IProgressMonitor monitor, DotNetProjectSubtypeNode st, MSBuildProject p, string fileName, string language)
		{
			var projectLoadMonitor = monitor as IProjectLoadProgressMonitor;
			if (projectLoadMonitor == null) {
				// projectLoadMonitor will be null when running through md-tool, but
				// this is not fatal if migration is not required, so just ignore it. --abock
				if (!st.IsMigrationRequired)
					return null;

				LoggingService.LogError (Environment.StackTrace);
				monitor.ReportError ("Could not open unmigrated project and no migrator was supplied", null);
				throw new Exception ("Could not open unmigrated project and no migrator was supplied");
			}
			
			var migrationType = st.MigrationHandler.CanPromptForMigration
				? st.MigrationHandler.PromptForMigration (projectLoadMonitor, p, fileName, language)
				: projectLoadMonitor.ShouldMigrateProject ();
			if (migrationType == MigrationType.Ignore) {
				if (st.IsMigrationRequired) {
					monitor.ReportError (string.Format ("{1} cannot open the project '{0}' unless it is migrated.", Path.GetFileName (fileName), BrandingService.ApplicationName), null);
					throw new Exception ("The user choose not to migrate the project");
				} else
					return null;
			}
			
			var baseDir = (FilePath) Path.GetDirectoryName (fileName);
			if (migrationType == MigrationType.BackupAndMigrate) {
				var backupDirFirst = baseDir.Combine ("backup");
				string backupDir = backupDirFirst;
				int i = 0;
				while (Directory.Exists (backupDir)) {
					backupDir = backupDirFirst + "-" + i.ToString ();
					if (i++ > 20) {
						throw new Exception ("Too many backup directories");
					}
				}
				Directory.CreateDirectory (backupDir);
				foreach (var file in st.MigrationHandler.FilesToBackup (fileName))
					File.Copy (file, Path.Combine (backupDir, Path.GetFileName (file)));
			}
			
			var type = st.MigrationHandler.Migrate (projectLoadMonitor, p, fileName, language);
			if (type == null)
				throw new Exception ("Could not migrate the project");

			return type;
		}
		
		FileFormat GetFileFormat (MSBuildFileFormat fmt)
		{
			return new FileFormat (fmt, fmt.Id, fmt.Name);
		}
		
		void RemoveDuplicateItems (MSBuildProject msproject, string fileName)
		{
			timer.Trace ("Checking for duplicate items");
			
			var uniqueIncludes = new Dictionary<string,object> ();
			var toRemove = new List<MSBuildItem> ();
			foreach (MSBuildItem bi in msproject.GetAllItems ()) {
				object existing;
				string key = bi.Name + "<" + bi.Include;
				if (!uniqueIncludes.TryGetValue (key, out existing)) {
					uniqueIncludes[key] = bi;
					continue;
				}
				var exBi = existing as MSBuildItem;
				if (exBi != null) {
					if (exBi.Condition != bi.Condition || exBi.Element.InnerXml != bi.Element.InnerXml) {
						uniqueIncludes[key] = new List<MSBuildItem> { exBi, bi };
					} else {
						toRemove.Add (bi);
					}
					continue;
				}
				
				var exList = (List<MSBuildItem>)existing;
				bool found = false;
				foreach (var m in (exList)) {
					if (m.Condition == bi.Condition && m.Element.InnerXml == bi.Element.InnerXml) {
						found = true;
						break;
					}
				}
				if (!found) {
					exList.Add (bi);
				} else {
					toRemove.Add (bi);
				}
			}
			if (toRemove.Count == 0)
				return;
			
			timer.Trace ("Removing duplicate items");
			
			foreach (var t in toRemove)
				msproject.RemoveItem (t);
			
			msproject.Save (fileName);
		}
		
		void LoadConfiguration (MSBuildSerializer serializer, List<ConfigData> configData, string conf, string platform)
		{
			MSBuildPropertySet grp = GetMergedConfiguration (configData, conf, platform, null);
			SolutionItemConfiguration config = EntityItem.CreateConfiguration (conf);
			
			config.Platform = platform;
			DataItem data = ReadPropertyGroupMetadata (serializer, grp, config);
			serializer.Deserialize (config, data);
			EntityItem.Configurations.Add (config);
			
			if (config is DotNetProjectConfiguration) {
				DotNetProjectConfiguration dpc = (DotNetProjectConfiguration) config;
				if (dpc.CompilationParameters != null) {
					data = ReadPropertyGroupMetadata (serializer, grp, dpc.CompilationParameters);
					serializer.Deserialize (dpc.CompilationParameters, data);
				}
			}
		}
		
		protected virtual void LoadProject (IProgressMonitor monitor, MSBuildProject msproject)
		{
			timer.Trace ("Initialize serialization");
			
			MSBuildSerializer ser = CreateSerializer ();
			ser.SerializationContext.BaseFile = EntityItem.FileName;
			ser.SerializationContext.ProgressMonitor = monitor;
			
			MSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();
			
			Item.SetItemHandler (this);
			
			DotNetProject dotNetProject = Item as DotNetProject;
			
			// Read all items
			
			timer.Trace ("Read project items");
			
			LoadProjectItems (msproject, ser, ProjectItemFlags.None);

			timer.Trace ("Read configurations");
			
			TargetFrameworkMoniker targetFx = null;
			
			if (dotNetProject != null) {
				string frameworkIdentifier = globalGroup.GetPropertyValue ("TargetFrameworkIdentifier");
				string frameworkVersion = globalGroup.GetPropertyValue ("TargetFrameworkVersion");
				string frameworkProfile = globalGroup.GetPropertyValue ("TargetFrameworkProfile");
				
				//determine the default target framework from the project type's default
				//overridden by the components in the project
				var def = dotNetProject.GetDefaultTargetFrameworkForFormat (GetFileFormat (GetToolsFormat ()));
				targetFx = new TargetFrameworkMoniker (
					string.IsNullOrEmpty (frameworkIdentifier)? def.Identifier : frameworkIdentifier,
					string.IsNullOrEmpty (frameworkVersion)? def.Version : frameworkVersion,
					string.IsNullOrEmpty (frameworkProfile)? def.Profile : frameworkProfile);
				
				if (dotNetProject.LanguageParameters != null) {
					DataItem data = ReadPropertyGroupMetadata (ser, globalGroup, dotNetProject.LanguageParameters);
					ser.Deserialize (dotNetProject.LanguageParameters, data);
				}
			}
			
			// Read configurations
			
			List<ConfigData> configData = GetConfigData (msproject, false);
			List<ConfigData> partialConfigurations = new List<ConfigData> ();
			HashSet<string> handledConfigurations = new HashSet<string> ();
			var configurations = new HashSet<string> ();
			var platforms = new HashSet<string> ();
			
			MSBuildPropertyGroup mergedToProjectProperties = ExtractMergedtoprojectProperties (ser, globalGroup, EntityItem.CreateConfiguration ("Dummy"));
			configData.Insert (0, new ConfigData (Unspecified, Unspecified, mergedToProjectProperties));
			
			// Load configurations, skipping the dummy config at index 0.
			for (int i = 1; i < configData.Count; i++) {
				ConfigData cgrp = configData[i];
				string platform = cgrp.Platform;
				string conf = cgrp.Config;
				
				if (platform != Unspecified)
					platforms.Add (platform);
				
				if (conf != Unspecified)
					configurations.Add (conf);
				
				if (conf == Unspecified || platform == Unspecified) {
					// skip partial configurations for now...
					partialConfigurations.Add (cgrp);
					continue;
				}
				
				string key = conf + "|" + platform;
				if (handledConfigurations.Contains (key))
					continue;
				
				LoadConfiguration (ser, configData, conf, platform);
				
				handledConfigurations.Add (key);
			}
			
			// Now we can load any partial configurations by combining them with known configs or platforms.
			if (partialConfigurations.Count > 0) {
				if (platforms.Count == 0)
					platforms.Add (string.Empty); // AnyCpu
				
				foreach (ConfigData cgrp in partialConfigurations) {
					if (cgrp.Config != Unspecified && cgrp.Platform == Unspecified) {
						string conf = cgrp.Config;
						
						foreach (var platform in platforms) {
							string key = conf + "|" + platform;
							
							if (handledConfigurations.Contains (key))
								continue;
							
							LoadConfiguration (ser, configData, conf, platform);
							
							handledConfigurations.Add (key);
						}
					} else if (cgrp.Config == Unspecified && cgrp.Platform != Unspecified) {
						string platform = cgrp.Platform;
						
						foreach (var conf in configurations) {
							string key = conf + "|" + platform;
							
							if (handledConfigurations.Contains (key))
								continue;
							
							LoadConfiguration (ser, configData, conf, platform);
							
							handledConfigurations.Add (key);
						}
					}
				}
			}
			
			// Read extended properties
			
			timer.Trace ("Read extended properties");
			
			DataItem globalData = ReadPropertyGroupMetadata (ser, globalGroup, Item);
			
			string extendedData = msproject.GetProjectExtensions ("MonoDevelop");
			if (!string.IsNullOrEmpty (extendedData)) {
				StringReader sr = new StringReader (extendedData);
				DataItem data = (DataItem) XmlConfigurationReader.DefaultReader.Read (new XmlTextReader (sr));
				globalData.ItemData.AddRange (data.ItemData);
			}
			ser.Deserialize (Item, globalData);

			// Final initializations
			
			timer.Trace ("Final initializations");
			
			//clean up the "InternalTargetFrameworkVersion" hack from MD 2.2, 2.4
			if (dotNetProject != null) {
				string fx = Item.ExtendedProperties ["InternalTargetFrameworkVersion"] as string;
				if (!string.IsNullOrEmpty (fx)) {
					targetFx = TargetFrameworkMoniker.Parse (fx);
					Item.ExtendedProperties.Remove ("InternalTargetFrameworkVersion");
				}
				
				dotNetProject.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (targetFx);
			}

			LoadFromMSBuildProject (monitor, msproject);

			Item.NeedsReload = false;
		}

		internal void LoadProjectItems (MSBuildProject msproject, MSBuildSerializer ser, ProjectItemFlags flags)
		{
			foreach (MSBuildItem buildItem in msproject.GetAllItems ()) {
				ProjectItem it = ReadItem (ser, buildItem);
				if (it == null)
					continue;
				it.Flags = flags;
				if (it is ProjectFile) {
					var file = (ProjectFile)it;
					if (file.Name.IndexOf ('*') > -1) {
						// Thanks to IsOriginatedFromWildcard, these expanded items will not be saved back to disk.
						foreach (var expandedItem in ResolveWildcardItems (file))
							EntityItem.Items.Add (expandedItem);
						// Add to wildcard items (so it can be re-saved) instead of Items (where tools will 
						// try to compile and display these nonstandard items
						EntityItem.WildcardItems.Add (it);
						continue;
					}
					if (ProjectTypeIsUnsupported && !File.Exists (file.FilePath))
						continue;
				}
				EntityItem.Items.Add (it);
				it.ExtendedProperties ["MSBuild.SourceProject"] = msproject.FileName;
			}
		}

		protected virtual void LoadFromMSBuildProject (IProgressMonitor monitor, MSBuildProject msproject)
		{
			foreach (var ext in GetMSBuildExtensions ())
				ext.LoadProject (monitor, EntityItem, msproject);
		}

		const string RecursiveDirectoryWildcard = "**";
		static readonly char[] directorySeparators = new [] {
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		};

		static string GetWildcardDirectoryName (string path)
		{
			int indexOfLast = path.LastIndexOfAny (directorySeparators);
			if (indexOfLast < 0)
				return String.Empty;
			return path.Substring (0, indexOfLast);
		}

		static string GetWildcardFileName (string path)
		{
			int indexOfLast = path.LastIndexOfAny (directorySeparators);
			if (indexOfLast < 0)
				return path;
			if (indexOfLast == path.Length)
				return String.Empty;
			return path.Substring (indexOfLast + 1, path.Length - (indexOfLast + 1));
		}

		static IEnumerable<string> ExpandWildcardFilePath (string filePath)
		{
			if (String.IsNullOrWhiteSpace (filePath))
				throw new ArgumentException ("Not a wildcard path");

			string dir = GetWildcardDirectoryName (filePath);
			string file = GetWildcardFileName (filePath);

			if (String.IsNullOrEmpty (dir) || String.IsNullOrEmpty (file))
				return null;

			SearchOption searchOption = SearchOption.TopDirectoryOnly;
			if (dir.EndsWith (RecursiveDirectoryWildcard, StringComparison.Ordinal)) {
				dir = dir.Substring (0, dir.Length - RecursiveDirectoryWildcard.Length);
				searchOption = SearchOption.AllDirectories;
			}

			if (!Directory.Exists (dir))
				return null;

			return Directory.GetFiles (dir, file, searchOption);
		}

		static IEnumerable<ProjectFile> ResolveWildcardItems (ProjectFile wildcardFile)
		{
			var paths = ExpandWildcardFilePath (wildcardFile.Name);
			if (paths == null)
				yield break;
			foreach (var resolvedFilePath in paths) {
				var projectFile = (ProjectFile)wildcardFile.Clone ();
				projectFile.Name = resolvedFilePath;
				projectFile.IsOriginatedFromWildcard = true;
				yield return projectFile;
			}
		}

		MSBuildPropertyGroup ExtractMergedtoprojectProperties (MSBuildSerializer ser, MSBuildPropertySet pgroup, SolutionItemConfiguration ob)
		{
			XmlDocument doc = new XmlDocument ();
			MSBuildPropertyGroup res = new MSBuildPropertyGroup (null, doc.CreateElement ("PropGroup"));

			// When reading a project, all configuration properties specified in the global property group have to
			// be merged with all project configurations, no matter if they have the MergeToProject attribute or not
			
			ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (ob.GetType ());
			foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, ob)) {
				MSBuildProperty bp = pgroup.GetProperty (prop.Name);
				if (bp != null) {
					var preserveCase = prop.DataType is MSBuildBoolDataType;
					res.SetPropertyValue (bp.Name, bp.Element.InnerXml, preserveCase, true);
				}
			}
			if (ob is DotNetProjectConfiguration) {
				object cparams = ((DotNetProjectConfiguration)ob).CompilationParameters;
				dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (cparams.GetType ());
				foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, cparams)) {
					MSBuildProperty bp = pgroup.GetProperty (prop.Name);
					if (bp != null) {
						var preserveCase = prop.DataType is MSBuildBoolDataType;
						res.SetPropertyValue (bp.Name, bp.Element.InnerXml, preserveCase, true);
					}
				}
			}
			return res;
		}
		
		IEnumerable<MergedProperty> GetMergeToProjectProperties (MSBuildSerializer ser, object ob)
		{
			ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (ob.GetType ());
			foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, ob)) {
				if (IsMergeToProjectProperty (prop)) {
					yield return new MergedProperty (prop.Name, prop.DataType is MSBuildBoolDataType);
				}
			}
		}

		struct MergedProperty
		{
			public readonly string Name;
			public readonly bool PreserveExistingCase;

			public MergedProperty (string name, bool preserveExistingCase)
			{
				this.Name = name;
				this.PreserveExistingCase = preserveExistingCase;
			}
		}

		internal ProjectItem ReadItem (MSBuildSerializer ser, MSBuildItem buildItem)
		{
			Project project = Item as Project;
			DotNetProject dotNetProject = Item as DotNetProject;
			
			DataType dt = ser.DataContext.GetConfigurationDataType (buildItem.Name);
			
			if (project != null) {
				if (buildItem.Name == "Folder") {
					// Read folders
					string path = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
					return new ProjectFile () { Name = Path.GetDirectoryName (path), Subtype = Subtype.Directory };
				}
				else if (buildItem.Name == "Reference" && dotNetProject != null) {
					ProjectReference pref;
					if (buildItem.HasMetadata ("HintPath")) {
						string hintPath = buildItem.GetMetadata ("HintPath");
						string path;
						if (!MSBuildProjectService.FromMSBuildPath (dotNetProject.ItemDirectory, hintPath, out path)) {
							pref = new ProjectReference (ReferenceType.Assembly, path);
							pref.SetInvalid (GettextCatalog.GetString ("Invalid file path"));
							pref.ExtendedProperties ["_OriginalMSBuildReferenceInclude"] = buildItem.Include;
							pref.ExtendedProperties ["_OriginalMSBuildReferenceHintPath"] = hintPath;
						} else {
							var type = File.Exists (path) ? ReferenceType.Assembly : ReferenceType.Package;
							pref = new ProjectReference (type, buildItem.Include, path);
							pref.ExtendedProperties ["_OriginalMSBuildReferenceHintPath"] = hintPath;
							if (MSBuildProjectService.IsAbsoluteMSBuildPath (hintPath))
								pref.ExtendedProperties ["_OriginalMSBuildReferenceIsAbsolute"] = true;
						}
					} else {
						string asm = buildItem.Include;
						// This is a workaround for a VS bug. Looks like it is writing this assembly incorrectly
						if (asm == "System.configuration")
							asm = "System.Configuration";
						else if (asm == "System.XML")
							asm = "System.Xml";
						else if (asm == "system")
							asm = "System";
						pref = new ProjectReference (ReferenceType.Package, asm);
					}
					var privateCopy = buildItem.GetBoolMetadata ("Private");
					if (privateCopy != null)
						pref.LocalCopy = privateCopy.Value;

					pref.Condition = buildItem.Condition;
					string specificVersion = buildItem.GetMetadata ("SpecificVersion");
					if (string.IsNullOrWhiteSpace (specificVersion)) {
						// If the SpecificVersion element isn't present, check if the Assembly Reference specifies a Version
						pref.SpecificVersion = ReferenceStringHasVersion (buildItem.Include);
					}
					else {
						bool value;
						// if we can't parse the value, default to false which is more permissive
						pref.SpecificVersion = bool.TryParse (specificVersion, out value) && value;
					}
					ReadBuildItemMetadata (ser, buildItem, pref, typeof(ProjectReference));
					return pref;
				}
				else if (buildItem.Name == "ProjectReference" && dotNetProject != null) {
					// Get the project name from the path, since the Name attribute may other stuff other than the name
					string path = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
					string name = Path.GetFileNameWithoutExtension (path);
					ProjectReference pref = new ProjectReference (ReferenceType.Project, name);
					pref.Condition = buildItem.Condition;
					var privateCopy = buildItem.GetBoolMetadata ("Private");
					if (privateCopy != null)
						pref.LocalCopy = privateCopy.Value;
					var roa = buildItem.GetBoolMetadata ("ReferenceOutputAssembly");
					pref.ReferenceOutputAssembly = roa == null || roa.Value;
					ReadBuildItemMetadata (ser, buildItem, pref, typeof(ProjectReference));
					return pref;
				}
				else if (dt == null && !string.IsNullOrEmpty (buildItem.Include)) {
					// Unknown item. Must be a file.
					if (!UnsupportedItems.Contains (buildItem.Name) && IsValidFile (buildItem.Include))
						return ReadProjectFile (ser, project, buildItem, typeof(ProjectFile));
				}
			}

			// ProjectReference objects only make sense on a DotNetProject, so don't load them
			// if that's not the type of the project.
			if (dt != null && dt.ValueType == typeof(ProjectReference) && dotNetProject == null)
				dt = null;
			
			if (dt != null && typeof(ProjectItem).IsAssignableFrom (dt.ValueType)) {
				ProjectItem obj = (ProjectItem) Activator.CreateInstance (dt.ValueType);
				ReadBuildItemMetadata (ser, buildItem, obj, dt.ValueType);
				return obj;
			}
			
			UnknownProjectItem uitem = new UnknownProjectItem (buildItem.Name, "");
			ReadBuildItemMetadata (ser, buildItem, uitem, typeof(UnknownProjectItem));
			
			return uitem;
		}
		
		bool ReferenceStringHasVersion (string asmName)
		{
			int commaPos = asmName.IndexOf (',');
			return commaPos >= 0 && asmName.IndexOf ("Version", commaPos, StringComparison.Ordinal) >= 0;
		}

		bool IsValidFile (string path)
		{
			// If it is an absolute uri, it's not a valid file
			try {
				if (Uri.IsWellFormedUriString (path, UriKind.Absolute)) {
					var f = new Uri (path);
					return f.Scheme == "file";
				}
			} catch {
				// Old mono versions may crash in IsWellFormedUriString if the path
				// is not an uri.
			}
			return true;
		}
		
		class ConfigData
		{
			public ConfigData (string conf, string plt, MSBuildPropertyGroup grp)
			{
				Config = conf;
				Platform = plt;
				Group = grp;
			}
			
			public bool FullySpecified {
				get { return Config != Unspecified && Platform != Unspecified; }
			}
			
			public string Config;
			public string Platform;
			public MSBuildPropertyGroup Group;
			public bool Exists;
			public bool IsNew; // The group did not exist in the original file
		}

		MSBuildPropertySet GetMergedConfiguration (List<ConfigData> configData, string conf, string platform, MSBuildPropertyGroup propGroupLimit)
		{
			MSBuildPropertySet merged = null;
			
			foreach (ConfigData grp in configData) {
				if (grp.Group == propGroupLimit)
					break;
				if ((grp.Config == conf || grp.Config == Unspecified || conf == Unspecified) && (grp.Platform == platform || grp.Platform == Unspecified || platform == Unspecified)) {
					if (merged == null)
						merged = grp.Group;
					else if (merged is MSBuildPropertyGroupMerged)
						((MSBuildPropertyGroupMerged)merged).Add (grp.Group);
					else {
						MSBuildPropertyGroupMerged m = new MSBuildPropertyGroupMerged ();
						m.Add ((MSBuildPropertyGroup)merged);
						m.Add (grp.Group);
						merged = m;
					}
				}
			}
			return merged;
		}

		bool ContainsSpecificPlatformConfiguration (List<ConfigData> configData, string conf)
		{
			foreach (ConfigData grp in configData) {
				if (grp.Config == conf && grp.Platform != Unspecified)
					return true;
			}
			return false;
		}

		public override void OnModified (string hint)
		{
			base.OnModified (hint);
			modifiedInMemory = true;
		}

		protected override void SaveItem (MonoDevelop.Core.IProgressMonitor monitor)
		{
			modifiedInMemory = false;

			MSBuildProject msproject = SaveProject (monitor);
			if (msproject == null)
				return;
			
			// Don't save the file to disk if the content did not change
			msproject.Save (EntityItem.FileName);

			if (projectBuilder != null)
				projectBuilder.Refresh ();
		}

		protected virtual MSBuildProject SaveProject (IProgressMonitor monitor)
		{
			if (Item is UnknownSolutionItem)
				return null;

			var toolsFormat = GetToolsFormat ();
			
			bool newProject;
			SolutionEntityItem eitem = EntityItem;
			
			MSBuildSerializer ser = CreateSerializer ();
			ser.SerializationContext.BaseFile = eitem.FileName;
			ser.SerializationContext.ProgressMonitor = monitor;
			
			DotNetProject dotNetProject = Item as DotNetProject;
			
			MSBuildProject msproject = new MSBuildProject ();
			newProject = EntityItem.FileName == null || !File.Exists (EntityItem.FileName);
			if (newProject) {
				msproject.DefaultTargets = "Build";
			} else {
				msproject.Load (EntityItem.FileName);
			}

			// Global properties
			
			MSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();
			if (globalGroup == null) {
				globalGroup = msproject.AddNewPropertyGroup (false);
			}
			
			if (eitem.Configurations.Count > 0) {
				ItemConfiguration conf = eitem.Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == "Debug");
				if (conf == null) conf = eitem.Configurations [0];
				MSBuildProperty bprop = globalGroup.SetPropertyValue ("Configuration", conf.Name, false);
				bprop.Condition = " '$(Configuration)' == '' ";
				
				string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
				bprop = globalGroup.SetPropertyValue ("Platform", platform, false);
				bprop.Condition = " '$(Platform)' == '' ";
			}
			
			if (TypeGuid == MSBuildProjectService.GenericItemGuid) {
				DataType dt = MSBuildProjectService.DataContext.GetConfigurationDataType (Item.GetType ());
				globalGroup.SetPropertyValue ("ItemType", dt.Name, false);
			}

			Item.ExtendedProperties ["ProjectGuid"] = Item.ItemId;
			if (subtypeGuids.Count > 0) {
				string gg = "";
				foreach (string sg in subtypeGuids) {
					if (gg.Length > 0)
						gg += ";";
					gg += sg;
				}
				gg += ";" + TypeGuid;
				Item.ExtendedProperties ["ProjectTypeGuids"] = gg.ToUpper ();
				globalGroup.SetPropertyValue ("ProjectTypeGuids", gg.ToUpper (), true);
			} else {
				Item.ExtendedProperties.Remove ("ProjectTypeGuids");
				globalGroup.RemoveProperty ("ProjectTypeGuids");
			}

			Item.ExtendedProperties ["ProductVersion"] = productVersion;
			Item.ExtendedProperties ["SchemaVersion"] = schemaVersion;

			// having no ToolsVersion is equivalent to 2.0, roundtrip that correctly
			if (ToolsVersion != "2.0")
				msproject.ToolsVersion = ToolsVersion;
			else if (string.IsNullOrEmpty (msproject.ToolsVersion))
				msproject.ToolsVersion = null;
			else
				msproject.ToolsVersion = "2.0";

			// This serialize call will write data to ser.InternalItemProperties and ser.ExternalItemProperties
			ser.Serialize (Item, Item.GetType ());
			
			object langParams = null;
			
			if (dotNetProject != null && dotNetProject.LanguageParameters != null) {
				// Remove all language parameters properties from the data item, since we are going to write them again.
				ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (dotNetProject.LanguageParameters.GetType ());
				foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, dotNetProject.LanguageParameters)) {
					DataNode n = ser.InternalItemProperties.ItemData [prop.Name];
					if (n != null)
						ser.InternalItemProperties.ItemData.Remove (n);
				}
				DataItem ditemComp = (DataItem) ser.Serialize (dotNetProject.LanguageParameters);
				ser.InternalItemProperties.ItemData.AddRange (ditemComp.ItemData);
				langParams = dotNetProject.LanguageParameters;
			}
			
			if (newProject)
				ser.InternalItemProperties.ItemData.Sort (globalConfigOrder);

			WritePropertyGroupMetadata (globalGroup, ser.InternalItemProperties.ItemData, ser, Item, langParams);
			
			// Convert debug property
			
			foreach (SolutionItemConfiguration conf in eitem.Configurations) {
				if (newProject && conf is DotNetProjectConfiguration) {
					conf.ExtendedProperties ["ErrorReport"] = "prompt";
				}
			}
			
			// Configurations

			if (eitem.Configurations.Count > 0) {
				List<ConfigData> configData = GetConfigData (msproject, true);

				var mergeToProjectPropertyValues = new Dictionary<string,MergedPropertyValue> ();
				var mergeToProjectProperties = new HashSet<MergedProperty> (GetMergeToProjectProperties (ser, eitem.Configurations [0]));
				var mergeToProjectPropertyNames = new HashSet<string> (mergeToProjectProperties.Select (p => p.Name));
				
				foreach (SolutionItemConfiguration conf in eitem.Configurations) {
					bool newConf = false;
					ConfigData cdata = FindPropertyGroup (configData, conf);
					if (cdata == null) {
						MSBuildPropertyGroup pg = msproject.AddNewPropertyGroup (true);
						pg.Condition = BuildConfigCondition (conf.Name, conf.Platform);
						cdata = new ConfigData (conf.Name, conf.Platform, pg);
						cdata.IsNew = true;
						configData.Add (cdata);
						newConf = true;
					}
					
					MSBuildPropertyGroup propGroup = cdata.Group;
					cdata.Exists = true;
					
					MSBuildPropertySet baseGroup = GetMergedConfiguration (configData, conf.Name, conf.Platform, propGroup);

					// Force the serialization of properties defined in
					// the base group, so that they can be later unmerged
					if (baseGroup != null)
						ForceDefaultValueSerialization (ser, baseGroup, conf);
					DataItem ditem = (DataItem) ser.Serialize (conf);
					ser.SerializationContext.ResetDefaultValueSerialization ();
					
					DotNetProjectConfiguration netConfig = conf as DotNetProjectConfiguration;
					
					if (netConfig != null && netConfig.CompilationParameters != null) {
						// Remove all compilation parameters properties from the data item, since we are going to write them again.
						ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (netConfig.CompilationParameters.GetType ());
						foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, netConfig.CompilationParameters)) {
							DataNode n = ditem.ItemData [prop.Name];
							if (n != null)
								ditem.ItemData.Remove (n);
						}
						if (baseGroup != null)
							ForceDefaultValueSerialization (ser, baseGroup, netConfig.CompilationParameters);
						DataItem ditemComp = (DataItem) ser.Serialize (netConfig.CompilationParameters);
						ser.SerializationContext.ResetDefaultValueSerialization ();
						ditem.ItemData.AddRange (ditemComp.ItemData);
					}
	
					if (newConf)
						ditem.ItemData.Sort (configOrder);
					
					WritePropertyGroupMetadata (propGroup, ditem.ItemData, ser, conf, netConfig != null ? netConfig.CompilationParameters : null);
					
					CollectMergetoprojectProperties (propGroup, mergeToProjectProperties, mergeToProjectPropertyValues);
					
					if (baseGroup != null)
						propGroup.UnMerge (baseGroup, mergeToProjectPropertyNames);
				}
				
				// Move properties with common values from configurations to the main
				// property group
				foreach (KeyValuePair<string,MergedPropertyValue> prop in mergeToProjectPropertyValues)
					globalGroup.SetPropertyValue (prop.Key, prop.Value.XmlValue, prop.Value.PreserveExistingCase, true);
				foreach (string prop in mergeToProjectPropertyNames) {
					if (!mergeToProjectPropertyValues.ContainsKey (prop))
						globalGroup.RemoveProperty (prop);
				}
				foreach (SolutionItemConfiguration conf in eitem.Configurations) {
					MSBuildPropertyGroup propGroup = FindPropertyGroup (configData, conf).Group;
					foreach (string mp in mergeToProjectPropertyValues.Keys)
						propGroup.RemoveProperty (mp);
				}
				
				// Remove groups corresponding to configurations that have been removed
				// or groups which don't have any property and did not already exist
				foreach (ConfigData cd in configData) {
					if ((!cd.Exists && cd.FullySpecified) || (cd.IsNew && !cd.Group.Properties.Any ()))
						msproject.RemoveGroup (cd.Group);
				}
			}
			
			SaveProjectItems (monitor, toolsFormat, ser, msproject);
			
			if (dotNetProject != null) {
				var moniker = dotNetProject.TargetFramework.Id;
				bool supportsMultipleFrameworks = toolsFormat.SupportsMonikers || toolsFormat.SupportedFrameworks.Length > 0;
				var def = dotNetProject.GetDefaultTargetFrameworkForFormat (GetFileFormat (toolsFormat));

				// If the format only supports one fx version, or the version is the default, there is no need to store it.
				// However, is there is already a value set, do not remove it.
				if (supportsMultipleFrameworks) {
					SetIfPresentOrNotDefaultValue (globalGroup, "TargetFrameworkVersion", "v" + moniker.Version, "v" + def.Version);
				}
				
				if (toolsFormat.SupportsMonikers) {
					SetIfPresentOrNotDefaultValue (globalGroup, "TargetFrameworkIdentifier", moniker.Identifier, def.Identifier);
					SetIfPresentOrNotDefaultValue (globalGroup, "TargetFrameworkProfile", moniker.Profile, def.Profile);
				}
			}

			// Impdate the imports section
			
			List<DotNetProjectImport> currentImports = msproject.Imports.Select (i => new DotNetProjectImport (i.Project)).ToList ();
			List<DotNetProjectImport> imports = new List<DotNetProjectImport> (currentImports);
			
			// If the project is not new, don't add the default project imports,
			// just assume that the current imports are correct
			UpdateImports (imports, dotNetProject, newProject);
			foreach (DotNetProjectImport imp in imports) {
				if (!currentImports.Contains (imp)) {
					MSBuildImport import = msproject.AddNewImport (imp.Name);
					if (imp.HasCondition ())
						import.Condition = imp.Condition;
					currentImports.Add (imp);
				}
			}
			foreach (DotNetProjectImport imp in currentImports) {
				if (!imports.Contains (imp))
					msproject.RemoveImport (imp.Name);
			}
			
			DataItem extendedData = ser.ExternalItemProperties;
			if (extendedData.HasItemData) {
				extendedData.Name = "Properties";
				StringWriter sw = new StringWriter ();
				XmlConfigurationWriter.DefaultWriter.Write (new XmlTextWriter (sw), extendedData);
				msproject.SetProjectExtensions ("MonoDevelop", sw.ToString ());
			} else
				msproject.RemoveProjectExtensions ("MonoDevelop");

			SaveToMSBuildProject (monitor, msproject);

			return msproject;
		}

		internal void SaveProjectItems (IProgressMonitor monitor, MSBuildFileFormat toolsFormat, MSBuildSerializer ser, MSBuildProject msproject, string pathPrefix = null)
		{
			// Remove old items
			Dictionary<string, ItemInfo> oldItems = new Dictionary<string, ItemInfo> ();
			foreach (MSBuildItem item in msproject.GetAllItems ())
				oldItems [item.Name + "<" + item.UnevaluatedInclude + "<" + item.Condition] = new ItemInfo () {
					Item = item
				};
			// Add the new items
			foreach (object ob in ((SolutionEntityItem)Item).Items.Concat (((SolutionEntityItem)Item).WildcardItems).Where (it => !it.Flags.HasFlag (ProjectItemFlags.DontPersist)))
				SaveItem (monitor, toolsFormat, ser, msproject, ob, oldItems, pathPrefix);
			foreach (ItemInfo itemInfo in oldItems.Values) {
				if (!itemInfo.Added)
					msproject.RemoveItem (itemInfo.Item);
			}
		}

		protected void SaveToMSBuildProject (IProgressMonitor monitor, MSBuildProject msproject)
		{
			foreach (var ext in GetMSBuildExtensions ())
				ext.SaveProject (monitor, EntityItem, msproject);
		}

		void SetIfPresentOrNotDefaultValue (MSBuildPropertySet propGroup, string name, string value, string defaultValue, bool isXml = false)
		{
			bool hasDefaultValue = string.IsNullOrEmpty (value) || value == defaultValue;
			var prop = propGroup.GetProperty (name);
			if (prop != null) {
				//if the value is default or empty, only remove the element if it was not already the default or empty
				//to avoid unnecessary project file churn
				if (hasDefaultValue) {
					var existing = prop.GetValue (isXml);
					bool alreadyHadDefaultValue = string.IsNullOrEmpty (existing) || existing == defaultValue;
					if (!alreadyHadDefaultValue)
						propGroup.RemoveProperty (name);
				} else {
					prop.SetValue (value, isXml);
				}
			} else if (!hasDefaultValue) {
				propGroup.SetPropertyValue (name, value, false, isXml);
			}
		}
		
		void ForceDefaultValueSerialization (MSBuildSerializer ser, MSBuildPropertySet baseGroup, object ob)
		{
			ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (ob.GetType());
			foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, ob)) {
				if (baseGroup.GetProperty (prop.Name) != null)
					ser.SerializationContext.ForceDefaultValueSerialization (prop);
			}
		}

		void CollectMergetoprojectProperties (MSBuildPropertyGroup pgroup, HashSet<MergedProperty> properties, Dictionary<string,MergedPropertyValue> mergeToProjectProperties)
		{
			// This method checks every property in pgroup which has the MergeToProject flag.
			// If the value of this property is the same as the one stored in mergeToProjectProperties
			// it means that the property can be merged to the main project property group (so far).
			
			foreach (var pinfo in new List<MergedProperty> (properties)) {
				MSBuildProperty prop = pgroup.GetProperty (pinfo.Name);
				
				MergedPropertyValue mvalue;
				if (!mergeToProjectProperties.TryGetValue (pinfo.Name, out mvalue)) {
					if (prop != null) {
						// This is the first time the value is checked. Just assign it.
						mergeToProjectProperties.Add (pinfo.Name, new MergedPropertyValue (prop.GetValue (true), pinfo.PreserveExistingCase));
						continue;
					}
					// If there is no value, it can't be merged
				}
				else if (prop != null && string.Equals (prop.GetValue (true), mvalue.XmlValue, StringComparison.OrdinalIgnoreCase))
					// Same value. It can be merged.
					continue;

				// The property can't be merged because different configurations have different
				// values for it. Remove it from the list.
				properties.Remove (pinfo);
				mergeToProjectProperties.Remove (pinfo.Name);
			}
		}

		struct MergedPropertyValue
		{
			public readonly string XmlValue;
			public readonly bool PreserveExistingCase;

			public MergedPropertyValue (string xmlValue, bool preserveExistingCase)
			{
				this.XmlValue = xmlValue;
				this.PreserveExistingCase = preserveExistingCase;
			}
		}
		
		void SaveItem (IProgressMonitor monitor, MSBuildFileFormat fmt, MSBuildSerializer ser, MSBuildProject msproject, object ob, Dictionary<string,ItemInfo> oldItems, string pathPrefix = null)
		{
			if (ob is ProjectReference) {
				SaveReference (monitor, fmt, ser, msproject, (ProjectReference) ob, oldItems);
			}
			else if (ob is ProjectFile) {
				SaveProjectFile (ser, msproject, (ProjectFile) ob, oldItems, pathPrefix);
			}
			else {
				string itemName;
				if (ob is UnknownProjectItem) {
					var ui = (UnknownProjectItem)ob;
					itemName = ui.ItemName;
					var buildItem = AddOrGetBuildItem (msproject, oldItems, itemName, ui.Include, ui.Condition);
					WriteBuildItemMetadata (ser, buildItem, ob, oldItems);
				}
				else {
					DataType dt = ser.DataContext.GetConfigurationDataType (ob.GetType ());
					var buildItem = msproject.AddNewItem (dt.Name, "");
					WriteBuildItemMetadata (ser, buildItem, ob, oldItems);
				}
			}
		}
		
		void SaveProjectFile (MSBuildSerializer ser, MSBuildProject msproject, ProjectFile file, Dictionary<string,ItemInfo> oldItems, string pathPrefix = null)
		{
			if (file.IsOriginatedFromWildcard) return;

			string itemName = (file.Subtype == Subtype.Directory)? "Folder" : file.BuildAction;

			string path = pathPrefix + MSBuildProjectService.ToMSBuildPath (Item.ItemDirectory, file.FilePath);
			if (path.Length == 0)
				return;
			
			//directory paths must end with '/'
			if ((file.Subtype == Subtype.Directory) && path[path.Length-1] != '\\')
				path = path + "\\";
			
			MSBuildItem buildItem = AddOrGetBuildItem (msproject, oldItems, itemName, path, file.Condition);
			WriteBuildItemMetadata (ser, buildItem, file, oldItems);
			
			if (!string.IsNullOrEmpty (file.DependsOn))
				buildItem.SetMetadata ("DependentUpon", MSBuildProjectService.ToMSBuildPath (Path.GetDirectoryName (file.FilePath), file.DependsOn));
			else
				buildItem.UnsetMetadata ("DependentUpon");

			if (!string.IsNullOrEmpty (file.ContentType))
				buildItem.SetMetadata ("SubType", file.ContentType);
			
			if (!string.IsNullOrEmpty (file.Generator))
				buildItem.SetMetadata ("Generator", file.Generator);
			else
				buildItem.UnsetMetadata ("Generator");
			
			if (!string.IsNullOrEmpty (file.CustomToolNamespace))
				buildItem.SetMetadata ("CustomToolNamespace", file.CustomToolNamespace);
			else
				buildItem.UnsetMetadata ("CustomToolNamespace");
			
			if (!string.IsNullOrEmpty (file.LastGenOutput))
				buildItem.SetMetadata ("LastGenOutput", file.LastGenOutput);
			else
				buildItem.UnsetMetadata ("LastGenOutput");
			
			if (!string.IsNullOrEmpty (file.Link))
				buildItem.SetMetadata ("Link", MSBuildProjectService.ToMSBuildPathRelative (Item.ItemDirectory, file.Link));
			else
				buildItem.UnsetMetadata ("Link");
			
			buildItem.Condition = file.Condition;
			
			if (file.CopyToOutputDirectory == FileCopyMode.None) {
				buildItem.UnsetMetadata ("CopyToOutputDirectory");
			} else {
				buildItem.SetMetadata ("CopyToOutputDirectory", file.CopyToOutputDirectory.ToString ());
			}
			
			if (!file.Visible) {
				buildItem.SetMetadata ("Visible", "False");
			} else {
				buildItem.UnsetMetadata ("Visible");
			}

			var resId = file.ResourceId;

			//For EmbeddedResource, emit LogicalName only when it does not match the default Id
			if (file.BuildAction == BuildAction.EmbeddedResource && GetDefaultResourceId (file) == resId)
				resId = null;

			if (!string.IsNullOrEmpty (resId)) {
				buildItem.SetMetadata ("LogicalName", resId);
			} else {
				buildItem.UnsetMetadata ("LogicalName");
			}
		}
		
		void SaveReference (IProgressMonitor monitor, MSBuildFileFormat fmt, MSBuildSerializer ser, MSBuildProject msproject, ProjectReference pref, Dictionary<string,ItemInfo> oldItems)
		{
			MSBuildItem buildItem;
			if (pref.ReferenceType == ReferenceType.Assembly) {
				string asm = null;
				string hintPath = null;
				if (pref.ExtendedProperties.Contains ("_OriginalMSBuildReferenceInclude")) {
					asm = (string) pref.ExtendedProperties ["_OriginalMSBuildReferenceInclude"];
					hintPath = (string) pref.ExtendedProperties ["_OriginalMSBuildReferenceHintPath"];
				}
				else {
					if (File.Exists (pref.HintPath)) {
						try {
							var aname = AssemblyName.GetAssemblyName (pref.HintPath);
							if (pref.SpecificVersion) {
								asm = aname.FullName;
							} else {
								asm = aname.Name;
							}
						} catch (Exception ex) {
							string msg = string.Format ("Could not get full name for assembly '{0}'.", pref.Reference);
							monitor.ReportWarning (msg);
							LoggingService.LogError (msg, ex);
						}
					}
					string basePath = Item.ItemDirectory;
					if (pref.ExtendedProperties.Contains ("_OriginalMSBuildReferenceIsAbsolute"))
						basePath = null;
					hintPath = MSBuildProjectService.ToMSBuildPath (basePath, pref.HintPath);
				}
				if (asm == null)
					asm = Path.GetFileNameWithoutExtension (pref.Reference);
				
				buildItem = AddOrGetBuildItem (msproject, oldItems, "Reference", asm, pref.Condition);
				
				if (!pref.SpecificVersion && ReferenceStringHasVersion (asm)) {
					buildItem.SetMetadata ("SpecificVersion", "False");
				} else {
					buildItem.UnsetMetadata ("SpecificVersion");
				}
				
				buildItem.SetMetadata ("HintPath", hintPath);
			}
			else if (pref.ReferenceType == ReferenceType.Package) {
				string include = pref.StoredReference;
				SystemPackage pkg = pref.Package;
				if (pkg != null && pkg.IsFrameworkPackage) {
					int i = include.IndexOf (',');
					if (i != -1)
						include = include.Substring (0, i).Trim ();
				}
				buildItem = AddOrGetBuildItem (msproject, oldItems, "Reference", include, pref.Condition);
				if (!pref.SpecificVersion && ReferenceStringHasVersion (include))
					buildItem.SetMetadata ("SpecificVersion", "False");
				else
					buildItem.UnsetMetadata ("SpecificVersion");
				
				//RequiredTargetFramework is undocumented, maybe only a hint for VS. Only seems to be used for .NETFramework
				var dnp = pref.OwnerProject as DotNetProject;
				IList supportedFrameworks = fmt.SupportedFrameworks;
				if (supportedFrameworks != null && dnp != null && pkg != null
					&& dnp.TargetFramework.Id.Identifier == TargetFrameworkMoniker.ID_NET_FRAMEWORK
					&& pkg.IsFrameworkPackage && supportedFrameworks.Contains (pkg.TargetFramework)
					&& pkg.TargetFramework.Version != "2.0" && supportedFrameworks.Count > 1)
				{
					TargetFramework fx = Runtime.SystemAssemblyService.GetTargetFramework (pkg.TargetFramework);
					buildItem.SetMetadata ("RequiredTargetFramework", fx.Id.Version);
				} else {
					buildItem.UnsetMetadata ("RequiredTargetFramework");
				}
				
				string hintPath = (string) pref.ExtendedProperties ["_OriginalMSBuildReferenceHintPath"];
				if (hintPath != null)
					buildItem.SetMetadata ("HintPath", hintPath);
				else
					buildItem.UnsetMetadata ("HintPath");
			}
			else if (pref.ReferenceType == ReferenceType.Project) {
				Project refProj = Item.ParentSolution != null ? Item.ParentSolution.FindProjectByName (pref.Reference) : null;
				if (refProj != null) {
					buildItem = AddOrGetBuildItem (msproject, oldItems, "ProjectReference", MSBuildProjectService.ToMSBuildPath (Item.ItemDirectory, refProj.FileName), pref.Condition);
					MSBuildProjectHandler handler = refProj.ItemHandler as MSBuildProjectHandler;
					if (handler != null)
						buildItem.SetMetadata ("Project", handler.Item.ItemId, oldValueComparison: StringComparison.OrdinalIgnoreCase);
					else
						buildItem.UnsetMetadata ("Project");
					buildItem.SetMetadata ("Name", refProj.Name);
					if (pref.ReferenceOutputAssembly)
						buildItem.UnsetMetadata ("ReferenceOutputAssembly");
					else
						buildItem.SetMetadata ("ReferenceOutputAssembly", false);
				} else {
					monitor.ReportWarning (GettextCatalog.GetString ("Reference to unknown project '{0}' ignored.", pref.Reference));
					return;
				}
			}
			else {
				// Custom
				DataType dt = ser.DataContext.GetConfigurationDataType (pref.GetType ());
				buildItem = AddOrGetBuildItem (msproject, oldItems, dt.Name, pref.Reference, pref.Condition);
			}

			if (pref.LocalCopy != pref.DefaultLocalCopy)
				buildItem.SetMetadata ("Private", pref.LocalCopy);
			else
				buildItem.UnsetMetadata ("Private");

			WriteBuildItemMetadata (ser, buildItem, pref, oldItems);
			buildItem.Condition = pref.Condition;
		}
		
		void UpdateImports (List<DotNetProjectImport> imports, DotNetProject project, bool addItemTypeImports)
		{
			if (targetImports != null && addItemTypeImports) {
				AddMissingImports (imports, targetImports);
			}

			List <string> updatedImports = imports.Select (import => import.Name).ToList ();
			foreach (IMSBuildImportProvider ip in AddinManager.GetExtensionObjects ("/MonoDevelop/ProjectModel/MSBuildImportProviders")) {
				ip.UpdateImports (EntityItem, updatedImports);
			}

			UpdateImports (imports, updatedImports);

			if (project != null) {
				AddMissingImports (imports, project.ImportsAdded);
				RemoveImports (imports, project.ImportsRemoved);
				project.ImportsSaved ();
			}
		}

		void AddMissingImports (List<DotNetProjectImport> existingImports, IEnumerable<string> newImports)
		{
			AddMissingImports (existingImports, newImports.Select (import => new DotNetProjectImport (import)));
		}

		void AddMissingImports (List<DotNetProjectImport> existingImports, IEnumerable<DotNetProjectImport> newImports)
		{
			foreach (DotNetProjectImport imp in newImports)
				if (!existingImports.Contains (imp))
					existingImports.Add (imp);
		}

		void UpdateImports (List<DotNetProjectImport> existingImports, List<string> updatedImports)
		{
			RemoveMissingImports (existingImports, updatedImports);
			AddMissingImports (existingImports, updatedImports);
		}

		void RemoveMissingImports (List<DotNetProjectImport> existingImports, List<string> updatedImports)
		{
			List <DotNetProjectImport> importsToRemove = existingImports.Where (import => !updatedImports.Contains (import.Name)).ToList ();
			RemoveImports (existingImports, importsToRemove);
		}

		void RemoveImports (List<DotNetProjectImport> existingImports, IEnumerable<DotNetProjectImport> importsToRemove)
		{
			foreach (DotNetProjectImport imp in importsToRemove)
				existingImports.Remove (imp);
		}

		IEnumerable<MSBuildExtension> GetMSBuildExtensions ()
		{
			foreach (var e in AddinManager.GetExtensionObjects<MSBuildExtension> ("/MonoDevelop/ProjectModel/MSBuildExtensions")) {
				e.Handler = this;
				yield return e;
				e.Handler = null;
			}
		}

		void ReadBuildItemMetadata (DataSerializer ser, MSBuildItem buildItem, object dataItem, Type extendedType)
		{
			DataItem ditem = new DataItem ();
			foreach (ItemProperty prop in ser.GetProperties (dataItem)) {
				string name = ToMsbuildItemName (prop.Name);
				if (name == "Include")
					ditem.ItemData.Add (new DataValue ("Include", buildItem.Include));
				else if (buildItem.HasMetadata (name)) {
					string data = buildItem.GetMetadata (name, !prop.DataType.IsSimpleType);
					ditem.ItemData.Add (GetDataNode (prop, data));
				}
			}
			ConvertFromMsbuildFormat (ditem);
			ser.Deserialize (dataItem, ditem);
		}
		
		void WriteBuildItemMetadata (DataSerializer ser, MSBuildItem buildItem, object dataItem, Dictionary<string,ItemInfo> oldItems)
		{
			var notWrittenProps = new HashSet<string> ();
			foreach (ItemProperty prop in ser.GetProperties (dataItem))
				notWrittenProps.Add (prop.Name);
			
			DataItem ditem = (DataItem) ser.Serialize (dataItem, dataItem.GetType ());
			if (ditem.HasItemData) {
				foreach (DataNode node in ditem.ItemData) {
					notWrittenProps.Remove (node.Name);
					if (node.Name == "Include" && node is DataValue)
						buildItem.Include = ((DataValue) node).Value;
					else {
						ConvertToMsbuildFormat (node);
						buildItem.SetMetadata (node.Name, GetXmlString (node), node is DataItem);
					}
				}
			}
			foreach (string prop in notWrittenProps)
				buildItem.UnsetMetadata (prop);
		}
		
		MSBuildItem AddOrGetBuildItem (MSBuildProject msproject, Dictionary<string,ItemInfo> oldItems, string name, string include, string condition)
		{
			ItemInfo itemInfo;
			string key = name + "<" + include + "<" + condition;
			if (oldItems.TryGetValue (key, out itemInfo)) {
				if (!itemInfo.Added) {
					itemInfo.Added = true;
					oldItems [key] = itemInfo;
				}
				return itemInfo.Item;
			} else {
				return msproject.AddNewItem (name, include);
			}
		}
		
		DataItem ReadPropertyGroupMetadata (DataSerializer ser, MSBuildPropertySet propGroup, object dataItem)
		{
			DataItem ditem = new DataItem ();

			foreach (MSBuildProperty bprop in propGroup.Properties) {
				DataNode node = null;
				foreach (XmlNode xnode in bprop.Element.ChildNodes) {
					if (xnode is XmlElement) {
						node = XmlConfigurationReader.DefaultReader.Read ((XmlElement)xnode);
						break;
					}
				}
				if (node == null) {
					node = new DataValue (bprop.Name, bprop.GetValue (false));
				}
				
				ConvertFromMsbuildFormat (node);
				ditem.ItemData.Add (node);
			}
			
			return ditem;
		}
		
		void WritePropertyGroupMetadata (MSBuildPropertySet propGroup, DataCollection itemData, MSBuildSerializer ser, params object[] itemsToReplace)
		{
			var notWrittenProps = new HashSet<string> ();
			
			foreach (object ob in itemsToReplace) {
				if (ob == null)
					continue;
				ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (ob.GetType ());
				foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, ob))
					notWrittenProps.Add (prop.Name);
			}
	
			foreach (DataNode node in itemData) {
				notWrittenProps.Remove (node.Name);
				ConvertToMsbuildFormat (node);

				// In the other msbuild contexts (metadata, solution properties, etc) we TitleCase by default, so the node.Value is TitleCase.
				// However, for property value, we lowercase by default and preserve existing case to reduce noise on VS-created files.
				var boolNode = node as MSBuildBoolDataValue;
				string value;
				bool preserveExistingCase;
				if (boolNode != null) {
					value = boolNode.RawValue? "true" : "false";
					preserveExistingCase = true;
				} else {
					value = GetXmlString (node);
					preserveExistingCase = false;
				}

				propGroup.SetPropertyValue (node.Name, value, preserveExistingCase, node is DataItem);
			}
			foreach (string prop in notWrittenProps)
				propGroup.RemoveProperty (prop);
		}
		
		string ToMsbuildItemName (string name)
		{
			return name.Replace ('.', '-');
		}

		void ConvertToMsbuildFormat (DataNode node)
		{
			ReplaceChar (node, true, '.', '-');
		}
		
		void ConvertFromMsbuildFormat (DataNode node)
		{
			ReplaceChar (node, true, '-', '.');
		}
		
		void ReplaceChar (DataNode node, bool force, char oldChar, char newChar)
		{
			DataItem it = node as DataItem;
			if ((force || it != null) && node.Name != null)
				node.Name = node.Name.Replace (oldChar, newChar);
			if (it != null) {
				foreach (DataNode cnode in it.ItemData)
					ReplaceChar (cnode, !it.UniqueNames, oldChar, newChar);
			}
		}

		List<ConfigData> GetConfigData (MSBuildProject msproject, bool includeGlobalGroups)
		{
			List<ConfigData> configData = new List<ConfigData> ();
			foreach (MSBuildPropertyGroup cgrp in msproject.PropertyGroups) {
				string conf, platform;
				if (ParseConfigCondition (cgrp.Condition, out conf, out platform) || includeGlobalGroups)
					configData.Add (new ConfigData (conf, platform, cgrp));
			}
			return configData;
		}
		
		ConfigData FindPropertyGroup (List<ConfigData> configData, SolutionItemConfiguration config)
		{
			foreach (ConfigData data in configData) {
				if (data.Config == config.Name && data.Platform == config.Platform)
					return data;
			}
			return null;
		}
		
		ProjectFile ReadProjectFile (DataSerializer ser, Project project, MSBuildItem buildItem, Type type)
		{
			string path = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
			ProjectFile file = (ProjectFile) Activator.CreateInstance (type);
			file.Name = path;
			file.BuildAction = buildItem.Name;
			
			ReadBuildItemMetadata (ser, buildItem, file, type);
			
			string dependentFile = buildItem.GetMetadata ("DependentUpon");
			if (!string.IsNullOrEmpty (dependentFile)) {
				dependentFile = MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (path), dependentFile);
				file.DependsOn = dependentFile;
			}
			
			string copyToOutputDirectory = buildItem.GetMetadata ("CopyToOutputDirectory");
			if (!string.IsNullOrEmpty (copyToOutputDirectory)) {
				switch (copyToOutputDirectory) {
				case "None": break;
				case "Always": file.CopyToOutputDirectory = FileCopyMode.Always; break;
				case "PreserveNewest": file.CopyToOutputDirectory = FileCopyMode.PreserveNewest; break;
				default:
					MonoDevelop.Core.LoggingService.LogWarning (
						"Unrecognised value {0} for CopyToOutputDirectory MSBuild property",
						copyToOutputDirectory);
					break;
				}
			}
			
			if (buildItem.GetMetadataIsFalse ("Visible"))
				file.Visible = false;
				
			
			string resourceId = buildItem.GetMetadata ("LogicalName");
			if (!string.IsNullOrEmpty (resourceId))
				file.ResourceId = resourceId;
			
			string contentType = buildItem.GetMetadata ("SubType");
			if (!string.IsNullOrEmpty (contentType))
				file.ContentType = contentType;
			
			string generator = buildItem.GetMetadata ("Generator");
			if (!string.IsNullOrEmpty (generator))
				file.Generator = generator;
			
			string customToolNamespace = buildItem.GetMetadata ("CustomToolNamespace");
			if (!string.IsNullOrEmpty (customToolNamespace))
				file.CustomToolNamespace = customToolNamespace;
			
			string lastGenOutput = buildItem.GetMetadata ("LastGenOutput");
			if (!string.IsNullOrEmpty (lastGenOutput))
				file.LastGenOutput = lastGenOutput;
			
			string link = buildItem.GetMetadata ("Link");
			if (!string.IsNullOrEmpty (link)) {
				if (!Platform.IsWindows)
					link = MSBuildProjectService.UnescapePath (link);
				file.Link = link;
			}
			
			file.Condition = buildItem.Condition;
			return file;
		}

		bool ParseConfigCondition (string cond, out string config, out string platform)
		{
			config = platform = Unspecified;
			int i = cond.IndexOf ("==", StringComparison.Ordinal);
			if (i == -1)
				return false;
			if (cond.Substring (0, i).Trim () == "'$(Configuration)|$(Platform)'") {
				if (!ExtractConfigName (cond.Substring (i + 2), out cond))
					return false;
				i = cond.IndexOf ('|');
				if (i != -1) {
					config = cond.Substring (0, i);
					platform = cond.Substring (i+1);
				} else {
					// Invalid configuration
					return false;
				}
				if (platform == "AnyCPU")
					platform = string.Empty;
				return true;
			}
			else if (cond.Substring (0, i).Trim () == "'$(Configuration)'") {
				if (!ExtractConfigName (cond.Substring (i + 2), out config))
					return false;
				platform = Unspecified;
				return true;
			}
			else if (cond.Substring (0, i).Trim () == "'$(Platform)'") {
				config = Unspecified;
				if (!ExtractConfigName (cond.Substring (i + 2), out platform))
					return false;
				if (platform == "AnyCPU")
					platform = string.Empty;
				return true;
			}
			return false;
		}

		bool ExtractConfigName (string name, out string config)
		{
			config = name.Trim (' ');
			if (config.Length <= 2)
				return false;
			if (config [0] != '\'' || config [config.Length - 1] != '\'')
				return false;
			config = config.Substring (1, config.Length - 2);
			return config.IndexOf ('\'') == -1;
		}
		
		string BuildConfigCondition (string config, string platform)
		{
			if (platform.Length == 0)
				platform = "AnyCPU";
			return " '$(Configuration)|$(Platform)' == '" + config + "|" + platform + "' ";
		}

		bool IsMergeToProjectProperty (ItemProperty prop)
		{
			foreach (object at in prop.CustomAttributes) {
				if (at is MergeToProjectAttribute)
					return true;
			}
			return false;
		}
		
		string GetXmlString (DataNode node)
		{
			if (node is DataValue)
				return ((DataValue)node).Value;
			else {
				StringWriter sw = new StringWriter ();
				XmlTextWriter xw = new XmlTextWriter (sw);
				XmlConfigurationWriter.DefaultWriter.Write (xw, node);
				return sw.ToString ();
			}
		}
		
		DataNode GetDataNode (ItemProperty prop, string xmlString)
		{
			if (prop.DataType.IsSimpleType)
				return new DataValue (prop.Name, xmlString);
			else {
				StringReader sr = new StringReader (xmlString);
				return XmlConfigurationReader.DefaultReader.Read (new XmlTextReader (sr));
			}
		}
		
		internal virtual MSBuildSerializer CreateSerializer ()
		{
			return new MSBuildSerializer (EntityItem.FileName);
		}
		
		static readonly MSBuildElementOrder globalConfigOrder = new MSBuildElementOrder (
			"Configuration","Platform","ProductVersion","SchemaVersion","ProjectGuid", "OutputType",
		    "AppDesignerFolder","RootNamespace","AssemblyName","StartupObject"
		);
		static readonly MSBuildElementOrder configOrder = new MSBuildElementOrder (
			"DebugSymbols","DebugType","Optimize","OutputPath","DefineConstants","ErrorReport","WarningLevel",
		    "TreatWarningsAsErrors","DocumentationFile"
		);

		// Those are properties which are dynamically set by this file format
		
		internal static readonly ItemMember[] ExtendedMSBuildProperties = new ItemMember [] {
			new ItemMember (typeof(SolutionEntityItem), "ProductVersion"),
			new ItemMember (typeof(SolutionEntityItem), "SchemaVersion"),
			new ItemMember (typeof(SolutionEntityItem), "ProjectGuid"),
			new ItemMember (typeof(DotNetProjectConfiguration), "ErrorReport"),
			new ItemMember (typeof(DotNetProjectConfiguration), "TargetFrameworkVersion", new object[] { new MergeToProjectAttribute () }),
			new ItemMember (typeof(ProjectReference), "RequiredTargetFramework"),
			new ItemMember (typeof(Project), "InternalTargetFrameworkVersion", true),
		};
		
		// Items generated by VS but which MD is not using and should be ignored
		
		internal static readonly IList<string> UnsupportedItems = new string[] {
			"BootstrapperFile", "AppDesigner", "WebReferences", "WebReferenceUrl", "Service",
			"ProjectReference", "Reference", // Reference elements are included here because they are special-cased for DotNetProject, and they are unsupported in other types of projects
			"InternalsVisibleTo",
			"InternalsVisibleToTest"
		};
	}
	
	class MSBuildSerializer: DataSerializer
	{
		public DataItem InternalItemProperties = new DataItem ();
		public DataItem ExternalItemProperties = new DataItem ();
		
		public MSBuildSerializer (string baseFile): base (MSBuildProjectService.DataContext)
		{
			// Use windows separators
			SerializationContext.BaseFile = baseFile;
			SerializationContext.DirectorySeparatorChar = '\\';
		}
		
		internal protected override bool CanHandleProperty (ItemProperty prop, SerializationContext serCtx, object instance)
		{
			if (instance is Project) {
				if (prop.Name == "Contents")
					return false;
			}
			if (instance is DotNetProject) {
				if (prop.Name == "References" || prop.Name == "LanguageParameters")
					return false;
			}
			if (instance is SolutionEntityItem) {
				if (prop.IsExtendedProperty (typeof(SolutionEntityItem)))
					return true;
				return prop.Name != "name" && prop.Name != "Configurations";
			}
			if (instance is SolutionFolder) {
				if (prop.Name == "Files")
					return false;
			}
			if (instance is ProjectFile)
				return prop.IsExtendedProperty (typeof(ProjectFile));
			if (instance is ProjectReference)
				return prop.IsExtendedProperty (typeof(ProjectReference)) || prop.Name == "Package" || prop.Name == "Aliases";
			if (instance is DotNetProjectConfiguration)
				if (prop.Name == "CodeGeneration")
					return false;
			if (instance is ItemConfiguration)
				if (prop.Name == "name")
					return false;
			return true;
		}
		
		internal protected override DataNode OnSerializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, object value)
		{
			DataNode data = base.OnSerializeProperty (prop, serCtx, instance, value);
			if (instance is SolutionEntityItem && data != null) {
				if (prop.IsExternal)
					ExternalItemProperties.ItemData.Add (data);
				else
					InternalItemProperties.ItemData.Add (data);
			}
			return data;
		}
	}
	
	class UnknownSolutionItemTypeException : InvalidOperationException
	{
		public UnknownSolutionItemTypeException ()
			: base ("Unknown solution item type")
		{
		}
		
		public UnknownSolutionItemTypeException (string name)
			: base ("Unknown solution item type: " + name)
		{
			this.TypeName = name;
		}
		
		public string TypeName { get; private set; }
	}
	
	class MSBuildElementOrder: Dictionary<string, int>
	{
		public MSBuildElementOrder (params string[] elements)
		{
			for (int n=0; n<elements.Length; n++)
				this [elements [n]] = n;
		}
	}
}
