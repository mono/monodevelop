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
using System.Linq;
using System.IO;
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
			if (!HasPageExtension (fileName))
				return false;
			var file = dnp.Files.GetFile (fileName);
			return file != null && file.BuildAction == BuildAction.Page;
		}
		
		public virtual bool HasPageExtension (FilePath fileName)
		{
			return fileName.HasExtension (".xib");
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
			
			bool isOpen = xcode != null && xcode.IsProjectOpen ();
			
			if (isOpen) {
				XC4Debug.Log ("Project open, ensuring files are saved");
				xcode.SaveProject ();
			}
			
			DetectXcodeChanges ();
			
			if (!isOpen) {
				XC4Debug.Log ("Project closed, disabling syncing");
				DisableSyncing ();
			}
		}
		
		void OpenXcodeProject ()
		{
			//FIXME: show a UI and progress while we do the initial sync
			XC4Debug.Log ("Syncing to Xcode");
			EnableSyncing ();
			UpdateTypes (true);
			UpdateXcodeProject ();
			xcode.OpenProject ();
		}
		
		public void OpenDocument (string file)
		{
			OpenXcodeProject ();
			
			XC4Debug.Log ("Opening file {0}", file);
			var xibFile = dnp.Files.GetFile (file);
			System.Diagnostics.Debug.Assert (xibFile != null);
			System.Diagnostics.Debug.Assert (IsPage (xibFile));
			xcode.OpenFile (xibFile.ProjectVirtualPath);
		}
		
		static bool IsPage (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Page;
		}
		
		static bool IsContent (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Content;
		}
		
		bool IncludeInSyncedProject (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Content
				|| (pf.BuildAction == BuildAction.Page && HasPageExtension (pf.FilePath));
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
			if (syncing && e.Any (finf => finf.Project == dnp && IsPage (finf.ProjectFile))) {
				if (!dnp.Files.Any (IsPage)) {
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
			bool updateTypes = false, update = false;
			foreach (ProjectFileEventInfo finf in e) {
				if (finf.Project != dnp)
					continue;
				if (finf.ProjectFile.BuildAction == BuildAction.Compile) {
					updateTypes = update = true;
					break;
				} else if (IncludeInSyncedProject (finf.ProjectFile)) {
					update = true;
				}
			}
			
			//FIXME: make this async
			if (updateTypes)
				UpdateTypes (true);
			if (update)
				UpdateXcodeProject ();
		}
		
		#endregion
		
		#region Outbound syncing
		
		void UpdateTypes (bool force)
		{
			XC4Debug.Log ("Updating CLI type information");
			var pinfo = infoService.GetProjectInfo (dnp);
			if (pinfo == null) {
				Console.WriteLine ("Did not get project info");
				return;
			}
			pinfo.Update (force);
			userTypes = pinfo.GetTypes ().Where (t => t.IsUserType).ToList ();
		}
		
		protected abstract XcodeProject CreateProject (string name);
		
		//FIXME: report errors
		void UpdateXcodeProject ()
		{
			xcode.UpdateProject (CreateSyncList (), CreateProject (dnp.Name));
		}
		
		List<XcodeSyncedItem> CreateSyncList ()
		{
			var syncList = new List<XcodeSyncedItem> ();
			foreach (var file in dnp.Files.Where (IncludeInSyncedProject))
				syncList.Add (new XcodeSyncedContent (file));
			foreach (var file in dnp.Files.Where (f => f.BuildAction == BuildAction.Resource))
				syncList.Add (new XcodeSyncedResource (file));
			
			foreach (var type in userTypes) {
				syncList.Add (new XcodeSyncedType (type));
			}
			return syncList;
		}
		
		#endregion
		
		#region Inbound syncing
		
		//FIXME: make this async so it doesn't block the UI
		void DetectXcodeChanges ()
		{
			XC4Debug.Log ("Detecting changes in synced files");
			var changeCtx = xcode.GetChanges (infoService, dnp);
			updatingProjectFiles = true;
			UpdateCliTypes (changeCtx);
			CopyFilesToMD (changeCtx);
			updatingProjectFiles = false;
		}
		
		void CopyFilesToMD (XcodeSyncBackContext context)
		{
			foreach (var file in context.FileSyncJobs) {
				XC4Debug.Log ("Copying changed file from Xcode: {0}", file.SyncedRelative);
				var tempFile = file.Original.ParentDirectory.Combine (".#" + file.Original.ParentDirectory.FileName);
				File.Copy (context.ProjectDir.Combine (file.SyncedRelative), tempFile);
				FileService.SystemRename (tempFile, file.Original);
				context.SetSyncTimeToNow (file.SyncedRelative);
			}
			Gtk.Application.Invoke (delegate {
				FileService.NotifyFilesChanged (context.FileSyncJobs.Select (f => f.Original));
			});
		}
		
		//FIXME: error handling
		void UpdateCliTypes (XcodeSyncBackContext context)
		{
			var provider = dnp.LanguageBinding.GetCodeDomProvider ();
			var options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			var writer = MonoDevelop.DesignerSupport.CodeBehindWriter.CreateForProject (
				new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), dnp);
			
			Dictionary<string,ProjectFile> newFiles;
			XC4Debug.Log ("Getting changed types from Xcode");
			var updates = context.GetTypeUpdates (out newFiles);
			if (updates == null) {
				XC4Debug.Log ("No changed types from Xcode found");
				return;
			}
			
			foreach (var df in updates) {
				XC4Debug.Log ("Syncing {0} types from Xcode to file {1}", df.Value.Count, df.Key);
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
			}
			writer.WriteOpenFiles ();
			
			foreach (var df in updates) {
				foreach (var type in df.Value) {
					context.SetSyncTimeToNow (type.ObjCName + ".h");
					context.SetSyncTimeToNow (type.ObjCName + ".m");
				}
			}
			
			foreach (var job in context.TypeSyncJobs) {
				context.ProjectInfo.InsertUpdatedType (job.Type);
			}
			
			if (newFiles != null) {
				foreach (var f in newFiles) {
					XC4Debug.Log ("Added new designer files {0}", f.Key);
					dnp.AddFile (f.Value);
				}
				Ide.IdeApp.ProjectOperations.Save (dnp);
			}
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
		[System.Diagnostics.Conditional ("DEBUG_XCODE_SYNC")]
		public static void Log (string message)
		{
			Console.Write ("XC4: ");
			Console.WriteLine (message);
		}
		
		[System.Diagnostics.Conditional ("DEBUG_XCODE_SYNC")]
		public static void Log (string messageFormat, params object[] values)
		{
			Console.Write ("XC4: ");
			Console.WriteLine (messageFormat, values);
		}
	}
}