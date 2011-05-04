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
		
		bool updatingProjectFiles;
		
		FilePath outputDir, xcproj, pbxproj;
		
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
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusOut += AppLostFocus;
		}
		
		void DisableSyncing ()
		{
			if (!syncing)
				return;
			syncing = false;
			xcodeProjectDirty = false;
			
			XcodeCloseWorkspaceInPath (outputDir);
			if (Directory.Exists (outputDir))
				Directory.Delete (outputDir, true);
			
			dnp.FileAddedToProject -= FileAddedToProject;
			dnp.FilePropertyChangedInProject -= FilePropertyChangedInProject;;
			dnp.FileRemovedFromProject -= FileRemovedFromProject;
			dnp.FileChangedInProject -= FileChangedInProject;
			dnp.NameChanged -= ProjectNameChanged;
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusIn -= AppRegainedFocus;
			MonoDevelop.Ide.IdeApp.CommandService.ApplicationFocusOut -= AppLostFocus;
		}
		
		void UpdateOutputDir ()
		{
			outputDir = dnp.BaseDirectory.Combine ("obj", "Xcode");
			xcproj = outputDir.Combine (dnp.Name + ".xcodeproj");
			pbxproj = xcproj.Combine (dnp.Name + ".pbxproj");
		}
		
		void AppRegainedFocus (object sender, EventArgs e)
		{
			if (IsXcodeProjectOpen ()) {
				XcodeSaveAllInPath (outputDir);
			} else {
				DisableSyncing ();
			}
			DetectXcodeChanges ();
		}
		
		void AppLostFocus (object sender, EventArgs e)
		{
			//TODO: sync changed types
		}
		
		bool IsXcodeProjectOpen ()
		{
			return XcodeCheckRunning () && XcodeCheckProjectOpen (xcproj);
		}
		
		void OpenXcodeProject ()
		{
			//FIXME: show a UI and progress while we do the initial sync
			EnableSyncing ();
			UpdateTypes (true);
			UpdateXcodeProject ();
			
			MonoMac.AppKit.NSWorkspace.SharedWorkspace.OpenFile (
				xcproj,
				XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION);
		}
		
		public IAsyncOperation OpenDocument (FilePath xib)
		{
			OpenXcodeProject ();
			
			//FIXME: detect when the project's opened, so it will open the xib file in the project
			//FIXME: why is the xcode project sometime "locked for editing"?
			//FIXME: switch xcode to the assistant view with the correct header file
			MonoMac.AppKit.NSWorkspace.SharedWorkspace.OpenFile (
				outputDir.Combine (xib.FileName),
				XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION);
			
			//FIXME: actually report progress of this operation
			return MonoDevelop.Core.ProgressMonitoring.NullAsyncOperation.Success;
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
			if (outputDir.IsNullOrEmpty || !Directory.Exists (outputDir))
				return;
			
			var isOpen = IsXcodeProjectOpen ();
			if (isOpen)
				XcodeCloseWorkspaceInPath (outputDir);
			
			Directory.Delete (outputDir, true);
			UpdateOutputDir ();
			
			if (isOpen)
				OpenXcodeProject ();
		}
		
		void FileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			if (syncing && e.Any (finf => IsPage (finf.ProjectFile)))
				if (!dnp.Files.Any (IsPage))
					DisableSyncing ();
			
			//FIXME: make this async
			UpdateTypes (true);
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			//FIXME: make this async
			UpdateTypes (true);
			UpdateXcodeProject ();
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (updatingProjectFiles)
				return;
			
			//FIXME: make this async
			UpdateTypes (true);
			UpdateXcodeProject ();
		}

		void FilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (!xcodeProjectDirty && syncing && e.Any (finf => IsContent (finf.ProjectFile)))
				xcodeProjectDirty = true;
			
			//FIXME: make this async
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
		
		//FIXME: report errors
		void UpdateXcodeProject ()
		{
			if (!xcodeProjectDirty && File.Exists (pbxproj))
				return;
			
			xcodeProjectDirty = false;
			
			if (!Directory.Exists (outputDir))
				Directory.CreateDirectory (outputDir);
			
			trackedFiles.Clear ();
			
			//FIXME: use resource directories in Xcode
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
		
		#endregion
		
		#region Inbound syncing
		
		//FIXME: make this async so it doesn't block the UI
		void DetectXcodeChanges ()
		{
			NSObjectProjectInfo pinfo = null;
			var typeSyncJobs = new List<SyncObjcBackJob> ();
			
			foreach (var f in trackedFiles.ToList ()) {
				var xcwrite = File.GetLastWriteTime (f.Key);
				if (xcwrite <= f.Value)
					continue;
				if (f.Key.EndsWith (".h")) {
					var job = ReadTypeFromXcode (f.Key, ref pinfo);
					if (job != null)
						typeSyncJobs.Add (job);
				}
				CopyChangedXibsBack ();
			}
			
			if (typeSyncJobs.Count > 0) {
				UpdateCliTypes (pinfo, typeSyncJobs);
			}
		}
		
		//FIXME: error reporting
		void CopyChangedXibsBack ()
		{
			foreach (var file in dnp.Files.Where (IsPage)) {
				var target = outputDir.Combine (file.ProjectVirtualPath);
				if (!File.Exists (target))
					continue;
				DateTime modified;
				if (trackedFiles.TryGetValue (target, out modified) && File.GetLastWriteTime (target) > modified)
					File.Copy (target, file.FilePath);
			}
		}
		
		//FIXME: report errors
		SyncObjcBackJob ReadTypeFromXcode (FilePath hFile, ref NSObjectProjectInfo pinfo)
		{
			var parsed = NSObjectInfoService.ParseHeader (hFile);
			
			if (pinfo == null)
				pinfo = NSObjectInfoService.GetProjectInfo (dnp);
			
			var objcType = pinfo.GetType (hFile.FileNameWithoutExtension);
			if (objcType == null) {
				Console.WriteLine ("Missing objc type {0}", hFile.FileNameWithoutExtension);
				return null;
			}
			if (parsed.ObjCName != objcType.ObjCName) {
				Console.WriteLine ("Parsed type name {0} does not match original {1}", parsed.ObjCName, objcType.ObjCName);
				return null;
			}
			if (!objcType.IsUserType) {
				Console.WriteLine ("Parsed type {0} is not a user type", objcType);
				return null;
			}
			
			//FIXME: detect unresolved types
			parsed.MergeCliInfo (objcType);
			pinfo.ResolveTypes (parsed);
			
			return new SyncObjcBackJob () {
				HFile = hFile,
				DesignerFile = objcType.GetDesignerFile (),
				Type = parsed,
			};
		}
		
		//FIXME: error handling
		void UpdateCliTypes (NSObjectProjectInfo pinfo, List<SyncObjcBackJob> typeSyncJobs)
		{
			var provider = dnp.LanguageBinding.GetCodeDomProvider ();
			var options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			var writer = MonoDevelop.DesignerSupport.CodeBehindWriter.CreateForProject (
				new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), dnp);
			
			
			var designerFile = new Dictionary<string, List<NSObjectTypeInfo>> ();
			Dictionary<string,ProjectFile> newFiles = null;
			foreach (var job in typeSyncJobs) {
				//generate designer filenames for classes without designer files
				if (job.DesignerFile == null) {
					var df = CreateDesignerFile (job);
					job.DesignerFile = df.FilePath;
					if (newFiles == null)
						newFiles = new Dictionary<string, ProjectFile> ();
					if (!newFiles.ContainsKey (job.DesignerFile))
						newFiles.Add (job.DesignerFile, df);
				}
				//group all the types by designer file
				List<NSObjectTypeInfo> types;
				if (!designerFile.TryGetValue (job.DesignerFile, out types))
					designerFile[job.DesignerFile] = types = new List<NSObjectTypeInfo> ();
				types.Add (job.Type);
			}
			
			//add in other designer types that exist in the designer files
			foreach (var ut in pinfo.GetTypes ()) {
				if (!ut.IsUserType)
					continue;
				var df = ut.GetDesignerFile ();
				List<NSObjectTypeInfo> types;
				if (df != null && designerFile.TryGetValue (df, out types))
					if (!types.Any (t => t.ObjCName == ut.ObjCName))
						types.Add (ut);
			}
			
			updatingProjectFiles = true;
			
			try {
				foreach (var df in designerFile) {
					var ccu = GenerateCompileUnit (provider, options, df.Key, df.Value);
					writer.Write (ccu, df.Key);
				}
				writer.WriteOpenFiles ();
				
				foreach (var job in typeSyncJobs) {
					pinfo.InsertUpdatedType (job.Type);
					trackedFiles[job.HFile] = File.GetLastWriteTime (job.HFile);
				}
				
				if (newFiles != null) {
					foreach (var f in newFiles)
						dnp.AddFile (f.Value);
					Ide.IdeApp.ProjectOperations.Save (dnp);
				}
			} finally {
				updatingProjectFiles = false;
			}
		}
		
		//FIXME: create the designer file based on xibs that use the class, not the other parts?
		ProjectFile CreateDesignerFile (SyncObjcBackJob job)
		{
			int i = 0;
			FilePath designerFile = null;
			do {
				FilePath f = job.Type.DefinedIn[0];
				string suffix = (i > 0? i.ToString () : "");
				string name = f.FileNameWithoutExtension + suffix + ".designer" + f.Extension;
				designerFile = f.ParentDirectory.Combine (name);
			} while (dnp.Files.GetFileWithVirtualPath (designerFile.ToRelative (dnp.BaseDirectory)) != null);
			var dependsOn = ((FilePath)job.Type.DefinedIn[0]).FileName;
			return new ProjectFile (designerFile, BuildAction.Compile) { DependsOn = dependsOn };
		}
		
		System.CodeDom.CodeCompileUnit GenerateCompileUnit (System.CodeDom.Compiler.CodeDomProvider provider,
			System.CodeDom.Compiler.CodeGeneratorOptions options, string file, List<NSObjectTypeInfo> types)
		{
			var ccu = new System.CodeDom.CodeCompileUnit ();
			var namespaces = new Dictionary<string, System.CodeDom.CodeNamespace> ();
			foreach (var t in types) {
				System.CodeDom.CodeTypeDeclaration type;
				string nsName;
				System.CodeDom.CodeNamespace ns;
				t.GenerateCodeTypeDeclaration (provider, options, wrapperName, out type, out nsName);
				if (!namespaces.TryGetValue (nsName, out ns)) {
					namespaces[nsName] = ns = new System.CodeDom.CodeNamespace (nsName);
					ccu.Namespaces.Add (ns);
				}
				ns.Types.Add (type);
			}
			return ccu;
		}
		
		class SyncObjcBackJob
		{
			public string HFile;
			public string DesignerFile;
			public NSObjectTypeInfo Type;
		}
		
		#endregion
		
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			DisableSyncing ();
		}
		
		static bool XcodeCheckRunning ()
		{
			var appPathKey = new MonoMac.Foundation.NSString ("NSApplicationPath");
			var xcodePath = new MonoMac.Foundation.NSString (XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION);
			return MonoMac.AppKit.NSWorkspace.SharedWorkspace.LaunchedApplications.Any (
				app => xcodePath.Equals (app[appPathKey]));
		}
		
		static void XcodeSaveAllInPath (string path)
		{
			MacInterop.AppleScript.Run (
				XCODE_SAVE_IN_PATH,
				XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION,
				path);
		}
		
		static bool XcodeCheckProjectOpen (string project)
		{
			var ret = MacInterop.AppleScript.Run (
				XCODE_CHECK_PROJECT_OPEN,
				XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION,
				project);
			return ret == "true";
		}
		
		static bool XcodeCloseWorkspaceInPath (string path)
		{
			var ret = MacInterop.AppleScript.Run (
				XCODE_CLOSE_WORKSPACE_IN_PATH,
				XcodeInterfaceBuilderDesktopApplication.XCODE_LOCATION,
				path);
			return ret == "true";
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
