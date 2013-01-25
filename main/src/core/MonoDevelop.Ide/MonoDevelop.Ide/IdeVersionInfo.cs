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
				LoggingService.LogError ("No Mono.Runtime.GetDiplayName method found.");
				return "error";
			}
			return (string)mi.Invoke (null, null); 
		}
		
		static string GetGtkVersion ()
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
		
		string ISystemInformationProvider.Title {
			get { return BrandingService.ApplicationName; }
		}

		string ISystemInformationProvider.Description {
			get {
				var sb = new System.Text.StringBuilder ();
				sb.Append ("Version ");
				sb.AppendLine (MonoDevelopVersion);
				
				sb.Append ("Installation UUID: ");
				sb.AppendLine (SystemInformation.InstallationUuid);
							
				sb.AppendLine ("Runtime:");
				if (IsMono ()) {
					sb.Append ("\tMono " + GetMonoVersionNumber ());
				} else {
					sb.Append ("\tMicrosoft .NET " + Environment.Version);
				}
			
				if (IntPtr.Size == 8)
					sb.Append (" (64-bit)");
				sb.AppendLine ();
				sb.Append ("\tGTK ");
				sb.AppendLine (GetGtkVersion ());
				sb.Append ("\tGTK# (");
				sb.Append (typeof(Gtk.VBox).Assembly.GetName ().Version);
				sb.Append (")");

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
				if (BuildVariables.PackageVersion != BuildVariables.PackageVersionLabel)
					v += BuildVariables.PackageVersion;
				if (IdeApp.Version.Revision >= 0) {
					if (v.Length > 0)
						v += " ";
					v += "build " + IdeApp.Version.Revision;
				}
				if (v.Length == 0)
					return BuildVariables.PackageVersionLabel;
				else
					return BuildVariables.PackageVersionLabel + " (" + v + ")";
			}
		}
	}
}

