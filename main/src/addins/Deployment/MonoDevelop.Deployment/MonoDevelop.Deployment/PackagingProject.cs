
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Deployment
{
	public class PackagingProject: SolutionEntityItem
	{
		PackageCollection packages;
		
		public event EventHandler PackagesChanged;
		
		public PackagingProject()
		{
			packages = new PackageCollection (this);
		}
		
		public Package AddPackage (string name, PackageBuilder builder)
		{
			Package p = new Package ();
			p.Name = name;
			p.PackageBuilder = builder;
			packages.Add (p);
			return p;
		}
		
		[ItemProperty]
		public PackageCollection Packages {
			get { return packages; }
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			PackagingProjectConfiguration conf = new PackagingProjectConfiguration ();
			conf.Name = name;
			return conf;
		}
		
		protected override void OnClean (IProgressMonitor monitor, string configuration)
		{
			foreach (Package p in packages)
				p.Clean (monitor);
		}
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, string configuration)
		{
			foreach (Package p in packages)
				p.Build (monitor);
			return null;
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
		}
		
		protected override bool OnGetNeedsBuilding (string configuration)
		{
			foreach (Package p in packages)
				if (p.NeedsBuilding)
					return true;
			return false;
		}
		
		protected override void OnSetNeedsBuilding (bool val, string configuration)
		{
			foreach (Package p in packages)
				p.NeedsBuilding = val;
		}
		
		internal void NotifyPackagesChanged ()
		{
			if (PackagesChanged != null)
				PackagesChanged (this, EventArgs.Empty);
		}
	}
	
	public class PackagingProjectConfiguration : SolutionItemConfiguration
	{
	}
}
