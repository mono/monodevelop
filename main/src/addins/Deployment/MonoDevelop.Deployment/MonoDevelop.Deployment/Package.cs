
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Deployment
{
	public class Package
	{
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
		
		public Package (PackageBuilder builder)
		{
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
		
		public bool Build (IProgressMonitor monitor)
		{
			DeployService.BuildPackage (monitor, this);
			needsBuilding = false;
			return true;
		}
		
		public bool NeedsBuilding {
			get {
				return needsBuilding;
			}
			set {
				needsBuilding = value;
			}
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
