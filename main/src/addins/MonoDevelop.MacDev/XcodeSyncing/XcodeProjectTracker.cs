// 
// XcodeProjectTracker.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.MacDev.ObjCIntegration;
using System.Threading.Tasks;
using MonoDevelop.MacDev.XcodeIntegration;

namespace MonoDevelop.MacDev.XcodeSyncing
{
	public interface IXcodeTrackedProject
	{
		XcodeProjectTracker XcodeProjectTracker { get; }
	}
	
	public abstract class XcodeProjectTracker : IDisposable
	{
		NSObjectInfoService infoService;
		DotNetProject dnp;
		List<NSObjectTypeInfo> userTypes;
		XcodeMonitor xcode;
		
		bool syncing;
		bool disposed;
		
		bool updatingProjectFiles;
		
		public XcodeProjectTracker (DotNetProject dnp, NSObjectInfoService infoService)
		{
			this.dnp = dnp;
			this.infoService = infoService;
			AppleSdkSettings.Changed += DisableSyncing;
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
		
		void EnableSyncing ()
		{
			if (syncing)
				return;
			syncing = true;
			xcode = new XcodeMonitor (dnp.BaseDirectory.Combine ("obj", "Xcode"), dnp.Name);
			
			XC4Debug.Log ("Enabled syncing for project: {0}", dnp.Name);
			
			dnp.FileAddedToProject += FileAddedToProject;
			dnp.FilePropertyChangedInProject += FilePropertyChangedInProject;
			dnp.FileRemovedFromProject += FileRemovedFromProject;
			dnp.FileChangedInProject += FileChangedInProject;
			dnp.NameChanged += ProjectNameChanged;
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn += AppRegainedFocus;
		}
		
		void DisableSyncing ()
		{
			if (!syncing)
				return;
			syncing = false;
			
			xcode.CloseProject ();
			xcode.DeleteProjectDirectory ();
			xcode = null;
			
			XC4Debug.Log ("Disabled syncing for project: {0}", dnp.Name);
			
			dnp.FileAddedToProject -= FileAddedToProject;
			dnp.FilePropertyChangedInProject -= FilePropertyChangedInProject;;
			dnp.FileRemovedFromProject -= FileRemovedFromProject;
			dnp.FileChangedInProject -= FileChangedInProject;
			dnp.NameChanged -= ProjectNameChanged;
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn -= AppRegainedFocus;
		}
		
		void AppRegainedFocus (object sender, EventArgs e)
		{
			if (!syncing)
				return;
			
			bool isOpen = false;
			using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Synchronizing changes from Xcode"))) {
				try {
					isOpen = xcode != null && xcode.IsProjectOpen ();
					if (isOpen) {
						monitor.BeginTask (GettextCatalog.GetString ("Saving Xcode project"), 0);
						xcode.SaveProject ();
					}
				} catch (Exception ex) {
					MonoDevelop.Ide.MessageService.ShowError (
						GettextCatalog.GetString ("MonoDevelop could not communicate with XCode"),
						GettextCatalog.GetString (
							"If XCode is still running, please ensure that all changes have been saved and " +
							"XCode has been exited before continuing, otherwise any new changes may be lost."));
					monitor.Log.WriteLine ("XCode could not be made save pending changes: {0}", ex);
				}
				if (isOpen) {
					monitor.EndTask ();
				}
				
				SyncXcodeChanges (monitor);
			}
			
			if (!isOpen) {
				XC4Debug.Log ("Project closed, disabling syncing");
				DisableSyncing ();
			}
		}
		
		bool OpenFileInXcodeProject (string path)
		{
			bool succeeded = false;
			using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Syncing to Xcode..."))) {
				try {
					EnableSyncing ();
					if (!UpdateTypes (monitor, true) || monitor.IsCancelRequested) {
						return succeeded;
					}
					if (!UpdateXcodeProject (monitor) || monitor.IsCancelRequested) {
						return succeeded;
					}
					xcode.OpenFile (path);
					succeeded = true;
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Could not open file in Xcode project"), ex);
				} finally {
					if (!succeeded)
						DisableSyncing ();
				}
			}
			return succeeded;
		}
		
