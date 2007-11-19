//
// AddinDatabase.cs
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
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Reflection;
using Mono.Addins.Description;

namespace Mono.Addins.Database
{
	class AddinDatabase
	{
		public const string GlobalDomain = "global";
		
		public const string VersionTag = "001";

		ArrayList allSetupInfos;
		ArrayList addinSetupInfos;
		ArrayList rootSetupInfos;
		internal static bool RunningSetupProcess;
		bool fatalDatabseError;
		Hashtable cachedAddinSetupInfos = new Hashtable ();
		AddinScanResult currentScanResult;
		AddinHostIndex hostIndex;
		FileDatabase fileDatabase;
		string addinDbDir;
		DatabaseConfiguration config = null;
		AddinRegistry registry;
		int lastDomainId;
		
		public AddinDatabase (AddinRegistry registry)
		{
			this.registry = registry;
			addinDbDir = Path.Combine (registry.RegistryPath, "addin-db-" + VersionTag);
			fileDatabase = new FileDatabase (AddinDbDir);
		}
		
		string AddinDbDir {
			get { return addinDbDir; }
		}
		
		public string AddinCachePath {
			get { return Path.Combine (AddinDbDir, "addin-data"); }
		}
		
		public string AddinFolderCachePath {
			get { return Path.Combine (AddinDbDir, "addin-dir-data"); }
		}
		
		public string AddinPrivateDataPath {
			get { return Path.Combine (AddinDbDir, "addin-priv-data"); }
		}
		
		public string HostsPath {
			get { return Path.Combine (AddinDbDir, "hosts"); }
		}
		
		string HostIndexFile {
			get { return Path.Combine (AddinDbDir, "host-index"); }
		}
		
		string ConfigFile {
			get { return Path.Combine (AddinDbDir, "config.xml"); }
		}
		
		internal bool IsGlobalRegistry {
			get {
				return registry.RegistryPath == AddinRegistry.GlobalRegistryPath;
			}
		}
		
		public void Clear ()
		{
			if (Directory.Exists (AddinCachePath))
				Directory.Delete (AddinCachePath, true);
			if (Directory.Exists (AddinFolderCachePath))
				Directory.Delete (AddinFolderCachePath, true);
		}
		
		public ExtensionNodeSet FindNodeSet (string domain, string addinId, string id)
		{
			return FindNodeSet (domain, addinId, id, new Hashtable ());
		}
		
		ExtensionNodeSet FindNodeSet (string domain, string addinId, string id, Hashtable visited)
		{
			if (visited.Contains (addinId))
				return null;
			visited.Add (addinId, addinId);
			Addin addin = GetInstalledAddin (domain, addinId, true, false);
			if (addin == null)
				return null;
			AddinDescription desc = addin.Description;
			if (desc == null)
				return null;
			foreach (ExtensionNodeSet nset in desc.ExtensionNodeSets)
				if (nset.Id == id)
					return nset;
			
			// Not found in the add-in. Look on add-ins on which it depends
			
			foreach (Dependency dep in desc.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null) continue;
				
				string aid = Addin.GetFullId (desc.Namespace, adep.AddinId, adep.Version);
				ExtensionNodeSet nset = FindNodeSet (domain, aid, id, visited);
				if (nset != null)
					return nset;
			}
			return null;
		}
		
		public ArrayList GetInstalledAddins (string domain, AddinType type)
		{
			if (type == AddinType.All) {
				if (allSetupInfos != null)
					return allSetupInfos;
			}
			else if (type == AddinType.Addin) {
				if (addinSetupInfos != null)
					return addinSetupInfos;
			}
			else {
				if (rootSetupInfos != null)
					return rootSetupInfos;
			}
		
			InternalCheck (domain);
			
			using (fileDatabase.LockRead ()) {
				return InternalGetInstalledAddins (domain, null, type);
			}
		}
		
		ArrayList InternalGetInstalledAddins (string domain, AddinType type)
		{
			return InternalGetInstalledAddins (domain, null, type);
		}
		
		ArrayList InternalGetInstalledAddins (string domain, string idFilter, AddinType type)
		{
			if (allSetupInfos == null) {
				ArrayList alist = new ArrayList ();

				// Global add-ins are valid for any private domain
				if (domain != AddinDatabase.GlobalDomain)
					FindInstalledAddins (alist, AddinDatabase.GlobalDomain, idFilter);

				FindInstalledAddins (alist, domain, idFilter);
				if (idFilter != null)
					return alist;
				allSetupInfos = alist;
			}
			if (type == AddinType.All)
				return FilterById (allSetupInfos, idFilter);
			
			if (type == AddinType.Addin) {
				if (addinSetupInfos == null) {
					addinSetupInfos = new ArrayList ();
					foreach (Addin adn in allSetupInfos)
						if (!adn.Description.IsRoot)
							addinSetupInfos.Add (adn);
				}
				return FilterById (addinSetupInfos, idFilter);
			}
			else {
				if (rootSetupInfos == null) {
					rootSetupInfos = new ArrayList ();
					foreach (Addin adn in allSetupInfos)
						if (adn.Description.IsRoot)
							rootSetupInfos.Add (adn);
				}
				return FilterById (rootSetupInfos, idFilter);
			}
		}
		
		ArrayList FilterById (ArrayList addins, string id)
		{
			if (id == null)
				return addins;
			ArrayList list = new ArrayList ();
			foreach (Addin adn in addins) {
				if (Addin.GetIdName (adn.Id) == id)
					list.Add (adn);
			}
			return list;
		}

