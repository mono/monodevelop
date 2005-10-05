//
// SetupService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Net;

using ICSharpCode.SharpZipLib.Zip;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Utils.DirectoryArchive;

namespace MonoDevelop.Core.AddIns.Setup
{
	public class SetupService
	{
		ArrayList addinSetupInfos;
		AddInStatus addinStatus;
		AddinSystemConfiguration config;
		string[] addInDirs;
		FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
		
		internal void Initialize (string[] addInDirs, bool ignoreDefaultPath)
		{
			if (ignoreDefaultPath && addInDirs == null)
				this.addInDirs = new string [0];
			else if (ignoreDefaultPath)
				this.addInDirs = addInDirs;
			else if (addInDirs != null) {
				this.addInDirs = new string [addInDirs.Length + 1];
				this.addInDirs [0] = AddinRootPath;
				addInDirs.CopyTo (this.addInDirs, 1);
			} else {
				this.addInDirs = new string [] { AddinRootPath };
			}
		}
		
		string[] AddinDirectories {
			get { return addInDirs; }
		}
		
		string BinPath {
			get { return Path.GetDirectoryName (Assembly.GetEntryAssembly().Location); } 
		}
		
		string UserConfigPath {
			get { return Runtime.Properties.ConfigDirectory; }
		}
		
		string AddinRootPath {
			get { return Path.Combine (BinPath, "../AddIns"); }
		}
		
		string RootConfigFile {
			get { return Path.Combine (UserConfigPath, "addins.config"); }
		}
		
		public void Install (IProgressMonitor monitor, params string[] files)
		{
			Package[] packages = new Package [files.Length];
			for (int n=0; n<files.Length; n++)
				packages [n] = AddinPackage.FromFile (files [n]);
			Install (monitor, packages);
		}
		
		public void Install (IProgressMonitor monitor, params AddinRepositoryEntry[] addins)
		{
			Package[] packages = new Package [addins.Length];
			for (int n=0; n<addins.Length; n++)
				packages [n] = AddinPackage.FromAddin (addins [n]);
			Install (monitor, packages);
		}
		
		public void Install (IProgressMonitor monitor, params Package[] packages)
		{
			ArrayList prepared = new ArrayList ();
			bool rollback = false;
			
			foreach (Package mpack in packages) {
				try {
					mpack.PrepareInstall (monitor, this);
					prepared.Add (mpack);
				} catch (Exception ex) {
					monitor.ReportError (null, ex);
					rollback = true;
					break;
				}
			}
			
			if (!rollback) {
				foreach (Package mpack in packages) {
					try {
						mpack.CommitInstall (monitor, this);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
						rollback = true;
						break;
					}
				}
			}
			
			if (rollback) {
				foreach (Package mpack in prepared) {
					try {
						mpack.RollbackInstall (monitor, this);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
					}
				}
			}
			
			foreach (Package mpack in prepared) {
				try {
					mpack.EndInstall (monitor, this);
				} catch (Exception ex) {
					monitor.Log.WriteLine (ex);
				}
			}
			
			SaveConfiguration ();
		}
		
		public void Uninstall (IProgressMonitor monitor, string id, string version)
		{
			AddinSetupInfo ia = GetInstalledAddin (id, version);
			if (ia == null)
				throw new InstallException ("The addin '" + id + "' is not installed.");
			Uninstall (monitor, ia.Addin);
		}
		
		public void Uninstall (IProgressMonitor monitor, AddinInfo addin)
		{
			if (GetDependentAddins (addin, false).Length != 0)
				throw new InstallException ("The addin '" + addin.Id + "' can't be uninstalled because other addins depend on it.");
			UnregisterAddin (monitor, addin);
		}
		
		public AddinInfo[] GetDependentAddins (AddinInfo addin, bool recursive)
		{
			ArrayList list = new ArrayList ();
			foreach (AddinSetupInfo iaddin in InternalGetInstalledAddins ()) {
				foreach (PackageDependency dep in iaddin.Addin.Dependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep != null && adep.AddinId == addin.Id && adep.Version == addin.Version)
						list.Add (iaddin.Addin);
					else if (recursive)
						list.AddRange (GetDependentAddins (addin, true));
				}
			}
			return (AddinInfo[]) list.ToArray (typeof (AddinInfo));
		}
		
