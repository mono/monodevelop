// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Projects
{
	public enum NewFileSearch {
		None,
		OnLoad,
		OnLoadAutoInsert
	}
	
	/// <summary>
	/// External language bindings must extend this class
	/// </summary>
	[DataInclude (typeof(ProjectFile))]
	[DataItem (FallbackType=typeof(UnknownProject))]
	public abstract class Project : CombineEntry
	{
		[ItemProperty ("Description", DefaultValue="")]
		protected string description     = "";

		[ItemProperty ("newfilesearch", DefaultValue = NewFileSearch.None)]
		protected NewFileSearch newFileSearch  = NewFileSearch.None;

		[ItemProperty ("enableviewstate", DefaultValue = true)]
		protected bool enableViewState = true;
		
		ProjectFileCollection projectFiles;

		protected ProjectReferenceCollection projectReferences;
		
		[ItemProperty ("DeploymentInformation")]
		protected DeployInformation deployInformation = new DeployInformation();
		
		bool isDirty = false;
		bool filesChecked;
		
		private FileSystemWatcher projectFileWatcher;
		
		public Project ()
		{
			Name = "New Project";
			projectReferences = new ProjectReferenceCollection ();
			projectReferences.SetProject (this);
			
			projectFileWatcher = new FileSystemWatcher();
			projectFileWatcher.Changed += new FileSystemEventHandler (OnFileChanged);
		}
		
		[DefaultValue("")]
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
				NotifyModified ();
			}
		}
		
		[Browsable(false)]
		[ItemProperty ("Contents")]
		[ItemProperty ("File", Scope=1)]
		public ProjectFileCollection ProjectFiles {
			get {
				if (projectFiles != null) return projectFiles;
				return projectFiles = new ProjectFileCollection (this);
			}
		}
		
		[Browsable(false)]
		[ItemProperty ("References")]
		public ProjectReferenceCollection ProjectReferences {
			get {
				return projectReferences;
			}
		}
		
		[DefaultValue(NewFileSearch.None)]
		public NewFileSearch NewFileSearch {
			get {
				return newFileSearch;
			}

			set {
				newFileSearch = value;
				NotifyModified ();
			}
		}
		
		[Browsable(false)]
		public bool EnableViewState {
			get {
				return enableViewState;
			}
			set {
				enableViewState = value;
				NotifyModified ();
			}
		}
		
		public abstract string ProjectType {
			get;
		}
		
		[Browsable(false)]
		public DeployInformation DeployInformation {
			get {
				return deployInformation;
			}
		}
		
		[Browsable(false)]
		public virtual string [] SupportedLanguages {
			get {
				return new String [] { "" };
			}
		}

		public bool IsFileInProject(string filename)
		{
			return GetProjectFile (filename) != null;
		}
		
		public ProjectFile GetProjectFile (string fileName)
		{
			return ProjectFiles.GetFile (fileName);
		}
		
		public virtual bool IsCompileable (string fileName)
		{
			return false;
		}
				
		public static Project LoadProject (string filename, IProgressMonitor monitor)
		{
			Project prj = Services.ProjectService.ReadFile (filename, monitor) as Project;
			if (prj == null)
				throw new InvalidOperationException ("Invalid project file: " + filename);
			
			return prj;
		}
		
		public override void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			base.Deserialize (handler, data);
			projectReferences.SetProject (this);
		}

		public virtual string GetParseableFileContent(string fileName)
		{
			fileName = fileName.Replace('\\', '/'); // FIXME PEDRO
			StreamReader sr = File.OpenText(fileName);
			string content = sr.ReadToEnd();
			sr.Close();
			return content;
		}
		
		internal void RenameReferences(string oldName, string newName)
		{
			ArrayList toBeRemoved = new ArrayList();

			foreach (ProjectReference refInfo in this.ProjectReferences) {
				if (refInfo.ReferenceType == ReferenceType.Project) {
					if (refInfo.Reference == oldName) {
						toBeRemoved.Add(refInfo);
					}
				}
			}
			
			foreach (ProjectReference pr in toBeRemoved) {
				this.ProjectReferences.Remove(pr);
				ProjectReference prNew = new ProjectReference (ReferenceType.Project, newName);
				this.ProjectReferences.Add(prNew);
			}			
		}

		public void CopyReferencesToOutputPath (bool force)
		{
			AbstractProjectConfiguration config = ActiveConfiguration as AbstractProjectConfiguration;
			if (config == null) {
				return;
			}
			CopyReferencesToOutputPath (config.OutputDirectory, force);
		}
		
		void CopyReferencesToOutputPath (string destPath, bool force)
		{
			foreach (ProjectReference projectReference in ProjectReferences) {
				if ((projectReference.LocalCopy || force) && projectReference.ReferenceType != ReferenceType.Gac) {
					foreach (string referenceFileName in projectReference.GetReferencedFileNames ()) {
						string destinationFileName = Path.Combine (destPath, Path.GetFileName (referenceFileName));
						try {
							if (destinationFileName != referenceFileName) {
								File.Copy(referenceFileName, destinationFileName, true);
								if (File.Exists (referenceFileName + ".mdb"))
									File.Copy (referenceFileName + ".mdb", destinationFileName + ".mdb", true);
							}
						} catch (Exception e) {
							Runtime.LoggingService.ErrorFormat ("Can't copy reference file from {0} to {1}: {2}", referenceFileName, destinationFileName, e);
						}
					}
				}
				if (projectReference.ReferenceType == ReferenceType.Project && RootCombine != null) {
					Project p = RootCombine.FindProject (projectReference.Reference);
					p.CopyReferencesToOutputPath (destPath, force);
				}
			}
		}
		
		void CleanReferencesInOutputPath (string destPath)
		{
			foreach (ProjectReference projectReference in ProjectReferences) {
				if (projectReference.ReferenceType != ReferenceType.Gac) {
					foreach (string referenceFileName in projectReference.GetReferencedFileNames ()) {
						string destinationFileName = Path.Combine (destPath, Path.GetFileName (referenceFileName));
						try {
							if (destinationFileName != referenceFileName) {
								File.Delete (destinationFileName);
								if (File.Exists (destinationFileName + ".mdb"))
									File.Delete (destinationFileName + ".mdb");
							}
						} catch (Exception e) {
							Runtime.LoggingService.ErrorFormat ("Can't delete reference file {0}: {2}", destinationFileName, e);
						}
					}
				}
				if (projectReference.ReferenceType == ReferenceType.Project && RootCombine != null) {
					Project p = RootCombine.FindProject (projectReference.Reference);
					p.CleanReferencesInOutputPath (destPath);
				}
			}
		}
		
		public override void Dispose()
		{
			base.Dispose ();
			projectFileWatcher.Dispose ();
			foreach (ProjectFile file in ProjectFiles) {
				file.Dispose ();
			}
		}
		
		public ProjectReference AddReference (string filename)
		{
			foreach (ProjectReference rInfo in ProjectReferences) {
				if (rInfo.Reference == filename) {
					return rInfo;
				}
			}
			ProjectReference newReferenceInformation = new ProjectReference (ReferenceType.Assembly, filename);
			ProjectReferences.Add (newReferenceInformation);
			return newReferenceInformation;
		}
		
		public ProjectFile AddFile (string filename, BuildAction action)
		{
			foreach (ProjectFile fInfo in ProjectFiles) {
				if (fInfo.Name == filename) {
					return fInfo;
				}
			}
			ProjectFile newFileInformation = new ProjectFile (filename, action);
			ProjectFiles.Add (newFileInformation);
			return newFileInformation;
		}
		
		public void AddFile (ProjectFile projectFile) {
			ProjectFiles.Add (projectFile);
		}

		public override void Clean ()
		{
			isDirty = true;
			
			// Delete the generated assembly
			string file = GetOutputFileName ();
			if (file != null) {
				if (File.Exists (file))
					File.Delete (file);
				if (File.Exists (file + ".mdb"))
					File.Delete (file + ".mdb");
			}

			// Delete referenced assemblies
			AbstractProjectConfiguration config = ActiveConfiguration as AbstractProjectConfiguration;
			if (config != null)
				CleanReferencesInOutputPath (config.OutputDirectory);
		}
		
		public override ICompilerResult Build (IProgressMonitor monitor)
		{
			return Build (monitor, true);
		}
		
		public virtual ICompilerResult Build (IProgressMonitor monitor, bool buildReferences)
		{
			if (buildReferences)
			{
				CombineEntryCollection referenced = new CombineEntryCollection ();
				GetReferencedProjects (referenced, this);
				
				referenced = Combine.TopologicalSort (referenced);
				
				CompilerResults cres = new CompilerResults (null);
				
				int builds = 0;
				int failedBuilds = 0;
					
				monitor.BeginTask (null, referenced.Count);
				foreach (Project p in referenced) {
					ICompilerResult res = p.Build (monitor, false);
					cres.Errors.AddRange (res.CompilerResults.Errors);
					monitor.Step (1);
					builds++;
					if (res.ErrorCount > 0) {
						failedBuilds = 1;
						break;
					}
				}
				monitor.EndTask ();
				return new DefaultCompilerResult (cres, "", builds, failedBuilds);
			}
			
			if (!NeedsBuilding)
				return new DefaultCompilerResult (new CompilerResults (null), "");
			
			try {
			
				IBuildStep[] steps = BuildPipeline;
				
				monitor.BeginTask (String.Format (GettextCatalog.GetString ("Building Project: {0} Configuration: {1}"), Name, ActiveConfiguration.Name), steps.Length);
				
				Runtime.StringParserService.Properties["Project"] = Name;
				
				ICompilerResult res = null;
				
				foreach (IBuildStep step in steps) {
					ICompilerResult sres = step.Build (monitor, this);
					if (sres != null) {
						if (res != null) {
							CompilerResults cres = new CompilerResults (null);
							cres.Errors.AddRange (res.CompilerResults.Errors);
							cres.Errors.AddRange (sres.CompilerResults.Errors);
							res = new DefaultCompilerResult (cres, res.CompilerOutput + "\n" + sres.CompilerOutput);
						} else
							res = sres;
					}
					monitor.Step (1);
				}
				
				isDirty = false;
				
				if (res != null) {
					string errorString = GettextCatalog.GetPluralString("{0} error", "{0} errors", res.ErrorCount, res.ErrorCount);
					string warningString = GettextCatalog.GetPluralString("{0} warning", "{0} warnings", res.WarningCount, res.WarningCount);
				
					monitor.Log.WriteLine(GettextCatalog.GetString("Build complete -- ") + errorString + ", " + warningString);
				}
				
				return res;
			} finally {
				monitor.EndTask ();
			}
		}
		
		protected virtual IBuildStep[] BuildPipeline {
			get {
				return (IBuildStep[]) Runtime.AddInService.GetTreeItems ("/SharpDevelop/Workbench/BuildPipeline", typeof(IBuildStep));
			}
		}
		
		
		void GetReferencedProjects (CombineEntryCollection referenced, Project project)
		{
			if (referenced.Contains (project)) return;
			
			if (NeedsBuilding)
				referenced.Add (project);

			foreach (ProjectReference pref in project.ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Project) {
					Combine c = project.RootCombine;
					if (c != null) {
						Project rp = c.FindProject (pref.Reference);
						if (rp != null)
							GetReferencedProjects (referenced, rp);
					}
				}
			}
		}
		
		protected virtual void DoPreBuild (IProgressMonitor monitor)
		{
			AbstractProjectConfiguration conf = ActiveConfiguration as AbstractProjectConfiguration;
				
			// create output directory, if not exists
			string outputDir = conf.OutputDirectory;
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo(outputDir);
				if (!directoryInfo.Exists) {
					directoryInfo.Create();
				}
			} catch (Exception e) {
				throw new ApplicationException("Can't create project output directory " + outputDir + " original exception:\n" + e.ToString());
			}
			
			if (conf != null && conf.ExecuteBeforeBuild != "" && File.Exists(conf.ExecuteBeforeBuild)) {
				monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("Executing: {0}"), conf.ExecuteBeforeBuild));
				ProcessStartInfo ps = GetBuildTaskStartInfo(conf.ExecuteBeforeBuild);
				Process process = new Process();
				process.StartInfo = ps;
				process.Start();
				monitor.Log.Write (process.StandardOutput.ReadToEnd());
				monitor.Log.WriteLine ();
			}
		}
		
		protected virtual ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			return new DefaultCompilerResult (new CompilerResults (null), "");
		}
		
		protected virtual void DoPostBuild (IProgressMonitor monitor)
		{
			AbstractProjectConfiguration conf = ActiveConfiguration as AbstractProjectConfiguration;

			if (conf != null && conf.ExecuteAfterBuild != "" && File.Exists(conf.ExecuteAfterBuild)) {
				monitor.Log.WriteLine ();
				monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("Executing: {0}"), conf.ExecuteAfterBuild));
				ProcessStartInfo ps = GetBuildTaskStartInfo(conf.ExecuteAfterBuild);
				Process process = new Process();
				process.StartInfo = ps;
				process.Start();
				monitor.Log.Write (process.StandardOutput.ReadToEnd());
			}
		}
		
		private ProcessStartInfo GetBuildTaskStartInfo(string file) {
			ProcessStartInfo ps = new ProcessStartInfo(file);
			ps.UseShellExecute = false;
			ps.RedirectStandardOutput = true;
			ps.WorkingDirectory = BaseDirectory;
			return ps;
		}
		
		public override void Execute (IProgressMonitor monitor, ExecutionContext context)
		{
			AbstractProjectConfiguration configuration = (AbstractProjectConfiguration) ActiveConfiguration;
				
			string args = configuration.CommandLineParameters;
			
			if (configuration.ExecuteScript != null && configuration.ExecuteScript.Length > 0) {
				IConsole console;
				if (configuration.ExternalConsole)
					console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
				else
					console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);

				ProcessWrapper p = Runtime.ProcessService.StartConsoleProcess (configuration.ExecuteScript, args, BaseDirectory, console, null);
				p.WaitForOutput ();
			} else {
				DoExecute (monitor, context);
			}
		}
		
		protected virtual void DoExecute (IProgressMonitor monitor, ExecutionContext context)
		{
		}
		
		public virtual string GetOutputFileName ()
		{
			return null;
		}
		
		public override bool NeedsBuilding {
			get {
				if (!isDirty) CheckNeedsBuild ();
				return isDirty;
			}
			set {
				isDirty = value;
			}
		}
		
		public override string FileName {
			get {
				return base.FileName;
			}
			set {
				base.FileName = value;
				if (value != null)
					UpdateFileWatch ();
			}
		}
		
		protected virtual void CheckNeedsBuild ()
		{
			DateTime tim = GetLastBuildTime ();
			if (tim == DateTime.MinValue) {
				isDirty = true;
				return;
			}
			
			if (!filesChecked) {
				foreach (ProjectFile file in ProjectFiles) {
					if (file.BuildAction == BuildAction.Exclude) continue;
					FileInfo finfo = new FileInfo (file.FilePath);
					if (finfo.Exists && finfo.LastWriteTime > tim) {
						isDirty = true;
						return;
					}
				}
				
				filesChecked = true;
			}

			foreach (ProjectReference pref in ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Project && RootCombine != null) {
					Project rp = RootCombine.FindProject (pref.Reference);
					if (rp != null && rp.NeedsBuilding) {
						isDirty = true;
						return;
					}
				}
			}
			
		}
		
		protected virtual DateTime GetLastBuildTime ()
		{
			string file = GetOutputFileName ();
			FileInfo finfo = new FileInfo (file);
			if (!finfo.Exists) return DateTime.MinValue;
			else return finfo.LastWriteTime;
		}
		
		private void UpdateFileWatch()
		{
			projectFileWatcher.EnableRaisingEvents = false;
			projectFileWatcher.Path = BaseDirectory;
			projectFileWatcher.EnableRaisingEvents = true;
		}
		
		void OnFileChanged (object source, FileSystemEventArgs e)
		{
			ProjectFile file = GetProjectFile (e.FullPath);
			if (file != null) {
				isDirty = true;
				NotifyFileChangedInProject (file);
			}

		}

 		internal void NotifyFileChangedInProject (ProjectFile file)
		{
			OnFileChangedInProject (new ProjectFileEventArgs (this, file));
		}
		
 		internal void NotifyFilePropertyChangedInProject (ProjectFile file)
		{
			NotifyModified ();
			OnFilePropertyChangedInProject (new ProjectFileEventArgs (this, file));
		}
		
		internal void NotifyFileRemovedFromProject (ProjectFile file)
		{
			isDirty = true;
			NotifyModified ();
			OnFileRemovedFromProject (new ProjectFileEventArgs (this, file));
		}
		
		internal void NotifyFileAddedToProject (ProjectFile file)
		{
			isDirty = true;
			NotifyModified ();
			OnFileAddedToProject (new ProjectFileEventArgs (this, file));
		}
		
		internal void NotifyFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			isDirty = true;
			NotifyModified ();
			OnFileRenamedInProject (args);
		}
		
		internal void NotifyReferenceRemovedFromProject (ProjectReference reference)
		{
			isDirty = true;
			NotifyModified ();
			OnReferenceRemovedFromProject (new ProjectReferenceEventArgs (this, reference));
		}
		
		internal void NotifyReferenceAddedToProject (ProjectReference reference)
		{
			isDirty = true;
			NotifyModified ();
			OnReferenceAddedToProject (new ProjectReferenceEventArgs (this, reference));
		}
		
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}
		
		protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}

 		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}
		
 		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}
		
 		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}
				
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		
		class MainBuildStep: IBuildStep
		{
			public ICompilerResult Build (IProgressMonitor monitor, Project project)
			{
				monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("Performing main compilation...")));
				return project.DoBuild (monitor);
			}
			
			public bool NeedsBuilding (Project project)
			{
				if (!project.isDirty) project.CheckNeedsBuild ();
				return project.isDirty;
			}
		}
		
		class PreBuildStep: IBuildStep
		{
			public ICompilerResult Build (IProgressMonitor monitor, Project project)
			{
				project.DoPreBuild (monitor);
				return null;
			}
			
			public bool NeedsBuilding (Project project)
			{
				return false;
			}
		}
		
		class PostBuildStep: IBuildStep
		{
			public ICompilerResult Build (IProgressMonitor monitor, Project project)
			{
				project.DoPostBuild (monitor);
				return null;
			}
			
			public bool NeedsBuilding (Project project)
			{
				return false;
			}
		}
	}
	
	public class UnknownProject: Project
	{
		public override string ProjectType {
			get { return ""; }
		}

		public override IConfiguration CreateConfiguration (string name)
		{
			return null;
		}
	}
}
