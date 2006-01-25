//
// AddinPackage.cs
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
	public class AddinPackage: Package
	{
		AddinInfo info;
		string packFile;
		string url;
		string tempFolder;
		string configFile;
		bool installed;
		AddinSetupInfo iaddin;
		bool rootInstall = true;
		
		public bool RootInstall {
			get { return rootInstall; }
			set { rootInstall = value; }
		}
		
		public AddinInfo Addin {
			get { return info; }
		}
		
		public override string Name {
			get { return info.Name + " v" + info.Version; }
		}
		
		public static AddinPackage FromRepository (AddinRepositoryEntry repAddin)
		{
			AddinPackage pack = new AddinPackage ();
			pack.info = repAddin.Addin;
			pack.url = new Uri (new Uri (repAddin.Repository.Url), repAddin.Url).ToString ();
			return pack;
		}
		
		public static AddinPackage FromFile (string file)
		{
			AddinPackage pack = new AddinPackage ();
			pack.info = ReadAddinInfo (file);
			pack.packFile = file;
			return pack;
		}
		
		public static AddinPackage FromInstalledAddin (AddinSetupInfo sinfo)
		{
			AddinPackage pack = new AddinPackage ();
			pack.info = sinfo.Addin;
			pack.rootInstall = !sinfo.IsUserAddin;
			return pack;
		}
		
		static AddinInfo ReadAddinInfo (string file)
		{
			ZipFile zfile = new ZipFile (file);
			foreach (ZipEntry ze in zfile) {
				if (ze.Name.EndsWith (".addin.xml")) {
					using (Stream s = zfile.GetInputStream (ze)) {
						return AddinInfo.ReadFromAddinFile (new StreamReader (s));
					}
				}
			}
			throw new InstallException ("Addin configuration file not found in package.");
		}
		
		public override bool IsUpgradeOf (Package p)
		{
			AddinPackage ap = p as AddinPackage;
			if (ap == null) return false;
			return info.SupportsVersion (ap.info.Version);
		}
		
		public override bool Equals (object ob)
		{
			AddinPackage ap = ob as AddinPackage;
			if (ap == null) return false;
			return ap.info.Id == info.Id && ap.info.Version == info.Version;
		}
		
		public override int GetHashCode ()
		{
			return (info.Id + info.Version).GetHashCode ();
		}
		
		public override void PrepareInstall (IProgressMonitor monitor, SetupService service)
		{
			if (service.GetInstalledAddin (info.Id, info.Version) != null)
				throw new InstallException ("The addin " + info.Id + " v" + info.Version + " is already installed.");
						
			if (url != null)
				packFile = service.DownloadFile (monitor, url);
			
			tempFolder = CreateTempFolder ();
			
			ZipDecompressor zip = new ZipDecompressor ();
			using (FileStream fs = new FileStream (packFile, FileMode.Open, FileAccess.Read)) {
				zip.Extract (fs, tempFolder);
			}
			
			foreach (string s in Directory.GetFiles (tempFolder))
				if (s.EndsWith (".addin.xml")) {
					configFile = s;
					break;
				}

			if (configFile == null)
				throw new InstallException ("Addin configuration file not found in package.");
			
			AddinConfiguration.Check (configFile);
		}
		
		public override void CommitInstall (IProgressMonitor monitor, SetupService service)
		{
			service.RegisterAddin (monitor, info, tempFolder, !rootInstall);
			installed = true;
		}
		
		public override void RollbackInstall (IProgressMonitor monitor, SetupService service)
		{
			if (installed) {
				iaddin = service.GetInstalledAddin (info.Id);
				if (iaddin != null)
					CommitUninstall (monitor, service);
			}
		}
		
		public override void EndInstall (IProgressMonitor monitor, SetupService service)
		{
			if (url != null && packFile != null)
				File.Delete (packFile);
			if (tempFolder != null)
				Directory.Delete (tempFolder, true);
		}
		
		public override void Resolve (IProgressMonitor monitor, SetupService service, PackageCollection toInstall, PackageCollection toUninstall, PackageCollection installedRequired, PackageDependencyCollection unresolved)
		{
			AddinSetupInfo ia = service.GetInstalledAddin (info.Id);
			
			if (ia != null) {
				Package p = AddinPackage.FromInstalledAddin (ia);
				if (!toUninstall.Contains (p))
					toUninstall.Add (p);
					
				if (!info.SupportsVersion (ia.Addin.Version)) {
				
					// This addin breaks the api of the currently installed one,
					// it has to be removed, together with all dependencies
					
					AddinSetupInfo[] ainfos = service.GetDependentAddins (info.Id, true);
					foreach (AddinSetupInfo ainfo in ainfos) {
						p = AddinPackage.FromInstalledAddin (ainfo);
						if (!toUninstall.Contains (p))
							toUninstall.Add (p);
					}
				}
			}
			
			foreach (PackageDependency dep in info.Dependencies) {
				dep.Resolve (monitor, service, this, toInstall, toUninstall, installedRequired, unresolved);
			}
		}
		
		public override void PrepareUninstall (IProgressMonitor monitor, SetupService service)
		{
			string id = info.Id;
			iaddin = service.GetInstalledAddin (id, info.Version);
			if (iaddin == null)
				throw new InstallException (string.Format (GettextCatalog.GetString ("The add-in '{0}' is not installed."), id));

			AddinConfiguration conf = iaddin.GetConfiguration ();
			string basePath = Path.GetDirectoryName (iaddin.ConfigFile);
			
			if (!service.HasWriteAccess (iaddin.ConfigFile))
				throw new InstallException (SetupService.GetUninstallErrorNoRoot (info));

			foreach (string relPath in conf.AllFiles) {
				string path = Path.Combine (basePath, relPath);
				if (!File.Exists (path))
					continue;
				if (!service.HasWriteAccess (path))
					throw new InstallException (SetupService.GetUninstallErrorNoRoot (info));
			}
			
			tempFolder = CreateTempFolder ();
			CopyAddinFiles (monitor, conf, iaddin.ConfigFile, tempFolder);
		}
		
		public override void CommitUninstall (IProgressMonitor monitor, SetupService service)
		{
			monitor.Log.WriteLine ("Uninstalling " + info.Id + " v" + info.Version);
			
			AddinConfiguration conf = iaddin.GetConfiguration ();
			string basePath = Path.GetDirectoryName (iaddin.ConfigFile);
			
			foreach (string relPath in conf.AllFiles) {
				string path = Path.Combine (basePath, relPath);
				if (!File.Exists (path))
					continue;
				File.Delete (path);
			}
			
			File.Delete (iaddin.ConfigFile);
			
			if (Directory.GetFiles (basePath).Length == 0) {
				try {
					Directory.Delete (basePath);
				} catch (Exception ex) {
					monitor.ReportWarning ("Directory " + basePath + " could not be deleted.");
				}
			}
			
			monitor.Log.WriteLine ("Done");
		}
		
		public override void RollbackUninstall (IProgressMonitor monitor, SetupService service)
		{
			if (tempFolder != null) {
				string configFile = Path.Combine (tempFolder, Path.GetFileName (iaddin.ConfigFile));
				AddinConfiguration conf = AddinConfiguration.Read (configFile);
				
				string addinDir = Path.GetDirectoryName (iaddin.ConfigFile);
				CopyAddinFiles (monitor, conf, configFile, addinDir);
			}
		}
		
		public override void EndUninstall (IProgressMonitor monitor, SetupService service)
		{
			if (tempFolder != null)
				Directory.Delete (tempFolder, true);
			tempFolder = null;
		}
		
		void CopyAddinFiles (IProgressMonitor monitor, AddinConfiguration conf, string configFile, string destPath)
		{
			if (!Directory.Exists (destPath))
				Directory.CreateDirectory (destPath);
			
			string dfile = Path.Combine (destPath, Path.GetFileName (configFile));
			if (File.Exists (dfile))
				File.Delete (dfile);
				
			File.Copy (configFile, dfile);
			
			string basePath = Path.GetDirectoryName (configFile);
			
			foreach (string relPath in conf.AllFiles) {
				string path = Path.Combine (basePath, relPath);
				if (!File.Exists (path))
					continue;
				
				string destf = Path.Combine (destPath, Path.GetDirectoryName (relPath));
				if (!Directory.Exists (destf))
					Directory.CreateDirectory (destf);
					
				dfile = Path.Combine (destPath, relPath);
				if (File.Exists (dfile))
					File.Delete (dfile);

				File.Copy (path, dfile);
			}
		}
		
		
	}
}
