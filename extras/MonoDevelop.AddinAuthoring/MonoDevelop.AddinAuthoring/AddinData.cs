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
using MonoDevelop.Projects.Serialization;
using Mono.Addins;
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinData
	{
		Project project;
		AddinRegistry registry;
		
		[ItemProperty]
		string registryPath;
		
		string absRegistryPath;
		
		AddinDescription manifest;
		AddinDescription compiledManifest;
		
		public event EventHandler Changed;
		internal static event AddinSupportEventHandler AddinSupportChanged;
		
		internal AddinData ()
		{
		}
		
		internal AddinData (Project project)
		{
			Bind (project);
		}
		
		internal void Bind (Project project)
		{
			this.project = project;
		}
		
		public static AddinData GetAddinData (Project project)
		{
			AddinData data = project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] as AddinData;
			if (data != null)
				data.Bind (project);
			return data;
		}
		
		public static AddinData EnableAddinAuthoringSupport (Project project)
		{
			AddinData data = GetAddinData (project);
			if (data != null)
				return data;
			
			data = new AddinData (project);
			project.ExtendedProperties ["MonoDevelop.AddinAuthoring"] = data;
			AddinSupportChanged (project, true);
			return data;
		}
		
		public static void DisableAddinAuthoringSupport (Project project)
		{
			AddinData data = GetAddinData (project);
			project.ExtendedProperties.Remove ("MonoDevelop.AddinAuthoring");
			if (data != null)
				AddinSupportChanged (project, false);
		}
		
		public Project Project {
			get { return project; }
		}
		
		public AddinDescription AddinManifest {
			get {
				if (manifest == null) {
					ProjectFile file = GetAddinManifestFile ();
					if (file == null)
						return null;
					manifest = AddinRegistry.ReadAddinManifestFile (file.FilePath);
				}
				return manifest;
			}
		}
		
		public AddinDescription CompiledAddinManifest {
			get {
				if (compiledManifest == null) {
					if (File.Exists (project.GetOutputFileName ()))
						compiledManifest = registry.GetAddinDescription (null, project.GetOutputFileName ());
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
				string[] s = AddinRegistry.GetRegisteredStartupFolders (RegistryPath);
				if (s.Length > 0)
					registry = new AddinRegistry (RegistryPath, s[0]);
				else
					registry = new AddinRegistry (RegistryPath);
				return registry;
			}
		}
		
		void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public ProjectFile GetAddinManifestFile ()
		{
			foreach (ProjectFile pf in project.ProjectFiles) {
				if (pf.FilePath.EndsWith (".addin") || pf.FilePath.EndsWith (".addin.xml"))
					return pf;
			}
			
			AddinDescription desc = new AddinDescription ();
			string file = Path.Combine (project.BaseDirectory, "manifest.addin.xml");
			desc.Save (file);
			return project.AddFile (file, BuildAction.EmbedAsResource);
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
