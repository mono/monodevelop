
using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	public class DeployServiceExtension
	{
		internal DeployServiceExtension Next;
		
		public virtual bool BuildPackage (IProgressMonitor monitor, PackageBuilder builder)
		{
			if (Next != null)
				return Next.BuildPackage (monitor, builder);
			else
				return builder.Build (monitor);
		}
		
		public virtual DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionItem entry, string configuration)
		{
			if (entry is SolutionFolder)
				return GetCombineDeployFiles (ctx, (SolutionFolder) entry, configuration);
			else if (entry is Project)
				return GetProjectDeployFiles (ctx, (Project) entry, configuration);
			else if (Next != null)
				return Next.GetDeployFiles (ctx, entry, configuration);
			else
				return new DeployFileCollection ();
		}
		
		public virtual DeployFileCollection GetCombineDeployFiles (DeployContext ctx, SolutionFolder combine, string configuration)
		{
			if (Next != null)
				return Next.GetDeployFiles (ctx, combine, configuration);
			else
				return new DeployFileCollection ();
		}
		
		public virtual DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, string configuration)
		{
			if (Next != null)
				return Next.GetDeployFiles (ctx, project, configuration);
			else
				return new DeployFileCollection ();
		}
		
		// Returns the path for the provided folderId.
		// The prefix can be null, an absolute path, or a symbolic representation
		// of a folder (e.g. a makefile variable)
		public virtual string ResolveDirectory (DeployContext context, string folderId)
		{
			if (Next != null)
				return Next.ResolveDirectory (context, folderId);
			else
				return null;
		}
	}
}


