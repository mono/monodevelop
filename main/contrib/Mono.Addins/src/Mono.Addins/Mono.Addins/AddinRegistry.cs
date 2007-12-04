//
// AddinRegistry.cs
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
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using Mono.Addins.Database;
using Mono.Addins.Description;

namespace Mono.Addins
{
	public class AddinRegistry: IDisposable
	{
		AddinDatabase database;
		StringCollection addinDirs;
		string basePath;
		string currentDomain;
		string startupDirectory;
		
		public AddinRegistry (string registryPath): this (registryPath, null)
		{
		}
		
		public AddinRegistry (string registryPath, string startupDirectory)
		{
			basePath = Util.GetFullPath (registryPath);
			database = new AddinDatabase (this);

			// Look for add-ins in the hosts directory and in the default
			// addins directory
			addinDirs = new StringCollection ();
			addinDirs.Add (database.HostsPath);
			addinDirs.Add (DefaultAddinsFolder);
			
			// Get the domain corresponding to the startup folder
			if (startupDirectory != null) {
				this.startupDirectory = startupDirectory;
				currentDomain = database.GetFolderDomain (null, startupDirectory);
			} else
				currentDomain = AddinDatabase.GlobalDomain;
		}
		
		public static AddinRegistry GetGlobalRegistry ()
		{
			return GetGlobalRegistry (null);
		}
		
		internal static AddinRegistry GetGlobalRegistry (string startupDirectory)
		{
			AddinRegistry reg = new AddinRegistry (GlobalRegistryPath, startupDirectory);
			string baseDir;
			if (Util.IsWindows)
				baseDir = Environment.GetFolderPath (Environment.SpecialFolder.CommonProgramFiles); 
			else
				baseDir = "/etc";
			
			reg.AddinDirectories.Add (Path.Combine (baseDir, "mono.addins"));
			return reg;
		}
		
		internal static string GlobalRegistryPath {
			get {
				string customDir = Environment.GetEnvironmentVariable ("MONO_ADDINS_GLOBAL_REGISTRY");
				if (customDir != null && customDir.Length > 0)
					return Util.GetFullPath (customDir);
				
				string path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData); 
				path = Path.Combine (path, "mono.addins");
				return Util.GetFullPath (path);
			}
		}
		
		public string RegistryPath {
			get { return basePath; }
		}
		
		public void Dispose ()
		{
			database.Shutdown ();
		}
		
		public Addin GetAddin (string id)
		{
			return database.GetInstalledAddin (currentDomain, id);
		}
		
		public Addin GetAddin (string id, bool exactVersionMatch)
		{
			return database.GetInstalledAddin (currentDomain, id, exactVersionMatch);
		}
		
		public Addin[] GetAddins ()
		{
			ArrayList list = database.GetInstalledAddins (currentDomain, AddinType.Addin);
			return (Addin[]) list.ToArray (typeof(Addin));
		}
		
		public Addin[] GetAddinRoots ()
		{
			ArrayList list = database.GetInstalledAddins (currentDomain, AddinType.Root);
			return (Addin[]) list.ToArray (typeof(Addin));
		}
		
		public AddinDescription GetAddinDescription (IProgressStatus progressStatus, string file)
		{
			string outFile = Path.GetTempFileName ();
			try {
				database.ParseAddin (progressStatus, file, outFile, false);
			}
			catch {
				File.Delete (outFile);
				throw;
			}
			
			try {
				AddinDescription desc = AddinDescription.Read (outFile);
				if (desc != null) {
					desc.AddinFile = file;
					desc.OwnerDatabase = database;
				}
				return desc;
			}
			catch {
				// Errors are already reported using the progress status object
				return null;
			}
			finally {
				File.Delete (outFile);
			}
		}
		
		public AddinDescription ReadAddinManifestFile (string file)
		{
			AddinDescription desc = AddinDescription.Read (file);
			desc.OwnerDatabase = database;
			return desc;
		}
		
		public bool IsAddinEnabled (string id)
		{
			return database.IsAddinEnabled (currentDomain, id);
		}
		
