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
using System.Security.Cryptography.X509Certificates;

namespace MonoDevelop.IPhone
{
	
	public class IPhoneBuildExtension : ProjectServiceExtension
	{
		
		public IPhoneBuildExtension ()
		{
		}
		
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return base.Build (monitor, item, configuration);
			
			//prebuild
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			bool isDevice = conf.Platform == IPhoneProject.PLAT_IPHONE;
			
			if (!IPhoneFramework.SdkIsInstalled (conf.MtouchSdkVersion)) {
				var r = new BuildResult ();
				r.AddError (string.Format ("Apple iPhone SDK for target OS version '{0}' is not installed", conf.MtouchSdkVersion));
				return r;
			}
			
			if (IPhoneFramework.SimOnly && isDevice) {
				//if in the GUI, show a dialog too
				if (MonoDevelop.Ide.IdeApp.IsInitialized)
					Gtk.Application.Invoke (delegate { IPhoneFramework.ShowSimOnlyDialog (); } );
				return IPhoneFramework.GetSimOnlyError ();
			}
			
			BuildResult result = null;
			IPhoneAppIdentity identity = null;
			if (isDevice) {
				monitor.BeginTask (GettextCatalog.GetString ("Detecting signing identity..."), 0);
				if ((result = GetIdentity (monitor, proj, conf, out identity)).ErrorCount > 0)
					return result;
				monitor.Log.WriteLine ("Provisioning profile: \"{0}\" ({1})", identity.Profile.Name, identity.Profile.Uuid);
				monitor.Log.WriteLine ("Signing Identity: \"{0}\"", Keychain.GetCertificateCommonName (identity.SigningKey));
				monitor.Log.WriteLine ("App ID: \"{0}\"", identity.AppID);
				monitor.EndTask ();
			} else {
				identity = new IPhoneAppIdentity () {
					BundleID = !string.IsNullOrEmpty (proj.BundleIdentifier)?
						proj.BundleIdentifier : GetDefaultBundleID (proj, null)
				};
			}
			
			result = base.Build (monitor, item, configuration).Append (result);
			if (result.ErrorCount > 0)
				return result;
			
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
				
