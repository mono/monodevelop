//  ProjectService.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	
	public enum BeforeCompileAction {
		Nothing,
		SaveAllFiles,
		PromptForSave,
	}
	
	public class ProjectService
	{
		DataContext dataContext = new DataContext ();
		ArrayList projectBindings = new ArrayList ();
		ProjectServiceExtension defaultExtension = new DefaultProjectServiceExtension ();
		ProjectServiceExtension extensionChain = new CustomCommandExtension ();
		
		FileFormatManager formatManager = new FileFormatManager ();
		FileFormat defaultFormat;
		
		public const string BuildTarget = "Build";
		public const string CleanTarget = "Clean";
		public const string DefaultConfiguration = null;
		
		const string ProjectFileFormatsExtensionPath = "/MonoDevelop/ProjectModel/ProjectFileFormats";
		const string FileFormatsExtensionPath = "/MonoDevelop/ProjectModel/FileFormats";
		const string SerializableClassesExtensionPath = "/MonoDevelop/ProjectModel/SerializableClasses";
		const string ExtendedPropertiesExtensionPath = "/MonoDevelop/ProjectModel/ExtendedProperties";
		const string ProjectBindingsExtensionPath = "/MonoDevelop/ProjectModel/ProjectBindings";
		
		internal event EventHandler DataContextChanged;
		
		internal ProjectService ()
		{
			AddinManager.AddExtensionNodeHandler (FileFormatsExtensionPath, OnFormatExtensionChanged);
			AddinManager.AddExtensionNodeHandler (SerializableClassesExtensionPath, OnSerializableExtensionChanged);
			AddinManager.AddExtensionNodeHandler (ExtendedPropertiesExtensionPath, OnPropertiesExtensionChanged);
			AddinManager.AddExtensionNodeHandler (ProjectBindingsExtensionPath, OnProjectsExtensionChanged);
			UpdateExtensions ();
			AddinManager.ExtensionChanged += OnExtensionChanged;
			
			defaultFormat = formatManager.GetFileFormat ("MSBuild05");
		}
		
		public DataContext DataContext {
			get { return dataContext; }
		}
		
		public FileFormatManager FileFormats {
			get { return formatManager; }
		}
		
		internal ProjectServiceExtension ExtensionChain {
			get { return extensionChain; }
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

		internal FileFormat GetDefaultFormat (object ob)
		{
			if (defaultFormat.Format.CanWriteFile (ob))
				return defaultFormat;
			FileFormat[] formats = FileFormats.GetFileFormatsForObject (ob);
			if (formats.Length == 0)
				throw new InvalidOperationException ("Can't handle objects of type '" + ob.GetType () + "'");
			return formats [0];
		}
		
		public SolutionEntityItem ReadSolutionItem (IProgressMonitor monitor, string file)
		{
			return extensionChain.LoadSolutionItem (monitor, file, delegate {
				FileFormat format;
				SolutionEntityItem item = ReadFile (monitor, file, typeof(SolutionEntityItem), out format) as SolutionEntityItem;
				if (item != null) {
					item.FileFormat = format;
					item.NeedsReload = false;
				}
				else
					throw new InvalidOperationException ("Invalid file format: " + file);
				return item;
			});
		}
		
		public SolutionItem ReadSolutionItem (IProgressMonitor monitor, SolutionItemReference reference)
		{
			return ReadSolutionItem (monitor, reference, null);
		}
		
		public SolutionItem ReadSolutionItem (IProgressMonitor monitor, SolutionItemReference reference, params WorkspaceItem[] workspaces)
		{
			if (reference.Id == null) {
				return ReadSolutionItem (monitor, reference.Path);
			}
			else {
				Solution sol = null;
				if (workspaces.Length > 0) {
					string file = Path.GetFullPath (reference.Path);
					foreach (WorkspaceItem workspace in workspaces) {
						foreach (Solution item in workspace.GetAllSolutions ()) {
							if (Path.GetFullPath (item.FileName) == file) {
								sol = item;
								break;
							}
						}
						if (sol != null)
							break;
					}
				} else {
					sol = ReadWorkspaceItem (monitor, reference.Path) as Solution;
				}
				
				if (sol == null)
					return null;
				
				return sol.GetSolutionItem (reference.Id);
			}
		}
		
		public WorkspaceItem ReadWorkspaceItem (IProgressMonitor monitor, string file)
		{
			WorkspaceItem item = ExtensionChain.LoadWorkspaceItem (monitor, file) as WorkspaceItem;
			if (item != null)
				item.NeedsReload = false;
			else
				throw new InvalidOperationException ("Invalid file format: " + file);
			return item;
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
			
			if (item.FileFormat != null)
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
				if (defaultFormat.Format.CanWriteFile (item))
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
					List<string> oldFiles = obj.GetItemFiles (true);
					ExcludeEntries (obj, includedChildIds);
					if (format != null)
						obj.ConvertToFormat (format, true);
					obj.Save (monitor);
					List<string> newFiles = obj.GetItemFiles (true);
	
					// Remove old files
					foreach (string file in oldFiles) {
						if (newFiles.Contains (file))
							continue;
						
						if (File.Exists (file)) {
							File.Delete (file);
						
							// Exclude empty directories
							string dir = Path.GetDirectoryName (file);
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
		
		bool CopyFiles (IProgressMonitor monitor, IWorkspaceFileObject obj, List<string> files, string targetBasePath)
		{
			string baseDir = Path.GetFullPath (obj.BaseDirectory);
			foreach (string file in files) {

				if (!File.Exists (file)) {
					monitor.ReportWarning (GettextCatalog.GetString ("File '{0}' not found.", file));
					continue;
				}
				string fname = FileService.GetFullPath (file);
				
				// Can't export files from outside the root solution directory
				if (!fname.StartsWith (baseDir + Path.DirectorySeparatorChar)) {
					if (obj is Solution)
						monitor.ReportError ("The solution '" + obj.Name + "' is referencing the file '" + Path.GetFileName (file) + "' which is located outside the root solution directory.", null);
					else
						monitor.ReportError ("The project '" + obj.Name + "' is referencing the file '" + Path.GetFileName (file) + "' which is located outside the project directory.", null);
					return false;
				}
				
				string rpath = FileService.AbsoluteToRelativePath (baseDir
				                                                   , fname);
				rpath = Path.Combine (targetBasePath, rpath);
				
				if (!Directory.Exists (Path.GetDirectoryName (rpath)))
					Directory.CreateDirectory (FileService.GetFullPath (Path.GetDirectoryName (rpath)));

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
				throw new InvalidOperationException ("Unknown file format: " + filename);
			
			Solution tempSolution = new Solution ();
			
			FileFormat solutionFileFormat;
			if (formats [0].Format.CanWriteFile (tempSolution))
				solutionFileFormat = formats [0];
			else
				solutionFileFormat = MonoDevelop.Projects.Formats.MD1.MD1ProjectService.FileFormat;
			
			string solFileName = solutionFileFormat.GetValidFileName (tempSolution, filename);
			
			if (File.Exists (solFileName)) {
				return (Solution) Services.ProjectService.ReadWorkspaceItem (monitor, solFileName);
			}
			else {
				// Create a temporary solution and add the project to the solution
				tempSolution.FileName = filename;
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
			return ExtensionChain.IsSolutionItemFile (filename);
		}
		
		public bool IsWorkspaceItemFile (string filename)
		{
			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;
			return ExtensionChain.IsWorkspaceItemFile (filename);
		}
		
		internal bool IsSolutionItemFileInternal (string filename)
		{
			return formatManager.GetFileFormats (filename, typeof(SolutionItem)).Length > 0;
		}
		
		internal bool IsWorkspaceItemFileInternal (string filename)
		{
			return formatManager.GetFileFormats (filename, typeof(WorkspaceItem)).Length > 0;
		}
		
		public string GetDefaultResourceId (ProjectFile pf)
		{
			if (pf.Project != null) {
				IResourceHandler handler = pf.Project.ItemHandler as IResourceHandler;
				if (handler != null)
					return handler.GetDefaultResourceId (pf);
			}
			return Path.GetFileName (pf.Name);
		}
		
		internal void InitializeDataContext (DataContext ctx)
		{
			foreach (DataTypeCodon dtc in AddinManager.GetExtensionNodes (SerializableClassesExtensionPath)) {
				Type t = dtc.Class;
				if (t == null)
					throw new UserException ("Type '" + dtc.TypeName + "' not found. It could not be registered as a serializable type.");
				ctx.IncludeType (t);
			}
			foreach (ItemPropertyCodon cls in AddinManager.GetExtensionNodes (ExtendedPropertiesExtensionPath)) {
				if (cls.Class != null && cls.PropertyType != null)
					ctx.RegisterProperty (cls.Class, cls.PropertyName, cls.PropertyType);
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
				Type t = ((DataTypeCodon)args.ExtensionNode).Class;
				if (t == null) {
					throw new UserException ("Type '" + ((DataTypeCodon)args.ExtensionNode).TypeName + "' not found. It could not be registered as a serializable type.");
				}
				DataContext.IncludeType (t);
			}
			// Types can't be excluded from a DataContext, but that's not a big problem anyway
			
			if (DataContextChanged != null)
				DataContextChanged (this, EventArgs.Empty);
		}
		
		void OnPropertiesExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				ItemPropertyCodon cls = (ItemPropertyCodon) args.ExtensionNode;
				if (cls.Class != null && cls.PropertyType != null)
					DataContext.RegisterProperty (cls.Class, cls.PropertyName, cls.PropertyType);
			}
			else {
				ItemPropertyCodon cls = (ItemPropertyCodon) args.ExtensionNode;
				if (cls.Class != null && cls.PropertyType != null)
					DataContext.UnregisterProperty (cls.Class, cls.PropertyName);
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
				UpdateExtensions ();
		}
		
		void UpdateExtensions ()
		{
			ProjectServiceExtension[] extensions = (ProjectServiceExtension[]) AddinManager.GetExtensionObjects ("/MonoDevelop/ProjectModel/ProjectServiceExtensions", typeof(ProjectServiceExtension));
			for (int n=0; n<extensions.Length - 1; n++)
				extensions [n].Next = extensions [n + 1];

			if (extensions.Length > 0) {
				extensions [extensions.Length - 1].Next = defaultExtension;
				extensionChain.Next = extensions [0];
			} else {
				extensionChain.Next = defaultExtension;
			}
		}
	}
	
	internal class DefaultProjectServiceExtension: ProjectServiceExtension
	{
		Dictionary <SolutionItem,bool> needsBuildingCache;
		
		public override void Save (IProgressMonitor monitor, SolutionEntityItem entry)
		{
			entry.OnSave (monitor);
		}
		
		public override void Save (IProgressMonitor monitor, WorkspaceItem entry)
		{
			entry.OnSave (monitor);
		}
		
		public override List<string> GetItemFiles (SolutionEntityItem entry, bool includeReferencedFiles)
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
		
		protected override void Clean (IProgressMonitor monitor, IBuildTarget item, string configuration)
		{
			if (item is SolutionEntityItem) {
				SolutionEntityItem entry = (SolutionEntityItem) item;
				SolutionItemConfiguration config = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
				if (config != null && config.CustomCommands.HasCommands (CustomCommandType.Clean)) {
					config.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.Clean, configuration);
					return;
				}
				entry.OnClean (monitor, configuration);
			}
			else if (item is WorkspaceItem) {
				((WorkspaceItem)item).OnRunTarget (monitor, ProjectService.CleanTarget, configuration);
			}
			else if (item is SolutionItem)
				((SolutionItem)item).OnClean (monitor, configuration);
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
		}

		protected override BuildResult Build (IProgressMonitor monitor, IBuildTarget item, string configuration)
		{
			BuildResult res;
			if (item is SolutionEntityItem) {
				SolutionEntityItem entry = (SolutionEntityItem) item;
				SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Build)) {
					conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.Build, configuration);
					res = new BuildResult ();
				}
				else
					res = entry.OnBuild (monitor, configuration);
			}
			else if (item is WorkspaceItem) {
				res = ((WorkspaceItem)item).OnRunTarget (monitor, ProjectService.BuildTarget, configuration);
			}
			else if (item is SolutionItem)
				res = ((SolutionItem)item).OnBuild (monitor, configuration);
			else
				throw new InvalidOperationException ("Unknown item type: " + item);
			
			res.SourceTarget = item;
			return res;
		}
		
		public override void Execute (IProgressMonitor monitor, IBuildTarget item, ExecutionContext context, string configuration)
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
		
		public override bool CanExecute (IBuildTarget item, ExecutionContext context, string configuration)
		{
			if (item is SolutionEntityItem) {
				SolutionEntityItem entry = (SolutionEntityItem) item;
				SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Execute))
					return true;
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
		
		public override bool GetNeedsBuilding (IBuildTarget item, string configuration)
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
		
		public override void SetNeedsBuilding (IBuildTarget item, bool val, string configuration)
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
}
