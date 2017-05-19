// MSBuildProjectService.cs
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
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using Mono.Addins;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using Cecil = Mono.Cecil;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core.Execution;
using System.Xml.Linq;

namespace MonoDevelop.Projects.MSBuild
{
	public static class MSBuildProjectService
	{
		internal const string ItemTypesExtensionPath = "/MonoDevelop/ProjectModel/MSBuildItemTypes";
		internal const string ImportRedirectsExtensionPath = "/MonoDevelop/ProjectModel/MSBuildImportRedirects";
		internal const string GlobalPropertyProvidersExtensionPath = "/MonoDevelop/ProjectModel/MSBuildGlobalPropertyProviders";
		internal const string UnknownMSBuildProjectTypesExtensionPath = "/MonoDevelop/ProjectModel/UnknownMSBuildProjectTypes";
		internal const string MSBuildProjectItemTypesPath = "/MonoDevelop/ProjectModel/MSBuildProjectItemTypes";
		internal const string MSBuildImportSearchPathsPath = "/MonoDevelop/ProjectModel/MSBuildImportSearchPaths";

		public const string GenericItemGuid = "{9344BDBB-3E7F-41FC-A0DD-8665D75EE146}";
		public const string FolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

		//NOTE: default toolsversion should match the default format.
		// remember to update the builder process' app.config too
		public const string DefaultFormat = "MSBuild12";

		static DataContext dataContext;

		static IMSBuildGlobalPropertyProvider [] globalPropertyProviders;
		static BuilderCache builders = new BuilderCache ();
		static Dictionary<string, Type> genericProjectTypes = new Dictionary<string, Type> ();
		static Dictionary<string, string> importRedirects = new Dictionary<string, string> ();
		static UnknownProjectTypeNode [] unknownProjectTypeNodes;
		static IDictionary<string, TypeExtensionNode> projecItemTypeNodes;

		static Dictionary<TargetRuntime, List<ImportSearchPathExtensionNode>> defaultImportSearchPaths = new Dictionary<TargetRuntime, List<ImportSearchPathExtensionNode>> ();
		static List<ImportSearchPathExtensionNode> importSearchPaths = new List<ImportSearchPathExtensionNode> ();
		static bool searchPathConfigNeedsUpdate;

		static AsyncCriticalSection buildersLock = new AsyncCriticalSection ();


		internal static bool ShutDown { get; private set; }

		static ExtensionNode [] itemTypeNodes;

		public static DataContext DataContext {
			get {
				if (dataContext == null) {
					dataContext = new MSBuildDataContext ();
					Services.ProjectService.InitializeDataContext (dataContext);
				}
				return dataContext;
			}
		}

		static MSBuildProjectService ()
		{
			Services.ProjectService.DataContextChanged += delegate {
				dataContext = null;
			};

			PropertyService.PropertyChanged += HandlePropertyChanged;
			DefaultMSBuildVerbosity = Runtime.Preferences.MSBuildVerbosity;

			Runtime.ShuttingDown += (sender, e) => ShutDown = true;

			AddinManager.ExtensionChanged += OnExtensionChanged;
			LoadExtensionData ();

			specialCharactersEscaped = new Dictionary<char, string> (specialCharacters.Length);
			specialCharactersUnescaped = new Dictionary<string, char> (specialCharacters.Length);
			for (int i = 0; i < specialCharacters.Length; ++i) {
				var escaped = ((int)specialCharacters [i]).ToString ("X");
				specialCharactersEscaped [specialCharacters [i]] = '%' + escaped;
				specialCharactersUnescaped [escaped] = specialCharacters [i];
			}

			CleanCachedMSBuildExes ();
			SetupDotNetCore ();
		}

		static void SetupDotNetCore ()
		{
			if (Platform.IsWindows)
				return;

			// Add .NET Core root directory to PATH. Without this, the MSBuild SDK resolver doesn't work.

			string dotnetDir;
			if (Platform.IsMac)
				dotnetDir = "/usr/local/share/dotnet";
			else
				dotnetDir = "/usr/share/dotnet";
		
			var systemPath = Environment.GetEnvironmentVariable ("PATH");
			if (!systemPath.Split (new char [] { ':' }).Contains (dotnetDir))
				Environment.SetEnvironmentVariable ("PATH", systemPath + ":" + dotnetDir);
		}
	

		static void OnExtensionChanged (object sender, ExtensionEventArgs args)
		{
			if (args.Path == ItemTypesExtensionPath || 
				args.Path == ImportRedirectsExtensionPath || 
				args.Path == UnknownMSBuildProjectTypesExtensionPath || 
				args.Path == GlobalPropertyProvidersExtensionPath ||
				args.Path == MSBuildProjectItemTypesPath)
				LoadExtensionData ();

			if (args.Path == MSBuildImportSearchPathsPath) {
				searchPathConfigNeedsUpdate = true;

				// Reload all builders since search paths have changed
				RecycleAllBuilders ().Ignore ();
			}
		}

		static void LoadExtensionData ()
		{
			// Get global property providers

			if (globalPropertyProviders != null) {
				foreach (var gpp in globalPropertyProviders)
					gpp.GlobalPropertiesChanged -= HandleGlobalPropertyProviderChanged;
			}

			globalPropertyProviders = AddinManager.GetExtensionObjects<IMSBuildGlobalPropertyProvider> (GlobalPropertyProvidersExtensionPath);

			foreach (var gpp in globalPropertyProviders)
				gpp.GlobalPropertiesChanged += HandleGlobalPropertyProviderChanged;

			// Get item type nodes

			itemTypeNodes = AddinManager.GetExtensionNodes<ExtensionNode> (ItemTypesExtensionPath).Concat (Enumerable.Repeat (GenericItemNode.Instance,1)).ToArray ();

			// Get import redirects

			var newImportRedirects = new Dictionary<string, string> ();
			foreach (ImportRedirectTypeNode node in AddinManager.GetExtensionNodes (ImportRedirectsExtensionPath))
				newImportRedirects [node.Project] = node.Addin.GetFilePath (node.Target);
			importRedirects = newImportRedirects;

			// Unknown project type information

			unknownProjectTypeNodes = AddinManager.GetExtensionNodes<UnknownProjectTypeNode> (UnknownMSBuildProjectTypesExtensionPath).ToArray ();

			projecItemTypeNodes = AddinManager.GetExtensionNodes<TypeExtensionNode> (MSBuildProjectItemTypesPath).ToDictionary (e => e.TypeName);
		}

		static Dictionary<string,Type> customProjectItemTypes = new Dictionary<string,Type> ();

		internal static void RegisterCustomProjectItemType (string name, Type type)
		{
			customProjectItemTypes [name] = type;
		}

		internal static void UnregisterCustomProjectItemType (string name)
		{
			customProjectItemTypes.Remove (name);
		}

		/// <summary>
		/// Registers a custom project import search path. This path will be used as a fallback when evaluating
		/// an import and targets file is not found using the value assigned by MSBuild to the property.
		/// </summary>
		/// <param name="propertyName">Name of the property for which to add a fallback path</param>
		/// <param name="path">The fallback path</param>
		public static void RegisterProjectImportSearchPath (string propertyName, FilePath path)
		{
			if (!importSearchPaths.Any (sp => sp.Property == propertyName && sp.Path == path)) {
				importSearchPaths.Add (new ImportSearchPathExtensionNode { Property = propertyName, Path = path });
				searchPathConfigNeedsUpdate = true;
				RecycleAllBuilders ().Ignore ();
			}
		}

