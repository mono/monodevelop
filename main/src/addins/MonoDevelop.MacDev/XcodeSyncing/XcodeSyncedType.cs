// 
// XcodeSyncedType.cs
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
	
	class XcodeSyncedType : XcodeSyncedItem
	{
		public NSObjectTypeInfo Type { get; set; }
		
		public XcodeSyncedType (NSObjectTypeInfo type)
		{
			this.Type = type;
		}
		
		public override bool NeedsSyncOut (XcodeSyncContext context)
		{
			//FIXME: types dep on other types on project, need better regeneration skipping
			var h = Type.ObjCName + ".h";
			var path = context.ProjectDir.Combine (h);
			if (File.Exists (path) && context.GetSyncTime (h) > Type.DefinedIn.Max (f => File.GetLastWriteTime (f)))
				return false;
			return true;
		}
		
		public override void SyncOut (XcodeSyncContext context)
		{
			Type.GenerateObjcType (context.ProjectDir);
			context.UpdateSyncTime (Type.ObjCName + ".h");
			context.UpdateSyncTime (Type.ObjCName + ".m");
		}
		
		public override bool NeedsSyncBack (XcodeSyncContext context)
		{
			var h = Type.ObjCName + ".h";
			var path = context.ProjectDir.Combine (h);
			if (File.Exists (path) && File.GetLastWriteTime (path) > context.GetSyncTime (h))
				return true;
			return false;
		}
		
		public override void SyncBack (XcodeSyncBackContext context)
		{
			var hFile = context.ProjectDir.Combine (Type.ObjCName + ".h");
			var parsed = NSObjectInfoService.ParseHeader (hFile);
			
			var objcType = context.ProjectInfo.GetType (Type.ObjCName);
			if (objcType == null) {
				context.ReportError ("Missing objc type {0}", Type.ObjCName);
				return;
			}
			if (parsed.ObjCName != objcType.ObjCName) {
				context.ReportError ("Parsed type name {0} does not match original {1}",
					parsed.ObjCName, objcType.ObjCName);
				return;
			}
			if (!objcType.IsUserType) {
				context.ReportError ("Parsed type {0} is not a user type", objcType);
				return;
			}
			
			//FIXME: detect unresolved types
			parsed.MergeCliInfo (objcType);
			context.ProjectInfo.ResolveTypes (parsed);
			
			context.TypeSyncJobs.Add (new XcodeSyncObjcBackJob () {
				HFile = hFile,
				DesignerFile = objcType.GetDesignerFile (),
				Type = parsed,
			});
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
