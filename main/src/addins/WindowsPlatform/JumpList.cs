// 
// JumpList.cs
//  
// Author:
//       Steven Schermerhorn <stevens+monoaddins@ischyrus.com>
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using Microsoft.Win32;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Desktop;
using Taskbar = Microsoft.WindowsAPICodePack.Taskbar;

namespace MonoDevelop.Platform
{
	public class JumpList : CommandHandler
	{
		private const string progId = "MonoDevelop";
		private const string appId = "MonoDevelop";

		private IList<string> supportedExtensions;
		private RecentFiles recentFiles;
		private Timer updateTimer;

		protected override void Run ()
		{
			bool isWindows7 = Taskbar.TaskbarManager.IsPlatformSupported;
			if (!isWindows7) {
				return;
			}
			
			bool areFileExtensionsRegistered = this.CheckRegistration ();
			
			if (!areFileExtensionsRegistered) {
				return;
			}
			
			this.updateTimer = new Timer (3000);
			this.updateTimer.Elapsed += this.OnUpdateTimerEllapsed;
			this.updateTimer.AutoReset = false;
			
			Taskbar.TaskbarManager.Instance.ApplicationId = progId;
			this.recentFiles = DesktopService.RecentFiles;
			this.recentFiles.Changed += this.OnRecentFilesChanged;
			this.UpdateJumpList ();
		}

		private void OnRecentFilesChanged (object sender, EventArgs args)
		{
			// This event fires several times for a single change. Rather than performing the update
			// several times we will restart the timer which has a 3 second delay on it. 
			// While this means the update won't make it to the JumpList immediately it is significantly
			// better for performance.
			this.updateTimer.Stop ();
			this.updateTimer.Start ();
		}

		private void OnUpdateTimerEllapsed (object sender, EventArgs args)
		{
			this.UpdateJumpList ();
		}

		private void UpdateJumpList ()
		{
			Taskbar.JumpList jumplist = Taskbar.JumpList.CreateJumpList ();
			jumplist.KnownCategoryToDisplay = Taskbar.JumpListKnownCategoryType.Neither;
			
			Taskbar.JumpListCustomCategory recentProjectsCategory = new Taskbar.JumpListCustomCategory ("Recent Solutions");
			Taskbar.JumpListCustomCategory recentFilesCategory = new Taskbar.JumpListCustomCategory ("Recent Files");
			
			jumplist.AddCustomCategories (recentProjectsCategory, recentFilesCategory);
			jumplist.KnownCategoryOrdinalPosition = 0;
			
			foreach (RecentFile recentProject in recentFiles.GetProjects ()) {
				// Windows is picky about files that are added to the jumplist. Only files that MonoDevelop
				// has been registered as supported in the registry can be added.
				bool isSupportedFileExtension = this.supportedExtensions.Contains (Path.GetExtension (recentProject.FileName));
				if (isSupportedFileExtension) {
					recentProjectsCategory.AddJumpListItems (new Taskbar.JumpListItem (recentProject.FileName));
				}
			}
			
			foreach (RecentFile recentFile in recentFiles.GetFiles ()) {
				if (this.supportedExtensions.Contains (Path.GetExtension (recentFile.FileName)))
					recentFilesCategory.AddJumpListItems (new Taskbar.JumpListItem (recentFile.FileName));
			}
			
			jumplist.Refresh ();
		}

		private bool CheckRegistration ()
		{
			// There are two 'types' of registry keys required for JumpLists to work.
			//
			// The first is the 'progid' entry. MonoDevelop's progid is 'MonoDevelop', thus it's progid registry entry
			// is '/HKCR/MonoDevelop'. This entry contains mostly useless information except for
			// /HKCR/MonoDevelop/shell/Open/Command where the value must be "c:\path\to\MonoDevelop.exe %1".
			// This command is used when the user selects one of the JumpList items.
			//
			// The second key is to identify which file extensions MonoDevelop can handle. If you open up the 
			// registry and look at HKEY_CLASS_ROOT you'll see a comprehensive set of entries for extensions.
			// Inside of each is the subkey OpenWithProgids. 'MonoDevelop' needs to be added for 
			// each file extension that is in the JumpList. Attempting to add a file to the JumpList that has
			// an extension not registered will throw an exception. 
			//
			// I believe a side-effect of this requirement
			// is that MonoDevelop will appear in the "Open With" list by default for these types.
			this.supportedExtensions = new List<string> ();
			bool reregister = false;
			
			// Determine the correct value for /HKCR/MonoDevelop/shell/Open/Command
			string exePath = Process.GetCurrentProcess ().MainModule.FileName;
			string executeString = exePath + " %1";
			
			bool registered = false;
			List<string> extensionsToRegister = new List<string> ();
			
			try {
				// Enumerate all of the extensions to make sure MonoDevelop is registered
				IEnumerable<string> knownExtensions = GetDefaultExtensions ();
				foreach (string extension in knownExtensions) {
					RegistryKey openWithKey = Registry.ClassesRoot.OpenSubKey (Path.Combine (extension, "OpenWithProgIds"));
					if (openWithKey == null) {
						// No such extension is in the registry
						continue;
					}
					
					this.supportedExtensions.Add (extension);
					
					string value = openWithKey.GetValue ("MonoDevelop", null) as string;
					if (value == null) {
						extensionsToRegister.Add (extension);
					}
				}
				
				// Verify that the class entry for monodevelop is present AND correct.
				RegistryKey progIdKey = Registry.ClassesRoot.OpenSubKey (progId + @"\shell\Open\Command", false);
				object path = progIdKey.GetValue (String.Empty);
				reregister = !executeString.Equals (path);
			} finally {
				bool someRegistrationRequired = extensionsToRegister.Count > 0 || reregister;
				if (someRegistrationRequired) {
					registered = DoRegister (executeString, extensionsToRegister);
				} else {
					// All the registry entries look correct.
					registered = true;
				}
			}
			
			return registered;
		}

