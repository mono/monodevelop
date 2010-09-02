// 
// MonoDroidFrameworkBackend.cs
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
using Mono.Addins;
using MonoDevelop.Core;
using System.Text;
using System.Runtime.InteropServices;

namespace MonoDevelop.MonoDroid
{
	public static class MonoDroidFramework
	{
		static MonoDroidFramework ()
		{
			EnvironmentOverrides = new Dictionary<string, string> ();
			try {
				FindMonoDroidSdk ();
				if (IsInstalled)
					FindAndroidJavaSdks ();
			} catch (Exception ex) {
				LoggingService.LogError ("Error detecting MonoDroid SDK", ex);
			}
		}
		
		internal static void FindAndroidJavaSdks ()
		{
			string configuredAndroidSdk, configuredJavaSdk;
			MonoDroidSettings.GetConfiguredSdkLocations (out configuredAndroidSdk, out configuredJavaSdk);
			
			var path = Environment.GetEnvironmentVariable ("PATH");
			var pathDirs = path.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
			
			FilePath androidSdk = configuredAndroidSdk;
			if (!ValidateAndroidSdkLocation (androidSdk)) {
				androidSdk = FindAndroidSdk (pathDirs);
			}
			if (androidSdk.IsNullOrEmpty) {
				LoggingService.LogError ("Android SDK not found, needed by MonoDroid addin");
				AndroidBinDir = JavaBinDir = null;
				return;
			}
			
			FilePath javaSdk = configuredJavaSdk;
			if (!ValidateJavaSdkLocation (javaSdk)) {
				javaSdk = FindJavaSdk (pathDirs);
			}
			if (javaSdk.IsNullOrEmpty) {
				LoggingService.LogError ("Java SDK not found, needed by MonoDroid addin");
				AndroidBinDir = JavaBinDir = null;
				return;
			}
			
			JavaBinDir = javaSdk.Combine ("bin");
			AndroidBinDir = androidSdk.Combine ("tools");
			
			EnvironmentOverrides ["PATH"] =
				AndroidBinDir + Path.PathSeparator + 
				JavaBinDir + Path.PathSeparator + 
				path;
		}
		
		static void FindMonoDroidSdk ()
		{
			FilePath loc = Environment.GetEnvironmentVariable ("MONODROID_PATH");
			if (loc.IsNullOrEmpty) {
				LoggingService.LogInfo ("MonoDroid SDK not found, disabling MonoDroid addin");
				return;
			}
				
			BinDir = loc.Combine ("bin");
			FrameworkDir = loc.Combine ("lib", "mono", "2.1");
			
			if (!File.Exists (BinDir.Combine ("monodroid.exe"))) {
				LoggingService.LogError ("MonoDroid SDK in '{0}' is missing monodroid.exe", loc);
				BinDir = FrameworkDir = null;
			} else if (!File.Exists (FrameworkDir.Combine ("mscorlib.dll"))) {
				LoggingService.LogError ("MonoDroid SDK in '{0}' is missing mscorlib.dll", loc);
				BinDir = FrameworkDir = null;
			}
		}
		
		internal static FilePath FindAndroidSdk (string[] pathDirs)
		{
			var loc = Which (AdbTool, pathDirs);
			if (!loc.IsNullOrEmpty) {
				loc = loc.ParentDirectory;
				if (ValidateAndroidSdkLocation (loc))
					return loc;
			}
			
			return FilePath.Null;
		}
		
