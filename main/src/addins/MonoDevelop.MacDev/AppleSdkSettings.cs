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
using MonoDevelop.Core;
using MonoMac.Foundation;
using System.IO;

namespace MonoDevelop.MacDev
{
	public static class AppleSdkSettings
	{
		const string SDK_KEY = "MonoDevelop.MacDev.AppleSdkRoot";
		internal const string DefaultRoot = "/Developer";
		static DateTime lastWritten;
		
		static string GetEnvLocation ()
		{
			return Environment.GetEnvironmentVariable ("MD_APPLE_SDK_ROOT");
		}
		
		internal static bool ValidateSdkLocation (FilePath location)
		{
			return System.IO.File.Exists (location.Combine ("Library", "version.plist"));
		}

		internal static void SetConfiguredSdkLocation (FilePath location)
		{
			if (location.IsNullOrEmpty || location == DefaultRoot)
				location = null;
			if (location == PropertyService.Get<string> (SDK_KEY))
				return;
			PropertyService.Set (SDK_KEY, location);
			if (GetEnvLocation () != null) {
				Init ();
				Changed ();
			}
		}
		
		internal static FilePath GetConfiguredSdkLocation ()
		{
			return PropertyService.Get<string> (SDK_KEY, null);
		}
		
		static void SetInvalid ()
		{
			IsValid = false;
			DTXcode = null;
			IsXcode4 = false;
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
				if (DeveloperRoot.IsNullOrEmpty)
					DeveloperRoot = "/Developer";
			}
			
			if (!ValidateSdkLocation (DeveloperRoot))
				return;
			
			try {
				var plist = XcodePath.Combine ("Contents", "Info.plist");
				if (!File.Exists (plist))
					return;
				lastWritten = File.GetLastWriteTime (plist);
				
				using (var pool = new NSAutoreleasePool ()) {
					var dict = NSDictionary.FromFile (plist);
					var val = (NSString) dict.ObjectForKey (new NSString ("DTXcode"));
					DTXcode = val.ToString ();
				}
				IsXcode4 = int.Parse (DTXcode) >= 0400;
				IsValid = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading Xcode information for prefix '" + DeveloperRoot + "'", ex);
				SetInvalid ();
			}
		}
		
		public static FilePath DeveloperRoot { get; private set; }
		
		public static FilePath XcodePath {
			get {
				return DeveloperRoot.Combine ("Applications", "Xcode.app");
			}
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
		
		public static event Action Changed;
	}
}
