// 
// XcodeSyncedContent.cs
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

namespace MonoDevelop.MacDev.XcodeSyncing
{
	class XcodeSyncedContent : XcodeSyncedItem
	{
		FilePath targetRelative, source;
		bool isInterfaceDefinition;
		
		public XcodeSyncedContent (ProjectFile pf)
		{
			isInterfaceDefinition = pf.BuildAction == BuildAction.InterfaceDefinition;
			source = pf.FilePath;

			switch (pf.BuildAction) {
			case BuildAction.BundleResource:
				targetRelative = ((IXcodeTrackedProject) pf.Project).GetBundleResourceId (pf);
				break;
			case BuildAction.Content:
			default:
				targetRelative = pf.ProjectVirtualPath;
				break;
			}
		}
		
		public override bool NeedsSyncOut (IProgressMonitor monitor, XcodeSyncContext context)
		{
			string target = context.ProjectDir.Combine (targetRelative);
			
			if (!File.Exists (target))
				return true;
			
			if (File.GetLastWriteTime (source) > context.GetSyncTime (targetRelative)) {
				monitor.Log.WriteLine ("{0} has changed since last sync.", targetRelative);
				return true;
			}
			
			return false;
		}
		
		public override void SyncOut (IProgressMonitor monitor, XcodeSyncContext context)
		{
			monitor.Log.WriteLine ("Exporting '{0}' to Xcode.", targetRelative);
			
			var target = context.ProjectDir.Combine (targetRelative);
			var dir = target.ParentDirectory;
			
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
			
			if (File.Exists (target))
				File.Delete (target);
			
			File.Copy (source, target);
			DateTime mtime = File.GetLastWriteTime (target);
			context.SetSyncTime (targetRelative, mtime);
		}
		
		public override bool NeedsSyncBack (IProgressMonitor monitor, XcodeSyncContext context)
		{
			if (!isInterfaceDefinition)
				return false;
			
			string target = context.ProjectDir.Combine (targetRelative);
			
			if (!File.Exists (target)) {
				monitor.Log.WriteLine ("{0} has been removed since last sync.", targetRelative);
				// FIXME: some day we should mirror this change back to MonoDevelop
				return false;
			}
			
			if (File.GetLastWriteTime (target) > context.GetSyncTime (targetRelative)) {
				monitor.Log.WriteLine ("{0} has changed since last sync.", targetRelative);
				return true;
			}
			
			return false;
		}
		
		public override void SyncBack (IProgressMonitor monitor, XcodeSyncBackContext context)
		{
			monitor.Log.WriteLine ("Queueing sync-back of changes made to '{0}' from Xcode.", targetRelative);
			
			context.FileSyncJobs.Add (new XcodeSyncFileBackJob (source, targetRelative, false));
		}
		
		public override void AddToProject (XcodeProject project, FilePath syncProjectDir)
		{
			project.AddResource (targetRelative);
		}
		
		public override string[] GetTargetRelativeFileNames ()
		{
			return new string [] { 
				targetRelative,
			};
		}
	}
}
