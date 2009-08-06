// 
// IPhoneBuildExtension.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using PropertyList;
using System.CodeDom.Compiler;




namespace MonoDevelop.IPhone
{
	
	
	public class IPhoneBuildExtension : ProjectServiceExtension
	{
		
		public IPhoneBuildExtension ()
		{
		}
		
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return base.Build (monitor, item, configuration);
			
			//prebuild
			
			BuildResult result = base.Build (monitor, item, configuration);
			if (result.ErrorCount > 0)
				return result;
			
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			
			if (!Directory.Exists (conf.AppDirectory))
				Directory.CreateDirectory (conf.AppDirectory);
			
			FilePath mtouchOutput = conf.NativeExe;
			if (new FilePair (conf.CompiledOutputName, mtouchOutput).NeedsBuilding ()) {
				BuildResult error;
				var mtouch = GetMTouch (proj, monitor, out error);
				if (error != null)
					return error.Append (result);
				
				var args = new StringBuilder ();
				//FIXME: make verbosity configurable?
				args.Append (" -v");
				
				args.Append (" --nomanifest --nosign");
					
				//FIXME: should we error out if the platform is invalid?
				if (conf.Platform == IPhoneProject.PLAT_IPHONE) {
					args.AppendFormat (" -dev \"{0}\" ", conf.AppDirectory);
				} else {
					args.AppendFormat (" -sim \"{0}\" ", conf.AppDirectory);
				}
				
				foreach (string asm in proj.GetReferencedAssemblies (configuration))
					args.AppendFormat (" -r=\"{0}\"", asm);
				
				if (!String.IsNullOrEmpty (conf.ExtraMtouchArgs)) {
					args.Append (" ");
					args.Append (conf.ExtraMtouchArgs);
				}
				
				args.AppendFormat (" \"{0}\"", conf.CompiledOutputName);
				
				mtouch.WorkingDirectory = conf.OutputDirectory;
				mtouch.Arguments = args.ToString ();
				
				monitor.BeginTask (GettextCatalog.GetString ("Compiling to native code"), 0);
				
				string output;
				int code;
				monitor.Log.WriteLine ("{0} {1}", mtouch.FileName, mtouch.Arguments);
				if ((code = ExecuteCommand (monitor, mtouch, out output)) != 0) {
					if (String.IsNullOrEmpty (output)) {
						result.AddError (null, 0, 0, code.ToString (), "mtouch failed with no output");
					} else {
						result.AddError (null, 0, 0, code.ToString (), "mtouch failed with the following message:\n" + output);
					}
					return result;
				}
				
				monitor.EndTask ();
			}
			
			//create the info.plist, merging in the template if it exists
			var plistOut = conf.AppDirectory.Combine ("Info.plist");
			ProjectFile appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
				if (result.Append (UpdateInfoPlist (monitor, proj, conf, appInfoIn)).ErrorCount > 0)
					return result;
			
			var pkgInfo = conf.AppDirectory.Combine ("PkgInfo");
			if (!File.Exists (pkgInfo))
				using (var f = File.OpenWrite (pkgInfo))
					f.Write (new byte [] { 0X41, 0X50, 0X50, 0X4C, 0x3f, 0x3f, 0x3f, 0x3f}, 0, 8);
			
			if (conf.Platform == IPhoneProject.PLAT_IPHONE) {
				monitor.BeginTask (GettextCatalog.GetString ("Signing application"), 0);
				string signingKey = Keychain.GetCertificateName (proj, false);
				if (String.IsNullOrEmpty (signingKey)) {
					result.AddWarning ("No signing key is specified. The application will not be signed");
				} else {
					int signResultCode;
					var psi = new ProcessStartInfo ("codesign") {
						UseShellExecute = false,
						RedirectStandardError = true,
						RedirectStandardOutput = true,
						Arguments = String.Format ("-v -s \"{0}\" \"{1}\"", signingKey, mtouchOutput),
					};
					psi.EnvironmentVariables.Add ("CODESIGN_ALLOCATE", "/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/codesign_allocate");
					string output;
					if ((signResultCode = ExecuteCommand (monitor, psi, out output)) != 0) {
						result.AddError ("Code signing failed: " + output);
						return result;
					}
				}
				monitor.EndTask ();
			}
			//TODO: create/update the xcode project
			return result;
		}
		
