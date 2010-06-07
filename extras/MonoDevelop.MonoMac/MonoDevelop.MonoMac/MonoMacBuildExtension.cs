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

namespace MonoDevelop.MonoMac
{
	
	public class MonoMacBuildExtension : ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			var res = base.Build (monitor, item, configuration);
			if (res.ErrorCount > 0)
				return res;
			
			var proj = item as MonoMacProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return res;
			var conf = (MonoMacProjectConfiguration) configuration.GetConfiguration (item);
			
			//copy exe, mdb, refs, copy-to-output, Content files to Resources
			foreach (var f in GetCopyFiles (proj, configuration, conf).Where (NeedsBuilding)) {
				f.EnsureOutputDirectory ();
				File.Copy (f.Input, f.Output, true);
			}
			
			//Interface Builder files
			var resDir = conf.AppDirectory.Combine ("Contents", "Resources");
			if (res.Append (MacBuildUtilities.CompileXibFiles (monitor, proj.Files, resDir)).ErrorCount > 0)
				return res;
			
			//info.plist
			var plistOut = conf.AppDirectory.Combine ("Contents", "Info.plist");
			var appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			var appInfoInSrc = appInfoIn == null? FilePath.Null : appInfoIn.FilePath;
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
				if (res.Append (MergeInfoPlist (proj, conf, plistOut, appInfoInSrc)).ErrorCount > 0)
					return res;
			
			//launch script
			var ls = conf.LaunchScript;
			if (!File.Exists (ls)) {
				if (!Directory.Exists (ls.ParentDirectory))
					Directory.CreateDirectory (ls.ParentDirectory);
				var src = AddinManager.CurrentAddin.GetFilePath ("MonoMacLaunchScript.sh");
				File.Copy (src, ls, true);
				var fi = new UnixFileInfo (ls);
				fi.FileAccessPermissions |= FileAccessPermissions.UserExecute
					| FileAccessPermissions.GroupExecute | FileAccessPermissions.OtherExecute;
			}
			
			//pkginfo
			var pkgInfo = conf.AppDirectory.Combine ("Contents", "PkgInfo");
			if (!File.Exists (pkgInfo))
				using (var f = File.OpenWrite (pkgInfo))
					f.Write (new byte [] { 0X41, 0X50, 0X50, 0X4C, 0x3f, 0x3f, 0x3f, 0x3f}, 0, 8); // "APPL???"
			
			return res;
		}
		
		BuildResult MergeInfoPlist (MonoMacProject proj, MonoMacProjectConfiguration conf, 
		                            FilePath plistOut, FilePath plistTemplate)
		{
			File.Copy (plistTemplate, plistOut, true);
			return null;
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
			
			//Interface Builder files
			var resDir = conf.AppDirectory.Combine ("Contents", "Resources");
			if (MacBuildUtilities.GetIBFilePairs (proj.Files, resDir).Any (NeedsBuilding))
				return true;
			
			//the Info.plist
			var plistOut = conf.AppDirectory.Combine ("Contents", "Info.plist");
			var appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
			    return true;
			
			//launch script
			var ls = conf.LaunchScript;
			if (new FilePair (proj.FileName, ls).NeedsBuilding ())
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
	}
}
