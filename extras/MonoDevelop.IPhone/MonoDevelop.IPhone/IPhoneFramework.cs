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
namespace MonoDevelop.IPhone
{
public static class IPhoneFramework
	{
		static bool? isInstalled = null;
		static bool? simOnly = null;
		static IPhoneSdkVersion[] installedSdkVersions, knownOSVersions;
		
		const string PLAT_PLIST = "/Developer/Platforms/iPhoneOS.platform/Info.plist";
		const string SIM_PLIST = "/Developer/Platforms/iPhoneOSSimulator.platform/Info.plist";
		
		public static bool IsInstalled {
			get {
				if (!isInstalled.HasValue) {
					isInstalled = Directory.Exists ("/Developer/MonoTouch");
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
		
		public static MonoDevelop.Projects.BuildResult GetSimOnlyError ()
		{
			var res = new MonoDevelop.Projects.BuildResult ();
			res.AddError (GettextCatalog.GetString (
				"The evaluation version of MonoTouch does not support targeting the device. " + 
				"Please go to http://monotouch.net to purchase the full version."));
			return res;
		}
		
		static FilePath GetSdkPath (string version)
		{
			return "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS" + version + ".sdk";
		}
		
		static string GetSdkPlistFilename (string version)
		{
			return GetSdkPath (version).Combine ("SDKSettings.plist");
		}
		
		public static bool SdkIsInstalled (string version)
		{
			return File.Exists (GetSdkPlistFilename (version));
		}
		
		public static bool SdkIsInstalled (IPhoneSdkVersion version)
		{
			return SdkIsInstalled (version.ToString ());
		}
		
		static Dictionary<string,DTSdkSettings> sdkSettingsCache = new Dictionary<string,DTSdkSettings> ();
		
		public static DTSdkSettings GetSdkSettings (IPhoneSdkVersion sdk)
		{
			DTSdkSettings settings;
			if (sdkSettingsCache.TryGetValue (sdk.ToString (), out settings))
				return settings;
			
			settings = new DTSdkSettings ();
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (GetSdkPlistFilename (sdk.ToString ()));
			var dict = (PlistDictionary) doc.Root;
			
			settings.AlternateSDK = ((PlistString)dict["AlternateSDK"]).Value;
			settings.CanonicalName = ((PlistString)dict["CanonicalName"]).Value;
			var props = (PlistDictionary) dict["DefaultProperties"];
			settings.DTCompiler = ((PlistString)props["GCC_VERSION"]).Value;
			
			var sdkPath = GetSdkPath (sdk.ToString ());
			var file = sdkPath + "/System/Library/CoreServices/SystemVersion.plist";
			settings.DTPlatformBuild = GrabRootString (file, "ProductBuildVersion");
			
			sdkSettingsCache[sdk.ToString ()] = settings;
			return settings;
		}
		
		static DTSettings dtSettings;
		
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
			
			var xcodeVersion = GrabRootString ("/Developer/Applications/Xcode.app/Contents/Info.plist", "CFBundleShortVersionString");
			vals.DTXcode = "0" + xcodeVersion.Replace (".", "");
			
			vals.DTXcodeBuild = GrabRootString ("/Developer/Library/version.plist", "ProductBuildVersion");
			
			return (dtSettings = vals);
		}
		
		static string GrabRootString (string file, string key)
		{
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (file);
			return ((PlistString) ((PlistDictionary)doc.Root)[key]).Value;
		}
			
		public static IPhoneSdkVersion GetClosestInstalledSdk (IPhoneSdkVersion v)
		{
			//sorted low to high, so get first that's >= requested version
			foreach (var i in InstalledSdkVersions) {
				if (i.CompareTo (v) >= 0)
					return i;
			}
			return IPhoneSdkVersion.UseDefault;
		}
		
		public static IList<IPhoneSdkVersion> InstalledSdkVersions {
			get {
				EnsureSdkVersions ();
				return installedSdkVersions;
			}
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
			foreach (var v in IPhoneFramework.InstalledSdkVersions) {
				//pre-3.2
				if (IPhoneSdkVersion.V3_2.CompareTo (v) > 0) {
					yield return new IPhoneSimulatorTarget (TargetDevice.IPhone, v);
				}
				//3.2
				else if (IPhoneSdkVersion.V3_2.CompareTo (v) == 0) {
					yield return new IPhoneSimulatorTarget (TargetDevice.IPad, v);
				}
				//4.0, 4.1
				else if (IPhoneSdkVersion.V4_0.CompareTo (v) == 0 || IPhoneSdkVersion.V4_1.CompareTo (v) == 0) {
					yield return new IPhoneSimulatorTarget (TargetDevice.IPhone, v);
				}
				//unknown, assume both
				else {
					yield return new IPhoneSimulatorTarget (TargetDevice.IPhone, v);
					yield return new IPhoneSimulatorTarget (TargetDevice.IPad, v);
				}
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
			};
			
			const string sdkDir = "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/";
			if (!Directory.Exists (sdkDir)) {
				installedSdkVersions = new IPhoneSdkVersion[0];
				return;
			}
			
			var sdks = new List<string> ();
			foreach (var dir in Directory.GetDirectories (sdkDir)) {
				if (!File.Exists (dir + "/SDKSettings.plist"))
					continue;
				
				string d = dir.Substring (sdkDir.Length);
				if (d.StartsWith ("iPhoneOS"))
					d = d.Substring ("iPhoneOS".Length);
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
					LoggingService.LogError ("Could not parse iPhone SDK version {0}:\n{1}", s, ex.ToString ());
				}
			}
			installedSdkVersions = vs.ToArray ();
			Array.Sort (installedSdkVersions);
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
		}

		public class DTSdkSettings
		{
			public string CanonicalName { get; set; }
			public string AlternateSDK { get; set; }
			public string DTCompiler { get; set; }
			public string DTPlatformBuild { get; set; }
		}
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