		public bool OpenDocument (string file)
		{
			XC4Debug.Log ("Opening file {0}", file);
			var xibFile = dnp.Files.GetFile (file);
			System.Diagnostics.Debug.Assert (xibFile != null);
			System.Diagnostics.Debug.Assert (IsInterfaceDefinition (xibFile));
			
			return OpenFileInXcodeProject (xibFile.ProjectVirtualPath);
		}
		
		static bool IsInterfaceDefinition (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.InterfaceDefinition;
		}
		
		bool IncludeInSyncedProject (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Content
				|| (pf.BuildAction == BuildAction.InterfaceDefinition && HasInterfaceDefinitionExtension (pf.FilePath));
		}
		
		#region Project change tracking
		
		void ProjectNameChanged (object sender, SolutionItemRenamedEventArgs e)
		{
			XC4Debug.Log ("Project name changed, resetting sync");
			xcode.CloseProject ();
			xcode.DeleteProjectDirectory ();
			xcode = new XcodeMonitor (dnp.BaseDirectory.Combine ("obj", "Xcode"), dnp.Name);
		}
		
		void FileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			if (syncing && e.Any (finf => finf.Project == dnp && IsInterfaceDefinition (finf.ProjectFile))) {
				if (!dnp.Files.Any (IsInterfaceDefinition)) {
					XC4Debug.Log ("All page files removed, disabling sync");
					DisableSyncing ();
					return;
				}
			}
			
