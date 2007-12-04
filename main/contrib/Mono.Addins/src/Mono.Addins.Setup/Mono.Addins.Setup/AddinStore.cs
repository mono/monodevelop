//
// AddinStore.cs
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
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using ICSharpCode.SharpZipLib.Zip;
using Mono.Addins;
using Mono.Addins.Setup.ProgressMonitoring;
using Mono.Addins.Description;
using Mono.Addins.Serialization;

namespace Mono.Addins.Setup
{
	internal class AddinStore
	{
		SetupService service;
		
		public AddinStore (SetupService service)
		{
			this.service = service;
		}
		
		internal void ResetCachedData ()
		{
		}
		
		public AddinRegistry Registry {
			get { return service.Registry; }
		}
		
		public bool Install (IProgressStatus statusMonitor, params string[] files)
		{
			Package[] packages = new Package [files.Length];
			for (int n=0; n<files.Length; n++)
				packages [n] = AddinPackage.FromFile (files [n]);

			return Install (statusMonitor, packages);
		}
		
		public bool Install (IProgressStatus statusMonitor, params AddinRepositoryEntry[] addins)
		{
			Package[] packages = new Package [addins.Length];
			for (int n=0; n<addins.Length; n++)
				packages [n] = AddinPackage.FromRepository (addins [n]);

			return Install (statusMonitor, packages);
		}
		
		internal bool Install (IProgressStatus monitor, params Package[] packages)
		{
			PackageCollection packs = new PackageCollection ();
			packs.AddRange (packages);
			return Install (monitor, packs);
		}
		
		internal bool Install (IProgressStatus statusMonitor, PackageCollection packs)
		{
			// Make sure the registry is up to date
			service.Registry.Update (statusMonitor);
			
			IProgressMonitor monitor = ProgressStatusMonitor.GetProgressMonitor (statusMonitor);
		
			PackageCollection toUninstall;
			DependencyCollection unresolved;
			if (!ResolveDependencies (monitor, packs, out toUninstall, out unresolved)) {
				monitor.ReportError ("Not all dependencies could be resolved.", null);
				return false;
			}
			
			ArrayList prepared = new ArrayList ();
			ArrayList uninstallPrepared = new ArrayList ();
			bool rollback = false;
			
			monitor.BeginTask ("Installing add-ins...", 100);
			
			// Prepare install
			
			monitor.BeginStepTask ("Initializing installation", toUninstall.Count + packs.Count + 1, 75);
			
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
			
			monitor.BeginStepTask ("Installing", toUninstall.Count + packs.Count + 1, 20);
			
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
				monitor.BeginStepTask ("Finishing installation", (prepared.Count + uninstallPrepared.Count)*2 + 1, 5);
			
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
				monitor.BeginStepTask ("Finishing installation", prepared.Count + uninstallPrepared.Count + 1, 5);
			
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
			
			// Update the extension maps
			service.Registry.Update (statusMonitor);
			
			monitor.EndTask ();

			monitor.EndTask ();
			
			service.SaveConfiguration ();
			ResetCachedData ();
			
			return !rollback;
		}
		
		public void Uninstall (IProgressStatus statusMonitor, string id)
		{
			IProgressMonitor monitor = ProgressStatusMonitor.GetProgressMonitor (statusMonitor);
		
			bool rollback = false;
			ArrayList toUninstall = new ArrayList ();
			ArrayList uninstallPrepared = new ArrayList ();
			
			Addin ia = service.Registry.GetAddin (id);
			if (ia == null)
				throw new InstallException ("The add-in '" + id + "' is not installed.");

			toUninstall.Add (AddinPackage.FromInstalledAddin (ia));

			Addin[] deps = GetDependentAddins (id, true);
			foreach (Addin dep in deps)
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
			
			// Update the extension maps
			service.Registry.Update (statusMonitor);
			
			monitor.EndTask ();
			
			service.SaveConfiguration ();
			ResetCachedData ();
		}
		
		public Addin[] GetDependentAddins (string id, bool recursive)
		{
			ArrayList list = new ArrayList ();
			FindDependentAddins (list, id, recursive);
			return (Addin[]) list.ToArray (typeof (Addin));
		}
		
		void FindDependentAddins (ArrayList list, string id, bool recursive)
		{
			foreach (Addin iaddin in service.Registry.GetAddins ()) {
				if (list.Contains (iaddin))
					continue;
				foreach (Dependency dep in iaddin.Description.MainModule.Dependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep != null && adep.AddinId == id) {
						list.Add (iaddin);
						if (recursive)
							FindDependentAddins (list, iaddin.Id, true);
					}
				}
			}
		}
		
		public bool ResolveDependencies (IProgressStatus statusMonitor, AddinRepositoryEntry[] addins, out PackageCollection resolved, out PackageCollection toUninstall, out DependencyCollection unresolved)
		{
			resolved = new PackageCollection ();
			for (int n=0; n<addins.Length; n++)
				resolved.Add (AddinPackage.FromRepository (addins [n]));
			return ResolveDependencies (statusMonitor, resolved, out toUninstall, out unresolved);
		}
		
		public bool ResolveDependencies (IProgressStatus statusMonitor, PackageCollection packages, out PackageCollection toUninstall, out DependencyCollection unresolved)
		{
			IProgressMonitor monitor = ProgressStatusMonitor.GetProgressMonitor (statusMonitor);
			return ResolveDependencies (monitor, packages, out toUninstall, out unresolved);
		}
		