		void FindInstalledAddins (ArrayList result, string domain, string idFilter)
		{
			if (idFilter == null) 
				idFilter = "*";
			string dir = Path.Combine (AddinCachePath, domain);
			if (Directory.Exists (dir)) {
				foreach (string file in fileDatabase.GetDirectoryFiles (dir, idFilter + ",*.maddin")) {
					string id = Path.GetFileNameWithoutExtension (file);
					result.Add (GetInstalledDomainAddin (domain, id, true, false, false));
				}
			}
		}

		public Addin GetInstalledAddin (string domain, string id)
		{
			return GetInstalledAddin (domain, id, false, false);
		}
		
		public Addin GetInstalledAddin (string domain, string id, bool exactVersionMatch)
		{
			return GetInstalledAddin (domain, id, exactVersionMatch, false);
		}
		
		public Addin GetInstalledAddin (string domain, string id, bool exactVersionMatch, bool enabledOnly)
		{
			// Try the given domain, and if not found, try the shared domain
			Addin ad = GetInstalledDomainAddin (domain, id, exactVersionMatch, enabledOnly, true);
			if (ad != null)
				return ad;
			if (domain != AddinDatabase.GlobalDomain)
				return GetInstalledDomainAddin (AddinDatabase.GlobalDomain, id, exactVersionMatch, enabledOnly, true);
			else
				return null;
		}
		
		Addin GetInstalledDomainAddin (string domain, string id, bool exactVersionMatch, bool enabledOnly, bool dbLockCheck)
		{
			Addin sinfo = null;
			string idd = id + " " + domain;
			object ob = cachedAddinSetupInfos [idd];
			if (ob != null) {
				sinfo = ob as Addin;
				if (sinfo != null) {
					if (!enabledOnly || sinfo.Enabled)
						return sinfo;
					if (exactVersionMatch)
						return null;
				}
				else if (enabledOnly)
					// Ignore the 'not installed' flag when disabled add-ins are allowed
					return null;
			}
		
			if (dbLockCheck)
				InternalCheck (domain);
			
			using ((dbLockCheck ? fileDatabase.LockRead () : null))
			{
				string path = GetDescriptionPath (domain, id);
				if (sinfo == null && fileDatabase.Exists (path)) {
					sinfo = new Addin (this, path);
					cachedAddinSetupInfos [idd] = sinfo;
					if (!enabledOnly || sinfo.Enabled)
						return sinfo;
					if (exactVersionMatch) {
						// Cache lookups with negative result
						cachedAddinSetupInfos [idd] = this;
						return null;
					}
				}
				
				// Exact version not found. Look for a compatible version
				if (!exactVersionMatch) {
					sinfo = null;
					string version, name, bestVersion = null;
					Addin.GetIdParts (id, out name, out version);
					
					foreach (Addin ia in InternalGetInstalledAddins (domain, name, AddinType.All)) 
					{
						if ((!enabledOnly || ia.Enabled) &&
						    (version.Length == 0 || ia.SupportsVersion (version)) && 
						    (bestVersion == null || Addin.CompareVersions (bestVersion, ia.Version) > 0)) 
						{
							bestVersion = ia.Version;
							sinfo = ia;
						}
					}
					if (sinfo != null) {
						cachedAddinSetupInfos [idd] = sinfo;
						return sinfo;
					}
				}
				
				// Cache lookups with negative result
				// Ignore the 'not installed' flag when disabled add-ins are allowed
				if (enabledOnly)
					cachedAddinSetupInfos [idd] = this;
				return null;
			}
		}
		
		public void Shutdown ()
		{
			ResetCachedData ();
		}
		
		public Addin GetAddinForHostAssembly (string domain, string assemblyLocation)
		{
			InternalCheck (domain);
			Addin ainfo = null;
			
			object ob = cachedAddinSetupInfos [assemblyLocation];
			if (ob != null)
				return ob as Addin; // Don't use a cast here is ob may not be an Addin.

			AddinHostIndex index = GetAddinHostIndex ();
			string addin, addinFile, rdomain;
			if (index.GetAddinForAssembly (assemblyLocation, out addin, out addinFile, out rdomain)) {
				string sid = addin + " " + rdomain;
				ainfo = cachedAddinSetupInfos [sid] as Addin;
				if (ainfo == null)
					ainfo = new Addin (this, GetDescriptionPath (rdomain, addin));
				cachedAddinSetupInfos [assemblyLocation] = ainfo;
				cachedAddinSetupInfos [addin + " " + rdomain] = ainfo;
			}
			
			return ainfo;
		}
		
		
		public bool IsAddinEnabled (string domain, string id)
		{
			Addin ainfo = GetInstalledAddin (domain, id);
			if (ainfo != null)
				return ainfo.Enabled;
			else
				return false;
		}
		
		internal bool IsAddinEnabled (string domain, string id, bool exactVersionMatch)
		{
			if (!exactVersionMatch)
				return IsAddinEnabled (domain, id);
			Addin ainfo = GetInstalledAddin (domain, id, exactVersionMatch, false);
			if (ainfo == null)
				return false;
			return Configuration.IsEnabled (id, ainfo.AddinInfo.EnabledByDefault);
		}
		
		public void EnableAddin (string domain, string id)
		{
			EnableAddin (domain, id, true);
		}
		
		internal void EnableAddin (string domain, string id, bool exactVersionMatch)
		{
			Addin ainfo = GetInstalledAddin (domain, id, exactVersionMatch, false);
			if (ainfo == null)
				// It may be an add-in root
				return;

			if (IsAddinEnabled (domain, id))
				return;
			
			// Enable required add-ins
			
			foreach (Dependency dep in ainfo.AddinInfo.Dependencies) {
				if (dep is AddinDependency) {
					AddinDependency adep = dep as AddinDependency;
					string adepid = Addin.GetFullId (ainfo.AddinInfo.Namespace, adep.AddinId, adep.Version);
					EnableAddin (domain, adepid, false);
				}
			}

			Configuration.SetStatus (id, true, ainfo.AddinInfo.EnabledByDefault);
			SaveConfiguration ();

			if (AddinManager.IsInitialized && AddinManager.Registry.RegistryPath == registry.RegistryPath)
				AddinManager.SessionService.ActivateAddin (id);
		}
		
