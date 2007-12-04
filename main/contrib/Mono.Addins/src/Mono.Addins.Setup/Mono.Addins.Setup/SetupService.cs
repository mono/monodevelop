//
// SetupService.cs
//
// Author:
//   Lluis Sanchez Gual
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
using System.Xml;
using System.IO;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Addins.Description;
using Mono.Addins.Setup.ProgressMonitoring;

namespace Mono.Addins.Setup
{
	public class SetupService
	{
		RepositoryRegistry repositories;
		string applicationNamespace;
		string installDirectory;
		AddinStore store;
		AddinSystemConfiguration config;
		
		AddinRegistry registry;
		
		public SetupService ()
		{
			if (AddinManager.IsInitialized)
				registry = AddinManager.Registry;
			else
				registry = AddinRegistry.GetGlobalRegistry ();
			
			repositories = new RepositoryRegistry (this);
			store = new AddinStore (this);
		}
		
		public SetupService (AddinRegistry registry)
		{
			this.registry = registry;
			repositories = new RepositoryRegistry (this);
			store = new AddinStore (this);
		}
		
		public AddinRegistry Registry {
			get { return registry; }
		}
		
		internal string RepositoryCachePath {
			get { return Path.Combine (registry.RegistryPath, "repository-cache"); }
		}
		
		string RootConfigFile {
			get { return Path.Combine (registry.RegistryPath, "addins-setup.config"); }
		}
		
		public string ApplicationNamespace {
			get { return applicationNamespace; }
			set { applicationNamespace = value; }
		}
		
		public string InstallDirectory {
			get {
				if (installDirectory != null && installDirectory.Length > 0)
					return installDirectory;
				else
					return registry.DefaultAddinsFolder;
			}
			set { installDirectory = value; }
		}
		
		public RepositoryRegistry Repositories {
			get { return repositories; }
		}
		
		internal AddinStore Store {
			get { return store; }
		}
		
		public bool ResolveDependencies (IProgressStatus statusMonitor, AddinRepositoryEntry[] addins, out PackageCollection resolved, out PackageCollection toUninstall, out DependencyCollection unresolved)
		{
			return store.ResolveDependencies (statusMonitor, addins, out resolved, out toUninstall, out unresolved);
		}
		
		public bool ResolveDependencies (IProgressStatus statusMonitor, PackageCollection packages, out PackageCollection toUninstall, out DependencyCollection unresolved)
		{
			return store.ResolveDependencies (statusMonitor, packages, out toUninstall, out unresolved);
		}
		
		public bool Install (IProgressStatus statusMonitor, params string[] files)
		{
			return store.Install (statusMonitor, files);
		}
		
		public bool Install (IProgressStatus statusMonitor, params AddinRepositoryEntry[] addins)
		{
			return store.Install (statusMonitor, addins);
		}
		
		public bool Install (IProgressStatus statusMonitor, PackageCollection packages)
		{
			return store.Install (statusMonitor, packages);
		}
		
		public void Uninstall (IProgressStatus statusMonitor, string id)
		{
			store.Uninstall (statusMonitor, id);
		}
		
		public static AddinHeader GetAddinHeader (Addin addin)
		{
			return AddinInfo.ReadFromDescription (addin.Description);
		}
		
		public Addin[] GetDependentAddins (string id, bool recursive)
		{
			return store.GetDependentAddins (id, recursive);
		}
		
		public void BuildPackage (IProgressStatus statusMonitor, string targetDirectory, params string[] filePaths)
		{
			foreach (string file in filePaths)
				BuildPackageInternal (statusMonitor, targetDirectory, file);
		}
		