		internal bool ResolveDependencies (IProgressMonitor monitor, PackageCollection packages, out PackageCollection toUninstall, out DependencyCollection unresolved)
		{
			PackageCollection requested = new PackageCollection();
			requested.AddRange (packages);
			
			unresolved = new DependencyCollection ();
			toUninstall = new PackageCollection ();
			PackageCollection installedRequired = new PackageCollection ();
			
			for (int n=0; n<packages.Count; n++) {
				Package p = packages [n];
				p.Resolve (monitor, this, packages, toUninstall, installedRequired, unresolved);
			}
			
			if (unresolved.Count != 0) {
				foreach (Dependency dep in unresolved)
					monitor.ReportError (string.Format ("The package '{0}' could not be found in any repository", dep.Name), null);
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
					Addin ia = service.Registry.GetAddin (ap.Addin.Id);
					if (File.Exists (ia.AddinFile) && !HasWriteAccess (ia.AddinFile)) {
						monitor.ReportError (GetUninstallErrorNoRoot (ap.Addin), null);
						return false;
					}
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
								monitor.ReportError ("Can't install two versions of the same add-in: '" + ap.Addin.Name + "'.", null);
								error = true;
							} else {
								packages.RemoveAt (k);
							}
						} else if (otherap.IsUpgradeOf (ap)) {
							if (requested.Contains (ap)) {
								monitor.ReportError ("Can't install two versions of the same add-in: '" + ap.Addin.Name + "'.", null);
								error = true;
							} else {
								packages.RemoveAt (n);
								n--;
							}
						} else {
							error = true;
							monitor.ReportError ("Can't install two versions of the same add-in: '" + ap.Addin.Name + "'.", null);
						}
						break;
					}
				}
			}
			
			return !error;
		}
		
		internal void ResolveDependency (IProgressMonitor monitor, Dependency dep, AddinPackage parentPackage, PackageCollection toInstall, PackageCollection toUninstall, PackageCollection installedRequired, DependencyCollection unresolved)
		{
			AddinDependency adep = dep as AddinDependency;
			if (adep == null)
				return;
			
			string nsid = Addin.GetFullId (parentPackage.Addin.Namespace, adep.AddinId, null);
			
			foreach (Package p in toInstall) {
				AddinPackage ap = p as AddinPackage;
				if (ap != null) {
					if (Addin.GetIdName (ap.Addin.Id) == nsid && ((AddinInfo)ap.Addin).SupportsVersion (adep.Version))
						return;
				} 
			}
			
			ArrayList addins = new ArrayList ();
			addins.AddRange (service.Registry.GetAddins ());
			addins.AddRange (service.Registry.GetAddinRoots ());
			
			foreach (Addin addin in addins) {
				if (Addin.GetIdName (addin.Id) == nsid && addin.SupportsVersion (adep.Version)) {
					AddinPackage p = AddinPackage.FromInstalledAddin (addin);
					if (!installedRequired.Contains (p))
						installedRequired.Add (p);
					return;
				}
			}
			
			AddinRepositoryEntry[] avaddins = service.Repositories.GetAvailableAddins ();
			foreach (PackageRepositoryEntry avAddin in avaddins) {
				if (Addin.GetIdName (avAddin.Addin.Id) == nsid && ((AddinInfo)avAddin.Addin).SupportsVersion (adep.Version)) {
					toInstall.Add (AddinPackage.FromRepository (avAddin));
					return;
				}
			}
			unresolved.Add (adep);
		}

		internal string GetAddinDirectory (AddinInfo info)
		{
			return Path.Combine (service.InstallDirectory, info.Id.Replace (',','.'));
		}
		
		internal void RegisterAddin (IProgressMonitor monitor, AddinInfo info, string sourceDir)
		{
			monitor.Log.WriteLine ("Installing " + info.Name + " v" + info.Version);
			string addinDir = GetAddinDirectory (info);
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
	
			foreach (string file in Directory.GetFiles (src)) {
				if (Path.GetFileName (file) != "addin.info")
					File.Copy (file, Path.Combine (destDir, Path.GetFileName (file)));
			}
	
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
		
		static XmlSerializer GetSerializer (Type type)
		{
			if (type == typeof(AddinSystemConfiguration))
				return new AddinSystemConfigurationSerializer ();
			else if (type == typeof(Repository))
				return new RepositorySerializer ();
			else
				return new XmlSerializer (type);
		}
		
		internal static object ReadObject (string file, Type type)
		{
			if (!File.Exists (file))
				return null;

			StreamReader r = new StreamReader (file);
			try {
				XmlSerializer ser = GetSerializer (type);
				return ser.Deserialize (r);
			} catch {
				return null;
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
				XmlSerializer ser = GetSerializer (obj.GetType());
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

			monitor.BeginTask ("Requesting " + url, 2);
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.Headers ["Pragma"] = "no-cache";
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
		
		internal static string GetUninstallErrorNoRoot (AddinHeader ainfo)
		{
			return string.Format ("The add-in '{0} v{1}' can't be uninstalled with the current user permissions.", ainfo.Name, ainfo.Version);
		}
	}
}