		/// <summary>
		/// Unregisters a previously registered import search path
		/// </summary>
		/// <param name="propertyName">Name of the property for which a fallback path was added.</param>
		/// <param name="path">The fallback path to remove</param>
		public static void UnregisterProjectImportSearchPath (string propertyName, FilePath path)
		{
			importSearchPaths.RemoveAll (i => i.Property == propertyName && i.Path == path);
			searchPathConfigNeedsUpdate = true;
			RecycleAllBuilders ().Ignore ();
		}

		/// <summary>
		/// Gets a list of all search paths assigned to properties
		/// </summary>
		/// <returns>The search paths</returns>
		/// <param name="runtime">Runtime for which to get the search paths.</param>
		/// <param name="includeImplicitImports">If set to <c>true</c>, it returns all search paths, including those registered by
		/// MSBuild and those registered using RegisterProjectImportSearchPath. If <c>false</c>, it only returns the paths
		/// registered by RegisterProjectImportSearchPath.</param>
		internal static IEnumerable<ImportSearchPathExtensionNode> GetProjectImportSearchPaths (TargetRuntime runtime, bool includeImplicitImports)
		{
			var result = AddinManager.GetExtensionNodes<ImportSearchPathExtensionNode> (MSBuildImportSearchPathsPath).Concat (importSearchPaths);
			if (includeImplicitImports)
				result = LoadDefaultProjectImportSearchPaths (runtime).Concat (result);
			return result;
		}

		internal static string GetDefaultSdksPath (TargetRuntime runtime)
		{
			string binDir;
			GetNewestInstalledToolsVersion (runtime, true, out binDir);
			return Path.Combine (binDir, "Sdks");
		}

		internal static IEnumerable<SdkInfo> FindRegisteredSdks ()
		{
			foreach (var node in GetProjectImportSearchPaths (null, false).Where (n => n.Property == "MSBuildSDKsPath")) {
				if (Directory.Exists (node.Path)) {
					foreach (var dir in Directory.GetDirectories (node.Path)) {
						if (File.Exists (Path.Combine (dir, "Sdk", "Sdk.props")))
							yield return new SdkInfo (Path.GetFileName (dir), null, Path.Combine (dir, "Sdk"));
					}
				}
			}
		}

		static List<ImportSearchPathExtensionNode> LoadDefaultProjectImportSearchPaths (TargetRuntime runtime)
		{
			// Load the default search paths defined in MSBuild.dll.config

			lock (defaultImportSearchPaths) {
				List<ImportSearchPathExtensionNode> list;
				if (defaultImportSearchPaths.TryGetValue (runtime, out list))
					return list;

				list = new List<ImportSearchPathExtensionNode> ();
				defaultImportSearchPaths [runtime] = list;

				string binDir;
				GetNewestInstalledToolsVersion (runtime, true, out binDir);

				var configFileName = Platform.IsWindows ? "MSBuild.exe.config" : "MSBuild.dll.config";
				var configFile = Path.Combine (binDir, configFileName);
				if (File.Exists (configFile)) {
					var doc = XDocument.Load (configFile);
					var projectImportSearchPaths = doc.Root.Elements ("msbuildToolsets").FirstOrDefault ()?.Elements ("toolset")?.FirstOrDefault ()?.Element ("projectImportSearchPaths");
					if (projectImportSearchPaths != null) {
						var os = Platform.IsMac ? "osx" : Platform.IsWindows ? "windows" : "unix";
						foreach (var searchPaths in projectImportSearchPaths.Elements ("searchPaths")) {
							var pathOs = (string)searchPaths.Attribute ("os")?.Value;
							if (!string.IsNullOrEmpty (pathOs) && pathOs != os)
								continue;
							foreach (var property in searchPaths.Elements ("property"))
								list.Add (new ImportSearchPathExtensionNode { Property = property.Attribute ("name").Value, Path = property.Attribute ("value").Value });
						}
					}
				}
				return list;
			}
		}

		static async void HandleGlobalPropertyProviderChanged (object sender, EventArgs e)
		{
			using (await buildersLock.EnterAsync ()) {
				var gpp = (IMSBuildGlobalPropertyProvider)sender;
				foreach (var builder in builders.GetAllBuilders ())
					await builder.SetGlobalProperties (new Dictionary<string,string> (gpp.GetGlobalProperties ()));
			}
		}

