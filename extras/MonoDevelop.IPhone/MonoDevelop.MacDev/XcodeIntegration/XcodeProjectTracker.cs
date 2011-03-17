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
		DotNetProject dnp;
		NSObjectInfoTracker typeTracker;
		HashSet<string> userClasses = new HashSet<string> ();
		
		bool syncing;
		
		FilePath outputDir;
		
		public XcodeProjectTracker (DotNetProject dnp, NSObjectInfoTracker typeTracker)
		{
			this.typeTracker = typeTracker;
			this.dnp = dnp;
			
			dnp.Saved += ProjectSaved;
			dnp.FileChangedInProject += FileChangedInProject;
			dnp.FileAddedToProject += FileAddedToProject;
			dnp.FileRemovedFromProject += FileRemovedFromProject;
			dnp.NameChanged += ProjectNameChanged;
			typeTracker.TypesLoaded += TypesLoaded;
			
			UpdateOutputDir ();
			
			syncing = dnp.Files.Any (IsPage);
		}

		void TypesLoaded (object sender, EventArgs e)
		{
			foreach (var ut in typeTracker.GetUserTypes ()) {
				userClasses.Add (ut.ObjCName);
				FilePath target = outputDir.Combine (ut.ObjCName + ".h");
				if (File.Exists (target) && File.GetLastWriteTime (target) >= ut.DefinedIn.Max (f => File.GetLastWriteTime (f)))
					continue;
				if (!Directory.Exists (outputDir))
					Directory.CreateDirectory (outputDir);
				typeTracker.GenerateObjcType (ut, outputDir);
			}
			
			if (syncing) {
				UpdateXcodeProject ();
			}
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
		
		static bool IsPageOrContent (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Page || pf.BuildAction == BuildAction.Content;
		}
		
		void FileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			if (syncing && e.Any (finf => IsPage (finf.ProjectFile)))
				syncing = dnp.Files.Any (IsPage);
			if (!syncing)
				return;
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			if (!syncing && e.Any (finf => IsPage (finf.ProjectFile)))
				syncing = true;
			if (!syncing)
				return;
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
		}

		void ProjectSaved (object sender, SolutionItemEventArgs e)
		{
			UpdateXcodeProject ();
		}
		
		void CopyAllFiles ()
		{
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
		
		void UpdateXcodeProject ()
		{
			//TODO: need to update project if user class list changes
			var projFile = outputDir.Combine (dnp.Name + ".xcodeproj", dnp.Name + ".pbxproj");
			if (File.Exists (projFile) && File.GetLastWriteTime (dnp.FileName) <= File.GetLastWriteTime (projFile))
				return;
			
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
			dnp.Saved -= ProjectSaved;
			dnp.FileChangedInProject -= FileChangedInProject;
			dnp.FileAddedToProject -= FileAddedToProject;
			dnp.FileRemovedFromProject -= FileRemovedFromProject;
		}
	}
}