// 
// XcodeMonitor.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) Xamarin, Inc. (http://xamarin.com)
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

using MonoDevelop.MacInterop;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.MacDev.PlistEditor;



namespace MonoDevelop.MacDev.XcodeSyncing
{
	class XcodeMonitor
	{
		FilePath projectDir;
		int nextHackDir = 0;
		bool updating = false;
		string name;
		
		FilePath xcproj, xcprojDir;
		List<XcodeSyncedItem> items;
		Dictionary<string,XcodeSyncedItem> itemMap = new Dictionary<string, XcodeSyncedItem> ();
		Dictionary<string,DateTime> syncTimeCache = new Dictionary<string, DateTime> ();
		
		XcodeProject pendingProjectWrite;
		
		public XcodeMonitor (FilePath projectDir, string name)
		{
			this.projectDir = projectDir;
			this.name = name;
			HackGetNextProjectDir ();
		}
		
		public bool ProjectExists ()
		{
			return Directory.Exists (xcproj);
		}
		
		public void UpdateProject (IProgressMonitor monitor, List<XcodeSyncedItem> allItems, XcodeProject emptyProject)
		{
			if (updating) {
				// Prevents a race condition with new files being added to the DotNetProject
				// by the code syncing new files that the user created with Xcode back to
				// MonoDevelop
				return;
			}
			
			items = allItems;
			
			monitor.BeginTask (GettextCatalog.GetString ("Updating Xcode project..."), items.Count);
			monitor.Log.WriteLine ("Updating synced project with {0} items", items.Count);
			XC4Debug.Log ("Updating synced project with {0} items", items.Count);
		
			var ctx = new XcodeSyncContext (xcprojDir, syncTimeCache);
			
			var toRemove = new HashSet<string> (itemMap.Keys);
			var toClose = new HashSet<string> ();
			var syncList = new List<XcodeSyncedItem> ();
			bool updateProject = false;
			
			foreach (var item in items) {
				bool needsSync = item.NeedsSyncOut (ctx);
				if (needsSync)
					syncList.Add (item);
				
				var files = item.GetTargetRelativeFileNames ();
				foreach (var f in files) {
					toRemove.Remove (f);
					if (!itemMap.ContainsKey (f)) {
						updateProject = true;
					} else if (needsSync) {
						toClose.Add (f);
					}
					itemMap [f] = item;
				}
			}
			updateProject = updateProject || toRemove.Any ();
			
			bool removedOldProject = false;
			if (updateProject) {
				if (pendingProjectWrite == null && ProjectExists ()) {
					monitor.Log.WriteLine ("Project file needs to be updated, closing and removing old project");
					CloseProject ();
					DeleteXcproj ();
					removedOldProject = true;
				}
			} else {
				foreach (var f in toClose)
					CloseFile (xcprojDir.Combine (f));
			}
			
			foreach (var f in toRemove) {
				itemMap.Remove (f);
				syncTimeCache.Remove (f);
				var path = xcprojDir.Combine (f);
				if (File.Exists (path))
					File.Delete (path);
			}
			
			if (removedOldProject) {
				HackRelocateProject ();
			}
			
			foreach (var item in items) {
				if (updateProject)
					item.AddToProject (emptyProject, xcprojDir);
			}
			
			foreach (var item in syncList) {
				monitor.Log.WriteLine ("Syncing item {0}", item.GetTargetRelativeFileNames ()[0]);
				item.SyncOut (ctx);
				monitor.Step (1);
			}
			
			if (updateProject) {
				monitor.Log.WriteLine ("Queuing Xcode project {0} to write when opened", xcprojDir);
				pendingProjectWrite = emptyProject;
			}
			monitor.EndTask ();
			monitor.ReportSuccess (GettextCatalog.GetString ("Xcode project updated."));
		}
		
