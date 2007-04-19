
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Deployment
{
	public class PackagingProject: CombineEntry
	{
		PackageCollection packages;
		
		public event EventHandler PackagesChanged;
		
		public PackagingProject()
		{
			packages = new PackageCollection (this);
		}
		
		public Package AddPackage (CombineEntry entry, PackageBuilder builder)
		{
			Package p = new Package ();
			p.PackagedEntryPath = entry.FileName;
			p.PackageBuilder = builder;
			packages.Add (p);
			return p;
		}
		
		[ItemProperty]
		public PackageCollection Packages {
			get { return packages; }
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			PackagingProjectConfiguration conf = new PackagingProjectConfiguration ();
			conf.Name = name;
			return conf;
		}
		
		protected override void OnClean (IProgressMonitor monitor)
		{
			foreach (Package p in packages)
				p.Clean (monitor);
		}
		
		protected override ICompilerResult OnBuild (IProgressMonitor monitor)
		{
			foreach (Package p in packages)
				p.Build (monitor);
			return null;
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context)
		{
		}
		
		protected override bool OnGetNeedsBuilding ()
		{
			foreach (Package p in packages)
				if (p.NeedsBuilding)
					return true;
			return false;
		}
		
		protected override void OnSetNeedsBuilding (bool val)
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
	
	public class PackagingProjectConfiguration : IConfiguration
	{
		[ItemProperty("name")]
		string name = null;
		
		public string Name {
			get { return name; }
			set { name = value; }
		}

		public object Clone()
		{
			IConfiguration conf = (IConfiguration) MemberwiseClone ();
			conf.CopyFrom (this);
			return conf;
		}
		
		public virtual void CopyFrom (IConfiguration configuration)
		{
		}
	}
}
