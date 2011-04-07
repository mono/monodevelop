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
using System.Xml;
using System.Xml.Linq;
using Mono.Addins;
using MonoDevelop.Core;
using System.Text;
using System.Runtime.InteropServices;
using MonoDroid;
using MonoDevelop.Ide;

namespace MonoDevelop.MonoDroid
{
	public static class MonoDroidFramework
	{
		static bool? isTrial;
		
		static MonoDroidFramework ()
		{
			EnvironmentOverrides = new Dictionary<string, string> ();
			DeviceManager = new DeviceManager ();
			VirtualDeviceManager = new VirtualDeviceManager ();
			UpdateSdkLocations ();
		}
		
		internal static void UpdateSdkLocations ()
		{
			try {
				var oldAndroidBinDir = AndroidBinDir;
				
				MonoDroidToolsDir = MonoDroidFrameworkDir = AndroidBinDir = JavaBinDir = null;
				Toolbox = null;
				EnvironmentOverrides.Remove ("PATH");
				
				string monoDroidToolsDir, monoDroidFrameworkDir, javaPath, androidPath;
				MonoDroidSdk.GetPaths (out monoDroidToolsDir, out monoDroidFrameworkDir, out androidPath, out javaPath,
					new Microsoft.Build.Utilities.TaskLoggingHelper ());
				
				if (monoDroidToolsDir == null) {
					LoggingService.LogInfo ("Mono for Android SDK not found, disabling Mono for Android addin");
					return;
				}
				
				MonoDroidToolsDir = monoDroidToolsDir;
				MonoDroidFrameworkDir = monoDroidFrameworkDir;
				
				if (androidPath == null) {
					LoggingService.LogError ("Android SDK not found, needed by Mono for Android addin");
					return;
				}
				
				if (javaPath == null) {
					LoggingService.LogError ("Java SDK not found, needed by Mono for Android addin");
					return;
				}
				
				JavaBinDir = Path.Combine (javaPath, "bin");
				AndroidBinDir = androidPath;
				
				EnvironmentOverrides ["PATH"] =
					AndroidBinDir + Path.PathSeparator + 
					JavaBinDir + Path.PathSeparator + 
					Environment.GetEnvironmentVariable ("PATH");
				
				Toolbox = new AndroidToolbox (AndroidBinDir, JavaBinDir);
				
				if (oldAndroidBinDir != AndroidBinDir)
					DeviceManager.AndroidSdkChanged ();
				
			} catch (Exception ex) {
				LoggingService.LogError ("Error detecting Mono for Android SDK", ex);
			}
		}
		
		
		
		/// <summary>
		/// Ensures all required SDKs are installed. If not, prompts the user to select the locations.
		/// </summary>
		/// <returns>True if the location is configured or the user selects a valid location.</returns>
		public static bool EnsureSdksInstalled ()
		{
			if (HasAndroidJavaSdks)
				return true;
			
			var dialog = new MonoDevelop.MonoDroid.Gui.MonoDroidSdkSettingsDialog ();
			try {
				int response = dialog.Run ();
				if (response == (int)Gtk.ResponseType.Ok)
					dialog.ApplyChanges ();
			} finally {
				dialog.Destroy ();
			}
			
			return HasAndroidJavaSdks;
		}
		
