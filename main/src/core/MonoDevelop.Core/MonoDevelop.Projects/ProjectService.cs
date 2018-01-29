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
using System.Collections.Generic;
using System.IO;
using System.Xml;

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
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	public class ProjectService
	{
		DataContext dataContext = new DataContext ();

		TargetFramework defaultTargetFramework;
		
		string defaultPlatformTarget = "x86";
		static readonly TargetFrameworkMoniker DefaultTargetFrameworkId = TargetFrameworkMoniker.NET_4_7;
		
		public const string BuildTarget = "Build";
		public const string CleanTarget = "Clean";
		
		const string SerializableClassesExtensionPath = "/MonoDevelop/ProjectModel/SerializableClasses";
		const string ProjectBindingsExtensionPath = "/MonoDevelop/ProjectModel/ProjectBindings";
		const string WorkspaceObjectReadersPath = "/MonoDevelop/ProjectModel/WorkspaceObjectReaders";

		internal const string ProjectModelExtensionsPath = "/MonoDevelop/ProjectModel/ProjectModelExtensions";

		internal event EventHandler DataContextChanged;
		
		internal ProjectService ()
		{
			AddinManager.AddExtensionNodeHandler (SerializableClassesExtensionPath, OnSerializableExtensionChanged);
		}
		
		public DataContext DataContext {
			get { return dataContext; }
		}
		
		IEnumerable<WorkspaceObjectReader> GetObjectReaders ()
		{
			return AddinManager.GetExtensionObjects<WorkspaceObjectReader> (WorkspaceObjectReadersPath);
		}

		WorkspaceObjectReader GetObjectReaderForFile (FilePath file, Type type)
		{
			foreach (var r in GetObjectReaders ())
				if (r.CanRead (file, type))
					return r;
			return null;
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

		public async Task<SolutionItem> ReadSolutionItem (ProgressMonitor monitor, string file)
		{
			using (var ctx = new SolutionLoadContext (null))
				return await ReadSolutionItem (monitor, file, null, null, null, ctx);
		}
		
		public Task<SolutionItem> ReadSolutionItem (ProgressMonitor monitor, string file, MSBuildFileFormat format, string typeGuid = null, string itemGuid = null, SolutionLoadContext ctx = null)
		{
			return Runtime.RunInMainThread (async delegate {
				if (!File.Exists (file))
					throw new IOException (GettextCatalog.GetString ("File not found: {0}", file));
				file = Path.GetFullPath (file);
				using (Counters.ReadSolutionItem.BeginTiming ("Read project " + file)) {
					file = GetTargetFile (file);
					var r = GetObjectReaderForFile (file, typeof(SolutionItem));
					if (r == null)
						throw new UnknownSolutionItemTypeException ();
					SolutionItem loadedItem = await r.LoadSolutionItem (monitor, ctx, file, format, typeGuid, itemGuid);
					if (loadedItem != null)
						loadedItem.NeedsReload = false;
					return loadedItem;
				}
			});
		}

		public Task<SolutionFolderItem> ReadSolutionItem (ProgressMonitor monitor, SolutionItemReference reference, params WorkspaceItem[] workspaces)
		{
			return Runtime.RunInMainThread (async delegate {
				if (reference.Id == null) {
					FilePath file = reference.Path.FullPath;
					foreach (WorkspaceItem workspace in workspaces) {
						foreach (SolutionItem eitem in workspace.GetAllItems<Solution>().SelectMany (s => s.GetAllSolutionItems ()))
							if (file == eitem.FileName)
								return eitem;
					}
					return await ReadSolutionItem (monitor, reference.Path);
				} else {
					Solution sol = null;
					if (workspaces.Length > 0) {
						FilePath file = reference.Path.FullPath;
						foreach (WorkspaceItem workspace in workspaces) {
							foreach (Solution item in workspace.GetAllItems<Solution>()) {
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
						sol = await ReadWorkspaceItem (monitor, reference.Path) as Solution;
					
					if (reference.Id == ":root:")
						return sol.RootFolder;
					else
						return sol.GetSolutionItem (reference.Id);
				}
			});
		}

		public Task<WorkspaceItem> ReadWorkspaceItem (ProgressMonitor monitor, FilePath file)
		{
			return Runtime.RunInMainThread (async delegate {
				if (!File.Exists (file))
					throw new UserException (GettextCatalog.GetString ("File not found: {0}", file));
				string fullpath = file.ResolveLinks ().FullPath;
				using (Counters.ReadWorkspaceItem.BeginTiming ("Read solution " + file)) {
					fullpath = GetTargetFile (fullpath);
					var r = GetObjectReaderForFile (file, typeof(WorkspaceItem));
					if (r == null)
						throw new InvalidOperationException ("Invalid file format: " + file);
					WorkspaceItem item = await r.LoadWorkspaceItem (monitor, fullpath);
					if (item != null)
						item.NeedsReload = false;
					else
						throw new InvalidOperationException ("Invalid file format: " + file);
					return item;
				}
			});
		}
		
		public Task<string> Export (ProgressMonitor monitor, string rootSourceFile, string targetPath, MSBuildFileFormat format)
		{
			rootSourceFile = GetTargetFile (rootSourceFile);
			return Export (monitor, rootSourceFile, null, targetPath, format);
		}
		
		public async Task<string> Export (ProgressMonitor monitor, string rootSourceFile, string[] includedChildIds, string targetPath, MSBuildFileFormat format)
		{
			IMSBuildFileObject obj = null;
			
			if (IsWorkspaceItemFile (rootSourceFile)) {
				obj = (await ReadWorkspaceItem (monitor, rootSourceFile)) as IMSBuildFileObject;
			} else if (IsSolutionItemFile (rootSourceFile)) {
				obj = await ReadSolutionItem (monitor, rootSourceFile);
			}
			if (obj == null)
				throw new InvalidOperationException ("File is not a solution or project.");
			using (obj) {
				return await Export (monitor, obj, includedChildIds, targetPath, format);
			}
		}
		
		async Task<string> Export (ProgressMonitor monitor, IMSBuildFileObject obj, string[] includedChildIds, string targetPath, MSBuildFileFormat format)
		{
			string rootSourceFile = obj.FileName;
			string sourcePath = Path.GetFullPath (Path.GetDirectoryName (rootSourceFile));
			targetPath = Path.GetFullPath (targetPath);
			
			if (sourcePath != targetPath) {
				if (!CopyFiles (monitor, obj, obj.GetItemFiles (true), targetPath, true))
					return null;
				
				string newFile = Path.Combine (targetPath, Path.GetFileName (rootSourceFile));
				if (IsWorkspaceItemFile (rootSourceFile))
					obj = (Solution) await ReadWorkspaceItem (monitor, newFile);
				else
					obj = await ReadSolutionItem (monitor, newFile);
				
				using (obj) {
					var oldFiles = obj.GetItemFiles (true).ToList ();
					ExcludeEntries (obj, includedChildIds);
					if (format != null)
						obj.ConvertToFormat (format);
					await obj.SaveAsync (monitor);
					var newFiles = obj.GetItemFiles (true);
					var resolvedTargetPath = new FilePath (targetPath).ResolveLinks ().FullPath;

					foreach (FilePath f in newFiles) {
						if (!f.IsChildPathOf (resolvedTargetPath)) {
							if (obj is Solution)
								monitor.ReportError (GettextCatalog.GetString ("The solution '{0}' is referencing the file '{1}' which is located outside the root solution directory.", obj.Name, f.FileName), null);
							else
								monitor.ReportError (GettextCatalog.GetString ("The project '{0}' is referencing the file '{1}' which is located outside the project directory.", obj.Name, f.FileName), null);
						}
						oldFiles.Remove (f);
					}
	
					// Remove old files
					foreach (FilePath file in oldFiles) {
						if (File.Exists (file)) {
							File.Delete (file);
						
							// Exclude empty directories
							FilePath dir = file.ParentDirectory;
							if (!Directory.EnumerateFileSystemEntries (dir).Any ()) {
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
						obj.ConvertToFormat (format);
					await obj.SaveAsync (monitor);
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
				
				foreach (SolutionFolderItem item in sol.GetAllItems<SolutionFolderItem> ()) {
					if (!childIds.ContainsKey (item.ItemId) && item.ParentFolder != null)
						item.ParentFolder.Items.Remove (item);
				}
			}
		}

		bool CopyFiles (ProgressMonitor monitor, IWorkspaceFileObject obj, IEnumerable<FilePath> files, FilePath targetBasePath, bool ignoreExternalFiles)
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
					if (ignoreExternalFiles)
						continue;
					if (obj is Solution)
						monitor.ReportError (GettextCatalog.GetString ("The solution '{0}' is referencing the file '{1}' which is located outside the root solution directory.", obj.Name, Path.GetFileName (file)), null);
					else
						monitor.ReportError (GettextCatalog.GetString ("The project '{0}' is referencing the file '{1}' which is located outside the project directory.", obj.Name, Path.GetFileName (file)), null);
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
		
		public DotNetProject CreateDotNetProject (string language, params string[] flavorGuids)
		{
			string typeGuid = MSBuildProjectService.GetLanguageGuid (language);
			return (DotNetProject) MSBuildProjectService.CreateProject (typeGuid, flavorGuids);
		}

		public Project CreateProject (string typeGuid, params string[] flavorGuids)
		{
			return MSBuildProjectService.CreateProject (typeGuid, flavorGuids);
		}

		public Project CreateProject (string typeAlias, ProjectCreateInformation info, XmlElement projectOptions, params string[] flavorGuids)
		{
			return MSBuildProjectService.CreateProject (typeAlias, info, projectOptions, flavorGuids);
		}

		public bool CanCreateProject (string typeAlias, params string[] flavorGuids)
		{
			return MSBuildProjectService.CanCreateProject (typeAlias, flavorGuids);
		}

		public bool CanCreateSolutionItem (string typeAlias, ProjectCreateInformation info, XmlElement projectOptions)
		{
			return MSBuildProjectService.CanCreateSolutionItem (typeAlias, info, projectOptions);
		}

		//TODO: find solution that contains the project if possible
		public async Task<Solution> GetWrapperSolution (ProgressMonitor monitor, string filename)
		{
			// First of all, check if a solution with the same name already exists
			
			string solFileName = Path.ChangeExtension (filename, ".sln");
			
			if (File.Exists (solFileName)) {
				return (Solution) await Services.ProjectService.ReadWorkspaceItem (monitor, solFileName);
			}
			else {
				// Create a temporary solution and add the project to the solution
				SolutionItem sitem = await Services.ProjectService.ReadSolutionItem (monitor, filename);
				Solution tempSolution = new Solution ();
				tempSolution.FileName = solFileName;
				tempSolution.ConvertToFormat (sitem.FileFormat);
				tempSolution.RootFolder.Items.Add (sitem);
				tempSolution.CreateDefaultConfigurations ();
				await tempSolution.SaveAsync (monitor);
				return tempSolution;
			}
		}

		public bool FileIsObjectOfType (FilePath file, Type type)
		{
			var filename = GetTargetFile (file);
			return GetObjectReaderForFile (filename, type) != null;
		}

		public bool IsSolutionItemFile (FilePath file)
		{
			return FileIsObjectOfType (file, typeof(SolutionItem));
		}

		public bool IsWorkspaceItemFile (FilePath file)
		{
			return FileIsObjectOfType (file, typeof(WorkspaceItem));
		}
		
		internal void InitializeDataContext (DataContext ctx)
		{
			foreach (DataTypeCodon dtc in AddinManager.GetExtensionNodes (SerializableClassesExtensionPath)) {
				ctx.IncludeType (dtc.Addin, dtc.TypeName, dtc.ItemName);
			}
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

		string GetTargetFile (string file)
		{
			if (!Platform.IsWindows) {
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
	
	internal static class Counters
	{
		public static Counter ItemsInMemory = InstrumentationService.CreateCounter ("Projects in memory", "Project Model");
		public static Counter ItemsLoaded = InstrumentationService.CreateCounter ("Projects loaded", "Project Model");
		public static Counter SolutionsInMemory = InstrumentationService.CreateCounter ("Solutions in memory", "Project Model");
		public static Counter SolutionsLoaded = InstrumentationService.CreateCounter ("Solutions loaded", "Project Model");

		public static TimerCounter ReadWorkspaceItem = InstrumentationService.CreateTimerCounter ("Workspace item read", "Project Model", id:"Projects.WorkspaceItemRead");
		public static TimerCounter ReadSolutionItem = InstrumentationService.CreateTimerCounter ("Solution item read", "Project Model", id:"Projects.SolutionItemRead");
		public static TimerCounter ReadMSBuildProject = InstrumentationService.CreateTimerCounter ("MSBuild project read", "Project Model", id:"Projects.MSBuildProjectRead");
		public static TimerCounter WriteMSBuildProject = InstrumentationService.CreateTimerCounter ("MSBuild project written", "Project Model", id:"Projects.MSBuildProjectWritten");

		public static TimerCounter BuildSolutionTimer = InstrumentationService.CreateTimerCounter ("Build solution", "Project Model", id:"Projects.BuildSolution");
		public static TimerCounter BuildProjectAndReferencesTimer = InstrumentationService.CreateTimerCounter ("Build project and references", "Project Model", id:"Projects.BuildProjectAndReferences");
		public static TimerCounter BuildProjectTimer = InstrumentationService.CreateTimerCounter ("Build project", "Project Model", id:"Projects.BuildProject");
		public static TimerCounter CleanProjectTimer = InstrumentationService.CreateTimerCounter ("Clean project", "Project Model", id:"Projects.CleanProject");
		public static TimerCounter BuildWorkspaceItemTimer = InstrumentationService.CreateTimerCounter ("Build workspace item", "Project Model");
		public static TimerCounter NeedsBuildingTimer = InstrumentationService.CreateTimerCounter ("Check needs building", "Project Model");
		
		public static TimerCounter BuildMSBuildProjectTimer = InstrumentationService.CreateTimerCounter ("Build MSBuild project", "Project Model", id:"Projects.BuildMSBuildProject");
		public static TimerCounter CleanMSBuildProjectTimer = InstrumentationService.CreateTimerCounter ("Clean MSBuild project", "Project Model", id:"Projects.CleanMSBuildProject");
		public static TimerCounter RunMSBuildTargetTimer = InstrumentationService.CreateTimerCounter ("Run MSBuild target", "Project Model", id:"Projects.RunMSBuildTarget");
		public static TimerCounter ResolveMSBuildReferencesTimer = InstrumentationService.CreateTimerCounter ("Resolve MSBuild references", "Project Model", id:"Projects.ResolveMSBuildReferences");

		public static TimerCounter HelpServiceInitialization = InstrumentationService.CreateTimerCounter ("Help Service initialization", "IDE");
		public static TimerCounter ParserServiceInitialization = InstrumentationService.CreateTimerCounter ("Parser Service initialization", "IDE");
	}
}