		public void DisableAddin (string domain, string id)
		{
			Addin ai = GetInstalledAddin (domain, id, true);
			if (ai == null)
				throw new InvalidOperationException ("Add-in '" + id + "' not installed.");

			if (!IsAddinEnabled (domain, id))
				return;
			
			Configuration.SetStatus (id, false, ai.AddinInfo.EnabledByDefault);
			SaveConfiguration ();
			
			// Disable all add-ins which depend on it
			
			try {
				string idName = Addin.GetIdName (id);
				
				foreach (Addin ainfo in GetInstalledAddins (domain, AddinType.Addin)) {
					foreach (Dependency dep in ainfo.AddinInfo.Dependencies) {
						AddinDependency adep = dep as AddinDependency;
						if (adep == null)
							continue;
						
						string adepid = Addin.GetFullId (ainfo.AddinInfo.Namespace, adep.AddinId, null);
						if (adepid != idName)
							continue;
						
						// The add-in that has been disabled, might be a requeriment of this one, or maybe not
						// if there is an older version available. Check it now.
						
						adepid = Addin.GetFullId (ainfo.AddinInfo.Namespace, adep.AddinId, adep.Version);
						Addin adepinfo = GetInstalledAddin (domain, adepid, false, true);
						
						if (adepinfo == null) {
							DisableAddin (domain, ainfo.Id);
							break;
						}
					}
				}
			}
			catch {
				// If something goes wrong, enable the add-in again
				Configuration.SetStatus (id, true, ai.AddinInfo.EnabledByDefault);
				SaveConfiguration ();
				throw;
			}

			if (AddinManager.IsInitialized && AddinManager.Registry.RegistryPath == registry.RegistryPath)
				AddinManager.SessionService.UnloadAddin (id);
		}		

		internal string GetDescriptionPath (string domain, string id)
		{
			return Path.Combine (Path.Combine (AddinCachePath, domain), id + ".maddin");
		}
		
		void InternalCheck (string domain)
		{
			// If the database is broken, don't try to regenerate it at every check.
			if (fatalDatabseError)
				return;

			bool update = false;
			using (fileDatabase.LockRead ()) {
				if (!Directory.Exists (AddinCachePath)) {
					update = true;
				}
			}
			if (update)
				Update (null, domain);
		}
		
		void GenerateAddinExtensionMapsInternal (IProgressStatus monitor, ArrayList addinsToUpdate, ArrayList removedAddins)
		{
			AddinUpdateData updateData = new AddinUpdateData (this, monitor);
			
			// Clear cached data
			cachedAddinSetupInfos.Clear ();
			
			// Collect all information
			
			AddinIndex addinHash = new AddinIndex ();
			
			if (monitor.LogLevel > 1)
				monitor.Log ("Generating add-in extension maps");
			
			Hashtable changedAddins = null;
			ArrayList descriptionsToSave = new ArrayList ();
			ArrayList files = new ArrayList ();
			
			bool partialGeneration = addinsToUpdate != null;
			string[] domains = GetDomains ();
			
			// Get the files to be updated
			
			if (partialGeneration) {
				changedAddins = new Hashtable ();
				foreach (string s in addinsToUpdate) {
					changedAddins [s] = s;
					
					// Look for the add-in in any of the existing folders
					foreach (string domain in domains) {
						string mp = GetDescriptionPath (domain, s);
						if (fileDatabase.Exists (mp)) {
							files.Add (mp);
						}
					}
				}
				foreach (string s in removedAddins)
					changedAddins [s] = s;
			}
			else {
				foreach (string dom in domains)
					files.AddRange (fileDatabase.GetDirectoryFiles (Path.Combine (AddinCachePath, dom), "*.maddin"));
			}
			
			// Load the descriptions.
			foreach (string file in files) {
			
				AddinDescription conf;
				if (!ReadAddinDescription (monitor, file, out conf)) {
					SafeDelete (monitor, file);
					continue;
				}

				// If the original file does not exist, the description can be deleted
				if (!File.Exists (conf.AddinFile)) {
					SafeDelete (monitor, file);
					continue;
				}
				
				// Remove old data from the description. If changedAddins==null, removes all data.
				// Otherwise, removes data only from the addins in the table.
				
				conf.UnmergeExternalData (changedAddins);
				descriptionsToSave.Add (conf);
				
				addinHash.Add (conf);
			}

			// Sort the add-ins, to make sure add-ins are processed before
			// all their dependencies
			ArrayList sorted = addinHash.GetSortedAddins ();

			// Register extension points and node sets
			foreach (AddinDescription conf in sorted)
				CollectExtensionPointData (conf, updateData);
			
			// Register extensions
			foreach (AddinDescription conf in sorted)
				CollectExtensionData (conf, updateData);
			
			// Save the maps
			foreach (AddinDescription conf in descriptionsToSave)
				conf.SaveBinary (fileDatabase);
			
			if (monitor.LogLevel > 1) {
				monitor.Log ("Addin relation map generated.");
				monitor.Log ("  Addins Updated: " + descriptionsToSave.Count);
				monitor.Log ("  Extension points: " + updateData.RelExtensionPoints);
				monitor.Log ("  Extensions: " + updateData.RelExtensions);
				monitor.Log ("  Extension nodes: " + updateData.RelExtensionNodes);
				monitor.Log ("  Node sets: " + updateData.RelNodeSetTypes);
			}
		}
		