		/// <summary>
		/// Whether the MonoDroid SDK has been detected.
		/// </summary>
		public static bool IsInstalled {
			get {
				return !MonoDroidFrameworkDir.IsNullOrEmpty;
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
		public static FilePath MonoDroidToolsDir { get; private set; }
		
		/// <summary>
		/// Directory with MonoDroid framework assemblies.
		/// </summary>
		public static FilePath MonoDroidFrameworkDir { get; private set; }
		
		/// <summary>
		/// Bin directory of the Java SDK.
		/// </summary>
		public static FilePath JavaBinDir { get; private set; }
		
		/// <summary>
		/// Tools directory of the Android SDK.
		/// </summary>
		public static FilePath AndroidBinDir { get; private set; }
		
		static string MandroidPath {
			get {
				string toolsDir = MonoDroidToolsDir;
				if (PropertyService.IsMac && toolsDir == "/Developer/MonoAndroid/usr/lib/mandroid")
					return "/Developer/MonoAndroid/usr/bin/mandroid";
				return Path.Combine (toolsDir, "mandroid.exe");
			}
		}
		
		/// <summary>
		/// Environment variables to be used when invoking MonoDroid tools.
		/// </summary>
		public static Dictionary<string,string> EnvironmentOverrides { get; private set; }
		
		public static FilePath SharedRuntimePackage {
			get {
				return MonoDroidToolsDir.Combine ("Mono.Android.DebugRuntime-debug.apk");
			}
		}

		public static int FrameworkVersionToApiLevel (string frameworkVersion)
		{
			foreach (AndroidVersion version in AndroidVersions)
				if (version.OSVersion == frameworkVersion)
					return version.ApiLevel;

			throw new ArgumentOutOfRangeException ("Framework version not recognized: " + frameworkVersion);
		}

		public static FilePath GetPlatformPackage (int apiLevel)
		{
			return MonoDroidToolsDir.Combine ("platforms", "android-" + apiLevel, "Mono.Android.Platform.apk");
		}

		public static int GetRuntimeVersion ()
		{
			var doc = XDocument.Load (MonoDroidToolsDir.Combine ("Mono.Android.DebugRuntime-debug.xml"));
			var version = doc.Element ("manifest").Attribute ("{http://schemas.android.com/apk/res/android}versionCode");
			return int.Parse (version.Value);
		}

		public static IEnumerable<string> GetToolsPaths ()
		{
			yield return MonoDroidFramework.MonoDroidFrameworkDir;
			yield return MonoDroidFramework.MonoDroidToolsDir;
			yield return MonoDroidFramework.AndroidBinDir;
			yield return MonoDroidFramework.JavaBinDir;
		}
		
		public static AndroidToolbox Toolbox { get; private set; }
		public static DeviceManager DeviceManager { get; private set; }
		public static VirtualDeviceManager VirtualDeviceManager { get; private set; }
		
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
			new AndroidVersion (10, "2.3"),
		};
		
		public static AndroidVersion DefaultAndroidVersion {
			get { return AndroidVersions[AndroidVersions.Length-2]; } // 2.2
		}
		
		public static bool IsTrial {
			get {
				if (isTrial.HasValue)
					return isTrial.Value;
				
				System.Diagnostics.Process prc = null;
				try {
					prc = System.Diagnostics.Process.Start (
						new System.Diagnostics.ProcessStartInfo (MandroidPath, "--activated") {
							UseShellExecute = false,
					});
					prc.WaitForExit (5000);
					isTrial = prc.ExitCode != 0;
				} catch (Exception ex) {
					LoggingService.LogError ("Error checking Mono for Android activation status", ex);
					isTrial = true;
				} finally {
					if (prc != null)
						prc.Dispose ();
				}
				return isTrial.Value;
			}
		}
		
		public static bool Activate ()
		{
			System.Diagnostics.Debug.Assert (IsTrial);
			if (PropertyService.IsMac) {
				string downloadUrl = "http://mono-android.net/";
				System.Diagnostics.Process.Start (downloadUrl);
				return false;
			}
			
			//on windows we can activate in place
			System.Diagnostics.Process prc = null;
			try {
				prc = System.Diagnostics.Process.Start (
					new System.Diagnostics.ProcessStartInfo (MandroidPath, "--activate") {
						UseShellExecute = false,
				});
				prc.WaitForExit ();
				isTrial = prc.ExitCode != 0;
			} catch (Exception ex) {
				LoggingService.LogError ("Error activating Mono for Android", ex);
				isTrial = true;
			} finally {
				if (prc != null)
					prc.Dispose ();
			}
			return isTrial.Value;
		}
		
		public static bool CheckTrial ()
		{
			if (!IsTrial)
				return false;
			MonoDroidUtility.InvokeSynch (ShowEvalDialog);
			return IsTrial;
		}
		
		static void ShowEvalDialog ()
		{
			string evalTitle = GettextCatalog.GetString ("Evaluation Version");
			string evalHeader = GettextCatalog.GetString ("Feature Not Available In Evaluation Version");
			string evalMessage = GettextCatalog.GetString (
				"Upgrade to the full version of Mono for Android to deploy\n" +
				"to devices, and to enable your applications to be distributed.");
			string continueMessage = GettextCatalog.GetString ("Continue evaluation");
			
			var dialog = new Gtk.Dialog () {
				Title = evalTitle,
			};
			
			dialog.VBox.PackStart (
			 	new Gtk.Label ("<b><big>" + evalHeader + "</big></b>") {
					Xalign = 0.5f,
					UseMarkup = true
				}, true, false, 12);
			
			var align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			align.Add (new Gtk.Label (evalMessage) {
					Xalign = 0.5f,
					Justify = Gtk.Justification.Center
				});
			
			align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			
			string activateMessage;
			if (PropertyService.IsWindows) {
				activateMessage = GettextCatalog.GetString ("Activate Mono for Android");
			} else {
				activateMessage = GettextCatalog.GetString ("Buy Mono for Android");
			}
			
			var buyButton = new Gtk.Button (new Gtk.Label ("<big>" + activateMessage + "</big>") { UseMarkup = true } );
			buyButton.Clicked += delegate {
				Activate ();
				dialog.Respond (Gtk.ResponseType.Accept);
			};
			align.Add (buyButton);
			
			dialog.AddButton (continueMessage, Gtk.ResponseType.Close);
			dialog.ShowAll ();
			
			MessageService.ShowCustomDialog (dialog);
		}
	}
	
	public class MonoDroidInstalledCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			return MonoDroidFramework.IsInstalled;
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
}

//dummy implementation of Microsoft.Build.Utilities.TaskLoggingHelper
//so we can use MonoDroidSdk without a dep on MSBuild
namespace Microsoft.Build.Utilities
{
	class TaskLoggingHelper
	{
		public void LogMessage (string message)
		{
		}
		
		public void LogMessage (string format, object arg0)
		{
		}
		
		public void LogMessage (string format, object arg0, object arg1)
		{
		}
		
		public void LogMessage (string format, params object[] args)
		{
		}
	}
}