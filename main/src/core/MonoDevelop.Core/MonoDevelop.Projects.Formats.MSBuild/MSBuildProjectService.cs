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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public static class MSBuildProjectService
	{
		internal const string ItemTypesExtensionPath = "/MonoDevelop/ProjectModel/MSBuildItemTypes";
		internal const string ImportRedirectsExtensionPath = "/MonoDevelop/ProjectModel/MSBuildImportRedirects";
		internal const string GlobalPropertyProvidersExtensionPath = "/MonoDevelop/ProjectModel/MSBuildGlobalPropertyProviders";
		internal const string UnknownMSBuildProjectTypesExtensionPath = "/MonoDevelop/ProjectModel/UnknownMSBuildProjectTypes";
		internal const string MSBuildProjectItemTypesPath = "/MonoDevelop/ProjectModel/MSBuildProjectItemTypes";

		public const string GenericItemGuid = "{9344BDBB-3E7F-41FC-A0DD-8665D75EE146}";
		public const string FolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
		
		//NOTE: default toolsversion should match the default format.
		// remember to update the builder process' app.config too
		public const string DefaultFormat = "MSBuild12";
		
		static DataContext dataContext;
		
		static IMSBuildGlobalPropertyProvider[] globalPropertyProviders;
		static Dictionary<string,RemoteBuildEngine> builders = new Dictionary<string, RemoteBuildEngine> ();
		static Dictionary<string,Type> genericProjectTypes = new Dictionary<string, Type> ();
		static Dictionary<string,string> importRedirects = new Dictionary<string, string> ();
		static UnknownProjectTypeNode[] unknownProjectTypeNodes;
		static IDictionary<string,TypeExtensionNode> projecItemTypeNodes;

		internal static bool ShutDown { get; private set; }

		static ExtensionNode[] itemTypeNodes;
		
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
		}

		static void OnExtensionChanged (object sender, ExtensionEventArgs args)
		{
			if (args.Path == ItemTypesExtensionPath || 
				args.Path == ImportRedirectsExtensionPath || 
				args.Path == UnknownMSBuildProjectTypesExtensionPath || 
				args.Path == GlobalPropertyProvidersExtensionPath ||
				args.Path == MSBuildProjectItemTypesPath)
				LoadExtensionData ();
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
				gpp.GlobalPropertiesChanged -= HandleGlobalPropertyProviderChanged;

			// Get item type nodes

			itemTypeNodes = AddinManager.GetExtensionNodes<ExtensionNode> (ItemTypesExtensionPath).ToArray ();

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

		static void HandleGlobalPropertyProviderChanged (object sender, EventArgs e)
		{
			lock (builders) {
				var gpp = (IMSBuildGlobalPropertyProvider) sender;
				foreach (var builder in builders.Values)
					builder.SetGlobalProperties (gpp.GetGlobalProperties ());
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
				item = await node.CreateSolutionItem (monitor, ctx, fileName);
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

			await item.LoadAsync (monitor, fileName, expectedFormat);
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
			return IsKnownTypeGuid (ConvertTypeAliasToGuid (typeGuid)) && ConvertTypeAliasesToGuids (flavorGuids).All (id => IsKnownFlavorGuid (id));
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
				var node = WorkspaceObject.GetModelExtensions (null).OfType<SolutionItemExtensionNode> ().FirstOrDefault (n => n.Guid.Equals (fid, StringComparison.InvariantCultureIgnoreCase));
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
					if (node.SupportsMigration && node.Guid.Equals (fid, StringComparison.InvariantCultureIgnoreCase))
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
			var projectLoadMonitor = monitor as ProjectLoadProgressMonitor;
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
			return WorkspaceObject.GetModelExtensions (null).OfType<SolutionItemExtensionNode> ().Any (n => n.Guid.Equals (guid, StringComparison.InvariantCultureIgnoreCase));
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
			return Task<SolutionItem>.Factory.StartNew (delegate {
				var t = ReadGenericProjectType (file);
				if (t == null)
					throw new UserException ("Unknown project type");

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
		
		public static string EscapeString (string str)
		{
			int i = str.IndexOfAny (specialCharacters);
			while (i != -1) {
				str = str.Substring (0, i) + '%' + ((int) str [i]).ToString ("X") + str.Substring (i + 1);
				i = str.IndexOfAny (specialCharacters, i + 3);
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
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i+1, 2), NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char) c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
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
			if (Platform.IsWindows) {
				resultPath = FileService.GetFullPath (path);
				return true;
			}
			
			// If the path exists with the exact casing specified, then we're done
			if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path)){
				resultPath = Path.GetFullPath (path);
				return true;
			}
			
			// Not on Windows, file doesn't exist. That could mean the project was brought from Windows
			// and the filename case in the project doesn't match the file on disk, because Windows is
			// case-insensitive. Since we have an absolute path, search the directory for the file with
			// the correct case.
			string[] names = path.Substring (1).Split ('/');
			string part = "/";
			
			for (int n=0; n<names.Length; n++) {
				string[] entries;

				if (names [n] == ".."){
					if (part == "/")
						return false; // Can go further back. It's not an existing file
					part = Path.GetFullPath (part + "/..");
					continue;
				}
				
				entries = Directory.GetFileSystemEntries (part);
				
				string fpath = null;
				foreach (string e in entries) {
					if (string.Compare (Path.GetFileName (e), names [n], StringComparison.OrdinalIgnoreCase) == 0) {
						fpath = e;
						break;
					}
				}
				if (fpath == null) {
					// Part of the path does not exist. Can't do any more checking.
					part = Path.GetFullPath (part);
					for (; n < names.Length; n++)
						part += "/" + names[n];
					resultPath = part;
					return true;
				}

				part = fpath;
			}
			resultPath = Path.GetFullPath (part);
			return true;
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

		static bool runLocal = false;
		
		internal static RemoteProjectBuilder GetProjectBuilder (TargetRuntime runtime, string minToolsVersion, string file, string solutionFile)
		{
			lock (builders) {
				//attempt to use 12.0 builder first if available
				string toolsVersion = "12.0";
				string binDir = runtime.GetMSBuildBinPath ("12.0");
				if (binDir == null) {
					//fall back to 4.0, we know it's always available
					toolsVersion = "4.0";
				}

				//check the ToolsVersion we found can handle the project
				Version tv, mtv;
				if (Version.TryParse (toolsVersion, out tv) && Version.TryParse (minToolsVersion, out mtv) && tv < mtv) {
					string error = null;
					if (runtime is MsNetTargetRuntime && minToolsVersion == "12.0")
						error = "MSBuild 2013 is not installed. Please download and install it from " +
						"http://www.microsoft.com/en-us/download/details.aspx?id=40760";
					throw new InvalidOperationException (error ?? string.Format (
						"Runtime '{0}' does not have MSBuild '{1}' ToolsVersion installed",
						runtime.Id, toolsVersion)
					);
				}

				//one builder per solution
				string builderKey = runtime.Id + " # " + solutionFile;
				RemoteBuildEngine builder;
				if (builders.TryGetValue (builderKey, out builder)) {
					builder.ReferenceCount++;
					return new RemoteProjectBuilder (file, builder);
				}

				//always start the remote process explicitly, even if it's using the current runtime and fx
				//else it won't pick up the assembly redirects from the builder exe
				var exe = GetExeLocation (runtime, toolsVersion);

				MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();
				var pinfo = new ProcessStartInfo (exe) {
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
				};
				runtime.GetToolsExecutionEnvironment ().MergeTo (pinfo);
				
				Process p = null;

				try {
					IBuildEngine engine;
					if (!runLocal) {
						p = runtime.ExecuteAssembly (pinfo);

						// The builder app will write the build engine reference
						// after reading the process id from the standard input
						ManualResetEvent ev = new ManualResetEvent (false);
						string responseKey = "[MonoDevelop]";
						string sref = null;
						p.ErrorDataReceived += (sender, e) => {
							if (e.Data == null)
								return;

							if (e.Data.StartsWith (responseKey, StringComparison.Ordinal)) {
								sref = e.Data.Substring (responseKey.Length);
								ev.Set ();
							} else
								Console.WriteLine (e.Data);
						};
						p.BeginErrorReadLine ();
						p.StandardInput.WriteLine (Process.GetCurrentProcess ().Id.ToString ());
						if (!ev.WaitOne (TimeSpan.FromSeconds (5)))
							throw new Exception ("MSBuild process could not be started");
						
						byte[] data = Convert.FromBase64String (sref);
						MemoryStream ms = new MemoryStream (data);
						BinaryFormatter bf = new BinaryFormatter ();
						engine = (IBuildEngine)bf.Deserialize (ms);
					} else {
						var asm = System.Reflection.Assembly.LoadFrom (exe);
						var t = asm.GetType ("MonoDevelop.Projects.Formats.MSBuild.BuildEngine");
						engine = (IBuildEngine)Activator.CreateInstance (t);
					}
					engine.SetCulture (GettextCatalog.UICulture);
					engine.SetGlobalProperties (GetCoreGlobalProperties (solutionFile));
					foreach (var gpp in globalPropertyProviders)
						engine.SetGlobalProperties (gpp.GetGlobalProperties ());
					builder = new RemoteBuildEngine (p, engine);
				} catch {
					if (p != null) {
						try {
							p.Kill ();
						} catch {
						}
					}
					throw;
				}

				builders [builderKey] = builder;
				builder.ReferenceCount = 1;
				builder.Disconnected += delegate {
					lock (builders)
						builders.Remove (builderKey);
				};
				return new RemoteProjectBuilder (file, builder);
			}
		}

		static IDictionary<string,string> GetCoreGlobalProperties (string slnFile)
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

			return dictionary;;
		}
		
		static string GetExeLocation (TargetRuntime runtime, string toolsVersion)
		{
			FilePath sourceExe = typeof(MSBuildProjectService).Assembly.Location;

			if ((runtime is MsNetTargetRuntime) && int.Parse (toolsVersion.Split ('.')[0]) >= 4)
				toolsVersion = "dotnet." + toolsVersion;

			var exe = sourceExe.ParentDirectory.Combine ("MSBuild", toolsVersion, "MonoDevelop.Projects.Formats.MSBuild.exe");
			if (File.Exists (exe))
				return exe;
			
			throw new InvalidOperationException ("Unsupported MSBuild ToolsVersion '" + toolsVersion + "'");
		}

		internal static void ReleaseProjectBuilder (RemoteBuildEngine engine)
		{
			lock (builders) {
				if (--engine.ReferenceCount != 0)
					return;
				builders.Remove (builders.First (kvp => kvp.Value == engine).Key);
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
	
	[ExportProjectType (MSBuildProjectService.GenericItemGuid, Extension="mdproj")]
	class GenericItemFactory: SolutionItemFactory
	{
		public override Task<SolutionItem> CreateItem (string fileName, string typeGuid)
		{
			return MSBuildProjectService.CreateGenericProject (fileName);
		}
	}
}
