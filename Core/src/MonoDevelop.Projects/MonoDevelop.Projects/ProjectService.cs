// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
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
using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	
	public enum BeforeCompileAction {
		Nothing,
		SaveAllFiles,
		PromptForSave,
	}
	
	public class ProjectService : AbstractService, IProjectService
	{
		DataContext dataContext = new DataContext ();
		ArrayList projectBindings = new ArrayList ();
		ProjectServiceExtension defaultExtension = new DefaultProjectServiceExtension ();
		ProjectServiceExtension extensionChain = new CustomCommandExtension ();
		
		FileFormatManager formatManager = new FileFormatManager ();
		IFileFormat defaultProjectFormat = new MdpFileFormat ();
		IFileFormat defaultCombineFormat = new MdsFileFormat ();
		
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
				if (entry is Project)
					format = defaultProjectFormat;
				else if (entry is Combine)
					format = defaultCombineFormat;
				else {
					IFileFormat[] formats = formatManager.GetFileFormatsForObject (entry);
					format = formats.Length > 0 ? formats [0] : null;
				}
				
				if (format == null)
					throw new InvalidOperationException ("FileFormat not provided for combine entry '" + entry.Name + "'");
				entry.FileName = format.GetValidFormatName (file);
			}
			entry.FileName = file;
			format.WriteFile (entry.FileName, entry, monitor);
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

		public override void InitializeService()
		{
			base.InitializeService();

			formatManager.RegisterFileFormat (defaultProjectFormat);
			formatManager.RegisterFileFormat (defaultCombineFormat);
			
			Runtime.AddInService.RegisterExtensionItemListener ("/SharpDevelop/Workbench/ProjectFileFormats", OnFormatExtensionChanged);
			Runtime.AddInService.RegisterExtensionItemListener ("/SharpDevelop/Workbench/SerializableClasses", OnSerializableExtensionChanged);
			Runtime.AddInService.RegisterExtensionItemListener ("/SharpDevelop/Workbench/Serialization/ExtendedProperties", OnPropertiesExtensionChanged);
			Runtime.AddInService.RegisterExtensionItemListener ("/SharpDevelop/Workbench/ProjectBindings", OnProjectsExtensionChanged);
			UpdateExtensions ();
			Runtime.AddInService.ExtensionChanged += OnExtensionChanged;
		}
		
		void OnFormatExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				FileFormatCodon codon = (FileFormatCodon) item;
				if (codon.FileFormat != null)
					formatManager.RegisterFileFormat (codon.FileFormat);
			}
		}
		
		void OnSerializableExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				Type t = ((DataTypeCodon)item).Type;
				if (t == null) {
					throw new UserException ("Type '" + ((DataTypeCodon)item).Class + "' not found. It could not be registered as a serializable type.");
				}
				DataContext.IncludeType (t);
			}
		}
		
		void OnPropertiesExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				ItemPropertyCodon cls = (ItemPropertyCodon) item;
				if (cls.ClassType != null && cls.PropertyType != null)
					DataContext.RegisterProperty (cls.ClassType, cls.PropertyName, cls.PropertyType);
			}
		}
		
		void OnProjectsExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add)
				projectBindings.Add (item);
		}
		
		void OnExtensionChanged (string path)
		{
			if (path == "/SharpDevelop/Workbench/ProjectServiceExtensions")
				UpdateExtensions ();
		}
		
		void UpdateExtensions ()
		{
			ProjectServiceExtension[] extensions = (ProjectServiceExtension[]) Runtime.AddInService.GetTreeItems ("/SharpDevelop/Workbench/ProjectServiceExtensions", typeof(ProjectServiceExtension));
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
