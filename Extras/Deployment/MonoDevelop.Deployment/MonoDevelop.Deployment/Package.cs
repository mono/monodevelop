
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Deployment
{
	public class Package
	{
		[ProjectPathItemProperty]
		string packagedEntryPath;
		
		[ItemProperty ("Builder")]
		PackageBuilder builder;
		
		[ItemProperty]
		string name;
		
		bool needsBuilding;
		
		internal PackagingProject project;
		
		public event EventHandler Changed;
		
		public Package ()
		{
		}
		
		public Package (CombineEntry entry, PackageBuilder builder)
		{
			packagedEntryPath = entry.FileName;
			this.builder = builder;
		}
		
		public PackagingProject ParentProject {
			get { return project; }
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public PackageBuilder PackageBuilder {
			get { return builder; }
			set { builder = value; NotifyChanged (); }
		}
		
		public string PackagedEntryPath {
			get { return packagedEntryPath; }
			set { packagedEntryPath = value; NotifyChanged (); }
		}
		
		public bool Build (IProgressMonitor monitor)
		{
			CombineEntry entry = GetEntry ();
			if (entry == null || entry is UnknownCombineEntry)
				return false;
			
			DeployService.BuildPackage (monitor, this);
			if (entry.RootCombine != project.RootCombine)
				entry.Dispose ();
			
			needsBuilding = false;
			return true;
		}
		
		public bool NeedsBuilding {
			get {
				if (needsBuilding || project == null)
					return true;
				CombineEntry e = GetEntry (project.RootCombine, Runtime.FileService.GetFullPath (packagedEntryPath));
				if (e != null)
					return e.NeedsBuilding;
				else
					return true;
			}
			set {
				needsBuilding = value;
			}
		}
		
		public CombineEntry GetEntry ()
		{
			if (project != null) {
				CombineEntry entry = GetEntry (project.RootCombine, Runtime.FileService.GetFullPath (packagedEntryPath));
				if (entry != null)
					return entry;
			}
			return Services.ProjectService.ReadCombineEntry (packagedEntryPath, new NullProgressMonitor ());
		}
		
		CombineEntry GetEntry (CombineEntry entry, string path)
		{
			if (Runtime.FileService.GetFullPath (entry.FileName) == path)
				return entry;
			
			if (entry is Combine) {
				foreach (CombineEntry e in ((Combine)entry).Entries) {
					CombineEntry fe = GetEntry (e, path);
					if (fe != null)
						return fe;
				}
			}
			return null;
		}
		
		public void Clean (IProgressMonitor monitor)
		{
			needsBuilding = true;
		}
		
		void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
	}
}
