// 
// XcodeSyncedResource.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
	class XcodeSyncedResource : XcodeSyncedItem
	{
		FilePath targetRelative, source;
		
		public XcodeSyncedResource (ProjectFile p)
		{
			this.targetRelative = p.ProjectVirtualPath;
			this.source = p.FilePath;
		}
		
		public override bool NeedsSyncOut (XcodeSyncContext context)
		{
			string target = context.ProjectDir.Combine (targetRelative);
			return !File.Exists (target) || context.GetSyncTime (targetRelative) < File.GetLastWriteTime (source);
		}
		
		public override void SyncOut (XcodeSyncContext context)
		{
			FilePath target = context.ProjectDir.Combine (targetRelative);
			var dir = target.ParentDirectory;
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
			if (File.Exists (target))
				File.Delete (target);
			var result = Mono.Unix.Native.Syscall.link (source, target);
			Mono.Unix.UnixMarshal.ThrowExceptionForLastErrorIf (result);
			context.UpdateSyncTime (targetRelative);
		}
		
		public override bool NeedsSyncBack (XcodeSyncContext context)
		{
			return false;
		}
		
		public override void SyncBack (XcodeSyncBackContext context)
		{
			throw new InvalidOperationException ();
		}
		/*
		const string resourceFilesGroup = "Resource Files";
		public override void AddToProject (XcodeProject project, FilePath syncProjectDir)
		{
			var grp = project.GetGroup (resourceFilesGroup) ?? project.AddGroup (resourceFilesGroup);
			project.AddResource (targetRelative, grp);
		}
		*/
		
		IEnumerable<string> GetGroups ()
		{
			var groups = new List<string> ();
			FilePath parent = targetRelative;
			while (true) {
				parent = parent.ParentDirectory;
				if (parent.IsEmpty)
					break;
				groups.Insert (0, parent.FileName);
			}
			return groups;
		}
		
		public override void AddToProject (XcodeProject project, FilePath syncProjectDir)
		{
			PBXGroup grp = project.RootGroup;
			
			foreach (var groupName in GetGroups ())
				grp = project.GetGroup (grp, groupName) ?? project.AddGroup (grp, groupName);
			
			project.AddResource (targetRelative.FileName, targetRelative, grp);
		}
		
		public override string[] GetTargetRelativeFileNames ()
		{
			return new string [] { 
				targetRelative,
			};
		}
	}
}

