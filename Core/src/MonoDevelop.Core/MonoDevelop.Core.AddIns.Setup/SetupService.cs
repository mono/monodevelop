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
using MonoDevelop.Core.ProgressMonitoring;
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
				this.addInDirs [0] = RootAddinPath;
				addInDirs.CopyTo (this.addInDirs, 1);
			} else {
				this.addInDirs = new string [] { RootAddinPath, UserAddinPath };
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
		
		string RootAddinPath {
			get { return Path.Combine (BinPath, "../AddIns"); }
		}
		
		string RootConfigFile {
			get { return Path.Combine (UserConfigPath, "addins.config"); }
		}
		
		string UserAddinPath {
			get { return Path.Combine (UserConfigPath, "addins"); }
		}
		
		public bool Install (IProgressMonitor monitor, params string[] files)
		{
			Package[] packages = new Package [files.Length];
			for (int n=0; n<files.Length; n++)
				packages [n] = AddinPackage.FromFile (files [n]);
			return Install (monitor, packages);
		}
		
		public bool Install (IProgressMonitor monitor, params AddinRepositoryEntry[] addins)
		{
			Package[] packages = new Package [addins.Length];
			for (int n=0; n<addins.Length; n++)
				packages [n] = AddinPackage.FromRepository (addins [n]);
			return Install (monitor, packages);
		}
		
		public bool Install (IProgressMonitor monitor, params Package[] packages)
		{
			PackageCollection packs = new PackageCollection ();
			packs.AddRange (packages);
			return Install (monitor, packs);
		}
		
		public bool Install (IProgressMonitor monitor, PackageCollection packs)
		{
			PackageCollection toUninstall;
			PackageDependencyCollection unresolved;
			if (!Runtime.SetupService.ResolveDependencies (monitor, packs, out toUninstall, out unresolved)) {
				monitor.ReportError (GettextCatalog.GetString ("Not all dependencies could be resolved."), null);
				return false;
			}
			
			ArrayList prepared = new ArrayList ();
			ArrayList uninstallPrepared = new ArrayList ();
			bool rollback = false;
			
			monitor.BeginTask (GettextCatalog.GetString ("Installing addins..."), 100);
			
			// Prepare install
			
			monitor.BeginStepTask (GettextCatalog.GetString ("Initializing installation"), toUninstall.Count + packs.Count + 1, 75);
			
			foreach (Package mpack in toUninstall) {
				try {
					mpack.PrepareUninstall (monitor, this);
					uninstallPrepared.Add (mpack);
					if (monitor.IsCancelRequested)
						throw new InstallException ("Installation cancelled.");
					monitor.Step (1);
				} catch (Exception ex) {
					monitor.ReportError (null, ex);
					rollback = true;
					break;
				}
			}
			
			monitor.Step (1);

			foreach (Package mpack in packs) {
				try {
					mpack.PrepareInstall (monitor, this);
					if (monitor.IsCancelRequested)
						throw new InstallException ("Installation cancelled.");
					prepared.Add (mpack);
					monitor.Step (1);
				} catch (Exception ex) {
					monitor.ReportError (null, ex);
					rollback = true;
					break;
				}
			}
			
			monitor.EndTask ();
			
			monitor.BeginStepTask (GettextCatalog.GetString ("Installing"), toUninstall.Count + packs.Count + 1, 20);
			
			// Commit install
			
			if (!rollback) {
				foreach (Package mpack in toUninstall) {
					try {
						mpack.CommitUninstall (monitor, this);
						if (monitor.IsCancelRequested)
							throw new InstallException ("Installation cancelled.");
						monitor.Step (1);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
						rollback = true;
						break;
					}
				}
			}
			
			monitor.Step (1);
			
			if (!rollback) {
				foreach (Package mpack in packs) {
					try {
						mpack.CommitInstall (monitor, this);
						if (monitor.IsCancelRequested)
							throw new InstallException ("Installation cancelled.");
						monitor.Step (1);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
						rollback = true;
						break;
					}
				}
			}
			
			monitor.EndTask ();
			
			// Rollback if failed
			
			if (monitor.IsCancelRequested)
				monitor = new NullProgressMonitor ();
			
			if (rollback) {
				monitor.BeginStepTask (GettextCatalog.GetString ("Finishing installation"), (prepared.Count + uninstallPrepared.Count)*2 + 1, 5);
			
				foreach (Package mpack in prepared) {
					try {
						mpack.RollbackInstall (monitor, this);
						monitor.Step (1);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
					}
				}
			
				foreach (Package mpack in uninstallPrepared) {
					try {
						mpack.RollbackUninstall (monitor, this);
						monitor.Step (1);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
					}
				}
			} else
				monitor.BeginStepTask (GettextCatalog.GetString ("Finishing installation"), prepared.Count + uninstallPrepared.Count + 1, 5);
			
			// Cleanup
			
			foreach (Package mpack in prepared) {
				try {
					mpack.EndInstall (monitor, this);
					monitor.Step (1);
				} catch (Exception ex) {
					monitor.Log.WriteLine (ex);
				}
			}
			
			monitor.Step (1);

			foreach (Package mpack in uninstallPrepared) {
				try {
					mpack.EndUninstall (monitor, this);
					monitor.Step (1);
				} catch (Exception ex) {
					monitor.Log.WriteLine (ex);
				}
			}
			
			monitor.EndTask ();

			monitor.EndTask ();
			
			SaveConfiguration ();
			ResetCachedData ();
			
			return !rollback;
		}
		
		public void Uninstall (IProgressMonitor monitor, AddinInfo addin)
		{
			Uninstall (monitor, addin.Id);
		}
		
		public void Uninstall (IProgressMonitor monitor, string id)
		{
			bool rollback = false;
			ArrayList toUninstall = new ArrayList ();
			ArrayList uninstallPrepared = new ArrayList ();
			
			AddinSetupInfo ia = GetInstalledAddin (id);
			if (ia == null)
				throw new InstallException ("The addin '" + id + "' is not installed.");

			toUninstall.Add (AddinPackage.FromInstalledAddin (ia));

			AddinSetupInfo[] deps = GetDependentAddins (id, true);
			foreach (AddinSetupInfo dep in deps)
				toUninstall.Add (AddinPackage.FromInstalledAddin (dep));
			
			monitor.BeginTask ("Uninstalling addins", toUninstall.Count*2 + uninstallPrepared.Count + 1);
			
			// Prepare install
			
			foreach (Package mpack in toUninstall) {
				try {
					mpack.PrepareUninstall (monitor, this);
					monitor.Step (1);
					uninstallPrepared.Add (mpack);
				} catch (Exception ex) {
					monitor.ReportError (null, ex);
					rollback = true;
					break;
				}
			}
			
			// Commit install
			
			if (!rollback) {
				foreach (Package mpack in toUninstall) {
					try {
						mpack.CommitUninstall (monitor, this);
						monitor.Step (1);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
						rollback = true;
						break;
					}
				}
			}
			
			// Rollback if failed
			
			if (rollback) {
				monitor.BeginTask ("Rolling back uninstall", uninstallPrepared.Count);
				foreach (Package mpack in uninstallPrepared) {
					try {
						mpack.RollbackUninstall (monitor, this);
					} catch (Exception ex) {
						monitor.ReportError (null, ex);
					}
				}
				monitor.EndTask ();
			}
			monitor.Step (1);

			// Cleanup
			
			foreach (Package mpack in uninstallPrepared) {
				try {
					mpack.EndUninstall (monitor, this);
					monitor.Step (1);
				} catch (Exception ex) {
					monitor.Log.WriteLine (ex);
				}
			}
			
			monitor.EndTask ();
			
			SaveConfiguration ();
			ResetCachedData ();
		}
		
		public AddinSetupInfo[] GetDependentAddins (string id, bool recursive)
		{
			ArrayList list = new ArrayList ();
			FindDependentAddins (list, id, recursive);
			return (AddinSetupInfo[]) list.ToArray (typeof (AddinSetupInfo));
		}
		
		void FindDependentAddins (ArrayList list, string id, bool recursive)
		{
			foreach (AddinSetupInfo iaddin in InternalGetInstalledAddins ()) {
				if (list.Contains (iaddin))
					continue;
				foreach (PackageDependency dep in iaddin.Addin.Dependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep != null && adep.AddinId == id) {
						list.Add (iaddin);
						if (recursive)
							FindDependentAddins (list, iaddin.Addin.Id, true);
					}
				}
			}
		}
		
		public bool ResolveDependencies (IProgressMonitor monitor, PackageCollection packages, out PackageCollection toUninstall, out PackageDependencyCollection unresolved)
		{
			PackageCollection requested = new PackageCollection();
			requested.AddRange (packages);
			
			unresolved = new PackageDependencyCollection ();
			toUninstall = new PackageCollection ();
			PackageCollection installedRequired = new PackageCollection ();
			
			for (int n=0; n<packages.Count; n++) {
				Package p = packages [n];
				p.Resolve (monitor, this, packages, toUninstall, installedRequired, unresolved);
			}
			
			if (unresolved.Count != 0) {
				foreach (PackageDependency dep in unresolved) {
					monitor.ReportError (string.Format (GettextCatalog.GetString ("The package '{0}' could not be found in any repository"), dep.Name), null);
				}
				return false;
			}
			
			// Check that we are not uninstalling packages that are required
			// by packages being installed.

			foreach (Package p in installedRequired) {
				if (toUninstall.Contains (p)) {
					// Only accept to uninstall this package if we are
					// going to install a newer version.
					bool foundUpgrade = false;
					foreach (Package tbi in packages)
						if (tbi.Equals (p) || tbi.IsUpgradeOf (p)) {
							foundUpgrade = true;
							break;
						}
					if (!foundUpgrade)
						return false;
				}
			}
			
			// Check that we are not trying to uninstall from a directory from
			// which we don't have write permissions
			
			foreach (Package p in toUninstall) {
				AddinPackage ap = p as AddinPackage;
				if (ap != null) {
					AddinSetupInfo ia = GetInstalledAddin (ap.Addin.Id);
					if (!HasWriteAccess (ia.ConfigFile)) {
						monitor.ReportError (GetUninstallErrorNoRoot (ap.Addin), null);
						return false;
					}
				}
			}
			
			// Don't try to install in the shared dir if we don't have permissions
			
			if (!HasWriteAccess (RootAddinPath)) {
				foreach (Package p in packages) {
					AddinPackage ap = p as AddinPackage;
					if (ap != null && ap.RootInstall)
						ap.RootInstall = false;
				}
			}
			
			// Check that we are not installing two versions of the same addin
			
			PackageCollection resolved = new PackageCollection();
			resolved.AddRange (packages);
			
			bool error = false;
			
			for (int n=0; n<packages.Count; n++) {
				AddinPackage ap = packages [n] as AddinPackage;
				if (ap == null) continue;
				
				for (int k=n+1; k<packages.Count; k++) {
					AddinPackage otherap = packages [k] as AddinPackage;
					if (otherap == null) continue;
					
					if (ap.Addin.Id == otherap.Addin.Id) {
						if (ap.IsUpgradeOf (otherap)) {
							if (requested.Contains (otherap)) {
								monitor.ReportError (GettextCatalog.GetString ("Can't install two versions of the same add-in: '") + ap.Addin.Name + "'.", null);
								error = true;
							} else {
								packages.RemoveAt (k);
							}
						} else if (otherap.IsUpgradeOf (ap)) {
							if (requested.Contains (ap)) {
								monitor.ReportError (GettextCatalog.GetString ("Can't install two versions of the same add-in: '") + ap.Addin.Name + "'.", null);
								error = true;
							} else {
								packages.RemoveAt (n);
								n--;
							}
						} else {
							error = true;
							monitor.ReportError (GettextCatalog.GetString ("Can't install two versions of the same add-in: '") + ap.Addin.Name + "'.", null);
						}
						break;
					}
				}
			}
			
			return !error;
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates ()
		{
			return GetAvailableAddin (null, null, null, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates (string repositoryUrl)
		{
			return GetAvailableAddin (repositoryUrl, null, null, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates (string id, string version)
		{
			return GetAvailableAddin (null, id, version, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates (string repositoryUrl, string id, string version)
		{
			return GetAvailableAddin (repositoryUrl, id, version, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins ()
		{
			return GetAvailableAddin (null, null, null, false);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins (string repositoryUrl)
		{
			return GetAvailableAddin (repositoryUrl, null, null);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddin (string id, string version)
		{
			return GetAvailableAddin (null, id, version);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddin (string repositoryUrl, string id, string version)
		{
			return GetAvailableAddin (repositoryUrl, id, version, false);
		}
		
		AddinRepositoryEntry[] GetAvailableAddin (string repositoryUrl, string id, string version, bool updates)
		{
			ArrayList list = new ArrayList ();
			
			IEnumerable ee;
			if (repositoryUrl != null) {
				ArrayList repos = new ArrayList ();
				GetRepositoryTree (repositoryUrl, repos);
				ee = repos;
			} else
				ee = Configuration.Repositories;
			
			foreach (RepositoryRecord rr in ee) {
				Repository rep = rr.GetCachedRepository();
				if (rep == null) continue;
				foreach (AddinRepositoryEntry addin in rep.Addins) {
					if ((id == null || addin.Addin.Id == id) && (version == null || addin.Addin.Version == version)) {
						if (updates) {
							AddinSetupInfo ainfo = GetInstalledAddin (addin.Addin.Id);
							if (ainfo == null || AddinInfo.CompareVersions (ainfo.Addin.Version, addin.Addin.Version) <= 0)
								continue;
						}
						list.Add (addin);
					}
				}
			}
			return (AddinRepositoryEntry[]) list.ToArray (typeof(AddinRepositoryEntry));
		}
		
		void GetRepositoryTree (string url, ArrayList list)
		{
			RepositoryRecord rr = FindRepositoryRecord (url);
			if (rr == null) return;
			
			if (list.Contains (rr))
				return;
				
			list.Add (rr);
			Repository rep = rr.GetCachedRepository ();
			if (rep == null)
				return;
			
			Uri absUri = new Uri (url);
			foreach (ReferenceRepositoryEntry re in rep.Repositories) {
				Uri refRepUri = new Uri (absUri, re.Url);
				GetRepositoryTree (refRepUri.ToString (), list);
			}
		}
		
		public AddinSetupInfo[] GetInstalledAddins ()
		{
			ArrayList list = InternalGetInstalledAddins ();
			return (AddinSetupInfo[]) list.ToArray (typeof(AddinSetupInfo));
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

			addinSetupInfos = new ArrayList ();
			
			foreach (string dir in AddinDirectories) {
				if (!Directory.Exists (dir))
					continue;
				StringCollection files = fileUtilityService.SearchDirectory (dir, "*.addin.xml");
				foreach (string file in files)
					addinSetupInfos.Add (new AddinSetupInfo (file));
			}
			return addinSetupInfos;
		}
		
		internal AddInStatus GetAddInStatus ()
		{
			if (addinStatus != null) return addinStatus;
			
			Hashtable hash = new Hashtable ();
			ArrayList apps = new ArrayList ();
			
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
		
		public bool IsRepositoryRegistered (string url)
		{
			return FindRepositoryRecord (url) != null;
		}
		
		public RepositoryRecord RegisterRepository (IProgressMonitor monitor, string url)
		{
			if (!url.EndsWith (".mrep"))
				url = url + "/main.mrep";
			
			RegisterRepository (url, false);
			try {
				UpdateRepository (monitor, url);
				RepositoryRecord rr = FindRepositoryRecord (url);
				Repository rep = rr.GetCachedRepository ();
				rr.Name = rep.Name;
				SaveConfiguration ();
				return rr;
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("The repository could not be registered"), ex);
				if (IsRepositoryRegistered (url))
					UnregisterRepository (url);
				return null;
			}
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
			try {
				int num = Configuration.Repositories.Count;
				for (int n=0; n<num; n++) {
					RepositoryRecord rr = (RepositoryRecord) Configuration.Repositories [n];
					if ((url == null || rr.Url == url) && !rr.IsReference)
						UpdateRepository (monitor, new Uri (rr.Url), rr);
					monitor.Step (1);
				}
			} finally {
				monitor.EndTask ();
			}
			SaveConfiguration ();
		}

		void UpdateRepository (IProgressMonitor monitor, Uri baseUri, RepositoryRecord rr)
		{
			Uri absUri = new Uri (baseUri, rr.Url);
			monitor.BeginTask ("Updating from " + absUri.ToString (), 2);
			Repository newRep;
			try {
				newRep = (Repository) DownloadObject (monitor, absUri.ToString (), typeof(Repository));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not get information from repository") + ": " + absUri.ToString (), ex);
				return;
			}
			
			monitor.Step (1);
			
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
			monitor.EndTask ();
			rr.UpdateCachedRepository (newRep);
		}
		
		public void BuildRepository (IProgressMonitor monitor, string path)
		{
			string mainPath = Path.Combine (path, "main.mrep");
			ArrayList allAddins = new ArrayList ();
			
			Repository rootrep = (Repository) ReadObject (mainPath, typeof(Repository));
			if (rootrep == null) {
				rootrep = new Repository ();
			}
			
			BuildRepository (monitor, rootrep, path, "root.mrep", allAddins);
			WriteObject (mainPath, rootrep);
			GenerateIndexPage (rootrep, allAddins, path);
			monitor.Log.WriteLine ("Updated main.mrep");
		}
		
		void BuildRepository (IProgressMonitor monitor, Repository rootrep, string rootPath, string relFilePath, ArrayList allAddins)
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
				allAddins.Add (entry);
				
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
				BuildRepository (monitor, rootrep, rootPath, Path.Combine (based, "main.mrep"), allAddins);
			}
		}
		
		void GenerateIndexPage (Repository rep, ArrayList addins, string basePath)
		{
			StreamWriter sw = new StreamWriter (Path.Combine (basePath, "index.html"));
			sw.WriteLine ("<html><body><head>");
			sw.WriteLine ("<html><body>");
			sw.WriteLine ("<link type='text/css' rel='stylesheet' href='md.css' />");
			sw.WriteLine ("</head>");
			sw.WriteLine ("<h1>MonoDevelop Add-in Repository</h1>");
			if (rep.Name != null && rep.Name != "")
				sw.WriteLine ("<h2>" + rep.Name + "</h2>");
			sw.WriteLine ("<p>This is a list of add-ins available in this repository. ");
			sw.WriteLine ("If you need information about how to install add-ins, please read <a href='http://www.monodevelop.com/Installing_Add-ins'>this</a>.</p>");
			sw.WriteLine ("<table border=1><thead><tr><th>Add-in</th><th>Version</th><th>Description</th></tr></thead>");
			
			foreach (AddinRepositoryEntry entry in addins) {
				sw.WriteLine ("<tr><td>" + entry.Addin.Id + "</td><td>" + entry.Addin.Version + "</td><td>" + entry.Addin.Description + "</td></tr>");
			}
			
			sw.WriteLine ("</table>");
			sw.WriteLine ("</body></html>");
			sw.Close ();
		}
		
		public void BuildPackage (IProgressMonitor monitor, string targetDirectory, params string[] filePaths)
		{
			foreach (string file in filePaths)
				BuildPackageInternal (monitor, targetDirectory, file);
		}
		
		void BuildPackageInternal (IProgressMonitor monitor, string targetDirectory, string filePath)
		{
			AddinConfiguration conf = AddinConfiguration.Read (filePath, true);
			
			string basePath = Path.GetDirectoryName (filePath);
			
			if (targetDirectory == null)
				targetDirectory = basePath;
			
			AddinInfo info;
			using (StreamReader sr = new StreamReader (filePath)) {
				info = AddinInfo.ReadFromAddinFile (sr);
			}
			
			string outFilePath = Path.Combine (targetDirectory, info.Id + "_" + info.Version) + ".mpack";

			ZipOutputStream s = new ZipOutputStream (File.Create (outFilePath));
			s.SetLevel(5);
			
			ArrayList list = new ArrayList ();
			list.Add (Path.GetFileName (filePath));
			list.AddRange (conf.AllFiles);
			
			monitor.BeginTask ("Creating package " + Path.GetFileName (outFilePath), list.Count);
			
			foreach (string file in list) {
				string fp = Path.Combine (basePath, file);
				FileStream fs = File.OpenRead (fp);
				
				byte[] buffer = new byte [fs.Length];
				fs.Read (buffer, 0, buffer.Length);
				
				ZipEntry entry = new ZipEntry (file);
				s.PutNextEntry (entry);
				s.Write (buffer, 0, buffer.Length);
				monitor.Log.WriteLine ("Added " + file);
				monitor.Step (1);
			}
			
			monitor.EndTask ();
			
			s.Finish();
			s.Close();			
		}
		
		internal string GetAddinDirectory (AddinInfo info, bool userAddin)
		{
			if (userAddin)
				return Path.Combine (UserAddinPath, info.Id + "_" + info.Version);
			else
				return Path.Combine (RootAddinPath, info.Id + "_" + info.Version);
		}
		
		internal void RegisterAddin (IProgressMonitor monitor, AddinInfo info, string sourceDir, bool userAddin)
		{
			monitor.Log.WriteLine ("Installing " + info.Id + " v" + info.Version);
			string addinDir = GetAddinDirectory (info, userAddin);
			if (!Directory.Exists (addinDir))
				Directory.CreateDirectory (addinDir);
			CopyDirectory (sourceDir, addinDir);

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
			string dir = Path.GetDirectoryName (file);
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
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
			if (url.StartsWith ("file://")) {
				string tmpfile = Path.GetTempFileName ();
				string path = url.Substring (7);
				File.Delete (tmpfile);
				File.Copy (path, tmpfile);
				return tmpfile;
			}

			monitor.BeginTask (GettextCatalog.GetString ("Requesting ") + url, 2);
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.Headers ["Pragma"] = "no-cache";
			HttpWebResponse resp = (HttpWebResponse) req.GetResponse ();
			monitor.Step (1);
			
			monitor.BeginTask (GettextCatalog.GetString ("Downloading ") + url, (int) resp.ContentLength);
			
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
					if (monitor.IsCancelRequested)
						throw new InstallException ("Installation cancelled.");
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
				throw;
			} finally {
				monitor.EndTask ();
				monitor.EndTask ();
			}
		}
			
		internal bool HasWriteAccess (string file)
		{
			if (File.Exists (file)) {
				try {
					File.OpenWrite (file).Close ();
					return true;
				} catch {
					return false;
				}
			}
			else if (Directory.Exists (file)) {
				string tpath = Path.Combine (file, ".test");
				int n = 0;
				while (Directory.Exists (tpath + n)) n++;
				try {
					Directory.CreateDirectory (tpath + n);
					Directory.Delete (tpath + n);
					return true;
				} catch {
					return false;
				}
			} else
				return false;
		}
		
		AddinSystemConfiguration Configuration {
			get {
				if (config == null) {
					config = (AddinSystemConfiguration) ReadObject (RootConfigFile, typeof(AddinSystemConfiguration));
					if (config == null) {
						config = new AddinSystemConfiguration ();
						RegisterRepository ("http://go-mono.com/md/main.mrep", false);
					}
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
		
		internal static string GetUninstallErrorNoRoot (AddinInfo ainfo)
		{
			return string.Format (GettextCatalog.GetString ("The addin '{0} v{1}' can't be uninstalled with the current user permissions."), ainfo.Id, ainfo.Version);
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