		internal static FilePath FindJavaSdk (string[] pathDirs)
		{
			FilePath loc = WindowsGetJavaPath ();
			if (ValidateJavaSdkLocation (loc))
				return loc;
			
			loc = Which (JarsignerTool, pathDirs);
			if (!loc.IsNullOrEmpty) {
				loc = loc.ParentDirectory;
				if (ValidateJavaSdkLocation (loc))
					return loc;
			}
			
			return FilePath.Null;
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
		
		internal static bool ValidateJavaSdkLocation (FilePath loc)
		{
			return !loc.IsNullOrEmpty && File.Exists (loc.Combine ("bin", JarsignerTool));
		}
		
		internal static bool ValidateAndroidSdkLocation (FilePath loc)
		{
			return !loc.IsNullOrEmpty && File.Exists (loc.Combine ("tools", AdbTool));
		}
		
		static string AdbTool {
			get { return PropertyService.IsWindows? "adb.exe" : "adb"; }
		}
		
		static string JarsignerTool {
			get { return PropertyService.IsWindows? "jarsigner.exe" : "jarsigner"; }
		}
		
		static FilePath Which (string executable, string[] pathDirs)
		{
			foreach (FilePath dir in pathDirs) {
				if (File.Exists (dir.Combine (executable)))
					return dir;
			}
			return null;
		}
		
		/// <summary>
		/// Ensures all required SDKs are installed. If not, prompts the user to select the locations.
		/// </summary>
		/// <returns>True if the location is configured or the user selects a valid location.</returns>
		public static bool EnsureSdksInstalled ()
		{
			if (HasAndroidJavaSdks)
				return true;
			
			//FIXME: prompt user for locations
			
			return false;
		}
		
		/// <summary>
		/// Whether the MonoDroid SDK has been detected.
		/// </summary>
		public static bool IsInstalled {
			get {
				return !BinDir.IsNullOrEmpty;
			}
		}
		
		/// <summary>
		/// Whether the Android and Java SDKs have been detected/configured.
		/// </summary>
		public static bool HasAndroidJavaSdks {
			get {
				return !JavaBinDir.IsNullOrEmpty && !AndroidBinDir.IsNullOrEmpty;
			}
		}
		
		/// <summary>
		/// Directory with MonoDroid tools binaries.
		/// </summary>
		public static FilePath BinDir { get; private set; }
		
		/// <summary>
		/// Directory with MonoDroid framework assemblies.
		/// </summary>
		public static FilePath FrameworkDir { get; private set; }
		
		/// <summary>
		/// Bin directory of the Java SDK.
		/// </summary>
		public static FilePath JavaBinDir { get; private set; }
		
		/// <summary>
		/// Tools directory of the Android SDK.
		/// </summary>
		public static FilePath AndroidBinDir { get; private set; }
		
		/// <summary>
		/// Environment variables to be used when invoking MonoDroid tools.
		/// </summary>
		public static Dictionary<string,string> EnvironmentOverrides { get; private set; }
		
		public static readonly string[] Permissions = new [] {
			"ACCESS_CHECKIN_PROPERTIES",
			"ACCESS_COARSE_LOCATION",
			"ACCESS_FINE_LOCATION",
			"ACCESS_LOCATION_EXTRA_COMMANDS",
			"ACCESS_MOCK_LOCATION",
			"ACCESS_NETWORK_STATE",
			"ACCESS_SURFACE_FLINGER",
			"ACCESS_WIFI_STATE",
			"ACCOUNT_MANAGER",
			"AUTHENTICATE_ACCOUNTS",
			"BATTERY_STATS",
			"BIND_APPWIDGET",
			"BIND_DEVICE_ADMIN",
			"BIND_INPUT_METHOD",
			"BIND_WALLPAPER",
			"BLUETOOTH",
			"BLUETOOTH_ADMIN",
			"BRICK",
			"BROADCAST_PACKAGE_REMOVED",
			"BROADCAST_SMS",
			"BROADCAST_STICKY",
			"BROADCAST_WAP_PUSH",
			"CALL_PHONE",
			"CALL_PRIVILEGED",
			"CAMERA",
			"CHANGE_COMPONENT_ENABLED_STATE",
			"CHANGE_CONFIGURATION",
			"CHANGE_NETWORK_STATE",
			"CHANGE_WIFI_MULTICAST_STATE",
			"CHANGE_WIFI_STATE",
			"CLEAR_APP_CACHE",
			"CLEAR_APP_USER_DATA",
			"CONTROL_LOCATION_UPDATES",
			"DELETE_CACHE_FILES",
			"DELETE_PACKAGES",
			"DEVICE_POWER",
			"DIAGNOSTIC",
			"DISABLE_KEYGUARD",
			"DUMP",
			"EXPAND_STATUS_BAR",
			"FACTORY_TEST",
			"FLASHLIGHT",
			"FORCE_BACK",
			"GET_ACCOUNTS",
			"GET_PACKAGE_SIZE",
			"GET_TASKS",
			"GLOBAL_SEARCH",
			"HARDWARE_TEST",
			"INJECT_EVENTS",
			"INSTALL_LOCATION_PROVIDER",
			"INSTALL_PACKAGES",
			"INTERNAL_SYSTEM_WINDOW",
			"INTERNET",
			"KILL_BACKGROUND_PROCESSES",
			"MANAGE_ACCOUNTS",
			"MANAGE_APP_TOKENS",
			"MASTER_CLEAR",
			"MODIFY_AUDIO_SETTINGS",
			"MODIFY_PHONE_STATE",
			"MOUNT_FORMAT_FILESYSTEMS",
			"MOUNT_UNMOUNT_FILESYSTEMS",
			"PERSISTENT_ACTIVITY",
			"PROCESS_OUTGOING_CALLS",
			"READ_CALENDAR",
			"READ_CONTACTS",
			"READ_FRAME_BUFFER",
			"READ_HISTORY_BOOKMARKS",
			"READ_INPUT_STATE",
			"READ_LOGS",
			"READ_OWNER_DATA",
			"READ_PHONE_STATE",
			"READ_SMS",
			"READ_SYNC_SETTINGS",
			"READ_SYNC_STATS",
			"REBOOT",
			"RECEIVE_BOOT_COMPLETED",
			"RECEIVE_MMS",
			"RECEIVE_SMS",
			"RECEIVE_WAP_PUSH",
			"RECORD_AUDIO",
			"REORDER_TASKS",
			"RESTART_PACKAGES",
			"SEND_SMS",
			"SET_ACTIVITY_WATCHER",
			"SET_ALWAYS_FINISH",
			"SET_ANIMATION_SCALE",
			"SET_DEBUG_APP",
			"SET_ORIENTATION",
			"SET_PREFERRED_APPLICATIONS",
			"SET_PROCESS_LIMIT",
			"SET_TIME",
			"SET_TIME_ZONE",
			"SET_WALLPAPER",
			"SET_WALLPAPER_HINTS",
			"SIGNAL_PERSISTENT_PROCESSES",
			"STATUS_BAR",
			"SUBSCRIBED_FEEDS_READ",
			"SUBSCRIBED_FEEDS_WRITE",
			"SYSTEM_ALERT_WINDOW",
			"UPDATE_DEVICE_STATS",
			"USE_CREDENTIALS",
			"VIBRATE",
			"WAKE_LOCK",
			"WRITE_APN_SETTINGS",
			"WRITE_CALENDAR",
			"WRITE_CONTACTS",
			"WRITE_EXTERNAL_STORAGE",
			"WRITE_GSERVICES",
			"WRITE_HISTORY_BOOKMARKS",
			"WRITE_OWNER_DATA",
			"WRITE_SECURE_SETTINGS",
			"WRITE_SETTINGS",
			"WRITE_SMS",
			"WRITE_SYNC_SETTINGS"
		};
		
		public static AndroidVersion[] AndroidVersions = new[] {
			new AndroidVersion (4, "1.6"),
			new AndroidVersion (5, "2.0"),
			new AndroidVersion (6, "2.0.1"),
			new AndroidVersion (7, "2.1"),
			new AndroidVersion (8, "2.2"),
		};
	}
	
	public class MonoDroidInstalledCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			return MonoDroidFramework.IsInstalled;
		}
	}
	
	public static class Adb
	{
		public static IEnumerable<MonoDroidDeviceTarget> GetDeviceTargets ()
		{
			//FIXME: implement
			yield break;
		}
	}
	
	public class AndroidVersion
	{
		public AndroidVersion (int apilevel, string osVersion)
		{
			this.ApiLevel = apilevel;
			this.OSVersion = osVersion;
		}
		
		public int ApiLevel { get; private set; }
		public string OSVersion { get; private set; }
		
		public string Label {
			get { return GettextCatalog.GetString ("API Level {0} (Android {1})", ApiLevel, OSVersion); }
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
