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
		Dictionary<string,XcodeSyncedItem> itemMap
			= new Dictionary<string, XcodeSyncedItem> ();
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
		
		public void UpdateProject (List<XcodeSyncedItem> syncList, XcodeProject emptyProject)
		{
			items = syncList;
			
			XC4Debug.Log ("Updating synced project with {0} items", items.Count);
			
			var toRemove = new HashSet<string> (itemMap.Keys);
			bool updateProject = false;
			
			foreach (var item in items) {
				var files = item.GetTargetFileNames (projectDir);
				foreach (var f in files) {
					toRemove.Remove (f);
					updateProject = updateProject || !itemMap.ContainsKey (f);
					itemMap [f] = item;
				}
			}
			updateProject = updateProject || toRemove.Any ();
			
			if (updateProject) {
				XC4Debug.Log ("Project file needs to be updated, closing and removing old project");
				CloseProject ();
				DeleteProjectArtifacts ();
			}
			
			foreach (var f in toRemove) {
				itemMap.Remove (f);
				syncTimeCache.Remove (f);
				if (File.Exists (f))
					File.Delete (f);
			}
			
			var ctx = new XcodeSyncContext (projectDir, syncTimeCache);
			foreach (var item in items) {
				if (updateProject)
					item.AddToProject (emptyProject, projectDir);
				if (item.NeedsSyncOut (ctx)) {
					XC4Debug.Log ("Syncing item {0}", item.GetTargetFileNames (projectDir)[0]);
					item.SyncOut (ctx);
				} else {
					XC4Debug.Log ("Skipping up to date item {0}", item.GetTargetFileNames (projectDir)[0]);
				}
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
			var success = AppleScript.Run (XCODE_CLOSE_WORKSPACE_IN_PATH, xcodePath, projectDir) == "true";
			XC4Debug.Log ("Closing project: {0}", success);
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
		
		const string XCODE_CLOSE_WORKSPACE_IN_PATH =
@"tell application ""{0}""
	set pp to ""{1}""
	repeat with d in documents
		set f to path of d
		if f starts with pp and f ends with "".xcworkspace"" then
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
