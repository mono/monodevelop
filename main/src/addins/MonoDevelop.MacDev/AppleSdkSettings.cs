// 
// AppleSdkSettings.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using MonoMac.Foundation;
using System.IO;

namespace MonoDevelop.MacDev
{
	public static class AppleSdkSettings
	{
		const string SDK_KEY = "MonoDevelop.MacDev.AppleSdkRoot";
		
		internal static string DefaultRoot {
			get {
				// If the current developer root corresponds to a default SDK location for
				// any of the supported configurations, return that as the default root. If
				// they have configured an SDK in a non-default location, fall back to using
				// the default location for the latest SDK.
				if (DefaultRoots.Contains (DeveloperRoot))
					return DeveloperRoot;
				return DefaultRoots [0];
			}
		}
		
		// Put newer SDKs at the top as we scan from 0 -> List.Count
		static readonly IList<string> DefaultRoots = new List<string> {
			"/Applications/Xcode.app",
			"/Developer"
		};
		static DateTime lastWritten;
		
		static string GetEnvLocation ()
		{
			return Environment.GetEnvironmentVariable ("MD_APPLE_SDK_ROOT");
		}
		
		static void GetNewPaths (FilePath root, out FilePath xcode, out FilePath vplist, out FilePath devroot)
		{
			xcode = root;
			vplist = root.Combine ("Contents", "version.plist");
			devroot = root.Combine ("Contents", "Developer");
		}
		
		static void GetOldPaths (FilePath root, out FilePath xcode, out FilePath vplist, out FilePath devroot)
		{
			xcode = root.Combine ("Applications", "Xcode.app");
			vplist = root.Combine ("Library", "version.plist");
			devroot = root;
		}
		
		static bool ValidatePaths (FilePath xcode, FilePath vplist, FilePath devroot)
		{
			return Directory.Exists (xcode)
				&& Directory.Exists (devroot)
				&& File.Exists (vplist)
				&& File.Exists (xcode.Combine ("Contents", "Info.plist"));
		}
		
		internal static bool ValidateSdkLocation (FilePath location, out FilePath xcode, out FilePath vplist, out FilePath devroot)
		{
			GetNewPaths (location, out xcode, out vplist, out devroot);
			if (ValidatePaths (xcode, vplist, devroot))
				return true;
			
			GetOldPaths (location, out xcode, out vplist, out devroot);
			if (ValidatePaths (xcode, vplist, devroot))
				return true;
			
			return false;
		}
		
		internal static void SetConfiguredSdkLocation (FilePath location)
		{
			if (location.IsNullOrEmpty || location == DefaultRoots.First ())
				location = null;
			if (location == PropertyService.Get<string> (SDK_KEY))
				return;
			PropertyService.Set (SDK_KEY, location.ToString ());

			//if the location is being overridden by an env var, the setting has no effect, so don't bother updating
			if (GetEnvLocation () != null)
				return;

			Init ();
			Changed ();
		}
		
		internal static FilePath GetConfiguredSdkLocation ()
		{
			return PropertyService.Get<string> (SDK_KEY, null);
		}
		
		static void SetInvalid ()
		{
			XcodePath = FilePath.Empty;
			DeveloperRoot = FilePath.Empty;
			DeveloperRootVersionPlist = FilePath.Empty;
			IsValid = false;
			DTXcode = null;
			IsXcode4 = false;
			XcodeVersion = null;
			XcodeRevision = int.MinValue;
			lastWritten = DateTime.MinValue;
		}
		
		static AppleSdkSettings ()
		{
			MonoDevelop.MacInterop.Cocoa.InitMonoMac ();
			Init ();
		}
		
		static void Init ()
		{
			SetInvalid ();
			
			DeveloperRoot = Environment.GetEnvironmentVariable ("MD_APPLE_SDK_ROOT");
			if (DeveloperRoot.IsNullOrEmpty) {
				DeveloperRoot = GetConfiguredSdkLocation ();
			}
			
			bool foundSdk = false;
			FilePath xcode, vplist, devroot;
			
			if (DeveloperRoot.IsNullOrEmpty) {
				foreach (var v in DefaultRoots)  {
					if (ValidateSdkLocation (v, out xcode, out vplist, out devroot)) {
						foundSdk = true;
						break;
					} else {
						LoggingService.LogDebug ("Apple iOS SDK not found at '{0}'", v);
					}
				}
			} else {
				foundSdk = ValidateSdkLocation (DeveloperRoot, out xcode, out vplist, out devroot);
			}
			
			if (foundSdk) {
				XcodePath = xcode;
				DeveloperRoot = devroot;
				DeveloperRootVersionPlist = vplist;
			} else {
				SetInvalid ();
				return;
			}

			try {
				var plist = XcodePath.Combine ("Contents", "Info.plist");
				
				if (!File.Exists (plist))
					return;
				
				lastWritten = File.GetLastWriteTime (plist);
				
				XcodeVersion = new Version (3, 2, 6);
				XcodeRevision = 0;
				
				// DTXCode was introduced after xcode 3.2.6 so it may not exist
				using (var pool = new NSAutoreleasePool ()) {
					var dict = NSDictionary.FromFile (plist);
					NSObject value;
					
					if (dict.TryGetValue ((NSString) "DTXcode", out value))
						DTXcode = ((NSString) value).ToString ();
					
					if (dict.TryGetValue ((NSString) "CFBundleShortVersionString", out value))
						XcodeVersion = Version.Parse (((NSString) value).ToString ());
					
					if (dict.TryGetValue ((NSString) "CFBundleVersion", out value))
						XcodeRevision = int.Parse (((NSString) value).ToString ());
				}

				IsXcode4 = !string.IsNullOrEmpty (DTXcode) && int.Parse (DTXcode) >= 0400;
				IsValid = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading Xcode information for prefix '" + DeveloperRoot + "'", ex);
				SetInvalid ();
			}
		}
		
		public static FilePath DeveloperRoot { get; private set; }

		public static FilePath DeveloperRootVersionPlist {
			get; private set;
		}

		public static FilePath XcodePath {
			get; private set;
		}

		public static void CheckChanged ()
		{
			var plist = XcodePath.Combine ("Contents", "Info.plist");
			DateTime w = DateTime.MinValue;
			if (File.Exists (plist))
				w = File.GetLastWriteTime (plist);
			if (w != lastWritten) {
				Init ();
				Changed ();
			}
		}
		
		public static bool IsValid { get; private set; }
		public static string DTXcode { get; private set; }
		public static bool IsXcode4 { get; private set; }
		
		public static Version XcodeVersion { get; private set; }
		public static int XcodeRevision { get; private set; }
		
		public static event Action Changed;
	}
	
	class AppleSdkAboutInformation : ISystemInformationProvider
	{
		public string Description {
			get {
				var sb = new System.Text.StringBuilder ();
				sb.AppendLine ("Apple Developer Tools:");
				if (!AppleSdkSettings.IsValid) {
					sb.AppendLine ("\t(Not Found)");
					return sb.ToString ();
				}
				
				using (var pool = new NSAutoreleasePool ()) {
					sb.AppendFormat ("\t Xcode {0} ({1})", AppleSdkSettings.XcodeVersion, AppleSdkSettings.XcodeRevision);
					sb.AppendLine ();
					
					var dict = NSDictionary.FromFile (AppleSdkSettings.DeveloperRootVersionPlist);
					sb.AppendFormat ("\t Build {0}",
						dict[(NSString)"ProductBuildVersion"]);
				}
				return sb.ToString ();
			}
		}
	}
}
