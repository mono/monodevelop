//
// AddinData.cs
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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinData: IDisposable
	{
		DotNetProject project;
		AddinRegistry registry;
		string lastOutputPath;
		
		[ItemProperty]
		string registryPath;
		
		[ItemProperty]
		bool isRoot;
		
		string absRegistryPath;
		
		AddinDescription manifest;
		AddinDescription compiledManifest;
		FileSystemWatcher watcher;
		DateTime lastNotifiedTimestamp;
		
		public event EventHandler Changed;
		internal static event AddinSupportEventHandler AddinSupportChanged;
		
		internal AddinData ()
		{
			AddinAuthoringService.Init ();
		}
		
		internal AddinData (DotNetProject project)
		{
			Bind (project);
		}
		
		internal void Bind (DotNetProject project)
		{
			this.project = project;
			
			watcher = new FileSystemWatcher (Path.GetDirectoryName (AddinManifestFileName));
			watcher.Filter = Path.GetFileName (AddinManifestFileName);
			watcher.Changed += OnDescFileChanged;
			watcher.EnableRaisingEvents = true;
			lastOutputPath = Path.GetDirectoryName (Project.GetOutputFileName (Project.DefaultConfigurationId));
			
			SyncRoot ();
			SyncReferences ();
		}
		
		void OnDescFileChanged (object s, EventArgs a)
		{
			Gtk.Application.Invoke (delegate {
				DateTime tim = File.GetLastWriteTime (AddinManifestFileName);
				if (tim != lastNotifiedTimestamp) {
					lastNotifiedTimestamp = tim;
					NotifyChanged ();
				}
			});
		}
		
		public void Dispose ()
		{
			watcher.Dispose ();
		}

		
		public static AddinData GetAddinData (DotNetProject project)
		{
			AddinData data = project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] as AddinData;
			if (data != null)
				data.Bind (project);
			return data;
		}
		
		public static AddinData EnableAddinAuthoringSupport (DotNetProject project)
		{
			AddinData data = GetAddinData (project);
			if (data != null)
				return data;
			
			data = new AddinData (project);
			project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] = data;
			AddinSupportChanged (project, true);
			return data;
		}
		
		public static void DisableAddinAuthoringSupport (DotNetProject project)
		{
			AddinData data = GetAddinData (project);
			project.ExtendedProperties.Remove ("MonoDevelop.AddinAuthoring");
			if (data != null)
				AddinSupportChanged (project, false);
		}
		
		public DotNetProject Project {
			get { return project; }
		}
		
		public AddinDescription CachedAddinManifest {
			get {
				if (manifest == null)
					manifest = LoadAddinManifest ();
				return manifest;
			}
		}
		
		public AddinDescription LoadAddinManifest ()
		{
			return AddinRegistry.ReadAddinManifestFile (AddinManifestFileName);
		}
		
		public string AddinManifestFileName {
			get {
				foreach (ProjectFile pf in project.Files) {
					if (pf.FilePath.ToString ().EndsWith (".addin") || pf.FilePath.ToString ().EndsWith (".addin.xml"))
						return pf.FilePath;
				}
				
				AddinDescription desc = new AddinDescription ();
				string file = Path.Combine (project.BaseDirectory, "manifest.addin.xml");
				desc.Save (file);
				project.AddFile (file, BuildAction.EmbeddedResource);
				return file;
			}
		}
		
		public AddinDescription CompiledAddinManifest {
			get {
				if (compiledManifest == null) {
					if (File.Exists (project.GetOutputFileName (project.DefaultConfigurationId)))
						compiledManifest = registry.GetAddinDescription (null, project.GetOutputFileName (project.DefaultConfigurationId));
				}
				return compiledManifest;
			}
		}
		
		public string RegistryPath {
			get {
				if (absRegistryPath != null)
					return absRegistryPath;
				if (registryPath == null)
					return null;
				
				if (registryPath.StartsWith ("~")) {
					absRegistryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
					absRegistryPath = Path.Combine (absRegistryPath, registryPath.Substring (2));
				}
				else if (Path.IsPathRooted (registryPath)) {
					absRegistryPath = registryPath;
				} else {
					absRegistryPath = Path.Combine (this.project.BaseDirectory, registryPath);
				}
				absRegistryPath = FileService.GetFullPath (absRegistryPath);
				return absRegistryPath;
			}
			set {
				absRegistryPath = value;
				string pf = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				if (value.StartsWith (pf + Path.DirectorySeparatorChar) || value == pf) {
					registryPath = "~" + value.Substring (pf.Length);
				} else {
					pf = project.BaseDirectory;
					if (value.StartsWith (pf + Path.DirectorySeparatorChar) || value == pf) {
						registryPath = FileService.AbsoluteToRelativePath (pf, value);
					}
					else
						registryPath = value;
				}
				NotifyChanged ();
			}
		}
		
		public AddinRegistry AddinRegistry {
			get {
				if (registry != null)
					return registry;
				return SetRegistry ();
			}
			set {
				registry = value;
				RegistryPath = registry.RegistryPath;
			}
		}
		
		AddinRegistry SetRegistry ()
		{
			if (RegistryPath == null)
				return registry = AddinRegistry.GetGlobalRegistry ();
			else {
				if (isRoot) {
					string outDir = Path.GetDirectoryName (Project.GetOutputFileName (Project.DefaultConfigurationId));
					registry = new AddinRegistry (RegistryPath, outDir);
					Console.WriteLine ("pp reg is root: " + RegistryPath + " - " + outDir);
				} else {
					string[] s = AddinRegistry.GetRegisteredStartupFolders (RegistryPath);
					if (s.Length > 0) {
						registry = new AddinRegistry (RegistryPath, s[0]);
						Console.WriteLine ("pp reg nor1: " + RegistryPath + " - " + s[0]);
					}
					else {
						registry = new AddinRegistry (RegistryPath);
						Console.WriteLine ("pp reg nor2: " + RegistryPath);
					}
				}
				return registry;
			}
		}
		
		internal void CheckOutputPath ()
		{
			if (CachedAddinManifest.IsRoot) {
				string outDir = Path.GetDirectoryName (Project.GetOutputFileName (Project.DefaultConfigurationId));
				if (lastOutputPath != outDir) {
					registry = null;
					NotifyChanged ();
				}
			}
		}
		
		public void NotifyChanged ()
		{
			manifest = null;
			SyncRoot ();
			SyncReferences ();
			
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		void SyncRoot ()
		{
			if (CachedAddinManifest.IsRoot != isRoot) {
				isRoot = CachedAddinManifest.IsRoot;
				registry = null;
				manifest = null;
			}
		}
		
		void SyncReferences ()
		{
			bool changed = false;
			Hashtable addinRefs = new Hashtable ();
			foreach (AddinDependency adep in CachedAddinManifest.MainModule.Dependencies) {
				bool found = false;
				foreach (ProjectReference pr in Project.References) {
					if ((pr is AddinProjectReference) && pr.Reference == adep.FullAddinId) {
						found = true;
						break;
					}
				}
				if (!found) {
					AddinProjectReference ar = new AddinProjectReference (adep.FullAddinId);
					Project.References.Add (ar);
					changed = true;
				}
				addinRefs [adep.FullAddinId] = adep;
			}
			
			ArrayList toDelete = new ArrayList ();
			foreach (ProjectReference pr in Project.References) {
				if ((pr is AddinProjectReference) && !addinRefs.ContainsKey (pr.Reference))
					toDelete.Add (pr);
			}
			foreach (ProjectReference pr in toDelete)
				Project.References.Remove (pr);
			
			if (changed || toDelete.Count > 0)
				Project.Save (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
		}
		
		internal static ExtensionNodeDescriptionCollection GetExtensionNodes (AddinRegistry registry, AddinDescription desc, string path)
		{
			ArrayList extensions = new ArrayList ();
			CollectExtensions (desc, path, extensions);
			foreach (Dependency dep in desc.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null) continue;
				Addin addin = registry.GetAddin (adep.FullAddinId);
				if (addin != null)
					CollectExtensions (addin.Description, path, extensions);
			}
			
			// Sort the extensions, to make sure they are added in the correct order
			// That is, deepest children last.
			extensions.Sort (new ExtensionComparer ());
			
			ExtensionNodeDescriptionCollection nodes = new ExtensionNodeDescriptionCollection ();
			
			// Add the nodes
			foreach (Extension ext in extensions) {
				string subp = path.Substring (ext.Path.Length);
				ExtensionNodeDescriptionCollection col = ext.ExtensionNodes;
				foreach (string p in subp.Split ('/')) {
					if (p.Length == 0) continue;
					ExtensionNodeDescription node = col [p];
					if (node == null) {
						col = null;
						break;
					}
					else
						col = node.ChildNodes;
				}
				if (col != null)
					nodes.AddRange (col);
			}
			return nodes;
		}
		
		static void CollectExtensions (AddinDescription desc, string path, ArrayList extensions)
		{
			foreach (Extension ext in desc.MainModule.Extensions) {
				if (ext.Path == path || path.StartsWith (ext.Path + "/"))
					extensions.Add (ext);
			}
		}
	}

	internal delegate void AddinSupportEventHandler (Project project, bool enabled);
}
