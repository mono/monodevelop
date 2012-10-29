// ProjectServiceExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	public class ProjectServiceExtension
	{
		internal ProjectServiceExtension Next;
		
		Stack<ItemLoadCallback> loadCallbackStack = new Stack<ItemLoadCallback> ();
		Stack<ItemCompileCallback> compileCallbackStack = new Stack<ItemCompileCallback> ();
		
		internal ProjectServiceExtension GetNext (IBuildTarget item)
		{
			if (Next.SupportsItem (item))
				return Next;
			else
				return Next.GetNext (item);
		}
		
		public virtual bool SupportsItem (IBuildTarget item)
		{
			return true;
		}
		
		public virtual object GetService (IBuildTarget item, Type type)
		{
			if (type.IsInstanceOfType (this))
				return this;
			else
				return GetNext (item).GetService (item, type);
		}

		public virtual void Save (IProgressMonitor monitor, SolutionEntityItem item)
		{
			GetNext (item).Save (monitor, item);
		}
		
		public virtual void Save (IProgressMonitor monitor, WorkspaceItem item)
		{
			GetNext (item).Save (monitor, item);
		}

		public virtual List<FilePath> GetItemFiles (SolutionEntityItem item, bool includeReferencedFiles)
		{
			return GetNext (item).GetItemFiles (item, includeReferencedFiles);
		}
		
		public virtual bool IsSolutionItemFile (string fileName)
		{
			return GetNext (UnknownItem.Instance).IsSolutionItemFile (fileName);
		}
		
		public virtual bool IsWorkspaceItemFile (string fileName)
		{
			return GetNext (UnknownItem.Instance).IsWorkspaceItemFile (fileName);
		}
		
		internal virtual SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName, ItemLoadCallback callback)
		{
			loadCallbackStack.Push (callback);
			try {
				SolutionEntityItem res = LoadSolutionItem (monitor, fileName);
				return res;
			} finally {
				loadCallbackStack.Pop ();
			}
		}
		
		protected virtual SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName)
		{
			return GetNext (UnknownItem.Instance).LoadSolutionItem (monitor, fileName, loadCallbackStack.Peek ());
		}
		
		public virtual WorkspaceItem LoadWorkspaceItem (IProgressMonitor monitor, string fileName)
		{
			return GetNext (UnknownItem.Instance).LoadWorkspaceItem (monitor, fileName);
		}
		
		public virtual BuildResult RunTarget (IProgressMonitor monitor, IBuildTarget item, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget)
				return Build (monitor, item, configuration);
			else if (target == ProjectService.CleanTarget) {
				Clean (monitor, item, configuration);
				return null;
			}
			else
				return GetNext (item).RunTarget (monitor, item, target, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, IBuildTarget item, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				Clean (monitor, (SolutionEntityItem) item, configuration);
			else if (item is WorkspaceItem)
				Clean (monitor, (WorkspaceItem) item, configuration);
			else
				GetNext (item).RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			GetNext (item).RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, Solution item, ConfigurationSelector configuration)
		{
			GetNext (item).RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, WorkspaceItem item, ConfigurationSelector configuration)
		{
			if (item is Solution)
				Clean (monitor, (Solution) item, configuration);
			else
				GetNext (item).RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, IBuildTarget item, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				return Build (monitor, (SolutionEntityItem) item, configuration);
			if (item is WorkspaceItem)
				return Build (monitor, (WorkspaceItem) item, configuration);
			return GetNext (item).RunTarget (monitor, item, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			return GetNext (item).RunTarget (monitor, item, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, WorkspaceItem item, ConfigurationSelector configuration)
		{
			if (item is Solution)
				return Build (monitor, (Solution) item, configuration);
			return GetNext (item).RunTarget (monitor, item, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, Solution solution, ConfigurationSelector configuration)
		{
			return GetNext (solution).RunTarget (monitor, solution, ProjectService.BuildTarget, configuration);
		}
		
		public virtual void Execute (IProgressMonitor monitor, IBuildTarget item, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				Execute (monitor, (SolutionEntityItem)item, context, configuration);
			else if (item is WorkspaceItem)
				Execute (monitor, (WorkspaceItem) item, context, configuration);
			else 
				GetNext (item).Execute (monitor, item, context, configuration);
		}
		
		protected virtual void Execute (IProgressMonitor monitor, SolutionEntityItem item, ExecutionContext context, ConfigurationSelector configuration)
		{
			GetNext (item).Execute (monitor, (IBuildTarget) item, context, configuration);
		}
		
		protected virtual void Execute (IProgressMonitor monitor, Solution solution, ExecutionContext context, ConfigurationSelector configuration)
		{
			GetNext (solution).Execute (monitor, (IBuildTarget) solution, context, configuration);
		}
		
		protected virtual void Execute (IProgressMonitor monitor, WorkspaceItem item, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (item is Solution)
				Execute (monitor, (Solution) item, context, configuration);
			else
				GetNext (item).Execute (monitor, (IBuildTarget) item, context, configuration);
		}
		
		public virtual bool CanExecute (IBuildTarget item, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				return CanExecute ((SolutionEntityItem)item, context, configuration);
			else if (item is WorkspaceItem)
				return CanExecute ((WorkspaceItem) item, context, configuration);
			else 
				return GetNext (item).CanExecute (item, context, configuration);
		}
		
		protected virtual bool CanExecute (SolutionEntityItem item, ExecutionContext context, ConfigurationSelector configuration)
		{
			return GetNext (item).CanExecute ((IBuildTarget) item, context, configuration);
		}
		
		protected virtual bool CanExecute (Solution solution, ExecutionContext context, ConfigurationSelector configuration)
		{
			return GetNext (solution).CanExecute ((IBuildTarget) solution, context, configuration);
		}
		
		protected virtual bool CanExecute (WorkspaceItem item, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (item is Solution)
				return CanExecute ((Solution) item, context, configuration);
			else
				return GetNext (item).CanExecute ((IBuildTarget) item, context, configuration);
		}
		
		public virtual IEnumerable<ExecutionTarget> GetExecutionTargets (IBuildTarget item, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				return GetExecutionTargets ((SolutionEntityItem)item, configuration);
			else if (item is WorkspaceItem)
				return GetExecutionTargets ((WorkspaceItem) item, configuration);
			else 
				return GetNext (item).GetExecutionTargets (item, configuration);
		}
		
		protected virtual IEnumerable<ExecutionTarget> GetExecutionTargets (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			return GetNext (item).GetExecutionTargets ((IBuildTarget) item, configuration);
		}
		
		protected virtual IEnumerable<ExecutionTarget> GetExecutionTargets (Solution solution, ConfigurationSelector configuration)
		{
			return GetNext (solution).GetExecutionTargets ((IBuildTarget) solution, configuration);
		}
		
		protected virtual IEnumerable<ExecutionTarget> GetExecutionTargets (WorkspaceItem item, ConfigurationSelector configuration)
		{
			if (item is Solution)
				return GetExecutionTargets ((Solution) item, configuration);
			else
				return GetNext (item).GetExecutionTargets ((IBuildTarget) item, configuration);
		}

		public virtual bool GetNeedsBuilding (IBuildTarget item, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				return GetNeedsBuilding ((SolutionEntityItem) item, configuration);
			if (item is WorkspaceItem)
				return GetNeedsBuilding ((WorkspaceItem) item, configuration);
			return GetNext (item).GetNeedsBuilding (item, configuration);
		}
		
		protected virtual bool GetNeedsBuilding (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			return GetNext (item).GetNeedsBuilding ((IBuildTarget) item, configuration);
		}
		
		protected virtual bool GetNeedsBuilding (Solution item, ConfigurationSelector configuration)
		{
			return GetNext (item).GetNeedsBuilding ((IBuildTarget) item, configuration);
		}
		
		protected virtual bool GetNeedsBuilding (WorkspaceItem item, ConfigurationSelector configuration)
		{
			if (item is Solution)
				return GetNeedsBuilding ((Solution) item, configuration);
			return GetNext (item).GetNeedsBuilding ((IBuildTarget) item, configuration);
		}
		
		public virtual void SetNeedsBuilding (IBuildTarget item, bool val, ConfigurationSelector configuration)
		{
			if (item is SolutionEntityItem)
				SetNeedsBuilding ((SolutionEntityItem) item, val, configuration);
			else if (item is WorkspaceItem)
				SetNeedsBuilding ((WorkspaceItem) item, val, configuration);
			else 
				GetNext (item).SetNeedsBuilding (item, val, configuration);
		}
		
		protected virtual void SetNeedsBuilding (SolutionEntityItem item, bool val, ConfigurationSelector configuration)
		{
			GetNext (item).SetNeedsBuilding ((IBuildTarget) item, val, configuration);
		}
		
		protected virtual void SetNeedsBuilding (Solution item, bool val, ConfigurationSelector configuration)
		{
			GetNext (item).SetNeedsBuilding ((IBuildTarget) item, val, configuration);
		}
		
		protected virtual void SetNeedsBuilding (WorkspaceItem item, bool val, ConfigurationSelector configuration)
		{
			if (item is Solution)
				SetNeedsBuilding ((Solution) item, val, configuration);
			else
				GetNext (item).SetNeedsBuilding ((IBuildTarget) item, val, configuration);
		}
		
		internal virtual BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData, ItemCompileCallback callback)
		{
			compileCallbackStack.Push (callback);
			try {
				BuildResult res = Compile (monitor, item, buildData);
				return res;
			} finally {
				compileCallbackStack.Pop ();
			}
		}
		
		protected virtual BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData)
		{
			return GetNext (item).Compile (monitor, item, buildData, compileCallbackStack.Peek ());
		}
	}

	public class BuildData
	{
		public ProjectItemCollection Items { get; internal set; }
		public DotNetProjectConfiguration Configuration { get; internal set; }
		public ConfigurationSelector ConfigurationSelector { get; internal set; }
	}
	
	class UnknownItem: IBuildTarget
	{
		public static UnknownItem Instance = new UnknownItem ();
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			return new BuildResult ();
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new System.NotImplementedException();
		}
		
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}
		
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return false;
		}
		
		public void SetNeedsBuilding (bool needsBuilding, ConfigurationSelector configuration)
		{
		}
		
		public void Save (IProgressMonitor monitor)
		{
		}
		
		public string Name {
			get { return "Unknown"; }
			set { }
		}
		
		
		public FilePath ItemDirectory {
			get { return FilePath.Empty; }
		}
		
		public FilePath BaseDirectory {
			get { return FilePath.Empty; }
			set { }
		}
		
		public void Dispose ()
		{
		}
		
		public System.Collections.IDictionary ExtendedProperties {
			get { return null; }
		}
	}
}
