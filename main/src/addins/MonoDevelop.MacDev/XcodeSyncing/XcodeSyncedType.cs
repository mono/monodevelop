// 
// XcodeSyncedType.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
	
	class XcodeSyncedType : XcodeSyncedItem
	{
		public NSObjectTypeInfo Type { get; set; }
		public string[] Frameworks { get; set; }
		
		public XcodeSyncedType (NSObjectTypeInfo type, string[] frameworks)
		{
			Frameworks = frameworks;
			Type = type;
		}
		
		public string GetObjCHeaderPath (XcodeSyncContext context)
		{
			return context.ProjectDir.Combine (Type.ObjCName + ".h");
		}
		
		public override bool NeedsSyncOut (IProgressMonitor monitor, XcodeSyncContext context)
		{
			//FIXME: types dep on other types on project, need better regeneration skipping
			string h = Type.ObjCName + ".h";
			
			if (!File.Exists (GetObjCHeaderPath (context)))
				return true;
			
			DateTime syncTime = context.GetSyncTime (h);
			foreach (var path in Type.DefinedIn) {
				if (File.GetLastWriteTime (path) > syncTime) {
					monitor.Log.WriteLine ("The {0} class has changed since last sync ({1}).", Type.CliName, Path.GetFileName (path));
					return true;
				}
			}
			
			return false;
		}
		
		public override void SyncOut (IProgressMonitor monitor, XcodeSyncContext context)
		{
			monitor.Log.WriteLine ("Exporting Objective-C source code for the {0} class to Xcode.", Type.CliName);
			Type.GenerateObjcType (context.ProjectDir, Frameworks);
			
			DateTime mtime = File.GetLastWriteTime (GetObjCHeaderPath (context));
			context.SetSyncTime (Type.ObjCName + ".h", mtime);
		}
		
		public override bool NeedsSyncBack (IProgressMonitor monitor, XcodeSyncContext context)
		{
			string path = GetObjCHeaderPath (context);
			string h = Type.ObjCName + ".h";
			
			if (File.Exists (path) && File.GetLastWriteTime (path) > context.GetSyncTime (h)) {
				monitor.Log.WriteLine ("{0} has changed since last sync.", h);
				return true;
			}
			
			return false;
		}
		
		public override void SyncBack (IProgressMonitor monitor, XcodeSyncBackContext context)
		{
			monitor.Log.WriteLine ("Queueing sync-back of changes made to the {0} class from Xcode.", Type.CliName);
			
			var objcType = context.ProjectInfo.GetType (Type.ObjCName);
			var hFile = GetObjCHeaderPath (context);
			
			if (objcType == null) {
				context.ReportError ("Missing Objective-C type: {0}", Type.ObjCName);
				return;
			}
			
			if (!objcType.IsUserType) {
				context.ReportError ("Parsed Objective-C type '{0}' is not a user type", objcType);
				return;
			}
			
			var parsed = NSObjectInfoService.ParseHeader (hFile);
			
			if (parsed == null) {
				context.ReportError ("Error parsing Objective-C type: {0}", Type.ObjCName);
				return;
			}
			
			if (parsed.ObjCName != objcType.ObjCName) {
				context.ReportError ("Parsed type name '{0}' does not match original: {1}",
					parsed.ObjCName, objcType.ObjCName);
				return;
			}
			
			parsed.MergeCliInfo (objcType);
			
			context.TypeSyncJobs.Add (XcodeSyncObjcBackJob.UpdateType (parsed, objcType.GetDesignerFile ()));
		}
		
		const string supportingFilesGroup = "Supporting Files";
		public override void AddToProject (XcodeProject project, FilePath syncProjectDir)
		{
			project.AddSource (Type.ObjCName + ".h");
			
			var grp = project.GetGroup (supportingFilesGroup) ?? project.AddGroup (supportingFilesGroup);
			project.AddSource (Type.ObjCName + ".m", grp);
		}
		
		public override string[] GetTargetRelativeFileNames ()
		{
			return new string [] {
				Type.ObjCName + ".h",
				Type.ObjCName + ".m",
			};
		}
	}
}
