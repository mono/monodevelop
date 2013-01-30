//  Project.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonoDevelop;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;


namespace MonoDevelop.Projects
{
	public enum NewFileSearch
	{
		None,
		OnLoad,
		OnLoadAutoInsert
	}
	
	/// <summary>
	/// A project
	/// </summary>
	/// <remarks>
	/// This is the base class for MonoDevelop projects. A project is a solution item which has a list of
	/// source code files and which can be built to generate an output.
	/// </remarks>
	[DataInclude(typeof(ProjectFile))]
	[ProjectModelDataItem(FallbackType = typeof(UnknownProject))]
	public abstract class Project : SolutionEntityItem
	{
		string[] buildActions;
		bool isDirty;

		public Project ()
		{
			FileService.FileChanged += OnFileChanged;
			files = new ProjectFileCollection ();
			Items.Bind (files);
			DependencyResolutionEnabled = true;
		}
		
		/// <summary>
		/// Description of the project.
		/// </summary>
		[ItemProperty("Description", DefaultValue = "")]
		private string description = "";
		public string Description {
			get { return description; }
			set {
				description = value;
				NotifyModified ("Description");
			}
		}
		
		/// <summary>
		/// Determines whether the provided file can be as part of this project
		/// </summary>
		/// <returns>
		/// <c>true</c> if the file can be compiled; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='fileName'>
		/// File name
		/// </param>
		public virtual bool IsCompileable (string fileName)
		{
			return false;
		}

		/// <summary>
		/// Files of the project
		/// </summary>
		public ProjectFileCollection Files {
			get { return files; }
		}
		private ProjectFileCollection files;
		
		
		[ItemProperty("newfilesearch", DefaultValue = NewFileSearch.None)]
		protected NewFileSearch newFileSearch = NewFileSearch.None;
		public NewFileSearch NewFileSearch {
			get { return newFileSearch; }

			set {
				newFileSearch = value;
				NotifyModified ("NewFileSearch");
			}
		}

		[ProjectPathItemProperty ("BaseIntermediateOutputPath")]
		FilePath baseIntermediateOutputPath;

		public virtual FilePath BaseIntermediateOutputPath {
			get {
				if (!baseIntermediateOutputPath.IsNullOrEmpty)
					return baseIntermediateOutputPath;
				return BaseDirectory.Combine ("obj");
			}
			set {
				if (value.IsNullOrEmpty)
					value = FilePath.Null;
				if (baseIntermediateOutputPath == value)
					return;
				NotifyModified ("BaseIntermediateOutputPath");
			}
		}

		/// <summary>
		/// Gets the type of the project.
		/// </summary>
		/// <value>
		/// The type of the project.
		/// </value>
		public abstract string ProjectType {
			get;
		}

		/// <summary>
		/// Gets or sets the icon of the project.
		/// </summary>
		/// <value>
		/// The stock icon.
		/// </value>
		public virtual IconId StockIcon {
			get { return stockIcon; }
			set { this.stockIcon = value; NotifyModified ("StockIcon"); }
		}
		IconId stockIcon = "md-project";
		
		/// <summary>
		/// List of languages that this project supports
		/// </summary>
		/// <value>
		/// The identifiers of the supported languages.
		/// </value>
		public virtual string[] SupportedLanguages {
			get { return new String[] { "" }; }
		}

		/// <summary>
		/// Gets the default build action for a file
		/// </summary>
		/// <returns>
		/// The default build action.
		/// </returns>
		/// <param name='fileName'>
		/// File name.
		/// </param>
		public virtual string GetDefaultBuildAction (string fileName)
		{
			return IsCompileable (fileName) ? BuildAction.Compile : BuildAction.None;
		}
		
		/// <summary>
		/// Gets a project file.
		/// </summary>
		/// <returns>
		/// The project file.
		/// </returns>
		/// <param name='fileName'>
		/// File name.
		/// </param>
		public ProjectFile GetProjectFile (string fileName)
		{
			return files.GetFile (fileName);
		}
		
		/// <summary>
		/// Determines whether a file belongs to this project
		/// </summary>
		/// <param name='fileName'>
		/// File name
		/// </param>
		public bool IsFileInProject (string fileName)
		{
			return files.GetFile (fileName) != null;
		}

