//
// AddinPackage.cs
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Net;

using ICSharpCode.SharpZipLib.Zip;
using Mono.Addins;
using Mono.Addins.Description;

namespace Mono.Addins.Setup
{
	internal class AddinPackage: Package
	{
		AddinInfo info;
		string packFile;
		string url;
		string tempFolder;
		string configFile;
		bool installed;
		Addin iaddin;
		
		public AddinHeader Addin {
			get { return info; }
		}
		
		public override string Name {
			get { return info.Name + " v" + info.Version; }
		}
		
		public static AddinPackage PackageFromRepository (AddinRepositoryEntry repAddin)
		{
			AddinPackage pack = new AddinPackage ();
			pack.info = (AddinInfo) repAddin.Addin;
			pack.url = new Uri (new Uri (repAddin.RepositoryUrl), repAddin.Url).ToString ();
			return pack;
		}
		
		public static AddinPackage PackageFromFile (string file)
		{
			AddinPackage pack = new AddinPackage ();
			pack.info = ReadAddinInfo (file);
			pack.packFile = file;
			return pack;
		}
		
		public static AddinPackage FromInstalledAddin (Addin sinfo)
		{
			AddinPackage pack = new AddinPackage ();
			pack.info = AddinInfo.ReadFromDescription (sinfo.Description);
			return pack;
		}
		
		static AddinInfo ReadAddinInfo (string file)
		{
			ZipFile zfile = new ZipFile (file);
			foreach (ZipEntry ze in zfile) {
				if (ze.Name == "addin.info") {
					using (Stream s = zfile.GetInputStream (ze)) {
						return AddinInfo.ReadFromAddinFile (new StreamReader (s));
					}
				}
			}
			throw new InstallException ("Addin configuration file not found in package.");
		}
		
		internal override bool IsUpgradeOf (Package p)
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
		
		internal override void PrepareInstall (IProgressMonitor monitor, AddinStore service)
		{
			if (service.Registry.GetAddin (Mono.Addins.Addin.GetFullId (info.Namespace, info.Id, info.Version), true) != null)
				throw new InstallException ("The addin " + info.Name + " v" + info.Version + " is already installed.");
						
			if (url != null)
				packFile = service.DownloadFile (monitor, url);
			
			tempFolder = CreateTempFolder ();

			// Extract the files			
			using (FileStream fs = new FileStream (packFile, FileMode.Open, FileAccess.Read)) {
				ZipFile zip = new ZipFile (fs);
				foreach (ZipEntry entry in zip) {
					string path = Path.Combine (tempFolder, entry.Name);
					string dir = Path.GetDirectoryName (path);
					if (!Directory.Exists (dir))
						Directory.CreateDirectory (dir);
						
					byte[] buffer = new byte [8192];
					int n=0;
					Stream inStream = zip.GetInputStream (entry);
					Stream outStream = null;
					try {
						outStream = File.Create (path);
						while ((n = inStream.Read (buffer, 0, buffer.Length)) > 0)
							outStream.Write (buffer, 0, n);
					} finally {
						inStream.Close ();
						if (outStream != null)
							outStream.Close ();
					}
				}
			}
			
			foreach (string s in Directory.GetFiles (tempFolder)) {
				if (Path.GetFileName (s) == "addin.info") {
					configFile = s;
					break;
				}
			}

			if (configFile == null)
				throw new InstallException ("Add-in information file not found in package.");
		}
		
		internal override void CommitInstall (IProgressMonitor monitor, AddinStore service)
		{
			service.RegisterAddin (monitor, info, tempFolder);
			installed = true;
		}
		
		internal override void RollbackInstall (IProgressMonitor monitor, AddinStore service)
		{
			if (installed) {
				iaddin = service.Registry.GetAddin (info.Id);
				if (iaddin != null)
					CommitUninstall (monitor, service);
			}
		}
		
		internal override void EndInstall (IProgressMonitor monitor, AddinStore service)
		{
			if (url != null && packFile != null)
				File.Delete (packFile);
			if (tempFolder != null)
				Directory.Delete (tempFolder, true);
		}
		
