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
using System.Runtime.InteropServices;
using System.Xml;

namespace MonoDevelop.Core
{
	/// <summary>
	/// The Property wrapper wraps a global property service value as an easy to use object.
	/// </summary>
	public class PropertyWrapper<T>
	{
		T value;
		readonly string propertyName;

		public T Value {
			get {
				return value;
			}
			set {
				Set (value);
			}
		}

		/// <summary>
		/// Set the property to the specified value.
		/// </summary>
		/// <param name='newValue'>
		/// The new value.
		/// </param>
		/// <returns>
		/// true, if the property has changed, false otherwise.
		/// </returns>
		public bool Set (T newValue)
		{
			if (!object.Equals (this.value, newValue)) {
				this.value = newValue;
				PropertyService.Set (propertyName, value);
				OnChanged (EventArgs.Empty);
				return true;
			}
			return false;
		}

		public PropertyWrapper (string propertyName, T defaultValue)
		{
			this.propertyName = propertyName;
			value = PropertyService.Get (propertyName, defaultValue);
		}

		public static implicit operator T (PropertyWrapper<T> watch)
		{
			return watch.value;
		}

		protected virtual void OnChanged (EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler Changed;
	}

	public static class PropertyService
	{
		public static PropertyWrapper<T> Wrap<T> (string property, T defaultValue)
		{
			return new PropertyWrapper<T> (property, defaultValue);
		}

		//force the static class to intialize
		internal static void Initialize ()
		{

		}
		readonly static string FileName = "MonoDevelopProperties.xml";
		static Properties properties;

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
		
		/// <summary>
		/// Location of data files that are bundled with MonoDevelop itself.
		/// </summary>
		public static FilePath DataPath {
			get {
				string result = System.Configuration.ConfigurationManager.AppSettings ["DataDirectory"];
				if (String.IsNullOrEmpty (result)) 
					result = Path.Combine (EntryAssemblyPath, Path.Combine ("..", "data"));
				return result;
			}
		}
		
		static PropertyService ()
		{
			Counters.PropertyServiceInitialization.BeginTiming ();
			
			string migrateVersion = null;
			UserProfile migratableProfile = null;
			
			var prefsPath = UserProfile.Current.ConfigDir.Combine (FileName);
			if (!File.Exists (prefsPath)) {
				if (GetMigratableProfile (out migratableProfile, out migrateVersion)) {
					FilePath migratePrefsPath = migratableProfile.ConfigDir.Combine (FileName);
					try {
						var parentDir = prefsPath.ParentDirectory;
						//can't use file service until property service is initialized
						if (!Directory.Exists (parentDir))
							Directory.CreateDirectory (parentDir);
						File.Copy (migratePrefsPath, prefsPath);
						LoggingService.LogInfo ("Migrated core properties from {0}", migratePrefsPath);
					} catch (IOException ex) {
						string message = string.Format ("Failed to migrate core properties from {0}", migratePrefsPath);
						LoggingService.LogError (message, ex);
					}
				} else {
					LoggingService.LogInfo ("Did not find previous version from which to migrate data");	
				}
			}
			
			if (!LoadProperties (prefsPath)) {
				properties = new Properties ();
				properties.Set ("MonoDevelop.Core.FirstRun", true);
			}
			
			if (migratableProfile != null)
				UserDataMigrationService.SetMigrationSource (migratableProfile, migrateVersion);
			
			properties.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args) {
				if (PropertyChanged != null)
					PropertyChanged (sender, args);
			};
			
			Counters.PropertyServiceInitialization.EndTiming ();
		}
		
		internal static bool GetMigratableProfile (out UserProfile profile, out string version)
		{
			profile = null;
			version = null;
			
			//try versioned profiles from most recent to oldest
			//skip the last in the array, it's the current profile
			int userProfileMostRecent = UserProfile.ProfileVersions.Length - 2;
			for (int i = userProfileMostRecent; i >= 1; i--) {
				string v = UserProfile.ProfileVersions[i];
				var p = UserProfile.GetProfile (v);
				if (File.Exists (p.ConfigDir.Combine (FileName))) {
					profile = p;
					version = v;
					return true;
				}
			}
			
			//try the old unversioned MD <= 2.4 profile
			if (BrandingService.ProfileDirectoryName == "MonoDevelop") {
				var md24 = UserProfile.ForMD24 ();
				if (File.Exists (md24.ConfigDir.Combine (FileName))) {
					profile = md24;
					version = "2.4";
					return true;
				}
			}

			return false;
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
		
		public static void SaveProperties ()
		{
			Debug.Assert (properties != null);
			var prefsPath = UserProfile.Current.ConfigDir.Combine (FileName);
			FileService.EnsureDirectoryExists (prefsPath.ParentDirectory);
			properties.Save (prefsPath);
		}
		
		public static bool HasValue (string property)
		{
			return properties.HasValue (property);
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
