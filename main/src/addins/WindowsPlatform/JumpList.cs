// 
// MyClass.cs
//  
// Author:
//       Steven Schermerhorn <stevens+monoaddins@ischyrus.com>
// 
// Copyright (c) 2011 sscherm
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
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Desktop;
using Taskbar = Microsoft.WindowsAPICodePack.Taskbar;
using System.Timers;

namespace MonoDevelop.Platform
{
	public class JumpList : CommandHandler
	{
		private const string progId = "MonoDevelop";
		private const string appId = "MonoDevelop";
		
		private IList<string> supportedExtensions;
		private RecentFiles recentFiles;

		protected override void Run ()
		{
			bool enable = Taskbar.TaskbarManager.IsPlatformSupported && this.CheckRegistration ();
			
			if (!enable) {
				return;
			}
			
			Taskbar.TaskbarManager.Instance.ApplicationId = progId;
			this.recentFiles = DesktopService.RecentFiles;
			this.UpdateJumpList();
		}
		
		private void UpdateJumpList()
		{
			try{
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
					if (isSupportedFileExtension){
						recentProjectsCategory.AddJumpListItems (new Taskbar.JumpListItem (recentProject.FileName));
					}
				}
				
				foreach (RecentFile recentFile in recentFiles.GetFiles ()) {
					if (this.supportedExtensions.Contains (Path.GetExtension (recentFile.FileName)))
						recentFilesCategory.AddJumpListItems (new Taskbar.JumpListItem (recentFile.FileName));
				}
				
				
				jumplist.Refresh ();
			} catch{
				// There are ocassional issues with Windows not liking the Window handle being provided.
				// The exception occurs while the WindowsAPICodePack library is attempting to set the Window's id.
				// The jumplist still gets updated and I wasn't able to track down what the exact issue is as
				// it seems to fail at random.
				
				// It also only fails on subsequent updates, which means that when the app enters this state the
				// jumplist won't be updated until the app is launched again.
			}
		}
		
		private bool CheckRegistration ()
		{
			// The supportedExtensions can only contain extensions that monodevelop has a registry entry under
			// HKCR/[extension]/OpenWithProgIds. This is important because a JumpListEntry can only reference
			// files that have this entry.
			this.supportedExtensions = new List<string>();
			bool reregister = false;
			
			string exePath = Process.GetCurrentProcess ().MainModule.FileName;
			string executeString = exePath + " %1";
			
			bool registered = false;
			List<string> extensionsToRegister = new List<string>();
			
			try {
				IEnumerable<string> knownExtensions = GetDefaultExtensions();
				foreach (string extension in knownExtensions) {
					RegistryKey openWithKey = Registry.ClassesRoot.OpenSubKey (Path.Combine (extension, "OpenWithProgIds"));
					if (openWithKey == null) {
						// No such extension is in the registry
						continue;
					}
					
					this.supportedExtensions.Add(extension);
					
					string value = openWithKey.GetValue ("MonoDevelop", null) as string;
					if (value == null) {
						extensionsToRegister.Add (extension);
					}
				}
				
				// Verify that the class entry for monodevelop is present AND correct
				RegistryKey progIdKey = Registry.ClassesRoot.OpenSubKey (progId + @"\shell\Open\Command", false);
				// This path needs to map to where monodevelop.exe or JumpLists won't work.
				object path = progIdKey.GetValue (String.Empty);
				reregister = !executeString.Equals (path);
			} finally {
				bool someRegistrationRequired = extensionsToRegister.Count > 0 || reregister;
				if (someRegistrationRequired) {
					registered = DoRegister (executeString, extensionsToRegister);
				} else {
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
			
			// Normal file extension filters can be registered. (.sln, .cs, etc) Something like 'mono_*' shouldn't be. 
			// To get the general idea, the file extension needs to be listed in HKCR. 
			// Because we are dealing with a file filter rather than a simple file extension list we can
			// help narrow the list down, only filters that start with '*.', there will be another check later
			// that will only register the extension if it already exists in HKCR so a more aggresive check isn't needed.
			List<string> usableExtensions = new List<string> (extensionSplit.Length);
			foreach (string extension in extensionSplit) {
				const string requiredFilterPattern = "*.";
				if (extension.Length > 2 && extension.StartsWith (requiredFilterPattern)) {
					// Substring(1) to skip over the '*'.
					usableExtensions.Add (extension.Substring (1));
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
			} catch(System.Exception ex) {
				LoggingService.LogError("Unable to initialize configuraiton for Windows 7 " +
				                        "taskbar integration. Try starting MonoDevelop as" +
				                        " administrator once.", ex);
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

