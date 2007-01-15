
using System;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ProjectServiceExtension
	{
		internal ProjectServiceExtension Next;
		internal bool BuildReferences;
		
		public virtual void Save (IProgressMonitor monitor, CombineEntry entry)
		{
			Next.Save (monitor, entry);
		}
		
		public virtual CombineEntry Load (IProgressMonitor monitor, string fileName)
		{
			return Next.Load (monitor, fileName);
		}
		
		public virtual void Clean (CombineEntry entry)
		{
			Next.Clean (entry);
		}
		
		internal ICompilerResult InternalBuild (IProgressMonitor monitor, CombineEntry entry, bool buildReferences)
		{
			BuildReferences = buildReferences;
			return Build (monitor, entry);
		}
		
		public virtual ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			if (entry is Project)
				return BuildProject (monitor, (Project) entry, BuildReferences);
			else
				return Next.Build (monitor, entry);
		}
		
		public virtual ICompilerResult BuildProject (IProgressMonitor monitor, Project project, bool buildReferences)
		{
			Next.BuildReferences = BuildReferences;
			return Next.Build (monitor, project);
		}
		
		public virtual void Execute (IProgressMonitor monitor, CombineEntry entry, ExecutionContext context)
		{
			Next.Execute (monitor, entry, context);
		}
		
		public virtual bool GetNeedsBuilding (CombineEntry entry)
		{
			return Next.GetNeedsBuilding (entry);
		}
		
		public virtual void SetNeedsBuilding (CombineEntry entry, bool val)
		{
			Next.SetNeedsBuilding (entry, val);
		}
	}
}