		BuildResult UpdateInfoPlist (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf, ProjectFile template)
		{
			var doc = new PlistDocument ();
			if (template != null) {
				try {
					doc.LoadFromXmlFile (template.FilePath);
				} catch (Exception ex) {
					var result = new BuildResult ();
					if (ex is XmlException)
						result.AddError (template.FilePath, ((XmlException)ex).LineNumber, ((XmlException)ex).LinePosition, null, ex.Message);
					else
						result.AddError (template.FilePath, 0, 0, null, ex.Message);
					monitor.ReportError (GettextCatalog.GetString ("Could not load Info.plist template: {0}", ex.Message), null);
					return result;
				}
			}
			
			var dict = doc.Root as PlistDictionary;
			if (dict == null)
				doc.Root = dict = new PropertyList.PlistDictionary ();
			
			bool sim = conf.Platform != IPhoneProject.PLAT_IPHONE;
			
			SetIfNotPresent (dict, "CFBundleDevelopmentRegion",
				String.IsNullOrEmpty (proj.BundleDevelopmentRegion)? "English" : proj.BundleDevelopmentRegion);
			
			SetIfNotPresent (dict, "CFBundleDisplayName", proj.BundleDisplayName ?? proj.Name);
			SetIfNotPresent (dict, "CFBundleExecutable", conf.NativeExe.FileName);
			
			FilePath icon = proj.BundleIcon.ToRelative (proj.BaseDirectory);
			if (!(icon.IsNullOrEmpty || icon.ToString () == "."))
				SetIfNotPresent (dict, "CFBundleIconFile", icon.ToString ());
			
			SetIfNotPresent (dict, "CFBundleIdentifier", proj.BundleIdentifier ?? ("com.yourcompany." + proj.Name));
			SetIfNotPresent (dict, "CFBundleInfoDictionaryVersion", "6.0");
			SetIfNotPresent (dict, "CFBundleName", proj.Name);
			SetIfNotPresent (dict, "CFBundlePackageType", "APPL");
			if (!sim)
				dict["CFBundleResourceSpecification"] = "ResourceRules.plist";
			SetIfNotPresent (dict, "CFBundleSignature", "????");
			SetIfNotPresent (dict,  "CFBundleSupportedPlatforms",
				new PropertyList.PlistArray () { sim? "iphonesimulator" : "iphoneos" });
			SetIfNotPresent (dict, "CFBundleVersion", "1.0");
			SetIfNotPresent (dict, "DTPlatformName", sim? "iphonesimulator" : "iphoneos");
			SetIfNotPresent (dict, "DTSDKName", sim? "iphonesimulator3.0" : "iphoneos3.0");
			SetIfNotPresent (dict,  "LSRequiresIPhoneOS", true);
			
			if (!sim)
				//FIXME allow user to choose version?
				SetIfNotPresent (dict, "MinimumOSVersion", "3.0");
			
			if (!String.IsNullOrEmpty (proj.MainNibFile.ToString ())) {
				string mainNib = proj.MainNibFile.ToRelative (proj.BaseDirectory);
				if (mainNib.EndsWith (".nib") || mainNib.EndsWith (".xib"))
				    mainNib = mainNib.Substring (0, mainNib.Length - 4).Replace ('\\', '/');
				SetIfNotPresent (dict, "NSMainNibFile", mainNib);
			}
			
			var plistOut = conf.AppDirectory.Combine ("Info.plist");
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Updating application manifest"), 0);
				using (XmlTextWriter writer = new XmlTextWriter (plistOut, Encoding.UTF8)) {
					writer.Formatting = Formatting.Indented;
					doc.Write (writer);
				}
				if (!sim) {
					using (XmlTextWriter writer = new XmlTextWriter (conf.AppDirectory.Combine ("ResourceRules.plist"), Encoding.UTF8)) {
						writer.Formatting = Formatting.Indented;
						GenerateResourceRulesPlist ().Write (writer);
					}
				}
			} catch (Exception ex) {
				var result = new BuildResult ();
				result.AddError (plistOut, 0, 0, null, ex.Message);
				monitor.ReportError (GettextCatalog.GetString ("Could not write file '{0}'", plistOut), ex);
				return result;
			} finally {
				monitor.EndTask ();
			}
			