		public void EnableAddin (string id)
		{
			database.EnableAddin (currentDomain, id, true);
		}
		
		public void DisableAddin (string id)
		{
			database.DisableAddin (currentDomain, id);
		}
		
		public void DumpFile (string file)
		{
			Mono.Addins.Serialization.BinaryXmlReader.DumpFile (file);
		}
		
		public void ResetConfiguration ()
		{
			database.ResetConfiguration ();
		}
		
		internal void NotifyDatabaseUpdated ()
		{
			if (startupDirectory != null)
				currentDomain = database.GetFolderDomain (null, startupDirectory);
		}

		public void Update (IProgressStatus monitor)
		{
			database.Update (monitor, currentDomain);
		}

		public void Rebuild (IProgressStatus monitor)
		{
			database.Repair (monitor, currentDomain);
		}
		
		internal Addin GetAddinForHostAssembly (string filePath)
		{
			return database.GetAddinForHostAssembly (currentDomain, filePath);
		}
		
		internal bool AddinDependsOn (string id1, string id2)
		{
			return database.AddinDependsOn (currentDomain, id1, id2);
		}
		
		internal void ScanFolders (IProgressStatus monitor, string folderToScan, StringCollection filesToIgnore)
		{
			database.ScanFolders (monitor, folderToScan, filesToIgnore);
		}
		
		internal void ParseAddin (IProgressStatus progressStatus, string file, string outFile)
		{
			database.ParseAddin (progressStatus, file, outFile, true);
		}
		
		public string DefaultAddinsFolder {
			get { return Path.Combine (basePath, "addins"); }
		}
		
		internal StringCollection AddinDirectories {
			get { return addinDirs; }
		}
		
		internal bool CreateHostAddinsFile (string hostFile)
		{
			hostFile = Util.GetFullPath (hostFile);
			string baseName = Path.GetFileNameWithoutExtension (hostFile);
			if (!Directory.Exists (database.HostsPath))
				Directory.CreateDirectory (database.HostsPath);
			
			foreach (string s in Directory.GetFiles (database.HostsPath, baseName + "*.addins")) {
				try {
					using (StreamReader sr = new StreamReader (s)) {
						XmlTextReader tr = new XmlTextReader (sr);
						tr.MoveToContent ();
						string host = tr.GetAttribute ("host-reference");
						if (host == hostFile)
							return false;
					}
				}
				catch {
					// Ignore this file
				}
			}
			
			string file = Path.Combine (database.HostsPath, baseName) + ".addins";
			int n=1;
			while (File.Exists (file)) {
				file = Path.Combine (database.HostsPath, baseName) + "_" + n + ".addins";
				n++;
			}
			
			using (StreamWriter sw = new StreamWriter (file)) {
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				tw.WriteStartElement ("Addins");
				tw.WriteAttributeString ("host-reference", hostFile);
				tw.WriteStartElement ("Directory");
				tw.WriteAttributeString ("shared", "false");
				tw.WriteString (Path.GetDirectoryName (hostFile));
				tw.WriteEndElement ();
				tw.Close ();
			}
			return true;
		}
		
		internal static string[] GetRegisteredStartupFolders (string registryPath)
		{
			string dbDir = Path.Combine (registryPath, "addin-db-" + AddinDatabase.VersionTag);
			dbDir = Path.Combine (dbDir, "hosts");
			
			if (!Directory.Exists (dbDir))
				return new string [0];
			
			ArrayList dirs = new ArrayList ();
			
			foreach (string s in Directory.GetFiles (dbDir, "*.addins")) {
				try {
					using (StreamReader sr = new StreamReader (s)) {
						XmlTextReader tr = new XmlTextReader (sr);
						tr.MoveToContent ();
						string host = tr.GetAttribute ("host-reference");
						dirs.Add (Path.GetDirectoryName (host));
					}
				}
				catch {
					// Ignore this file
				}
			}
			return (string[]) dirs.ToArray (typeof(string));
		}
	}
}