		/// <summary>
		/// Gets a list of build actions supported by this project
		/// </summary>
		/// <remarks>
		/// Common actions are grouped at the top, separated by a "--" entry *IF* there are 
		/// more "uncommon" actions than "common" actions
		/// </remarks>
		public string[] GetBuildActions ()
		{
			if (buildActions != null)
				return buildActions;

			// find all the actions in use and add them to the list of standard actions
			Hashtable actions = new Hashtable ();
			object marker = new object (); //avoid using bools as they need to be boxed. re-use single object instead
			//ad the standard actions
			foreach (string action in GetStandardBuildActions ())
				actions[action] = marker;

			//add any more actions that are in the project file
			foreach (ProjectFile pf in files)
				if (!actions.ContainsKey (pf.BuildAction))
					actions[pf.BuildAction] = marker;

			//remove the "common" actions, since they're handled separately
			IList<string> commonActions = GetCommonBuildActions ();
			foreach (string action in commonActions)
				if (actions.Contains (action))
					actions.Remove (action);

			//calculate dimensions for our new array and create it
			int dashPos = commonActions.Count;
			bool hasDash = commonActions.Count > 0 && actions.Count > 0;
			int arrayLen = commonActions.Count + actions.Count;
			int uncommonStart = hasDash ? dashPos + 1 : dashPos;
			if (hasDash)
				arrayLen++;
			buildActions = new string[arrayLen];

			//populate it
			if (commonActions.Count > 0)
				commonActions.CopyTo (buildActions, 0);
			if (hasDash)
				buildActions[dashPos] = "--";
			if (actions.Count > 0)
				actions.Keys.CopyTo (buildActions, uncommonStart);

			//sort the actions
			if (hasDash) {
				//it may be better to leave common actions in the order that the project specified
				//Array.Sort (buildActions, 0, commonActions.Count, StringComparer.Ordinal);
				Array.Sort (buildActions, uncommonStart, arrayLen - uncommonStart, StringComparer.Ordinal);
			} else {
				Array.Sort (buildActions, StringComparer.Ordinal);
			}
			return buildActions;
		}
		
		/// <summary>
		/// Gets a list of standard build actions.
		/// </summary>
		protected virtual IEnumerable<string> GetStandardBuildActions ()
		{
			return BuildAction.StandardActions;
		}

		/// <summary>
		/// Gets a list of common build actions (common actions are shown first in the project build action list)
		/// </summary>
		protected virtual IList<string> GetCommonBuildActions ()
		{
			return BuildAction.StandardActions;
		}

		public static Project LoadProject (string filename, IProgressMonitor monitor)
		{
			Project prj = Services.ProjectService.ReadSolutionItem (monitor, filename) as Project;
			if (prj == null)
				throw new InvalidOperationException ("Invalid project file: " + filename);

			return prj;
		}


		public override void Dispose ()
		{
			FileService.FileChanged -= OnFileChanged;
			base.Dispose ();
		}
		
		/// <summary>
		/// Adds a file to the project
		/// </summary>
		/// <returns>
		/// The file instance.
		/// </returns>
		/// <param name='filename'>
		/// Absolute path to the file.
		/// </param>
		public ProjectFile AddFile (string filename)
		{
			return AddFile (filename, null);
		}
		
		public IEnumerable<ProjectFile> AddFiles (IEnumerable<FilePath> files)
		{
			return AddFiles (files, null);
		}
		
		/// <summary>
		/// Adds a file to the project
		/// </summary>
		/// <returns>
		/// The file instance.
		/// </returns>
		/// <param name='filename'>
		/// Absolute path to the file.
		/// </param>
		/// <param name='buildAction'>
		/// Build action to assign to the file.
		/// </param>
		public ProjectFile AddFile (string filename, string buildAction)
		{
			foreach (ProjectFile fInfo in Files) {
				if (fInfo.Name == filename) {
					return fInfo;
				}
			}

			if (String.IsNullOrEmpty (buildAction)) {
				buildAction = GetDefaultBuildAction (filename);
			}

			ProjectFile newFileInformation = new ProjectFile (filename, buildAction);
			Files.Add (newFileInformation);
			return newFileInformation;
		}
		
