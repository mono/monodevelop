// VersionInformationTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//   Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2009 RemObjects Software
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
//
//

using System;
using Gtk;
using MonoDevelop.Core;
using System.Reflection;
using System.Text;
using System.IO;


namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class VersionInformationTabPage: VBox
	{
		//FIXME: move this somewhere it can be accessed by the error reporting code
		static string GetVersionInformation ()
		{
			var sb = new StringBuilder ();
			
			string mdversion = BuildVariables.PackageVersion == BuildVariables.PackageVersionLabel
				? BuildVariables.PackageVersionLabel
				: string.Format ("{0} ({1})",  BuildVariables.PackageVersionLabel, BuildVariables.PackageVersion);
			sb.Append ("MonoDevelop ");
			sb.AppendLine (mdversion);
			
			sb.AppendLine ("Operating System:");
			if (Platform.IsMac) {
				sb.Append ("\tMac OS X ");
			} else if (Platform.IsWindows) {
				sb.Append ("\tWindows ");
			} else {
				sb.Append ("\tLinux ");
			}
			sb.Append (Environment.OSVersion.Version.ToString ());
			if (IntPtr.Size == 8 || Environment.GetEnvironmentVariable ("PROCESSOR_ARCHITEW6432") != null)
				sb.Append (" (64-bit)");
			sb.AppendLine ();
			
			sb.AppendLine ("Runtime:");
			if (IsMono ()) {
				sb.Append ("\tMono " + GetMonoVersionNumber ());
			} else {
				sb.Append ("\tMicrosoft .NET " + Environment.Version);
			}
			if (IntPtr.Size == 8)
				sb.Append (" (64-bit)");
			sb.AppendLine ();
				
			sb.Append ("\tGTK " + GetGtkVersion ());
			sb.AppendLine (" (GTK# " + typeof(VBox).Assembly.GetName ().Version + ")");
				
			if (Platform.IsMac) {
				var mtVersion = GetMonotouchVersion ();
				if (mtVersion != null)
					sb.AppendLine ("MonoTouch: " + mtVersion);
			}
				
// TODO:Get mono droid version.
//				var droidVersion = GetMonoDroidVersion ();
//				if (droidVersion != null)
//					sb.AppendLine ("\tMono for Android: " + droidVersion);
			
			var biFile = ((FilePath)typeof (VersionInformationTabPage).Assembly.Location).ParentDirectory.Combine ("buildinfo");
			if (File.Exists (biFile)) {
				sb.AppendLine ("Build information:");
				foreach (var line in File.ReadAllLines (biFile)) {
					if (!string.IsNullOrWhiteSpace (line)) {
						sb.Append ("\t");
						sb.AppendLine (line.Trim ());
					}
				}
			}
			
			return sb.ToString ();
		}
		
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
		
		#region Monotouch
		static string GetMonotouchVersion ()
		{
			var mtdir = GetMonotouchLocation ();
			var mtouch = System.IO.Path.Combine (mtdir, "usr/bin/mtouch");
			if (!File.Exists (mtouch))
				return mtouch + "not found.";
			var version = System.IO.Path.Combine (mtdir, "Version");
			if (!File.Exists (version))
				return "unknown version";
			bool isEvaluation = !File.Exists (System.IO.Path.Combine (mtdir, "usr/bin/arm-darwin-mono"));
			if (isEvaluation)
				return File.ReadAllText (version).Trim () + " (Evaluation)";
			return File.ReadAllText (version);
		}

		static string GetMonotouchLocation ()
		{
			const string DefaultLocation = "/Developer/MonoTouch";
			const string MTOUCH_LOCATION_ENV_VAR = "MD_MTOUCH_SDK_ROOT";
			var mtRoot = Environment.GetEnvironmentVariable (MTOUCH_LOCATION_ENV_VAR);
			return mtRoot ?? GetConfiguredMonoTouchSdkRoot () ?? DefaultLocation;
		}
		
		static string GetConfiguredMonoTouchSdkRoot ()
		{
			const string MTOUCH_SDK_KEY = "MonoDevelop.IPhone.MonoTouchSdkRoot";
			return PropertyService.Get<string> (MTOUCH_SDK_KEY, null);
		}
		#endregion
		
		#region MonoDroid
		static string GetMonoDroidVersion ()
		{
			string monoDroidBinDir;
			string monoDroidFrameworkDir;
			GetMonoDroidSdk (out monoDroidBinDir, out monoDroidFrameworkDir);
			return monoDroidBinDir;
		}
		
		static void GetMonoDroidSdk (out string monoDroidBinDir, out string monoDroidFrameworkDir)
		{
			monoDroidBinDir = monoDroidFrameworkDir = null;

			if (Platform.IsWindows) {
				// Find user's \Program Files
				var programFilesX86 = GetProgramFilesX86 ();

				// We keep our tools in:
				// \Program Files\MSBuild\Novell
				monoDroidBinDir = programFilesX86 + @"\MSBuild\Novell";

				// This will probably never be used on Windows
				var fxDir = programFilesX86 + @"\Reference Assemblies\Microsoft\Framework\MonoAndroid\v1.0";
				
				if (System.IO.File.Exists (fxDir + @"\mscorlib.dll"))
					monoDroidFrameworkDir = fxDir;
				else
					monoDroidFrameworkDir = null;

				return;
			} 
			
			string monoAndroidPath = Environment.GetEnvironmentVariable ("MONO_ANDROID_PATH");
			string libmandroid = System.IO.Path.Combine ("lib", "mandroid");
			string debugRuntime = "Mono.Android.DebugRuntime-debug.apk";

			foreach (var loc in new[]{
					new { D = monoAndroidPath,              L = libmandroid,  E = debugRuntime },
					new { D = "/Developer/MonoAndroid/usr", L = libmandroid,  E = debugRuntime },
					new { D = "/opt/mono-android",          L = libmandroid,  E = debugRuntime }})
				if (CheckMonoDroidPath (loc.D, loc.L, loc.E, out monoDroidBinDir, out monoDroidFrameworkDir))
					return;
		}
		
		static bool CheckMonoDroidPath (string monoDroidPath, string relBinPath, string mandroid, out string monoDroidBinDir, out string monoDroidFrameworkDir)
		{
			monoDroidBinDir = monoDroidFrameworkDir = null;
			
			if (string.IsNullOrEmpty (monoDroidPath))
				return false;
			
			var bin = System.IO.Path.Combine (monoDroidPath, relBinPath);
			if (!System.IO.File.Exists (System.IO.Path.Combine (bin, mandroid)))
				return false;
			
			monoDroidBinDir = bin;
			monoDroidFrameworkDir = System.IO.Path.Combine (monoDroidPath, "lib", "mono", "2.1");
			return true;
		}
		
		static string GetProgramFilesX86 ()
		{
			if (IntPtr.Size == 8 || !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("PROCESSOR_ARCHITEW6432")))
				return Environment.GetEnvironmentVariable ("PROGRAMFILES(X86)");
			else
				return Environment.GetEnvironmentVariable ("PROGRAMFILES");
		}
		#endregion
		
		public VersionInformationTabPage ()
		{
			var buf = new TextBuffer (null);
			buf.Text = GetVersionInformation ();
			
			var sw = new ScrolledWindow () {
				BorderWidth = 6,
				ShadowType = ShadowType.EtchedIn,
				Child = new TextView (buf) {
					Editable = false,
					LeftMargin = 4,
					RightMargin = 4,
					PixelsAboveLines = 4,
					PixelsBelowLines = 4
				}
			};
			
			PackStart (sw, true, true, 0);
		}
	}
}
