// 
// IPhoneFrameworkBackend.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Core.Serialization;
using MonoDevelop.MacDev.Plist;

using System.Runtime.InteropServices;

namespace MonoDevelop.IPhone
{
	public static class IPhoneFramework
	{
		static bool? isInstalled = null;
		static bool? simOnly = null;
		static IPhoneSdkVersion? monoTouchVersion;
		static IPhoneSdkVersion[] installedSdkVersions, knownOSVersions, installedSimVersions;
		static DTSettings dtSettings;
		static Dictionary<string,DTSdkSettings> sdkSettingsCache = new Dictionary<string,DTSdkSettings> ();
		static Dictionary<string,DTSdkSettings> simSettingsCache = new Dictionary<string,DTSdkSettings> ();
		
		static DateTime? lastSdkVersionWrite = DateTime.MinValue;
		static DateTime? lastMTExeWrite = DateTime.MinValue;
		
		const string PLAT_PLIST = "/Developer/Platforms/iPhoneOS.platform/Info.plist";
		const string PLAT_VERSION_PLIST = "/Developer/Platforms/iPhoneOS.platform/version.plist";
		const string SIM_PLIST = "/Developer/Platforms/iPhoneSimulator.platform/Info.plist";
		const string MT_VERSION_FILE = "/Developer/MonoTouch/Version";
		const string MONOTOUCH_EXE = "/Developer/MonoTouch/usr/bin/mtouch";
		const string VERSION_PLIST = "/Developer/Library/version.plist";
		const string SYSTEM_VERSION_PLIST = "/System/Library/CoreServices/SystemVersion.plist";
		const string SDK_DIR = "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/";
		const string SIM_SDK_DIR = "/Developer/Platforms/iPhoneSimulator.platform/Developer/SDKs/";
		
		public static bool IsInstalled {
			get {
				if (!isInstalled.HasValue) {
					isInstalled = File.Exists (MONOTOUCH_EXE);
				}
				return isInstalled.Value;
			}
		}
		
		public static bool SimOnly {
			get {
				if (!simOnly.HasValue) {
					simOnly = !File.Exists ("/Developer/MonoTouch/usr/bin/arm-darwin-mono");
				}
				return simOnly.Value;
			}
		}
		
		public static IPhoneSdkVersion MonoTouchVersion {
			get {
				if (!monoTouchVersion.HasValue) {
					if (File.Exists (MT_VERSION_FILE)) {
						try {
							monoTouchVersion = IPhoneSdkVersion.Parse (
								File.ReadAllText (MT_VERSION_FILE).Trim ());
						} catch (Exception ex) {
							LoggingService.LogError ("Failed to read MonoTouch version", ex);
						}
					}
					if (!monoTouchVersion.HasValue)
						monoTouchVersion = new IPhoneSdkVersion ();
				}
				return monoTouchVersion.Value;
			}
		}
		
		public static MonoDevelop.Projects.BuildResult GetSimOnlyError ()
		{
			var res = new MonoDevelop.Projects.BuildResult ();
			res.AddError (GettextCatalog.GetString (
				"The evaluation version of MonoTouch does not support targeting the device. " + 
				"Please go to http://monotouch.net to purchase the full version."));
			return res;
		}
		
		static FilePath GetSdkPath (string version, bool sim)
		{
			if (sim)
				return "/Developer/Platforms/iPhoneSimulator.platform/Developer/SDKs/iPhoneSimulator" + version + ".sdk";
			else
				return "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS" + version + ".sdk";
		}
		
		static string GetSdkPlistFilename (string version, bool sim)
		{
			return GetSdkPath (version, sim).Combine ("SDKSettings.plist");
		}
		
		public static bool SdkIsInstalled (string version, bool sim)
		{
			return File.Exists (GetSdkPlistFilename (version, sim));
		}
		
		public static bool SdkIsInstalled (IPhoneSdkVersion version, bool sim)
		{
			return SdkIsInstalled (version.ToString (), sim);
		}
		