		// Collects extension data in a hash table. The key is the path, the value is a list
		// of add-ins ids that extend that path
		
		void CollectExtensionPointData (AddinDescription conf, AddinUpdateData updateData)
		{
			foreach (ExtensionNodeSet nset in conf.ExtensionNodeSets) {
				try {
					updateData.RegisterNodeSet (conf, nset);
					updateData.RelNodeSetTypes++;
				} catch (Exception ex) {
					throw new InvalidOperationException ("Error reading node set: " + nset.Id, ex);
				}
			}
			
			foreach (ExtensionPoint ep in conf.ExtensionPoints) {
				try {
					updateData.RegisterExtensionPoint (conf, ep);
					updateData.RelExtensionPoints++;
				} catch (Exception ex) {
					throw new InvalidOperationException ("Error reading extension point: " + ep.Path, ex);
				}
			}
		}
		
		void CollectExtensionData (AddinDescription conf, AddinUpdateData updateData)
		{
			foreach (ModuleDescription module in conf.AllModules) {
				foreach (Extension ext in module.Extensions) {
					updateData.RelExtensions++;
					updateData.RegisterExtension (conf, module, ext);
					AddChildExtensions (conf, module, updateData, ext.Path, ext.ExtensionNodes, false);
				}
			}
		}
		
		void AddChildExtensions (AddinDescription conf, ModuleDescription module, AddinUpdateData updateData, string path, ExtensionNodeDescriptionCollection nodes, bool conditionChildren)
		{
			// Don't register conditions as extension nodes.
			if (!conditionChildren)
				updateData.RegisterExtension (conf, module, path);
			
			foreach (ExtensionNodeDescription node in nodes) {
				if (node.NodeName == "ComplexCondition")
					continue;
				updateData.RelExtensionNodes++;
				string id = node.GetAttribute ("id");
				if (id.Length != 0)
					AddChildExtensions (conf, module, updateData, path + "/" + id, node.ChildNodes, node.NodeName == "Condition");
			}
		}
		
		string[] GetDomains ()
		{
			string[] dirs = fileDatabase.GetDirectories (AddinCachePath);
			string[] ids = new string [dirs.Length];
			for (int n=0; n<dirs.Length; n++)
				ids [n] = Path.GetFileName (dirs [n]);
			return ids;
		}

		public string GetUniqueDomainId ()
		{
			if (lastDomainId != 0) {
				lastDomainId++;
				return lastDomainId.ToString ();
			}
			lastDomainId = 1;
			foreach (string s in fileDatabase.GetDirectories (AddinCachePath)) {
				string dn = Path.GetFileName (s);
				if (dn == GlobalDomain)
					continue;
				try {
					int n = int.Parse (dn);
					if (n >= lastDomainId)
						lastDomainId = n + 1;
				} catch {
				}
			}
			return lastDomainId.ToString ();
		}
		
