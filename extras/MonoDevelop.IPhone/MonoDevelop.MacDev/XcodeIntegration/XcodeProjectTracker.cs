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
		Dictionary<string,DateTime> trackedFiles = new Dictionary<string,DateTime> ();
		
		bool xcodeProjectDirty;
		bool syncing;
		bool disposed;
		
		FilePath outputDir;
		
		static bool? trackerEnabled;
		public static bool TrackerEnabled {
			get {
				if (!trackerEnabled.HasValue)
					trackerEnabled = Environment.GetEnvironmentVariable ("MD_XC4_TEST") != null;
				return trackerEnabled.Value;
			}
		}
		
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
			MonoMac.AppKit.NSWorkspace.SharedWorkspace.OpenFile (outputDir.Combine (xib.FileName), xcode);
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
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn += AppRegainedFocus;
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
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn -= AppRegainedFocus;
		}

		void AppRegainedFocus (object sender, EventArgs e)
		{
			DetectXcodeChanges ();
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
			
			UpdateTypes (true);
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			UpdateTypes (true);
			UpdateXcodeProject ();
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			UpdateTypes (true);
			UpdateXcodeProject ();
		}

		void FilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (!xcodeProjectDirty && syncing && e.Any (finf => IsContent (finf.ProjectFile)))
				xcodeProjectDirty = true;
			
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
				trackedFiles[target] = File.GetLastWriteTime (target);
			}
			else if (!trackedFiles.ContainsKey (target)) {
				trackedFiles[target] = File.GetLastWriteTime (target);
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
			
			string fullH = outputDir.Combine (type.ObjCName + ".h");
			trackedFiles[fullH] = File.GetLastWriteTime (fullH);
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
			var projFile = outputDir.Combine (dnp.Name + ".xcodeproj", dnp.Name + ".pbxproj");
			if (!xcodeProjectDirty && File.Exists (projFile))
				return;
			
			xcodeProjectDirty = false;
			
			if (!Directory.Exists (outputDir))
				Directory.CreateDirectory (outputDir);
			
			trackedFiles.Clear ();
			
			var xcp = new XcodeProject (dnp.Name);
			foreach (var file in dnp.Files.Where (IsPageOrContent)) {
				string pvp = file.ProjectVirtualPath;
				xcp.AddResource (pvp);
				CopyFile (file);
			}
			
			foreach (var cls in userClasses) {
				string h = cls + ".h";
				xcp.AddSource (h);
				xcp.AddSource (cls + ".m");
				string fullH = outputDir.Combine (h);
				trackedFiles[fullH] = File.GetLastWriteTime (fullH);
			}
			
			xcp.Generate (outputDir);
		}
		
		void DetectXcodeChanges ()
		{
			foreach (var f in trackedFiles) {
				var xcwrite = File.GetLastWriteTime (f.Key);
				if (xcwrite <= f.Value)
					continue;
				if (f.Key.EndsWith (".h")) {
					SyncTypeFromXcode (f.Key);
				}
			}
		}
		
		void SyncTypeFromXcode (FilePath hFile)
		{
			var parsed = NSObjectInfoService.ParseHeader (hFile);
			
			var pinfo = NSObjectInfoService.GetProjectInfo (dnp);
			var objcType = pinfo.GetType (hFile.FileNameWithoutExtension);
			if (objcType == null) {
				Console.WriteLine ("Missing objc type {0}", hFile.FileNameWithoutExtension);
				return;
			}
			if (parsed.ObjCName != objcType.ObjCName) {
				Console.WriteLine ("Parsed type name {0} does not match original {1}", parsed.ObjCName, objcType.ObjCName);
				return;
			}
			if (!objcType.IsUserType) {
				Console.WriteLine ("Parsed type {0} is not a user type", objcType);
				return;
			}
			
			//FIXME: detect unresolved types
			pinfo.ResolveTypes (parsed);
			var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider ("C#");
			var options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			var ccu = parsed.GenerateDesignerClass (provider, options, objcType, wrapperName);
			
			//provider.GenerateCodeFromCompileUnit (ccu, Console.Out, options);
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