		public static DTSdkSettings GetSdkSettings (IPhoneSdkVersion sdk, bool isSim)
		{
			Dictionary<string,DTSdkSettings> cache = isSim? simSettingsCache : sdkSettingsCache;
			
			DTSdkSettings settings;
			if (cache.TryGetValue (sdk.ToString (), out settings))
				return settings;
			
			try {
				settings = LoadSdkSettings (sdk, isSim);
			} catch (Exception ex) {
				var sdkName = isSim? "iPhoneSimulator" : "iPhoneOS";
				LoggingService.LogError (string.Format ("Error loading settings for SDK {0} {1}", sdkName, sdk), ex);
			}
			
			cache[sdk.ToString ()] = settings;
			return settings;
		}
		
		static DTSdkSettings LoadSdkSettings (IPhoneSdkVersion sdk, bool isSim)
		{
			var settings = new DTSdkSettings ();
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (GetSdkPlistFilename (sdk.ToString (), isSim));
			var dict = (PlistDictionary) doc.Root;
			
			if (!isSim)
				settings.AlternateSDK = ((PlistString)dict["AlternateSDK"]).Value;
			
			settings.CanonicalName = ((PlistString)dict["CanonicalName"]).Value;
			var props = (PlistDictionary) dict["DefaultProperties"];
			settings.DTCompiler = ((PlistString)props["GCC_VERSION"]).Value;
			
			TargetDevice deviceFamilies = TargetDevice.NotSet;
			PlistArray deviceFamiliesArr;
			if ((deviceFamiliesArr = props.TryGetValue ("SUPPORTED_DEVICE_FAMILIES") as PlistArray) != null) {
				foreach (var v in deviceFamiliesArr) {
					var s = v as PlistString;
					int i;
					if (s != null && int.TryParse (s.Value, out i)) {
						deviceFamilies |= (TargetDevice) i;
					}
				}
			}
			settings.DeviceFamilies = deviceFamilies;
			
			var sdkPath = GetSdkPath (sdk.ToString (), isSim);
			settings.DTSDKBuild = GrabRootString (sdkPath + SYSTEM_VERSION_PLIST, "ProductBuildVersion");
			
			return settings;
		}
		
		public static DTSettings GetDTSettings ()
		{
			if (dtSettings != null)
				return dtSettings;
			
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (PLAT_PLIST);
			var dict = (PlistDictionary) doc.Root;
			var infos = (PlistDictionary) dict["AdditionalInfo"];
			var vals = new DTSettings ();
			
			vals.DTPlatformVersion = ((PlistString)infos["DTPlatformVersion"]).Value;
			
			IntPtr pool = SendMessage (GetClass ("NSAutoreleasePool"), GetSelector ("new"));
			try {
				var bundle = SendMessage (GetClass ("NSString"), GetSelector ("stringWithUTF8String:"), "DTXcode");
				var plist = SendMessage (GetClass ("NSString"), GetSelector ("stringWithUTF8String:"), "/Developer/Applications/Xcode.app/Contents/Info.plist");
				var data = SendMessage (GetClass ("NSDictionary"), GetSelector ("dictionaryWithContentsOfFile:"), plist);
				var val = SendMessage (data, GetSelector ("objectForKey:"), bundle);
				vals.DTXcode = Marshal.PtrToStringAuto (SendMessage (val, GetSelector ("UTF8String")));
			} finally {
				SendMessage (pool, GetSelector ("release"));
			}
			
			vals.DTPlatformBuild = GrabRootString (PLAT_VERSION_PLIST, "ProductBuildVersion");
			vals.DTXcodeBuild = GrabRootString (VERSION_PLIST, "ProductBuildVersion");
			vals.BuildMachineOSBuild = GrabRootString (SYSTEM_VERSION_PLIST, "ProductBuildVersion");
			
			return (dtSettings = vals);
		}
		
