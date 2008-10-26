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

namespace MonoDevelop.Projects
{
	public class ProjectServiceExtension
	{
		internal ProjectServiceExtension Next;
		
		Stack<ItemLoadCallback> loadCallbackStack = new Stack<ItemLoadCallback> ();
		Stack<ItemCompileCallback> compileCallbackStack = new Stack<ItemCompileCallback> ();

		public virtual void Save (IProgressMonitor monitor, SolutionEntityItem item)
		{
			Next.Save (monitor, item);
		}
		
		public virtual void Save (IProgressMonitor monitor, WorkspaceItem item)
		{
			Next.Save (monitor, item);
		}
		
		public virtual List<string> GetItemFiles (SolutionEntityItem item, bool includeReferencedFiles)
		{
			return Next.GetItemFiles (item, includeReferencedFiles);
		}
		
		public virtual bool IsSolutionItemFile (string fileName)
		{
			return Next.IsSolutionItemFile (fileName);
		}
		
		public virtual bool IsWorkspaceItemFile (string fileName)
		{
			return Next.IsWorkspaceItemFile (fileName);
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
			return Next.LoadSolutionItem (monitor, fileName, loadCallbackStack.Peek ());
		}
		
		public virtual WorkspaceItem LoadWorkspaceItem (IProgressMonitor monitor, string fileName)
		{
			return Next.LoadWorkspaceItem (monitor, fileName);
		}
		
