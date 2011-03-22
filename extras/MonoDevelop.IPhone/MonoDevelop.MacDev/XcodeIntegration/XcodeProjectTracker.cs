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

namespace MonoDevelop.MacDev.XcodeIntegration
{
	public class XcodeProjectTracker : IDisposable
	{
		string wrapperName;
		DotNetProject dnp;
		HashSet<string> userClasses = new HashSet<string> ();
		
		bool xcodeProjectDirty;
		bool syncing;
		bool disposed;
		
		FilePath outputDir;
		
		public XcodeProjectTracker (DotNetProject dnp, string wrapperName)
		{
			this.dnp = dnp;
			this.wrapperName = wrapperName;
			
			dnp.FileAddedToProject += FileAddedToProject;
			dnp.FilePropertyChangedInProject += FilePropertyChangedInProject;
			
			if (dnp.Files.Any (IsPage))
				EnableSyncing ();
		}
		
		void EnableSyncing ()
		{
			if (syncing)
				return;
			syncing = true;
			xcodeProjectDirty = true;
			
			//typeTracker = new NSObjectInfoTracker (dnp, wrapperName);
			UpdateOutputDir ();
			
			dnp.FileRemovedFromProject += FileRemovedFromProject;
			dnp.FileChangedInProject += FileChangedInProject;
			dnp.NameChanged += ProjectNameChanged;
//			typeTracker.TypesLoaded += TypesLoaded;
//			typeTracker.UserTypeChanged += UserTypesChanged;
		}
		
		void DisableSyncing ()
		{
			if (syncing)
				return;
			syncing = false;
			xcodeProjectDirty = false;
			
			dnp.FileRemovedFromProject -= FileRemovedFromProject;
			dnp.FileChangedInProject -= FileChangedInProject;
			dnp.NameChanged -= ProjectNameChanged;
//			typeTracker.TypesLoaded -= TypesLoaded;
//			typeTracker.UserTypeChanged -= UserTypesChanged;
//			typeTracker.Dispose ();
		}

		void UserTypesChanged (object sender, UserTypeChangeEventArgs e)
		{
			foreach (var change in e.Changes) {
				switch (change.Kind) {
				case UserTypeChangeKind.Added:
				case UserTypeChangeKind.Modified:
					UpdateUserType (change.Type);
					break;
				case UserTypeChangeKind.Removed:
					RemoveUserType (change.Type.ObjCName);
					break;
				}
			}
			
			UpdateXcodeProject ();
		}
	
		void TypesLoaded (object sender, EventArgs e)
		{
			//TODO: skip types that were already in the project when MD was loaded
			UpdateTypes (false);
			
			UpdateXcodeProject ();
		}
		
		void ProjectNameChanged (object sender, SolutionItemRenamedEventArgs e)
		{
			if (!outputDir.IsNullOrEmpty && Directory.Exists (outputDir)) {
				Directory.Delete (outputDir, true);
			}
			UpdateOutputDir ();
		}
		
		void UpdateOutputDir ()
		{
			outputDir = dnp.BaseDirectory.Combine ("obj", "Xcode");
		}
		
		static bool IsPage (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Page;
		}
		
		static bool IsContent (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Content;
		}
		
		static bool IsPageOrContent (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Page || pf.BuildAction == BuildAction.Content;
		}
		
		void FileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			if (syncing && e.Any (finf => IsPage (finf.ProjectFile)))
				if (!dnp.Files.Any (IsPage))
					DisableSyncing ();
			
			if (syncing)
				UpdateTypes (true);
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			if (!syncing && e.Any (finf => IsPage (finf.ProjectFile)))
				EnableSyncing ();
			
			if (syncing)
				UpdateTypes (true);
			
			UpdateXcodeProject ();
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (syncing)
				UpdateTypes (true);
			
			UpdateXcodeProject ();
		}

		void FilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (!syncing && e.Any (finf => IsPage (finf.ProjectFile)))
				EnableSyncing ();
			
			if (!xcodeProjectDirty && syncing && e.Any (finf => IsContent (finf.ProjectFile)))
				xcodeProjectDirty = true;
			
			if (syncing)
				UpdateTypes (true);
			
			UpdateXcodeProject ();
		}
		
		void UpdateTypes (bool rescan)
		{
			var pinfo = NSObjectInfoService.GetProjectInfo (dnp);
			if (pinfo == null) {
				Console.WriteLine ("Null PI");
				return;
			}
			
			pinfo.Update (rescan);
			
			var currentUTs = new Dictionary<string,NSObjectTypeInfo> ();
			foreach (var ut in pinfo.GetTypes ().Where (t => t.IsUserType)) {
				currentUTs.Add (ut.ObjCName, ut);
			}
			
			foreach (var removed in this.userClasses.Where (c => !currentUTs.ContainsKey (c)).ToList ()) {
				RemoveUserType (removed);
			}
			
			foreach (var ut in currentUTs) {
				UpdateUserType (ut.Value);
			}
		}
		
		void CopyFile (ProjectFile p)
		{
			var target = outputDir.Combine (p.ProjectVirtualPath);
			if (!File.Exists (target) || File.GetLastWriteTime (target) < File.GetLastWriteTime (p.FilePath)) {
				var dir = target.ParentDirectory;
				if (!Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				if (File.Exists (target))
					File.Delete (target);
				Mono.Unix.UnixMarshal.ThrowExceptionForLastErrorIf (Mono.Unix.Native.Syscall.link (p.FilePath, target));
			}
		}
		
		void UpdateUserType (NSObjectTypeInfo type)
		{
			if (userClasses.Add (type.ObjCName))
				xcodeProjectDirty = true;
			
			FilePath target = outputDir.Combine (type.ObjCName + ".h");
			if (File.Exists (target) && File.GetLastWriteTime (target) >= type.DefinedIn.Max (f => File.GetLastWriteTime (f)))
				return;
			
			if (!Directory.Exists (outputDir))
				Directory.CreateDirectory (outputDir);
			
			type.GenerateObjcType (outputDir);
		}
		
		void RemoveUserType (string objcName)
		{
			userClasses.Remove (objcName);
			xcodeProjectDirty = true;
			
			string header = outputDir.Combine (objcName + ".h");
			if (File.Exists (header))
				File.Delete (header);
			string impl = outputDir.Combine (objcName + ".m");
			if (File.Exists (impl))
				File.Delete (impl);
		}
		
		void UpdateXcodeProject ()
		{
			if (!syncing)
				return;
			
			var projFile = outputDir.Combine (dnp.Name + ".xcodeproj", dnp.Name + ".pbxproj");
			if (!xcodeProjectDirty && File.Exists (projFile))
				return;
			
			xcodeProjectDirty = false;
			
			if (!Directory.Exists (outputDir))
				Directory.CreateDirectory (outputDir);
			
			var xcp = new XcodeProject (dnp.Name);
			foreach (var file in dnp.Files.Where (IsPageOrContent)) {
				xcp.AddResource (file.ProjectVirtualPath);
				CopyFile (file);
			}
			
			foreach (var cls in userClasses) {
				xcp.AddSource (cls + ".h");
				xcp.AddSource (cls + ".m");
			}
			
			xcp.Generate (outputDir);
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			DisableSyncing ();
			dnp.FileAddedToProject -= FileAddedToProject;
			dnp.FilePropertyChangedInProject -= FilePropertyChangedInProject;;
		}
	}
}