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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
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
		bool isRoot;
		bool updating;
		
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
			project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] = this;
			
			watcher = new FileSystemWatcher (Path.GetDirectoryName (AddinManifestFileName));
			watcher.Filter = Path.GetFileName (AddinManifestFileName);
			watcher.Changed += OnDescFileChanged;
			watcher.EnableRaisingEvents = true;
			lastOutputPath = Path.GetDirectoryName (Project.GetOutputFileName (ConfigurationSelector.Default));
			
			if (!string.IsNullOrEmpty (ApplicationName)) {
				project.ParentSolution.GetAddinData ().ApplicationName = ApplicationName;
			}
			
			if (project.ParentSolution != null) {
				SyncRoot ();
				SyncReferences ();
			}
			
			project.ReferenceAddedToProject += ProjectReferenceAddedToProject;
			project.ReferenceRemovedFromProject += ProjectReferenceRemovedFromProject;
		}
		
		void ProjectReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
		}

		void ProjectReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			if (updating)
				return;
			if (e.ProjectReference.ReferenceType == ReferenceType.Project) {
				DotNetProject rp = project.ParentSolution.FindProjectByName (e.ProjectReference.Reference) as DotNetProject;
				if (rp != null) {
					AddinData adata = AddinData.GetAddinData (rp);
					if (adata != null) {
						CachedAddinManifest.MainModule.Dependencies.Add (new AddinDependency (adata.CachedAddinManifest.AddinId));
						CachedAddinManifest.Save ();
						NotifyChanged (false);
					}
				}
			}
		}
		
		void OnDescFileChanged (object s, EventArgs a)
		{
			Gtk.Application.Invoke (delegate {
				DateTime tim = File.GetLastWriteTime (AddinManifestFileName);
				if (tim != lastNotifiedTimestamp) {
					lastNotifiedTimestamp = tim;
					NotifyChanged (true);
				}
			});
		}
		
		public void Dispose ()
		{
			if (watcher != null)
				watcher.Dispose ();
			project.ReferenceAddedToProject -= ProjectReferenceAddedToProject;
			project.ReferenceRemovedFromProject -= ProjectReferenceRemovedFromProject;
		}

		
		public static AddinData GetAddinData (DotNetProject project)
		{
			// Extensibility options are only available when a project belongs to a solution
			if (project.ParentSolution == null)
				return null;
			
			AddinData data = project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] as AddinData;
			if (data != null)
				return data;
			
			if (data == null) {
				foreach (ProjectFile pfile in project.Files) {
					if (pfile.Name.EndsWith (".addin.xml") || pfile.Name.EndsWith (".addin"))
						return new AddinData (project);
				}
			}
			return null;
		}
		
		public static AddinData EnableAddinAuthoringSupport (DotNetProject project)
		{
			AddinData data = GetAddinData (project);
			if (data != null)
				return data;
			
			data = new AddinData (project);
			project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] = data;
			if (AddinSupportChanged != null)
				AddinSupportChanged (project, true);
			return data;
		}
		
		public static void DisableAddinAuthoringSupport (DotNetProject project)
		{
			AddinData data = GetAddinData (project);
			project.ExtendedProperties.Remove ("MonoDevelop.AddinAuthoring");
			if (data != null && AddinSupportChanged != null)
				AddinSupportChanged (project, false);
		}
		
		public DotNetProject Project {
			get { return project; }
		}
		
		public bool IsRoot {
			get { return isRoot; }
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
			AddinDescription d = AddinRegistry.ReadAddinManifestFile (AddinManifestFileName);
			return d;
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
					if (File.Exists (project.GetOutputFileName (ConfigurationSelector.Default))) {
						compiledManifest = registry.GetAddinDescription (new ConsoleProgressStatus (false), project.GetOutputFileName (ConfigurationSelector.Default));
					}
				}
				return compiledManifest;
			}
		}
		
		public string ApplicationName {
			get { return project.ExtendedProperties ["ExtensionDomain"] as string; }
			set {
				if (string.IsNullOrEmpty (value))
					project.ExtendedProperties.Remove ("ExtensionDomain");
				else
					project.ExtendedProperties ["ExtensionDomain"] = value;
				project.ParentSolution.GetAddinData ().ApplicationName = value;
				NotifyChanged (true);
			}
		}

		public AddinRegistry AddinRegistry {
			get {
				if (registry != null)
					return registry;
				return SetRegistry ();
			}
		}
		
		AddinRegistry SetRegistry ()
		{
			if (Project.ParentSolution != null)
				return Project.ParentSolution.GetAddinRegistry ();
			else
				throw new InvalidOperationException ();
		}
		
		internal void CheckOutputPath ()
		{
			if (CachedAddinManifest.IsRoot) {
				string outDir = Path.GetDirectoryName (Project.GetOutputFileName (ConfigurationSelector.Default));
				if (lastOutputPath != outDir) {
					registry = null;
					NotifyChanged (true);
				}
			}
		}
		
		public void NotifyChanged ()
		{
			NotifyChanged (true);
		}
		
		public void NotifyChanged (bool externalChange)
		{
			if (externalChange) {
				manifest = null;
				SyncRoot ();
				SyncReferences ();
			}
			
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
					} else if (pr.ReferenceType == ReferenceType.Project) {
						DotNetProject rp = Project.ParentSolution.FindProjectByName (pr.Reference) as DotNetProject;
						if (rp != null) {
							AddinData ad = AddinData.GetAddinData (rp);
							if (ad != null && ad.CachedAddinManifest.AddinId == adep.FullAddinId) {
								found = true;
								break;
							}
						}
					}
				}
				if (!found) {
					try {
						updating = true;
						DotNetProject p = FindProjectImplementingAddin (adep.FullAddinId);
						if (p != null)
							Project.References.Add (new ProjectReference (p));
						else
							Project.References.Add (new AddinProjectReference (adep.FullAddinId));
					} finally {
						updating = false;
					}
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
		
		DotNetProject FindProjectImplementingAddin (string fullId)
		{
			if (Project.ParentSolution == null)
				return null;
			foreach (DotNetProject p in Project.ParentSolution.GetAllSolutionItems<DotNetProject> ()) {
				AddinData adata = AddinData.GetAddinData (p);
				if (adata != null && adata.CachedAddinManifest.AddinId == fullId)
					return p;
			}
			return null;
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
