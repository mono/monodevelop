// 
// MonoDroidCommands.cs
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

using MonoDevelop.Core;
using System;
using System.Xml.Linq;
using System.IO;

namespace MonoDevelop.MonoDroid
{
	public static class MonoDroidSettings
	{
		public static int DebuggerPort {
			get { return PropertyService.Get ("MonoDroid.Debugger.Port", 10000); }
		}
		
		public static int DebuggerOutputPort {
			get { return PropertyService.Get ("MonoDroid.Debugger.OutputPort", 10001); }
		}
		
		public static System.Net.IPAddress GetDebuggerHostIP (bool emulator)
		{
			if (emulator)
				return System.Net.IPAddress.Loopback;

			var ipStr = PropertyService.Get ("MonoDroid.Debugger.HostIP", "");
			try {
				if (!string.IsNullOrEmpty (ipStr))
					return System.Net.IPAddress.Parse (ipStr);
			} catch (Exception e) {
				LoggingService.LogInfo ("Error parsing Debugger HostIP: {0}: {1}", ipStr, e);
			}
			
			return System.Net.Dns.GetHostEntry (System.Net.Dns.GetHostName ()).AddressList[0];
		}
		
		internal static void SetConfiguredSdkLocations (string androidSdk, string javaSdk)
		{
			if (PropertyService.IsWindows) {
				SetWindowsConfiguredSdkLocations (androidSdk, javaSdk);
			} else {
				SetUnixConfiguredSdkLocations (androidSdk, javaSdk);
			}
		}
		
		internal static void SetWindowsConfiguredSdkLocations (string androidSdk, string javaSdk)
		{
			var wow = RegistryEx.Wow64.Key32;
			var key = @"SOFTWARE\Novell\MonoDroid";
			
			RegistryEx.SetValueString (RegistryEx.CurrentUser, key, "AndroidSdkDirectory", androidSdk ?? "", wow);
			RegistryEx.SetValueString (RegistryEx.CurrentUser, key, "JavaSdkDirectory", javaSdk ?? "", wow);
		}

		static void SetUnixConfiguredSdkLocations (string androidSdk, string javaSdk)
		{
			FilePath file = MonoDroidSdkConfigPath;
			XDocument doc = null;
			if (!File.Exists (file)) {
				string dir = file.ParentDirectory;
				if (!Directory.Exists (dir))
					Directory.CreateDirectory (dir);
			} else {
				doc = XDocument.Load (file);
			}
			
			if (doc == null || doc.Root == null) {
				doc = new XDocument (new XElement ("monodroid"));
			}
			
			var androidEl = doc.Root.Element ("android-sdk");
			if (androidEl == null) {
				androidEl = new XElement ("android-sdk");
				doc.Root.Add (androidEl);
			}
			androidEl.SetAttributeValue ("path", androidSdk);
			
			var javaEl = doc.Root.Element ("java-sdk");
			if (javaEl == null) {
				javaEl = new XElement ("java-sdk");
				doc.Root.Add (javaEl);
			}
			javaEl.SetAttributeValue ("path", javaSdk);
			
			doc.Save (file);
		}
		
		internal static void GetConfiguredSdkLocations (out string androidSdk, out string javaSdk)
		{
			if (PropertyService.IsWindows) {
				GetWindowsConfiguredSdkLocations (out androidSdk, out javaSdk);
			} else {
				GetUnixConfiguredSdkLocations (out androidSdk, out javaSdk);
			}
		}
		
		internal static void GetWindowsConfiguredSdkLocations (out string androidSdk, out string javaSdk)
		{
			var roots = new [] { RegistryEx.CurrentUser, RegistryEx.LocalMachine };
			var wow = RegistryEx.Wow64.Key32;
			var key = @"SOFTWARE\Novell\MonoDroid";
			
			androidSdk = null;
			javaSdk = null;
			
			foreach (var root in roots) {
				androidSdk = RegistryEx.GetValueString (root, key, "AndroidSdkDirectory", wow);
				if (!string.IsNullOrEmpty (androidSdk))
					break;
			}
			
			foreach (var root in roots) {
				javaSdk = RegistryEx.GetValueString (root, key, "JavaSdkDirectory", wow);
				if (!string.IsNullOrEmpty (javaSdk))
					break;
			}
			
			if (androidSdk != null && androidSdk.Length == 0)
				androidSdk = null;
			if (javaSdk != null && javaSdk.Length == 0)
				javaSdk = null;
		}

		static void GetUnixConfiguredSdkLocations (out string androidSdk, out string javaSdk)
		{
			androidSdk = null;
			javaSdk = null;
			
			string file = MonoDroidSdkConfigPath;
			if (!File.Exists (file))
				return;
			
			var doc = XDocument.Load (file);;
			
			var androidEl = doc.Root.Element ("android-sdk");
			if (androidEl != null)
				androidSdk = (string) androidEl.Attribute ("path");
			
			var javaEl = doc.Root.Element ("java-sdk");
			if (javaEl != null)
				javaSdk = (string) javaEl.Attribute ("path");
		}
		
		static string MonoDroidSdkConfigPath {
			get {
				var p = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
				return Path.Combine (Path.Combine (p, "xbuild"), "monodroid-config.xml");
			}
		}
	}
}
