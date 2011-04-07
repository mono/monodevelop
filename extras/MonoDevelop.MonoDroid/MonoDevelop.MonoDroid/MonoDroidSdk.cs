// 
// MonoDroidSdk.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Text;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Utilities;

namespace MonoDroid
{
	// This file must be kept in sync between: 
	//  - The Mono for Android MSBuild tooling
	//  - The Mono for Android VS Plugin
	//  - The Mono for Android MonoDevelop Plugin [extras/MonoDevelop.MonoDroid/MonoDevelop.MonoDroid/MonoDroidSdk.cs]
	internal static class MonoDroidSdk
	{
		public static readonly bool IsWindows, IsMac;
		
		//From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = System.Runtime.InteropServices.Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = System.Runtime.InteropServices.Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					System.Runtime.InteropServices.Marshal.FreeHGlobal (buf);
			}
			return false;
		}
		
		[System.Runtime.InteropServices.DllImport ("libc")]
		static extern int uname (IntPtr buf);
		
		static MonoDroidSdk ()
		{
			IsWindows = Path.DirectorySeparatorChar == '\\';
			IsMac = !IsWindows && IsRunningOnMac ();
		}
		
		public static void GetPaths (out string monoDroidBinDir, out string monoDroidFrameworkDir,
			out string androidSdkPath, out string javaSdkPath, TaskLoggingHelper log)
		{
			monoDroidBinDir = monoDroidFrameworkDir = androidSdkPath = javaSdkPath = null;
			
			GetMonoDroidSdk (out monoDroidBinDir, out monoDroidFrameworkDir);
			
			GetConfiguredSdkLocations (out androidSdkPath, out javaSdkPath, log);
			
			if (!ValidateAndroidSdkLocation (androidSdkPath))
				androidSdkPath = null;
			if (!ValidateJavaSdkLocation (javaSdkPath))
				javaSdkPath = null;
			if (androidSdkPath != null && javaSdkPath != null)
				return;
			
			var path = Environment.GetEnvironmentVariable ("PATH");
			var pathDirs = path.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
			
			if (androidSdkPath == null)
				androidSdkPath = FindAndroidSdk (pathDirs);

			if (javaSdkPath == null)
				javaSdkPath = FindJavaSdk (pathDirs);
		}
		
		/// <summary>
		/// Gets the MonoDroid SDK location.
		/// </summary>
		/// <returns>SDK location, or null if it was not found.</returns>
		static void GetMonoDroidSdk (out string monoDroidBinDir, out string monoDroidFrameworkDir)
		{
			monoDroidBinDir = monoDroidFrameworkDir = null;

			if (IsWindows) {
				// Find user's \Program Files
				var programFilesX86 = GetProgramFilesX86 ();

				// We keep our tools in:
				// \Program Files\MSBuild\Novell
				monoDroidBinDir = programFilesX86 + @"\MSBuild\Novell";

				// This will probably never be used on Windows
				var fxDir = programFilesX86 + @"\Reference Assemblies\Microsoft\Framework\MonoAndroid\v1.0";
				
				if (File.Exists (fxDir + @"\mscorlib.dll"))
					monoDroidFrameworkDir = fxDir;
				else
					monoDroidFrameworkDir = null;

				return;
			} 
			
			string monoAndroidPath  = Environment.GetEnvironmentVariable ("MONO_ANDROID_PATH");
			string libmandroid      = Path.Combine ("lib", "mandroid");
			string debugRuntime     = "Mono.Android.DebugRuntime-debug.apk";

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
			
			var bin = Path.Combine (monoDroidPath, relBinPath);
			if (!File.Exists (Path.Combine (bin, mandroid)))
				return false;
			
			monoDroidBinDir = bin;
			monoDroidFrameworkDir = Path.Combine (Path.Combine (Path.Combine (monoDroidPath, "lib"), "mono"), "2.1");
			return true;
		}
		
		/// <summary>
		/// Finds the Android SDK location. Should prefer values from GetConfiguredSdkLocations, if valid.
		/// </summary>
		/// <returns>SDK location, or null if it was not found.</returns>
		public static string FindAndroidSdk (string[] pathDirs)
		{
			if (IsWindows) {
				var programFilesX86 = GetProgramFilesX86 ();
				var installerLoc = programFilesX86 + @"\Android\android-sdk-windows";
				if (ValidateAndroidSdkLocation (installerLoc))
					return installerLoc;
				var unzipLoc = programFilesX86 + @"C:\android-sdk-windows";
				if (ValidateAndroidSdkLocation (unzipLoc))
					return unzipLoc;
			}
			
			var loc = Which (AdbTool, pathDirs);
			if (!string.IsNullOrEmpty (loc)) {
				loc = Path.GetDirectoryName (loc);
				if (ValidateAndroidSdkLocation (loc))
					return loc;
			}
			return null;
		}
		
		/// <summary>
		/// Finds the Java SDK location. Should prefer values from GetConfiguredSdkLocations, if valid.
		/// </summary>
		/// <returns>SDK location, or null if it was not found.</returns>
		public static string FindJavaSdk (string[] pathDirs)
		{
			string loc;
			if (IsWindows) {
				loc = WindowsGetJavaPath ();
				if (ValidateJavaSdkLocation (loc))
					return loc;
			}
			
			loc = Which (JarSignerTool, pathDirs);
			if (!string.IsNullOrEmpty (loc)) {
				loc = Path.GetDirectoryName (loc);
				if (ValidateJavaSdkLocation (loc))
					return loc;
			}
			return null;
		}
		
		/// <summary>
		/// Checks that a value is the location of a Java SDK.
		/// </summary>
		public static bool ValidateJavaSdkLocation (string loc)
		{
			return !string.IsNullOrEmpty (loc) && File.Exists (Path.Combine (Path.Combine (loc, "bin"), JarSignerTool));
		}
		
		/// <summary>
		/// Checks that a value is the location of an Android SDK.
		/// </summary>
		public static bool ValidateAndroidSdkLocation (string loc)
		{
			return !string.IsNullOrEmpty (loc) && File.Exists (Path.Combine (Path.Combine (loc, "platform-tools"), AdbTool));
		}
		
		static string WindowsGetJavaPath ()
		{
			foreach (var wow64 in new[] {RegistryEx.Wow64.Key32, RegistryEx.Wow64.Key64 }) {
				var currentVersion = RegistryEx.GetValueString (RegistryEx.LocalMachine, @"SOFTWARE\JavaSoft\Java Development Kit", "CurrentVersion", wow64);
				if (!string.IsNullOrEmpty (currentVersion)) {
					string javaPath = RegistryEx.GetValueString (RegistryEx.LocalMachine, @"SOFTWARE\JavaSoft\Java Development Kit\" + currentVersion, "JavaHome", wow64);
					if (!string.IsNullOrEmpty (javaPath))
						return javaPath;
				}
			}
			return null;
		}
		
		static string AdbTool {
			get { return IsWindows? "adb.exe" : "adb"; }
		}
		
		static string JarSignerTool {
			get { return IsWindows? "jarsigner.exe" : "jarsigner"; }
		}
		
		static string Which (string executable, string[] pathDirs)
		{
			foreach (var dir in pathDirs) {
				if (File.Exists (Path.Combine (dir, (executable))))
					return dir;
			}
			return null;
		}
		
		/// <summary>
		/// Sets the configured sdk locations.
		/// </summary>
		public static void SetConfiguredSdkLocations (string androidSdk, string javaSdk)
		{
			if (IsWindows) {
				SetWindowsConfiguredSdkLocations (androidSdk, javaSdk);
			} else {
				SetUnixConfiguredSdkLocations (androidSdk, javaSdk);
			}
		}
		
		/// <summary>
		/// Gets the configured sdk locations. They may be invalid, so should be validated before use.
		/// </summary>
		public static void GetConfiguredSdkLocations (out string androidSdk, out string javaSdk, TaskLoggingHelper log)
		{
			if (IsWindows) {
				androidSdk = GetWindowsAndroidSdkLocation (log);
				javaSdk = GetWindowsJavaSdkLocation (log);
			} else {
				GetUnixConfiguredSdkLocations (out androidSdk, out javaSdk);
			}
		}
		
		const string MDREG_KEY = @"SOFTWARE\Novell\Mono for Android";
		const string MDREG_ANDROID = "AndroidSdkDirectory";
		const string MDREG_JAVA = "JavaSdkDirectory";
		const string MDREG_MONODROID = "InstallDirectory";
		const string ANDROID_INSTALLER_PATH = @"SOFTWARE\Android SDK Tools";
		const string ANDROID_INSTALLER_KEY = "Path";

		static void SetWindowsConfiguredSdkLocations (string androidSdk, string javaSdk)
		{
			var wow = RegistryEx.Wow64.Key32;
			
			RegistryEx.SetValueString (RegistryEx.CurrentUser, MDREG_KEY, MDREG_ANDROID, androidSdk ?? "", wow);
			RegistryEx.SetValueString (RegistryEx.CurrentUser, MDREG_KEY, MDREG_JAVA, javaSdk ?? "", wow);
		}

		static void SetUnixConfiguredSdkLocations (string androidSdk, string javaSdk)
		{
			androidSdk = NullIfEmpty (androidSdk);
			javaSdk = NullIfEmpty (javaSdk);
			
			var file = MonoDroidSdkConfigPath;
			XDocument doc = null;
			if (!File.Exists (file)) {
				string dir = Path.GetDirectoryName (file);
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
		
		private static string GetWindowsAndroidSdkLocation (TaskLoggingHelper log)
		{
			var roots = new[] { RegistryEx.CurrentUser, RegistryEx.LocalMachine };
			var wow = RegistryEx.Wow64.Key32;

			log.LogMessage ("Looking for Android SDK..");

			// Check for the key written by the Android SDK installer first
			foreach (var root in roots)
				if (CheckRegistryKeyForExecutable (root, ANDROID_INSTALLER_PATH, ANDROID_INSTALLER_KEY, wow, "platform-tools", AdbTool, log))
					return RegistryEx.GetValueString (root, ANDROID_INSTALLER_PATH, ANDROID_INSTALLER_KEY, wow);

			// Check for the key the user gave us in the VS options
			foreach (var root in roots)
				if (CheckRegistryKeyForExecutable (root, MDREG_KEY, MDREG_ANDROID, wow, "platform-tools", AdbTool, log))
					return RegistryEx.GetValueString (root, MDREG_KEY, MDREG_ANDROID, wow);

			// Check 2 default locations
			var program_files = GetProgramFilesX86 ();
			var installerLoc = Path.Combine (program_files, @"\Android\android-sdk-windows");
			var unzipLoc = Path.Combine (program_files, @"C:\android-sdk-windows");

			if (ValidateAndroidSdkLocation (installerLoc)) {
				log.LogMessage ("  adb.exe found in {0}", installerLoc);
				return installerLoc;
			}

			if (ValidateAndroidSdkLocation (unzipLoc)) {
				log.LogMessage ("  adb.exe found in {0}", unzipLoc);
				return unzipLoc;
			}

			// We ran out of things to check..
			return null;
		}

		private static string GetWindowsJavaSdkLocation (TaskLoggingHelper log)
		{
			string subkey = @"SOFTWARE\JavaSoft\Java Development Kit";

			log.LogMessage ("Looking for Java SDK..");

			foreach (var wow64 in new[] { RegistryEx.Wow64.Key32, RegistryEx.Wow64.Key64 }) {
				string key_name = string.Format (@"{0}\{1}\{2}", "HKLM", subkey, "CurrentVersion");
				var currentVersion = RegistryEx.GetValueString (RegistryEx.LocalMachine, subkey, "CurrentVersion", wow64);

				if (!string.IsNullOrEmpty (currentVersion)) {
					log.LogMessage ("  Key {0} found: {1}.", key_name, currentVersion);

					if (CheckRegistryKeyForExecutable (RegistryEx.LocalMachine, subkey + "\\" + currentVersion, "JavaHome", wow64, "bin", JarSignerTool, log))
						return RegistryEx.GetValueString (RegistryEx.LocalMachine, subkey + "\\" + currentVersion, "JavaHome", wow64);
				}

				log.LogMessage ("  Key {0} not found.", key_name);
			}

			// We ran out of things to check..
			return null;
		}

		private static bool CheckRegistryKeyForExecutable (UIntPtr key, string subkey, string valueName, MonoDroid.RegistryEx.Wow64 wow64, string subdir, string exe, TaskLoggingHelper log)
		{
			string key_name = string.Format (@"{0}\{1}\{2}", key == RegistryEx.CurrentUser ? "HKCU" : "HKLM", subkey, valueName);

			var path = NullIfEmpty (RegistryEx.GetValueString (key, subkey, valueName, wow64));

			if (path == null) {
				log.LogMessage ("  Key {0} not found.", key_name);
				return false;
			}

			if (!File.Exists (Path.Combine (path, subdir, exe))) {
				log.LogMessage ("  Key {0} found:\n    Path does not contain {1} in \\{2} ({3}).", key_name, exe, subdir, path);
				return false;
			}

			log.LogMessage ("  Key {0} found:\n    Path contains {1} in \\{2} ({3}).", key_name, exe, subdir, path);

			return true;
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
		
		static string NullIfEmpty (string s)
		{
			if (s == null || s.Length != 0)
				return s;
			return null;
		}
		
		static string GetProgramFilesX86 ()
		{
			if (IntPtr.Size == 8 || !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("PROCESSOR_ARCHITEW6432")))
				return Environment.GetEnvironmentVariable("PROGRAMFILES(X86)");
			else
				return Environment.GetEnvironmentVariable("PROGRAMFILES");
		}
	}
	
	class RegistryEx
	{
		const string ADVAPI = "advapi32.dll";
		
		public static UIntPtr CurrentUser = (UIntPtr)0x80000001;
		public static UIntPtr LocalMachine = (UIntPtr)0x80000002;
		
		[DllImport (ADVAPI, CharSet = CharSet.Unicode, SetLastError = true)]
		static extern int RegOpenKeyEx (UIntPtr hKey, string subKey, uint reserved, uint sam, out UIntPtr phkResult);
		
		[DllImport (ADVAPI, CharSet = CharSet.Unicode, SetLastError = true)]
		static extern int RegQueryValueExW (UIntPtr hKey, string lpValueName, int lpReserved, out uint lpType,
		  StringBuilder lpData, ref uint lpcbData);
		
		[DllImport (ADVAPI, CharSet = CharSet.Unicode, SetLastError = true)]
		static extern int RegSetValueExW (UIntPtr hKey, string lpValueName, int lpReserved,
			uint dwType, string data, uint cbData);
		
		[DllImport (ADVAPI, CharSet = CharSet.Unicode, SetLastError = true)]
		static extern int RegSetValueExW (UIntPtr hKey, string lpValueName, int lpReserved,
			uint dwType, IntPtr data, uint cbData);
		
		[DllImport (ADVAPI, CharSet = CharSet.Unicode, SetLastError = true)]
		static extern int RegCreateKeyEx (UIntPtr hKey, string subKey, uint reserved, string @class, uint options,
			uint samDesired, IntPtr lpSecurityAttributes, out UIntPtr phkResult, out Disposition lpdwDisposition);
		
		[DllImport ("advapi32.dll", SetLastError = true)]
		static extern int RegCloseKey (UIntPtr hKey);

		public static string GetValueString (UIntPtr key, string subkey, string valueName, Wow64 wow64)
		{
			UIntPtr regKeyHandle;
			uint sam = (uint)Rights.QueryValue + (uint)wow64;
			if (RegOpenKeyEx (key, subkey, 0, sam, out regKeyHandle) != 0)
				return null;

			try {
				uint type;
				var sb = new StringBuilder (2048);
				uint cbData = (uint) sb.Capacity;
				if (RegQueryValueExW (regKeyHandle, valueName, 0, out type, sb, ref cbData) == 0) {
					return sb.ToString ();
				}
				return null;
			} finally {
				RegCloseKey (regKeyHandle);
			}
		}
		
		public static void SetValueString (UIntPtr key, string subkey, string valueName, string value, Wow64 wow64)
		{
			UIntPtr regKeyHandle;
			uint sam = (uint)(Rights.CreateSubKey | Rights.SetValue) + (uint)wow64;
			uint options = (uint) Options.NonVolatile;
			Disposition disposition;
			if (RegCreateKeyEx (key, subkey, 0, null, options, sam, IntPtr.Zero, out regKeyHandle, out disposition) != 0) {
				throw new Exception ("Could not open or craete key");
			}

			try {
				uint type = (uint)ValueType.String;
				uint lenBytesPlusNull = ((uint)value.Length + 1) * 2;
				var result = RegSetValueExW (regKeyHandle, valueName, 0, type, value, lenBytesPlusNull);
				if (result != 0)
					throw new Exception (string.Format ("Error {0} setting registry key '{1}{2}@{3}'='{4}'",
						result, key, subkey, valueName, value));
			} finally {
				RegCloseKey (regKeyHandle);
			}
		}

		[Flags]
		enum Rights : uint
		{
			None = 0,
			QueryValue = 0x0001,
			SetValue = 0x0002,
			CreateSubKey = 0x0004,
			EnumerateSubKey = 0x0008,
		}
		
		enum Options
		{
			BackupRestore = 0x00000004,
			CreateLink = 0x00000002,
			NonVolatile = 0x00000000,
			Volatile = 0x00000001,
		}

		public enum Wow64 : uint
		{
			Key64 = 0x0100,
			Key32 = 0x0200,
		}
		
		enum ValueType : uint
		{
			None = 0, //REG_NONE
			String = 1, //REG_SZ
			UnexpandedString = 2, //REG_EXPAND_SZ
			Binary = 3, //REG_BINARY
			DWord = 4, //REG_DWORD
			DWordLittleEndian = 4, //REG_DWORD_LITTLE_ENDIAN
			DWordBigEndian = 5, //REG_DWORD_BIG_ENDIAN
			Link = 6, //REG_LINK
			MultiString = 7, //REG_MULTI_SZ
			ResourceList = 8, //REG_RESOURCE_LIST
			FullResourceDescriptor = 9, //REG_FULL_RESOURCE_DESCRIPTOR
			ResourceRequirementsList = 10, //REG_RESOURCE_REQUIREMENTS_LIST
			QWord = 11, //REG_QWORD
			QWordLittleEndian = 11, //REG_QWORD_LITTLE_ENDIAN
		}
		
		enum Disposition : uint
		{
			CreatedNewKey  = 0x00000001,
			OpenedExistingKey = 0x00000002,
		}
	}
}