		void BuildPackageInternal (IProgressStatus monitor, string targetDirectory, string filePath)
		{
			AddinDescription conf = registry.GetAddinDescription (monitor, filePath);
			if (conf == null) {
				monitor.ReportError ("Could not read add-in file: " + filePath, null);
				return;
			}
			
			string basePath = Path.GetDirectoryName (filePath);
			
			if (targetDirectory == null)
				targetDirectory = basePath;

			// Generate the file name
			
			string name;
			if (conf.LocalId.Length == 0)
				name = Path.GetFileNameWithoutExtension (filePath);
			else
				name = conf.LocalId;
			name = Addin.GetFullId (conf.Namespace, name, conf.Version);
			name = name.Replace (',','_').Replace (".__", ".");
			
			string outFilePath = Path.Combine (targetDirectory, name) + ".mpack";
			
			ZipOutputStream s = new ZipOutputStream (File.Create (outFilePath));
			s.SetLevel(5);
			
			// Generate a stripped down description of the add-in in a file, since the complete
			// description may be declared as assembly attributes
			
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			doc.LoadXml (conf.SaveToXml ().OuterXml);
			CleanDescription (doc.DocumentElement);
			MemoryStream ms = new MemoryStream ();
			XmlTextWriter tw = new XmlTextWriter (ms, System.Text.Encoding.UTF8);
			tw.Formatting = Formatting.Indented;
			doc.WriteTo (tw);
			tw.Flush ();
			byte[] data = ms.ToArray ();
			
			ZipEntry infoEntry = new ZipEntry ("addin.info");
			s.PutNextEntry (infoEntry);
			s.Write (data, 0, data.Length);
			
			// Now add the add-in files
			
			ArrayList list = new ArrayList ();
			if (!conf.AllFiles.Contains (Path.GetFileName (filePath)))
				list.Add (Path.GetFileName (filePath));
			foreach (string f in conf.AllFiles) {
				list.Add (f);
			}
			
			monitor.Log ("Creating package " + Path.GetFileName (outFilePath));
			
			foreach (string file in list) {
				string fp = Path.Combine (basePath, file);
				using (FileStream fs = File.OpenRead (fp)) {
					byte[] buffer = new byte [fs.Length];
					fs.Read (buffer, 0, buffer.Length);
					
					ZipEntry entry = new ZipEntry (file);
					s.PutNextEntry (entry);
					s.Write (buffer, 0, buffer.Length);
				}
			}
			
			s.Finish();
			s.Close();			
		}
		
		void CleanDescription (XmlElement parent)
		{
			ArrayList todelete = new ArrayList ();
			
			foreach (XmlNode nod in parent.ChildNodes) {
				XmlElement elem = nod as XmlElement;
				if (elem == null)
					continue;
				if (elem.LocalName == "Module")
					CleanDescription (elem);
				else if (elem.LocalName != "Dependencies" && elem.LocalName != "Runtime")
					todelete.Add (elem);
			}
			foreach (XmlElement e in todelete)
				parent.RemoveChild (e);
		}
		
		public void BuildRepository (IProgressStatus statusMonitor, string path)
		{
			string mainPath = Path.Combine (path, "main.mrep");
			ArrayList allAddins = new ArrayList ();
			
			Repository rootrep = (Repository) AddinStore.ReadObject (mainPath, typeof(Repository));
			if (rootrep == null) {
				rootrep = new Repository ();
			}
			
			IProgressMonitor monitor = ProgressStatusMonitor.GetProgressMonitor (statusMonitor);
			BuildRepository (monitor, rootrep, path, "root.mrep", allAddins);
			AddinStore.WriteObject (mainPath, rootrep);
			GenerateIndexPage (rootrep, allAddins, path);
			monitor.Log.WriteLine ("Updated main.mrep");
		}
		
