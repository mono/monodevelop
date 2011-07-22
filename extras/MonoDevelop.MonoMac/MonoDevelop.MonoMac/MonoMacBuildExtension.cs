// 
// MonoMacBuildExtension.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.CodeDom.Compiler;
using Mono.Addins;
using MonoDevelop.MacDev;
using Mono.Unix;
using MonoDevelop.MacDev.Plist;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.MonoMac
{
	public class MonoMacBuildExtension : ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			var proj = item as MonoMacProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return base.Build (monitor, item, configuration);
			
			var conf = (MonoMacProjectConfiguration) configuration.GetConfiguration (item);
			var resDir = conf.AppDirectory.Combine ("Contents", "Resources");
			var appDir = conf.AppDirectory;
			
			var res = base.Build (monitor, item, configuration);
			if (res.ErrorCount > 0)
				return res;
			
			//copy exe, mdb, refs, copy-to-output, Content files to Resources
			var filesToCopy = GetCopyFiles (proj, configuration, conf).Where (NeedsBuilding).ToList ();
			if (filesToCopy.Count > 0) {
				monitor.BeginTask ("Copying resource files to app bundle", filesToCopy.Count);
				foreach (var f in filesToCopy) {
					f.EnsureOutputDirectory ();
					File.Copy (f.Input, f.Output, true);
					monitor.Log.WriteLine ("Copied {0}", f.Output.ToRelative (appDir));
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
			
			//FIXME: only do this check if there are actually xib files
			if (!Platform.IsMac) {
				res.AddWarning ("Cannot compile xib files on non-Mac platforms");
			} else {
				//Interface Builder files
				if (res.Append (CompileXibFiles (monitor, proj.Files, resDir)).ErrorCount > 0)
					return res;
			}
			
			//info.plist
			var plistOut = conf.AppDirectory.Combine ("Contents", "Info.plist");
			var appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
				if (res.Append (MergeInfoPlist (monitor, proj, conf, appInfoIn, plistOut)).ErrorCount > 0)
					return res;
			
			if (Platform.IsWindows) {
				res.AddWarning ("Cannot create app bundle on Windows");
			} else {	
				//launch script
				var macOSDir = appDir.Combine ("Contents", "MacOS");
				CopyExecutableFile (AddinManager.CurrentAddin.GetFilePath ("MonoMacLaunchScript.sh"), conf.LaunchScript);
				CopyExecutableFile (AddinManager.CurrentAddin.GetFilePath ("mono-version-check"),
					macOSDir.Combine ("mono-version-check"));
				
				var si = new UnixSymbolicLinkInfo (appDir.Combine (conf.AppName));
				if (!si.Exists)
					si.CreateSymbolicLinkTo ("/Library/Frameworks/Mono.framework/Versions/Current/bin/mono");
			}
			
			//pkginfo
			var pkgInfo = conf.AppDirectory.Combine ("Contents", "PkgInfo");
			if (!File.Exists (pkgInfo))
				using (var f = File.OpenWrite (pkgInfo))
					f.Write (new byte [] { 0X41, 0X50, 0X50, 0X4C, 0x3f, 0x3f, 0x3f, 0x3f}, 0, 8); // "APPL???"
			
			return res;
		}
		
		static void CopyExecutableFile (FilePath src, FilePath dest)
		{
			if (File.Exists (dest))
				return;
			
			if (!Directory.Exists (dest.ParentDirectory))
				Directory.CreateDirectory (dest.ParentDirectory);
			File.Copy (src, dest, true);
			var fi = new UnixFileInfo (dest);
			fi.FileAccessPermissions |= FileAccessPermissions.UserExecute
				| FileAccessPermissions.GroupExecute | FileAccessPermissions.OtherExecute;
		}
		
		BuildResult MergeInfoPlist (IProgressMonitor monitor, MonoMacProject proj, MonoMacProjectConfiguration conf, 
		                            ProjectFile template, FilePath plistOut)
		{
			return MacBuildUtilities.CreateMergedPlist (monitor, template, plistOut, (PlistDocument doc) => {
				var result = new BuildResult ();
				var dict = doc.Root as PlistDictionary;
				if (dict == null)
					doc.Root = dict = new PlistDictionary ();
				
				//required keys that the user is likely to want to modify
				SetIfNotPresent (dict, "CFBundleName", proj.Name);
				SetIfNotPresent (dict, "CFBundleIdentifier", "com.yourcompany." + proj.Name);
				SetIfNotPresent (dict, "CFBundleShortVersionString", proj.Version);
				SetIfNotPresent (dict, "CFBundleVersion", "1");
				SetIfNotPresent (dict, "LSMinimumSystemVersion", "10.6");
				SetIfNotPresent (dict, "CFBundleDevelopmentRegion", "English");
				
				//required keys that the user probably should not modify
				dict["CFBundleExecutable"] = conf.LaunchScript.FileName;
				SetIfNotPresent (dict, "CFBundleInfoDictionaryVersion", "6.0");
				SetIfNotPresent (dict, "CFBundlePackageType", "APPL");
				SetIfNotPresent (dict, "CFBundleSignature", "????");
				
				return result;
			});
		}
		
		static void SetIfNotPresent (PlistDictionary dict, string key, PlistObjectBase value)
		{
			if (!dict.ContainsKey (key))
				dict[key] = value;
		}
		
		protected override bool GetNeedsBuilding (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			if (base.GetNeedsBuilding (item, configuration))
				return true;
			
			var proj = item as MonoMacProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return false;
			var conf = (MonoMacProjectConfiguration) configuration.GetConfiguration (item);
			
			//all content files
			if (GetCopyFiles (proj, configuration, conf).Where (NeedsBuilding).Any ())
				return true;
			
			if (Platform.IsMac) {
				//Interface Builder files
				var resDir = conf.AppDirectory.Combine ("Contents", "Resources");
				if (GetIBFilePairs (proj.Files, resDir).Any (NeedsBuilding))
					return true;
			}
			
			//the Info.plist
			var plistOut = conf.AppDirectory.Combine ("Contents", "Info.plist");
			var appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
			    return true;
			
			//launch script
			var ls = conf.LaunchScript;
			if (!File.Exists (ls))
				return true;
			
			//pkginfo
			if (!File.Exists (conf.AppDirectory.Combine ("Contents", "PkgInfo")))
			    return true;
			
			return false;
		}
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			base.Clean (monitor, item, configuration);
			
			var proj = item as MonoMacProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return;
			var conf = (MonoMacProjectConfiguration) configuration.GetConfiguration (item);
			
			if (Directory.Exists (conf.AppDirectory))
				Directory.Delete (conf.AppDirectory, true);
		}
		
		protected override BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData)
		{
			return base.Compile (monitor, item, buildData);
		}
		
		static bool NeedsBuilding (FilePair fp)
		{
			return fp.NeedsBuilding ();
		}
		
		static BuildResult BuildError (string error)
		{
			var br = new BuildResult ();
			br.AddError (error);
			return br;
		}
		
		static IEnumerable<FilePair> GetCopyFiles (MonoMacProject project, ConfigurationSelector sel, MonoMacProjectConfiguration conf)
		{
			var resDir = conf.AppDirectoryResources;
			var output = conf.CompiledOutputName;
			yield return new FilePair (output, resDir.Combine (output.FileName));
			
			if (conf.DebugMode) {
				FilePath mdbFile = project.TargetRuntime.GetAssemblyDebugInfoFile (output);
				if (File.Exists (mdbFile))
					yield return new FilePair (mdbFile, resDir.Combine (mdbFile.FileName));
			}
			
			foreach (FileCopySet.Item s in project.GetSupportFileList (sel))
				yield return new FilePair (s.Src, resDir.Combine (s.Target));
			
			foreach (var pf in project.Files)
				if (pf.BuildAction == BuildAction.Content)
					yield return new FilePair (pf.FilePath, pf.ProjectVirtualPath.ToAbsolute (resDir));
		}
		
		public static BuildResult CompileXibFiles (IProgressMonitor monitor, IEnumerable<ProjectFile> files,
			FilePath outputRoot)
		{
			var result = new BuildResult ();
			var ibfiles = GetIBFilePairs (files, outputRoot).Where (NeedsBuilding).ToList ();
			
			if (ibfiles.Count > 0) {
				monitor.BeginTask (GettextCatalog.GetString ("Compiling interface definitions"), 0);
				foreach (var file in ibfiles) {
					file.EnsureOutputDirectory ();
					var args = new ProcessArgumentBuilder ();
					args.Add ("--errors", "--warnings", "--notices", "--output-format", "human-readable-text");
					args.AddQuoted (file.Input);
					args.Add ("--compile");
					args.AddQuoted (file.Output);
					var psi = new ProcessStartInfo ("ibtool", args.ToString ());
					monitor.Log.WriteLine (psi.FileName + " " + psi.Arguments);
					int code;
					try {
						code = MacBuildUtilities.ExecuteBuildCommand (monitor, psi);
					} catch (System.ComponentModel.Win32Exception ex) {
						LoggingService.LogError ("Error running ibtool", ex);
						result.AddError (null, 0, 0, null, "ibtool not found. Please ensure the Apple SDK is installed.");
						return result;
					}
					if (monitor.IsCancelRequested)
						return result;
					
					if (code != 0) {
						result.AddError (null, 0, 0, null, "ibtool returned error code " + code);
						return result;
					}
				}
				monitor.EndTask ();
			}
			return result;
		}
		
		public static IEnumerable<FilePair> GetIBFilePairs (IEnumerable<ProjectFile> files, string outputRoot)
		{
			foreach (var pf in files) {
				if (pf.BuildAction != BuildAction.Page)
					continue;
				
				var name = pf.ProjectVirtualPath;
				switch (name.Extension) {
				case ".xib":
					name = name.ChangeExtension (".nib");
					break;
				case ".nib":
					break;
				default:
					//FIXME: warn about unknown type
					continue;
				}
				
				string[] splits = name.ToString ().Split (Path.DirectorySeparatorChar);
				name = splits.Last ();
				if (splits.Length > 1 && splits[0].EndsWith (".lproj"))
					name = new FilePath (splits[0]).Combine (name);
				
				yield return new FilePair (pf.FilePath, name.ToAbsolute (outputRoot));
			}
		}
	}
}
