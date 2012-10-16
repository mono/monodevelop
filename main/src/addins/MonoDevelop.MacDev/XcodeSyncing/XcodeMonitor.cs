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
		FilePath originalProjectDir;
		static int nextHackDir = 0;
		string name;
		
		FilePath xcproj, projectDir;
		List<XcodeSyncedItem> items;
		Dictionary<string,XcodeSyncedItem> itemMap = new Dictionary<string, XcodeSyncedItem> ();
		Dictionary<string,DateTime> syncTimeCache = new Dictionary<string, DateTime> ();
		
		XcodeProject pendingProjectWrite;
		
		public XcodeMonitor (FilePath projectDir, string name)
		{
			this.originalProjectDir = projectDir;
			this.name = name;
			HackGetNextProjectDir ();
		}
		
		public bool ProjectExists ()
		{
			return Directory.Exists (xcproj);
		}
		
		// Note: This method may throw TimeoutException or AppleScriptException
		public void UpdateProject (IProgressMonitor monitor, List<XcodeSyncedItem> allItems, XcodeProject emptyProject)
		{
			items = allItems;
			
			monitor.BeginTask (GettextCatalog.GetString ("Updating Xcode project for {0}...", name), items.Count);
			
			var ctx = new XcodeSyncContext (projectDir, syncTimeCache);
			
			var toRemove = new HashSet<string> (itemMap.Keys);
			var toClose = new HashSet<string> ();
			var syncList = new List<XcodeSyncedItem> ();
			bool updateProject = false;

			monitor.Log.WriteLine ("Calculating sync list...");
			foreach (var item in items) {
				bool needsSync = item.NeedsSyncOut (monitor, ctx);
				if (needsSync)
					syncList.Add (item);
				
				var files = item.GetTargetRelativeFileNames ();
				foreach (var file in files) {
					toRemove.Remove (file);
					
					if (!itemMap.ContainsKey (file)) {
						monitor.Log.WriteLine ("   '{0}' needs to be added.", file);
						updateProject = true;
					} else if (needsSync) {
						monitor.Log.WriteLine ("   '{0}' needs to be closed & updated.", file);
						toClose.Add (file);
					}
					
					itemMap[file] = item;
				}
			}
			updateProject = updateProject || toRemove.Any ();
			
			bool removedOldProject = false;
			if (updateProject) {
				if (pendingProjectWrite == null && ProjectExists ()) {
					monitor.Log.WriteLine ("The project.pbxproj file needs to be updated, closing and removing old project.");
					CloseProject ();
					DeleteXcproj (monitor);
					removedOldProject = true;

					// All items need to be re-synced...
					syncList.Clear ();
					foreach (var item in items) {
						syncList.Add (item);

						var files = item.GetTargetRelativeFileNames ();
						foreach (var file in files)
							itemMap[file] = item;
					}
				}
			} else {
				foreach (var f in toClose)
					CloseFile (monitor, projectDir.Combine (f));
			}

			if (removedOldProject) {
				HackRelocateProject (monitor);
				ctx.ProjectDir = projectDir;
				syncTimeCache.Clear ();
			} else {
				foreach (var f in toRemove) {
					itemMap.Remove (f);
					syncTimeCache.Remove (f);
					var path = projectDir.Combine (f);
					try {
						if (File.Exists (path))
							File.Delete (path);
					} catch {
					}
				}
			}
			
			foreach (var item in items) {
				if (updateProject)
					item.AddToProject (emptyProject, projectDir);
			}
			
			foreach (var item in syncList) {
				item.SyncOut (monitor, ctx);
				monitor.Step (1);
			}
			
			if (updateProject) {
				monitor.Log.WriteLine ("Queued write of '{0}'.", xcproj);
				pendingProjectWrite = emptyProject;
			}

			monitor.EndTask ();
			monitor.ReportSuccess (GettextCatalog.GetString ("Xcode project updated."));
		}
		
		// Xcode keeps some kind of internal lock on project files while it's running and
		// gets extremely unhappy if a new or changed project is in the same location as
		// a previously opened one.
		//
		// To work around this we increment a subdirectory name and use that.
		//
		void HackRelocateProject (IProgressMonitor monitor)
		{
			HackGetNextProjectDir ();
			
			monitor.Log.WriteLine ("Changed Xcode project path to {0}", projectDir);
		}
		
		void HackGetNextProjectDir ()
		{
			do {
				projectDir = originalProjectDir.Combine (nextHackDir.ToString ());
				nextHackDir++;
			} while (Directory.Exists (projectDir));
			
			xcproj = projectDir.Combine (name + ".xcodeproj");
		}
		
		HashSet<string> GetKnownFiles ()
		{
			HashSet<string> knownItems = new HashSet<string> ();
			
			foreach (var item in items) {
				foreach (var file in item.GetTargetRelativeFileNames ())
					knownItems.Add (Path.Combine (projectDir, file));
			}
			
			return knownItems;
		}
		
		void ScanForAddedFiles (IProgressMonitor monitor, XcodeSyncBackContext ctx, HashSet<string> knownFiles, string directory, string relativePath)
		{
			foreach (var file in Directory.EnumerateFiles (directory)) {
				if (file.EndsWith ("~") || file.EndsWith (".m"))
					continue;
				
				if (knownFiles.Contains (file))
					continue;
				
				if (file.EndsWith (".h")) {
					NSObjectTypeInfo parsed = NSObjectInfoService.ParseHeader (file);
					
					monitor.Log.WriteLine ("New Objective-C header file found: {0}", Path.Combine (relativePath, Path.GetFileName (file)));
					ctx.TypeSyncJobs.Add (XcodeSyncObjcBackJob.NewType (parsed, relativePath));
				} else {
					FilePath original, relative;
					
					if (relativePath != null) {
						relative = new FilePath (Path.Combine (relativePath, Path.GetFileName (file)));
						original = ctx.Project.BaseDirectory.Combine (relative);
					} else {
						relative = new FilePath (Path.GetFileName (file));
						original = ((IXcodeTrackedProject) ctx.Project).DefaultBundleResourceDir.Combine (relative);
					}
					
					monitor.Log.WriteLine ("New content file found: {0}", relative);
					ctx.FileSyncJobs.Add (new XcodeSyncFileBackJob (original, relative, true));
				}
			}
			
			foreach (var dir in Directory.EnumerateDirectories (directory)) {
				string relative;
				
				// Ignore *.xcodeproj directories and any directories named DerivedData at the toplevel
				if (dir.EndsWith (".xcodeproj") || (relativePath == null && Path.GetFileName (dir) == "DerivedData"))
					continue;
				
				if (relativePath != null)
					relative = Path.Combine (relativePath, Path.GetFileName (dir));
				else
					relative = Path.GetFileName (dir);
				
				ScanForAddedFiles (monitor, ctx, knownFiles, dir, relative);
			}
		}

		public XcodeSyncBackContext GetChanges (IProgressMonitor monitor, NSObjectInfoService infoService, DotNetProject project)
		{
			var ctx = new XcodeSyncBackContext (projectDir, syncTimeCache, infoService, project);
			var needsSync = new List<XcodeSyncedItem> (items.Where (i => i.NeedsSyncBack (monitor, ctx)));
			var knownFiles = GetKnownFiles ();
			
			if (Directory.Exists (projectDir)) {
				monitor.BeginTask ("Scanning for newly-added files in the Xcode project...", 0);
				ScanForAddedFiles (monitor, ctx, knownFiles, projectDir, null);
				monitor.EndTask ();
			}
			
			if (needsSync.Count > 0) {
				monitor.BeginStepTask (GettextCatalog.GetString ("Synchronizing changes made to known files in Xcode back to MonoDevelop..."), needsSync.Count, 1);
				for (int i = 0; i < needsSync.Count; i++) {
					var item = needsSync [i];
					item.SyncBack (monitor, ctx);
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
			
			return ctx;
		}
		
		public bool CheckRunning ()
		{
			var appPathKey = new NSString ("NSApplicationPath");
			var appPathVal = new NSString (AppleSdkSettings.XcodePath);
			
			return NSWorkspace.SharedWorkspace.LaunchedApplications.Any (app => appPathVal.Equals (app[appPathKey]));
		}
		
		// Note: This method may throw TimeoutException or AppleScriptException
		public void SaveProject (IProgressMonitor monitor)
		{
			monitor.Log.WriteLine ("Asking Xcode to save pending changes for the {0} project", name);
			AppleScript.Run (XCODE_SAVE_IN_PATH, AppleSdkSettings.XcodePath, projectDir);
		}

		void SyncProject (IProgressMonitor monitor)
		{
			if (pendingProjectWrite != null) {
				monitor.Log.WriteLine ("Generating project.pbxproj file for {0}", name);
				pendingProjectWrite.Generate (projectDir);
				pendingProjectWrite = null;
			}
		}
		
		// Note: This method may throw TimeoutException or AppleScriptException
		public void OpenFile (IProgressMonitor monitor, string relativeName)
		{
			SyncProject (monitor);
			
			string path = projectDir.Combine (relativeName);
			
			monitor.Log.WriteLine ("Asking Xcode to open '{0}'...", path);
			try {
				AppleScript.Run (XCODE_OPEN_PROJECT_FILE, AppleSdkSettings.XcodePath, xcproj, path);
				monitor.Log.WriteLine ("Xcode successfully opened '{0}'", path);
			} catch (AppleScriptException asex) {
				monitor.Log.WriteLine ("Xcode failed to open '{0}': OSAError={1}: {2}", path, (int) asex.ErrorCode, asex.Message);
				throw;
			} catch (TimeoutException) {
				monitor.Log.WriteLine ("Xcode timed out trying to open '{0}'.", path);
				throw;
			}
		}
		
		public void DeleteProjectDirectory ()
		{
			bool isRunning = CheckRunning ();
			
			XC4Debug.Log ("Deleting temporary Xcode project directories.");

			try {
				if (Directory.Exists (projectDir))
					Directory.Delete (projectDir, true);
			} catch (Exception ex) {
				XC4Debug.Indent ();
				XC4Debug.Log (ex.Message);
				XC4Debug.Unindent ();
			}

			if (isRunning) {
				XC4Debug.Log ("Xcode still running, leaving empty directory in place to prevent name re-use.");
				if (!Directory.Exists (projectDir))
					Directory.CreateDirectory (projectDir);
			} else {
				XC4Debug.Log ("Xcode not running, removing all temporary directories.");
				try {
					if (Directory.Exists (originalProjectDir))
						Directory.Delete (originalProjectDir, true);
				} catch (Exception ex) {
					XC4Debug.Indent ();
					XC4Debug.Log (ex.Message);
					XC4Debug.Unindent ();
				}
			}
		}
		
		void DeleteXcproj (IProgressMonitor monitor)
		{
			monitor.Log.WriteLine ("Deleting project artifacts.");
			try {
				if (Directory.Exists (xcproj))
					Directory.Delete (xcproj, true);
			} catch {
			}
		}
		
		static string GetWorkspacePath (string infoPlist)
		{
			try {
				var dict = PDictionary.FromFile (infoPlist);
				PString val;
				if (dict.TryGetValue<PString>("WorkspacePath", out val))
					return val.Value;
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading info.plist from " + infoPlist, e);
			}
			return null;
		}
		
		// Note: This method may throw TimeoutException or AppleScriptException
		public bool IsProjectOpen ()
		{
			if (!CheckRunning ())
				return false;
			
			return AppleScript.Run (XCODE_CHECK_PROJECT_OPEN, AppleSdkSettings.XcodePath, xcproj) == "true";
		}
		
		public void CloseProject ()
		{
			if (!CheckRunning ())
				return;
			
			XC4Debug.Log ("Asking Xcode to close the {0} project...", name);
			
			try {
				// Exceptions closing the project are non-fatal.
				bool closed = AppleScript.Run (XCODE_CLOSE_IN_PATH, AppleSdkSettings.XcodePath, projectDir) == "true";
				XC4Debug.Log ("Xcode {0} the project.", closed ? "successfully closed" : "failed to close");
			} catch (AppleScriptException asex) {
				XC4Debug.Log ("Xcode failed to close the project: OSAError {0}: {1}", (int) asex.ErrorCode, asex.Message);
			} catch (TimeoutException) {
				XC4Debug.Log ("Xcode timed out trying to close the project.");
			}
		}
		
		// Note: This method may throw TimeoutException or AppleScriptException
		public void OpenProject (IProgressMonitor monitor)
		{
			SyncProject (monitor);
			
			monitor.Log.WriteLine ("Asking Xcode to open the {0} project...", name);
			try {
				AppleScript.Run (XCODE_OPEN_PROJECT, AppleSdkSettings.XcodePath, projectDir);
				monitor.Log.WriteLine ("Xcode successfully opened the {0} project.", name);
			} catch (AppleScriptException asex) {
				monitor.Log.WriteLine ("Xcode failed to open the {0} project: OSAError {1}: {2}", name, (int) asex.ErrorCode, asex.Message);
				throw;
			} catch (TimeoutException) {
				monitor.Log.WriteLine ("Xcode timed out trying to open the {0} project.", name);
				throw;
			}
		}
		
		// Note: This method may throw TimeoutException or AppleScriptException
		void CloseFile (IProgressMonitor monitor, string fileName)
		{
			if (!CheckRunning ())
				return;
			
			monitor.Log.WriteLine ("Asking Xcode to close '{0}'...", fileName);
			try {
				AppleScript.Run (XCODE_CLOSE_IN_PATH, AppleSdkSettings.XcodePath, fileName);
				monitor.Log.WriteLine ("Xcode successfully closed '{0}'", fileName);
			} catch (AppleScriptException asex) {
				monitor.Log.WriteLine ("Xcode failed to close '{0}': OSAError {1}: {2}", fileName, (int) asex.ErrorCode, asex.Message);
				throw;
			} catch (TimeoutException) {
				monitor.Log.WriteLine ("Xcode timed out trying to close '{0}'.", fileName);
				throw;
			}
		}
		
		// Note: Can throw TimeoutException
		const string XCODE_OPEN_PROJECT =
@"tell application ""{0}""
	if it is not running then
		with timeout of 60 seconds
			launch
			activate
		end timeout
	end if
	with timeout of 60 seconds
		open ""{1}""
	end timeout
end tell";
		
		// Note: Can throw TimeoutException
		const string XCODE_OPEN_PROJECT_FILE =
@"tell application ""{0}""
	with timeout of 60 seconds
		activate
		activate
		open ""{1}""
		activate
		open ""{2}""
	end timeout
end tell";
		
		// Note: Can throw TimeoutException
		const string XCODE_SAVE_IN_PATH =
@"tell application ""{0}""
	if it is running then
		set fileExtensions to {{ "".storyboard"", "".xib"", "".h"", "".m"" }}
		set projectPath to ""{1}""
		
		with timeout of 30 seconds
			repeat with doc in documents
				if doc is modified then
					set docPath to path of doc
					if docPath starts with projectPath then
						repeat with ext in fileExtensions
							if docPath ends with ext then
								save doc
								exit repeat
							end if
						end repeat
					end if
				end if
			end repeat
		end timeout
	end if
end tell";
		
		// Note: Can throw TimeoutException
		const string XCODE_CLOSE_IN_PATH =
@"tell application ""{0}""
	if it is running then
		set projectPath to ""{1}""
		with timeout of 30 seconds
			repeat with doc in documents
				set docPath to path of doc
				if docPath starts with projectPath then
					close doc
					return true
				end if
			end repeat
		end timeout
	end if
	return false
end tell";
		
		// Note: Can throw TimeoutException
		const string XCODE_CHECK_PROJECT_OPEN =
@"tell application ""{0}""
	if it is running then
		with timeout of 30 seconds
			set projectPath to ""{1}""
			repeat with proj in projects
				if real path of proj is projectPath then
					return true
					exit repeat
				end if
			end repeat
		end timeout
	end if
	return false
end tell";
	}
}
