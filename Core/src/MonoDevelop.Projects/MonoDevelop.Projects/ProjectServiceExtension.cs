
using System;
using System.Collections.Specialized;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ProjectServiceExtension
	{
		internal ProjectServiceExtension Next;

		
		public virtual void Save (IProgressMonitor monitor, CombineEntry entry)
		{
			Next.Save (monitor, entry);
		}
		
		public virtual StringCollection GetExportFiles (CombineEntry entry)
		{
			return Next.GetExportFiles (entry);
		}
		
		public virtual bool IsCombineEntryFile (string fileName)
		{
			return Next.IsCombineEntryFile (fileName);
		}
		
		public virtual CombineEntry Load (IProgressMonitor monitor, string fileName)
		{
			return Next.Load (monitor, fileName);
		}
		
		public virtual void Clean (IProgressMonitor monitor, CombineEntry entry)
		{
			Next.Clean (monitor, entry);
		}
		
		public virtual ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			return Next.Build (monitor, entry);
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

		public virtual string GetDefaultResourceId (ProjectFile pf)
		{
			return Next.GetDefaultResourceId (pf);
		}
	}
}