		static void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "MonoDevelop.Ide.MSBuildVerbosity") {
				DefaultMSBuildVerbosity = (MSBuildVerbosity) e.NewValue;
			}
		}

		internal static MSBuildVerbosity DefaultMSBuildVerbosity { get; private set; }

		public static bool IsTargetsAvailable (string targetsPath)
		{
			if (string.IsNullOrEmpty (targetsPath))
				return false;

			string msbuild = Runtime.SystemAssemblyService.CurrentRuntime.GetMSBuildExtensionsPath ();
			Dictionary<string, string> variables = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase) {
				{ "MSBuildExtensionsPath64", msbuild },
				{ "MSBuildExtensionsPath32", msbuild },
				{ "MSBuildExtensionsPath",   msbuild }
			};

			string path = StringParserService.Parse (targetsPath, variables);
			if (Path.DirectorySeparatorChar != '\\')
				path = path.Replace ('\\', Path.DirectorySeparatorChar);

			return File.Exists (path);
		}

		/// <summary>
		/// Given a project referenced in an Import, returns a project that should be loaded instead, or null if there is no redirect
		/// </summary>
		internal static string GetImportRedirect (string project)
		{
			string target;
			if (importRedirects.TryGetValue (project, out target))
				return target;
			return null;
		}

		/// <summary>
		/// Loads a solution item
		/// </summary>
		/// <returns>The item.</returns>
		/// <param name="monitor">Progress monitor</param>
		/// <param name="fileName">File path to the item file</param>
		/// <param name="expectedFormat">File format that the project should have</param>
		/// <param name="typeGuid">Optional item type GUID. If not provided, the type is guessed from the file extension.</param>
		/// <param name="itemGuid">Optional item Id</param>
		/// <param name="ctx">Optional solution context</param>
		public async static Task<SolutionItem> LoadItem (ProgressMonitor monitor, string fileName, MSBuildFileFormat expectedFormat, string typeGuid, string itemGuid, SolutionLoadContext ctx)
		{
			SolutionItem item = null;

			// Find an extension node that can handle this item type
			var node = GetItemTypeNodes ().FirstOrDefault (n => n.CanHandleFile (fileName, typeGuid));

			if (node != null) {
				item = await node.CreateSolutionItem (monitor, ctx, fileName).ConfigureAwait (false);
				if (item == null)
					return null;
			}

			if (item == null) {
				// If it is a known unsupported project, load it as UnknownProject
				item = CreateUnknownSolutionItem (monitor, fileName, typeGuid, typeGuid, ctx);
				if (item == null)
					return null;
			}

			item.EnsureInitialized ();

			item.BeginLoad ();
			ctx.LoadCompleted += delegate {
				item.EndLoad ();
				item.NotifyItemReady ();
			};

			await item.LoadAsync (monitor, fileName, expectedFormat, itemGuid).ConfigureAwait (false);
			return item;
		}

		internal static SolutionItem CreateUnknownSolutionItem (ProgressMonitor monitor, string fileName, string typeGuid, string unknownTypeGuid, SolutionLoadContext ctx)
		{
			bool loadAsProject = false;
			string unsupportedMessage;

			var relPath = ctx != null && ctx.Solution != null ? new FilePath (fileName).ToRelative (ctx.Solution.BaseDirectory).ToString() : new FilePath (fileName).FileName;
			var guids = !string.IsNullOrEmpty (unknownTypeGuid) ? unknownTypeGuid.Split (new char[] {';'}, StringSplitOptions.RemoveEmptyEntries) : new string[0];

			if (!string.IsNullOrEmpty (unknownTypeGuid)) {
				var projectInfo = MSBuildProjectService.GetUnknownProjectTypeInfo (guids, fileName);
				if (projectInfo != null) {
					loadAsProject = projectInfo.LoadFiles;
					unsupportedMessage = projectInfo.GetInstructions ();
					LoggingService.LogWarning (string.Format ("Could not load {0} project '{1}'. {2}", projectInfo.Name, relPath, projectInfo.GetInstructions ()));
					if (!loadAsProject)
						monitor.ReportWarning (GettextCatalog.GetString ("Could not load {0} project '{1}'. {2}", projectInfo.Name, relPath, projectInfo.GetInstructions ()));
				} else {
					unsupportedMessage = GettextCatalog.GetString ("Unknown project type: {0}", unknownTypeGuid);
					LoggingService.LogWarning (string.Format ("Could not load project '{0}' with unknown item type '{1}'", relPath, unknownTypeGuid));
					monitor.ReportWarning (GettextCatalog.GetString ("Could not load project '{0}' with unknown item type '{1}'", relPath, unknownTypeGuid));
					return null;
				}
			} else {
				unsupportedMessage = GettextCatalog.GetString ("Unknown project type");
				LoggingService.LogWarning (string.Format ("Could not load project '{0}' with unknown item type", relPath));
				monitor.ReportWarning (GettextCatalog.GetString ("Could not load project '{0}' with unknown item type", relPath));
			}
			if (loadAsProject) {
				var project = (Project) CreateUninitializedInstance (typeof(UnknownProject));
				project.UnsupportedProjectMessage = unsupportedMessage;
				project.SetCreationContext (Project.CreationContext.Create (typeGuid, new string[0]));
				return project;
			} else
				return null;
		}


		/// <summary>
		/// Creates an uninitialized solution item instance
		/// </summary>
		/// <param name="type">Solution item type</param>
		/// <remarks>
		/// Some subclasses (such as ProjectTypeNode) need to assign some data to
		/// the object before it is initialized. However, by default initialization
		/// is automatically made by the constructor, so to support this scenario
		/// the initialization has to be delayed. This is done by setting the
		/// MonoDevelop.DelayItemInitialization logical context property.
		/// When this property is set, the object is not initialized, and it has
		/// to be manually initialized by calling EnsureInitialized.
		/// </remarks>
		internal static SolutionItem CreateUninitializedInstance (Type type)
		{
			try {
				System.Runtime.Remoting.Messaging.CallContext.LogicalSetData ("MonoDevelop.DelayItemInitialization", true);
				return (SolutionItem)Activator.CreateInstance (type, true);
			} finally {
				System.Runtime.Remoting.Messaging.CallContext.LogicalSetData ("MonoDevelop.DelayItemInitialization", false);
			}
		}

		internal static bool CanCreateSolutionItem (string type, ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			type = ConvertTypeAliasToGuid (type);

			foreach (var node in GetItemTypeNodes ()) {
				if (node.CanCreateSolutionItem (type, info, projectOptions))
					return true;
			}
			return false;
		}

		internal static Project CreateProject (string typeGuid, params string[] flavorGuids)
		{
			flavorGuids = ConvertTypeAliasesToGuids (flavorGuids);

			foreach (var node in GetItemTypeNodes ().OfType<ProjectTypeNode> ()) {
				if (node.Guid.Equals (typeGuid, StringComparison.OrdinalIgnoreCase) || node.TypeAlias == typeGuid) {
					var p = node.CreateProject (flavorGuids);
					p.EnsureInitialized ();
					p.NotifyItemReady ();
					return p;
				}
			}
			throw new InvalidOperationException ("Unknown project type: " + typeGuid);
		}

		internal static bool CanCreateProject (string typeGuid, params string[] flavorGuids)
		{
			return IsKnownTypeGuid (ConvertTypeAliasToGuid (typeGuid)) && ConvertTypeAliasesToGuids (flavorGuids).All (IsKnownFlavorGuid);
		}

		internal static SolutionItem CreateSolutionItem (string type, ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			type = ConvertTypeAliasToGuid (type);

			foreach (var node in GetItemTypeNodes ()) {
				if (node.CanCreateSolutionItem (type, info, projectOptions)) {
					var item = node.CreateSolutionItem (type, info, projectOptions);
					item.NotifyItemReady ();
					return item;
				}
			}
			throw new InvalidOperationException ("Unknown project type: " + type);
		}

		internal static Project CreateProject (string typeGuid, ProjectCreateInformation info, System.Xml.XmlElement projectOptions, params string[] flavorGuids)
		{
			flavorGuids = ConvertTypeAliasesToGuids (flavorGuids);

			foreach (var node in GetItemTypeNodes ().OfType<ProjectTypeNode> ()) {
				if (node.Guid.Equals (typeGuid, StringComparison.OrdinalIgnoreCase) || typeGuid == node.TypeAlias) {
					var p = node.CreateProject (flavorGuids);
					p.EnsureInitialized ();
					p.InitializeFromTemplate (info, projectOptions);
					p.NotifyItemReady ();
					return p;
				}
			}
			throw new InvalidOperationException ("Unknown project type: " + typeGuid);
		}

		public  static string ConvertTypeAliasToGuid (string type)
		{
			var node = GetItemTypeNodes ().FirstOrDefault (n => n.TypeAlias == type);
			if (node != null)
				return node.Guid;
			var enode = WorkspaceObject.GetModelExtensions (null).OfType<SolutionItemExtensionNode> ().FirstOrDefault (n => n.TypeAlias == type);
			if (enode != null)
				return enode.Guid;
			return type;
		}

		public static string[] ConvertTypeAliasesToGuids (string[] types)
		{
			string[] copy = null;
			for (int n=0; n<types.Length; n++) {
				var nt = ConvertTypeAliasToGuid (types[n]);
				if (nt != types[n]) {
					if (copy == null)
						copy = types.ToArray ();
					copy [n] = nt;
				}
			}
			return copy ?? types;
		}

		internal static MSBuildSupport GetMSBuildSupportForFlavors (IEnumerable<string> flavorGuids)
		{
			foreach (var fid in flavorGuids) {
				var node = WorkspaceObject.GetModelExtensions (null).OfType<SolutionItemExtensionNode> ().FirstOrDefault (n => n.Guid != null && n.Guid.Equals (fid, StringComparison.InvariantCultureIgnoreCase));
				if (node != null) {
					if (node.MSBuildSupport != MSBuildSupport.Supported)
						return node.MSBuildSupport;
				} else if (!IsKnownTypeGuid (fid))
					throw new UnknownSolutionItemTypeException (fid);
			}
			return MSBuildSupport.Supported;
		}

		internal static List<SolutionItemExtensionNode> GetMigrableFlavors (string[] flavorGuids)
		{
			var list = new List<SolutionItemExtensionNode> ();
			foreach (var fid in flavorGuids) {
				foreach (var node in WorkspaceObject.GetModelExtensions (null).OfType<SolutionItemExtensionNode> ()) {
					if (node.SupportsMigration && node.Guid != null && node.Guid.Equals (fid, StringComparison.InvariantCultureIgnoreCase))
						list.Add (node);
				}
			}
			return list;
		}

		internal static async Task MigrateFlavors (ProgressMonitor monitor, string fileName, string typeGuid, MSBuildProject p, List<SolutionItemExtensionNode> nodes)
		{
			var language = GetLanguageFromGuid (typeGuid);

			bool migrated = false;

			foreach (var node in nodes) {
				if (await MigrateProject (monitor, node, p, fileName, language))
					migrated = true;
			}
			if (migrated)
				await p.SaveAsync (fileName);
		}

		static async Task<bool> MigrateProject (ProgressMonitor monitor, SolutionItemExtensionNode st, MSBuildProject p, string fileName, string language)
		{
			var projectLoadMonitor = GetProjectLoadProgressMonitor (monitor);
			if (projectLoadMonitor == null) {
				// projectLoadMonitor will be null when running through md-tool, but
				// this is not fatal if migration is not required, so just ignore it. --abock
				if (!st.IsMigrationRequired)
					return false;

				LoggingService.LogError (Environment.StackTrace);
				monitor.ReportError ("Could not open unmigrated project and no migrator was supplied", null);
				throw new UserException ("Project migration failed");
			}
			
			var migrationType = st.MigrationHandler.CanPromptForMigration
				? await st.MigrationHandler.PromptForMigration (projectLoadMonitor, p, fileName, language)
				: projectLoadMonitor.ShouldMigrateProject ();
			if (migrationType == MigrationType.Ignore) {
				if (st.IsMigrationRequired) {
					monitor.ReportError (string.Format ("{1} cannot open the project '{0}' unless it is migrated.", Path.GetFileName (fileName), BrandingService.ApplicationName), null);
					throw new UserException ("The user choose not to migrate the project");
				} else
					return false;
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

			if (!await st.MigrationHandler.Migrate (projectLoadMonitor, p, fileName, language))
				throw new UserException ("Project migration failed");

			return true;
		}

		static ProjectLoadProgressMonitor GetProjectLoadProgressMonitor (ProgressMonitor monitor)
		{
			var projectLoadMonitor = monitor as ProjectLoadProgressMonitor;
			if (projectLoadMonitor != null)
				return projectLoadMonitor;

			var aggregatedMonitor = monitor as AggregatedProgressMonitor;
			if (aggregatedMonitor != null)
				return aggregatedMonitor.LeaderMonitor as ProjectLoadProgressMonitor;

			return null;
		}

		internal static string GetLanguageGuid (string language)
		{
			foreach (var node in GetItemTypeNodes ().OfType<DotNetProjectTypeNode> ()) {
				if (node.Language == language)
					return node.Guid;
			}
			throw new InvalidOperationException ("Language not supported: " + language);
		}

		internal static string GetLanguageFromGuid (string guid)
		{
			foreach (var node in GetItemTypeNodes ().OfType<DotNetProjectTypeNode> ()) {
				if (node.Guid.Equals (guid, StringComparison.OrdinalIgnoreCase))
					return node.Language;
			}
			throw new InvalidOperationException ("Language not supported: " + guid);
		}

		internal static bool IsKnownFlavorGuid (string guid)
		{
			return WorkspaceObject.GetModelExtensions (null).OfType<SolutionItemExtensionNode> ().Any (n => n.Guid != null && n.Guid.Equals (guid, StringComparison.InvariantCultureIgnoreCase));
		}

		internal static bool IsKnownTypeGuid (string guid)
		{
			foreach (var node in GetItemTypeNodes ()) {
				if (node.Guid.Equals (guid, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		internal static string GetTypeGuidFromAlias (string alias)
		{
			foreach (var node in GetItemTypeNodes ()) {
				if (node.TypeAlias.Equals (alias, StringComparison.OrdinalIgnoreCase))
					return node.Guid;
			}
			return null;
		}

		public static void CheckHandlerUsesMSBuildEngine (SolutionFolderItem item, out bool useByDefault, out bool require)
		{
			var handler = item as Project;
			if (handler == null) {
				useByDefault = require = false;
				return;
			}
			useByDefault = handler.MSBuildEngineSupport.HasFlag (MSBuildSupport.Supported);
			require = handler.MSBuildEngineSupport.HasFlag (MSBuildSupport.Required);
		}

		static IEnumerable<SolutionItemTypeNode> GetItemTypeNodes ()
		{
			foreach (ExtensionNode node in itemTypeNodes) {
				if (node is SolutionItemTypeNode)
					yield return (SolutionItemTypeNode) node;
			}
			foreach (var node in customNodes)
				yield return node;
		}

		static List<SolutionItemTypeNode> customNodes = new List<SolutionItemTypeNode> ();

		internal static void RegisterCustomItemType (SolutionItemTypeNode node)
		{
			customNodes.Add (node);
		}
		
		internal static void UnregisterCustomItemType (SolutionItemTypeNode node)
		{
			customNodes.Remove (node);
		}

		internal static bool CanReadFile (FilePath file)
		{
			foreach (SolutionItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleFile (file, null)) {
					return true;
				}
			}
			return GetUnknownProjectTypeInfo (new string[0], file) != null;
		}
		
		internal static string GetExtensionForItem (SolutionItem item)
		{
			foreach (SolutionItemTypeNode node in GetItemTypeNodes ()) {
				if (node.Guid.Equals (item.TypeGuid, StringComparison.OrdinalIgnoreCase))
					return node.Extension;
			}
			// The generic handler should always be found
			throw new InvalidOperationException ();
		}

		internal static string GetTypeGuidForItem (SolutionItem item)
		{
			var className = item.GetType ().FullName;
			foreach (SolutionItemTypeNode node in GetItemTypeNodes ()) {
				if (node.ItemTypeName == className)
					return node.Guid;
			}
			return GenericItemGuid;
		}

		internal static MSBuildSupport GetMSBuildSupportForProject (Project project)
		{
			if (project is UnknownProject)
				return MSBuildSupport.NotSupported;
			
			foreach (var node in GetItemTypeNodes ().OfType<ProjectTypeNode> ()) {
				if (node.Guid.Equals (project.TypeGuid, StringComparison.OrdinalIgnoreCase)) {
					if (node.MSBuildSupport != MSBuildSupport.Supported)
						return node.MSBuildSupport;
					return GetMSBuildSupportForFlavors (project.FlavorGuids);
				}
			}
			return MSBuildSupport.NotSupported;
		}

		public static void RegisterGenericProjectType (string projectId, Type type)
		{
			lock (genericProjectTypes) {
				if (!typeof(Project).IsAssignableFrom (type))
					throw new ArgumentException ("Type is not a subclass of MonoDevelop.Projects.Project");
				genericProjectTypes [projectId] = type;
			}
		}

		internal static Task<SolutionItem> CreateGenericProject (string file)
		{
			if (file == null)
				return Task.FromResult<SolutionItem> (new GenericProject ());

			return Task<SolutionItem>.Factory.StartNew (delegate {
				var t = ReadGenericProjectType (file);
				if (t == null)
					throw new UserException ("Unknown project type");

				var dt = Services.ProjectService.DataContext.GetConfigurationDataType (t);
				if (dt != null) {
					if (!typeof(Project).IsAssignableFrom (dt.ValueType))
						throw new UserException ("Unknown project type: " + t);
					return (SolutionItem)Activator.CreateInstance (dt.ValueType);
				}

				Type type;
				lock (genericProjectTypes) {
					if (!genericProjectTypes.TryGetValue (t, out type))
						throw new UserException ("Unknown project type: " + t);
				}
				return (SolutionItem)Activator.CreateInstance (type);
			});
		}

		static string ReadGenericProjectType (string file)
		{
			using (XmlTextReader tr = new XmlTextReader (file)) {
				tr.MoveToContent ();
				if (tr.LocalName != "Project")
					return null;
				if (tr.IsEmptyElement)
					return null;
				tr.ReadStartElement ();
				tr.MoveToContent ();
				while (tr.LocalName != "PropertyGroup" && !tr.EOF) {
					tr.Skip ();
					tr.MoveToContent ();
				}
				if (tr.LocalName != "PropertyGroup")
					return null;
				if (tr.IsEmptyElement)
					return null;
				tr.ReadStartElement ();
				tr.MoveToContent ();
				while (tr.NodeType != XmlNodeType.EndElement) {
					if (tr.NodeType == XmlNodeType.Element && !tr.IsEmptyElement && tr.LocalName == "ItemType")
						return tr.ReadElementString ();
					tr.Skip ();
				}
				return null;
			}
		}
		
		static char[] specialCharacters = new char [] {'%', '$', '@', '(', ')', '\'', ';', '?' };
		static Dictionary<char, string> specialCharactersEscaped;
		static Dictionary<string, char> specialCharactersUnescaped;
		
		public static string EscapeString (string str)
		{
			int i = str.IndexOfAny (specialCharacters);
			if (i != -1) {
				var sb = new System.Text.StringBuilder ();
				int start = 0;
				while (i != -1) {
					sb.Append (str, start, i - start);
					sb.Append (specialCharactersEscaped [str [i]]);
					if (i >= str.Length)
						break;
					start = i + 1;
					i = str.IndexOfAny (specialCharacters, start);
				}
				if (start < str.Length)
					sb.Append (str, start, str.Length - start);
				return sb.ToString ();
			}
			return str;
		}

		public static string UnescapePath (string path)
		{
			if (string.IsNullOrEmpty (path))
				return path;
			
			if (!Platform.IsWindows)
				path = path.Replace ("\\", "/");
			
			return UnscapeString (path);
		}
		
		public static string UnscapeString (string str)
		{
			int i = str.IndexOf ('%');
			if (i != -1) {
				var sb = new System.Text.StringBuilder ();
				int start = 0;
				while (i != -1) {
					int c;
					char ch;
					var sub = str.Substring (i + 1, 2);
					if (specialCharactersUnescaped.TryGetValue (sub, out ch)) {
						sb.Append (str, start, i - start);
						sb.Append (ch);
					} else if (int.TryParse (sub, NumberStyles.HexNumber, null, out c)) {
						sb.Append (str, start, i - start);
						sb.Append ((char)c);
					}
					start = i + 3;
					i = str.IndexOf ('%', start);
				}
				sb.Append (str, start, str.Length - start);
				return sb.ToString ();
			}
			return str;
		}
		
		public static string ToMSBuildPath (string baseDirectory, string absPath, bool normalize = true)
		{
			if (string.IsNullOrEmpty (absPath))
				return absPath;
			if (baseDirectory != null) {
				absPath = FileService.AbsoluteToRelativePath (baseDirectory, absPath);
				if (normalize)
					absPath = FileService.NormalizeRelativePath (absPath);
			}
			return EscapeString (absPath).Replace ('/', '\\');
		}
		
		internal static string ToMSBuildPathRelative (string baseDirectory, string absPath)
		{
			FilePath file = ToMSBuildPath (baseDirectory, absPath);
			return file.ToRelative (baseDirectory);
		}

		
		internal static string FromMSBuildPathRelative (string basePath, string relPath)
		{
			FilePath file = FromMSBuildPath (basePath, relPath);
			return file.ToRelative (basePath);
		}

		public static string FromMSBuildPath (string basePath, string relPath)
		{
			string res;
			FromMSBuildPath (basePath, relPath, out res);
			return res;
		}
		
		internal static bool IsAbsoluteMSBuildPath (string path)
		{
			if (path.Length > 1 && char.IsLetter (path [0]) && path[1] == ':')
				return true;
			if (path.Length > 0 && path [0] == '\\')
				return true;
			return false;
		}
		
		internal static bool FromMSBuildPath (string basePath, string relPath, out string resultPath)
		{
			resultPath = relPath;
			
			if (string.IsNullOrEmpty (relPath))
				return false;
			
			string path = UnescapePath (relPath);

			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':') {
				if (Platform.IsWindows) {
					resultPath = path; // Return the escaped value
					return true;
				} else
					return false;
			}
			
			bool isRooted = Path.IsPathRooted (path);
			
			if (!isRooted && basePath != null) {
				path = Path.Combine (basePath, path);
				isRooted = Path.IsPathRooted (path);
			}
			
			// Return relative paths as-is, we can't do anything else with them
			if (!isRooted) {
				resultPath = FileService.NormalizeRelativePath (path);
				return true;
			}
			
			// If we're on Windows, don't need to fix file casing.
			//if (Platform.IsWindows) {
				resultPath = FileService.GetFullPath (path);
				return true;
			//}

			// Code below and IF above are commented because after we replaced XBuild with MSBuild .targets files
			// load time of Main.sln(as example) went from 30sec to 2.5min because MSBuild .targets have many more Before/After targets tries
			// resulting in a lot of Directory.EnumerateFileSystemEntries fetching from hard drive resulting in slow load time.
			// Since MSBuild.exe doesn't handle file name case mismatches on case-sensetive file system, it doesn't make sense to do this in IDE.

			//// If the path exists with the exact casing specified, then we're done
			//if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path)){
			//	resultPath = Path.GetFullPath (path);
			//	return true;
			//}

			//// Not on Windows, file doesn't exist. That could mean the project was brought from Windows
			//// and the filename case in the project doesn't match the file on disk, because Windows is
			//// case-insensitive. Since we have an absolute path, search the directory for the file with
			//// the correct case.
			//string[] names = path.Substring (1).Split ('/');
			//string part = "/";
			
			//for (int n=0; n<names.Length; n++) {
			//	IEnumerable<string> entries;

			//	if (names [n] == ".."){
			//		if (part == "/")
			//			return false; // Can go further back. It's not an existing file
			//		part = Path.GetFullPath (part + "/..");
			//		continue;
			//	}
				
			//	entries = Directory.EnumerateFileSystemEntries (part);
				
			//	string fpath = null;
			//	foreach (string e in entries) {
			//		if (string.Compare (Path.GetFileName (e), names [n], StringComparison.OrdinalIgnoreCase) == 0) {
			//			fpath = e;
			//			break;
			//		}
			//	}
			//	if (fpath == null) {
			//		// Part of the path does not exist. Can't do any more checking.
			//		part = Path.GetFullPath (part);
			//		if (n < names.Length)
			//			part += "/" + string.Join ("/", names, n, names.Length - n);
			//		resultPath = part;
			//		return true;
			//	}

			//	part = fpath;
			//}
			//resultPath = Path.GetFullPath (part);
			//return true;
		}

		//Given a filename like foo.it.resx, splits it into - foo, it, resx
		//Returns true only if a valid culture is found
		//Note: hand-written as this can get called lotsa times
		public static bool TrySplitResourceName (string fname, out string only_filename, out string culture, out string extn)
		{
			only_filename = culture = extn = null;

			int last_dot = -1;
			int culture_dot = -1;
			int i = fname.Length - 1;
			while (i >= 0) {
				if (fname [i] == '.') {
					last_dot = i;
					break;
				}
				i --;
			}
			if (i < 0)
				return false;

			i--;
			while (i >= 0) {
				if (fname [i] == '.') {
					culture_dot = i;
					break;
				}
				i --;
			}
			if (culture_dot < 0)
				return false;

			culture = fname.Substring (culture_dot + 1, last_dot - culture_dot - 1);
			if (!CultureNamesTable.ContainsKey (culture))
				return false;

			only_filename = fname.Substring (0, culture_dot);
			extn = fname.Substring (last_dot + 1);
			return true;
		}

		internal static string GetNewestInstalledToolsVersion (TargetRuntime runtime, bool requiresMicrosoftBuild, out string binDir)
		{
			string [] supportedToolsVersions;
			if (requiresMicrosoftBuild || Runtime.Preferences.BuildWithMSBuild || Platform.IsWindows)
				supportedToolsVersions = new [] { "15.0"};
			else
				supportedToolsVersions = new [] { "14.0", "12.0", "4.0" };

			foreach (var toolsVersion in supportedToolsVersions) {
				binDir = runtime.GetMSBuildBinPath (toolsVersion);
				if (binDir != null) {
					return toolsVersion;
				}
			}
			throw new Exception ("Did not find MSBuild for runtime " + runtime.Id);
		}

		/// <summary>
		/// Forces the reload of all project builders
		/// </summary>
		/// <remarks>
		/// This method can be used to discard all currently active project builders, and force the creation
		/// of new ones. This method is useful when there is a change in the MSBuild options or environment
		/// that has an effect on all builders. If a builder is running a task, it will be discarded when
		/// the task ends.
		/// </remarks>
		public static async Task RecycleAllBuilders ()
		{
			using (await buildersLock.EnterAsync ()) {
				foreach (var b in builders.GetAllBuilders ())
					b.Shutdown ();
			}
		}

		internal static async Task<RemoteProjectBuilder> GetProjectBuilder (TargetRuntime runtime, string minToolsVersion, string file, string solutionFile, int customId, bool requiresMicrosoftBuild, bool lockBuilder = false)
		{
			Version mtv = Version.Parse (minToolsVersion);
			if (mtv >= new Version (15,0))
				requiresMicrosoftBuild = true;

			using (await buildersLock.EnterAsync ())
			{
				string binDir;
				var toolsVersion = GetNewestInstalledToolsVersion (runtime, requiresMicrosoftBuild, out binDir);

				Version tv;
				if (Version.TryParse (toolsVersion, out tv) && Version.TryParse (minToolsVersion, out mtv) && tv < mtv) {
					throw new InvalidOperationException (string.Format (
						"Project requires MSBuild ToolsVersion '{0}' which is not supported by runtime '{1}'",
						toolsVersion, runtime.Id)
					);
				}

				//one builder per solution
				string builderKey = runtime.Id + " # " + solutionFile + " # " + customId + " # " + requiresMicrosoftBuild;

				RemoteBuildEngine builder = null;

				if (lockBuilder) {
					foreach (var b in builders.GetBuilders (builderKey)) {
						if (b.IsShuttingDown)
							continue;
						if (b.Lock ()) {
							builder = b;
							break;
						}
						b.Unlock ();
					}
				} else
					builder = builders.GetBuilders (builderKey).FirstOrDefault (b => !b.IsShuttingDown);
				
				if (builder != null) {
					builder.ReferenceCount++;
					return await builder.CreateRemoteProjectBuilder (file).ConfigureAwait (false);
				}

				return await Task.Run (async () => {
					//always start the remote process explicitly, even if it's using the current runtime and fx
					//else it won't pick up the assembly redirects from the builder exe
					var exe = GetExeLocation (runtime, toolsVersion, requiresMicrosoftBuild);
					RemoteProcessConnection connection = null;

					try {
							
						connection = new RemoteProcessConnection (exe, runtime.GetExecutionHandler ());
						await connection.Connect ().ConfigureAwait (false);

						var props = GetCoreGlobalProperties (solutionFile, binDir, toolsVersion);
						foreach (var gpp in globalPropertyProviders) {
							foreach (var e in gpp.GetGlobalProperties ())
								props [e.Key] = e.Value;
						}
						
						await connection.SendMessage (new InitializeRequest {
							IdeProcessId = Process.GetCurrentProcess ().Id,
							BinDir = binDir,
							CultureName = GettextCatalog.UICulture.Name,
							GlobalProperties = props
						}).ConfigureAwait (false);

						builder = new RemoteBuildEngine (connection);

					} catch {
						if (connection != null) {
							try {
								connection.Dispose ();
							} catch {
							}
						}
						throw;
					}

					builders.Add (builderKey, builder);
					builder.ReferenceCount = 1;
					builder.Disconnected += async delegate {
						using (await buildersLock.EnterAsync ().ConfigureAwait (false))
							builders.Remove (builder);
					};
					if (lockBuilder)
						builder.Lock ();
					return await builder.CreateRemoteProjectBuilder (file).ConfigureAwait (false);
				});
			}
		}

		static Dictionary<string,string> GetCoreGlobalProperties (string slnFile, string binDir, string toolsVersion)
		{
			var dictionary = new Dictionary<string,string> ();

			//this causes build targets to behave how they should inside an IDE, instead of in a command-line process
			dictionary.Add ("BuildingInsideVisualStudio", "true");

			//we don't have host compilers in MD, and this is set to true by some of the MS targets
			//which causes it to always run the CoreCompile task if BuildingInsideVisualStudio is also
			//true, because the VS in-process compiler would take care of the deps tracking
			dictionary.Add ("UseHostCompilerIfAvailable", "false" );

			if (string.IsNullOrEmpty (slnFile))
				return dictionary;

			dictionary.Add ("SolutionPath", Path.GetFullPath (slnFile));
			dictionary.Add ("SolutionName", Path.GetFileNameWithoutExtension (slnFile));
			dictionary.Add ("SolutionFilename", Path.GetFileName (slnFile));
			dictionary.Add ("SolutionDir", Path.GetDirectoryName (slnFile) + Path.DirectorySeparatorChar);

			//when running the dev15 MSBuild from commandline or inside MSBuild, it sets "VSToolsPath" correctly. when running from MD, it falls back to a bad default. override it.
			if (Platform.IsWindows) {
				dictionary.Add ("VSToolsPath", Path.GetFullPath (Path.Combine (binDir, "..", "..", "Microsoft", "VisualStudio", "v" + toolsVersion)));
			}

			return dictionary;
		}

#region MSBuild exe file location

		/// <summary>
		/// Gets the project builder exe to be used to for a specific runtime and tools version
		/// </summary>
		static string GetExeLocation (TargetRuntime runtime, string toolsVersion, bool requiresMicrosoftBuild)
		{
			// If the builder for the latest MSBuild tools is being requested, return a local copy of the exe.
			// That local copy is configured to add additional msbuild search paths defined by add-ins.

			var mainExe = GetMSBuildExeLocationInBundle (runtime);
			var exe = GetExeLocationInBundle (runtime, toolsVersion, requiresMicrosoftBuild);
			if (exe == mainExe)
				return GetLocalMSBuildExeLocation (runtime);
			return exe;
		}

		static string GetMSBuildExeLocationInBundle (TargetRuntime runtime)
		{
			return GetExeLocationInBundle (runtime, "15.0", true);
		}

		static string GetExeLocationInBundle (TargetRuntime runtime, string toolsVersion, bool requiresMicrosoftBuild)
		{
			// Locate the project builder exe in the MD directory

			var builderDir = new FilePath (typeof(MSBuildProjectService).Assembly.Location).ParentDirectory.Combine ("MSBuild");

			var version = Version.Parse (toolsVersion);
			bool useMicrosoftBuild = 
				requiresMicrosoftBuild ||
				((version >= new Version (15, 0)) && Runtime.Preferences.BuildWithMSBuild) ||
				(version >= new Version (4, 0) && runtime is MsNetTargetRuntime);

			if (useMicrosoftBuild) {
				toolsVersion = "dotnet." + toolsVersion;
			}

			var exe = builderDir.Combine (toolsVersion, "MonoDevelop.Projects.Formats.MSBuild.exe");
			if (File.Exists (exe))
				return exe;
			
			throw new InvalidOperationException ("Unsupported MSBuild ToolsVersion '" + version + "'");
		}

		static string GetLocalMSBuildExeLocation (TargetRuntime runtime)
		{
			// Gets a path to the local copy of the project builder for the provided runtime.
			// If no local copy exists, create one.

			// Builders are copied to a folder inside the cache folder. This folder is cleaned
			// every time XS is started, removing unused builders. The process id is used
			// as folder name, so it is easy to check if the folder is currently in use or not.

			var dirId = Process.GetCurrentProcess ().Id.ToString () + "_" + runtime.InternalId;
			var exesDir = UserProfile.Current.CacheDir.Combine ("MSBuild").Combine (dirId);
			var originalExe = GetMSBuildExeLocationInBundle (runtime);
			var originalExeConfig = originalExe + ".config";
			var destinationExe = exesDir.Combine (Path.GetFileName (originalExe));
			var destinationExeConfig = destinationExe + ".config";

			var localResolversDir = Path.Combine (exesDir, "SdkResolvers");
			var mdResolverDir = Path.Combine (localResolversDir, "MonoDevelop.MSBuildResolver");
			var mdResolverConfig = Path.Combine (mdResolverDir, "sdks.config");

			string binDir;
			GetNewestInstalledToolsVersion (runtime, true, out binDir);

			if (Platform.IsWindows) {
				// on Windows copy the official MSBuild.exe.config from the VS 2017 install
				// and use this as the starting point
				originalExeConfig = Path.Combine (binDir, "MSBuild.exe.config");
			}

			if (!Directory.Exists (exesDir)) {
				// Copy the builder to the local dir, including the debug file and config file.
				Directory.CreateDirectory (exesDir);
				File.Copy (originalExe, destinationExe);
				var exeMdb = originalExe + ".mdb";
				if (File.Exists (exeMdb))
					File.Copy (exeMdb, exesDir.Combine (Path.GetFileName (exeMdb)));
				var exePdb = Path.ChangeExtension (originalExe, ".pdb");
				if (File.Exists (exePdb))
					File.Copy (exePdb, exesDir.Combine (Path.GetFileName (exePdb)));

				// On Windows we need to copy the MSBuild .dlls locally to the builder directory.
				// The assembly resolve logic in the builder that loads them from the original
				// directory at runtime doesn't work for multiple AppDomains, and so for example
				// WPF MarkupCompilePass1 will fail since it can't load MSBuild binaries into an
				// AppDomain it creates.
				if (Platform.IsWindows) {
					var dlls = Directory.GetFiles (binDir, "*.dll");
					foreach (var dll in dlls) {
						var destination = Path.Combine (exesDir, Path.GetFileName (dll));
						if (!File.Exists (destination))
							File.Copy (dll, destination);
					}
				}

				// Mono has Microsoft.Build.{Tasks,Utilities}.{v4.0,v12.0} assemlies, which are xbuild's
				// implementation, installed in the GAC.
				//
				// With msbuild, we want to use the facade assemblies of the same name, installed with
				// msbuild, which redirect to the corresponding .Core assemblies.
				//
				// We have an AssemblyResolve event handler which resolves some msbuild assemblies from
				// the correct path. But that is fired only if the runtime fails to resolve the assembly, which
				// happens when, for example, MSBuild.dll is requesting Microsoft.Build, 15.1.0.0 .
				//
				// But for the v4.0/v12.0 assemblies, the runtime is able to resolve them from the GAC and
				// so the event handler never gets fired.
				//
				// To ensure that msbuild is able to load the facade assemblies, we copy them over next to
				// the builder. This is temporary though. It can be removed when xbuild is removed from mono,
				// and we can put these in the proper locations in mono (GAC/facades?).
				//
				if (Platform.IsMac || Platform.IsLinux) {
					var assemblies = new string[] {
								"Microsoft.Build.Tasks.v4.0.dll",
								"Microsoft.Build.Utilities.v4.0.dll",
								"Microsoft.Build.Tasks.v12.0.dll",
								"Microsoft.Build.Utilities.v12.0.dll" };

					foreach (var asm in assemblies) {
						var src = Path.Combine(binDir, asm);
						var dest = Path.Combine (exesDir, asm);
						if (File.Exists (src))
							File.Copy (src, dest);
					}
				}

				// Copy the resolvers directory since MSBuild looks for then in a directory relative
				// to MSBuild.dll
				var resolversDir = Path.Combine (binDir, "SdkResolvers");
				if (Directory.Exists (resolversDir))
					FileService.CopyDirectory (resolversDir, localResolversDir);
				else
					Directory.CreateDirectory (localResolversDir);

				// Copy the MonoDevelop resolver, used for sdks registered by add-ins.
				// This resolver will load registered sdks from the file sdks.config
				var builderDir = new FilePath (typeof (MSBuildProjectService).Assembly.Location).ParentDirectory.Combine ("MSBuild");
				Directory.CreateDirectory (mdResolverDir);
				File.Copy (Path.Combine (builderDir, "MonoDevelop.MSBuildResolver.dll"), Path.Combine (mdResolverDir, "MonoDevelop.MSBuildResolver.dll"));

				searchPathConfigNeedsUpdate = true;
			}

			if (searchPathConfigNeedsUpdate) {
				// There is already a local copy of the builder, but the config file needs to be updated.
				searchPathConfigNeedsUpdate = false;
				UpdateMSBuildExeConfigFile (runtime, originalExeConfig, destinationExeConfig, mdResolverConfig, binDir);
			}
			return destinationExe;
		}

		static void UpdateMSBuildExeConfigFile (TargetRuntime runtime, string sourceConfigFile, string destinationConfigFile, string mdResolverConfig, string binDir)
		{
			// Creates an MSBuild config file with the search paths registered by add-ins.

			var doc = XDocument.Load (sourceConfigFile);
			var configuration = doc.Root;

			if (Platform.IsWindows) {
				// we want the config file to have the UseLegacyPathHandling=false switch
				// https://blogs.msdn.microsoft.com/jeremykuhne/2016/06/21/more-on-new-net-path-handling/
				var runtimeElement = configuration.Element ("runtime");
				ConfigFileUtilities.SetOrAppendSubelementAttributeValue (runtimeElement, "AppContextSwitchOverrides", "value", "Switch.System.IO.UseLegacyPathHandling=false");
			}

			var toolset = doc.Root.Elements ("msbuildToolsets").FirstOrDefault ()?.Elements ("toolset")?.FirstOrDefault ();
			if (toolset != null) {
					
				// This is required for MSBuild to properly load the searchPaths element (@radical knows why)
				SetMSBuildConfigProperty (toolset, "MSBuildBinPath", binDir, append: false, insertBefore: true);

				// this must match MSBuildBinPath w/MSBuild15
				SetMSBuildConfigProperty (toolset, "MSBuildToolsPath", binDir, append: false, insertBefore: true);

				if (Platform.IsWindows) {
					var extensionsPath = Path.GetDirectoryName (Path.GetDirectoryName (binDir));
					SetMSBuildConfigProperty (toolset, "MSBuildExtensionsPath", extensionsPath);
					SetMSBuildConfigProperty (toolset, "MSBuildExtensionsPath32", extensionsPath);
					SetMSBuildConfigProperty (toolset, "MSBuildToolsPath", binDir);
					SetMSBuildConfigProperty (toolset, "MSBuildToolsPath32", binDir);

					var sdksPath = Path.Combine (extensionsPath, "Sdks");
					SetMSBuildConfigProperty (toolset, "MSBuildSDKsPath", sdksPath);

					var roslynTargetsPath = Path.Combine (binDir, "Roslyn");
					SetMSBuildConfigProperty (toolset, "RoslynTargetsPath", roslynTargetsPath);

					var vcTargetsPath = Path.Combine (extensionsPath, "Common7", "IDE", "VC", "VCTargets");
					SetMSBuildConfigProperty (toolset, "VCTargetsPath", vcTargetsPath);
				}

				var projectImportSearchPaths = doc.Root.Elements ("msbuildToolsets").FirstOrDefault ()?.Elements ("toolset")?.FirstOrDefault ()?.Element ("projectImportSearchPaths");
				if (projectImportSearchPaths != null) {
					var os = Platform.IsMac ? "osx" : Platform.IsWindows ? "windows" : "unix";
					XElement searchPaths = projectImportSearchPaths.Elements ("searchPaths").FirstOrDefault (sp => sp.Attribute ("os")?.Value == os);
					if (searchPaths == null) {
						searchPaths = new XElement ("searchPaths");
						searchPaths.SetAttributeValue ("os", os);
						projectImportSearchPaths.Add (searchPaths);
					}
					foreach (var path in GetProjectImportSearchPaths (runtime, false))
						SetMSBuildConfigProperty (searchPaths, path.Property, path.Path, append: true, insertBefore: false);
				}
				doc.Save (destinationConfigFile);
			}

			// Update the sdk list for the MD resolver
			SdkInfo.SaveConfig (mdResolverConfig, FindRegisteredSdks ());
		}

		static void SetMSBuildConfigProperty (XElement elem, string name, string value, bool append = false, bool insertBefore = false)
		{
			var prop = elem.Elements ("property").FirstOrDefault (p => p.Attribute ("name")?.Value == name);
			if (prop != null) {
				var val = prop.Attribute ("value")?.Value;
				if (append)
					prop.SetAttributeValue ("value", val + ";" + value);
				else
					prop.SetAttributeValue ("value", value);
			} else {
				prop = new XElement ("property");
				prop.SetAttributeValue ("name", name);
				prop.SetAttributeValue ("value", value);
				if (insertBefore)
					elem.AddFirst (prop);
				else
					elem.Add (prop);
			}
		}

		static void CleanCachedMSBuildExes ()
		{
			// Removes local copies of project builders that are not currently being used.

			var exesDir = UserProfile.Current.CacheDir.Combine ("MSBuild");
			if (!Directory.Exists (exesDir))
				return;
			
			foreach (var dir in Directory.GetDirectories (exesDir)) {
				// The file name has to parts: <process-id>_<runtime-id>
				var spid = Path.GetFileName (dir);
				int i = spid.IndexOf ('_');
				if (i == -1)
					continue;
				spid = spid.Substring (0, i);
				int pid;
				if (int.TryParse (Path.GetFileName (spid), out pid)) {
					try {
						// If there is a process running with this id it means the builder is still being used
						if (Process.GetProcessById (pid) != null)
							continue;
					} catch {
						// Ignore
					}
					// No process for this id, it should be safe to delete the folder
					try {
						Directory.Delete (dir, true);
					} catch (Exception ex) {
						LoggingService.LogError ("Could not delete MSBuild cache folder", ex);
					}
				}
			}
		}

#endregion

		internal static async void ReleaseProjectBuilder (RemoteBuildEngine engine)
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (--engine.ReferenceCount != 0)
					return;
				builders.Remove (engine);
			}
			engine.Dispose ();
		}

		static Dictionary<string, string> cultureNamesTable;
		static Dictionary<string, string> CultureNamesTable {
			get {
				if (cultureNamesTable == null) {
					cultureNamesTable = new Dictionary<string, string> ();
					foreach (CultureInfo ci in CultureInfo.GetCultures (CultureTypes.AllCultures))
						cultureNamesTable [ci.Name] = ci.Name;
				}

				return cultureNamesTable;
			}
		}

		internal static UnknownProjectTypeNode GetUnknownProjectTypeInfo (string[] guids, string fileName = null)
		{
			var ext = fileName != null ? Path.GetExtension (fileName).TrimStart ('.') : null;
			var nodes = unknownProjectTypeNodes.Where (p => guids.Any (p.MatchesGuid) || (ext != null && p.Extension == ext)).ToList ();
			return nodes.FirstOrDefault (n => !n.IsSolvable) ?? nodes.FirstOrDefault (n => n.IsSolvable);
		}

		internal static Type GetProjectItemType (string itemName)
		{
			var node = projecItemTypeNodes.Values.FirstOrDefault (e => e.Id == itemName);
			if (node != null) {
				var t = node.Addin.GetType (node.TypeName, true);
				if (!typeof(ProjectItem).IsAssignableFrom (t))
					throw new InvalidOperationException ("Project item type '" + node.TypeName + "' is not a subclass of ProjectItem");
				return t;
			}
			Type tt;
			if (customProjectItemTypes.TryGetValue (itemName, out tt))
				return tt;
			else
				return null;
		}

		internal static string GetNameForProjectItem (Type type)
		{
			TypeExtensionNode node;
			if (projecItemTypeNodes.TryGetValue (type.FullName, out node))
				return node.Id;

			var r = customProjectItemTypes.FirstOrDefault (k => k.Value == type);
			if (r.Key != null)
				return r.Key;
			return null;
		}
	}
	
	class MSBuildDataContext: DataContext
	{
		protected override DataType CreateConfigurationDataType (Type type)
		{
			if (type == typeof(bool))
				return new MSBuildBoolDataType ();
			else if (type == typeof(bool?))
				return new MSBuildNullableBoolDataType ();
			else
				return base.CreateConfigurationDataType (type);
		}
	}
	
	class MSBuildBoolDataType: PrimitiveDataType
	{
		public MSBuildBoolDataType (): base (typeof(bool))
		{
		}
		
		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new MSBuildBoolDataValue (Name, (bool) value);
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return String.Equals (((DataValue)data).Value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	class MSBuildBoolDataValue : DataValue
	{
		public MSBuildBoolDataValue (string name, bool value)
			: base (name, value ? "True" : "False")
		{
			RawValue = value;
		}

		public bool RawValue { get; private set; }
	}

	class MSBuildNullableBoolDataType: PrimitiveDataType
	{
		public MSBuildNullableBoolDataType (): base (typeof(bool))
		{
		}

		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new MSBuildNullableBoolDataValue (Name, (bool?) value);
		}

		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			var d = (DataValue)data;
			if (string.IsNullOrEmpty (d.Value))
				return (bool?) null;
			return (bool?) String.Equals (d.Value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	class MSBuildNullableBoolDataValue : DataValue
	{
		public MSBuildNullableBoolDataValue (string name, bool? value)
			: base (name, value.HasValue? (value.Value? "True" : "False") : null)
		{
			RawValue = value;
		}

		public bool? RawValue { get; private set; }
	}

	class GenericItemNode : ProjectTypeNode
	{
		public static readonly GenericItemNode Instance = new GenericItemNode ();

		public GenericItemNode ()
		{
			Guid = MSBuildProjectService.GenericItemGuid;
			Extension = "mdproj";
			MSBuildSupport = MSBuildSupport.NotSupported;
			TypeAlias = "GenericProject";
		}

		public override Type ItemType {
			get {
				return typeof (GenericItemFactory);
			}
		}

		class GenericItemFactory: SolutionItemFactory
		{
			public override Task<SolutionItem> CreateItem (string fileName, string typeGuid)
			{
				return MSBuildProjectService.CreateGenericProject (fileName);
			}
		}
	}

	class BuilderCache
	{
		Dictionary<string,List<RemoteBuildEngine>> builders = new Dictionary<string, List<RemoteBuildEngine>> ();

		public void Add (string key, RemoteBuildEngine builder)
		{
			List<RemoteBuildEngine> list;
			if (!builders.TryGetValue (key, out list))
				builders [key] = list = new List<RemoteBuildEngine> ();
			list.Add (builder);
        }

		public IEnumerable<RemoteBuildEngine> GetBuilders (string key)
		{
			List<RemoteBuildEngine> list;
			if (builders.TryGetValue (key, out list))
				return list;
			else
				return Enumerable.Empty<RemoteBuildEngine> ();
		}

		public IEnumerable<RemoteBuildEngine> GetAllBuilders ()
		{
			return builders.Values.SelectMany (r => r);
		}

		public void Remove (RemoteBuildEngine builder)
		{
			foreach (var p in builders) {
				if (p.Value.Remove (builder)) {
					if (p.Value.Count == 0)
						builders.Remove (p.Key);
					return;
                }
            }
        }
	}
}
