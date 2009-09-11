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
				
				AppendExtrasMtouchArgs (args, proj, conf);
				
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
			
			if (result.Append (ProcessPackaging (monitor, proj, conf)).ErrorCount > 0)
				return result;
			
			//TODO: create/update the xcode project
			return result;
		}

		static internal void AppendExtrasMtouchArgs (StringBuilder args, IPhoneProject proj, IPhoneProjectConfiguration conf)
		{
			AppendExtraArgs (args, conf.ExtraMtouchArgs, proj, conf);
		}
		
		static void AppendExtraArgs (StringBuilder args, string extraArgs, IPhoneProject proj, IPhoneProjectConfiguration conf)
		{
			if (!String.IsNullOrEmpty (extraArgs)) {
				args.Append (" ");
				var customTags = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase) {
					{ "projectdir", proj.BaseDirectory },
					{ "solutiondir", proj.ParentSolution.BaseDirectory },
					{ "appbundledir", conf.AppDirectory },
					{ "targetpath", conf.CompiledOutputName },
					{ "targetdir", conf.CompiledOutputName.ParentDirectory },
					{ "targetname", conf.CompiledOutputName.FileName },
					{ "targetext", conf.CompiledOutputName.Extension },
				};
				string substExtraArgs = StringParserService.Parse (extraArgs, customTags);
				args.Append (" ");
				args.Append (substExtraArgs);
			}
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
			SetIfNotPresent (dict, "CFBundleVersion", proj.BundleVersion ?? "1.0");
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
			} catch (Exception ex) {
				var result = new BuildResult ();
				result.AddError (plistOut, 0, 0, null, ex.Message);
				monitor.ReportError (GettextCatalog.GetString ("Could not write file '{0}'", plistOut), ex);
				return result;
			} finally {
				monitor.EndTask ();
			}
			
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
			return GetTool ("mtouch", project, monitor, out error);
		}
		
		internal ProcessStartInfo GetTool (string tool, IPhoneProject project, IProgressMonitor monitor, out BuildResult error)
		{
			var toolPath = project.TargetRuntime.GetToolPath (project.TargetFramework, tool);
			if (String.IsNullOrEmpty (toolPath)) {
				var err = GettextCatalog.GetString ("Error: Unable to find '" + tool + "' tool.");
				monitor.ReportError (err, null);
				error = new BuildResult ();
				error.AddError (null, 0, 0, null, err);
				return null;
			}
			
			error = null;
			return new ProcessStartInfo (toolPath) {
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
			if (GetIBFilePairs (proj.Files, conf.AppDirectory).Where (NeedsBuilding).Any ())
				return true;
			
			//Content files
			if (GetContentFilePairs (proj.Files, conf.AppDirectory).Where (NeedsBuilding).Any ())
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
		
		static bool NeedsBuilding (FilePair fp)
		{
			return fp.NeedsBuilding ();
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
			
			//remove the xcode project
			if (Directory.Exists (conf.OutputDirectory.Combine ("XcodeProject")))
				Directory.Delete (conf.OutputDirectory.Combine ("XcodeProject"), true);
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
			
			string appDir = ((IPhoneProjectConfiguration)buildData.Configuration).AppDirectory;
			
			var ibfiles = GetIBFilePairs (buildData.Items.OfType<ProjectFile> (), appDir).Where (NeedsBuilding).ToList ();
			
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
			
			var contentFiles = GetContentFilePairs (buildData.Items.OfType<ProjectFile> (), appDir).Where (NeedsBuilding).ToList ();
			
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
		
		BuildResult ProcessPackaging (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf)
		{
			bool sim = conf.Platform != IPhoneProject.PLAT_IPHONE;
			bool dist = !sim && !string.IsNullOrEmpty (conf.CodesignKey) && conf.CodesignKey.StartsWith (Keychain.DIST_CERT_PREFIX);
			BuildResult result = new BuildResult ();
			
			//don't bother signing in the sim
			if (sim)
				return null;
			
			var pkgInfo = conf.AppDirectory.Combine ("PkgInfo");
			if (!File.Exists (pkgInfo))
				using (var f = File.OpenWrite (pkgInfo))
					f.Write (new byte [] { 0X41, 0X50, 0X50, 0X4C, 0x3f, 0x3f, 0x3f, 0x3f}, 0, 8);
			
			monitor.BeginTask (GettextCatalog.GetString ("Compressing resources"), 0);
			
			var optTool = new ProcessStartInfo () {
				FileName = "/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/iphoneos-optimize",
				Arguments = conf.AppDirectory,
			};
			
			monitor.Log.WriteLine (optTool.FileName + " " + optTool.Arguments);
			string errorOutput;
			int code = ExecuteCommand (monitor, optTool, out errorOutput);
			if (code != 0) {
				result.AddError ("Compressing the resources failed: " + errorOutput);
				return result;
			}
			
			monitor.EndTask ();
			
			string xcentName = null;
			MobileProvision provision = null;
			
			if (dist) {
				monitor.BeginTask (GettextCatalog.GetString ("Embedding provisioning profile"), 0);
				
				if (string.IsNullOrEmpty (conf.CodesignProvision)) {
					string err = string.Format ("Provisioning profile missing from code signing settings");
					result.AddError (err);
					return result;
				}
				
				string provisionFile = MobileProvision.ProfileDirectory.Combine (conf.CodesignProvision).ChangeExtension (".mobileprovision");
				if (!File.Exists (provisionFile)) {
					string err = string.Format ("The provisioning profile '{0}' could not be found", conf.CodesignProvision);
					result.AddError (err);
					return result;
				}
				
				try {
					provision = MobileProvision.LoadFromFile (provisionFile);
				} catch (Exception ex) {
					string msg = "Could not read mobile provisioning file '" + provisionFile + "'.";
					monitor.ReportError (msg, ex);
					result.AddError (msg);
					return result;
				}
				string appid = provision.ApplicationIdentifierPrefix + "." + proj.BundleIdentifier;
				
				if (provision.Entitlements.ContainsKey ("application-identifier")) {
					var allowed = ((PlistString)provision.Entitlements.ContainsKey ("application-identifier")).Value;
					for (int i = 0;; i++) {
						if (i < allowed.Length && i < appid.Length) {
							if (allowed[i] == '*')
								break;
							if (appid[i] == allowed[i])
								continue;
						}
						result.AddWarning (String.Format (
						 	"Application identifier '{0}' does not match the provisioning profile entitlements ID '{1}'.",
							appid, allowed));
					}
				}
				
				try {
					File.Copy (provisionFile, conf.AppDirectory.Combine ("embedded.mobileprovision"), true);
				} catch (IOException ex) {
					result.AddError ("Embedding the provisioning profile failed: " + ex.Message);
					return result;
				}
				
				monitor.EndTask ();
				
				monitor.BeginTask (GettextCatalog.GetString ("Processing entitlements file"), 0);
				
				BuildResult mtpResult;
				var mtouchpack = GetTool ("mtouchpack", proj, monitor, out mtpResult);
				if (mtouchpack == null)
					return result.Append (mtpResult);
				
				if (!string.IsNullOrEmpty (conf.CodesignEntitlements)) {
					if (!File.Exists (conf.CodesignEntitlements))
						result.AddWarning ("Entitlements file \"" + conf.CodesignEntitlements + "\" not found. Using default.");
				}
				
				xcentName = Path.ChangeExtension (conf.OutputAssembly, ".xcent");
				
				mtouchpack.Arguments = string.Format ("-genxcent \"{0}\" -appid=\"{2}\"", xcentName, appid);
				if(!string.IsNullOrEmpty (conf.CodesignEntitlements))
					mtouchpack.Arguments = mtouchpack.Arguments + string.Format (" -entitlements \"{1}\"", conf.CodesignEntitlements);
				
				monitor.Log.WriteLine ("mtouchpack " + mtouchpack.Arguments);
				code = ExecuteCommand (monitor, mtouchpack, out errorOutput);
				if (code != 0) {
					result.AddError ("Processing the entitlements failed: " + errorOutput);
					return result;
				}
				
				monitor.EndTask ();
				
				monitor.BeginTask (GettextCatalog.GetString ("Preparing resources rules"), 0);
				
				string resRulesFile = conf.AppDirectory.Combine ("ResourceRules.plist");
				if (File.Exists (resRulesFile))
					File.Delete (resRulesFile);
				
				bool addedResRules = false;
				if (!string.IsNullOrEmpty (conf.CodesignResourceRules)) {
					if (File.Exists (conf.CodesignResourceRules)) {
						File.Copy (conf.CodesignResourceRules, resRulesFile);
						addedResRules = true;
					} else {
						result.AddWarning ("Resources rules file \"" + conf.CodesignResourceRules + "\" not found. Using default.");
					}
				}
				
				if (!addedResRules) {
					using (XmlTextWriter writer = new XmlTextWriter (conf.AppDirectory.Combine ("ResourceRules.plist"), Encoding.UTF8)) {
						writer.Formatting = Formatting.Indented;
						GenerateResourceRulesPlist ().Write (writer);
					}
				}
				
				monitor.EndTask ();
			}
			
			if (String.IsNullOrEmpty (conf.CodesignKey)) {
				monitor.Log.WriteLine ("Code signing disabled, skipping signing.");
				return result;
			}
			
			monitor.BeginTask (GettextCatalog.GetString ("Signing application"), 0);
			
			IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate2> installedKeyNames;
			
			if (conf.CodesignKey == Keychain.DEV_CERT_PREFIX || conf.CodesignKey == Keychain.DIST_CERT_PREFIX) {
				installedKeyNames = Keychain.GetAllSigningCertificates ()
					.Where (c => Keychain.GetCertificateCommonName (c).StartsWith (conf.CodesignKey));
			} else {
				installedKeyNames = Keychain.GetAllSigningCertificates ()
					.Where (c => Keychain.GetCertificateCommonName (c) == conf.CodesignKey);
			}
			
			if (provision != null) {
				installedKeyNames = installedKeyNames.Where (c => {
					foreach (var provcert in provision.DeveloperCertificates)
						if (c.Thumbprint == provcert.Thumbprint)
							return true;
					return false;
				});
			}
			
			var installedCert = installedKeyNames.FirstOrDefault ();
				
			if (installedCert == null) {
				if (provision != null)
					result.AddError ("Identity '" + conf.CodesignKey + "' did not match the provisioning profile \""
					                 + provision.Name + "\". The application will not be signed");
				else
					result.AddError ("A key could not be found matching the name \"" + conf.CodesignKey
					                    + "\". The application will not be signed");

				return result;
			}
			
			var args = new StringBuilder ();
			args.AppendFormat ("-v -f -s \"{0}\"", Keychain.GetCertificateCommonName (installedCert));
			
			if (dist) {
				args.AppendFormat (" --resources-rules=\"{0}\" --entitlements \"{1}\"",
				                   conf.AppDirectory.Combine ("ResourceRules.plist"), xcentName);
			}
			
			args.Append (" ");
			args.Append (conf.AppDirectory);
			
			AppendExtraArgs (args, conf.CodesignExtraArgs, proj, conf);
				
			int signResultCode;
			var psi = new ProcessStartInfo ("codesign") {
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				Arguments = args.ToString (),
			};
			
			monitor.Log.WriteLine ("codesign ", psi.Arguments);
			psi.EnvironmentVariables.Add ("CODESIGN_ALLOCATE", "/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/codesign_allocate");
			string output;
			if ((signResultCode = ExecuteCommand (monitor, psi, out output)) != 0) {
				result.AddError (string.Format ("Code signing failed with error code {0}: {1}", signResultCode, output));
				return result;
			}
			monitor.EndTask ();
			
			return result;
		}
		
		IEnumerable<FilePair> GetIBFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			return allItems.OfType<ProjectFile> ().Where (pf => pf.BuildAction == BuildAction.Page && pf.FilePath.Extension == ".xib")
				.Select (pf => {
					string[] splits = ((string)pf.RelativePath).Split (Path.DirectorySeparatorChar);
					FilePath name = splits.Last ();
					if (splits.Length > 1 && splits[0].EndsWith (".lproj"))
						name = new FilePath (splits[0]).Combine (name);
					return new FilePair (pf.FilePath, name.ChangeExtension (".nib").ToAbsolute (outputRoot));
				});
		}
		
		IEnumerable<FilePair> GetContentFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			return allItems.OfType<ProjectFile> ().Where (pf => pf.BuildAction == BuildAction.Content)
				.Select (pf => new FilePair (pf.FilePath, pf.RelativePath.ToAbsolute (outputRoot)));
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