		public virtual BuildResult RunTarget (IProgressMonitor monitor, IBuildTarget item, string target, string configuration)
		{
			if (target == ProjectService.BuildTarget)
				return Build (monitor, item, configuration);
			else if (target == ProjectService.CleanTarget) {
				Clean (monitor, item, configuration);
				return null;
			}
			else
				return Next.RunTarget (monitor, item, target, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, IBuildTarget item, string configuration)
		{
			if (item is SolutionEntityItem)
				Clean (monitor, (SolutionEntityItem) item, configuration);
			else if (item is WorkspaceItem)
				Clean (monitor, (WorkspaceItem) item, configuration);
			else
				Next.RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			Next.RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, Solution item, string configuration)
		{
			Next.RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual void Clean (IProgressMonitor monitor, WorkspaceItem item, string configuration)
		{
			if (item is Solution)
				Clean (monitor, (Solution) item, configuration);
			else
				Next.RunTarget (monitor, item, ProjectService.CleanTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, IBuildTarget item, string configuration)
		{
			if (item is SolutionEntityItem)
				return Build (monitor, (SolutionEntityItem) item, configuration);
			if (item is WorkspaceItem)
				return Build (monitor, (WorkspaceItem) item, configuration);
			return Next.RunTarget (monitor, item, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			return Next.RunTarget (monitor, item, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, WorkspaceItem item, string configuration)
		{
			if (item is Solution)
				return Build (monitor, (Solution) item, configuration);
			return Next.RunTarget (monitor, item, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual BuildResult Build (IProgressMonitor monitor, Solution solution, string configuration)
		{
			return Next.RunTarget (monitor, solution, ProjectService.BuildTarget, configuration);
		}
		
		public virtual void Execute (IProgressMonitor monitor, IBuildTarget item, ExecutionContext context, string configuration)
		{
			if (item is SolutionEntityItem)
				Execute (monitor, (SolutionEntityItem)item, context, configuration);
			else if (item is WorkspaceItem)
				Execute (monitor, (WorkspaceItem) item, context, configuration);
			else 
				Next.Execute (monitor, item, context, configuration);
		}
		
		protected virtual void Execute (IProgressMonitor monitor, SolutionEntityItem item, ExecutionContext context, string configuration)
		{
			Next.Execute (monitor, (IBuildTarget) item, context, configuration);
		}
		
		protected virtual void Execute (IProgressMonitor monitor, Solution solution, ExecutionContext context, string configuration)
		{
			Next.Execute (monitor, (IBuildTarget) solution, context, configuration);
		}
		
		protected virtual void Execute (IProgressMonitor monitor, WorkspaceItem item, ExecutionContext context, string configuration)
		{
			if (item is Solution)
				Execute (monitor, (Solution) item, context, configuration);
			else
				Next.Execute (monitor, (IBuildTarget) item, context, configuration);
		}
		
		public virtual bool CanExecute (IBuildTarget item, ExecutionContext context, string configuration)
		{
			if (item is SolutionEntityItem)
				return CanExecute ((SolutionEntityItem)item, context, configuration);
			else if (item is WorkspaceItem)
				return CanExecute ((WorkspaceItem) item, context, configuration);
			else 
				return Next.CanExecute (item, context, configuration);
		}
		
		protected virtual bool CanExecute (SolutionEntityItem item, ExecutionContext context, string configuration)
		{
			return Next.CanExecute ((IBuildTarget) item, context, configuration);
		}
		
		protected virtual bool CanExecute (Solution solution, ExecutionContext context, string configuration)
		{
			return Next.CanExecute ((IBuildTarget) solution, context, configuration);
		}
		
		protected virtual bool CanExecute (WorkspaceItem item, ExecutionContext context, string configuration)
		{
			if (item is Solution)
				return CanExecute ((Solution) item, context, configuration);
			else
				return Next.CanExecute ((IBuildTarget) item, context, configuration);
		}
		
		public virtual bool GetNeedsBuilding (IBuildTarget item, string configuration)
		{
			if (item is SolutionEntityItem)
				return GetNeedsBuilding ((SolutionEntityItem) item, configuration);
			if (item is WorkspaceItem)
				return GetNeedsBuilding ((WorkspaceItem) item, configuration);
			return Next.GetNeedsBuilding (item, configuration);
		}
		
		protected virtual bool GetNeedsBuilding (SolutionEntityItem item, string configuration)
		{
			return Next.GetNeedsBuilding ((IBuildTarget) item, configuration);
		}
		
		protected virtual bool GetNeedsBuilding (Solution item, string configuration)
		{
			return Next.GetNeedsBuilding ((IBuildTarget) item, configuration);
		}
		
		protected virtual bool GetNeedsBuilding (WorkspaceItem item, string configuration)
		{
			if (item is Solution)
				return GetNeedsBuilding ((Solution) item, configuration);
			return Next.GetNeedsBuilding ((IBuildTarget) item, configuration);
		}
		
		public virtual void SetNeedsBuilding (IBuildTarget item, bool val, string configuration)
		{
			if (item is SolutionEntityItem)
				SetNeedsBuilding ((SolutionEntityItem) item, val, configuration);
			else if (item is WorkspaceItem)
				SetNeedsBuilding ((WorkspaceItem) item, val, configuration);
			else 
				Next.SetNeedsBuilding (item, val, configuration);
		}
		
		protected virtual void SetNeedsBuilding (SolutionEntityItem item, bool val, string configuration)
		{
			Next.SetNeedsBuilding ((IBuildTarget) item, val, configuration);
		}
		
		protected virtual void SetNeedsBuilding (Solution item, bool val, string configuration)
		{
			Next.SetNeedsBuilding ((IBuildTarget) item, val, configuration);
		}
		
		protected virtual void SetNeedsBuilding (WorkspaceItem item, bool val, string configuration)
		{
			if (item is Solution)
				SetNeedsBuilding ((Solution) item, val, configuration);
			else
				Next.SetNeedsBuilding ((IBuildTarget) item, val, configuration);
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
			return Next.Compile (monitor, item, buildData, compileCallbackStack.Peek ());
		}
		
	}

	public class BuildData
	{
		public ProjectFileCollection Files { get; internal set; }
		public ProjectReferenceCollection References { get; internal set; }
		public DotNetProjectConfiguration Configuration { get; internal set; }
	}
}