		// Xcode keeps some kind of internal lock on project files while it's running and
		// gets extremely unhappy if a new or changed project is in the same location as
		// a previously opened one.
		//
		// To work around this we increment a subdirectory name and use that, and do some
		// careful bookkeeping to reduce the unnecessary I/O overhead that this adds.
		//
		void HackRelocateProject ()
		{
			var oldProjectDir = xcprojDir;
			HackGetNextProjectDir ();
			XC4Debug.Log ("Relocating {0} to {1}", oldProjectDir, xcprojDir);
			foreach (var f in syncTimeCache) {
				var target = xcprojDir.Combine (f.Key);
				var src = oldProjectDir.Combine (f.Key);
				var parent = target.ParentDirectory;
				if (!Directory.Exists (parent))
					Directory.CreateDirectory (parent);
				File.Move (src, target);
			}
		}
		
		void HackGetNextProjectDir ()
		{
			do {
				this.xcprojDir = projectDir.Combine (nextHackDir.ToString ());
				nextHackDir++;
			} while (Directory.Exists (this.xcprojDir));
			this.xcproj = xcprojDir.Combine (name + ".xcodeproj");
		}
		
		void AddNewFilesToProject (IProgressMonitor monitor, XcodeSyncBackContext ctx)
		{
			HashSet<string> knownItems = new HashSet<string> ();
			List<string> addedFiles = new List<string> ();
			
			foreach (var item in items) {
				foreach (var file in item.GetTargetRelativeFileNames ())
					knownItems.Add (Path.Combine (ctx.ProjectDir, file));
			}
			
			foreach (var file in Directory.EnumerateFiles (ctx.ProjectDir)) {
				if (file.EndsWith ("~") || file.EndsWith (".m"))
					continue;
				
				if (knownItems.Contains (file))
					continue;
				
				addedFiles.Add (file);
			}
			
			if (addedFiles.Count == 0)
				return;
			
			monitor.Log.WriteLine ("Importing externally added project files...");
			
			string baseProjectDir = ctx.Project.BaseDirectory.CanonicalPath;
			for (int i = 0; i < addedFiles.Count; i++) {
				string path = Path.Combine (baseProjectDir, Path.GetFileName (addedFiles[i]));
				
				if (path.EndsWith (".h")) {
					// Need to parse this file and hope we can port it to C#...
					string designerFile = Path.Combine (baseProjectDir, Path.GetFileNameWithoutExtension (path) + ".designer.cs");
					string csharpFile = Path.Combine (baseProjectDir, Path.GetFileNameWithoutExtension (path) + ".cs");
					string ns = ctx.Project.GetDefaultNamespace (csharpFile);
					
					var parsed = NSObjectInfoService.ParseHeader (ns, addedFiles[i], new string[] { designerFile });
					
					ctx.ProjectInfo.ResolveTypes (parsed);
					if (!ctx.ProjectInfo.ContainsErrors && !File.Exists (csharpFile) && !File.Exists (designerFile)) {
						File.Create (designerFile);
						
						using (var writer = File.CreateText (csharpFile)) {
							writer.WriteLine ("// THIS FILE WAS AUTOGENERATED BY IMPORTING AN OBJECTIVE-C HEADER FROM XCODE");
							writer.WriteLine ("// You... probably don't want to leave this as just a stub.");
							writer.WriteLine ();
							writer.WriteLine ("using System;");
							writer.WriteLine ("using System.Drawing;");
							writer.WriteLine ();
							if (parsed.Frameworks.Count > 0) {
								foreach (var framework in parsed.Frameworks)
									writer.WriteLine ("using MonoTouch.{0};", framework);
								writer.WriteLine ();
							}
							writer.WriteLine ("namespace {0} {{", ns);
							writer.WriteLine ("\tpublic partial class {0} : {1}", parsed.ObjCName, parsed.BaseObjCType);
							writer.WriteLine ("\t{");
							writer.WriteLine ("\t\tpublic {0} (IntPtr handle) : base (handle)", parsed.ObjCName);
							writer.WriteLine ("\t\t{");
							writer.WriteLine ("\t\t}");
							writer.WriteLine ("\t}");
							writer.WriteLine ("}");
						}
						
						ctx.Project.AddFile (csharpFile, BuildAction.Compile);
						ctx.Project.AddFile (designerFile, BuildAction.Compile).DependsOn = csharpFile;
						
						XcodeSyncObjcBackJob job = new XcodeSyncObjcBackJob ();
						job.DesignerFile = designerFile;
						job.HFile = addedFiles[i];
						job.Type = parsed;
						
						ctx.TypeSyncJobs.Add (job);
					} else {
						monitor.Log.WriteLine ("Sync back skipped {0} because of errors in project info.", addedFiles[i]);
					}
				} else {
					// Simply copy this file over to our MonoDevelop project
					string buildAction = path.EndsWith (".xib") || path.EndsWith (".storyboard") ? BuildAction.InterfaceDefinition : BuildAction.Content;
					File.Copy (addedFiles[i], path);
					ctx.Project.AddFile (path, buildAction);
				}
			}
			
			monitor.ReportSuccess (string.Format (GettextCatalog.GetPluralString ("Imported {0} file", "Imported {0} files", addedFiles.Count), addedFiles.Count));
		}

