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
using System.Text;
using Mono.Addins;
using MonoDevelop.MacDev;
using Mono.Unix;
using MonoDevelop.MacDev.Plist;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

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
			
			//make sure the codebehind files are updated before building
			var res = MacBuildUtilities.UpdateCodeBehind (monitor, proj.CodeBehindGenerator, proj.Files);
			if (res.ErrorCount > 0)
				return res;
			
			res = res.Append (base.Build (monitor, item, configuration));
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
			
			if (!PropertyService.IsMac) {
				res.AddWarning ("Cannot compile xib files on non-Mac platforms");
			} else {
				//Interface Builder files
				if (res.Append (MacBuildUtilities.CompileXibFiles (monitor, proj.Files, resDir)).ErrorCount > 0)
					return res;
			}
			
			//info.plist
			var plistOut = conf.AppDirectory.Combine ("Contents", "Info.plist");
			var appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
				if (res.Append (MergeInfoPlist (monitor, proj, conf, appInfoIn, plistOut)).ErrorCount > 0)
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
			
			// external frameworks
			int externalFrameworksCount = proj.Items.GetAll<MonoMacFrameworkItem> ().Count();
			if (externalFrameworksCount > 0) {
				monitor.BeginTask ("Copying frameworks to bundle", externalFrameworksCount);
				var externalFrameworks = conf.AppDirectory.Combine ("Contents", "Frameworks");
				
				if (!Directory.Exists (externalFrameworks))
					Directory.CreateDirectory (externalFrameworks);
				
				foreach (MonoMacFrameworkItem node in proj.Items.GetAll<MonoMacFrameworkItem> ()) {
					var bundleName = Path.GetFileName (node.FullPath);
					monitor.Log.WriteLine (string.Format ("Copying '{0}' to bundle", bundleName));

					var destFramework = externalFrameworks.Combine (bundleName);
					if (Directory.Exists (destFramework)) {
						Directory.Delete (destFramework, true);
					}
					Directory.CreateDirectory (destFramework);
					
					CopyFolder (node.FullPath, destFramework);
					monitor.Step (1);
				}
				
				monitor.EndTask ();
			}
			
			return res;
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
			
			if (PropertyService.IsMac) {
				//Interface Builder files
				var resDir = conf.AppDirectory.Combine ("Contents", "Resources");
				if (MacBuildUtilities.GetIBFilePairs (proj.Files, resDir).Any (NeedsBuilding))
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
			
			//external frameworks
			var externalFrameworks = conf.AppDirectory.Combine ("Contents", "Frameworks");
			if (proj.Items.GetAll<MonoMacFrameworkItem> ().Any ())
			{
				if (!Directory.Exists (externalFrameworks))
			    	return true;
				
				FilePath generatedFilePath = proj.BaseDirectory.Combine ("obj", conf.Id).Combine (proj.LanguageBinding.GetFileName ("MonoMacFrameworks.g"));
				if (!File.Exists (generatedFilePath))
					return true;
					
				if (File.Exists (proj.FileName) && File.GetLastWriteTime (proj.FileName) >= File.GetLastWriteTime (generatedFilePath))
					return true;
			}
			
			foreach (MonoMacFrameworkItem node in proj.Items.GetAll<MonoMacFrameworkItem> ()) {
				string dirName = Path.GetFileName (node.FullPath);
				
				var destFramework = externalFrameworks.Combine (dirName);
				if (!Directory.Exists (destFramework))
					return true;
			}
			
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
			MonoMacProject proj = item as MonoMacProject;
			if (proj == null || !proj.Items.GetAll<MonoMacFrameworkItem> ().Any ())
				return base.Compile (monitor, item, buildData);
			
			var objDir = proj.BaseDirectory.Combine ("obj", buildData.Configuration.Id);
			if (!Directory.Exists (objDir))
				Directory.CreateDirectory (objDir);
			
			var generatedFile = objDir.Combine (proj.LanguageBinding.GetFileName ("MonoMacFrameworks.g"));
			buildData.Items.Add (new ProjectFile (generatedFile, BuildAction.Compile));
			
			if (!File.Exists (generatedFile) || File.GetLastWriteTime (generatedFile) < File.GetLastWriteTime (proj.FileName))
				GenerateExternalFrameworkCodeFile (proj, buildData.Configuration);
	
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
		
		
		static private void GenerateExternalFrameworkCodeFile (MonoMacProject proj, DotNetProjectConfiguration conf)
		{
			var projectConf = conf as MonoMacProjectConfiguration;
			if (projectConf == null)
				return;
			
			FilePath generatedFilePath = proj.BaseDirectory.Combine ("obj", conf.Id, proj.LanguageBinding.GetFileName ("MonoMacFrameworks.g"));
			
			var fi = new FileInfo (generatedFilePath);
			var projFile = new FileInfo (proj.FileName);

			if (fi.Exists && projFile.Exists && projFile.LastWriteTime < fi.LastWriteTime)
				return;
			
			File.Delete (generatedFilePath);
			
			var loadFrameworksCU = new CodeCompileUnit ();
			CodeNamespace projectNameSpace = new CodeNamespace (proj.DefaultNamespace);
			projectNameSpace.Imports.Add (new CodeNamespaceImport ("System"));
			projectNameSpace.Imports.Add (new CodeNamespaceImport ("System.Runtime.InteropServices"));
			projectNameSpace.Imports.Add (new CodeNamespaceImport ("MonoMac.ObjCRuntime"));
			
			var mainClass = new CodeTypeDeclaration ("MonoMacFrameworks") {
				Attributes = MemberAttributes.Public | MemberAttributes.Static
			};
			
			var loadFrameworkMethod = new CodeMemberMethod () {
				Name = "Initialize",
				Attributes = MemberAttributes.Public | MemberAttributes.Static,
			};
			loadFrameworkMethod.Comments.Add (new CodeCommentStatement ("Call this method prior to NSApplication.Init()", true));

			mainClass.Members.Add (loadFrameworkMethod);
			projectNameSpace.Types.Add (mainClass);
			loadFrameworksCU.Namespaces.Add (projectNameSpace);
		
			var dlfcnClass = new CodeTypeReference ("Dlfcn");
			var dlfcnRef = new CodeTypeReferenceExpression (dlfcnClass);
			
			var dlopenMethod = new CodeMethodReferenceExpression (dlfcnRef, "dlopen");
			var dlerrorMethod = new CodeMethodReferenceExpression (dlfcnRef, "dlerror");
		 	
			var consoleType = new CodeTypeReferenceExpression () {
            	Type = new CodeTypeReference (typeof (Console))
			};
			
	        var writeLineRef = new CodeMethodReferenceExpression (consoleType, "WriteLine");
        
			var intPtrType = new CodeTypeReferenceExpression () {
				Type = new CodeTypeReference (typeof (IntPtr))
			};
			
			FilePath externalFrameworks = projectConf.AppDirectory.Combine ("Contents", "Frameworks");
			
			foreach (MonoMacFrameworkItem node in proj.Items.GetAll<MonoMacFrameworkItem> ()) {

				string libName = Path.GetFileName (node.FullPath).Replace (".framework", "");
				
				var writeLine = new CodeMethodInvokeExpression (
					writeLineRef, new CodePrimitiveExpression (
						string.Format ("Failed to open '{0}' with error: '{1}'",
							libName, "{0}")), new CodeMethodInvokeExpression (dlerrorMethod));
	
				var check = new CodeConditionStatement ();
				check.Condition = new CodeBinaryOperatorExpression (
					new CodeMethodInvokeExpression (dlopenMethod,
						new CodePrimitiveExpression ((string)externalFrameworks.Combine (Path.GetFileName (node.FullPath), libName)), 
				                                     new CodePrimitiveExpression (0)),
				    CodeBinaryOperatorType.IdentityEquality,                                               
	                new CodeFieldReferenceExpression (intPtrType, "Zero"));
				check.TrueStatements.Add (writeLine);
				
				loadFrameworkMethod.Statements.Add (check);
			}
			
			CodeDomProvider codeDom = proj.LanguageBinding.GetCodeDomProvider ();
        
        	using (StreamWriter sw = new StreamWriter (generatedFilePath))
        		codeDom.GenerateCodeFromCompileUnit (loadFrameworksCU, sw, null);
		}
		
		static private void CopyFolder (string sourceFolder, string destFolder)
        {
            if (!Directory.Exists (destFolder))
                Directory.CreateDirectory (destFolder);
			
            string[] files = Directory.GetFiles (sourceFolder);
            foreach (string file in files) {
                string name = Path.GetFileName (file);
				if (name[0] == '.')
					continue;
                string dest = Path.Combine (destFolder, name);

                File.Copy (file, dest);
            }
			
            string[] folders = Directory.GetDirectories (sourceFolder);
            foreach (string folder in folders) {
                string name = Path.GetFileName (folder);
				if (name[0] == '.')
					continue;
                string dest = Path.Combine (destFolder, name);
                CopyFolder (folder, dest);
            }
        }
	}
}
