// 
// ProjectService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Projects.Extensions;
using Mono.Unix;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Projects
{
	public class ProjectService
	{
		DataContext dataContext = new DataContext ();
		ArrayList projectBindings = new ArrayList ();
		ProjectServiceExtension defaultExtensionChain;
		DefaultProjectServiceExtension extensionChainTerminator = new DefaultProjectServiceExtension ();
		
		FileFormatManager formatManager = new FileFormatManager ();
		FileFormat defaultFormat;
		TargetFramework defaultTargetFramework;
		
		string defaultPlatformTarget = "x86";
		public const string DefaultTargetFrameworkId = "3.5";
		
		public const string BuildTarget = "Build";
		public const string CleanTarget = "Clean";
		
		const string FileFormatsExtensionPath = "/MonoDevelop/ProjectModel/FileFormats";
		const string SerializableClassesExtensionPath = "/MonoDevelop/ProjectModel/SerializableClasses";
		const string ExtendedPropertiesExtensionPath = "/MonoDevelop/ProjectModel/ExtendedProperties";
		const string ProjectBindingsExtensionPath = "/MonoDevelop/ProjectModel/ProjectBindings";
		
		internal event EventHandler DataContextChanged;
		
		LocalDataStoreSlot extensionChainSlot;
		
		class ExtensionChainInfo
		{
			public ExtensionContext ExtensionContext;
			public ItemTypeCondition ItemTypeCondition;
			public ProjectLanguageCondition ProjectLanguageCondition;
		}
		
		internal ProjectService ()
		{
			extensionChainSlot = Thread.AllocateDataSlot ();
			AddinManager.AddExtensionNodeHandler (FileFormatsExtensionPath, OnFormatExtensionChanged);
			AddinManager.AddExtensionNodeHandler (SerializableClassesExtensionPath, OnSerializableExtensionChanged);
			AddinManager.AddExtensionNodeHandler (ExtendedPropertiesExtensionPath, OnPropertiesExtensionChanged);
			AddinManager.AddExtensionNodeHandler (ProjectBindingsExtensionPath, OnProjectsExtensionChanged);
			AddinManager.ExtensionChanged += OnExtensionChanged;
			
			defaultFormat = formatManager.GetFileFormat ("MSBuild05");
		}
		
		public DataContext DataContext {
			get { return dataContext; }
		}
		
		public FileFormatManager FileFormats {
			get { return formatManager; }
		}
		
		public ProjectServiceExtension GetExtensionChain (IBuildTarget target)
		{
			ProjectServiceExtension chain;
			if (target != null) {
				ExtensionChainInfo einfo = (ExtensionChainInfo) Thread.GetData (extensionChainSlot);
				if (einfo == null) {
					einfo = new ExtensionChainInfo ();
					ExtensionContext ctx = AddinManager.CreateExtensionContext ();
					einfo.ExtensionContext = ctx;
					einfo.ItemTypeCondition = new ItemTypeCondition (target.GetType ());
					einfo.ProjectLanguageCondition = new ProjectLanguageCondition (target);
					ctx.RegisterCondition ("ItemType", einfo.ItemTypeCondition);
					ctx.RegisterCondition ("ProjectLanguage", einfo.ProjectLanguageCondition);
					Thread.SetData (extensionChainSlot, einfo);
				} else {
					einfo.ItemTypeCondition.ObjType = target.GetType ();
					einfo.ProjectLanguageCondition.TargetProject = target;
				}
				ProjectServiceExtension[] extensions = (ProjectServiceExtension[]) einfo.ExtensionContext.GetExtensionObjects ("/MonoDevelop/ProjectModel/ProjectServiceExtensions", typeof(ProjectServiceExtension));
				chain = CreateExtensionChain (extensions);
			}
			else {
				if (defaultExtensionChain == null) {
					ExtensionContext ctx = AddinManager.CreateExtensionContext ();
					ctx.RegisterCondition ("ItemType", new ItemTypeCondition (typeof(UnknownItem)));
					ctx.RegisterCondition ("ProjectLanguage", new ProjectLanguageCondition (UnknownItem.Instance));
					ProjectServiceExtension[] extensions = (ProjectServiceExtension[]) ctx.GetExtensionObjects ("/MonoDevelop/ProjectModel/ProjectServiceExtensions", typeof(ProjectServiceExtension));
					defaultExtensionChain = CreateExtensionChain (extensions);
				}
				chain = defaultExtensionChain;
				target = UnknownItem.Instance;
			}
			
			if (chain.SupportsItem (target))
				return chain;
			else
				return chain.GetNext (target);
		}
		
		ProjectServiceExtension CreateExtensionChain (ProjectServiceExtension[] extensions)
		{
			var first = new CustomCommandExtension ();
			
			for (int n=0; n<extensions.Length - 1; n++)
				extensions [n].Next = extensions [n + 1];

			if (extensions.Length > 0) {
				extensions [extensions.Length - 1].Next = extensionChainTerminator;
				first.Next = extensions [0];
			} else {
				first.Next = extensionChainTerminator;
			}
			return first;
		}
		
		public string DefaultPlatformTarget {
			get { return defaultPlatformTarget; }
			set { defaultPlatformTarget = value; }
		}

		public TargetFramework DefaultTargetFramework {
			get {
				if (defaultTargetFramework == null)
					defaultTargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (DefaultTargetFrameworkId); 
				return defaultTargetFramework;
			}
			set {
				defaultTargetFramework = value;
			}
		}
		
		public string DefaultFileFormatId {
			get { return defaultFormat.Id; }
			set {
				FileFormat f = FileFormats.GetFileFormat (value);
				if (f != null)
					defaultFormat = f;
				else
					LoggingService.LogError ("Unknown format: " + value);
			}
		}

		public FileFormat DefaultFileFormat {
			get { return defaultFormat; }
		}

		internal FileFormat GetDefaultFormat (object ob)
		{
			if (defaultFormat.CanWrite (ob))
				return defaultFormat;
			FileFormat[] formats = FileFormats.GetFileFormatsForObject (ob);
			if (formats.Length == 0)
				throw new InvalidOperationException ("Can't handle objects of type '" + ob.GetType () + "'");
			return formats [0];
		}
		
		public SolutionEntityItem ReadSolutionItem (IProgressMonitor monitor, string file)
		{
			file = Path.GetFullPath (file);
			using (Counters.ReadSolutionItem.BeginTiming ("Read project " + file)) {
				file = GetTargetFile (file);
				SolutionEntityItem loadedItem = GetExtensionChain (null).LoadSolutionItem (monitor, file, delegate {
					FileFormat format;
					SolutionEntityItem item = ReadFile (monitor, file, typeof(SolutionEntityItem), out format) as SolutionEntityItem;
					if (item != null)
						item.FileFormat = format;
					else
						throw new InvalidOperationException ("Invalid file format: " + file);
					return item;
				});
				loadedItem.NeedsReload = false;
				return loadedItem;
			}
		}
		
		public SolutionItem ReadSolutionItem (IProgressMonitor monitor, SolutionItemReference reference, params WorkspaceItem[] workspaces)
		{
			if (reference.Id == null) {
				FilePath file = reference.Path.FullPath;
				foreach (WorkspaceItem workspace in workspaces) {
					foreach (SolutionEntityItem eitem in workspace.GetAllSolutionItems<SolutionEntityItem> ())
						if (file == eitem.FileName)
							return eitem;
				}
				return ReadSolutionItem (monitor, reference.Path);
			}
			else {
				Solution sol = null;
				if (workspaces.Length > 0) {
					FilePath file = reference.Path.FullPath;
					foreach (WorkspaceItem workspace in workspaces) {
						foreach (Solution item in workspace.GetAllSolutions ()) {
							if (item.FileName.FullPath == file) {
								sol = item;
								break;
							}
						}
						if (sol != null)
							break;
					}
				}
				if (sol == null)
					sol = ReadWorkspaceItem (monitor, reference.Path) as Solution;
				
				if (reference.Id == ":root:")
					return sol.RootFolder;
				else
					return sol.GetSolutionItem (reference.Id);
			}
		}
		
		public WorkspaceItem ReadWorkspaceItem (IProgressMonitor monitor, string file)
		{
			file = Path.GetFullPath (file);
			using (Counters.ReadWorkspaceItem.BeginTiming ("Read solution " + file)) {
				file = GetTargetFile (file);
				WorkspaceItem item = GetExtensionChain (null).LoadWorkspaceItem (monitor, file) as WorkspaceItem;
				if (item != null)
					item.NeedsReload = false;
				else
					throw new InvalidOperationException ("Invalid file format: " + file);
				return item;
			}
		}
		
		internal void InternalWriteSolutionItem (IProgressMonitor monitor, string file, SolutionEntityItem item)
		{
			string newFile = WriteFile (monitor, file, item, null);
			if (newFile != null)
				item.FileName = newFile;
			else
				throw new InvalidOperationException ("FileFormat not provided for solution item '" + item.Name + "'");
		}
		
		internal WorkspaceItem InternalReadWorkspaceItem (string file, IProgressMonitor monitor)
		{
			FileFormat format;
			WorkspaceItem item = ReadFile (monitor, file, typeof(WorkspaceItem), out format) as WorkspaceItem;
			
			if (item == null)
				throw new InvalidOperationException ("Invalid file format: " + file);
			
			if (!item.FormatSet)
				item.ConvertToFormat (format, false);

			return item;
		}
		
		internal void InternalWriteWorkspaceItem (IProgressMonitor monitor, string file, WorkspaceItem item)
		{
			string newFile = WriteFile (monitor, file, item, item.FileFormat);
			if (newFile != null)
				item.FileName = newFile;
			else
				throw new InvalidOperationException ("FileFormat not provided for workspace item '" + item.Name + "'");
		}
		
		object ReadFile (IProgressMonitor monitor, string file, Type expectedType, out FileFormat format)
		{
			FileFormat[] formats = formatManager.GetFileFormats (file, expectedType);

			if (formats.Length == 0)
				throw new InvalidOperationException ("Unknown file format: " + file);
			
			format = formats [0];
			object obj = format.Format.ReadFile (file, expectedType, monitor);
			if (obj == null)
				throw new InvalidOperationException ("Invalid file format: " + file);

			return obj;
		}
		
		string WriteFile (IProgressMonitor monitor, string file, object item, FileFormat format)
		{
			if (format == null) {
				if (defaultFormat.CanWrite (item))
					format = defaultFormat;
				else {
					FileFormat[] formats = formatManager.GetFileFormatsForObject (item);
					format = formats.Length > 0 ? formats [0] : null;
				}
				
				if (format == null)
					return null;
				file = format.GetValidFileName (item, file);
			}
			
			if (!FileService.RequestFileEdit (file))
				throw new UserException (GettextCatalog.GetString ("The project could not be saved"), GettextCatalog.GetString ("Write permission has not been granted for file '{0}'", file));
			
			format.Format.WriteFile (file, item, monitor);
			return file;
		}
		
		public string Export (IProgressMonitor monitor, string rootSourceFile, string targetPath, FileFormat format)
		{
			rootSourceFile = GetTargetFile (rootSourceFile);
			return Export (monitor, rootSourceFile, null, targetPath, format);
		}
		
		public string Export (IProgressMonitor monitor, string rootSourceFile, string[] includedChildIds, string targetPath, FileFormat format)
		{
			IWorkspaceFileObject obj;
			
			if (IsWorkspaceItemFile (rootSourceFile)) {
				obj = ReadWorkspaceItem (monitor, rootSourceFile) as Solution;
			} else {
				obj = ReadSolutionItem (monitor, rootSourceFile);
				if (obj == null)
					throw new InvalidOperationException ("File is not a solution or project.");
			}
			using (obj) {
				return Export (monitor, obj, includedChildIds, targetPath, format);
			}
		}
		
		string Export (IProgressMonitor monitor, IWorkspaceFileObject obj, string[] includedChildIds, string targetPath, FileFormat format)
		{
			string rootSourceFile = obj.FileName;
			string sourcePath = Path.GetFullPath (Path.GetDirectoryName (rootSourceFile));
			targetPath = Path.GetFullPath (targetPath);
			
			if (sourcePath != targetPath) {
				if (!CopyFiles (monitor, obj, obj.GetItemFiles (true), targetPath))
					return null;
				
				string newFile = Path.Combine (targetPath, Path.GetFileName (rootSourceFile));
				if (IsWorkspaceItemFile (rootSourceFile))
					obj = ReadWorkspaceItem (monitor, newFile);
				else
					obj = (SolutionEntityItem) ReadSolutionItem (monitor, newFile);
				
				using (obj) {
					List<FilePath> oldFiles = obj.GetItemFiles (true);
					ExcludeEntries (obj, includedChildIds);
					if (format != null)
						obj.ConvertToFormat (format, true);
					obj.Save (monitor);
					List<FilePath> newFiles = obj.GetItemFiles (true);
	
					// Remove old files
					foreach (FilePath file in oldFiles) {
						if (newFiles.Contains (file))
							continue;
						
						if (File.Exists (file)) {
							File.Delete (file);
						
							// Exclude empty directories
							FilePath dir = file.ParentDirectory;
							if (Directory.GetFiles (dir).Length == 0 && Directory.GetDirectories (dir).Length == 0) {
								try {
									Directory.Delete (dir);
								} catch (Exception ex) {
									monitor.ReportError (null, ex);
								}
							}
						}
					}
					return obj.FileName;
				}
			}
			else {
				using (obj) {
					ExcludeEntries (obj, includedChildIds);
					if (format != null)
						obj.ConvertToFormat (format, true);
					obj.Save (monitor);
					return obj.FileName;
				}
			}
		}
		
		void ExcludeEntries (IWorkspaceFileObject obj, string[] includedChildIds)
		{
			Solution sol = obj as Solution;
			if (sol != null && includedChildIds != null) {
				// Remove items not to be exported.
				
				Dictionary<string,string> childIds = new Dictionary<string,string> ();
				foreach (string it in includedChildIds)
					childIds [it] = it;
				
				foreach (SolutionItem item in sol.GetAllSolutionItems<SolutionItem> ()) {
					if (!childIds.ContainsKey (item.ItemId) && item.ParentFolder != null)
						item.ParentFolder.Items.Remove (item);
				}
			}
		}

		bool CopyFiles (IProgressMonitor monitor, IWorkspaceFileObject obj, List<FilePath> files, FilePath targetBasePath)
		{
			FilePath baseDir = obj.BaseDirectory.FullPath;
			foreach (FilePath file in files) {

				if (!File.Exists (file)) {
					monitor.ReportWarning (GettextCatalog.GetString ("File '{0}' not found.", file));
					continue;
				}
				FilePath fname = file.FullPath;
				
				// Can't export files from outside the root solution directory
				if (!fname.IsChildPathOf (baseDir)) {
					if (obj is Solution)
						monitor.ReportError ("The solution '" + obj.Name + "' is referencing the file '" + Path.GetFileName (file) + "' which is located outside the root solution directory.", null);
					else
						monitor.ReportError ("The project '" + obj.Name + "' is referencing the file '" + Path.GetFileName (file) + "' which is located outside the project directory.", null);
					return false;
				}

				FilePath rpath = fname.ToRelative (baseDir);
				rpath = rpath.ToAbsolute (targetBasePath);
				
				if (!Directory.Exists (rpath.ParentDirectory))
					Directory.CreateDirectory (rpath.ParentDirectory);

				File.Copy (file, rpath, true);
			}
			return true;
		}
		
		public bool CanCreateSingleFileProject (string file)
		{
			foreach (ProjectBindingCodon projectBinding in projectBindings) {
				if (projectBinding.ProjectBinding.CanCreateSingleFileProject (file))
					return true;
			}
			return false;
		}
		
		public Project CreateSingleFileProject (string file)
		{
			foreach (ProjectBindingCodon projectBinding in projectBindings) {
				if (projectBinding.ProjectBinding.CanCreateSingleFileProject (file)) {
					return projectBinding.ProjectBinding.CreateSingleFileProject (file);
				}
			}
			return null;
		}
		
		public Project CreateProject (string type, ProjectCreateInformation info, XmlElement projectOptions)
		{
			foreach (ProjectBindingCodon projectBinding in projectBindings) {
				if (projectBinding.ProjectBinding.Name == type) {
					Project project = projectBinding.ProjectBinding.CreateProject (info, projectOptions);
					return project;
				}
			}
			return null;
		}
		
		public Solution GetWrapperSolution (IProgressMonitor monitor, string filename)
		{
			// First of all, check if a solution with the same name already exists
			
			FileFormat[] formats = Services.ProjectService.FileFormats.GetFileFormats (filename, typeof(SolutionEntityItem));
			if (formats.Length == 0)
				formats = new FileFormat [] { DefaultFileFormat };
			
			Solution tempSolution = new Solution ();
			
			FileFormat solutionFileFormat;
			if (formats [0].CanWrite (tempSolution))
				solutionFileFormat = formats [0];
			else
				solutionFileFormat = MonoDevelop.Projects.Formats.MD1.MD1ProjectService.FileFormat;
			
			string solFileName = solutionFileFormat.GetValidFileName (tempSolution, filename);
			
			if (File.Exists (solFileName)) {
				return (Solution) Services.ProjectService.ReadWorkspaceItem (monitor, solFileName);
			}
			else {
				// Create a temporary solution and add the project to the solution
				tempSolution.SetLocation (Path.GetDirectoryName (filename), Path.GetFileNameWithoutExtension (filename));
				SolutionEntityItem sitem = Services.ProjectService.ReadSolutionItem (monitor, filename);
				tempSolution.ConvertToFormat (solutionFileFormat, false);
				tempSolution.RootFolder.Items.Add (sitem);
				tempSolution.CreateDefaultConfigurations ();
				tempSolution.Save (monitor);
				return tempSolution;
			}
		}
		
		public bool IsSolutionItemFile (string filename)
		{
			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;
			filename = GetTargetFile (filename);
			return GetExtensionChain (null).IsSolutionItemFile (filename);
		}
		
		public bool IsWorkspaceItemFile (string filename)
		{
			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;
			filename = GetTargetFile (filename);
			return GetExtensionChain (null).IsWorkspaceItemFile (filename);
		}
		
		internal bool IsSolutionItemFileInternal (string filename)
		{
			return formatManager.GetFileFormats (filename, typeof(SolutionItem)).Length > 0;
		}
		
		internal bool IsWorkspaceItemFileInternal (string filename)
		{
			return formatManager.GetFileFormats (filename, typeof(WorkspaceItem)).Length > 0;
		}
		
		internal void InitializeDataContext (DataContext ctx)
		{
			foreach (DataTypeCodon dtc in AddinManager.GetExtensionNodes (SerializableClassesExtensionPath)) {
				ctx.IncludeType (dtc.Addin, dtc.TypeName, dtc.ItemName);
			}
			foreach (ItemPropertyCodon cls in AddinManager.GetExtensionNodes (ExtendedPropertiesExtensionPath)) {
				ctx.RegisterProperty (cls.Addin, cls.TypeName, cls.PropertyName, cls.PropertyTypeName, cls.External, cls.SkipEmpty);
			}
		}

		void OnFormatExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			FileFormatNode node = (FileFormatNode) args.ExtensionNode;
			if (args.Change == ExtensionChange.Add)
				formatManager.RegisterFileFormat ((IFileFormat) args.ExtensionObject, node.Id, node.Name);
			else
				formatManager.UnregisterFileFormat ((IFileFormat) args.ExtensionObject);
		}
		
		void OnSerializableExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				DataTypeCodon t = (DataTypeCodon) args.ExtensionNode;
				DataContext.IncludeType (t.Addin, t.TypeName, t.ItemName);
			}
			// Types can't be excluded from a DataContext, but that's not a big problem anyway
			
			if (DataContextChanged != null)
				DataContextChanged (this, EventArgs.Empty);
		}
		
		void OnPropertiesExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				ItemPropertyCodon cls = (ItemPropertyCodon) args.ExtensionNode;
				DataContext.RegisterProperty (cls.Addin, cls.TypeName, cls.PropertyName, cls.PropertyTypeName, cls.External, cls.SkipEmpty);
			}
			else {
				ItemPropertyCodon cls = (ItemPropertyCodon) args.ExtensionNode;
				DataContext.UnregisterProperty (cls.Addin, cls.TypeName, cls.PropertyName);
			}
			
			if (DataContextChanged != null)
				DataContextChanged (this, EventArgs.Empty);
		}
		
		void OnProjectsExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				projectBindings.Add (args.ExtensionNode);
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			if (args.PathChanged ("/MonoDevelop/ProjectModel/ProjectServiceExtensions"))
				defaultExtensionChain = null;
		}
		
		string GetTargetFile (string file)
		{
			if (!PropertyService.IsWindows) {
				try {
					UnixSymbolicLinkInfo fi = new UnixSymbolicLinkInfo (file);
					if (fi.IsSymbolicLink)
						return fi.ContentsPath;
				} catch {
				}
			}
			return file;
		}
	}
	
	internal class DefaultProjectServiceExtension: ProjectServiceExtension
	{
		Dictionary <SolutionItem,bool> needsBuildingCache;
		
		public override object GetService (IBuildTarget item, Type type)
		{
			return null;
		}
		
		public override void Save (IProgressMonitor monitor, SolutionEntityItem entry)
		{
			entry.OnSave (monitor);
		}
		
		public override void Save (IProgressMonitor monitor, WorkspaceItem entry)
		{
			entry.OnSave (monitor);
		}

		public override List<FilePath> GetItemFiles (SolutionEntityItem entry, bool includeReferencedFiles)
		{
			return entry.OnGetItemFiles (includeReferencedFiles);
		}
		
		public override bool IsSolutionItemFile (string filename)
		{
			return Services.ProjectService.IsSolutionItemFileInternal (filename);
		}
		
		public override bool IsWorkspaceItemFile (string filename)
		{
			return Services.ProjectService.IsWorkspaceItemFileInternal (filename);
		}
		
		internal override SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName, ItemLoadCallback callback)
		{
			return callback (monitor, fileName);
		}
		
		public override WorkspaceItem LoadWorkspaceItem (IProgressMonitor monitor, string fileName)
		{
			return Services.ProjectService.InternalReadWorkspaceItem (fileName, monitor);
		}
		
		public override BuildResult RunTarget (IProgressMonitor monitor, IBuildTarget item, string target, ConfigurationSelector configuration)
		{
			BuildResult res;
			if (item is WorkspaceItem) {
				res = ((WorkspaceItem)item).OnRunTarget (monitor, target, configuration);
			}
			else if (item is SolutionItem)
				res = ((SolutionItem)item).OnRunTarget (monitor, target, configuration);
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
			
			if (res != null)
				res.SourceTarget = item;
			return res;
		}

		public override void Execute (IProgressMonitor monitor, IBuildTarget item, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem) {
				SolutionEntityItem entry = (SolutionEntityItem) item;
				SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Execute)) {
					conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.Execute, context, configuration);
					return;
				}
				entry.OnExecute (monitor, context, configuration);
			}
			else if (item is WorkspaceItem) {
				((WorkspaceItem)item).OnExecute (monitor, context, configuration);
			}
			else if (item is SolutionItem)
				((SolutionItem)item).OnExecute (monitor, context, configuration);
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
		}
		
		public override bool CanExecute (IBuildTarget item, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem) {
				SolutionEntityItem entry = (SolutionEntityItem) item;
				SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Execute))
					return conf.CustomCommands.CanExecute (CustomCommandType.Execute, context, configuration);
				return entry.OnGetCanExecute (context, configuration);
			}
			else if (item is WorkspaceItem) {
				return ((WorkspaceItem)item).OnGetCanExecute (context, configuration);
			}
			else if (item is SolutionItem)
				return ((SolutionItem)item).OnGetCanExecute (context, configuration);
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
		}
		
		public override bool GetNeedsBuilding (IBuildTarget item, ConfigurationSelector configuration)
		{
			if (item is SolutionItem) {
				SolutionItem entry = (SolutionItem) item;
				// This is a cache to avoid unneeded recursive calls to GetNeedsBuilding.
				bool cleanCache = false;
				if (needsBuildingCache == null) {
					needsBuildingCache = new Dictionary <SolutionItem,bool> ();
					cleanCache = true;
				} else {
					bool res;
					if (needsBuildingCache.TryGetValue (entry, out res))
						return res;
				}
				
				bool nb = entry.OnGetNeedsBuilding (configuration);
				
				needsBuildingCache [entry] = nb;
				if (cleanCache)
					needsBuildingCache = null;
				return nb;
			}
			else if (item is WorkspaceItem) {
				return ((WorkspaceItem)item).OnGetNeedsBuilding (configuration);
			}
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
		}
		
		public override void SetNeedsBuilding (IBuildTarget item, bool val, ConfigurationSelector configuration)
		{
			if (item is SolutionItem) {
				SolutionItem entry = (SolutionItem) item;
				entry.OnSetNeedsBuilding (val, configuration);
			}
			else if (item is WorkspaceItem) {
				((WorkspaceItem)item).OnSetNeedsBuilding (val, configuration);
			}
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
		}

		internal override BuildResult Compile(IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData, ItemCompileCallback callback)
		{
			return callback (monitor, item, buildData);
		}
	}	
	
	internal static class Counters
	{
		public static Counter ItemsInMemory = InstrumentationService.CreateCounter ("Projects in memory", "Project Model");
		public static Counter ItemsLoaded = InstrumentationService.CreateCounter ("Projects loaded", "Project Model");
		public static Counter SolutionsInMemory = InstrumentationService.CreateCounter ("Solutions in memory", "Project Model");
		public static Counter SolutionsLoaded = InstrumentationService.CreateCounter ("Solutions loaded", "Project Model");
		public static TimerCounter ReadWorkspaceItem = InstrumentationService.CreateTimerCounter ("Workspace item read", "Project Model");
		public static TimerCounter ReadSolutionItem = InstrumentationService.CreateTimerCounter ("Solution item read", "Project Model");
		public static TimerCounter ReadMSBuildProject = InstrumentationService.CreateTimerCounter ("MSBuild project read", "Project Model");
		public static TimerCounter WriteMSBuildProject = InstrumentationService.CreateTimerCounter ("MSBuild project written", "Project Model");
		public static TimerCounter BuildSolutionTimer = InstrumentationService.CreateTimerCounter ("Solution built", "Project Model");
		public static TimerCounter BuildProjectTimer = InstrumentationService.CreateTimerCounter ("Project built", "Project Model");
		public static TimerCounter BuildWorkspaceItemTimer = InstrumentationService.CreateTimerCounter ("Workspace item built", "Project Model");
		public static TimerCounter NeedsBuildingTimer = InstrumentationService.CreateTimerCounter ("Needs building checked", "Project Model");
		
		public static Counter TypeIndexEntries = InstrumentationService.CreateCounter ("Type index entries", "Parser Service");
		public static Counter LiveTypeObjects = InstrumentationService.CreateCounter ("Live type objects", "Parser Service");
		public static Counter LiveDatabases = InstrumentationService.CreateCounter ("Parser databases", "Parser Service");
		public static Counter LiveAssemblyDatabases = InstrumentationService.CreateCounter ("Assembly databases", "Parser Service");
		public static Counter LiveProjectDatabases = InstrumentationService.CreateCounter ("Project databases", "Parser Service");
		public static TimerCounter DatabasesRead = InstrumentationService.CreateTimerCounter ("Parser database read", "Parser Service");
		public static TimerCounter DatabasesWritten = InstrumentationService.CreateTimerCounter ("Parser database written", "Parser Service");
		public static TimerCounter FileParse = InstrumentationService.CreateTimerCounter ("File parsed", "Parser Service");
		public static TimerCounter AssemblyParseTime = InstrumentationService.CreateTimerCounter ("Assembly parsed", "Parser Service");
		
		public static TimerCounter HelpServiceInitialization = InstrumentationService.CreateTimerCounter ("Help Service initialization", "IDE");
		public static TimerCounter ParserServiceInitialization = InstrumentationService.CreateTimerCounter ("Parser Service initialization", "IDE");
	}
}
