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
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using System.Xml;
using System.Text;
using System.Diagnostics;
using MonoDevelop.MacDev.Plist;
using System.CodeDom.Compiler;
using System.Security.Cryptography.X509Certificates;
using MonoDevelop.MacDev;

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
			
			IPhoneFramework.CheckInfoCaches ();
			
			//prebuild
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			bool isDevice = conf.Platform == IPhoneProject.PLAT_IPHONE;
			
			if (IPhoneFramework.SimOnly && isDevice) {
				//if in the GUI, show a dialog too
				if (MonoDevelop.Ide.IdeApp.IsInitialized)
					Gtk.Application.Invoke (delegate { IPhoneFramework.ShowSimOnlyDialog (); } );
				return IPhoneFramework.GetSimOnlyError ();
			}
			
			var result = new BuildResult ();
			
			var sdkVersion = conf.MtouchSdkVersion.ResolveIfDefault ();
			
			if (!IPhoneFramework.SdkIsInstalled (sdkVersion)) {
				sdkVersion = IPhoneFramework.GetClosestInstalledSdk (sdkVersion);
				
				if (sdkVersion.IsUseDefault || !IPhoneFramework.SdkIsInstalled (sdkVersion)) {
					if (conf.MtouchSdkVersion.IsUseDefault)
						result.AddError (
							string.Format ("The Apple iPhone SDK is not installed."));
					else
						result.AddError (
							string.Format ("Apple iPhone SDK version '{0}' is not installed, and no newer version was found.",
							conf.MtouchSdkVersion));
					return result;
				}
					
				result.AddWarning (
					string.Format ("Apple iPhone SDK version '{0}' is not installed. Using newer version '{1}' instead'.",
					conf.MtouchSdkVersion, sdkVersion));
			}
			
			IPhoneAppIdentity identity = null;
			if (isDevice) {
				monitor.BeginTask (GettextCatalog.GetString ("Detecting signing identity..."), 0);
				if ((result = GetIdentity (monitor, proj, conf, out identity).Append (result)).ErrorCount > 0)
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
			
			var assemblyRefs = proj.GetReferencedAssemblies (configuration).Distinct ().ToList ();
			
			FilePath mtouchOutput = conf.NativeExe;
			if (new FilePair (conf.CompiledOutputName, mtouchOutput).NeedsBuilding ()) {
				BuildResult error;
				var mtouch = GetMTouch (proj, monitor, out error);
				if (error != null)
					return error.Append (result);
				
				var args = new ProcessArgumentBuilder ();
				//FIXME: make verbosity configurable?
				args.Add ("-v");
				
				args.Add ("--nomanifest", "--nosign");
					
				//FIXME: should we error out if the platform is invalid?
				if (conf.Platform == IPhoneProject.PLAT_IPHONE) {
					args.Add ("-dev");
					args.AddQuoted (conf.AppDirectory);
				} else {
					args.Add ("-sim");
					args.AddQuoted (conf.AppDirectory);
				}
				
				foreach (string asm in assemblyRefs)
					args.AddQuotedFormat ("-r={0}", asm);
				
				IPhoneSdkVersion osVersion = IPhoneSdkVersion.V3_0;
				try {
					osVersion = IPhoneSdkVersion.Parse (conf.MtouchMinimumOSVersion);
				} catch {
					result.AddWarning ("Could not parse minimum OS version '" + conf.MtouchMinimumOSVersion + "'");
				}
				
				if (osVersion < IPhoneSdkVersion.V3_0 && conf.MtouchArch == MtouchArch.ARMv7) {
					result.AddError ("Apps with a minimum OS older than 3.1 cannot be ARMv7 only");
					return result;
				}
				
				AppendExtrasMtouchArgs (args, sdkVersion, proj, conf);
				
				args.AddQuoted (conf.CompiledOutputName);
				
				mtouch.WorkingDirectory = conf.OutputDirectory;
				mtouch.Arguments = args.ToString ();
				
				monitor.BeginTask (GettextCatalog.GetString ("Compiling to native code"), 0);
				
				string output;
				int code;
				monitor.Log.WriteLine ("{0} {1}", mtouch.FileName, mtouch.Arguments);
				if ((code = MacBuildUtilities.ExecuteCommand (monitor, mtouch, out output)) != 0) {
					if (String.IsNullOrEmpty (output)) {
						result.AddError (null, 0, 0, code.ToString (), "mtouch failed with no output");
					} else {
						result.AddError (null, 0, 0, code.ToString (), "mtouch failed with the following message:\n" + output);
					}
					return result;
				}
				
				monitor.EndTask ();
			}
			
			//unpack nibs and content from dll resources (MT 4+ only)
			if (IPhoneFramework.MonoTouchVersion >= new IPhoneSdkVersion (3, 99))
				if (result.Append (UnpackContent (monitor, conf, assemblyRefs)).ErrorCount > 0)
					return result;
			
			//create the info.plist, merging in the template if it exists
			var plistOut = conf.AppDirectory.Combine ("Info.plist");
			ProjectFile appInfoIn = proj.Files.GetFile (proj.BaseDirectory.Combine ("Info.plist"));
			if (new FilePair (proj.FileName, plistOut).NeedsBuilding () ||
			    	(appInfoIn != null && new FilePair (appInfoIn.FilePath, plistOut).NeedsBuilding ())) {
				try {
					monitor.BeginTask (GettextCatalog.GetString ("Updating application manifest"), 0);
					if (result.Append (UpdateInfoPlist (monitor, sdkVersion, proj, conf, identity, appInfoIn, plistOut)).ErrorCount > 0)
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
				if (result.Append (ProcessPackaging (monitor, sdkVersion, proj, conf, identity)).ErrorCount > 0)
					return result;
			} finally {
				//if packaging failed, make sure that it's marked as needing building
				if (result.ErrorCount > 0 && File.Exists (conf.AppDirectory.Combine ("PkgInfo")))
					File.Delete (conf.AppDirectory.Combine ("PkgInfo"));	
			}	
			
			//TODO: create/update the xcode project
			return result;
		}
		
		static BuildResult UnpackContent (IProgressMonitor monitor, IPhoneProjectConfiguration cfg, List<string> assemblies)
		{
			bool isDevice = cfg.Platform == IPhoneProject.PLAT_IPHONE;
			
			//remove framework references, they don't contain embedded content
			List<string> toProcess = new List<string> ();
			for (int i = 0; i < assemblies.Count; i++) {
				var asm = assemblies[i];
				if (!asm.StartsWith ("/Developer/MonoTouch/usr/lib/mono/2.1") && asm != "mscorlib")
					toProcess.Add (asm);
			}
			//optimize the case where there are no non-framework references
			if (toProcess.Count == 0)
				return null;
			
			var result = new BuildResult ();
			var appDir = cfg.AppDirectory;
			
			//check that the dlls are also in the app bundle
			for (int i = 0; i < toProcess.Count; i++) {
				var asmInBundle = appDir.Combine (Path.GetFileName (toProcess[i]));
				if (!File.Exists (asmInBundle)) {
					var m = GettextCatalog.GetString ("Library '{0}' missing in app bundle, cannot extract content", asmInBundle.FileName);
					result.AddWarning (m);
					toProcess.RemoveAt (i--);
				}
			}
			
			//determine which dlls have been extracted, so we can skip re-extracting their content
			var previouslyProcessedDlls = new HashSet<string> ();
			var infoFile = cfg.ObjDir.Combine ("monotouch_dll_content_processed");
			bool hasInfoFile = File.Exists (infoFile);
			DateTime infoFileWritten = DateTime.MinValue;
			List<bool> skipExtractList = null;
			if (hasInfoFile) {
				infoFileWritten = File.GetLastWriteTimeUtc (infoFile);
				foreach (var line in File.ReadAllLines (infoFile))
					previouslyProcessedDlls.Add (line);
				
				//remove logged dlls that are no longer in the build
				previouslyProcessedDlls.IntersectWith (toProcess);
				
				//don't re-process dlls that were written before the info file and are in the info file
				skipExtractList = toProcess.Select (asm =>
					!previouslyProcessedDlls.Add (asm)
					&& infoFileWritten > File.GetLastWriteTimeUtc (asm)
				).ToList ();
			} else {
				previouslyProcessedDlls.UnionWith (toProcess);
			}
			
			//early exit if nothing needs to be extracted or stripped
			if (toProcess.Count == 0 || (!isDevice && skipExtractList != null && skipExtractList.All (b => b)))
				return result;
			
			monitor.BeginTask ("Extracting embedded content", 0);
			for (int i = 0; i < toProcess.Count; i++) {
				FilePath asmFile = toProcess[i];
				bool skipExtract = skipExtractList != null && skipExtractList[i];
				if (skipExtract)
					continue;
				try {
					ExtractFromContentAssembly (monitor, appDir, asmFile);
				} catch (Exception ex) {
					string message = string.Format ("Error extracting content from assembly '{0}'", asmFile);
					monitor.ReportError (message, ex);
					result.AddError (message);
					monitor.EndTask ();
					return result;
				}
			}
			monitor.EndTask ();
			
			var dir = infoFile.ParentDirectory;
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
			File.WriteAllLines (infoFile, previouslyProcessedDlls.ToArray ());
			
			return result;
		}
		
		static void ExtractFromContentAssembly (IProgressMonitor monitor, FilePath appDir, FilePath asmInBundle)
		{
			Mono.Cecil.AssemblyDefinition a = Mono.Cecil.AssemblyDefinition.ReadAssembly (asmInBundle);
			foreach (Mono.Cecil.ModuleDefinition m in a.Modules) {
				for (int i = 0; i < m.Resources.Count; i++) {
					var er = m.Resources[i] as Mono.Cecil.EmbeddedResource;
					if (er != null) {
						FilePath sname;
						if (er.Name.StartsWith ("__monotouch_content_")) {
							var s = er.Name.Substring ("__monotouch_content_".Length);
							sname = UnescapeMangledResource (s);
						} else if (er.Name.StartsWith ("__monotouch_page_")) {
							var s = er.Name.Substring ("__monotouch_page_".Length);
							sname = UnescapeMangledResource (s);
						} else {
							continue;
						}
						
						monitor.Log.WriteLine ("Extracted {0} from {1}", sname, asmInBundle.FileName);
							
						var file = sname.ToAbsolute (appDir);
						var parentDir = file.ParentDirectory;
						if (!Directory.Exists (parentDir))
							Directory.CreateDirectory (parentDir);
							
						using (var fileStream = File.OpenWrite (file))
							er.GetResourceStream ().CopyTo (fileStream);
					}
				}
			}
		}
		
		static internal void AppendExtrasMtouchArgs (ProcessArgumentBuilder args, IPhoneSdkVersion sdkVersion, 
			IPhoneProject proj, IPhoneProjectConfiguration conf)
		{
			if (conf.MtouchDebug)
				args.Add ("-debug");
			
			switch (conf.MtouchLink) {
			case MtouchLinkMode.SdkOnly:
				args.Add ("-linksdkonly");
				break;
			case MtouchLinkMode.None:
				args.Add ("-nolink");
				break;
			case MtouchLinkMode.Full:
			default:
				break;
			}
			
			if (!string.IsNullOrEmpty (conf.MtouchI18n)) {
				args.AddQuotedFormat ("-i18n={0}", conf.MtouchI18n);
			}
			
			if (!sdkVersion.Equals (IPhoneSdkVersion.V3_0))
				args.AddQuotedFormat ("-sdk={0}", sdkVersion);
			
			if (conf.MtouchMinimumOSVersion != "3.0")
				args.AddQuotedFormat ("-targetver={0}", conf.MtouchMinimumOSVersion);
			
			if (IPhoneFramework.MonoTouchVersion >= new IPhoneSdkVersion (3, 99)) {
				if (conf.MtouchUseSGen)
					args.Add ("--sgen");
				if (conf.MtouchUseLlvm) {
					args.Add ("--llvm");
					switch (conf.MtouchArch) {
					case MtouchArch.ARMv6_ARMv7:
						args.Add ("--fat");
						break;
					case MtouchArch.ARMv7:
						args.Add ("--armv7");
						break;
					}
					if (conf.MtouchArch != MtouchArch.ARMv6 && conf.MtouchUseThumb)
						args.Add ("--thumb");
				}
			}
			
			AddExtraArgs (args, conf.MtouchExtraArgs, proj, conf);
		}
		
		static void AddExtraArgs (ProcessArgumentBuilder args, string extraArgs, IPhoneProject proj,
			IPhoneProjectConfiguration conf)
		{
			if (!string.IsNullOrEmpty (extraArgs)) {
				var customTags = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase) {
					{ "projectdir",   proj.BaseDirectory },
					{ "solutiondir",  proj.ParentSolution.BaseDirectory },
					{ "appbundledir", conf.AppDirectory },
					{ "targetpath",   conf.CompiledOutputName },
					{ "targetdir",    conf.CompiledOutputName.ParentDirectory },
					{ "targetname",   conf.CompiledOutputName.FileName },
					{ "targetext",    conf.CompiledOutputName.Extension },
				};
				args.Add (StringParserService.Parse (extraArgs, customTags));
			}
		}
		
		BuildResult UpdateInfoPlist (IProgressMonitor monitor, IPhoneSdkVersion sdkVersion, IPhoneProject proj,
			IPhoneProjectConfiguration conf, IPhoneAppIdentity identity, ProjectFile template, string plistOut)
		{
			return MacBuildUtilities.CreateMergedPlist (monitor, template, plistOut, (PlistDocument doc) => {
				var result = new BuildResult ();
				var dict = doc.Root as PlistDictionary;
				if (dict == null)
					doc.Root = dict = new PlistDictionary ();
				
				bool sim = conf.Platform != IPhoneProject.PLAT_IPHONE;
				bool v3_2_orNewer = sdkVersion >= IPhoneSdkVersion.V3_2;
				bool v3_1_orNewer = sdkVersion >= IPhoneSdkVersion.V3_1;
				bool v4_0_orNewer = sdkVersion >= IPhoneSdkVersion.V4_0;
				bool supportsIPhone = (proj.SupportedDevices & TargetDevice.IPhone) != 0;
				bool supportsIPad = (proj.SupportedDevices & TargetDevice.IPad) != 0;
				
				var sdkSettings = IPhoneFramework.GetSdkSettings (sdkVersion);
				var dtSettings = IPhoneFramework.GetDTSettings ();
				
				SetIfNotPresent (dict, "BuildMachineOSBuild", dtSettings.BuildMachineOSBuild);
				
				SetIfNotPresent (dict, "CFBundleDevelopmentRegion",
					String.IsNullOrEmpty (proj.BundleDevelopmentRegion)? "en" : proj.BundleDevelopmentRegion);
				
				SetIfNotPresent (dict, "CFBundleDisplayName", proj.BundleDisplayName ?? proj.Name);
				SetIfNotPresent (dict, "CFBundleExecutable", conf.NativeExe.FileName);
				
				// < 3.2 icon
				if (supportsIPhone) {
					if (!dict.ContainsKey ("CFBundleIconFile")) {
						var icon = proj.BundleIcon.ToRelative (proj.BaseDirectory);
						if (icon.IsNullOrEmpty || icon.ToString () == ".")
							result.AddWarning ("Application bundle icon has not been set (iPhone Application options panel)");
						else
							dict ["CFBundleIconFile"] = icon.FileName;
					}
				}
				
				//newer icons - see http://developer.apple.com/library/ios/#qa/qa2010/qa1686.html
				if (v3_2_orNewer && !dict.ContainsKey ("CFBundleIconFiles")) {
					var arr = new PlistArray ();
					dict["CFBundleIconFiles"] = arr;
					
					if (supportsIPhone)
						AddIconRelativeIfNotEmpty (proj, arr, proj.BundleIcon, "Icon.png");
					
					if (v4_0_orNewer && supportsIPhone)
						if (!AddIconRelativeIfNotEmpty (proj, arr, proj.BundleIconHigh, "Icon@2x.png"))
							result.AddWarning ("iPhone high res bundle icon has not been set (iPhone Application options panel)");
					
					if (supportsIPad)
						if (!AddIconRelativeIfNotEmpty (proj, arr, proj.BundleIconIPad, "Icon-72.png"))
							result.AddWarning ("iPad bundle icon has not been set (iPhone Application options panel)");
					
					AddIconRelativeIfNotEmpty (proj, arr, proj.BundleIconSpotlight, "Icon-Small.png");
					
					if (supportsIPad)
						AddIconRelativeIfNotEmpty (proj, arr, proj.BundleIconIPadSpotlight, "Icon-Small-50.png");
					
					if (v4_0_orNewer && supportsIPhone)
						AddIconRelativeIfNotEmpty (proj, arr, proj.BundleIconSpotlightHigh, "Icon-Small@2x.png");
				}
				
				SetIfNotPresent (dict, "CFBundleIdentifier", identity.BundleID);
				SetIfNotPresent (dict, "CFBundleInfoDictionaryVersion", "6.0");
				SetIfNotPresent (dict, "CFBundleName", proj.Name);
				SetIfNotPresent (dict, "CFBundlePackageType", "APPL");
				if (!sim)
					dict["CFBundleResourceSpecification"] = "ResourceRules.plist";
				SetIfNotPresent (dict, "CFBundleSignature", "????");
				SetIfNotPresent (dict,  "CFBundleSupportedPlatforms",
					new PlistArray () { sim? "iPhoneSimulator" : "iPhoneOS" });
				SetIfNotPresent (dict, "CFBundleVersion", proj.BundleVersion ?? "1.0");
				
				if (!sim) {
					SetIfNotPresent (dict, "DTCompiler", sdkSettings.DTCompiler);
					SetIfNotPresent (dict, "DTPlatformBuild", dtSettings.DTPlatformBuild);
					SetIfNotPresent (dict, "DTSDKBuild", sdkSettings.DTSDKBuild);
				}
				SetIfNotPresent (dict, "DTPlatformName", sim? "iphonesimulator" : "iphoneos");
				if (!sim) {
					SetIfNotPresent (dict, "DTPlatformVersion", dtSettings.DTPlatformVersion);
				}
				SetIfNotPresent (dict, "DTSDKName", sim? sdkSettings.AlternateSDK : sdkSettings.CanonicalName);
				if (!sim) {
					SetIfNotPresent (dict, "DTXcode", dtSettings.DTXcode);
					SetIfNotPresent (dict, "DTXcodeBuild", dtSettings.DTXcodeBuild);
				}
				
				SetIfNotPresent (dict,  "LSRequiresIPhoneOS", true);
				if (v3_2_orNewer)
					SetIfNotPresent (dict,  "UIDeviceFamily", GetSupportedDevices (proj.SupportedDevices));
				
				if (v3_1_orNewer) {
					if (conf.MtouchArch != MtouchArch.ARMv6_ARMv7) {
						var val = conf.MtouchArch == MtouchArch.ARMv6? "armv6" : "armv7";
						var key = "UIRequiredDeviceCapabilities";
						var caps = dict.TryGetValue (key) ?? (dict[key] = new PlistArray ());
						var a = caps as PlistArray;
						if (a != null) {
							a.Add (val);
						} else {
							var d = (PlistDictionary) caps;
							d[val] = new PlistBoolean (true);
						}
					}
				}
				
				SetIfNotPresent (dict, "MinimumOSVersion", conf.MtouchMinimumOSVersion);
				
				SetNibProperty (dict, proj, proj.MainNibFile, "NSMainNibFile");
				if (proj.SupportedDevices == TargetDevice.IPhoneAndIPad)
					SetNibProperty (dict, proj, proj.MainNibFileIPad, "NSMainNibFile~ipad");
				
				
				if (v3_2_orNewer) {
					if (!dict.ContainsKey (OrientationUtil.KEY)) {
						result.AddWarning ("Supported orientations have not been set (iPhone Application options panel)");
					} else {
						var val = OrientationUtil.Parse ((PlistArray)dict[OrientationUtil.KEY]);
						if (!OrientationUtil.IsValidPair (val))
							result.AddWarning ("Supported orientations are not matched pairs (Info.plist)");
						if (dict.ContainsKey (OrientationUtil.KEY_IPAD)) {
							var pad = OrientationUtil.Parse ((PlistArray)dict[OrientationUtil.KEY_IPAD]);
							if (pad != Orientation.None && !OrientationUtil.IsValidPair (pad))
								result.AddWarning ("iPad orientations are not matched pairs (Info.plist)");
						}
					}
				}   
				
				return result;
			});
		}
		
		static bool AddIconRelativeIfNotEmpty (IPhoneProject proj, PlistArray arr, FilePath iconFullPath)
		{
			return AddIconRelativeIfNotEmpty (proj, arr, iconFullPath, null);
		}
		
		static bool AddIconRelativeIfNotEmpty (IPhoneProject proj, PlistArray arr, FilePath iconFullPath, string name)
		{
			var icon = iconFullPath.ToRelative (proj.BaseDirectory).ToString ();
			if (string.IsNullOrEmpty (icon) || icon == ".")
				return false;
			arr.Add (null ?? icon);
			return true;
		}
		
		static bool SetNibProperty (PlistDictionary dict, IPhoneProject proj, FilePath mainNibProp, string propName)
		{
			if (!dict.ContainsKey (propName)) {
				if (mainNibProp.IsNullOrEmpty) {
					return false;
				} else {
					string mainNib = mainNibProp.ToRelative (proj.BaseDirectory);
					if (mainNib.EndsWith (".nib") || mainNib.EndsWith (".xib"))
					    mainNib = mainNib.Substring (0, mainNib.Length - 4).Replace ('\\', '/');
					dict[propName] = mainNib;
				}
			}
			return true;
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
			return MacBuildUtilities.GetTool ("mtouch", project, monitor, out error);
		}
		
		protected override bool GetNeedsBuilding (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			if (base.GetNeedsBuilding (item, configuration))
				return true;
			
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null)
				return false;
			
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			
			if (proj.CompileTarget == CompileTarget.Library) {
				var nibDir = conf.ObjDir.Combine ("nibs");
				if (MacBuildUtilities.GetIBFilePairs (proj.Files, nibDir).Where (NeedsBuilding).Any ())
					return true;
				
				var dllWriteTime = File.GetLastWriteTimeUtc (conf.CompiledOutputName);
				foreach (var cp in GetContentFilePairs (proj.Files, conf.AppDirectory))
					if (File.Exists (cp.Input) && File.GetLastWriteTimeUtc (cp.Input) > dllWriteTime)
						return true;
			}
			
			if (proj.CompileTarget != CompileTarget.Exe)
				return false;
			
			if (!Directory.Exists (conf.AppDirectory))
				return true;
			
			bool isDevice = conf.Platform == IPhoneProject.PLAT_IPHONE;
			if (isDevice && !File.Exists (conf.AppDirectory.Combine ("PkgInfo")))
				return true;
			
			// the mtouch output
			FilePath mtouchOutput = conf.NativeExe;
			if (new FilePair (conf.CompiledOutputName, mtouchOutput).NeedsBuilding ())
				return true;
			
			//Interface Builder files
			if (MacBuildUtilities.GetIBFilePairs (proj.Files, conf.AppDirectory).Where (NeedsBuilding).Any ())
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
			if (proj == null)
				return;
			
			var conf = (IPhoneProjectConfiguration) proj.GetConfiguration (configuration);
			
			if (proj.CompileTarget == CompileTarget.Library) {
				var nibDir = conf.ObjDir.Combine ("nibs");
				if (Directory.Exists (nibDir))
					Directory.Delete (nibDir, true);
			}
			
			if (proj.CompileTarget != CompileTarget.Exe)
				return;
			
			//contains mtouch output, nibs, plist
			if (Directory.Exists (conf.AppDirectory))
				Directory.Delete (conf.AppDirectory, true);
			
			string embeddedContentInfoFile = conf.ObjDir.Combine ("monotouch_dll_content_processed");
			if (File.Exists (embeddedContentInfoFile))
				File.Delete (embeddedContentInfoFile);
			
			//remove the xcode project
			if (Directory.Exists (conf.OutputDirectory.Combine ("XcodeProject")))
				Directory.Delete (conf.OutputDirectory.Combine ("XcodeProject"), true);
		}
		
		void MangleLibraryResourceNames (BuildData buildData, FilePath tempNibDir)
		{
			for (int i = 0; i < buildData.Items.Count; i++) {
				var pf = buildData.Items[i] as ProjectFile;
				if (pf != null) {
					if (pf.BuildAction == BuildAction.Content) {
						buildData.Items[i] = new ProjectFile (pf.FilePath, BuildAction.EmbeddedResource) {
							ResourceId = "__monotouch_content_" + EscapeMangledResource (pf.ProjectVirtualPath)
						};
					} else if (pf.BuildAction == BuildAction.Page) {
						var vpath = pf.ProjectVirtualPath;
						if (vpath.Extension != ".xib")
							continue;
						vpath = vpath.ChangeExtension (".nib");
						var nibPath = vpath.ToAbsolute (tempNibDir);
						buildData.Items[i] = new ProjectFile (nibPath, BuildAction.EmbeddedResource) {
							ResourceId = "__monotouch_page_" + EscapeMangledResource (vpath)
						};
					}
				}
			}
		}
		
		static string EscapeMangledResource (string filename)
		{
			var sb = new StringBuilder (filename);
			sb.Replace ("_", "__");
			sb.Replace ("/", "_f");
			sb.Replace ("\\", "_b");
			return sb.ToString ();
		}
		
		static string UnescapeMangledResource (string mangled)
		{
			var sb = new StringBuilder (mangled.Length);
			for (int i = 0; i < mangled.Length; i++) {
				char c = mangled[i];
				if (c == '_') {
					i++;
					char c2 = mangled[i];
					switch (c2) {
					case '_':
						sb.Append ('_');
						break;
					case 'f':
						sb.Append ('/');
						break;
					case 'b':
						sb.Append ('\\');
						break;
					default:
						throw new Exception ("Unkmow resource escape char '" + c2 + "'");
					}
					continue;
				}
				sb.Append (c);
			}
			return sb.ToString ();
		}
		
		protected override BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData)
		{
			IPhoneProject proj = item as IPhoneProject;
			if (proj == null || (proj.CompileTarget != CompileTarget.Exe && proj.CompileTarget != CompileTarget.Library))
				return base.Compile (monitor, item, buildData);
			
			var cfg = (IPhoneProjectConfiguration) buildData.Configuration;
			var projFiles = buildData.Items.OfType<ProjectFile> ();
			
			if (proj.CompileTarget == CompileTarget.Library) {
				if (IPhoneFramework.MonoTouchVersion < new IPhoneSdkVersion (3, 99))
					return base.Compile (monitor, item, buildData);
				
				//pack nibs and content into the dll resources (MT 4+ only)
				var nibDir = cfg.ObjDir.Combine ("nibs");
				var xibRes = MacBuildUtilities.CompileXibFiles (monitor, projFiles, nibDir);
				if (xibRes.ErrorCount > 0)
					return xibRes;
				MangleLibraryResourceNames (buildData, nibDir);
				return xibRes.Append (base.Compile (monitor, item, buildData));
			}	
			
			string appDir = cfg.AppDirectory;
			
			var sdkVersion = cfg.MtouchSdkVersion.ResolveIfDefault ();
			if (!IPhoneFramework.SdkIsInstalled (sdkVersion))
				sdkVersion = IPhoneFramework.GetClosestInstalledSdk (sdkVersion);
			
			var result = MacBuildUtilities.UpdateCodeBehind (monitor, proj.CodeBehindGenerator, projFiles);
			if (result.ErrorCount > 0)
				return result;
			
			if (!cfg.IsValidAppName)
				result.AddWarning ("iOS executable name should be alphanumeric, or it may not run (Project Options->Build->Output).");
			
			if (result.Append (base.Compile (monitor, item, buildData)).ErrorCount > 0)
				return result;
			
			if (result.Append (MacBuildUtilities.CompileXibFiles (monitor, projFiles, appDir)).ErrorCount > 0)
				return result;
			
			var contentFiles = GetContentFilePairs (projFiles, appDir)
				.Where (NeedsBuilding).ToList ();
			
			contentFiles.AddRange (GetIconContentFiles (sdkVersion, proj, cfg));
			
			if (contentFiles.Count > 0) {
				monitor.BeginTask (GettextCatalog.GetString ("Copying content files"), contentFiles.Count);	
				foreach (var file in contentFiles) {
					file.EnsureOutputDirectory ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("Copying '{0}' to '{1}'", file.Input, file.Output));
					if (!File.Exists (file.Input)) {
						var msg = String.Format ("File '{0}' is missing.", file.Input);
						monitor.Log.WriteLine (msg);
						result.AddError (null, 0, 0, null, msg);
					} else {
						File.Copy (file.Input, file.Output, true);
					}
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
			return result;
		}
		
		static IEnumerable<FilePair> GetIconContentFiles (IPhoneSdkVersion sdkVersion, IPhoneProject proj,
			IPhoneProjectConfiguration conf)
		{
			bool v3_2_orNewer = sdkVersion >= IPhoneSdkVersion.V3_2;
			bool v4_0_orNewer = sdkVersion >= IPhoneSdkVersion.V4_0;
			bool supportsIPhone = (proj.SupportedDevices & TargetDevice.IPhone) != 0;
			bool supportsIPad = (proj.SupportedDevices & TargetDevice.IPad) != 0;
			var appDir = conf.AppDirectory;
			
			if (supportsIPhone && !proj.BundleIcon.IsNullOrEmpty)
					yield return new FilePair (proj.BundleIcon, appDir.Combine ("Icon.png"));
			
			if (!proj.BundleIconSpotlight.IsNullOrEmpty)
				yield return new FilePair (proj.BundleIconSpotlight, appDir.Combine ("Icon-Small.png"));
			
			if (v3_2_orNewer && supportsIPad) {
				if (!proj.BundleIconIPad.IsNullOrEmpty)
					yield return new FilePair (proj.BundleIconIPad, appDir.Combine ("Icon-72.png"));
				if (!proj.BundleIconIPadSpotlight.IsNullOrEmpty)
					yield return new FilePair (proj.BundleIconIPadSpotlight, appDir.Combine ("Icon-Small-50.png"));
			}
			
			if (supportsIPhone && v4_0_orNewer) {
				if (!proj.BundleIconHigh.IsNullOrEmpty)
					yield return new FilePair (proj.BundleIconHigh, appDir.Combine ("Icon@2x.png"));
				if (!proj.BundleIconSpotlightHigh.IsNullOrEmpty)
					yield return new FilePair (proj.BundleIconSpotlightHigh, appDir.Combine ("Icon-Small@2x.png"));
			}
		}
		
		static BuildResult ProcessPackaging (IProgressMonitor monitor, IPhoneSdkVersion sdkVersion, IPhoneProject proj,
			IPhoneProjectConfiguration conf, IPhoneAppIdentity identity)
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
			if (result.Append (GenXcent (monitor, sdkVersion, proj, conf, identity, out xcent)).ErrorCount > 0)
				return result;
			
			string resRules;
			if (result.Append (PrepareResourceRules (monitor, sdkVersion, conf, out resRules)).ErrorCount > 0)
				return result;
			
			if (result.Append (SignAppBundle (monitor, proj, conf, identity.SigningKey, resRules, xcent)).ErrorCount > 0)
				return result;
			
			return result;
		}
		
		static BuildResult CompressResources (IProgressMonitor monitor, IPhoneProjectConfiguration conf)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Compressing resources"), 0);
			
			var optTool = new ProcessStartInfo (
				"/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/iphoneos-optimize",
				 ProcessArgumentBuilder.Quote (conf.AppDirectory));
			
			monitor.Log.WriteLine (optTool.FileName + " " + optTool.Arguments);
			string errorOutput;
			int code = MacBuildUtilities.ExecuteCommand (monitor, optTool, out errorOutput);
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
				conf.CodesignKey = IPhoneProject.DEV_CERT_PREFIX;
			}
			
			IList<X509Certificate2> certs = null;
			if (conf.CodesignKey == IPhoneProject.DEV_CERT_PREFIX || conf.CodesignKey == IPhoneProject.DIST_CERT_PREFIX) {
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
		
		static BuildResult GenXcent (IProgressMonitor monitor, IPhoneSdkVersion sdkVersion, IPhoneProject proj, 
			IPhoneProjectConfiguration conf, IPhoneAppIdentity identity, out string xcentName)
		{
			xcentName = conf.CompiledOutputName.ChangeExtension (".xcent");
			
			monitor.BeginTask (GettextCatalog.GetString ("Processing entitlements file"), 0);
			
			string srcFile;
			
			if (!string.IsNullOrEmpty (conf.CodesignEntitlements)) {
				if (!File.Exists (conf.CodesignEntitlements))
					return BuildError ("Entitlements file \"" + conf.CodesignEntitlements + "\" not found.");
				srcFile = conf.CodesignEntitlements;
			} else {
				srcFile = "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS" + sdkVersion.ToString ()
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
			var newDict = new PlistDictionary ();
			doc.Root = newDict;
			newDict["application-identifier"] = identity.AppID;
			var keychainGroups = new PlistArray (new [] { identity.AppID } );
			newDict["keychain-access-groups"] = keychainGroups;
			
			//merge in the user's values
			foreach (var item in oldDict) {
				//FIXME: we currently ignore these items, and write our own, but maybe we should do substitutes
				//i.e. $(AppIdentifierPrefix)$(CFBundleIdentifier)
				if (item.Key == "application-identifier") {
					var str = item.Value as PlistString;
					if (str == null || string.IsNullOrEmpty (str.Value) || str.Value.Contains ('$'))
						continue;
				} else if (item.Key == "keychain-access-groups") {
					//special handling, merge into the array
					var keyArr = item.Value as PlistArray;
					foreach (var key in keyArr) {
						var str = key as PlistString;
						if (str != null && !string.IsNullOrEmpty (str.Value) && !str.Value.Contains ('$')) {
							keychainGroups.Add (str.Value);
						}
					}
					continue;
				}
				newDict[item.Key] = item.Value;
			}
			
			//merge in the settings from the provisioning profile, skipping some
			foreach (var item in identity.Profile.Entitlements)
				if (item.Key != "application-identifier" && item.Key != "keychain-access-groups")
					newDict[item.Key] = item.Value;
			
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
		
		static BuildResult PrepareResourceRules (IProgressMonitor monitor, IPhoneSdkVersion sdkVersion, IPhoneProjectConfiguration conf, out string resRulesFile)
		{
			resRulesFile = conf.AppDirectory.Combine ("ResourceRules.plist");
			
			monitor.BeginTask (GettextCatalog.GetString ("Preparing resources rules"), 0);
			
			if (File.Exists (resRulesFile))
				File.Delete (resRulesFile);
			
			string resRulesSrc = String.IsNullOrEmpty (conf.CodesignResourceRules)
				? "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS"
					+ sdkVersion.ToString () + ".sdk/ResourceRules.plist"
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
			
			var args = new ProcessArgumentBuilder ();
			args.Add ("-v", "-f", "-s");
			args.AddQuoted (Keychain.GetCertificateCommonName (key));
			args.AddQuotedFormat ("--resource-rules={0}", resRules);
			args.Add ("--entitlements");
			args.AddQuoted (xcent);
			args.AddQuoted (conf.AppDirectory);
			
			AddExtraArgs (args, conf.CodesignExtraArgs, proj, conf);
				
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
			if ((signResultCode = MacBuildUtilities.ExecuteCommand (monitor, psi, out output)) != 0) {
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
		
		static BuildResult UpdateDebugSettingsPlist (IProgressMonitor monitor, IPhoneProjectConfiguration conf,
		                                             ProjectFile template, string target)
		{
			if (template != null && template.BuildAction != BuildAction.Content)
				template = null;
			
			//if not in debug mode, make sure that the settings file is either
			//copied cleanly or deleted
			if (!conf.DebugMode) {
				if (template != null) {
					MacBuildUtilities.EnsureDirectoryForFile (target);
					File.Copy (template.FilePath, target, true);
				} else if (File.Exists (target)) {
					File.Delete (target);
				}
				return null;
			}
			
			return MacBuildUtilities.CreateMergedPlist (monitor, template, target, (PlistDocument doc) => {
				var br = new BuildResult ();
				var debuggerIP = System.Net.IPAddress.Any;
				bool sim = conf.Platform == IPhoneProject.PLAT_SIM;
				
				try {
					debuggerIP = IPhoneSettings.GetDebuggerHostIP (sim);
				} catch {
					br.AddWarning (GettextCatalog.GetString ("Could not resolve host IP for debugger settings"));
				}
				
				var dict = doc.Root as PlistDictionary;
				if (dict == null)
					doc.Root = dict = new PlistDictionary ();
				
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
					{ "DefaultValue", IPhoneSettings.DebuggerPort.ToString () }
				});
					
				arr.Add (new PlistDictionary (true) {
					{ "Type", "PSTextFieldSpecifier" },
					{ "Title", "Output Port" },
					{ "Key", "__monotouch_output_port" },
					{ "AutocapitalizationType", "None" },
					{ "AutocorrectionType", "No" },
					{ "DefaultValue", IPhoneSettings.DebuggerOutputPort.ToString () }
				});
				
				return br;
			});
		}
	}
}
