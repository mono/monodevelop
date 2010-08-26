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


namespace MonoDevelop.MonoDroid
{
	public class AndroidAppManifest
	{
		XDocument doc;
		XElement manifest, application;
		XNamespace aNS = "http://schemas.android.com/apk/res/android";
		
		private AndroidAppManifest (XDocument doc)
		{
			this.doc = doc;
			manifest = doc.Root;
			if (manifest.Name != "manifest")
				throw new Exception ("App manifest does not have 'manifest' root element");
			
			application = doc.Root;
			if (application.Name != "application")
				throw new Exception ("App manifest does not have 'application' element");
		}
		
		public static AndroidAppManifest Create (string packageName, string appLabel)
		{
			return new AndroidAppManifest (XDocument.Parse (
@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package="""" android:versionCode=""1"" android:versionName=""1.0"">
  <application android:label="""">
  </application>
  <uses-sdk android:minSdkVersion=""4"" />
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
			MonoDevelop.Projects.Text.TextFile.WriteFile (fileName, doc.ToString (), "UTF8");
		}
		
		public string PackageName {
			get { return (string) manifest.Attribute ("package");  }
			set { manifest.SetAttributeValue ("package", value); }
		}
		
		public string ApplicationLabel {
			get { return (string) application.Attribute (aNS + "label");  }
			set { application.SetAttributeValue (aNS + "label", value); }
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
			var manifest = AndroidAppManifest.Load (filename);
			return manifest.PackageName;
		}
	}
	
	/* COPIED FROM MONODEVELOP.ASPNET */
	
	/// <summary>
	/// Caches items for filename keys. Files may not exist, which doesn't matter.
	/// When a project file with that name is cached in any way, the cache item will be flushed.
	/// </summary>
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

		void FileRenamedInProject (object sender, MonoDevelop.Projects.ProjectFileRenamedEventArgs e)
		{
			cache.Remove (e.OldName);
		}

		void FileChangedInProject (object sender, MonoDevelop.Projects.ProjectFileEventArgs e)
		{
			cache.Remove (e.ProjectFile.Name);
		}
		
		/// <summary>
		/// Queries the cache for an item. If the file does not exist in the project, returns null.
		/// </summary>
		protected U Get (string filename)
		{
			U value;
			if (cache.TryGetValue (filename, out value))
				return value;
			
			var pf = Project.GetProjectFile (filename);
			if (pf != null)
				value = GenerateInfo (filename);
			
			return cache[filename] = value;
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

