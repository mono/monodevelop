// 
// IPhoneSdk.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.MacDev.Plist;
using System.Runtime.InteropServices;
using System.Linq;

namespace MonoDevelop.IPhone
{
	public class AppleIPhoneSdk
	{
		static IPhoneSdkVersion[] knownOSVersions = new [] {
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
		
		static DTSettings dtSettings;
		static Dictionary<string,DTSdkSettings> sdkSettingsCache = new Dictionary<string,DTSdkSettings> ();
		static Dictionary<string,DTSdkSettings> simSettingsCache = new Dictionary<string,DTSdkSettings> ();
		static DateTime lastSdkVersionWrite = DateTime.MinValue;
		
		public FilePath DeveloperRoot { get; private set; }
		public FilePath DevicePlatform { get { return DeveloperRoot.Combine ("Platforms/iPhoneOS.platform"); } }
		public FilePath SimPlatform { get { return DeveloperRoot.Combine ("Platforms/iPhoneOS.platform"); } }
		
		const string VERSION_PLIST = "Library/version.plist";
		const string SYSTEM_VERSION_PLIST = "/System/Library/CoreServices/SystemVersion.plist";
		
		public AppleIPhoneSdk (string sdkRoot)
		{
			this.DeveloperRoot = sdkRoot;
			Init ();
		}
		
		void Init ()
		{
			IsInstalled = File.Exists (DevicePlatform.Combine ("Info.plist"));
			if (IsInstalled) {
				File.GetLastWriteTime (DeveloperRoot.Combine (VERSION_PLIST));
				InstalledSdkVersions = EnumerateSdks (DevicePlatform.Combine ("Developer/SDKs"), "iPhoneOS");
				InstalledSimVersions = EnumerateSdks (SimPlatform.Combine ("Developer/SDKs"), "iPhoneSimulator");
			} else {
				InstalledSdkVersions = new IPhoneSdkVersion[0];
				InstalledSimVersions = new IPhoneSdkVersion[0];
			}
		}
		
		public bool IsInstalled { get; private set; }
		public IPhoneSdkVersion[] InstalledSdkVersions { get; private set; }
		public IPhoneSdkVersion[] InstalledSimVersions { get; private set; }
		
		static IPhoneSdkVersion[] EnumerateSdks (string sdkDir, string name)
		{
			if (!Directory.Exists (sdkDir))
				return new IPhoneSdkVersion[0];
			
			var sdks = new List<string> ();
			
			foreach (FilePath dir in Directory.GetDirectories (sdkDir)) {
				if (!File.Exists (dir.Combine ("SDKSettings.plist")))
					continue;
				string d = dir.FileName;
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
		
		public FilePath GetSdkPath (IPhoneSdkVersion version, bool sim)
		{
			return GetSdkPath (version.ToString (), sim);
		}
		
		public FilePath GetSdkPath (string version, bool sim)
		{
			if (sim)
				return DeveloperRoot.Combine ("Platforms/iPhoneSimulator.platform/Developer/SDKs/iPhoneSimulator" + version + ".sdk");
			else
				return DeveloperRoot.Combine ("Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS" + version + ".sdk");
		}
		
		string GetSdkPlistFilename (string version, bool sim)
		{
			return GetSdkPath (version, sim).Combine ("SDKSettings.plist");
		}
		
		public bool SdkIsInstalled (string version, bool sim)
		{
			return File.Exists (GetSdkPlistFilename (version, sim));
		}
		
		public bool SdkIsInstalled (IPhoneSdkVersion version, bool sim)
		{
			return SdkIsInstalled (version.ToString (), sim);
		}
		
		public DTSdkSettings GetSdkSettings (IPhoneSdkVersion sdk, bool isSim)
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
		
		DTSdkSettings LoadSdkSettings (IPhoneSdkVersion sdk, bool isSim)
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
			settings.DTSDKBuild = GrabRootString ("/" + SYSTEM_VERSION_PLIST, "ProductBuildVersion");
			
			return settings;
		}
		
		public DTSettings GetDTSettings ()
		{
			if (dtSettings != null)
				return dtSettings;
			
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (DevicePlatform.Combine ("Info.plist"));
			var dict = (PlistDictionary) doc.Root;
			var infos = (PlistDictionary) dict["AdditionalInfo"];
			var vals = new DTSettings ();
			
			vals.DTPlatformVersion = ((PlistString)infos["DTPlatformVersion"]).Value;
			
			IntPtr pool = SendMessage (GetClass ("NSAutoreleasePool"), GetSelector ("new"));
			try {
				var bundle = SendMessage (GetClass ("NSString"), GetSelector ("stringWithUTF8String:"), "DTXcode");
				var plist = SendMessage (GetClass ("NSString"), GetSelector ("stringWithUTF8String:"),
					DeveloperRoot.Combine ("Applications/Xcode.app/Contents/Info.plist"));
				var data = SendMessage (GetClass ("NSDictionary"), GetSelector ("dictionaryWithContentsOfFile:"), plist);
				var val = SendMessage (data, GetSelector ("objectForKey:"), bundle);
				vals.DTXcode = Marshal.PtrToStringAuto (SendMessage (val, GetSelector ("UTF8String")));
			} finally {
				SendMessage (pool, GetSelector ("release"));
			}
			
			vals.DTPlatformBuild = GrabRootString (DevicePlatform.Combine ("version.plist"), "ProductBuildVersion");
			vals.DTXcodeBuild = GrabRootString (DeveloperRoot.Combine (VERSION_PLIST), "ProductBuildVersion");
			vals.BuildMachineOSBuild = GrabRootString (DeveloperRoot.Combine (SYSTEM_VERSION_PLIST), "ProductBuildVersion");
			
			return (dtSettings = vals);
		}
		
		static string GrabRootString (string file, string key)
		{
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (file);
			return ((PlistString) ((PlistDictionary)doc.Root)[key]).Value;
		}
			
		public IPhoneSdkVersion GetClosestInstalledSdk (IPhoneSdkVersion v, bool sim)
		{
			//sorted low to high, so get first that's >= requested version
			foreach (var i in GetInstalledSdkVersions (sim)) {
				if (i.CompareTo (v) >= 0)
					return i;
			}
			return IPhoneSdkVersion.UseDefault;
		}
		
		public IList<IPhoneSdkVersion> GetInstalledSdkVersions (bool sim)
		{
			return sim? InstalledSimVersions : InstalledSdkVersions;
		}
		
		public IList<IPhoneSdkVersion> KnownOSVersions { get { return knownOSVersions; } }
		
		public IEnumerable<IPhoneSimulatorTarget> GetSimulatorTargets (IPhoneSdkVersion minVersion, TargetDevice projSupportedDevices)
		{	
			return GetSimulatorTargets ().Where (t => t.Supports (minVersion, projSupportedDevices));
		}
		
		public IEnumerable<IPhoneSimulatorTarget> GetSimulatorTargets ()
		{
			foreach (var v in GetInstalledSdkVersions (true)) {
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
		
		internal void CheckCaches ()
		{
			DateTime lastWrite = DateTime.MinValue;
			try {
				lastWrite = File.GetLastWriteTime (DeveloperRoot.Combine (VERSION_PLIST));
				if (lastWrite == lastSdkVersionWrite)
					return;
			} catch (IOException) {
			}
			lastSdkVersionWrite = lastWrite;
			
			dtSettings = null;
			sdkSettingsCache.Clear ();
			simSettingsCache.Clear ();
			
			Init ();
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
}

