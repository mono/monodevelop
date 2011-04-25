// 
// ProjectFileCache.cs
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
using System.Xml.Linq;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.MonoDroid
{
	public class AndroidAppManifest
	{
		XDocument doc;
		XElement manifest, application, usesSdk;
		XNamespace aNS = "http://schemas.android.com/apk/res/android";
		const string aPermPrefix = "android.permission.";
		
		private AndroidAppManifest (XDocument doc)
		{
			this.doc = doc;
			manifest = doc.Root;
			if (manifest.Name != "manifest")
				throw new Exception ("App manifest does not have 'manifest' root element");
			
			application = manifest.Element ("application");
			if (application == null)
				manifest.Add (application = new XElement ("application"));
			
			usesSdk = manifest.Element ("uses-sdk");
			if (usesSdk == null)
				manifest.Add (usesSdk = new XElement ("uses-sdk"));
		}
		
		public static AndroidAppManifest Create (string packageName, string appLabel)
		{
			return new AndroidAppManifest (XDocument.Parse (
@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" android:versionCode=""1"" android:versionName=""1.0"">
  <application android:label="""">
  </application>
  <uses-sdk />
</manifest>")) {
				PackageName = packageName,
				ApplicationLabel = appLabel,
			};
		}
		
		public static AndroidAppManifest Load (FilePath filename)
		{
			var doc = XDocument.Load (filename);
			return new AndroidAppManifest (doc);
		}
		
		public void WriteToFile (FilePath fileName)
		{
			//get the doc to actually write a "utf-8" format
			string text;
			using (var ms = new MemoryStream ()) {
				var xmlSettings = new XmlWriterSettings () {
					Encoding = Encoding.UTF8,
					CloseOutput = false,
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\n",
				};
				using (var writer = XmlTextWriter.Create (ms, xmlSettings))
					doc.Save (writer);
				text = Encoding.UTF8.GetString (ms.GetBuffer (), 0, (int)ms.Length);
			}
			
			//use textfile API because it's safer (writes out to another file then moves)
			MonoDevelop.Projects.Text.TextFile.WriteFile (fileName, text, "utf-8");
		}

		static string NullIfEmpty (string value)
		{
			return string.IsNullOrEmpty (value) ? null : value;
		}
		
		public string PackageName {
			get { return (string) manifest.Attribute ("package");  }
			set { manifest.SetAttributeValue ("package", NullIfEmpty (value)); }
		}
		
		public string ApplicationLabel {
			get { return (string) application.Attribute (aNS + "label");  }
			set { application.SetAttributeValue (aNS + "label", NullIfEmpty (value)); }
		}
		
		public string ApplicationIcon {
			get { return (string) application.Attribute (aNS + "icon");  }
			set { application.SetAttributeValue (aNS + "icon", NullIfEmpty (value)); }
		}
		
		public string VersionName {
			get { return (string) manifest.Attribute (aNS + "versionName");  }
			set { manifest.SetAttributeValue (aNS + "versionName", NullIfEmpty (value)); }
		}
		
		public string VersionCode {
			get { return (string) manifest.Attribute (aNS + "versionCode");  }
			set { manifest.SetAttributeValue (aNS + "versionCode", NullIfEmpty (value)); }
		}

		public string InstallLocation {
			get { return (string) manifest.Attribute (aNS + "installLocation"); }
			set { manifest.SetAttributeValue (aNS + "installLocation", NullIfEmpty (value)); }
		}
		
		public int? MinSdkVersion {
			get { return (int?) usesSdk.Attribute (aNS + "minSdkVersion");  }
			set { usesSdk.SetAttributeValue (aNS + "minSdkVersion", value); }
		}
		
		public IEnumerable<string> AndroidPermissions {
			get {
				var aName = aNS + "name";
				foreach (var el in manifest.Elements ("uses-permission")) {
					var name = (string) el.Attribute (aName);
					if (name != null && name.StartsWith (aPermPrefix) && name.Length > aPermPrefix.Length)
						yield return name.Substring (aPermPrefix.Length);
				}
			}
		}
		
		public void SetAndroidPermissions (IEnumerable<string> permissions)
		{
			var newPerms = new HashSet<string> (permissions);
			var current = new HashSet<string> (AndroidPermissions);
			AddAndroidPermissions (newPerms.Except (current));
			RemoveAndroidPermissions (current.Except (newPerms));
		}
		
		void AddAndroidPermissions (IEnumerable<string> permissions)
		{
			var aName = aNS + "name";
			var newElements = permissions.Select (p =>
				new XElement ("uses-permission", new XAttribute (aName, aPermPrefix + p)));
			
			var lastPerm = manifest.Elements ("uses-permission").LastOrDefault ();
			if (lastPerm != null) {
				foreach (var el in newElements) {
					lastPerm.AddAfterSelf (el);
					lastPerm = el;
				}
			} else {
				foreach (var el in newElements)
					manifest.Add (el);
			}
		}
		
		void RemoveAndroidPermissions (IEnumerable<string> permissions)
		{
			var aName = aNS + "name";
			var perms = new HashSet<string> (permissions.Select (p => aPermPrefix + p));
			var list = manifest.Elements ("uses-permission")
				.Where (el => perms.Contains ((string)el.Attribute (aName))).ToList ();
			foreach (var el in list)
				el.Remove ();
		}

		public string GetLaunchableActivityName ()
		{
			var aName = aNS + "name";
			foreach (var activity in application.Elements ("activity")) {
				var filter = activity.Element ("intent-filter");
				if (filter != null) {
					foreach (var category in filter.Elements ("category"))
						if (category != null && (string)category.Attribute (aName) == "android.intent.category.LAUNCHER")
							return (string) activity.Attribute (aName);
				}
			}
			return null;
		}
	}
	
	class AndroidPackageNameCache : ProjectFileCache<MonoDroidProject,string>
	{
		public AndroidPackageNameCache (MonoDroidProject project) : base (project)
		{
		}
		
		public string GetPackageName (string manifestFileName)
		{
			return Get (manifestFileName);
		}
		
		protected override string GenerateInfo (string filename)
		{
			try {
				var manifest = AndroidAppManifest.Load (filename);
				return manifest.PackageName;
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading android manifest '" + filename + "'", ex);
				return null;
			}
		}
	}
	
	/* COPIED FROM MONODEVELOP.ASPNET AND MODIFIED */
	
	/// <summary>
	/// Caches file-derived values for filename keys.
	/// </summary>
	/// <description>
	/// Used to cache values extracted from files. Subclasses of this class extract the values
	/// on request. Requested files may not exist, in which case the returned value is the default/null value.
	/// When a project file with that name is changed in any way, the cache item will be flushed.
	/// The files in the cache do not need to be in the project, but if not, changes to the files will not be tracked.
	/// </description>
	/// <remarks>Not safe for multithreaded access.</remarks>
	abstract class ProjectFileCache<T,U> : IDisposable
		where T : MonoDevelop.Projects.Project
	{
		protected T Project { get; private set; }
		
		Dictionary<string, U> cache;
		
		/// <summary>Creates a ProjectFileCache</summary>
		/// <param name="project">The project the cache is bound to</param>
		public ProjectFileCache (T project)
		{
			this.Project = project;
			cache =  new Dictionary<string, U> ();
			Project.FileChangedInProject += FileChangedInProject;
			Project.FileRemovedFromProject += FileChangedInProject;
			Project.FileAddedToProject += FileChangedInProject;
			Project.FileRenamedInProject += FileRenamedInProject;
		}

		void FileRenamedInProject (object sender, ProjectFileRenamedEventArgs args)
		{
			foreach (ProjectFileRenamedEventInfo e in args)
				cache.Remove (e.OldName);
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileRenamedEventInfo e in args)
				cache.Remove (e.ProjectFile.Name);
		}
		
		/// <summary>
		/// Queries the cache for an item. If the file does not exist, returns default/null.
		/// </summary>
		protected U Get (string filename)
		{
			U value;
			if (cache.TryGetValue (filename, out value))
				return value;
			
			if (File.Exists (filename))
				return cache[filename] = GenerateInfo (filename);
			
			return cache[filename] = default (U);
		}
		
		/// <summary>
		/// Detaches from the project's events.
		/// </summary>
		public void Dispose ()
		{
			Project.FileChangedInProject -= FileChangedInProject;
			Project.FileRemovedFromProject -= FileChangedInProject;
			Project.FileAddedToProject -= FileChangedInProject;
			Project.FileRenamedInProject -= FileRenamedInProject;
		}
		
		/// <summary>
		/// Generates info for a given filename.
		/// </summary>
		/// <returns>Null if no info could be generated for the requested filename, e.g. if it did not exist.</returns>
		protected abstract U GenerateInfo (string filename);
	}
}