		public bool ResolveDependencies (IProgressMonitor monitor, PackageCollection packages, PackageDependencyCollection unresolved)
		{
			for (int n=0; n<packages.Count; n++) {
				Package p = packages [n];
				p.Resolve (monitor, this, packages, unresolved);
			}
			return unresolved.Count == 0;
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins ()
		{
			return GetAvailableAddins (null, null);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins (string id)
		{
			return GetAvailableAddins (id, null);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins (string id, string version)
		{
			ArrayList list = new ArrayList ();
			foreach (RepositoryRecord rr in Configuration.Repositories) {
				foreach (AddinRepositoryEntry addin in rr.GetCachedRepository().Addins) {
					if ((id == null || addin.Addin.Id == id) && (version == null || addin.Addin.Version == version))
						list.Add (addin);
				}
			}
			return (AddinRepositoryEntry[]) list.ToArray (typeof(AddinRepositoryEntry));
		}
		
		public AddinInfo[] GetInstalledAddins ()
		{
			ArrayList list = new ArrayList ();
			foreach (AddinSetupInfo ia in InternalGetInstalledAddins ())
				list.Add (ia.Addin);
			return (AddinInfo[]) list.ToArray (typeof(AddinInfo));
		}
		
		public AddinSetupInfo GetInstalledAddin (string id)
		{
			return GetInstalledAddin (id, null);
		}
		
		public AddinSetupInfo GetInstalledAddin (string id, string version)
		{
			foreach (AddinSetupInfo ia in InternalGetInstalledAddins ()) {
				if ((id == null || ia.Addin.Id == id) && (version == null || ia.Addin.Version == version))
					return ia;
			}
			return null;
		}
		
		ArrayList InternalGetInstalledAddins ()
		{
			if (addinSetupInfos != null)
				return addinSetupInfos;

			DateTime t = DateTime.Now;
			
			addinSetupInfos = new ArrayList ();
			
			foreach (string dir in AddinDirectories) {
				StringCollection files = fileUtilityService.SearchDirectory (dir, "*.addin.xml");
				foreach (string file in files)
					addinSetupInfos.Add (new AddinSetupInfo (file));
			}
//			Console.WriteLine ("GOT IN " + (DateTime.Now - t).TotalMilliseconds);
			return addinSetupInfos;
		}
		
		internal AddInStatus GetAddInStatus ()
		{
			if (addinStatus != null) return addinStatus;
			
			Hashtable hash = new Hashtable ();
			ArrayList apps = new ArrayList ();
			
			DateTime t = DateTime.Now;
			
			foreach (AddinSetupInfo ia in InternalGetInstalledAddins ()) {
				AddinConfiguration conf = ia.GetConfiguration ();
				foreach (XmlElement elem in conf.Content.SelectNodes ("AddIn/Extension")) {
					string path = elem.GetAttribute ("path");
					AddChildExtensions (ia.Addin.Id, hash, path, elem);
				}
				foreach (XmlElement elem in conf.Content.SelectNodes ("AddIn/Extension[@path='/Workspace/Applications']/Class")) {
					ApplicationRecord arec = new ApplicationRecord ();
					arec.Id = elem.GetAttribute ("id");
					arec.AddIn = ia.Addin.Id;
					apps.Add (arec);
				}
			}
			addinStatus = new AddInStatus ();
			ExtensionRelation[] rels = new ExtensionRelation [hash.Count];
			int n = 0;
			foreach (DictionaryEntry de in hash) {
				ExtensionRelation rel = new ExtensionRelation ();
				rel.Path = (string) de.Key;
				rel.AddIns = (string[]) ((ArrayList)de.Value).ToArray (typeof(string));
				rels [n++] = rel;
			}
			addinStatus.ExtensionRelations = rels;
			addinStatus.Applications = (ApplicationRecord[]) apps.ToArray (typeof(ApplicationRecord));
//			Console.WriteLine ("GOT IN " + (DateTime.Now - t).TotalMilliseconds);
			
			return addinStatus;
		}
		
		void AddChildExtensions (string addin, Hashtable hash, string path, XmlElement elem)
		{
			ArrayList list = (ArrayList) hash [path];
			if (list == null) {
				list = new ArrayList ();
				hash [path] = list;
			}
			list.Add (addin);
			foreach (XmlNode node in elem.ChildNodes) {
				XmlElement cel = node as XmlElement;
				if (cel == null) continue;
				string id = cel.GetAttribute ("id");
				if (id.Length != 0)
					AddChildExtensions (addin, hash, path + "/" + id, cel);
			}
		}
		
		void ResetCachedData ()
		{
			addinStatus = null;
			addinSetupInfos = null;
		}
		
		public RepositoryRecord[] GetRepositories ()
		{
			ArrayList list = new ArrayList ();
			foreach (RepositoryRecord rep in Configuration.Repositories)
				if (!rep.IsReference) list.Add (rep);
			return (RepositoryRecord[]) list.ToArray (typeof (RepositoryRecord));
		}
		
		public void RegisterRepository (string url)
		{
			RegisterRepository (url, false);
		}
		
		RepositoryRecord RegisterRepository (string url, bool isReference)
		{
			RepositoryRecord rr = FindRepositoryRecord (url);
			if (rr != null) {
				if (rr.IsReference && !isReference) {
					rr.IsReference = false;
					SaveConfiguration ();
				}
				return rr;
			}
			
			rr = new RepositoryRecord ();
			rr.Url = url;
			rr.IsReference = isReference;
			
			string name = Path.Combine (UserConfigPath, "repository-cache");
			if (!Directory.Exists (name))
				Directory.CreateDirectory (name);
			name = Path.Combine (name, new Uri (url).Host);
			rr.File = name + "_" + Configuration.RepositoryIdCount + ".mrep";
			
			rr.Id = "rep" + Configuration.RepositoryIdCount;
			Configuration.Repositories.Add (rr);
			Configuration.RepositoryIdCount++;
			SaveConfiguration ();
			return rr;
		}
		
		public void UnregisterRepository (string url)
		{
			RepositoryRecord rep = FindRepositoryRecord (url);
			if (rep == null)
				throw new InstallException ("The repository at url '" + url + "' is not registered");
			
			foreach (RepositoryRecord rr in Configuration.Repositories) {
				if (rr == rep) continue;
				Repository newRep = rr.GetCachedRepository ();
				if (newRep == null) continue;
				foreach (ReferenceRepositoryEntry re in newRep.Repositories) {
					if (re.Url == url) {
						rep.IsReference = true;
						return;
					}
				}
			}
			
			// There are no other repositories referencing this one, so we can safely delete
			
			Repository delRep = rep.GetCachedRepository ();
			Configuration.Repositories.Remove (rep);
			rep.ClearCachedRepository ();
			
			if (delRep != null) {
				foreach (ReferenceRepositoryEntry re in delRep.Repositories)
					UnregisterRepository (new Uri (new Uri (url), re.Url).ToString ());
			}

			SaveConfiguration ();
		}
		
		RepositoryRecord FindRepositoryRecord (string url)
		{
			foreach (RepositoryRecord rr in Configuration.Repositories)
				if (rr.Url == url) return rr;
			return null;
		}
		
		public void UpdateRepositories (IProgressMonitor monitor)
		{
			UpdateRepository (monitor, (string)null);
		}
		
		public void UpdateRepository (IProgressMonitor monitor, string url)
		{
			monitor.BeginTask ("Updating repositories", Configuration.Repositories.Count);
			int num = Configuration.Repositories.Count;
			for (int n=0; n<num; n++) {
				RepositoryRecord rr = (RepositoryRecord) Configuration.Repositories [n];
				if ((url == null || rr.Url == url) && !rr.IsReference)
					UpdateRepository (monitor, new Uri (rr.Url), rr);
			}
			monitor.EndTask ();
			SaveConfiguration ();
		}

		void UpdateRepository (IProgressMonitor monitor, Uri baseUri, RepositoryRecord rr)
		{
			Uri absUri = new Uri (baseUri, rr.Url);
			monitor.Log.WriteLine ("Updating from " + absUri.ToString ());
			Repository newRep = (Repository) DownloadObject (monitor, absUri.ToString (), typeof(Repository));
			
			foreach (ReferenceRepositoryEntry re in newRep.Repositories) {
				Uri refRepUri = new Uri (absUri, re.Url);
				string refRepUrl = refRepUri.ToString ();
				RepositoryRecord refRep = FindRepositoryRecord (refRepUrl);
				if (refRep == null)
					refRep = RegisterRepository (refRepUrl, true);
				if (refRep.LastModified < re.LastModified) {
					UpdateRepository (monitor, refRepUri, refRep);
				}
			}
			rr.UpdateCachedRepository (newRep);
		}
		
		public void BuildRepository (IProgressMonitor monitor, string path)
		{
			string mainPath = Path.Combine (path, "root.mrep");
			Repository rootrep = (Repository) ReadObject (mainPath, typeof(Repository));
			if (rootrep == null) {
				rootrep = new Repository ();
			}
			
			BuildRepository (monitor, rootrep, path, "main.mrep");
			monitor.Log.WriteLine ("Updated root.mrep");
			WriteObject (mainPath, rootrep);
		}
		
		void BuildRepository (IProgressMonitor monitor, Repository rootrep, string rootPath, string relFilePath)
		{
			DateTime lastModified = DateTime.MinValue;
			
			string mainFile = Path.Combine (rootPath, relFilePath);
			string mainPath = Path.GetDirectoryName (mainFile);
			
			Repository mainrep = (Repository) ReadObject (mainFile, typeof(Repository));
			if (mainrep == null) {
				mainrep = new Repository ();
			}
			
			bool modified = false;
			
			monitor.Log.WriteLine ("Checking directory: " + mainPath);
			foreach (string file in Directory.GetFiles (mainPath, "*.mpack")) {
				string fname = Path.GetFileName (file);
				AddinRepositoryEntry entry = (AddinRepositoryEntry) mainrep.FindEntry (fname);
				if (entry != null) {
				} else {
					entry = new AddinRepositoryEntry ();
					AddinPackage p = AddinPackage.FromFile (file);
					entry.Addin = p.Addin;
					entry.Url = fname;
					mainrep.AddEntry (entry);
					modified = true;
					monitor.Log.WriteLine ("Added addin: " + fname);
				}
				
				DateTime date = File.GetLastWriteTime (file);
				if (date > lastModified)
					lastModified = date;
			}
			
			ArrayList toRemove = new ArrayList ();
			foreach (AddinRepositoryEntry entry in mainrep.Addins)
				if (!File.Exists (Path.Combine (mainPath, entry.Url)))
					toRemove.Add (entry);
					
			foreach (AddinRepositoryEntry entry in toRemove)
				mainrep.RemoveEntry (entry);
			
			if (modified || toRemove.Count > 0) {
				WriteObject (mainFile, mainrep);
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
				BuildRepository (monitor, rootrep, rootPath, Path.Combine (based, "main.mrep"));
			}
		}
		
		internal string GetAddinDirectory (AddinInfo info)
		{
			return Path.Combine (AddinRootPath, info.Id + "_" + info.Version);
		}
		
		internal void RegisterAddin (IProgressMonitor monitor, AddinInfo info, string sourceDir)
		{
			monitor.Log.WriteLine ("Installing " + info.Id + " v" + info.Version);
			string addinDir = GetAddinDirectory (info);
			if (!Directory.Exists (addinDir))
				Directory.CreateDirectory (addinDir);
			CopyDirectory (sourceDir, addinDir);
			
			UninstallInfo uinfo = new UninstallInfo ();
			uinfo.Paths.Add (addinDir);
			
			WriteObject (Path.Combine (addinDir, "uninstall-info.xml"), uinfo);
			
			monitor.Log.WriteLine ("Done");
			
			ResetCachedData ();
		}

	
		void CopyDirectory (string src, string dest)
		{
			CopyDirectory (src, dest, "");
		}
		
		void CopyDirectory (string src, string dest, string subdir)
		{
			string destDir = Path.Combine (dest, subdir);
	
			if (!Directory.Exists (destDir))
				Directory.CreateDirectory (destDir);
	
			foreach (string file in Directory.GetFiles (src))
				File.Copy (file, Path.Combine (destDir, Path.GetFileName (file)));
	
			foreach (string dir in Directory.GetDirectories (src))
				CopyDirectory (dir, dest, Path.Combine (subdir, Path.GetFileName (dir)));
		}
		
		internal void UnregisterAddin (IProgressMonitor monitor, AddinInfo info)
		{
			AddinSetupInfo iaddin = GetInstalledAddin (info.Id, info.Version);
			if (iaddin == null)
				throw new InstallException ("Addin not installed.");
				
			string ufile = Path.Combine (iaddin.Directory, "uninstall-info.xml");
			if (!File.Exists (ufile))
				throw new InstallException ("Unistall information is not available for the addin: " + info.Id);

			UninstallInfo uinfo = (UninstallInfo) ReadObject (ufile, typeof(UninstallInfo));
			monitor.Log.WriteLine ("Uninstalling " + info.Id + " v" + info.Version);
			
			foreach (string path in uinfo.Paths) {
				if (File.Exists (path))
					File.Delete (path);
				else if (Directory.Exists (path))					
					Directory.Delete (iaddin.Directory, true);
			}
			
			monitor.Log.WriteLine ("Done");
		}
		
		internal object DownloadObject (IProgressMonitor monitor, string url, Type type)
		{
			string file = null;
			try {
				file = DownloadFile (monitor, url);
				return ReadObject (file, type);
			} finally {
				if (file != null)
					File.Delete (file);
			}
		}
		
		internal static object ReadObject (string file, Type type)
		{
			if (!File.Exists (file))
				return null;

			StreamReader r = new StreamReader (file);
			try {
				XmlSerializer ser = new XmlSerializer (type);
				return ser.Deserialize (r);
			} finally {
				r.Close ();
			}
		}
		
		internal static void WriteObject (string file, object obj)
		{
			StreamWriter s = new StreamWriter (file);
			try {
				XmlSerializer ser = new XmlSerializer (obj.GetType());
				ser.Serialize (s, obj);
				s.Close ();
			} catch {
				s.Close ();
				if (File.Exists (file))
					File.Delete (file);
				throw;
			}
		}
		
		internal string DownloadFile (IProgressMonitor monitor, string url)
		{
			monitor.BeginTask ("Requesting " + url, 2);
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			HttpWebResponse resp = (HttpWebResponse) req.GetResponse ();
			monitor.Step (1);
			
			monitor.BeginTask ("Downloading " + url, (int) resp.ContentLength);
			
			string file = Path.GetTempFileName ();
			FileStream fs = null;
			Stream s = null;
			try {
				fs = new FileStream (file, FileMode.Create, FileAccess.Write);
				s = req.GetResponse ().GetResponseStream ();
				byte[] buffer = new byte [4096];
				
				int n;
				while ((n = s.Read (buffer, 0, buffer.Length)) != 0) {
					monitor.Step (n);
					fs.Write (buffer, 0, n);
				}
				fs.Close ();
				s.Close ();
				return file;
			} catch {
				if (fs != null)
					fs.Close ();
				if (s != null)
					s.Close ();
				File.Delete (file);
			} finally {
				monitor.EndTask ();
				monitor.EndTask ();
			}
			return file;
		}
		
		AddinSystemConfiguration Configuration {
			get {
				if (config == null) {
					config = (AddinSystemConfiguration) ReadObject (RootConfigFile, typeof(AddinSystemConfiguration));
					if (config == null)
						config = new AddinSystemConfiguration ();
				}
				return config;
			}
		}
		
		void SaveConfiguration ()
		{
			if (config != null) {
				WriteObject (RootConfigFile, config); 
			}
		}
	}
	
	public class AddInStatus
	{
		public ExtensionRelation[] ExtensionRelations;
		public ApplicationRecord[] Applications;
	}
	
	public class ExtensionRelation
	{
		[XmlAttribute]
		public string Path;

		[XmlElement ("AddIn")]
		public string[] AddIns;
	}
	
	public class ApplicationRecord
	{
		[XmlAttribute]
		public string Id;
		[XmlAttribute]
		public string AddIn;
	}
}
