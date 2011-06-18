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

namespace MonoDevelop.MacDev.XcodeSyncing
{
	class XcodeMonitor
	{
		string xcodePath = XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION;
		
		FilePath xcproj, projectDir;
		List<XcodeSyncedItem> items;
		Dictionary<string,XcodeSyncedItem> itemMap = new Dictionary<string, XcodeSyncedItem> ();
		Dictionary<string,DateTime> syncTimeCache = new Dictionary<string, DateTime> ();
		
		public XcodeMonitor (FilePath projectDir, string name)
		{
			this.projectDir = projectDir;
			this.xcproj = projectDir.Combine (name + ".xcodeproj");
		}
		
		public bool ProjectExists ()
		{
			return Directory.Exists (xcproj);
		}
		
		public void UpdateProject (List<XcodeSyncedItem> allItems, XcodeProject emptyProject)
		{
			items = allItems;
			
			XC4Debug.Log ("Updating synced project with {0} items", items.Count);
			
			var ctx = new XcodeSyncContext (projectDir, syncTimeCache);
			
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
			
			if (updateProject) {
				XC4Debug.Log ("Project file needs to be updated, closing and removing old project");
				CloseProject ();
				DeleteProjectArtifacts ();
			} else {
				foreach (var f in toClose)
					CloseFile (projectDir.Combine (f));
			}
			
			foreach (var f in toRemove) {
				itemMap.Remove (f);
				syncTimeCache.Remove (f);
				var path = projectDir.Combine (f);
				if (File.Exists (path))
					File.Delete (path);
			}
			
			foreach (var item in items) {
				if (updateProject)
					item.AddToProject (emptyProject, projectDir);
			}
			
			foreach (var item in syncList) {
				XC4Debug.Log ("Syncing item {0}", item.GetTargetRelativeFileNames ()[0]);
				item.SyncOut (ctx);
			}
			
			if (updateProject) {
				XC4Debug.Log ("Writing Xcode project");
				emptyProject.Generate (projectDir);
			}
		}

		public XcodeSyncBackContext GetChanges (NSObjectInfoService infoService, DotNetProject project)
		{
			var ctx = new XcodeSyncBackContext (projectDir, syncTimeCache, infoService, project);
			foreach (var item in items) {
				if (item.NeedsSyncBack (ctx)) {
					item.SyncBack (ctx);
				}
			}
			return ctx;
		}
		
		public bool CheckRunning ()
		{
			var appPathKey = new NSString ("NSApplicationPath");
			var appPathVal = new NSString (xcodePath);
			return NSWorkspace.SharedWorkspace.LaunchedApplications.Any (app => appPathVal.Equals (app[appPathKey]));
		}
		
		public void SaveProject ()
		{
			XC4Debug.Log ("Saving Xcode project");
			AppleScript.Run (XCODE_SAVE_IN_PATH, xcodePath, projectDir);
		}
		
		public void OpenProject ()
		{
			if (!NSWorkspace.SharedWorkspace.OpenFile (xcproj, xcodePath))
				throw new Exception ("Failed to open Xcode project");
		}
		
		public void OpenFile (string relativeName)
		{
			XC4Debug.Log ("Opening file in Xcode: {0}", relativeName);
			OpenProject ();
			NSWorkspace.SharedWorkspace.OpenFile (projectDir.Combine (relativeName), xcodePath);
		}
		
		public void DeleteProjectDirectory ()
		{
			XC4Debug.Log ("Deleting project directory");
			if (Directory.Exists (projectDir))
				Directory.Delete (projectDir, true);
		}
		
		void DeleteProjectArtifacts ()
		{
			XC4Debug.Log ("Deleting project artifacts");
			if (Directory.Exists (xcproj))
				Directory.Delete (xcproj, true);
		}
		
		public bool IsProjectOpen ()
		{
			if (!CheckRunning ())
				return false;
			return AppleScript.Run (XCODE_CHECK_PROJECT_OPEN, xcodePath, xcproj) == "true";
		}
		
		public bool CloseProject ()
		{
			var success = AppleScript.Run (XCODE_CLOSE_IN_PATH, xcodePath, projectDir) == "true";
			XC4Debug.Log ("Closing project: {0}", success);
			return success;
		}
		
		public bool CloseFile (string fileName)
		{
			var success = AppleScript.Run (XCODE_CLOSE_IN_PATH, xcodePath, fileName) == "true";
			XC4Debug.Log ("Closing file {0}: {1}", fileName, success);
			return success;
		}
		
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