		static string GrabRootString (string file, string key)
		{
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (file);
			return ((PlistString) ((PlistDictionary)doc.Root)[key]).Value;
		}
			
		public static IPhoneSdkVersion GetClosestInstalledSdk (IPhoneSdkVersion v, bool sim)
		{
			//sorted low to high, so get first that's >= requested version
			foreach (var i in GetInstalledSdkVersions (sim)) {
				if (i.CompareTo (v) >= 0)
					return i;
			}
			return IPhoneSdkVersion.UseDefault;
		}
		
		public static IList<IPhoneSdkVersion> GetInstalledSdkVersions (bool sim)
		{
			EnsureSdkVersions ();
			return sim? installedSimVersions : installedSdkVersions;
		}
		
		public static IList<IPhoneSdkVersion> KnownOSVersions {
			get {
				EnsureSdkVersions ();
				return knownOSVersions;
			}
		}
		
		public static IEnumerable<IPhoneSimulatorTarget> GetSimulatorTargets (IPhoneSdkVersion minVersion, TargetDevice projSupportedDevices)
		{	
			return GetSimulatorTargets ().Where (t => t.Supports (minVersion, projSupportedDevices));
		}
		
		public static IEnumerable<IPhoneSimulatorTarget> GetSimulatorTargets ()
		{
			foreach (var v in IPhoneFramework.GetInstalledSdkVersions (true)) {
				var settings = GetSdkSettings (v, true);
				
				if (v < IPhoneSdkVersion.V3_2) {
					yield return new IPhoneSimulatorTarget (TargetDevice.IPhone, v);
					continue;
				}
				if (v == IPhoneSdkVersion.V3_2) {
					yield return new IPhoneSimulatorTarget (TargetDevice.IPad, v);
					continue;
				}
				
				if (settings.DeviceFamilies.HasFlag (TargetDevice.IPhone))
					yield return new IPhoneSimulatorTarget (TargetDevice.IPhone, v);
				if (settings.DeviceFamilies.HasFlag (TargetDevice.IPad))
					yield return new IPhoneSimulatorTarget (TargetDevice.IPad, v);
			}
		}
		
		static void EnsureSdkVersions ()
		{
			if (installedSdkVersions == null)
				Init ();
		}
		
		static void Init ()
		{
			knownOSVersions = new [] {
				new IPhoneSdkVersion (new [] { 3, 0 }),
				new IPhoneSdkVersion (new [] { 3, 1 }),
				new IPhoneSdkVersion (new [] { 3, 1, 2 }),
				new IPhoneSdkVersion (new [] { 3, 1, 3 }),
				new IPhoneSdkVersion (new [] { 3, 2 }),
				new IPhoneSdkVersion (new [] { 4, 0 }),
				new IPhoneSdkVersion (new [] { 4, 1 }),
				new IPhoneSdkVersion (new [] { 4, 2 }),
				new IPhoneSdkVersion (new [] { 4, 3 }),
			};
			installedSdkVersions = EnumerateSdks (SDK_DIR, "iPhoneOS");
			installedSimVersions = EnumerateSdks (SIM_SDK_DIR, "iPhoneSimulator");
		}
		
		static IPhoneSdkVersion[] EnumerateSdks (string sdkDir, string name)
		{
			if (!Directory.Exists (sdkDir))
				return new IPhoneSdkVersion[0];
			
			var sdks = new List<string> ();
			
			foreach (var dir in Directory.GetDirectories (sdkDir)) {
				if (!File.Exists (Path.Combine (dir, "SDKSettings.plist")))
					continue;
				string d = Path.GetFileName (dir);
				if (!d.StartsWith (name))
					continue;
				d = d.Substring (name.Length);
				if (d.EndsWith (".sdk"))
					d = d.Substring (0, d.Length - ".sdk".Length);
				if (d.Length > 0)
					sdks.Add (d);
			}
			var vs = new List<IPhoneSdkVersion> ();
			foreach (var s in sdks) {
				try {
					vs.Add (IPhoneSdkVersion.Parse (s));
				} catch (Exception ex) {
					LoggingService.LogError ("Could not parse {0} SDK version '{1}':\n{2}", name, s, ex.ToString ());
				}
			}
			var versions = vs.ToArray ();
			Array.Sort (versions);
			return versions;
		}
		