		private static IEnumerable<string> GetDefaultExtensions ()
		{
			Dictionary<string, string> observedExtensions = new Dictionary<string, string> ();
			foreach (string filter in AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectFileFilters")) {
				foreach (string extension in ParseFilter (filter)) {
					if (!observedExtensions.ContainsKey (extension)) {
						observedExtensions.Add (extension, extension);
						yield return extension;
					}
				}
			}
			
			foreach (string filter in AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/FileFilters")) {
				foreach (string extension in ParseFilter (filter)) {
					if (!observedExtensions.ContainsKey (extension)) {
						observedExtensions.Add (extension, extension);
						yield return extension;
					}
				}
			}
		}

		private static IList<string> ParseFilter (string filter)
		{
			string[] friendlyNameSplit = filter.Split (new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (friendlyNameSplit == null || friendlyNameSplit.Length != 2) {
				return new string[0];
			}
			
			string[] extensionSplit = friendlyNameSplit[1].Split (new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			
			List<string> usableExtensions = new List<string> (extensionSplit.Length);
			foreach (string extension in extensionSplit) {
				const string requiredFilterPattern = "*.";
				// The pattern must start with '*.' and not have more one '*'.
				if (extension.StartsWith (requiredFilterPattern) && extension.IndexOf('*', 1) == -1) {
					usableExtensions.Add(extension.Substring(1));
				}
			}
			return usableExtensions;
		}

		private static bool DoRegister (string executeCommandPattern, IList<string> extensions)
		{
			try {
				//First of all, unregister:
				foreach (string extension in extensions) {
					UnregisterFileAssociation (progId, extension);
				}
				UnregisterProgId (progId);
				
				// Then register
				foreach (string extension in extensions) {
					RegisterFileAssociation (progId, extension);
				}
				RegisterProgId (progId, appId, executeCommandPattern);
				
				return true;
			} catch (System.Exception ex) {
				LoggingService.LogError ("Unable to initialize configuration for Windows 7 " + "taskbar integration. Try starting MonoDevelop as" + " administrator once.", ex);
				return false;
			}
		}

		private static void RegisterProgId (string progId, string appId, string openWith)
		{
			RegistryKey progIdKey = Registry.ClassesRoot.CreateSubKey (progId);
			progIdKey.SetValue ("FriendlyTypeName", "@shell32.dll,-8975");
			progIdKey.SetValue ("DefaultIcon", "@shell32.dll,-47");
			progIdKey.SetValue ("CurVer", progId);
			progIdKey.SetValue ("AppUserModelID", appId);
			
			RegistryKey shell = progIdKey.CreateSubKey ("shell");
			shell.SetValue (String.Empty, "Open");
			shell = shell.CreateSubKey ("Open");
			shell = shell.CreateSubKey ("Command");
			shell.SetValue (String.Empty, openWith);
			
			shell.Close ();
			progIdKey.Close ();
		}

		private static void UnregisterProgId (string progId)
		{
			try {
				Registry.ClassesRoot.DeleteSubKeyTree (progId);
			} catch {
			}
		}

		private static void RegisterFileAssociation (string progId, string extension)
		{
			RegistryKey openWithKey = Registry.ClassesRoot.CreateSubKey (Path.Combine (extension, "OpenWithProgIds"));
			openWithKey.SetValue (progId, String.Empty);
			openWithKey.Close ();
		}

		private static void UnregisterFileAssociation (string progId, string extension)
		{
			try {
				RegistryKey openWithKey = Registry.ClassesRoot.CreateSubKey (Path.Combine (extension, "OpenWithProgIds"));
				openWithKey.DeleteValue (progId);
				openWithKey.Close ();
			} catch {
			}
		}
	}
}

