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
using System.Threading.Tasks;

namespace MonoDevelop.MacDev.XcodeIntegration
{
	public interface IXcodeTrackedProject
	{
		XcodeProjectTracker XcodeProjectTracker { get; }
	}
	
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
		}
		
		public IAsyncOperation OpenDocument (FilePath xib)
		{
			EnableSyncing ();
			UpdateTypes (true);
			UpdateXcodeProject ();
			
			var xcode = XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION;
			MonoMac.AppKit.NSWorkspace.SharedWorkspace.OpenFile (outputDir.Combine (dnp.Name + ".xcodeproj"), xcode);
			MonoMac.AppKit.NSWorkspace.SharedWorkspace.OpenFile (xib, xcode);
			return MonoDevelop.Core.ProgressMonitoring.NullAsyncOperation.Success;
		}
		
		void EnableSyncing ()
		{
			if (syncing)
				return;
			syncing = true;
			xcodeProjectDirty = true;
			
			UpdateOutputDir ();
			
			dnp.FileAddedToProject += FileAddedToProject;
			dnp.FilePropertyChangedInProject += FilePropertyChangedInProject;
			dnp.FileRemovedFromProject += FileRemovedFromProject;
			dnp.FileChangedInProject += FileChangedInProject;
			dnp.NameChanged += ProjectNameChanged;
		}
		
		void DisableSyncing ()
		{
			if (syncing)
				return;
			syncing = false;
			xcodeProjectDirty = false;
			
			dnp.FileAddedToProject -= FileAddedToProject;
			dnp.FilePropertyChangedInProject -= FilePropertyChangedInProject;;
			dnp.FileRemovedFromProject -= FileRemovedFromProject;
			dnp.FileChangedInProject -= FileChangedInProject;
			dnp.NameChanged -= ProjectNameChanged;
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
		
		#region Project change tracking
		
		void ProjectNameChanged (object sender, SolutionItemRenamedEventArgs e)
		{
			//FIXME: get Xcode to close and re-open the project
			if (!outputDir.IsNullOrEmpty && Directory.Exists (outputDir)) {
				Directory.Delete (outputDir, true);
			}
			UpdateOutputDir ();
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
			if (!xcodeProjectDirty && syncing && e.Any (finf => IsContent (finf.ProjectFile)))
				xcodeProjectDirty = true;
			
			if (syncing)
				UpdateTypes (true);
			
			UpdateXcodeProject ();
		}
		
		#endregion
		
		#region Outbound syncing
		
		void UpdateTypes (bool rescan)
		{
			var pinfo = NSObjectInfoService.GetProjectInfo (dnp);
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

			//FIXME: types dep on other types on project, need better regeneration skipping			
			//FilePath target = outputDir.Combine (type.ObjCName + ".h");
			//if (File.Exists (target) && File.GetLastWriteTime (target) >= type.DefinedIn.Max (f => File.GetLastWriteTime (f)))
			//	return;
			
			if (!Directory.Exists (outputDir))
				Directory.CreateDirectory (outputDir);
			
			type.GenerateObjcType (outputDir);
			
			string headerFlag = outputDir.Combine (type.ObjCName + ".h.modified");
			if (File.Exists (headerFlag))
				File.SetLastWriteTime (headerFlag, DateTime.Now);
			else
				File.WriteAllText (headerFlag, "");
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
		
		#endregion
		
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			DisableSyncing ();
		}
	}
}