//
// PropertyService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MonoDevelop.Core
{
	public static class PropertyService
	{
		readonly static string FileName = "MonoDevelopProperties.xml";
		static Properties properties;

		public readonly static bool IsWindows;
		public readonly static bool IsMac;

		public static Properties GlobalInstance {
			get { return properties; }
		}
		
		public static FilePath EntryAssemblyPath {
			get {
				if (Assembly.GetEntryAssembly () != null)
					return Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location);
				return AppDomain.CurrentDomain.BaseDirectory;
			}
		}
		
		public static FilePath ConfigPath {
			get {
				string configPath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
				return Path.Combine (configPath, "MonoDevelop");
			}
		}
		
		public static FilePath DataPath {
			get {
				string result = System.Configuration.ConfigurationManager.AppSettings ["DataDirectory"];
				if (String.IsNullOrEmpty (result)) 
					result = Path.Combine (EntryAssemblyPath, Path.Combine ("..", "data"));
				return result;
			}
		}
		
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
		
		static PropertyService ()
		{
			Counters.PropertyServiceInitialization.BeginTiming ();
			
			IsWindows = Path.DirectorySeparatorChar == '\\';

			if (!LoadProperties (Path.Combine (ConfigPath, FileName))) {
				if (!LoadProperties (Path.Combine (DataPath, FileName))) {
					properties = new Properties ();
					properties.Set ("MonoDevelop.Core.FirstRun", true);
				}
			}
			properties.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args) {
				if (PropertyChanged != null)
					PropertyChanged (sender, args);
			};
			IsMac = !IsWindows && IsRunningOnMac();
			
			Counters.PropertyServiceInitialization.EndTiming ();
		}
		
		static bool LoadProperties (string fileName)
		{
			properties = null;
			if (File.Exists (fileName)) {
				try {
					properties = Properties.Load (fileName);
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading properties from file '{0}':\n{1}", fileName, ex);
				}
			}
			
			//if it failed and a backup file exists, try that instead
			string backupFile = fileName + ".previous";
			if (properties == null && File.Exists (backupFile)) {
				try {
					properties = Properties.Load (backupFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading properties from backup file '{0}':\n{1}", backupFile, ex);
				}
			}
			return properties != null;
		}
		
		public static void SaveProperties()
		{
			Debug.Assert (properties != null);
			if (!Directory.Exists (ConfigPath))
				Directory.CreateDirectory (ConfigPath);
			properties.Save (Path.Combine (ConfigPath, FileName));
		}
		
		public static T Get<T> (string property, T defaultValue)
		{
			return properties.Get (property, defaultValue);
		}
		
		public static T Get<T> (string property)
		{
			return properties.Get<T> (property);
		}
		
		public static void Set (string key, object val)
		{
			properties.Set (key, val);
		}
		
		public static void AddPropertyHandler (string propertyName, EventHandler<PropertyChangedEventArgs> handler)
		{
			properties.AddPropertyHandler (propertyName, handler);
		}
		
		public static void RemovePropertyHandler (string propertyName, EventHandler<PropertyChangedEventArgs> handler)
		{
			properties.RemovePropertyHandler (propertyName, handler);
		}
		
		public static event EventHandler<PropertyChangedEventArgs> PropertyChanged;
	}
}
