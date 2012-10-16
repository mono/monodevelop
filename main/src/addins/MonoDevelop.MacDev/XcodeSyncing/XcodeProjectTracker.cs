// 
// XcodeProjectTracker.cs
//  
// Authors: Michael Hutchinson <mhutchinson@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc.
// Copyright (c) 2012 Xamarin Inc.
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
using System.IO;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.MacDev.ObjCIntegration;
using System.Threading.Tasks;
using MonoDevelop.MacDev.XcodeIntegration;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacDev.XcodeSyncing
{
	public interface IXcodeTrackedProject
	{
		XcodeProjectTracker XcodeProjectTracker { get; }
		FilePath GetBundleResourceId (ProjectFile pf);
		FilePath DefaultBundleResourceDir { get; }
	}
	
	public abstract class XcodeProjectTracker : IDisposable
	{
		readonly NSObjectInfoService infoService;
		readonly DotNetProject dnp;

		object xcode_lock = new object ();
		List<NSObjectTypeInfo> userTypes;
		XcodeMonitor xcode;

		bool updatingProjectFiles;
		bool disposed;
		
		FilePath ObjDir {
			get { return dnp.BaseDirectory.Combine ("obj"); }
		}
		
		FilePath XcodeDir {
			get { return ObjDir.Combine ("Xcode"); }
		}

		public XcodeProjectTracker (DotNetProject dnp, NSObjectInfoService infoService)
		{
			if (dnp == null)
				throw new ArgumentNullException ("dnp");
			this.dnp = dnp;
			this.infoService = infoService;
			AppleSdkSettings.Changed += AppleSdkSettingsChanged;
		}

		public bool ShouldOpenInXcode (FilePath fileName)
		{
			if (!HasInterfaceDefinitionExtension (fileName))
				return false;

			var file = dnp.Files.GetFile (fileName);
			return file != null && (file.BuildAction == BuildAction.InterfaceDefinition);
		}
		
		public virtual bool HasInterfaceDefinitionExtension (FilePath fileName)
		{
			return fileName.HasExtension (".xib");
		}
		
		protected virtual string[] GetFrameworks ()
		{
			return new string[] { "Foundation" };
		}
		
		bool SyncingEnabled {
			get { return xcode != null; }
		}

		void AppleSdkSettingsChanged ()
		{
			lock (xcode_lock) {
				DisableSyncing (true);
			}
		}
		
		void EnableSyncing (IProgressMonitor monitor)
		{
			if (SyncingEnabled)
				return;
			
			monitor.Log.WriteLine ("Enabled syncing for project: {0}", dnp.Name);
			
			xcode = new XcodeMonitor (XcodeDir, dnp.Name);
			
			dnp.FileAddedToProject += FileAddedToProject;
			dnp.FilePropertyChangedInProject += FilePropertyChangedInProject;
			dnp.FileRemovedFromProject += FileRemovedFromProject;
			dnp.FileChangedInProject += FileChangedInProject;
			dnp.NameChanged += ProjectNameChanged;
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn += AppRegainedFocus;
		}
		
		void DisableSyncing (bool closeProject)
		{
			if (!SyncingEnabled)
				return;
			
			XC4Debug.Log ("Disabled syncing for project: {0}", dnp.Name);
			
			XC4Debug.Indent ();
			try {
				if (closeProject)
					xcode.CloseProject ();
				xcode.DeleteProjectDirectory ();
			} finally {
				MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn -= AppRegainedFocus;
				dnp.FileAddedToProject -= FileAddedToProject;
				dnp.FilePropertyChangedInProject -= FilePropertyChangedInProject;;
				dnp.FileRemovedFromProject -= FileRemovedFromProject;
				dnp.FileChangedInProject -= FileChangedInProject;
				dnp.NameChanged -= ProjectNameChanged;

				XC4Debug.Unindent ();
				xcode = null;
			}
		}
		
		void ShowXcodeScriptError ()
		{
			MonoDevelop.Ide.MessageService.ShowError (
				GettextCatalog.GetString ("MonoDevelop could not communicate with Xcode"),
				GettextCatalog.GetString ("If Xcode is still running, please ensure that all changes have been saved and " +
			                          "Xcode has been exited before continuing, otherwise any new changes may be lost."));
		}
		
		void AppRegainedFocus (object sender, EventArgs e)
		{
			lock (xcode_lock) {
				if (!SyncingEnabled)
					return;

				XC4Debug.Log ("MonoDevelop has regained focus.");

				using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Synchronizing changes from Xcode..."))) {
					bool projectOpen = false;

					try {
						// Note: Both IsProjectOpen() and SaveProject() may throw TimeoutExceptions or AppleScriptExceptions
						if ((projectOpen = xcode.IsProjectOpen ()))
							xcode.SaveProject (monitor);
					} catch (Exception ex) {
						ShowXcodeScriptError ();
						monitor.Log.WriteLine ("Xcode failed to save pending changes to project: {0}", ex);

						// Note: This will cause us to disable syncing after we sync whatever we can over from Xcode...
						projectOpen = false;
					}

					try {
						SyncXcodeChanges (monitor);
					} finally {
						if (!projectOpen) {
							XC4Debug.Log ("Xcode project for '{0}' is not open, disabling syncing.", dnp.Name);
							DisableSyncing (false);
						}
					}
				}
			}
		}
		
		bool OpenFileInXcodeProject (IProgressMonitor monitor, string path)
		{
			bool succeeded = false;
			
			try {
				EnableSyncing (monitor);
				
				if (!UpdateTypes (monitor) || monitor.IsCancelRequested)
					return false;
				
				if (!UpdateXcodeProject (monitor) || monitor.IsCancelRequested)
					return false;
				
				xcode.OpenFile (monitor, path);
				succeeded = true;
			} catch (AppleScriptException asex) {
				ShowXcodeScriptError ();
				monitor.ReportError (GettextCatalog.GetString ("Could not open file in Xcode project."), asex);
			} catch (TimeoutException tex) {
				ShowXcodeScriptError ();
				monitor.ReportError (GettextCatalog.GetString ("Could not open file in Xcode project."), tex);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not open file in Xcode project."), ex);
			} finally {
				if (!succeeded)
					DisableSyncing (true);
			}
			
			return succeeded;
		}
		
		public bool OpenDocument (string path)
		{
			var xibFile = dnp.Files.GetFile (path);
			bool success = false;

			Debug.Assert (xibFile != null);
			Debug.Assert (IsInterfaceDefinition (xibFile));

			lock (xcode_lock) {
				using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Opening document '{0}' from project '{1}' in Xcode...", xibFile.ProjectVirtualPath, dnp.Name))) {
					monitor.BeginTask (GettextCatalog.GetString ("Opening document '{0}' from project '{1}' in Xcode...", xibFile.ProjectVirtualPath, dnp.Name), 0);
					success = OpenFileInXcodeProject (monitor, xibFile.ProjectVirtualPath);
					monitor.EndTask ();
				}
			}

			return success;
		}
		
		static bool IsInterfaceDefinition (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.InterfaceDefinition;
		}
		
		bool IncludeInSyncedProject (ProjectFile pf)
		{
			if (pf.BuildAction == BuildAction.BundleResource) {
				var ixtp = dnp as IXcodeTrackedProject;

				if (ixtp == null)
					return false;

				var resource = ixtp.GetBundleResourceId (pf);
				if (resource.ParentDirectory.IsNullOrEmpty)
					return true;

				return false;
			}
			
			return (pf.BuildAction == BuildAction.Content && pf.ProjectVirtualPath.ParentDirectory.IsNullOrEmpty)
				|| (pf.BuildAction == BuildAction.InterfaceDefinition && HasInterfaceDefinitionExtension (pf.FilePath));
		}
		
		#region Project change tracking
		
		void ProjectNameChanged (object sender, SolutionItemRenamedEventArgs e)
		{
			lock (xcode_lock) {
				if (!SyncingEnabled)
					return;

				XC4Debug.Log ("Project '{0}' was renamed to '{1}', resetting Xcode sync.", e.OldName, e.NewName);

				XC4Debug.Indent ();
				try {
					xcode.CloseProject ();
					xcode.DeleteProjectDirectory ();
				} finally {
					XC4Debug.Unindent ();
				}

				xcode = new XcodeMonitor (dnp.BaseDirectory.Combine ("obj", "Xcode"), dnp.Name);
			}
		}
		
		void FileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			lock (xcode_lock) {
				if (!SyncingEnabled)
					return;

				XC4Debug.Log ("Files removed from project '{0}'", dnp.Name);
				foreach (var file in e)
					XC4Debug.Log ("   * Removed: {0}", file.ProjectFile.ProjectVirtualPath);

				XC4Debug.Indent ();
				try {
					if (e.Any (finf => finf.Project == dnp && IsInterfaceDefinition (finf.ProjectFile))) {
						if (!dnp.Files.Any (IsInterfaceDefinition)) {
							XC4Debug.Log ("Last Interface Definition file removed from '{0}', disabling Xcode sync.", dnp.Name);
							DisableSyncing (true);
							return;
						}
					}
				} finally {
					XC4Debug.Unindent ();
				}

				CheckFileChanges (e);
			}
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			lock (xcode_lock) {
				if (!SyncingEnabled)
					return;

				XC4Debug.Log ("Files added to project '{0}'", dnp.Name);
				foreach (var file in e)
					XC4Debug.Log ("   * Added: {0}", file.ProjectFile.ProjectVirtualPath);

				CheckFileChanges (e);
			}
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			lock (xcode_lock) {
				// avoid infinite recursion when we add files
				if (!SyncingEnabled || updatingProjectFiles)
					return;

				XC4Debug.Log ("Files changed in project '{0}'", dnp.Name);
				foreach (var file in e)
					XC4Debug.Log ("   * Changed: {0}", file.ProjectFile.ProjectVirtualPath);

				CheckFileChanges (e);
			}
		}

		void FilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			lock (xcode_lock) {
				if (!SyncingEnabled)
					return;

				XC4Debug.Log ("File properties changed in project '{0}'", dnp.Name);
				foreach (var file in e)
					XC4Debug.Log ("   * Property Changed: {0}", file.ProjectFile.ProjectVirtualPath);

				CheckFileChanges (e);
			}
		}

		void CheckFileChanges (ProjectFileEventArgs e)
		{
			bool updateTypes = false, updateProject = false;
			
			foreach (ProjectFileEventInfo finfo in e) {
				if (finfo.Project != dnp)
					continue;

				if (finfo.ProjectFile.BuildAction == BuildAction.Compile) {
					updateTypes = true;
				} else if (IncludeInSyncedProject (finfo.ProjectFile)) {
					updateProject = true;
				}
			}
			
			if (updateTypes) {
				using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Syncing types to Xcode..."))) {
					//FIXME: make this async (and safely async)
					//FIXME: only update the project if obj-c types change
					updateProject |= UpdateTypes (monitor);
				}
			}
			
			if (updateProject) {
				using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Syncing project to Xcode..."))) {
					//FIXME: make this async (and safely async)
					var running = xcode.CheckRunning ();
					UpdateXcodeProject (monitor);
					if (running) {
						try {
							xcode.OpenProject (monitor);
						} catch (AppleScriptException) {
							ShowXcodeScriptError ();
						} catch (TimeoutException) {
							ShowXcodeScriptError ();
						}
					}
				}
			}
		}
		
		#endregion
		
		#region Progress monitors
		
		//FIXME: should we use a modal monitor to prevent the user doing unexpected things?
		IProgressMonitor GetStatusMonitor (string title)
		{
			IProgressMonitor monitor = MonoDevelop.Ide.IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				title, null, true);
			
			monitor = new MonoDevelop.Core.ProgressMonitoring.AggregatedProgressMonitor (
				monitor, XC4Debug.GetLoggingMonitor ());
			
			return monitor;
		}
		
		#endregion
		
		#region Outbound syncing
		
		bool UpdateTypes (IProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Updating Objective-C type information"), 0);
			try {
				var pinfo = infoService.GetProjectInfo (dnp);
				if (pinfo == null)
					return true;
				// FIXME: report progress
				pinfo.Update (true);
				userTypes = pinfo.GetTypes ().Where (t => t.IsUserType).ToList ();
				return true;
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Error updating Objective-C type information"), ex);
				return false;
			} finally {
				monitor.EndTask ();
			}
		}
		
		protected abstract XcodeProject CreateProject (string name);
		
		bool UpdateXcodeProject (IProgressMonitor monitor)
		{
			try {
				// Ensure that the obj directory and all subfiles/subdirectories
				// are writeable so we can create the temp files for xcode syncing
				dnp.BaseDirectory.MakeWritable ();
				ObjDir.MakeWritable ();
				XcodeDir.MakeWritable (true);
				
				xcode.UpdateProject (monitor, CreateSyncList (), CreateProject (dnp.Name));
				return true;
			} catch (AppleScriptException asex) {
				ShowXcodeScriptError ();
				monitor.ReportError (GettextCatalog.GetString ("Error updating Xcode project"), asex);
				return false;
			} catch (TimeoutException tex) {
				ShowXcodeScriptError ();
				monitor.ReportError (GettextCatalog.GetString ("Error updating Xcode project"), tex);
				return false;
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Error updating Xcode project"), ex);
				return false;
			}
		}
		
		List<XcodeSyncedItem> CreateSyncList ()
		{
			var syncList = new List<XcodeSyncedItem> ();
			foreach (var file in dnp.Files.Where (IncludeInSyncedProject))
				syncList.Add (new XcodeSyncedContent (file));
			foreach (var type in userTypes)
				syncList.Add (new XcodeSyncedType (type, GetFrameworks ()));
			
			return syncList;
		}
		
		#endregion
		
		#region Inbound syncing
		
		bool SyncXcodeChanges (IProgressMonitor monitor)
		{
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Detecting changed files in Xcode..."), 0);
				var changeCtx = xcode.GetChanges (monitor, infoService, dnp);
				monitor.EndTask ();
				
				updatingProjectFiles = true;
				bool filesAdded = false;
				bool typesAdded = false;
				
				// First, copy any changed/added resource files to MonoDevelop's project directory.
				CopyFilesToMD (monitor, changeCtx);
				
				// Then update CLI types.
				if (UpdateCliTypes (monitor, changeCtx, out typesAdded))
					filesAdded = true;
				
				// Next, parse UI definition files for custom classes
				if (AddCustomClassesFromUIDefinitionFiles (monitor, changeCtx))
					typesAdded = true;
				
				// Finally, add any newly created resource files to the DotNetProject.
				if (AddFilesToMD (monitor, changeCtx))
					filesAdded = true;
				
				// Save the DotNetProject.
				if (filesAdded || typesAdded)
					Ide.IdeApp.ProjectOperations.Save (dnp);
				
				// Notify MonoDevelop of file changes.
				Gtk.Application.Invoke (delegate {
					// FIXME: this should probably filter out any IsFreshlyAdded file jobs
					FileService.NotifyFilesChanged (changeCtx.FileSyncJobs.Select (f => f.Original));
				});
				
				if (typesAdded && xcode.CheckRunning ())
					UpdateXcodeProject (monitor);
				
				return true;
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Error synchronizing changes from Xcode"), ex);
				return false;
			} finally {
				updatingProjectFiles = false;
			}
		}
		
		/// <summary>
		/// Copies resource files from the Xcode project (back) to the MonoDevelop project directory.
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor.
		/// </param>
		/// <param name='context'>
		/// The sync context.
		/// </param>
		void CopyFilesToMD (IProgressMonitor monitor, XcodeSyncBackContext context)
		{
			if (context.FileSyncJobs.Count == 0)
				return;
			
			monitor.BeginStepTask ("Copying files from Xcode back to MonoDevelop...", context.FileSyncJobs.Count, 1);
			
			foreach (var file in context.FileSyncJobs) {
				monitor.Log.WriteLine ("Copying {0} file from Xcode: {1}", file.IsFreshlyAdded ? "new" : "changed", file.SyncedRelative);
				
				if (!Directory.Exists (file.Original.ParentDirectory))
					Directory.CreateDirectory (file.Original.ParentDirectory);
				
				var tempFile = file.Original.ParentDirectory.Combine (".#" + file.Original.ParentDirectory.FileName);
				FilePath path = context.ProjectDir.Combine (file.SyncedRelative);
				
				if (File.Exists (path)) {
					File.Copy (path, tempFile);
					FileService.SystemRename (tempFile, file.Original);
					
					DateTime mtime = File.GetLastWriteTime (file.Original);
					context.SetSyncTime (file.SyncedRelative, mtime);
				} else {
					monitor.ReportWarning (string.Format ("'{0}' does not exist.", file.SyncedRelative));
				}
				
				monitor.Step (1);
			}
			
			monitor.EndTask ();
		}
		
		/// <summary>
		/// Adds any newly created resource files to MonoDevelop's DotNetProject.
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor.
		/// </param>
		/// <param name='context'>
		/// The sync context.
		/// </param>
		/// <returns>
		/// Returns whether or not new files were added to the project.
		/// </returns>
		bool AddFilesToMD (IProgressMonitor monitor, XcodeSyncBackContext context)
		{
			bool filesAdded = false;
			
			if (context.FileSyncJobs.Count == 0)
				return false;
			
			foreach (var file in context.FileSyncJobs) {
				if (!file.IsFreshlyAdded)
					continue;
				
				monitor.Log.WriteLine ("Adding '{0}' to project '{1}'", file.SyncedRelative, dnp.Name);

				FilePath path = new FilePath (file.Original);
				string buildAction = HasInterfaceDefinitionExtension (path) ? BuildAction.InterfaceDefinition : BuildAction.BundleResource;
				context.Project.AddFile (path, buildAction);
				filesAdded = true;
			}
			
			return filesAdded;
		}
		
		protected virtual IEnumerable<NSObjectTypeInfo> GetCustomTypesFromUIDefinition (FilePath fileName)
		{
			yield break;
		}
		
		/// <summary>
		/// Adds the custom classes from user interface definition files.
		/// </summary>
		/// <returns>
		/// <c>true</c> if new types were added to the project, or <c>false</c> otherwise.
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor.
		/// </param>
		/// <param name='context'>
		/// A sync-back context.
		/// </param>
		bool AddCustomClassesFromUIDefinitionFiles (IProgressMonitor monitor, XcodeSyncBackContext context)
		{
			var provider = dnp.LanguageBinding.GetCodeDomProvider ();
			var options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			var writer = MonoDevelop.DesignerSupport.CodeBehindWriter.CreateForProject (
				new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), dnp);
			bool addedTypes = false;
			bool beganTask = false;
			
			// Collect our list of custom classes from UI definition files
			foreach (var job in context.FileSyncJobs) {
				if (!HasInterfaceDefinitionExtension (job.Original))
					continue;
				
				if (!beganTask) {
					monitor.BeginTask (GettextCatalog.GetString ("Generating custom classes defined in UI definition files..."), 0);
					beganTask = true;
				}
				
				string relative = job.SyncedRelative.ParentDirectory;
				string dir = dnp.BaseDirectory;
				
				if (!string.IsNullOrEmpty (relative))
					dir = Path.Combine (dir, relative);
				
				foreach (var type in GetCustomTypesFromUIDefinition (job.Original)) {
					if (context.ProjectInfo.ContainsType (type.ObjCName))
						continue;
					
					string designerPath = Path.Combine (dir, type.ObjCName + ".designer." + provider.FileExtension);
					string path = Path.Combine (dir, type.ObjCName + "." + provider.FileExtension);
					string ns = dnp.GetDefaultNamespace (path);
					
					type.CliName = ns + "." + provider.CreateValidIdentifier (type.ObjCName);
					
					if (provider is Microsoft.CSharp.CSharpCodeProvider) {
						CodebehindTemplateBase cs = new CSharpCodeTypeDefinition () {
							WrapperNamespace = infoService.WrapperRoot,
							Provider = provider,
							Type = type,
						};
						
						writer.WriteFile (path, cs.TransformText ());
						
						List<NSObjectTypeInfo> types = new List<NSObjectTypeInfo> ();
						types.Add (type);
						
						cs = new CSharpCodeCodebehind () {
							WrapperNamespace = infoService.WrapperRoot,
							Provider = provider,
							Types = types,
						};
						
						writer.WriteFile (designerPath, cs.TransformText ());
						
						context.ProjectInfo.InsertUpdatedType (type);
					} else {
						// FIXME: implement support for non-C# languages
					}
					
					dnp.AddFile (new ProjectFile (path));
					dnp.AddFile (new ProjectFile (designerPath) { DependsOn = path });
					addedTypes = true;
				}
			}
			
			writer.WriteOpenFiles ();
			
			if (beganTask)
				monitor.EndTask ();
			
			return addedTypes;
		}
		
		/// <summary>
		/// Updates the cli types.
		/// </summary>
		/// <returns>
		/// Returns whether or not any files were added to the project.
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor.
		/// </param>
		/// <param name='context'>
		/// A sync-back context.
		/// </param>
		/// <param name='typesAdded'>
		/// An output variable specifying whether or not any types were added to the project.
		/// </param>
		bool UpdateCliTypes (IProgressMonitor monitor, XcodeSyncBackContext context, out bool typesAdded)
		{
			var provider = dnp.LanguageBinding.GetCodeDomProvider ();
			var options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			var writer = MonoDevelop.DesignerSupport.CodeBehindWriter.CreateForProject (
				new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), dnp);
			
			monitor.BeginTask (GettextCatalog.GetString ("Detecting changes made in Xcode to custom user types..."), 0);
			Dictionary<string, NSObjectTypeInfo> newTypes;
			Dictionary<string, ProjectFile> newFiles;
			var updates = context.GetTypeUpdates (monitor, provider, out newTypes, out newFiles);
			if ((updates == null || updates.Count == 0) && newTypes == null && newFiles == null) {
				monitor.Log.WriteLine ("No changes found.");
				monitor.EndTask ();
				typesAdded = false;
				return false;
			}
			monitor.EndTask ();
			
			int count = updates.Count + (newTypes != null ? newTypes.Count : 0);
			monitor.BeginTask (GettextCatalog.GetString ("Updating custom user types in MonoDevelop..."), count);
			
			// First, add new types...
			if (newTypes != null && newTypes.Count > 0) {
				foreach (var nt in newTypes) {
					monitor.Log.WriteLine ("Adding new type: {0}", nt.Value.CliName);
					
					if (provider is Microsoft.CSharp.CSharpCodeProvider) {
						var cs = new CSharpCodeTypeDefinition () {
							WrapperNamespace = infoService.WrapperRoot,
							Provider = provider,
							Type = nt.Value,
						};
						
						string baseDir = Path.GetDirectoryName (nt.Key);
						if (!Directory.Exists (baseDir))
							Directory.CreateDirectory (baseDir);
						
						writer.WriteFile (nt.Key, cs.TransformText ());
					} else {
						// FIXME: implement support for non-C# languages
					}
					
					monitor.Step (1);
				}
				
				typesAdded = true;
			} else {
				typesAdded = false;
			}
			
			// Next, generate the designer files for any added/changed types
			foreach (var df in updates) {
				monitor.Log.WriteLine ("Generating designer file: {0}", df.Key);
				if (provider is Microsoft.CSharp.CSharpCodeProvider) {
					var cs = new CSharpCodeCodebehind () {
						Types = df.Value,
						WrapperNamespace = infoService.WrapperRoot,
						Provider = provider,
					};
					writer.WriteFile (df.Key, cs.TransformText ());
				} else {
					var ccu = GenerateCompileUnit (provider, options, df.Key, df.Value);
					writer.WriteFile (df.Key, ccu);
				}
				
				monitor.Step (1);
			}
			
			writer.WriteOpenFiles ();
			
			// Update sync timestamps
			foreach (var df in updates) {
				foreach (var type in df.Value)
					context.SetSyncTime (type.ObjCName + ".h", DateTime.Now);
			}
			
			// Add new files to the DotNetProject
			if (newFiles != null) {
				foreach (var f in newFiles) {
					monitor.Log.WriteLine ("Added new designer file: {0}", f.Key);
					dnp.AddFile (f.Value);
				}
			}
			
			monitor.EndTask ();
			
			return newFiles != null && newFiles.Count > 0;
		}
		
		System.CodeDom.CodeCompileUnit GenerateCompileUnit (System.CodeDom.Compiler.CodeDomProvider provider,
			System.CodeDom.Compiler.CodeGeneratorOptions options, string file, List<NSObjectTypeInfo> types)
		{
			var ccu = new System.CodeDom.CodeCompileUnit ();
			var namespaces = new Dictionary<string, System.CodeDom.CodeNamespace> ();
			foreach (var t in types) {
				System.CodeDom.CodeTypeDeclaration type;
				string nsName;
				System.CodeDom.CodeNamespace ns;
				t.GenerateCodeTypeDeclaration (provider, options, infoService.WrapperRoot, out type, out nsName);
				if (!namespaces.TryGetValue (nsName, out ns)) {
					namespaces[nsName] = ns = new System.CodeDom.CodeNamespace (nsName);
					ccu.Namespaces.Add (ns);
				}
				ns.Types.Add (type);
			}
			return ccu;
		}
		
		#endregion
		
		public void Dispose ()
		{
			if (disposed)
				return;

			AppleSdkSettings.Changed -= AppleSdkSettingsChanged;
			lock (xcode_lock) {
				DisableSyncing (true);
			}
			disposed = true;
		}
	}
	
	static class XC4Debug
	{
		static int indentLevel = 0;
		static TextWriter writer;
		
		static XC4Debug ()
		{
			FilePath logDir = UserProfile.Current.LogDir;
			FilePath logFile = logDir.Combine (UniqueLogFile);
			
			FileService.EnsureDirectoryExists (logDir);
			
			try {
				var stream = File.Open (logFile, FileMode.Create, FileAccess.Write, FileShare.Read);
				writer = new StreamWriter (stream) { AutoFlush = true };
			} catch (Exception ex) {
				LoggingService.LogError ("Could not create Xcode sync logging file", ex);
			}
		}
		
		static string UniqueLogFile {
			get {
				return string.Format ("Xcode4Sync-{0}.log", LoggingService.LogTimestamp.ToString ("yyyy-MM-dd__HH-mm-ss"));
			}
		}
		
		static string TimeStamp {
			get { return string.Format ("[{0}] ", DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss.f")); }
		}
		
		public static void Indent ()
		{
			indentLevel++;
		}
		
		public static void Unindent ()
		{
			indentLevel--;
		}
		
		public static void Log (string message)
		{
			if (writer == null)
				return;
			
			writer.WriteLine (TimeStamp + new string (' ', indentLevel * 3) + message);
		}
		
		public static void Log (string format, params object[] args)
		{
			if (writer == null)
				return;
			
			writer.WriteLine (TimeStamp + new string (' ', indentLevel * 3) + format, args);
		}
		
		public static IProgressMonitor GetLoggingMonitor ()
		{
			if (writer == null)
				return new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ();
			
			return new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor (writer) { EnableTimeStamp = true, WrapText = false };
		}
	}
}
