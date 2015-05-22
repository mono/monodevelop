
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using System.Threading.Tasks;

namespace MonoDevelop.Deployment
{
	public class PackagingProject: SolutionItem
	{
		PackageCollection packages;
		
		public event EventHandler PackagesChanged;
		
		public PackagingProject()
		{
			Initialize (this);
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
		
		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			PackagingProjectConfiguration conf = new PackagingProjectConfiguration ();
			conf.Name = name;
			return conf;
		}
		
		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			foreach (Package p in packages)
				p.Clean (monitor);
			return Task.FromResult (BuildResult.Success);
		}
		
		protected async override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			foreach (Package p in packages)
				if (!await p.Build (monitor))
					break;
			return BuildResult.Success;
		}
		
		protected override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			foreach (Package p in packages)
				if (p.NeedsBuilding)
					return true;
			return false;
		}
		
		internal void NotifyPackagesChanged ()
		{
			AssertMainThread ();
			if (PackagesChanged != null)
				PackagesChanged (this, EventArgs.Empty);
		}
	}
	
	public class PackagingProjectConfiguration : SolutionItemConfiguration
	{
	}
}
