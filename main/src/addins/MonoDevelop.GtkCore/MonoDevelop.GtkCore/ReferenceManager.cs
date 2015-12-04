// ReferenceManager.cs
//
// Author: Mike Kestner <mkestner@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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


using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore {

	public class ReferenceManager : IDisposable {

		DotNetProject project;
		
		public ReferenceManager (DotNetProject project)
		{
			this.project = project;
			project.AssemblyContext.Changed += ResetSupportedVersions;
		}

		public void Dispose ()
		{
			project.AssemblyContext.Changed -= ResetSupportedVersions;
			project = null;
		}
		
		void ResetSupportedVersions (object o, EventArgs a)
		{
			supported_versions = null;
		}
		
		string CurrentAssemblyVersion {
			get {
				foreach (ProjectReference pref in project.References) {
					if (!IsGtkReference (pref))
						continue;
					string val = pref.StoredReference;
					int idx = val.IndexOf (",") + 1;
					return val.Substring (idx).Trim ();
				}
				return String.Empty;
			}
		}

		public string GtkPackageVersion {
			get { return GetGtkPackageVersion (CurrentAssemblyVersion); }
			set {
				if (String.IsNullOrEmpty (value))
					throw new ArgumentException ("value");

				Update (GetGtkAssemblyVersion (value));
			}
		}

		public string TargetGtkVersion {
			get {
				string assm_version = CurrentAssemblyVersion;
				if (String.IsNullOrEmpty (assm_version))
					return String.Empty;
				int idx = assm_version.IndexOf (",");
				if (idx > 0)
					assm_version = assm_version.Substring (0, idx);
				idx = assm_version.IndexOf ("=");
				if (idx > 0)
					assm_version = assm_version.Substring (idx + 1);
				string[] toks = assm_version.Split ('.');
				if (toks.Length > 1)
					return toks[0] + "." + toks[1];
				return String.Empty;
			}
		}
		
		string GetGtkAssemblyVersion (string pkg_version)
		{
			if (String.IsNullOrEmpty (pkg_version))
				return String.Empty;

			pkg_version = pkg_version + ".";
			foreach (SystemAssembly asm in project.AssemblyContext.GetAssemblies ()) {
				if (asm.Name == "gtk-sharp" && asm.Version.StartsWith (pkg_version)) {
					int i = asm.FullName.IndexOf (',');
					return asm.FullName.Substring (i+1).Trim ();
				}
			}
			return string.Empty;
		}

		string GetGtkPackageVersion (string assembly_version)
		{
			if (String.IsNullOrEmpty (assembly_version))
				return String.Empty;
			
			int i = assembly_version.IndexOf ('=');
			assembly_version = assembly_version.Substring (i+1);
			return GetVersionPrefix (assembly_version);
		}

		public bool Update ()
		{
			return Update (CurrentAssemblyVersion);
		}

		bool Update (string assm_version)
		{
			if (assm_version == null)
				throw new ArgumentException (assm_version);

			bool changed = false;
			updating = true;

			bool gdk = false, gtk = false, posix = false;
			
			foreach (ProjectReference r in new List<ProjectReference> (project.References)) {
				if (r.ReferenceType != ReferenceType.Package)
					continue;
				string name = GetReferenceName (r);
				if (name == "gdk-sharp")
					gdk = true;
				if (name == "gtk-sharp")
					gtk = true;
				else if (name == "Mono.Posix")
					posix = true;
				
				// Is a gtk-sharp-2.0 assembly?
				if (Array.IndexOf (gnome_assemblies, name) == -1)
					continue;
				
				string sr = r.StoredReference;
				string version = sr.Substring (sr.IndexOf (",") + 1).Trim ();
				if (version != assm_version) {
					project.References.Remove (r);
					if (name == "gnome-sharp" && assm_version == "Version=2.12.0.0, Culture=neutral, PublicKeyToken=35e10195dab3c99f") {
						project.References.Add (new ProjectReference (ReferenceType.Package, name + ", Version=2.24.0.0, Culture=neutral, PublicKeyToken=35e10195dab3c99f"));
					} else {
						project.References.Add (new ProjectReference (ReferenceType.Package, name + ", " + assm_version));
					}
					changed = true;
				}
			}

			if (!gtk) {
				project.References.Add (new ProjectReference (ReferenceType.Package, "gtk-sharp" + ", " + assm_version));
				project.ExtendedProperties ["GtkReferenceExists"] = true;
				changed = true;
			}

			if (!GtkDesignInfo.HasDesignedObjects (project))
				return changed;

			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			if (!gdk) {
				project.References.Add (new ProjectReference (ReferenceType.Package, "gdk-sharp" + ", " + assm_version));
				changed = true;
			}
				
			if (!posix && info.GenerateGettext && info.GettextClass == "Mono.Unix.Catalog") {
				// Add a reference to Mono.Posix. Use the version for the selected project's runtime version.
				string aname = project.AssemblyContext.FindInstalledAssembly ("Mono.Posix, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756", null, project.TargetFramework);
				if (aname != null) {
					aname = project.AssemblyContext.GetAssemblyNameForVersion (aname, project.TargetFramework);
					if (aname != null) {
						project.References.Add (new ProjectReference (ReferenceType.Package, aname));
						changed = true;
					}
				}
			}
			updating = false;
			return changed;
		}
		
		static bool updating;
		static string[] gnome_assemblies = new string [] { 
			"art-sharp", "atk-sharp", "gconf-sharp", "gdk-sharp", 
			"glade-sharp","glib-sharp", "gnome-sharp",
			"gnome-vfs-sharp", "gtk-dotnet", "gtkhtml-sharp", 
			"gtk-sharp", "pango-sharp", "rsvg-sharp"
		};
		
		public static void Initialize ()
		{
			IdeApp.Workspace.ReferenceAddedToProject += OnReferenceAdded;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferenceRemoved;
		}
		
		static void OnReferenceAdded (object o, ProjectReferenceEventArgs args)
		{
			if (updating || !IsGtkReference (args.ProjectReference))
				return;

			args.Project.ExtendedProperties ["GtkReferenceExists"] = true;
			string sr = args.ProjectReference.StoredReference;
			string version = sr.Substring (sr.IndexOf (",") + 1).Trim ();
			ReferenceManager rm = new ReferenceManager (args.Project as DotNetProject);
			rm.Update (version);
		}

		static void OnReferenceRemoved (object o, ProjectReferenceEventArgs args)
		{
			if (updating || !IsGtkReference (args.ProjectReference))
				return;

			DotNetProject dnp = args.Project as DotNetProject;

			if (MessageService.Confirm (GettextCatalog.GetString ("The Gtk# User Interface designer will be disabled by removing the gtk-sharp reference."), new AlertButton (GettextCatalog.GetString ("Disable Designer")))) {
				dnp.ExtendedProperties ["GtkReferenceExists"] = false;
				GtkDesignInfo.DisableProject (dnp);
			} else
				dnp.References.Add (new ProjectReference (ReferenceType.Package, args.ProjectReference.StoredReference));
		}

		static string GetReferenceName (ProjectReference pref)
		{
			string stored = pref.StoredReference;
			int idx =stored.IndexOf (",");
			if (idx == -1)
				return stored.Trim ();

			return stored.Substring (0, idx).Trim ();
		}

		static bool IsGtkReference (ProjectReference pref)
		{
			if (pref.ReferenceType != ReferenceType.Package)
				return false;

			return GetReferenceName (pref) == "gtk-sharp";
		}

		public static bool HasGtkReference (DotNetProject project)
		{
			foreach (ProjectReference pref in project.References)
				if (IsGtkReference (pref))
					return true;
			return false;
		}

		List<string> supported_versions;
		string default_version;

		public string DefaultGtkVersion {
			get {
				if (SupportedGtkVersions.Count > 0 && default_version == null)
					default_version = SupportedGtkVersions [0];
				return default_version; 
			}
		}

		public List<string> SupportedGtkVersions {
			get {
				if (supported_versions == null) {
					supported_versions = new List<string> ();
					foreach (SystemAssembly asm in project.AssemblyContext.GetAssemblies ()) {
						if (asm.Name == "gtk-sharp") {
							string v = GetVersionPrefix (asm.Version);
							if (!supported_versions.Contains (v))
								supported_versions.Add (v);
							if (v == "2.8")
								default_version = v;
						}
					}
					supported_versions.Sort ();
				}
				return supported_versions;
			}
		}
		
		string GetVersionPrefix (string version)
		{
			int i = version.IndexOf ('.');
			i = version.IndexOf ('.', i + 1);
			return version.Substring (0, i);
		}
	}	
}