		void BuildRepository (IProgressMonitor monitor, Repository rootrep, string rootPath, string relFilePath, ArrayList allAddins)
		{
			DateTime lastModified = DateTime.MinValue;
			
			string mainFile = Path.Combine (rootPath, relFilePath);
			string mainPath = Path.GetDirectoryName (mainFile);
			
			Repository mainrep = (Repository) AddinStore.ReadObject (mainFile, typeof(Repository));
			if (mainrep == null) {
				mainrep = new Repository ();
			}
			
			bool modified = false;
			
			monitor.Log.WriteLine ("Checking directory: " + mainPath);
			foreach (string file in Directory.GetFiles (mainPath, "*.mpack")) {
				string fname = Path.GetFileName (file);
				PackageRepositoryEntry entry = (PackageRepositoryEntry) mainrep.FindEntry (fname);
				if (entry == null) {
					entry = new PackageRepositoryEntry ();
					AddinPackage p = (AddinPackage) Package.FromFile (file);
					entry.Addin = (AddinInfo) p.Addin;
					entry.Url = fname;
					mainrep.AddEntry (entry);
					modified = true;
					monitor.Log.WriteLine ("Added addin: " + fname);
				}
				allAddins.Add (entry);
				
				DateTime date = File.GetLastWriteTime (file);
				if (date > lastModified)
					lastModified = date;
			}
			
			ArrayList toRemove = new ArrayList ();
			foreach (PackageRepositoryEntry entry in mainrep.Addins)
				if (!File.Exists (Path.Combine (mainPath, entry.Url)))
					toRemove.Add (entry);
					
			foreach (PackageRepositoryEntry entry in toRemove)
				mainrep.RemoveEntry (entry);
			
			if (modified || toRemove.Count > 0) {
				AddinStore.WriteObject (mainFile, mainrep);
				monitor.Log.WriteLine ("Updated " + relFilePath);
			}

			ReferenceRepositoryEntry repEntry = (ReferenceRepositoryEntry) rootrep.FindEntry (relFilePath);
			if (repEntry != null) {
				if (repEntry.LastModified < lastModified)
					repEntry.LastModified = lastModified;
			} else {
				repEntry = new ReferenceRepositoryEntry ();
				repEntry.LastModified = lastModified;
				repEntry.Url = relFilePath;
				rootrep.AddEntry (repEntry);
			}
			
			foreach (string dir in Directory.GetDirectories (mainPath)) {
				string based = dir.Substring (rootPath.Length + 1);
				BuildRepository (monitor, rootrep, rootPath, Path.Combine (based, "main.mrep"), allAddins);
			}
		}
		
		void GenerateIndexPage (Repository rep, ArrayList addins, string basePath)
		{
			StreamWriter sw = new StreamWriter (Path.Combine (basePath, "index.html"));
			sw.WriteLine ("<html><body>");
			sw.WriteLine ("<h1>Add-in Repository</h1>");
			if (rep.Name != null && rep.Name != "")
				sw.WriteLine ("<h2>" + rep.Name + "</h2>");
			sw.WriteLine ("<p>This is a list of add-ins available in this repository.</p>");
			sw.WriteLine ("<table border=1><thead><tr><th>Add-in</th><th>Version</th><th>Description</th></tr></thead>");
			
			foreach (PackageRepositoryEntry entry in addins) {
				sw.WriteLine ("<tr><td>" + entry.Addin.Name + "</td><td>" + entry.Addin.Version + "</td><td>" + entry.Addin.Description + "</td></tr>");
			}
			
			sw.WriteLine ("</table>");
			sw.WriteLine ("</body></html>");
			sw.Close ();
		}
		
		internal AddinSystemConfiguration Configuration {
			get {
				if (config == null) {
					config = (AddinSystemConfiguration) AddinStore.ReadObject (RootConfigFile, typeof(AddinSystemConfiguration));
					if (config == null)
						config = new AddinSystemConfiguration ();
				}
				return config;
			}
		}
		
		internal void SaveConfiguration ()
		{
			if (config != null) {
				AddinStore.WriteObject (RootConfigFile, config); 
			}
		}

		internal void ResetConfiguration ()
		{
			if (File.Exists (RootConfigFile))
				File.Delete (RootConfigFile);
			ResetAddinInfo ();
		}
				
		internal void ResetAddinInfo ()
		{
			if (Directory.Exists (RepositoryCachePath))
				Directory.Delete (RepositoryCachePath, true);
		}
	}
}