		public XcodeSyncBackContext GetChanges (IProgressMonitor monitor, NSObjectInfoService infoService, DotNetProject project)
		{
			// Don't let MonoDevelop clobber the Xcode project directory while we're still syncing files back to MonoDevelop
			updating = true;
			
			var ctx = new XcodeSyncBackContext (xcprojDir, syncTimeCache, infoService, project);
			var needsSync = new List<XcodeSyncedItem> (items.Where (i => i.NeedsSyncBack (ctx)));
			AddNewFilesToProject (monitor, ctx);
			if (needsSync.Count > 0) {
				monitor.Log.WriteLine ("Synchronizing external project changes...");
				for (int i = 0; i < needsSync.Count; i++) {
					var item = needsSync [i];
					item.SyncBack (ctx);
				}
				monitor.ReportSuccess (string.Format (GettextCatalog.GetPluralString ("Synchronized {0} file", "Synchronized {0} files", needsSync.Count), needsSync.Count));
			}
			
			// Okay, we're done, let MonoDevelop clobber stuff again...
			updating = false;
			
			return ctx;
		}
		
		public bool CheckRunning ()
		{
			var appPathKey = new NSString ("NSApplicationPath");
			var appPathVal = new NSString (AppleSdkSettings.XcodePath);
			return NSWorkspace.SharedWorkspace.LaunchedApplications.Any (app => appPathVal.Equals (app[appPathKey]));
		}
		
		public void SaveProject ()
		{
			AppleScript.Run (XCODE_SAVE_IN_PATH, AppleSdkSettings.XcodePath, xcprojDir);
		}

		void SyncProject ()
		{
			if (pendingProjectWrite != null) {
				pendingProjectWrite.Generate (xcprojDir);
				pendingProjectWrite = null;
			}
		}
		
		public void OpenProject ()
		{
			SyncProject ();
			AppleScript.Run (XCODE_OPEN_PROJECT, AppleSdkSettings.XcodePath, xcproj);
		}
		
		public void OpenFile (string relativeName)
		{
			XC4Debug.Log ("Opening file in Xcode: {0}", relativeName);
			SyncProject ();
			AppleScript.Run (XCODE_OPEN_PROJECT_FILE, AppleSdkSettings.XcodePath, xcproj, xcprojDir.Combine (relativeName));
		}
		
		public void DeleteProjectDirectory ()
		{
			XC4Debug.Log ("Deleting temp project directories");
			bool isRunning = CheckRunning ();
			if (Directory.Exists (xcprojDir))
				Directory.Delete (xcprojDir, true);
			if (isRunning) {
				XC4Debug.Log ("Xcode still running, leaving empty directory in place to prevent name re-use");
				Directory.CreateDirectory (xcprojDir);
			} else {
				XC4Debug.Log ("Xcode not running, removing all temp directories");
				if (Directory.Exists (this.projectDir))
					Directory.Delete (this.projectDir, true);
			}
			XC4Debug.Log ("Deleting derived data");
			DeleteDerivedData ();
		}
		