		public static void CheckInfoCaches ()
		{
			CheckSdkCaches ();
			CheckMTCaches ();
		}
		
		static void CheckSdkCaches ()
		{
			DateTime? lastWrite;
			try {
				lastWrite = File.GetLastWriteTime (VERSION_PLIST);
			} catch (IOException) {
				lastWrite = null;
			}
			if (lastWrite == lastSdkVersionWrite)
				return;
			lastSdkVersionWrite = lastWrite;
			
			installedSdkVersions = null;
			knownOSVersions = null;
			dtSettings = null;
			sdkSettingsCache.Clear ();
			simSettingsCache.Clear ();
		}
		
		static void CheckMTCaches ()
		{
			DateTime? lastWrite;
			try {
				lastWrite = File.GetLastWriteTime (MONOTOUCH_EXE);
			} catch (IOException) {
				lastWrite = null;
			}
			if (lastWrite == lastMTExeWrite)
				return;
			lastMTExeWrite = lastWrite;
			
			simOnly = null;
			monoTouchVersion = null;
		}
		
		public static void ShowSimOnlyDialog ()
		{
			if (!SimOnly)
				return;
			
			var dialog = new Dialog ();
			dialog.Title =  GettextCatalog.GetString ("Evaluation Version");
			
			dialog.VBox.PackStart (
			 	new Label ("<b><big>Feature Not Available In Evaluation Version</big></b>") {
					Xalign = 0.5f,
					UseMarkup = true
				}, true, false, 12);
			
			var align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			align.Add (new Label (
				"You should upgrade to the full version of MonoTouch to target and deploy\n" +
				" to the device, and to enable your applications to be distributed.") {
					Xalign = 0.5f,
					Justify = Justification.Center
				});
			
			align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			var buyButton = new Button (
				new Label (GettextCatalog.GetString ("<big>Buy MonoTouch</big>")) { UseMarkup = true } );
			buyButton.Clicked += delegate {
				System.Diagnostics.Process.Start ("http://monotouch.net");
				dialog.Respond (ResponseType.Accept);
			};
			align.Add (buyButton);
			
			dialog.AddButton (GettextCatalog.GetString ("Continue evaluation"), ResponseType.Close);
			dialog.ShowAll ();
			
			MessageService.ShowCustomDialog (dialog);
		}

		public class DTSettings
		{
			public string DTXcode { get; set; }
			public string DTXcodeBuild { get; set; }
			public string DTPlatformVersion { get; set; }
			public string DTPlatformBuild { get; set; }
			public string BuildMachineOSBuild { get; set; }
		}

		public class DTSdkSettings
		{
			public string CanonicalName { get; set; }
			public string AlternateSDK { get; set; }
			public string DTCompiler { get; set; }
			public string DTSDKBuild { get; set; }
			public TargetDevice DeviceFamilies { get; set; }
		}

		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
		static extern IntPtr GetSelector (string selector);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
		static extern IntPtr GetClass (string klass);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern IntPtr SendMessage (IntPtr klass, IntPtr selector);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern IntPtr SendMessage (IntPtr klass, IntPtr selector, IntPtr arg1);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern IntPtr SendMessage (IntPtr klass, IntPtr selector, string arg1);
	}
	
	public class MonoTouchInstalledCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			return IPhoneFramework.IsInstalled;
		}
	}
	
	/*
	public class IPhoneSdkVersionCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			return IPhoneFramework.IsInstalled;
		}
	}*/
}
