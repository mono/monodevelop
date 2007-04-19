
using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	public class DeployServiceExtension
	{
		internal DeployServiceExtension Next;
		
		public virtual void BuildPackage (IProgressMonitor monitor, CombineEntry entry, PackageBuilder builder)
		{
			if (Next != null)
				Next.BuildPackage (monitor, entry, builder);
			else
				builder.Build (monitor, entry);
		}
		
		public virtual DeployFileCollection GetDeployFiles (DeployContext ctx, CombineEntry entry)
		{
			if (entry is Combine)
				return GetCombineDeployFiles (ctx, (Combine) entry);
			else if (entry is Project)
				return GetProjectDeployFiles (ctx, (Project) entry);
			else if (Next != null)
				return Next.GetDeployFiles (ctx, entry);
			else
				return new DeployFileCollection ();
		}
		
		public virtual DeployFileCollection GetCombineDeployFiles (DeployContext ctx, Combine combine)
		{
			if (Next != null)
				return Next.GetDeployFiles (ctx, combine);
			else
				return new DeployFileCollection ();
		}
		
		public virtual DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project)
		{
			if (Next != null)
				return Next.GetDeployFiles (ctx, project);
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