		void DeleteXcproj ()
		{
			XC4Debug.Log ("Deleting project artifacts");
			if (Directory.Exists (xcproj))
				Directory.Delete (xcproj, true);
		}
		
		static string GetWorkspacePath (string infoPlist)
		{
			try {
				var dict = PDictionary.Load (infoPlist);
				PString val;
				if (dict.TryGetValue<PString>("WorkspacePath", out val))
					return val.Value;
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading info.plist from:" + infoPlist, e);
			}
			return null;
		}
		
		void DeleteDerivedData ()
		{
			var derivedDataPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Library/Developer/Xcode/DerivedData");
			string[] subDirs;
			
			try {
				if (!Directory.Exists (derivedDataPath))
					return;
				subDirs = Directory.GetDirectories (derivedDataPath);
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting derived data directories.", e);
				return;
			}
			
			foreach (var subDir in subDirs) {
				var plistPath = Path.Combine (subDir, "info.plist");
				var workspacePath = GetWorkspacePath (plistPath);
				if (workspacePath == null)
					continue;
				//clean up derived data for all our hack projects
				if (workspacePath.StartsWith (projectDir)) {
					try {
						XC4Debug.Log ("Deleting derived data directory");
						Directory.Delete (subDir, true);
					} catch (Exception e) {
						LoggingService.LogError ("Error while removing derived data directory " + subDir, e);
					}
					return;
				}
			}
			XC4Debug.Log ("Couldn't find & delete derived data directory");
		}
		
		public bool IsProjectOpen ()
		{
			if (!CheckRunning ())
				return false;
			return AppleScript.Run (XCODE_CHECK_PROJECT_OPEN, AppleSdkSettings.XcodePath, xcproj) == "true";
		}
		
		public bool CloseProject ()
		{
			if (!CheckRunning ())
				return true;
			
			var success = AppleScript.Run (XCODE_CLOSE_IN_PATH, AppleSdkSettings.XcodePath, xcprojDir) == "true";
			XC4Debug.Log ("Closing project: {0}", success);
			return success;
		}
		
		public bool CloseFile (string fileName)
		{
			if (!CheckRunning ())
				return true;
			
			var success = AppleScript.Run (XCODE_CLOSE_IN_PATH, AppleSdkSettings.XcodePath, fileName) == "true";
			XC4Debug.Log ("Closing file {0}: {1}", fileName, success);
			return success;
		}

		const string XCODE_OPEN_PROJECT =
@"tell application ""{0}""
	activate
	open ""{1}""
end tell";

		const string XCODE_OPEN_PROJECT_FILE =
@"tell application ""{0}""
	activate
	open ""{1}""
	open ""{2}""
end tell";

		const string XCODE_SAVE_IN_PATH =
@"tell application ""{0}""
	set pp to ""{1}""
	set ext to {{ "".xib"", "".h"", "".m"" }}
	repeat with d in documents
		if d is modified then
			set f to path of d
			if f starts with pp then
				repeat with e in ext
					if f ends with e then
						save d
						exit repeat
					end if
				end repeat
			end if
		end if
	end repeat
end tell";
		
		const string XCODE_CLOSE_IN_PATH =
@"tell application ""{0}""
	set pp to ""{1}""
	repeat with d in documents
		set f to path of d
		if f starts with pp then
			close d
			return true
		end if
	end repeat
	return false
end tell";
		
		const string XCODE_CHECK_PROJECT_OPEN =
@"tell application ""{0}""
	set pp to ""{1}""
	repeat with p in projects
		if real path of p is pp then
			return true
			exit repeat
		end if
	end repeat
	return false
end tell";
	}
}