		internal void ResetCachedData ()
		{
			allSetupInfos = null;
			addinSetupInfos = null;
			rootSetupInfos = null;
			hostIndex = null;
			cachedAddinSetupInfos.Clear ();
		}
		
		
		public bool AddinDependsOn (string domain, string id1, string id2)
		{
			Addin addin1 = GetInstalledAddin (domain, id1, false);
			
			// We can assumbe that if the add-in is not returned here, it may be a root addin.
			if (addin1 == null)
				return false;

			id2 = Addin.GetIdName (id2);
			foreach (Dependency dep in addin1.AddinInfo.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null)
					continue;
				string depid = Addin.GetFullId (addin1.AddinInfo.Namespace, adep.AddinId, null);
				if (depid == id2)
					return true;
				else if (AddinDependsOn (domain, depid, id2))
					return true;
			}
			return false;
		}
		
		public void Repair (IProgressStatus monitor, string domain)
		{
			using (fileDatabase.LockWrite ()) {
				try {
					if (Directory.Exists (AddinCachePath))
						Directory.Delete (AddinCachePath, true);
					if (Directory.Exists (AddinFolderCachePath))
						Directory.Delete (AddinFolderCachePath, true);
					if (File.Exists (HostIndexFile))
						File.Delete (HostIndexFile);
				}
				catch (Exception ex) {
					monitor.ReportError ("The add-in registry could not be rebuilt. It may be due to lack of write permissions to the directory: " + AddinDbDir, ex);
				}
			}
			Update (monitor, domain);
		}
		
		public void Update (IProgressStatus monitor, string domain)
		{
			if (monitor == null)
				monitor = new ConsoleProgressStatus (false);

			if (RunningSetupProcess)
				return;
			
			fatalDatabseError = false;
			
			DateTime tim = DateTime.Now;
			
			Hashtable installed = new Hashtable ();
			bool changesFound = CheckFolders (monitor);
			
			if (monitor.IsCanceled)
				return;
			
			if (monitor.LogLevel > 1)
				monitor.Log ("Folders checked (" + (int) (DateTime.Now - tim).TotalMilliseconds + " ms)");
			
			if (changesFound) {
				// Something has changed, the add-ins need to be re-scanned, but it has
				// to be done in an external process
				
				if (domain != null) {
					using (fileDatabase.LockRead ()) {
						foreach (Addin ainfo in InternalGetInstalledAddins (domain, AddinType.Addin)) {
							installed [ainfo.Id] = ainfo.Id;
						}
					}
				}
				
				RunScannerProcess (monitor);
			
				ResetCachedData ();
				
				registry.NotifyDatabaseUpdated ();
			}
			
			if (fatalDatabseError)
				monitor.ReportError ("The add-in database could not be updated. It may be due to file corruption. Try running the setup repair utility", null);
			
			// Update the currently loaded add-ins
			if (changesFound && domain != null && AddinManager.IsInitialized && AddinManager.Registry.RegistryPath == registry.RegistryPath) {
				Hashtable newInstalled = new Hashtable ();
				foreach (Addin ainfo in GetInstalledAddins (domain, AddinType.Addin)) {
					newInstalled [ainfo.Id] = ainfo.Id;
				}
				
				foreach (string aid in installed.Keys) {
					if (!newInstalled.Contains (aid))
						AddinManager.SessionService.UnloadAddin (aid);
				}
				
				foreach (string aid in newInstalled.Keys) {
					if (!installed.Contains (aid)) {
						AddinManager.SessionService.ActivateAddin (aid);
					}
				}
			}
		}
		
		void RunScannerProcess (IProgressStatus monitor)
		{
			IProgressStatus scanMonitor = monitor;
			ArrayList pparams = new ArrayList ();
			pparams.Add (null); // scan folder
			
			bool retry = false;
			do {
				try {
					if (monitor.LogLevel > 1)
						monitor.Log ("Looking for addins");
					SetupProcess.ExecuteCommand (scanMonitor, registry.RegistryPath, AddinManager.StartupDirectory, "scan", (string[]) pparams.ToArray (typeof(string)));
					retry = false;
				}
				catch (Exception ex) {
					ProcessFailedException pex = ex as ProcessFailedException;
					if (pex != null) {
						// Get the last logged operation.
						if (pex.LastLog.StartsWith ("scan:")) {
							// It crashed while scanning a file. Add the file to the ignore list and try again.
							string file = pex.LastLog.Substring (5);
							pparams.Add (file);
							monitor.ReportWarning ("Could not scan file: " + file);
							retry = true;
							continue;
						}
					}
					fatalDatabseError = true;
					// If the process has crashed, try to do a new scan, this time using verbose log,
					// to give the user more information about the origin of the crash.
					if (pex != null && !retry) {
						monitor.ReportError ("Add-in scan operation failed. The Mono runtime may have encountered an error while trying to load an assembly.", null);
						if (monitor.LogLevel <= 1) {
							// Re-scan again using verbose log, to make it easy to find the origin of the error.
							retry = true;
							scanMonitor = new ConsoleProgressStatus (true);
						}
					} else
						retry = false;
					
					if (!retry) {
						monitor.ReportError ("Add-in scan operation failed", (ex is ProcessFailedException ? null : ex));
						monitor.Cancel ();
						return;
					}
				}
			}
			while (retry);
		}
		
		bool DatabaseInfrastructureCheck (IProgressStatus monitor)
		{
			// Do some sanity check, to make sure the basic database infrastructure can be created
			
			bool hasChanges = false;
			
			try {
			
				if (!Directory.Exists (AddinCachePath)) {
					Directory.CreateDirectory (AddinCachePath);
					hasChanges = true;
				}
			
				if (!Directory.Exists (AddinFolderCachePath)) {
					Directory.CreateDirectory (AddinFolderCachePath);
					hasChanges = true;
				}
			
				// Make sure we can write in those folders

				Util.CheckWrittableFloder (AddinCachePath);
				Util.CheckWrittableFloder (AddinFolderCachePath);
				
				fatalDatabseError = false;
			}
			catch (Exception ex) {
				monitor.ReportError ("Add-in cache directory could not be created", ex);
				fatalDatabseError = true;
				monitor.Cancel ();
			}
			return hasChanges;
		}
		
		
		internal bool CheckFolders (IProgressStatus monitor)
		{
			using (fileDatabase.LockRead ()) {
				AddinScanResult scanResult = new AddinScanResult ();
				scanResult.CheckOnly = true;
				InternalScanFolders (monitor, scanResult);
				return scanResult.ChangesFound;
			}
		}
		
		internal void ScanFolders (IProgressStatus monitor, string folderToScan, StringCollection filesToIgnore)
		{
			AddinScanResult res = new AddinScanResult ();
			res.FilesToIgnore = filesToIgnore;
			ScanFolders (monitor, res);
		}
		
		internal void ScanFolders (IProgressStatus monitor, AddinScanResult scanResult)
		{
			IDisposable checkLock = null;
			
			if (scanResult.CheckOnly)
				checkLock = fileDatabase.LockRead ();
			else {
				// All changes are done in a transaction, which won't be committed until
				// all files have been updated.
				
				if (!fileDatabase.BeginTransaction ()) {
					// The database is already being updated. Can't do anything for now.
					return;
				}
			}
			
			EventInfo einfo = typeof(AppDomain).GetEvent ("ReflectionOnlyAssemblyResolve");
			ResolveEventHandler resolver = new ResolveEventHandler (OnResolveAddinAssembly);
			
			try
			{
				// Perform the add-in scan
				
				if (!scanResult.CheckOnly) {
					AppDomain.CurrentDomain.AssemblyResolve += resolver;
					if (einfo != null) einfo.AddEventHandler (AppDomain.CurrentDomain, resolver);
				}
				
				InternalScanFolders (monitor, scanResult);
				
				if (!scanResult.CheckOnly)
					fileDatabase.CommitTransaction ();
			}
			catch {
				if (!scanResult.CheckOnly)
					fileDatabase.RollbackTransaction ();
				throw;
			}
			finally {
				currentScanResult = null;
				
				if (scanResult.CheckOnly)
					checkLock.Dispose ();
				else {
					AppDomain.CurrentDomain.AssemblyResolve -= resolver;
					if (einfo != null) einfo.RemoveEventHandler (AppDomain.CurrentDomain, resolver);
				}
			}
		}
		
		void InternalScanFolders (IProgressStatus monitor, AddinScanResult scanResult)
		{
			DateTime tim = DateTime.Now;
			
			DatabaseInfrastructureCheck (monitor);
			if (monitor.IsCanceled)
				return;
			
			try {
				scanResult.HostIndex = GetAddinHostIndex ();
			}
			catch (Exception ex) {
				if (scanResult.CheckOnly) {
					scanResult.ChangesFound = true;
					return;
				}
				monitor.ReportError ("Add-in root index is corrupt. The add-in database will be regenerated.", ex);
				scanResult.RegenerateAllData = true;
			}
			
			AddinScanner scanner = new AddinScanner (this);
			
			// Check if any of the previously scanned folders has been deleted
			
			foreach (string file in Directory.GetFiles (AddinFolderCachePath, "*.data")) {
				AddinScanFolderInfo folderInfo;
				bool res = ReadFolderInfo (monitor, file, out folderInfo);
				if (!res || !Directory.Exists (folderInfo.Folder)) {
					if (res) {
						// Folder has been deleted. Remove the add-ins it had.
						scanner.UpdateDeletedAddins (monitor, folderInfo, scanResult);
					}
					else {
						// Folder info file corrupt. Regenerate all.
						scanResult.ChangesFound = true;
						scanResult.RegenerateRelationData = true;
					}
					
					if (!scanResult.CheckOnly)
						SafeDelete (monitor, file);
					else
						return;
				}
			}
			
			// Look for changes in the add-in folders
			
			foreach (string dir in registry.AddinDirectories) {
				if (dir == registry.DefaultAddinsFolder)
					scanner.ScanFolderRec (monitor, dir, GlobalDomain, scanResult);
				else
					scanner.ScanFolder (monitor, dir, GlobalDomain, scanResult);
				if (scanResult.CheckOnly) {
					if (scanResult.ChangesFound || monitor.IsCanceled)
						return;
				}
			}
			
			if (scanResult.CheckOnly)
				return;
			
			// Scan the files which have been modified
			
			currentScanResult = scanResult;

			foreach (FileToScan file in scanResult.FilesToScan)
				scanner.ScanFile (monitor, file.File, file.AddinScanFolderInfo, scanResult);

			// Save folder info
			
			foreach (AddinScanFolderInfo finfo in scanResult.ModifiedFolderInfos)
				SaveFolderInfo (monitor, finfo);

			if (monitor.LogLevel > 1)
				monitor.Log ("Folders scan completed (" + (int) (DateTime.Now - tim).TotalMilliseconds + " ms)");

			SaveAddinHostIndex ();
			ResetCachedData ();
			
			if (!scanResult.ChangesFound) {
				if (monitor.LogLevel > 1)
					monitor.Log ("No changes found");
				return;
			}
			
			tim = DateTime.Now;
			try {
				if (scanResult.RegenerateRelationData)
					scanResult.AddinsToUpdateRelations = null;
				
				GenerateAddinExtensionMapsInternal (monitor, scanResult.AddinsToUpdateRelations, scanResult.RemovedAddins);
			}
			catch (Exception ex) {
				fatalDatabseError = true;
				monitor.ReportError ("The add-in database could not be updated. It may be due to file corruption. Try running the setup repair utility", ex);
			}
			
			if (monitor.LogLevel > 1)
				monitor.Log ("Add-in relations analyzed (" + (int) (DateTime.Now - tim).TotalMilliseconds + " ms)");
			
			SaveAddinHostIndex ();
		}
		
		public void ParseAddin (IProgressStatus progressStatus, string file, string outFile, bool inProcess)
		{
			if (!inProcess) {
				SetupProcess.ExecuteCommand (progressStatus, registry.RegistryPath, AddinManager.StartupDirectory, "get-desc", Path.GetFullPath (file), outFile);
				return;
			}
			
			using (fileDatabase.LockRead ())
			{
				// First of all, check if the file belongs to a registered add-in
				AddinScanFolderInfo finfo;
				if (GetFolderInfoForPath (progressStatus, Path.GetDirectoryName (file), out finfo) && finfo != null) {
					AddinFileInfo afi = finfo.GetAddinFileInfo (file);
					if (afi != null && afi.IsAddin) {
						AddinDescription adesc;
						GetAddinDescription (progressStatus, afi.Domain, afi.AddinId, out adesc);
						if (adesc != null)
							adesc.Save (outFile);
						return;
					}
				}
				
				
				AddinScanner scanner = new AddinScanner (this);
				
				SingleFileAssemblyResolver res = new SingleFileAssemblyResolver (progressStatus, registry, scanner);
				ResolveEventHandler resolver = new ResolveEventHandler (res.Resolve);

				EventInfo einfo = typeof(AppDomain).GetEvent ("ReflectionOnlyAssemblyResolve");
				
				try {
					AppDomain.CurrentDomain.AssemblyResolve += resolver;
					if (einfo != null) einfo.AddEventHandler (AppDomain.CurrentDomain, resolver);
				
					AddinDescription desc = scanner.ScanSingleFile (progressStatus, file, new AddinScanResult ());
					if (desc != null)
						desc.Save (outFile);
				}
				finally {
					AppDomain.CurrentDomain.AssemblyResolve -= resolver;
					if (einfo != null) einfo.RemoveEventHandler (AppDomain.CurrentDomain, resolver);
				}
			}
		}
		
		public string GetFolderDomain (IProgressStatus progressStatus, string path)
		{
			AddinScanFolderInfo folderInfo;
			if (GetFolderInfoForPath (progressStatus, path, out folderInfo) && folderInfo != null && !folderInfo.SharedFolder)
				return folderInfo.Domain;
			else
				return GlobalDomain;
		}
		
		Assembly OnResolveAddinAssembly (object s, ResolveEventArgs args)
		{
			string file = currentScanResult.GetAssemblyLocation (args.Name);
			if (file != null)
				return Util.LoadAssemblyForReflection (file);
			else {
				Console.WriteLine ("Assembly not found: " + args.Name);
				return null;
			}
		}
		
		public string GetFolderConfigFile (string path)
		{
			path = Path.GetFullPath (path);
			
			string s = path.Replace ("_", "__");
			s = s.Replace (Path.DirectorySeparatorChar, '_');
			s = s.Replace (Path.AltDirectorySeparatorChar, '_');
			s = s.Replace (Path.VolumeSeparatorChar, '_');
			
			return Path.Combine (AddinFolderCachePath, s + ".data");
		}
		
		internal void UninstallAddin (IProgressStatus monitor, string domain, string addinId, AddinScanResult scanResult)
		{
			scanResult.AddRemovedAddin (addinId);
			string file = GetDescriptionPath (domain, addinId);
			if (!fileDatabase.Exists (file)) {
				return;
			}
			
			// Add-in already existed. The dependencies of the old add-in need to be re-analized

			AddinDescription desc;
			if (ReadAddinDescription (monitor, file, out desc)) {
				Util.AddDependencies (desc, scanResult);
				if (desc.IsRoot)
					scanResult.HostIndex.RemoveHostData (desc.AddinId, desc.AddinFile);
			} else
				// If we can't get information about the old assembly, just regenerate all relation data
				scanResult.RegenerateRelationData = true;

			SafeDelete (monitor, file);
			string dir = Path.GetDirectoryName (file);
			if (fileDatabase.DirectoryIsEmpty (dir))
				SafeDeleteDir (monitor, dir);
			SafeDeleteDir (monitor, Path.Combine (AddinPrivateDataPath, Path.GetFileNameWithoutExtension (file)));
		}
		
		public bool GetAddinDescription (IProgressStatus monitor, string domain, string addinId, out AddinDescription description)
		{
			string file = GetDescriptionPath (domain, addinId);
			return ReadAddinDescription (monitor, file, out description);
		}
		
		public bool ReadAddinDescription (IProgressStatus monitor, string file, out AddinDescription description)
		{
			try {
				description = AddinDescription.ReadBinary (fileDatabase, file);
				if (description != null)
					description.OwnerDatabase = this;
				return true;
			}
			catch (Exception ex) {
				if (monitor == null)
					throw;
				description = null;
				monitor.ReportError ("Could not read folder info file", ex);
				return false;
			}
		}
		
		public bool SaveDescription (IProgressStatus monitor, AddinDescription desc, string replaceFileName)
		{
			try {
				if (replaceFileName != null)
					desc.SaveBinary (fileDatabase, replaceFileName);
				else {
					string file = GetDescriptionPath (desc.Domain, desc.AddinId);
					string dir = Path.GetDirectoryName (file);
					if (!fileDatabase.DirExists (dir))
						fileDatabase.CreateDir (dir);
					desc.SaveBinary (fileDatabase, file);
				}
				return true;
			}
			catch (Exception ex) {
				monitor.ReportError ("Add-in info file could not be saved", ex);
				return false;
			}
		}
		
		public bool AddinDescriptionExists (string domain, string addinId)
		{
			string file = GetDescriptionPath (domain, addinId);
			return fileDatabase.Exists (file);
		}
		
		public bool ReadFolderInfo (IProgressStatus monitor, string file, out AddinScanFolderInfo folderInfo)
		{
			try {
				folderInfo = AddinScanFolderInfo.Read (fileDatabase, file);
				return true;
			}
			catch (Exception ex) {
				folderInfo = null;
				monitor.ReportError ("Could not read folder info file", ex);
				return false;
			}
		}
		
		public bool GetFolderInfoForPath (IProgressStatus monitor, string path, out AddinScanFolderInfo folderInfo)
		{
			try {
				folderInfo = AddinScanFolderInfo.Read (fileDatabase, AddinFolderCachePath, path);
				return true;
			}
			catch (Exception ex) {
				folderInfo = null;
				if (monitor != null)
					monitor.ReportError ("Could not read folder info file", ex);
				return false;
			}
		}

		public bool SaveFolderInfo (IProgressStatus monitor, AddinScanFolderInfo folderInfo)
		{
			try {
				folderInfo.Write (fileDatabase, AddinFolderCachePath);
				return true;
			}
			catch (Exception ex) {
				monitor.ReportError ("Could not write folder info file", ex);
				return false;
			}
		}
		
		public bool DeleteFolderInfo (IProgressStatus monitor, AddinScanFolderInfo folderInfo)
		{
			return SafeDelete (monitor, folderInfo.FileName);
		}
		
		public bool SafeDelete (IProgressStatus monitor, string file)
		{
			try {
				fileDatabase.Delete (file);
				return true;
			}
			catch (Exception ex) {
				if (monitor.LogLevel > 1) {
					monitor.Log ("Could not delete file: " + file);
					monitor.Log (ex.ToString ());
				}
				return false;
			}
		}
		
		public bool SafeDeleteDir (IProgressStatus monitor, string dir)
		{
			try {
				fileDatabase.DeleteDir (dir);
				return true;
			}
			catch (Exception ex) {
				if (monitor.LogLevel > 1) {
					monitor.Log ("Could not delete directory: " + dir);
					monitor.Log (ex.ToString ());
				}
				return false;
			}
		}
		
		AddinHostIndex GetAddinHostIndex ()
		{
			if (hostIndex != null)
				return hostIndex;
			
			using (fileDatabase.LockRead ()) {
				if (fileDatabase.Exists (HostIndexFile))
					hostIndex = AddinHostIndex.Read (fileDatabase, HostIndexFile);
				else
					hostIndex = new AddinHostIndex ();
			}
			return hostIndex;
		}
		
		void SaveAddinHostIndex ()
		{
			if (hostIndex != null)
				hostIndex.Write (fileDatabase, HostIndexFile);
		}
		
		internal string GetUniqueAddinId (string file, string oldId, string ns, string version)
		{
			string baseId = "__" + Path.GetFileNameWithoutExtension (file);

			if (Path.GetExtension (baseId) == ".addin")
				baseId = Path.GetFileNameWithoutExtension (baseId);
			
			string name = baseId;
			string id = Addin.GetFullId (ns, name, version);
			
			// If the old Id is already an automatically generated one, reuse it
			if (oldId != null && oldId.StartsWith (id))
				return name;
			
			int n = 1;
			while (AddinIdExists (id)) {
				name = baseId + "_" + n;
				id = Addin.GetFullId (ns, name, version);
				n++;
			}
			return name;
		}
		
		bool AddinIdExists (string id)
		{
			foreach (string d in fileDatabase.GetDirectories (AddinCachePath)) {
				if (fileDatabase.Exists (Path.Combine (d, id + ".addin")))
				    return true;
			}
			return false;
		}
		
		public void ResetConfiguration ()
		{
			if (File.Exists (ConfigFile))
				File.Delete (ConfigFile);
		}
		
		DatabaseConfiguration Configuration {
			get {
				if (config == null) {
					using (fileDatabase.LockRead ()) {
						if (fileDatabase.Exists (ConfigFile))
							config = DatabaseConfiguration.Read (ConfigFile);
						else
							config = new DatabaseConfiguration ();
					}
				}
				return config;
			}
		}
		
		void SaveConfiguration ()
		{
			if (config != null) {
				using (fileDatabase.LockWrite ()) {
					config.Write (ConfigFile);
				}
			}
		}
	}
	
	class SingleFileAssemblyResolver
	{
		AddinScanResult scanResult;
		AddinScanner scanner;
		AddinRegistry registry;
		IProgressStatus progressStatus;
		
		public SingleFileAssemblyResolver (IProgressStatus progressStatus, AddinRegistry registry, AddinScanner scanner)
		{
			this.scanner = scanner;
			this.registry = registry;
			this.progressStatus = progressStatus;
		}
		
		public Assembly Resolve (object s, ResolveEventArgs args)
		{
			if (scanResult == null) {
				scanResult = new AddinScanResult ();
				scanResult.LocateAssembliesOnly = true;
			
				foreach (string dir in registry.AddinDirectories)
					scanner.ScanFolder (progressStatus, dir, AddinDatabase.GlobalDomain, scanResult);
			}
		
			string afile = scanResult.GetAssemblyLocation (args.Name);
			if (afile != null)
				return Util.LoadAssemblyForReflection (afile);
			else
				return null;
		}
	}
	
	class AddinIndex
	{
		Hashtable addins = new Hashtable ();
		
		public void Add (AddinDescription desc)
		{
			string id = Addin.GetFullId (desc.Namespace, desc.LocalId, null);
			ArrayList list = (ArrayList) addins [id];
			if (list == null) {
				list = new ArrayList (); 
				addins [id] = list;
			}
			list.Add (desc);
		}
		
		ArrayList FindDescriptions (string domain, string fullid)
		{
			// Returns all registered add-ins which are compatible with the provided
			// fullid. Compatible means that the id is the same and the version is within
			// the range of compatible versions of the add-in.
			
			ArrayList res = new ArrayList ();
			string id = Addin.GetIdName (fullid);
			ArrayList list = (ArrayList) addins [id];
			if (list == null)
				return res;
			string version = Addin.GetIdVersion (fullid);
			foreach (AddinDescription desc in list) {
				if ((desc.Domain == domain || domain == AddinDatabase.GlobalDomain) && desc.SupportsVersion (version))
					res.Add (desc);
			}
			return res;
		}
		
		public ArrayList GetSortedAddins ()
		{
			Hashtable inserted = new Hashtable ();
			Hashtable lists = new Hashtable ();
			
			foreach (ArrayList dlist in addins.Values) {
				foreach (AddinDescription desc in dlist)
					InsertSortedAddin (inserted, lists, desc);
			}
			
			// Merge all domain lists into a single list.
			// Make sure the global domain is inserted the last
			
			ArrayList global = (ArrayList) lists [AddinDatabase.GlobalDomain];
			lists.Remove (AddinDatabase.GlobalDomain);
			
			ArrayList list = new ArrayList ();
			foreach (ArrayList dl in lists.Values) {
				list.AddRange (dl);
			}
			if (global != null)
				list.AddRange (global);
			return list;
		}

		void InsertSortedAddin (Hashtable inserted, Hashtable lists, AddinDescription desc)
		{
			string sid = desc.AddinId + " " + desc.Domain;
			if (inserted.ContainsKey (sid))
				return;
			inserted [sid] = desc;
			foreach (ModuleDescription mod in desc.AllModules) {
				foreach (Dependency dep in mod.Dependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep == null)
						continue;
					ArrayList descs = FindDescriptions (desc.Domain, adep.FullAddinId);
					if (descs.Count > 0) {
						foreach (AddinDescription sd in descs)
							InsertSortedAddin (inserted, lists, sd);
					}
//					else 
//						Console.WriteLine ("NOT FOUND: " + adep.FullAddinId + " " + desc.Domain + " from " + sid);
				}
			}
			ArrayList list = (ArrayList) lists [desc.Domain];
			if (list == null) {
				list = new ArrayList ();
				lists [desc.Domain] = list;
			}
			
			list.Add (desc);
		}
	}
	
	enum AddinType
	{
		Addin,
		Root,
		All
	}
}


