// 
// IdeVersionInfo.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011 Alan McGovern
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
using System.Reflection;

namespace MonoDevelop.Ide
{
	class IdeVersionInfo : ISystemInformationProvider
	{
		static bool IsMono ()
		{
			return Type.GetType ("Mono.Runtime") != null;
		}

		static string GetMonoVersionNumber ()
		{
			var t = Type.GetType ("Mono.Runtime"); 
			if (t == null)
				return "unknown";
			var mi = t.GetMethod ("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
			if (mi == null) {
				LoggingService.LogError ("No Mono.Runtime.GetDisplayName method found.");
				return "error";
			}
			return (string)mi.Invoke (null, null); 
		}
		
		public static string GetGtkVersion ()
		{
			uint v1 = 2, v2 = 0, v3 = 0;
			
			while (v1 < 99 && Gtk.Global.CheckVersion (v1, v2, v3) == null)
				v1++;
			v1--;
			
			while (v2 < 99 && Gtk.Global.CheckVersion (v1, v2, v3) == null)
				v2++;
			v2--;
			
			v3 = 0;
			while (v3 < 99 && Gtk.Global.CheckVersion (v1, v2, v3) == null)
				v3++;
			v3--;
			
			if (v1 == 99 || v2 == 99 || v3 == 99)
				return "unknown";
			return v1 +"." + v2 + "."+ v3;
		}
		
		static string GetGtkTheme ()
		{
			var settings = Gtk.Settings.Default;
			return settings != null ? settings.ThemeName : null;
		}

		static string GetMonoUpdateInfo ()
		{
			try {
				FilePath mscorlib = typeof (object).Assembly.Location;
				var updateinfo = mscorlib.ParentDirectory.Combine ("../../../updateinfo").FullPath;
				if (!System.IO.File.Exists (updateinfo))
					return null;
				var s = System.IO.File.ReadAllText (updateinfo).Split (new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				return s[s.Length - 1].Trim ();
			} catch {
			}
			return null;

		}

		public static string GetRuntimeInfo ()
		{
			string val;
			if (IsMono ()) {
				val = "Mono " + GetMonoVersionNumber ();
			} else {
				val = "Microsoft .NET " + Environment.Version;
			}

			if (IntPtr.Size == 8)
				val += (" (64-bit)");

			return val;
		}
		
		string ISystemInformationProvider.Title {
			get { return BrandingService.ApplicationLongName; }
		}

		string ISystemInformationProvider.Description {
			get {
				var sb = new System.Text.StringBuilder ();
				sb.Append ("Version ");
				sb.AppendLine (MonoDevelopVersion);
				
				sb.Append ("Installation UUID: ");
				sb.AppendLine (SystemInformation.InstallationUuid);
							
				sb.AppendLine ("Runtime:");
				sb.Append ("\t");
				sb.Append (GetRuntimeInfo ());
				sb.AppendLine ();
				sb.Append ("\tGTK+ ");
				sb.Append (GetGtkVersion ());
				var gtkTheme = GetGtkTheme ();
				if (!string.IsNullOrEmpty (gtkTheme))
					sb.Append (" (").Append (gtkTheme).AppendLine (" theme)");
				else
					sb.AppendLine ();

				var nativeRuntime = DesktopService.GetNativeRuntimeDescription ();
				if (!string.IsNullOrEmpty (nativeRuntime)) {
					sb.Append ('\t');
					sb.AppendLine (nativeRuntime);
				}

				if (Platform.IsWindows && !IsMono ()) {
					using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Xamarin\GtkSharp\Version")) {
						Version ver;
						if (key != null && Version.TryParse (key.GetValue (null) as string, out ver))
							sb.Append ("\tGTK# ").Append (ver);
					}
				}
				if (Platform.IsMac && IsMono ()) {
					var pkgVer = GetMonoUpdateInfo ();
					if (!string.IsNullOrEmpty (pkgVer)) {
						sb.AppendLine ();
						sb.Append ("\tPackage version: ");
						sb.Append (pkgVer);
					}
				}

				return sb.ToString (); 
			}
		}
		
		public static string MonoDevelopVersion {
			get {
				string v = "";
#pragma warning disable 162
				if (BuildInfo.Version != BuildInfo.VersionLabel)
					v += BuildInfo.Version;
#pragma warning restore 162
				if (Runtime.Version.Revision >= 0) {
					if (v.Length > 0)
						v += " ";
					v += "build " + Runtime.Version.Revision;
				}
				if (v.Length == 0)
					return BuildInfo.VersionLabel;
				else
					return BuildInfo.VersionLabel + " (" + v + ")";
			}
		}
	}
}