			//TODO: compile PLists to binary
			
			return null;
		}
		
		void SetIfNotPresent (PlistDictionary dict, string key, PlistObjectBase value)
		{
			if (!dict.ContainsKey (key))
				dict[key] = value;
		}
		
		PlistDocument GenerateResourceRulesPlist ()
		{
			return new PlistDocument (
				new PlistDictionary () {
					{ "rules", new PlistDictionary (true) {
						{ ".*", new PlistDictionary () {
							{ "omit", true },
							{ "weight", 10 }
						}},
						{ "ResourceRules.plist", new PlistDictionary () {
							{ "omit", true },
							{ "weight", 100 }
						}}
					}}
				}
			);
		}
		
		internal ProcessStartInfo GetMTouch (IPhoneProject project, IProgressMonitor monitor, out BuildResult error)
		{
			var mtouch = project.TargetRuntime.GetToolPath (project.TargetFramework, "mtouch");
			if (String.IsNullOrEmpty (mtouch)) {
				var err = GettextCatalog.GetString ("Error: Unable to find 'mtouch' tool.");
				monitor.ReportError (err, null);
				error = new BuildResult ();
				error.AddError (null, 0, 0, null, err);
				return null;
			}
			
			error = null;
			return new ProcessStartInfo (mtouch) {
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			};
		}
		
		protected override bool GetNeedsBuilding (SolutionEntityItem item, string configuration)
		{
			if (base.GetNeedsBuilding (item, configuration))
				return true;
			
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return false;
			
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			
			if (!Directory.Exists (conf.AppDirectory))
				return true;
			
			if (!File.Exists (conf.AppDirectory.Combine ("PkgInfo")))
				return true;
			
			// the mtouch output
			FilePath mtouchOutput = conf.NativeExe;
			if (new FilePair (conf.CompiledOutputName, mtouchOutput).NeedsBuilding ())
				return true;
			
			//Interface Builder files
			foreach (var fp in GetFiles (proj.Files, BuildAction.Page, proj.BaseDirectory, conf.AppDirectory))
				if (new FilePair (fp.Input, Path.ChangeExtension (fp.Output, ".nib")).NeedsBuilding ())
					return true;
			
			//Content files
			foreach (var fp in GetFiles (proj.Files, BuildAction.Content, proj.BaseDirectory, conf.AppDirectory))
				if (fp.NeedsBuilding ())
					return true;
			
			// the Info.plist
			var plistOut = conf.AppDirectory.Combine ("Info.plist");
			ProjectFile appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ()))
			    return true;
			
			//TODO: determine whether the the xcode project needs building
			return false;
		}
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			base.Clean (monitor, item, configuration);
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return;
			
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			
			//contains mtouch output, nibs, plist
			if (Directory.Exists (conf.AppDirectory))
				Directory.Delete (conf.AppDirectory, true);
			
			//TODO: remove the xcode project
		}
		
		protected override BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData)
		{
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return base.Compile (monitor, item, buildData);
			
			var cfg = (IPhoneProjectConfiguration) buildData.Configuration;
			
			//make sure the codebehind files are updated before building
			monitor.BeginTask (GettextCatalog.GetString ("Updating CodeBehind files"), 0);	
			var cbWriter = MonoDevelop.DesignerSupport.CodeBehindWriter.CreateForProject (monitor, proj);
			BuildResult result = null;
			if (cbWriter.SupportsPartialTypes) {
				result = CodeBehind.UpdateXibCodebehind (cbWriter, proj, buildData.Items.OfType<ProjectFile> ());
				cbWriter.WriteOpenFiles ();
				if (cbWriter.WrittenCount > 0)
					monitor.Log.WriteLine (GettextCatalog.GetString ("Updated {0} CodeBehind files", cbWriter.WrittenCount));
			} else {
				monitor.ReportWarning ("Cannot generate designer code, because CodeDom provider does not support partial classes.");
			}
			monitor.EndTask ();
			
			if (base.GetNeedsBuilding (item, cfg.Id)) {
				result = base.Compile (monitor, item, buildData).Append (result);
				if (result.ErrorCount > 0)
					return result;
			}
			
			var ibfiles = GetFiles (buildData.Items, BuildAction.Page, proj.BaseDirectory, ((IPhoneProjectConfiguration)buildData.Configuration).AppDirectory)
				.Select (x => new FilePair (x.Input, Path.ChangeExtension (x.Output, ".nib")))
				.Where (x => x.NeedsBuilding ()).ToList ();
			
			if (ibfiles.Count > 0) {
				monitor.BeginTask (GettextCatalog.GetString ("Compiling interface definitions"), 0);	
				foreach (var file in ibfiles) {
					file.EnsureOutputDirectory ();
					var psi = new ProcessStartInfo ("ibtool", String.Format ("\"{0}\" --compile \"{1}\"", file.Input, file.Output));
					monitor.Log.WriteLine (psi.FileName + " " + psi.Arguments);
					psi.WorkingDirectory = cfg.OutputDirectory;
					string errorOutput;
					int code = ExecuteCommand (monitor, psi, out errorOutput);
					if (code != 0) {
						//FIXME: parse the plist that ibtool returns
						result.AddError (null, 0, 0, null, "ibtool returned error code " + code);
					}
				}
				monitor.EndTask ();
			}
			
			var contentFiles = GetFiles (buildData.Items, BuildAction.Content, proj.BaseDirectory, ((IPhoneProjectConfiguration)buildData.Configuration).AppDirectory)
				.Where (x => x.NeedsBuilding ()).ToList ();
			
			if (!proj.BundleIcon.IsNullOrEmpty) {
				FilePair icon = new FilePair (proj.BundleIcon, cfg.AppDirectory.Combine (proj.BundleIcon.FileName));
				if (!File.Exists (proj.BundleIcon)) {
					result.AddError (null, 0, 0, null, String.Format ("Application icon '{0}' is missing.", proj.BundleIcon));
					return result;
				} else {
					contentFiles.Add (icon);
				}
			}
			
			if (contentFiles.Count > 0) {
				monitor.BeginTask (GettextCatalog.GetString ("Copying content files"), contentFiles.Count);	
				foreach (var file in contentFiles) {
					file.EnsureOutputDirectory ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("Copying '{0}' to '{1}'", file.Input, file.Output));
					File.Copy (file.Input, file.Output, true);
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
			return result;
		}
		
		IEnumerable<FilePair> GetFiles<T> (ProjectItemCollection<T> allItems, string buildAction, FilePath projectBase, FilePath appDir) where T : ProjectItem
		{
			foreach (var item in allItems) {
				var pf = item as ProjectFile;
				if (pf != null && pf.BuildAction == buildAction)
					yield return new FilePair (pf.FilePath, pf.FilePath.ToRelative (projectBase).ToAbsolute (appDir));
			}
		}
		
		struct FilePair
		{
			public FilePair (FilePath input, FilePath output)
			{
				this.Input = input;
				this.Output = output;
			}
			public FilePath Input, Output;
			
			public bool NeedsBuilding ()
			{
				return !File.Exists (Output) || File.GetLastWriteTime (Input) > File.GetLastWriteTime (Output);
			}
			
			public void EnsureOutputDirectory ()
			{
				if (!Directory.Exists (Output.ParentDirectory))
					Directory.CreateDirectory (Output.ParentDirectory);
			}
		}
		
		//copied from MoonlightBuildExtension
		int ExecuteCommand (IProgressMonitor monitor, System.Diagnostics.ProcessStartInfo startInfo, out string errorOutput)
		{
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			
			errorOutput = string.Empty;
			int exitCode = -1;
			
			var swError = new StringWriter ();
			var chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (swError);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				var p = Runtime.ProcessService.StartProcess (startInfo, monitor.Log, chainedError, null);
				operationMonitor.AddOperation (p); //handles cancellation
				
				p.WaitForOutput ();
				errorOutput = swError.ToString ();
				exitCode = p.ExitCode;
				p.Dispose ();
				
				if (monitor.IsCancelRequested) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Build cancelled"));
					monitor.ReportError (GettextCatalog.GetString ("Build cancelled"), null);
					if (exitCode == 0)
						exitCode = -1;
				}
			} finally {
				chainedError.Close ();
				swError.Close ();
				operationMonitor.Dispose ();
			}
			
			return exitCode;
		}
	}
}