		public IEnumerable<ProjectFile> AddFiles (IEnumerable<FilePath> files, string buildAction)
		{
			List<ProjectFile> newFiles = new List<ProjectFile> ();
			foreach (FilePath filename in files) {
				string ba = buildAction;
				if (String.IsNullOrEmpty (ba))
					ba = GetDefaultBuildAction (filename);

				ProjectFile newFileInformation = new ProjectFile (filename, ba);
				newFiles.Add (newFileInformation);
			}
			Files.AddRange (newFiles);
			return newFiles;
		}
		
		/// <summary>
		/// Adds a file to the project
		/// </summary>
		/// <param name='projectFile'>
		/// The file.
		/// </param>
		public void AddFile (ProjectFile projectFile)
		{
			Files.Add (projectFile);
		}
		
		/// <summary>
		/// Adds a directory to the project.
		/// </summary>
		/// <returns>
		/// The directory instance.
		/// </returns>
		/// <param name='relativePath'>
		/// Relative path of the directory.
		/// </param>
		/// <remarks>
		/// The directory is created if it doesn't exist
		/// </remarks>
		public ProjectFile AddDirectory (string relativePath)
		{
			string newPath = Path.Combine (BaseDirectory, relativePath);

			foreach (ProjectFile fInfo in Files)
				if (fInfo.Name == newPath && fInfo.Subtype == Subtype.Directory)
					return fInfo;

			if (!Directory.Exists (newPath)) {
				if (File.Exists (newPath)) {
					string message = GettextCatalog.GetString ("Cannot create directory {0}, as a file with that name exists.", newPath);
					throw new InvalidOperationException (message);
				}
				FileService.CreateDirectory (newPath);
			}

			ProjectFile newDir = new ProjectFile (newPath);
			newDir.Subtype = Subtype.Directory;
			AddFile (newDir);
			return newDir;
		}
		