		internal override void Resolve (IProgressMonitor monitor, AddinStore service, PackageCollection toInstall, PackageCollection toUninstall, PackageCollection installedRequired, DependencyCollection unresolved)
		{
			Addin ia = service.Registry.GetAddin (info.Id);
			
			if (ia != null) {
				Package p = AddinPackage.FromInstalledAddin (ia);
				if (!toUninstall.Contains (p))
					toUninstall.Add (p);
					
				if (!info.SupportsVersion (ia.Version)) {
				
					// This addin breaks the api of the currently installed one,
					// it has to be removed, together with all dependencies
					
					Addin[] ainfos = service.GetDependentAddins (info.Id, true);
					foreach (Addin ainfo in ainfos) {
						p = AddinPackage.FromInstalledAddin (ainfo);
						if (!toUninstall.Contains (p))
							toUninstall.Add (p);
					}
				}
			}
			
			foreach (Dependency dep in info.Dependencies) {
				service.ResolveDependency (monitor, dep, this, toInstall, toUninstall, installedRequired, unresolved);
			}
		}
		
		internal override void PrepareUninstall (IProgressMonitor monitor, AddinStore service)
		{
			iaddin = service.Registry.GetAddin (info.Id, true);
			if (iaddin == null)
				throw new InstallException (string.Format ("The add-in '{0}' is not installed.", info.Name));

			AddinDescription conf = iaddin.Description;
			string basePath = Path.GetDirectoryName (conf.AddinFile);
			
			if (!File.Exists (iaddin.AddinFile)) {
				monitor.ReportWarning (string.Format ("The add-in '{0}' is scheduled for uninstalling, but the add-in file could not be found.", info.Name));
				return;
			}
			
			if (!service.HasWriteAccess (iaddin.AddinFile))
				throw new InstallException (AddinStore.GetUninstallErrorNoRoot (info));

			foreach (string relPath in conf.AllFiles) {
				string path = Path.Combine (basePath, relPath);
				if (!File.Exists (path))
					continue;
				if (!service.HasWriteAccess (path))
					throw new InstallException (AddinStore.GetUninstallErrorNoRoot (info));
			}
			
			tempFolder = CreateTempFolder ();
			CopyAddinFiles (monitor, conf, iaddin.AddinFile, tempFolder);
		}
		
		internal override void CommitUninstall (IProgressMonitor monitor, AddinStore service)
		{
			if (tempFolder == null)
				return;

			monitor.Log.WriteLine ("Uninstalling " + info.Name + " v" + info.Version);
			
			AddinDescription conf = iaddin.Description;
			string basePath = Path.GetDirectoryName (conf.AddinFile);
			
			foreach (string relPath in conf.AllFiles) {
				string path = Path.Combine (basePath, relPath);
				if (!File.Exists (path))
					continue;
				File.Delete (path);
			}
			
			File.Delete (iaddin.AddinFile);
			
			if (Directory.GetFiles (basePath).Length == 0) {
				try {
					Directory.Delete (basePath);
				} catch {
					monitor.ReportWarning ("Directory " + basePath + " could not be deleted.");
				}
			}
			
			monitor.Log.WriteLine ("Done");
		}
		
		internal override void RollbackUninstall (IProgressMonitor monitor, AddinStore service)
		{
			if (tempFolder != null) {
				AddinDescription conf = iaddin.Description;
				string configFile = Path.Combine (tempFolder, Path.GetFileName (iaddin.AddinFile));
				
				string addinDir = Path.GetDirectoryName (iaddin.AddinFile);
				CopyAddinFiles (monitor, conf, configFile, addinDir);
			}
		}
		
		internal override void EndUninstall (IProgressMonitor monitor, AddinStore service)
		{
			if (tempFolder != null)
				Directory.Delete (tempFolder, true);
			tempFolder = null;
		}
		
		void CopyAddinFiles (IProgressMonitor monitor, AddinDescription conf, string configFile, string destPath)
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