				foreach (string asm in proj.GetReferencedAssemblies (configuration).Distinct ())
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
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ())) {
				try {
					monitor.BeginTask (GettextCatalog.GetString ("Updating application manifest"), 0);
					if (result.Append (UpdateInfoPlist (monitor, proj, conf, identity, appInfoIn, plistOut)).ErrorCount > 0)
						return result;
				} finally {
					monitor.EndTask ();
				}
			}
			
			//create the Setting.bundle plist for debug settings, merging in the template if it exists
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Updating debug settings manifest"), 0);
				var sbRootRel = Path.Combine ("Settings.bundle", "Root.plist");
				var sbRootOut = conf.AppDirectory.Combine (sbRootRel);
				var sbRootIn  = proj.Files.GetFile (proj.BaseDirectory.Combine (sbRootRel));
				if (result.Append (UpdateDebugSettingsPlist (monitor, conf, sbRootIn, sbRootOut)).ErrorCount > 0)
					return result;
			} finally {
				monitor.EndTask ();
			}
			
			try {
				if (result.Append (ProcessPackaging (monitor, proj, conf, identity)).ErrorCount > 0)
					return result;
			} finally {
				//if packaging failed, make sure that it's marked as needing buildind
				if (result.ErrorCount > 0 && File.Exists (conf.AppDirectory.Combine ("PkgInfo")))
					File.Delete (conf.AppDirectory.Combine ("PkgInfo"));	
			}	
			
			//TODO: create/update the xcode project
			return result;
		}

		static internal void AppendExtrasMtouchArgs (StringBuilder args, IPhoneProject proj, IPhoneProjectConfiguration conf)
		{
			if (conf.MtouchDebug)
				args.Append (" -debug");
			
			switch (conf.MtouchLink) {
			case MtouchLinkMode.SdkOnly:
				args.Append (" -linksdkonly");
				break;
			case MtouchLinkMode.None:
				args.Append (" -nolink");
				break;
			case MtouchLinkMode.Full:
			default:
				break;
			}
			
			if (!string.IsNullOrEmpty (conf.MtouchI18n)) {
				args.Append (" -i18n=");
				args.Append (conf.MtouchI18n);
			}
			
			if (conf.MtouchSdkVersion != "3.0")
				args.AppendFormat (" -sdk=\"{0}\"", conf.MtouchSdkVersion);
			
			AppendExtraArgs (args, conf.MtouchExtraArgs, proj, conf);
		}
		
		static void AppendExtraArgs (StringBuilder args, string extraArgs, IPhoneProject proj,
		                             IPhoneProjectConfiguration conf)
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
		
		BuildResult UpdateInfoPlist (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf,
		                             IPhoneAppIdentity identity, ProjectFile template, string plistOut)
		{
			return CreateMergedPlist (monitor, conf, template, plistOut, 
				(IPhoneProjectConfiguration config, PlistDocument doc) => 
			{
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
					SetIfNotPresent (dict, "CFBundleIconFile", icon.FileName);
				
				SetIfNotPresent (dict, "CFBundleIdentifier", identity.BundleID);
				SetIfNotPresent (dict, "CFBundleInfoDictionaryVersion", "6.0");
				SetIfNotPresent (dict, "CFBundleName", proj.Name);
				SetIfNotPresent (dict, "CFBundlePackageType", "APPL");
				if (!sim)
					dict["CFBundleResourceSpecification"] = "ResourceRules.plist";
				SetIfNotPresent (dict, "CFBundleSignature", "????");
				SetIfNotPresent (dict,  "CFBundleSupportedPlatforms",
					new PropertyList.PlistArray () { sim? "iPhoneSimulator" : "iPhoneOS" });
				SetIfNotPresent (dict, "CFBundleVersion", proj.BundleVersion ?? "1.0");
				SetIfNotPresent (dict, "DTPlatformName", sim? "iphonesimulator" : "iphoneos");
				SetIfNotPresent (dict, "DTSDKName", (sim? "iphonesimulator" : "iphoneos")  + conf.MtouchSdkVersion);
				SetIfNotPresent (dict,  "LSRequiresIPhoneOS", true);
				if (proj.SupportedDevices != TargetDevice.IPhone)
					SetIfNotPresent (dict,  "UIDeviceFamily", GetSupportedDevices (proj.SupportedDevices));
				SetIfNotPresent (dict, "DTPlatformVersion", conf.MtouchSdkVersion);
				
				if (!sim)
					//FIXME allow user to choose version?
					SetIfNotPresent (dict, "MinimumOSVersion", conf.MtouchSdkVersion);
				
				SetNibProperty (dict, proj, proj.MainNibFile, "NSMainNibFile");
				if (proj.SupportedDevices == TargetDevice.IPhoneAndIPad)
					SetNibProperty (dict, proj, proj.MainNibFileIPad, "NSMainNibFile~ipad");
				
				return null;
			});
		}
		
		static void SetNibProperty (PlistDictionary dict, IPhoneProject proj, FilePath mainNibProp, string propName)
		{
			if (!mainNibProp.IsNullOrEmpty) {
				string mainNib = mainNibProp.ToRelative (proj.BaseDirectory);
				if (mainNib.EndsWith (".nib") || mainNib.EndsWith (".xib"))
				    mainNib = mainNib.Substring (0, mainNib.Length - 4).Replace ('\\', '/');
				SetIfNotPresent (dict, propName, mainNib);
			};
		}
		
		static PlistArray GetSupportedDevices (TargetDevice devices)
		{
			switch (devices) {
			case TargetDevice.IPhone:
				return new PlistArray (new int[] { 1 });
			case TargetDevice.IPad:
				return new PlistArray (new int[] { 2 });
			case TargetDevice.IPhoneAndIPad:
				return new PlistArray (new int[] { 1, 2 });
			default:
				LoggingService.LogError ("Bad TargetDevice value {0}", devices);
				goto case TargetDevice.IPhoneAndIPad;
			}
		}
		
		static void SetIfNotPresent (PlistDictionary dict, string key, PlistObjectBase value)
		{
			if (!dict.ContainsKey (key))
				dict[key] = value;
		}
		
		internal ProcessStartInfo GetMTouch (IPhoneProject project, IProgressMonitor monitor, out BuildResult error)
		{
			return GetTool ("mtouch", project, monitor, out error);
		}
		
		internal ProcessStartInfo GetTool (string tool, IPhoneProject project, IProgressMonitor monitor,
		                                   out BuildResult error)
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
		
		protected override bool GetNeedsBuilding (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			if (base.GetNeedsBuilding (item, configuration))
				return true;
			
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return false;
			
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			
			if (!Directory.Exists (conf.AppDirectory))
				return true;
			
			if (!Directory.Exists (conf.AppDirectory.Combine ("PkgInfo")))
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
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
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
				bool forceRegen = !Directory.Exists (cfg.AppDirectory);
				result = CodeBehind.UpdateXibCodebehind (cbWriter, proj, buildData.Items.OfType<ProjectFile> (), forceRegen);
				cbWriter.WriteOpenFiles ();
				if (cbWriter.WrittenCount > 0)
					monitor.Log.WriteLine (GettextCatalog.GetString ("Updated {0} CodeBehind files", cbWriter.WrittenCount));
			} else {
				monitor.ReportWarning ("Cannot generate designer code, because CodeDom " +
					"provider does not support partial classes.");
			}
			monitor.EndTask ();
			
			if (base.GetNeedsBuilding (item, buildData.ConfigurationSelector)) {
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
					var psi = new ProcessStartInfo ("ibtool",
						String.Format ("\"{0}\" --compile \"{1}\"", file.Input, file.Output)
					);
					monitor.Log.WriteLine (psi.FileName + " " + psi.Arguments);
					psi.WorkingDirectory = cfg.OutputDirectory;
					string errorOutput;
					int code;
					try {
					code = ExecuteCommand (monitor, psi, out errorOutput);
					} catch (System.ComponentModel.Win32Exception ex) {
						LoggingService.LogError ("Error running ibtool", ex);
						result.AddError (null, 0, 0, null, "ibtool not found. Please ensure the Apple SDK is installed.");
						return result;
					}
					if (code != 0) {
						//FIXME: parse the plist that ibtool returns
						result.AddError (null, 0, 0, null, "ibtool returned error code " + code);
					}
				}
				monitor.EndTask ();
			}
			
			var contentFiles = GetContentFilePairs (buildData.Items.OfType<ProjectFile> (), appDir)
				.Where (NeedsBuilding).ToList ();
			
			if (!proj.BundleIcon.IsNullOrEmpty) {
				FilePair icon = new FilePair (proj.BundleIcon, cfg.AppDirectory.Combine (proj.BundleIcon.FileName));
				if (!File.Exists (proj.BundleIcon)) {
					result.AddError (null, 0, 0, null, String.Format ("Application icon '{0}' is missing.", proj.BundleIcon));
					return result;
				} else if (icon.NeedsBuilding ()) {
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
		
		static BuildResult ProcessPackaging (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf,
		                                     IPhoneAppIdentity identity)
		{
			//don't bother signing in the sim
			bool isDevice = conf.Platform == IPhoneProject.PLAT_IPHONE;
			if (!isDevice)
				return null;
			
			BuildResult result = new BuildResult ();
			
			var pkgInfo = conf.AppDirectory.Combine ("PkgInfo");
			if (!File.Exists (pkgInfo))
				using (var f = File.OpenWrite (pkgInfo))
					f.Write (new byte [] { 0X41, 0X50, 0X50, 0X4C, 0x3f, 0x3f, 0x3f, 0x3f}, 0, 8);
			
			if (result.Append (CompressResources (monitor, conf)).ErrorCount > 0)
				return result;
			
			if (result.Append (EmbedProvisioningProfile (monitor, conf, identity.Profile)).ErrorCount > 0)
				return result;
			
			string xcent;
			if (result.Append (GenXcent (monitor, proj, conf, identity, out xcent)).ErrorCount > 0)
				return result;
			
			string resRules;
			if (result.Append (PrepareResourceRules (monitor, conf, out resRules)).ErrorCount > 0)
				return result;
			
			if (result.Append (SignAppBundle (monitor, proj, conf, identity.SigningKey, resRules, xcent)).ErrorCount > 0)
				return result;
			
			return result;
		}
		
		static BuildResult CompressResources (IProgressMonitor monitor, IPhoneProjectConfiguration conf)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Compressing resources"), 0);
			
			var optTool = new ProcessStartInfo () {
				FileName = "/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/iphoneos-optimize",
				Arguments = conf.AppDirectory,
			};
			
			monitor.Log.WriteLine (optTool.FileName + " " + optTool.Arguments);
			string errorOutput;
			int code = ExecuteCommand (monitor, optTool, out errorOutput);
			if (code != 0) {
				var result = new BuildResult ();
				result.AddError ("Compressing the resources failed: " + errorOutput);
				return result;
			}
			
			monitor.EndTask ();
			
			return null;
		}
		
		static BuildResult GetIdentity (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf,
		                                out IPhoneAppIdentity identity)
		{
			var result = new BuildResult ();
			identity = new IPhoneAppIdentity ();
			bool defaultID = string.IsNullOrEmpty (proj.BundleIdentifier);
			
			if (!defaultID)
				identity.BundleID = proj.BundleIdentifier;
			
			//treat empty as "developer automatic"
			if (string.IsNullOrEmpty (conf.CodesignKey)) {
				conf.CodesignKey = Keychain.DEV_CERT_PREFIX;
			}
			
			IList<X509Certificate2> certs = null;
			if (conf.CodesignKey == Keychain.DEV_CERT_PREFIX || conf.CodesignKey == Keychain.DIST_CERT_PREFIX) {
				certs = Keychain.FindNamedSigningCertificates (x => x.StartsWith (conf.CodesignKey)).ToList ();
				if (certs.Count == 0) {
					result.AddError ("No valid iPhone code signing keys found in keychain.");
					return result;
				}
			} else {
				identity.SigningKey = Keychain.FindNamedSigningCertificates (x => x == conf.CodesignKey).FirstOrDefault ();
				if (identity.SigningKey == null) {
					result.AddError (string.Format ("iPhone code signing key '{0}' not found in keychain.", conf.CodesignKey));
					return result;
				}
				certs = new X509Certificate2[] { identity.SigningKey };
			}
			
			if (!string.IsNullOrEmpty (conf.CodesignProvision)) {
				//if the profile was installed by Xcode, we can determine the filename directly from the UUID
				//but if it was installed by iTunes, we need to search all profiles for the UUID.
				var file = MobileProvision.ProfileDirectory.Combine (conf.CodesignProvision).ChangeExtension (".mobileprovision");
				if (File.Exists (file)) {
					try {
						identity.Profile = MobileProvision.LoadFromFile (file);
					} catch (Exception ex) {
						string msg = "Could not read provisioning profile '" + file + "'.";
						monitor.ReportError (msg, ex);
						result.AddError (msg);
						return result;
					}
				} else {
					identity.Profile = MobileProvision.GetAllInstalledProvisions ()
						.Where (p => p.Uuid == conf.CodesignProvision).FirstOrDefault ();
				}
				
				if (identity.Profile == null) {
					result.AddError (string.Format ("The specified provisioning profile '{0}' could not be found", conf.CodesignProvision));
					return result;
				}
				
				var prof = identity.Profile; //capture ref for lambda
				identity.SigningKey = certs.Where (c => prof.DeveloperCertificates
				                           .Any (p => p.Thumbprint == c.Thumbprint)).FirstOrDefault ();
				if (identity.SigningKey == null) {
					result.AddError (string.Format ("No iPhone code signing key matches specified provisioning profile '{0}'.", conf.CodesignProvision));
					return result;
				}
				
				if (defaultID) {
					identity.BundleID = GetDefaultBundleID (proj, GetProfileBundleID (identity.Profile));
					result.AddWarning (string.Format ("Project does not have bundle identifier specified. Generated '{0}' to match provisioning profile.", identity.BundleID));
				}
				
				bool exact;
				identity.AppID = ConstructValidAppId (identity.Profile, identity.BundleID, out exact);
				if (identity.AppID == null) {
					result.AddError (string.Format (
						"Project bundle ID '{0}' does not match specified provisioning profile '{1}'", identity.BundleID, conf.CodesignProvision));
					return result;
				}
				return result;
			}
			
			var pairs = (from p in MobileProvision.GetAllInstalledProvisions ()
				from c in certs
				where p.DeveloperCertificates.Any (d => d.Thumbprint == c.Thumbprint)
				select new { Cert = c, Profile = p }).ToList ();
				
			if (pairs.Count == 0) {
				result.AddError ("No installed provisioning profiles match the installed iPhone code signing keys.");
				return result;
			}
			
			if (!defaultID) {
				//find a provisioning profile with compatible appid, preferring exact match
				foreach (var p in pairs) {
					bool exact;
					var id = ConstructValidAppId (p.Profile, identity.BundleID, out exact);
					if (id != null) {
						if (exact || identity.AppID == null) {
							identity.Profile = p.Profile;
							identity.SigningKey = p.Cert;
							identity.AppID = id;
						}
						if (exact)
							break;
					}
				}
			} else {
				//pick provisioning profile to provide appid and better default bundle ID, preferring star bundle IDs
				foreach (var p in pairs) {
					var suggestion = GetProfileBundleID (p.Profile);
					bool star = (suggestion != null) && suggestion.EndsWith ("*");
					if (star || identity.Profile == null) {
						identity.Profile = p.Profile;
						identity.SigningKey = p.Cert;
						identity.BundleID = GetDefaultBundleID (proj, suggestion);
						bool exact;
						identity.AppID = ConstructValidAppId (p.Profile, identity.BundleID, out exact);
					}
					if (star)
						break;
				}
				result.AddWarning (string.Format ("Project does not have bundle identifier specified. Generated '{0}' to match an installed provisioning profile.", identity.BundleID));
			}
			
			if (identity.Profile != null && identity.SigningKey != null && identity.AppID != null)
				return result;
			
			if (identity.SigningKey != null) {
				result.AddError (string.Format (
					"Bundle identifier '{0}' does not match any installed provisioning profile for selected signing identity '{0}'.",
					identity.BundleID, identity.SigningKey));
			} else {
				result.AddError (string.Format (
					"Bundle identifier '{0}' does not match any installed provisioning profile.",
					identity.BundleID));
			}
			return result;
		}
		
		class IPhoneAppIdentity
		{
			public MobileProvision Profile { get; set; }
			public string AppID { get; set; }
			public string BundleID { get; set; }
			public X509Certificate2 SigningKey { get; set; }
			public bool DefaultID { get; set; }
		}
		
		static string GetDefaultBundleID (IPhoneProject project, string suggestion)
		{
			if (string.IsNullOrEmpty (suggestion)) {
				return "com.yourcompany." + GetFilteredProjectName (project);
			} else if (suggestion.EndsWith ("*")) {
				return suggestion.Substring (0, suggestion.Length - 1) + GetFilteredProjectName (project);
			} else {
				return suggestion;
			}
		}
		
		static string GetFilteredProjectName (IPhoneProject project)
		{
			var sb = new StringBuilder ();
			foreach (char c in project.Name)
				if (char.IsLetterOrDigit (c))
					sb.Append (c);
			return sb.Length > 0? sb.ToString ().ToLowerInvariant () : "application";
		}
		
		static BuildResult EmbedProvisioningProfile (IProgressMonitor monitor, IPhoneProjectConfiguration conf, MobileProvision profile)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Embedding provisioning profile"), 0);
			
			try {
				File.Copy (profile.FileName, conf.AppDirectory.Combine ("embedded.mobileprovision"), true);
			} catch (IOException ex) {
				var result = new BuildResult ();
				result.AddError ("Embedding the provisioning profile failed: " + ex.Message);
				return result;
			}
			
			monitor.EndTask ();
			return null;
		}
		
		static string GetProfileBundleID (MobileProvision provision)
		{
			if (!provision.Entitlements.ContainsKey ("application-identifier"))
				return null;
			
			var id = ((PlistString)provision.Entitlements ["application-identifier"]).Value;
			int i = id.IndexOf ('.') + 1;
			if (i > 0 && i < id.Length)
				return id.Substring (i);
			return null;
		}
		
		static string ConstructValidAppId (MobileProvision provision, string bundleId, out bool exact)
		{
			exact = false;
			
			string appid = provision.ApplicationIdentifierPrefix[0] + "." + bundleId;
			
			if (!provision.Entitlements.ContainsKey ("application-identifier"))
				return null;
			
			var allowed = ((PlistString)provision.Entitlements ["application-identifier"]).Value;
			int max = Math.Max (allowed.Length, appid.Length);
			for (int i = 0; i < max; i++) {
				if (i < allowed.Length && allowed[i] == '*')
					break;
				if (i >= appid.Length || allowed[i] != appid[i])
					return null;
			}
			exact = (allowed.Length == appid.Length) && allowed[allowed.Length -1] != '*';
			return appid;
		}
		
		static BuildResult GenXcent (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf,
		                      IPhoneAppIdentity identity, out string xcentName)
		{
			xcentName = conf.CompiledOutputName.ChangeExtension (".xcent");
			
			monitor.BeginTask (GettextCatalog.GetString ("Processing entitlements file"), 0);
			
			string srcFile;
			
			if (!string.IsNullOrEmpty (conf.CodesignEntitlements)) {
				if (!File.Exists (conf.CodesignEntitlements))
					return BuildError ("Entitlements file \"" + conf.CodesignEntitlements + "\" not found.");
				srcFile = conf.CodesignEntitlements;
			} else {
				srcFile = "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS" + conf.MtouchSdkVersion
					+ ".sdk/Entitlements.plist";
			}
			
			var doc = new PlistDocument ();
			try {
				doc.LoadFromXmlFile (srcFile);
			} catch (Exception ex) {
				monitor.Log.WriteLine (ex.ToString ());
				return BuildError ("Error loading entitlements source file '" + srcFile +"'.");
			}
			
			//insert the app ID into the plist at the beginning
			var oldDict = doc.Root as PlistDictionary;
			var newDict = new PropertyList.PlistDictionary ();
			doc.Root = newDict;
			newDict["application-identifier"] = identity.AppID;
			
			//merge in the settings from the provisioning profile
			foreach (var item in identity.Profile.Entitlements)
				if (item.Key != "application-identifier" && item.Key != "keychain-access-groups")
					newDict.Add (item.Key, item.Value);
			
			//and merge in the user's values
			foreach (var item in oldDict) {
				if (newDict.ContainsKey (item.Key))
					newDict[item.Key] = item.Value;
				else
					newDict.Add (item.Key, item.Value);
			}
			
			try {
				WriteXcent (doc, xcentName);
			} catch (Exception ex) {
				monitor.Log.WriteLine (ex.ToString ());
				return BuildError ("Error writing entitlements file '" + xcentName +"'.");
			}
			
			monitor.EndTask ();
			return null;
		}
		
		static void WriteXcent (PlistDocument doc, string file)
		{
			//write the plist to a byte[] as UTF8 without a BOM
			var ms = new MemoryStream ();
			var xmlSettings = new XmlWriterSettings () {
				Encoding = new UTF8Encoding (false), //no BOM
				CloseOutput = false,
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\n",
			};
			using (var writer = XmlTextWriter.Create (ms, xmlSettings))
				doc.Write (writer);
			
			//HACK: workaround for bug in Apple's entitlements XML parser
			//having written to a UTF8 stream to convince the xmlwriter to do the right thing,
			//we now convert to string and back to do some substitutions to work around bugs
			//in Apple's braindead entitlements XML parser.
			//Specifically, it chokes on "<true />" but accepts "<true">
			//Hence, to be on the safe side, we produce EXACTLY the same format
			var sb = new StringBuilder (Encoding.UTF8.GetString (ms.GetBuffer ()));
			sb.Replace ("-//Apple Computer//DTD PLIST 1.0//EN", "-//Apple//DTD PLIST 1.0//EN");
			sb.Replace ("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			sb.Replace ("\n\t", "\n");
			sb.Replace (" />\n", "/>\n");
			sb.Append ("\n");
			var buf = Encoding.UTF8.GetBytes (sb.ToString ());
			
			//write the xcent file with the magic header, length, and the plist
			byte[] magic = new byte[] { 0xfa, 0xde, 0x71, 0x71 };
			byte[] fileLen = Mono.DataConverter.BigEndian.GetBytes ((uint)buf.Length + 8); // 8 = magic.length + magicLen.Length
			using (var fs = File.Open (file, FileMode.Create)) {
				fs.Write (magic, 0, magic.Length);
				fs.Write (fileLen, 0, fileLen.Length);
				fs.Write (buf, 0, (int)buf.Length);
			}
		}
		
		static BuildResult PrepareResourceRules (IProgressMonitor monitor, IPhoneProjectConfiguration conf, out string resRulesFile)
		{
			resRulesFile = conf.AppDirectory.Combine ("ResourceRules.plist");
			
			monitor.BeginTask (GettextCatalog.GetString ("Preparing resources rules"), 0);
			
			if (File.Exists (resRulesFile))
				File.Delete (resRulesFile);
			
			string resRulesSrc = String.IsNullOrEmpty (conf.CodesignResourceRules)
				? "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS"
					+ conf.MtouchSdkVersion + ".sdk/ResourceRules.plist"
				: (string) conf.CodesignResourceRules;
			if (File.Exists (resRulesSrc)) {
				File.Copy (resRulesSrc, resRulesFile, true);
			} else {
				return BuildError ("Resources rules file \"" + conf.CodesignResourceRules + "\" not found.");
			}
			
			monitor.EndTask ();
			return null;
		}
		
		static BuildResult SignAppBundle (IProgressMonitor monitor, IPhoneProject proj, IPhoneProjectConfiguration conf,
		                           X509Certificate2 key, string resRules, string xcent)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Signing application"), 0);
			
			var args = new StringBuilder ();
			args.AppendFormat ("-v -f -s \"{0}\"", Keychain.GetCertificateCommonName (key));
			args.AppendFormat (" --resource-rules=\"{0}\" --entitlements \"{1}\"", resRules, xcent);
			
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
			
			monitor.Log.WriteLine ("codesign " + psi.Arguments);
			psi.EnvironmentVariables.Add ("CODESIGN_ALLOCATE",
				"/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/codesign_allocate");
			string output;
			if ((signResultCode = ExecuteCommand (monitor, psi, out output)) != 0) {
				monitor.Log.WriteLine (output);
				return BuildError (string.Format ("Code signing failed with error code {0}. See output for details.", signResultCode));
			}
			monitor.EndTask ();
			
			return null;
		}
		
		static BuildResult BuildError (string error)
		{
			var br = new BuildResult ();
			br.AddError (error);
			return br;
		}
		
		static IEnumerable<FilePair> GetIBFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			return allItems.OfType<ProjectFile> ()
				.Where (pf => pf.BuildAction == BuildAction.Page && pf.FilePath.Extension == ".xib")
				.Select (pf => {
					string[] splits = ((string)pf.ProjectVirtualPath).Split (Path.DirectorySeparatorChar);
					FilePath name = splits.Last ();
					if (splits.Length > 1 && splits[0].EndsWith (".lproj"))
						name = new FilePath (splits[0]).Combine (name);
					return new FilePair (pf.FilePath, name.ChangeExtension (".nib").ToAbsolute (outputRoot));
				});
		}
		
		static IEnumerable<FilePair> GetContentFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			//these are filenames that could overwrite important packaging files
			//FIXME: warn if the user has marked these as content, or just ignore?
			//FIXME: check for _CodeResources and dlls and binaries too?
			var forbiddenNames = new HashSet<string> (new [] {
				"Info.plist",
				"Embedded.mobileprovision",
				"ResourceRules.plist",
				"PkgInfo",
				"CodeResources"
			}, StringComparer.OrdinalIgnoreCase);
			
			return allItems.OfType<ProjectFile> ()
				.Where (pf => pf.BuildAction == BuildAction.Content && !forbiddenNames.Contains (pf.ProjectVirtualPath))
				.Select (pf => new FilePair (pf.FilePath, pf.ProjectVirtualPath.ToAbsolute (outputRoot)));
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
		static int ExecuteCommand (IProgressMonitor monitor, System.Diagnostics.ProcessStartInfo startInfo, out string errorOutput)
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
		
		static BuildResult CreateMergedPlist (IProgressMonitor monitor, IPhoneProjectConfiguration conf,
			ProjectFile template, string outPath,
			Func<IPhoneProjectConfiguration, PlistDocument,BuildResult> merge)
		{
			var result = new BuildResult ();
			
			var doc = new PlistDocument ();
			if (template != null) {
				try {
					doc.LoadFromXmlFile (template.FilePath);
				} catch (Exception ex) {
					if (ex is XmlException)
						result.AddError (template.FilePath, ((XmlException)ex).LineNumber,
						                 ((XmlException)ex).LinePosition, null, ex.Message);
					else
						result.AddError (template.FilePath, 0, 0, null, ex.Message);
					monitor.ReportError (GettextCatalog.GetString ("Could not load file '{0}': {1}",
					                                               template.FilePath, ex.Message), null);
					return result;
				}
			}
			
			if (result.Append (merge (conf, doc)).ErrorCount > 0)
				return result;
			
			try {
				EnsureDirectoryForFile (outPath);
				using (XmlTextWriter writer = new XmlTextWriter (outPath, Encoding.UTF8)) {
					writer.Formatting = Formatting.Indented;
					doc.Write (writer);
				}
			} catch (Exception ex) {
				result.AddError (outPath, 0, 0, null, ex.Message);
				monitor.ReportError (GettextCatalog.GetString ("Could not write file '{0}'", outPath), ex);
			}
			return result;
		}
		
		static void EnsureDirectoryForFile (string filename)
		{
			string dir = Path.GetDirectoryName (filename);
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
		}
		
		static BuildResult UpdateDebugSettingsPlist (IProgressMonitor monitor, IPhoneProjectConfiguration conf,
		                                             ProjectFile template, string target)
		{
			if (template != null && template.BuildAction != BuildAction.Content)
				template = null;
			
			//if not in debug mode, make sure that the settings file is either
			//copied cleanly or deleted
			if (!conf.DebugMode) {
				if (template != null) {
					EnsureDirectoryForFile (target);
					File.Copy (template.FilePath, target, true);
				} else if (File.Exists (target)) {
					File.Delete (target);
				}
				return null;
			}
			
			return CreateMergedPlist (monitor, conf, template, target,
				(IPhoneProjectConfiguration config, PlistDocument doc) =>
			{
				var br = new BuildResult ();
				var debuggerIP = System.Net.IPAddress.Any;
				bool sim = config.Platform == IPhoneProject.PLAT_SIM;
				
				try {
					debuggerIP = GetDebuggerIP (sim);
				} catch {
					br.AddWarning (GettextCatalog.GetString ("Could not resolve host IP for debugger settings"));
				}
				
				var dict = doc.Root as PlistDictionary;
				if (dict == null)
					doc.Root = dict = new PropertyList.PlistDictionary ();
				
				SetIfNotPresent (dict, "Title", "AppSettings");
				SetIfNotPresent (dict, "StringsTable", "Root");
				
				var arr = dict.TryGetValue ("PreferenceSpecifiers") as PlistArray;
				if (arr == null)
					dict["PreferenceSpecifiers"] = arr = new PlistArray ();
				
				arr.Add (new PlistDictionary (true) {
					{ "Type", "PSGroupSpecifier" },
					{ "Title", "Debug Settings" }
				});
				
				arr.Add (new PlistDictionary (true) {
					{ "Type", "PSToggleSwitchSpecifier" },
					{ "Title", "Enabled" },
					{ "Key", "__monotouch_debug_enabled" },
					{ "DefaultValue", "1" },
					{ "TrueValue", "1" },
					{ "FalseValue", "0" }
				});
				
				arr.Add (new PlistDictionary (true) {
					{ "Type", "PSTextFieldSpecifier" },
					{ "Title", "Debugger Host" },
					{ "Key", "__monotouch_debug_host" },
					{ "AutocapitalizationType", "None" },
					{ "AutocorrectionType", "No" },
					{ "DefaultValue", debuggerIP.ToString () }
				});
					
				arr.Add (new PlistDictionary (true) {
					{ "Type", "PSTextFieldSpecifier" },
					{ "Title", "Debugger Port" },
					{ "Key", "__monotouch_debug_port" },
					{ "AutocapitalizationType", "None" },
					{ "AutocorrectionType", "No" },
					{ "DefaultValue", DebuggerPort.ToString () }
				});
					
				arr.Add (new PlistDictionary (true) {
					{ "Type", "PSTextFieldSpecifier" },
					{ "Title", "Output Port" },
					{ "Key", "__monotouch_output_port" },
					{ "AutocapitalizationType", "None" },
					{ "AutocorrectionType", "No" },
					{ "DefaultValue", DebuggerOutputPort.ToString () }
				});
				
				return br;
			});
		}
		
		public static System.Net.IPAddress GetDebuggerIP (bool simulator)
		{
			if (simulator)
				return System.Net.IPAddress.Loopback;
			
			var ipStr = MonoDevelop.Core.PropertyService.Get<string> ("MonoTouch.Debugger.HostIP", "");
			try {
				if (!string.IsNullOrEmpty (ipStr))
					return System.Net.IPAddress.Parse (ipStr);
			} catch (Exception e) {
				LoggingService.LogInfo ("Error parsing Debugger HostIP: {0}: {1}", ipStr, e);
			}
			
			return System.Net.Dns.GetHostEntry (System.Net.Dns.GetHostName ()).AddressList[0];
		}
		
		public static int DebuggerPort {
			get {
				return MonoDevelop.Core.PropertyService.Get<int> ("MonoTouch.Debugger.Port", 10000);
			}
		}
		
		public static int DebuggerOutputPort {
			get {
				return MonoDevelop.Core.PropertyService.Get<int> ("MonoTouch.Debugger.OutputPort", 10001);
			}
		}
	}
}