		//HACK: the build code is structured such that support file copying is in here instead of the item handler
		//so in order to avoid doing them twice when using the msbuild engine, we special-case them
		bool UsingMSBuildEngine ()
		{
			var msbuildHandler = ItemHandler as MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectHandler;
			return msbuildHandler != null && msbuildHandler.UseMSBuildEngine;
		}

		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			// create output directory, if not exists
			ProjectConfiguration conf = GetConfiguration (configuration) as ProjectConfiguration;
			if (conf == null) {
				BuildResult cres = new BuildResult ();
				cres.AddError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration.ToString (), Name));
				return cres;
			}
			
			StringParserService.Properties["Project"] = Name;
			
			if (UsingMSBuildEngine ()) {
				var r = DoBuild (monitor, configuration);
				isDirty = false;
				return r;
			}
			
			string outputDir = conf.OutputDirectory;
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo (outputDir);
				if (!directoryInfo.Exists) {
					directoryInfo.Create ();
				}
			} catch (Exception e) {
				throw new ApplicationException ("Can't create project output directory " + outputDir + " original exception:\n" + e.ToString ());
			}

			//copy references and files marked to "CopyToOutputDirectory"
			CopySupportFiles (monitor, configuration);
		
			monitor.Log.WriteLine ("Performing main compilation...");
			
			BuildResult res = DoBuild (monitor, configuration);

			isDirty = false;

			if (res != null) {
				string errorString = GettextCatalog.GetPluralString ("{0} error", "{0} errors", res.ErrorCount, res.ErrorCount);
				string warningString = GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", res.WarningCount, res.WarningCount);

				monitor.Log.WriteLine (GettextCatalog.GetString ("Build complete -- ") + errorString + ", " + warningString);
			}

			return res;
		}
		
		/// <summary>
		/// Copies the support files to the output directory
		/// </summary>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='configuration'>
		/// Configuration for which to copy the files.
		/// </param>
		/// <remarks>
		/// Copies all support files to the output directory of the given configuration. Support files
		/// include: assembly references with the Local Copy flag, data files with the Copy to Output option, etc.
		/// </remarks>
		public void CopySupportFiles (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);

			foreach (FileCopySet.Item item in GetSupportFileList (configuration)) {
				FilePath dest = Path.GetFullPath (Path.Combine (config.OutputDirectory, item.Target));
				FilePath src = Path.GetFullPath (item.Src);

				try {
					if (dest == src)
						continue;

					if (item.CopyOnlyIfNewer && File.Exists (dest) && (File.GetLastWriteTimeUtc (dest) >= File.GetLastWriteTimeUtc (src)))
						continue;

					// Use Directory.Create so we don't trigger the VersionControl addin and try to
					// add the directory to version control.
					if (!Directory.Exists (Path.GetDirectoryName (dest)))
						Directory.CreateDirectory (Path.GetDirectoryName (dest));

					if (File.Exists (src)) {
						dest.Delete ();
						FileService.CopyFile (src, dest);
						
						// Copied files can't be read-only, so they can be removed when rebuilding the project
						FileAttributes atts = File.GetAttributes (dest);
						if (atts.HasFlag (FileAttributes.ReadOnly))
							File.SetAttributes (dest, atts & ~FileAttributes.ReadOnly);
					}
					else
						monitor.ReportError (GettextCatalog.GetString ("Could not find support file '{0}'.", src), null);

				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Error copying support file '{0}'.", dest), ex);
				}
			}
		}

		/// <summary>
		/// Removes all support files from the output directory
		/// </summary>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='configuration'>
		/// Configuration for which to delete the files.
		/// </param>
		/// <remarks>
		/// Deletes all support files from the output directory of the given configuration. Support files
		/// include: assembly references with the Local Copy flag, data files with the Copy to Output option, etc.
		/// </remarks>
		public void DeleteSupportFiles (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);

			foreach (FileCopySet.Item item in GetSupportFileList (configuration)) {
				FilePath dest = Path.Combine (config.OutputDirectory, item.Target);

				// Ignore files which were not copied
				if (Path.GetFullPath (dest) == Path.GetFullPath (item.Src))
					continue;

				try {
					dest.Delete ();
				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Error deleting support file '{0}'.", dest), ex);
				}
			}
		}
		
		/// <summary>
		/// Gets a list of files required to use the project output
		/// </summary>
		/// <returns>
		/// A list of files.
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are required to use the project output binary, for example: data files with
		/// the Copy to Output option, debug information files, generated resource files, etc.
		/// </remarks>
		public FileCopySet GetSupportFileList (ConfigurationSelector configuration)
		{
			var list = new FileCopySet ();
			PopulateSupportFileList (list, configuration);
			return list;
		}

		/// <summary>
		/// Gets a list of files required to use the project output
		/// </summary>
		/// <param name='list'>
		/// List where to add the support files.
		/// </param>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are required to use the project output binary, for example: data files with
		/// the Copy to Output option, debug information files, generated resource files, etc.
		/// </remarks>
		internal protected virtual void PopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
		{
			foreach (ProjectFile pf in Files) {
				if (pf.CopyToOutputDirectory == FileCopyMode.None)
					continue;
				list.Add (pf.FilePath, pf.CopyToOutputDirectory == FileCopyMode.PreserveNewest, pf.ProjectVirtualPath);
			}
		}
		
		/// <summary>
		/// Gets a list of files generated when building this project
		/// </summary>
		/// <returns>
		/// A list of files.
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are generated when this project is built, including: the generated binary,
		/// debug information files, satellite assemblies.
		/// </remarks>
		public List<FilePath> GetOutputFiles (ConfigurationSelector configuration)
		{
			List<FilePath> list = new List<FilePath> ();
			PopulateOutputFileList (list, configuration);
			return list;
		}

		/// <summary>
		/// Gets a list of files retuired to use the project output
		/// </summary>
		/// <param name='list'>
		/// List where to add the support files.
		/// </param>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are required to use the project output binary, for example: data files with
		/// the Copy to Output option, debug information files, generated resource files, etc.
		/// </remarks>
		internal protected virtual void PopulateOutputFileList (List<FilePath> list, ConfigurationSelector configuration)
		{
			string file = GetOutputFileName (configuration);
			if (file != null)
				list.Add (file);
		}		

		/// <summary>
		/// Builds the project.
		/// </summary>
		/// <returns>
		/// The build result.
		/// </returns>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='configuration'>
		/// Configuration to build.
		/// </param>
		/// <remarks>
		/// This method is invoked to build the project. Support files such as files with the Copy to Output flag will
		/// be copied before calling this method.
		/// </remarks>
		protected virtual BuildResult DoBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			BuildResult res = ItemHandler.RunTarget (monitor, "Build", configuration);
			return res ?? new BuildResult ();
		}

		protected override void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			SetDirty ();
			
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", config.Id, Name), null);
				return;
			}
			
			if (UsingMSBuildEngine ()) {
				DoClean (monitor, config.Selector);
				return;
			}
			
			monitor.Log.WriteLine ("Removing output files...");
			
			// Delete generated files
			foreach (FilePath file in GetOutputFiles (configuration)) {
				if (File.Exists (file)) {
					file.Delete ();
					if (file.ParentDirectory.CanonicalPath != config.OutputDirectory.CanonicalPath && Directory.GetFiles (file.ParentDirectory).Length == 0)
						file.ParentDirectory.Delete ();
				}
			}
	
			DeleteSupportFiles (monitor, configuration);
			
			DoClean (monitor, config.Selector);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Clean complete"));
		}

		protected virtual void DoClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ItemHandler.RunTarget (monitor, "Clean", configuration);
		}

		void GetBuildableReferencedItems (List<SolutionItem> referenced, SolutionItem item, ConfigurationSelector configuration)
		{
			if (referenced.Contains (item))
				return;

			if (item.NeedsBuilding (configuration))
				referenced.Add (item);

			foreach (SolutionItem ritem in item.GetReferencedItems (configuration))
				GetBuildableReferencedItems (referenced, ritem, configuration);
		}

		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
				return;
			}
			DoExecute (monitor, context, configuration);
		}
		
		/// <summary>
		/// Executes the project
		/// </summary>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='context'>
		/// Execution context.
		/// </param>
		/// <param name='configuration'>
		/// Configuration to execute.
		/// </param>
		protected virtual void DoExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
		}

		/// <summary>
		/// Gets the absolute path to the output file generated by this project.
		/// </summary>
		/// <returns>
		/// Absolute path the the output file.
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration.
		/// </param>
		public virtual FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			return FilePath.Null;
		}

		protected internal override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			if (!isDirty) {
				if (CheckNeedsBuild (configuration))
					SetDirty ();
			}
			return isDirty;
		}

		protected internal override void OnSetNeedsBuilding (bool value, ConfigurationSelector configuration)
		{
			isDirty = value;
		}

		void SetDirty ()
		{
			if (!Loading)
				isDirty = true;
		}

		/// <summary>
		/// Checks if the project needs to be built
		/// </summary>
		/// <returns>
		/// <c>True</c> if the project needs to be built (it has changes)
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration.
		/// </param>
		protected virtual bool CheckNeedsBuild (ConfigurationSelector configuration)
		{
			DateTime tim = GetLastBuildTime (configuration);
			if (tim == DateTime.MinValue)
				return true;

			foreach (ProjectFile file in Files) {
				if (file.BuildAction == BuildAction.Content || file.BuildAction == BuildAction.None)
					continue;
				try {
					if (File.GetLastWriteTime (file.FilePath) > tim)
						return true;
				} catch (IOException) {
					// Ignore.
				}
			}

			foreach (SolutionItem pref in GetReferencedItems (configuration)) {
				if (pref.GetLastBuildTime (configuration) > tim || pref.NeedsBuilding (configuration))
					return true;
			}

			try {
				if (File.GetLastWriteTime (FileName) > tim)
					return true;
			} catch {
				// Ignore
			}

			return false;
		}

		protected internal override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			string file = GetOutputFileName (configuration);
			if (file == null)
				return DateTime.MinValue;

			FileInfo finfo = new FileInfo (file);
			if (!finfo.Exists)
				return DateTime.MinValue;
			else
				return finfo.LastWriteTime;
		}

		internal virtual void OnFileChanged (object source, FileEventArgs e)
		{
			foreach (FileEventInfo fi in e) {
				ProjectFile file = GetProjectFile (fi.FileName);
				if (file != null) {
					SetDirty ();
					try {
						NotifyFileChangedInProject (file);
					} catch {
						// Workaround Mono bug. The watcher seems to
						// stop watching if an exception is thrown in
						// the event handler
					}
				}
			}
		}

		protected internal override List<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = base.OnGetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (ProjectFile pf in Files) {
					if (pf.Subtype != Subtype.Directory)
						col.Add (pf.FilePath);
				}
			}
			return col;
		}

		protected internal override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsAdded (objs);
			NotifyFileAddedToProject (objs.OfType<ProjectFile> ());
		}

		protected internal override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);
			NotifyFileRemovedFromProject (objs.OfType<ProjectFile> ());
		}

		internal void NotifyFileChangedInProject (ProjectFile file)
		{
			OnFileChangedInProject (new ProjectFileEventArgs (this, file));
		}

		internal void NotifyFilePropertyChangedInProject (ProjectFile file)
		{
			NotifyModified ("Files");
			OnFilePropertyChangedInProject (new ProjectFileEventArgs (this, file));
		}

		// A collection of files that depend on other files for which the dependencies
		// have not yet been resolved.
		UnresolvedFileCollection unresolvedDeps;

		void NotifyFileRemovedFromProject (IEnumerable<ProjectFile> objs)
		{
			if (!objs.Any ())
				return;
			
			var args = new ProjectFileEventArgs ();
			
			foreach (ProjectFile file in objs) {
				file.SetProject (null);
				args.Add (new ProjectFileEventInfo (this, file));
				if (DependencyResolutionEnabled) {
					unresolvedDeps.Remove (file);
					foreach (ProjectFile f in file.DependentChildren) {
						f.DependsOnFile = null;
						if (!string.IsNullOrEmpty (f.DependsOn))
							unresolvedDeps.Add (f);
					}
					file.DependsOnFile = null;
				}
			}
			SetDirty ();
			NotifyModified ("Files");
			OnFileRemovedFromProject (args);
		}

		void NotifyFileAddedToProject (IEnumerable<ProjectFile> objs)
		{
			if (!objs.Any ())
				return;
			
			var args = new ProjectFileEventArgs ();
			
			foreach (ProjectFile file in objs) {
				if (file.Project != null)
					throw new InvalidOperationException ("ProjectFile already belongs to a project");
				file.SetProject (this);
				args.Add (new ProjectFileEventInfo (this, file));
				ResolveDependencies (file);
			}

			SetDirty ();
			NotifyModified ("Files");
			OnFileAddedToProject (args);
		}

		internal void UpdateDependency (ProjectFile file, FilePath oldPath)
		{
			unresolvedDeps.Remove (file, oldPath);
			ResolveDependencies (file);
		}

		internal void ResolveDependencies (ProjectFile file)
		{
			if (!DependencyResolutionEnabled)
				return;

			if (!file.ResolveParent ())
				unresolvedDeps.Add (file);

			List<ProjectFile> resolved = null;
			foreach (ProjectFile unres in unresolvedDeps.GetUnresolvedFilesForPath (file.FilePath)) {
				if (string.IsNullOrEmpty (unres.DependsOn)) {
					if (resolved == null)
						resolved = new List<ProjectFile> ();
					resolved.Add (unres);
				}
				if (unres.ResolveParent (file)) {
					if (resolved == null)
						resolved = new List<ProjectFile> ();
					resolved.Add (unres);
				}
			}
			if (resolved != null)
				foreach (ProjectFile pf in resolved)
					unresolvedDeps.Remove (pf);
		}

		bool DependencyResolutionEnabled {

			get { return unresolvedDeps != null; }
			set {
				if (value) {
					if (unresolvedDeps != null)
						return;
					unresolvedDeps = new UnresolvedFileCollection ();
					foreach (ProjectFile file in files)
						ResolveDependencies (file);
				} else {
					unresolvedDeps = null;
				}
			}
		}

		internal void NotifyFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			SetDirty ();
			NotifyModified ("Files");
			OnFileRenamedInProject (args);
		}
		
		/// <summary>
		/// Raises the FileRemovedFromProject event.
		/// </summary>
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}

		/// <summary>
		/// Raises the FileAddedToProject event.
		/// </summary>
		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}
		
		/// <summary>
		/// Raises the FileChangedInProject event.
		/// </summary>
		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}
		
		/// <summary>
		/// Raises the FilePropertyChangedInProject event.
		/// </summary>
		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}
		
		/// <summary>
		/// Raises the FileRenamedInProject event.
		/// </summary>
		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}

		/// <summary>
		/// Occurs when a file is removed from this project.
		/// </summary>
		public event ProjectFileEventHandler FileRemovedFromProject;
		
		/// <summary>
		/// Occurs when a file is added to this project.
		/// </summary>
		public event ProjectFileEventHandler FileAddedToProject;

		/// <summary>
		/// Occurs when a file of this project has been modified
		/// </summary>
		public event ProjectFileEventHandler FileChangedInProject;
		
		/// <summary>
		/// Occurs when a property of a file of this project has changed
		/// </summary>
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		
		/// <summary>
		/// Occurs when a file of this project has been renamed
		/// </summary>
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
	}

	public class UnknownProject : Project
	{
		public override string ProjectType {
			get { return ""; }
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return null;
		}
		
		internal protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			BuildResult res = new BuildResult ();
			res.AddError ("Unknown project type");
			return res;
		}
	}

	public delegate void ProjectEventHandler (Object sender, ProjectEventArgs e);
	public class ProjectEventArgs : EventArgs
	{
		public ProjectEventArgs (Project project)
		{
			this.project = project;
		}

		private Project project;
		public Project Project {
			get { return project; }
		}
	}

	class UnresolvedFileCollection
	{
		// Holds a dictionary of files that depend on other files, and for which the dependency
		// has not yet been resolved. The key of the dictionary is the path to a parent
		// file to be resolved, and the value can be a ProjectFile object or a List<ProjectFile>
		// (This may happen if several files depend on the same parent file)
		Dictionary<FilePath,object> unresolvedDeps = new Dictionary<FilePath, object> ();

		public void Remove (ProjectFile file)
		{
			Remove (file, null);
		}

		public void Remove (ProjectFile file, FilePath dependencyPath)
		{
			if (dependencyPath.IsNullOrEmpty) {
				if (string.IsNullOrEmpty (file.DependsOn))
					return;
				dependencyPath = file.DependencyPath;
			}

			object depFile;
			if (unresolvedDeps.TryGetValue (dependencyPath, out depFile)) {
				if ((depFile is ProjectFile) && ((ProjectFile)depFile == file))
					unresolvedDeps.Remove (dependencyPath);
				else if (depFile is List<ProjectFile>) {
					var list = (List<ProjectFile>) depFile;
					list.Remove (file);
					if (list.Count == 1)
						unresolvedDeps [dependencyPath] = list[0];
				}
			}
		}

		public void Add (ProjectFile file)
		{
			object depFile;
			if (unresolvedDeps.TryGetValue (file.DependencyPath, out depFile)) {
				if (depFile is ProjectFile) {
					if ((ProjectFile)depFile != file) {
						var list = new List<ProjectFile> ();
						list.Add ((ProjectFile)depFile);
						list.Add (file);
						unresolvedDeps [file.DependencyPath] = list;
					}
				}
				else if (depFile is List<ProjectFile>) {
					var list = (List<ProjectFile>) depFile;
					if (!list.Contains (file))
						list.Add (file);
				}
			} else
				unresolvedDeps [file.DependencyPath] = file;
		}

		public IEnumerable<ProjectFile> GetUnresolvedFilesForPath (FilePath filePath)
		{
			object depFile;
			if (unresolvedDeps.TryGetValue (filePath, out depFile)) {
				if (depFile is ProjectFile)
					yield return (ProjectFile) depFile;
				else {
					foreach (var f in (List<ProjectFile>) depFile)
						yield return f;
				}
			}
		}
	}
}