			CheckFileChanges (e);
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			CheckFileChanges (e);
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			//avoid infinite recursion when we add files
			if (updatingProjectFiles)
				return;
			CheckFileChanges (e);
		}

		void FilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			CheckFileChanges (e);
		}
		
		void CheckFileChanges (ProjectFileEventArgs e)
		{
			if (!syncing)
				return;
			
			XC4Debug.Log ("Checking for changed files");
			bool updateTypes = false, updateProject = false;
			foreach (ProjectFileEventInfo finf in e) {
				if (finf.Project != dnp)
					continue;
				if (finf.ProjectFile.BuildAction == BuildAction.Compile) {
					updateTypes = true;
					break;
				} else if (IncludeInSyncedProject (finf.ProjectFile)) {
					updateProject = true;
				}
			}
			
			if (updateTypes) {
				using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Syncing to Xcode..."))) {
					//FIXME: make this async (and safely async)
					//FIXME: only update the project if obj-c types change
					updateProject = UpdateTypes (monitor, true);
				}
			}
			
			if (updateProject) {
				using (var monitor = GetStatusMonitor (GettextCatalog.GetString ("Syncing to Xcode..."))) {
					//FIXME: make this async (and safely async)
					UpdateXcodeProject (monitor);
				}
			}
		}
		
		#endregion
		
		#region Progress monitors
		
		//FIXME: should be use a modal monitor to prevent the user doing unexpected things?
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
		
		bool UpdateTypes (IProgressMonitor monitor, bool force)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Updating Objective-C type information"), 0);
			try {
				var pinfo = infoService.GetProjectInfo (dnp);
				if (pinfo == null)
					throw new Exception ("Did not get project info");
				//FIXME: report progress
				pinfo.Update (force);
				userTypes = pinfo.GetTypes ().Where (t => t.IsUserType).ToList ();
				return true;
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Error updating Objective-C type information"), ex);
				return false;
			}
		}
		
		protected abstract XcodeProject CreateProject (string name);
		
		bool UpdateXcodeProject (IProgressMonitor monitor)
		{
			try {
				xcode.UpdateProject (monitor, CreateSyncList (), CreateProject (dnp.Name));
				return true;
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
				monitor.BeginTask (GettextCatalog.GetString ("Detecting changed files in Xcode"), 0);
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
				
				if (typesAdded)
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
			
			foreach (var file in context.FileSyncJobs) {
				monitor.Log.WriteLine ("Copying {0} file from Xcode: {1}", file.IsFreshlyAdded ? "added" : "changed", file.SyncedRelative);
				
				if (!Directory.Exists (file.Original.ParentDirectory))
					Directory.CreateDirectory (file.Original.ParentDirectory);
				
				var tempFile = file.Original.ParentDirectory.Combine (".#" + file.Original.ParentDirectory.FileName);
				File.Copy (context.ProjectDir.Combine (file.SyncedRelative), tempFile);
				FileService.SystemRename (tempFile, file.Original);
				context.SetSyncTimeToNow (file.SyncedRelative);
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
			bool needsEndTask = false;
			
			if (context.FileSyncJobs.Count == 0)
				return false;
			
			foreach (var file in context.FileSyncJobs) {
				if (!file.IsFreshlyAdded)
					continue;
				
				monitor.Log.WriteLine ("Adding new file to project: {0}", file.SyncedRelative);
				
				FilePath path = new FilePath (file.Original);
				string buildAction = HasInterfaceDefinitionExtension (path) ? BuildAction.InterfaceDefinition : BuildAction.Content;
				context.Project.AddFile (path, buildAction);
				needsEndTask = true;
			}
			
			if (needsEndTask) {
				monitor.EndTask ();
				return true;
			}
			
			return false;
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
			
			monitor.BeginTask (GettextCatalog.GetString ("Generating custom classes defined in UI definition files"), 0);
			
			// Collect our list of custom classes from UI definition files
			foreach (var job in context.FileSyncJobs) {
				if (!HasInterfaceDefinitionExtension (job.Original))
					continue;
				
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
			
			monitor.BeginTask (GettextCatalog.GetString ("Detecting changes made in Xcode"), 0);
			Dictionary<string, NSObjectTypeInfo> newTypes;
			Dictionary<string, ProjectFile> newFiles;
			var updates = context.GetTypeUpdates (monitor, provider, out newTypes, out newFiles);
			if ((updates == null || updates.Count == 0) && newTypes == null && newFiles == null) {
				monitor.Log.WriteLine ("No changes found");
				monitor.EndTask ();
				typesAdded = false;
				return false;
			}
			
			monitor.Log.WriteLine ("Found {0} changed types", updates.Count);
			monitor.EndTask ();
			
			int count = updates.Count + (newTypes != null ? newTypes.Count : 0);
			monitor.BeginTask (GettextCatalog.GetString ("Updating types in MonoDevelop"), count);
			
			// First, add new types...
			if (newTypes != null && newTypes.Count > 0) {
				foreach (var nt in newTypes) {
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
				monitor.Log.WriteLine ("Syncing {0} types from Xcode to file '{1}'", df.Value.Count, df.Key);
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
				foreach (var type in df.Value) {
					context.SetSyncTimeToNow (type.ObjCName + ".h");
					context.SetSyncTimeToNow (type.ObjCName + ".m");
				}
			}
			
			// Add new files to the DotNetProject
			if (newFiles != null) {
				foreach (var f in newFiles) {
					monitor.Log.WriteLine ("Added new designer file {0}", f.Key);
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
			disposed = true;
			DisableSyncing ();
			AppleSdkSettings.Changed -= DisableSyncing;
		}
	}
	
	static class XC4Debug
	{
		static TextWriter writer;
		
		static XC4Debug ()
		{
			FilePath logDir = UserProfile.Current.LogDir;
			FilePath logFile = logDir.Combine ("Xcode4Sync.log");
			FileService.EnsureDirectoryExists (logDir);
			try {
				writer = new StreamWriter (logFile) { AutoFlush = true };
			} catch (Exception ex) {
				LoggingService.LogError ("Could not create Xcode sync logging file", ex);
			}
		}
		
		public static void Log (string message)
		{
			if (writer == null)
				return;
			writer.WriteLine (message);
		}
		
		public static void Log (string messageFormat, params object[] values)
		{
			if (writer == null)
				return;
			writer.WriteLine (messageFormat, values);
		}
		
		public static IProgressMonitor GetLoggingMonitor ()
		{
			return new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor (writer);
		}
	}
}
