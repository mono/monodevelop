// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
using MonoDevelop.Projects.Serialization;

using MonoDevelop.Core.Properties;
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
	
	public class ProjectService : IProjectService
	{
		DataContext dataContext = new DataContext ();
		ArrayList projectBindings = new ArrayList ();
		ProjectServiceExtension defaultExtension = new DefaultProjectServiceExtension ();
		ProjectServiceExtension extensionChain = new CustomCommandExtension ();
		
		FileFormatManager formatManager = new FileFormatManager ();
		IFileFormat defaultFormat = new MonoDevelopFileFormat ();
		
		public ProjectService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/ProjectFileFormats", OnFormatExtensionChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/SerializableClasses", OnSerializableExtensionChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/ExtendedProperties", OnPropertiesExtensionChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/ProjectBindings", OnProjectsExtensionChanged);
			UpdateExtensions ();
			AddinManager.ExtensionChanged += OnExtensionChanged;
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
		
		public CombineEntry ReadCombineEntry (string file, IProgressMonitor monitor)
		{
			CombineEntry entry = ExtensionChain.Load (monitor, file);
			if (entry != null)
				entry.NeedsReload = false;
			return entry;
		}
		
		internal CombineEntry ReadFile (string file, IProgressMonitor monitor)
		{
			IFileFormat[] formats = formatManager.GetFileFormats (file);

			if (formats.Length == 0)
				throw new InvalidOperationException ("Unknown file format: " + file);
			
			CombineEntry obj = formats[0].ReadFile (file, monitor) as CombineEntry;
			if (obj == null)
				throw new InvalidOperationException ("Invalid file format: " + file);
			
			if (obj.FileFormat == null)	
				obj.FileFormat = formats[0];

			return obj;
		}
		
		internal void WriteFile (string file, CombineEntry entry, IProgressMonitor monitor)
		{
			IFileFormat format = entry.FileFormat;
			if (format == null) {
				if (defaultFormat.CanWriteFile (entry))
					format = defaultFormat;
				else {
					IFileFormat[] formats = formatManager.GetFileFormatsForObject (entry);
					format = formats.Length > 0 ? formats [0] : null;
				}
				
				if (format == null)
					throw new InvalidOperationException ("FileFormat not provided for combine entry '" + entry.Name + "'");
				entry.FileName = format.GetValidFormatName (entry, file);
			}
			entry.FileName = file;
			format.WriteFile (entry.FileName, entry, monitor);
		}
		
		public string Export (IProgressMonitor monitor, string rootSourceFile, string targetPath, IFileFormat format)
		{
			return Export (monitor, rootSourceFile, null, targetPath, format);
		}
		
		public string Export (IProgressMonitor monitor, string rootSourceFile, string[] childEnryFiles, string targetPath, IFileFormat format)
		{
			string newFile;
			CombineEntry entry;
			
			string sourcePath = Path.GetFullPath (Path.GetDirectoryName (rootSourceFile));
			targetPath = Path.GetFullPath (targetPath);
			
			if (sourcePath != targetPath) {
				using (CombineEntry sourceEntry = ReadCombineEntry (rootSourceFile, monitor)) {
					if (!Export (monitor, sourceEntry, sourceEntry.BaseDirectory, targetPath, true))
						return null;
				}
				
				newFile = Path.Combine (targetPath, Path.GetFileName (rootSourceFile));
				entry = ReadCombineEntry (newFile, monitor);
			}
			else {
				newFile = rootSourceFile;
				entry = ReadCombineEntry (newFile, monitor);
			}
			
			using (entry)
			{
				ArrayList entriesToExport = new ArrayList ();
				if (childEnryFiles != null) {
					entriesToExport.Add (Path.GetFullPath (rootSourceFile));
					foreach (string file in childEnryFiles)
						entriesToExport.Add (Path.GetFullPath (file));

					if (sourcePath != targetPath) {
						// The file location has changed, the entry paths have to be relocated
						for (int n=0; n<entriesToExport.Count; n++) {
							string file = (string) entriesToExport [n];
							file = Runtime.FileService.AbsoluteToRelativePath (sourcePath, file);
							file = Runtime.FileService.RelativeToAbsolutePath (targetPath, file);
							file = Path.GetFullPath (file);
							entriesToExport [n] = file;
						}
					}
				} else if (entry is Combine) {
					foreach (CombineEntry e in ((Combine)entry).GetAllEntries ())
						entriesToExport.Add (Path.GetFullPath (e.FileName));
				}
				
				// The project needs to be converted
				
				ArrayList oldFiles = new ArrayList ();
				ChangeFormat (monitor, entry, entriesToExport, format, oldFiles);
				entry.Save (monitor);
				
				if (newFile != rootSourceFile) {
					foreach (string file in oldFiles) {
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
				return entry.FileName;
			}
		}
		
		bool Export (IProgressMonitor monitor, CombineEntry entry, string baseCombinePath, string targetBasePath, bool recursive)
		{
			StringCollection files = entry.GetExportFiles ();
			
			foreach (string file in files) {
				string fname = Runtime.FileService.GetFullPath (file);
				
				// Can't export files from outside the root solution directory
				if (!fname.StartsWith (baseCombinePath + Path.DirectorySeparatorChar)) {
					monitor.ReportError ("The project or solution '" + entry.Name + "' is referencing the file '" + Path.GetFileName (file) + "' which is located outside the root solution directory.", null);
					return false;
				}
				
				string rpath = Runtime.FileService.AbsoluteToRelativePath (baseCombinePath, fname);
				rpath = Path.Combine (targetBasePath, rpath);
				
				if (!Directory.Exists (Path.GetDirectoryName (rpath)))
					Directory.CreateDirectory (Runtime.FileService.GetFullPath (Path.GetDirectoryName (rpath)));
				
				File.Copy (file, rpath, true);
			}
			
			if (recursive && entry is Combine) {
				foreach (CombineEntry e in ((Combine)entry).Entries)
					if (!Export (monitor, e, baseCombinePath, targetBasePath, true))
						return false;
			}
			return true;
		}
		
		bool ChangeFormat (IProgressMonitor monitor, CombineEntry entry, ArrayList entriesToExport, IFileFormat format, ArrayList oldFiles)
		{
			if (entry is Combine) {
				bool needsConversion = false;
				Combine comb = (Combine) entry;
				CombineEntry[] ents = new CombineEntry [comb.Entries.Count];
				comb.Entries.CopyTo (ents);
				foreach (CombineEntry e in ents) {
					if (ChangeFormat (monitor, e, entriesToExport, format, oldFiles))
						needsConversion = true;
					else
						comb.Entries.Remove (e);
				}
				if (!needsConversion && !entriesToExport.Contains (Runtime.FileService.GetFullPath (entry.FileName))) {
					oldFiles.AddRange (entry.GetExportFiles ());
					return false;
				}
			}
			else {
				if (!entriesToExport.Contains (Runtime.FileService.GetFullPath (entry.FileName))) {
					oldFiles.AddRange (entry.GetExportFiles ());
					return false;
				}
			}
			
			StringCollection prevFiles = entry.GetExportFiles ();
			
			if (format != null) {
				if (!format.CanWriteFile (entry)) {
					monitor.ReportWarning (GettextCatalog.GetString ("The project or solution '{0}' can't be converted to format '{1}'", entry.Name, format.Name));
					return false;
				}
				entry.FileFormat = format;
				entry.FileName = format.GetValidFormatName (entry, entry.FileName);
			}
			
			// Add to oldFiles the files which are not included in the new format
			
			StringCollection newFiles = entry.GetExportFiles ();
			foreach (string file in prevFiles) {
				if (!newFiles.Contains (file))
					oldFiles.Add (file);
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
		
		public bool IsCombineEntryFile (string filename)
		{
			return ExtensionChain.IsCombineEntryFile (filename);
		}
		
		internal bool IsCombineEntryFileInternal (string filename)
		{
			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;
				
			return formatManager.GetFileFormats (filename).Length > 0;
		}
		
		void OnFormatExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				formatManager.RegisterFileFormat ((IFileFormat) args.ExtensionObject);
			else
				formatManager.UnregisterFileFormat ((IFileFormat) args.ExtensionObject);
		}
		
		void OnSerializableExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				Type t = ((DataTypeCodon)args.ExtensionNode).Class;
				if (t == null) {
					throw new UserException ("Type '" + ((DataTypeCodon)args.ExtensionNode).Class + "' not found. It could not be registered as a serializable type.");
				}
				DataContext.IncludeType (t);
			}
			// Types can't be excluded from a DataContext, but that's not a big problem anyway
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
	
	public class DefaultProjectServiceExtension: ProjectServiceExtension
	{
		Dictionary <CombineEntry,bool> needsBuildingCache;
		
		public override void Save (IProgressMonitor monitor, CombineEntry entry)
		{
			entry.OnSave (monitor);
		}
		
		public override System.Collections.Specialized.StringCollection GetExportFiles (CombineEntry entry)
		{
			return entry.OnGetExportFiles ();
		}
		
		public override bool IsCombineEntryFile (string filename)
		{
			return Services.ProjectService.IsCombineEntryFileInternal (filename);
		}
		
		public override CombineEntry Load (IProgressMonitor monitor, string fileName)
		{
			return Services.ProjectService.ReadFile (fileName, monitor);
		}
		
		public override void Clean (IProgressMonitor monitor, CombineEntry entry)
		{
			AbstractConfiguration config = entry.ActiveConfiguration as AbstractConfiguration;
			if (config != null && config.CustomCommands.HasCommands (CustomCommandType.Clean)) {
				config.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.Clean);
				return;
			}
			
			entry.OnClean (monitor);
		}
		
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			AbstractConfiguration conf = entry.ActiveConfiguration as AbstractConfiguration;
			if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Build)) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.Build);
				return new DefaultCompilerResult (new CompilerResults (null), "");
			}
			return entry.OnBuild (monitor);
		}
		
		public override void Execute (IProgressMonitor monitor, CombineEntry entry, ExecutionContext context)
		{
			AbstractConfiguration configuration = entry.ActiveConfiguration as AbstractConfiguration;
			if (configuration != null && configuration.CustomCommands.HasCommands (CustomCommandType.Execute)) {
				configuration.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.Execute, context);
				return;
			}
			entry.OnExecute (monitor, context);
		}
		
		public override bool GetNeedsBuilding (CombineEntry entry)
		{
			// This is a cache to avoid unneeded recursive calls to GetNeedsBuilding.
			bool cleanCache = false;
			if (needsBuildingCache == null) {
				needsBuildingCache = new Dictionary <CombineEntry,bool> ();
				cleanCache = true;
			} else {
				bool res;
				if (needsBuildingCache.TryGetValue (entry, out res))
					return res;
			}
			
			bool nb = entry.OnGetNeedsBuilding ();
			
			needsBuildingCache [entry] = nb;
			if (cleanCache)
				needsBuildingCache = null;
			return nb;
		}
		
		public override void SetNeedsBuilding (CombineEntry entry, bool value)
		{
			entry.OnSetNeedsBuilding (value);
		}
	}	
}
