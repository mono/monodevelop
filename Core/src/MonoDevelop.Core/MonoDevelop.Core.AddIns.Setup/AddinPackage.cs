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
		
		public AddinInfo Addin {
			get { return info; }
		}
		
		public static AddinPackage FromAddin (AddinRepositoryEntry repAddin)
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
		
		public override void PrepareInstall (IProgressMonitor monitor, SetupService service)
		{
			if (service.GetInstalledAddin (info.Id, info.Version) != null)
				throw new InstallException ("The addin " + info.Id + " v" + info.Version + " is already installed.");
						
			if (url != null)
				packFile = service.DownloadFile (monitor, url);
			
			string bname = Path.Combine (Path.GetTempPath (), "mdtmp");
			tempFolder = bname;
			int n = 0;
			while (Directory.Exists (tempFolder))
				tempFolder = bname + (++n);
			
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
			service.RegisterAddin (monitor, info, tempFolder);
			installed = true;
		}
		
		public override void RollbackInstall (IProgressMonitor monitor, SetupService service)
		{
			if (installed)
				service.UnregisterAddin (monitor, info);
		}
		
		public override void EndInstall (IProgressMonitor monitor, SetupService service)
		{
			if (url != null && packFile != null)
				File.Delete (packFile);
			if (tempFolder != null)
				Directory.Delete (tempFolder, true);
		}
		
		public override void Resolve (IProgressMonitor monitor, SetupService service, PackageCollection packages, PackageDependencyCollection unresolved)
		{
			foreach (PackageDependency dep in info.Dependencies) {
				if (!dep.Resolve (monitor, service, packages))
					unresolved.Add (dep);
			}
		}
	}
}
