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
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	public class ProjectServiceExtension
	{
		internal ProjectServiceExtension Next;
		
		Stack<ItemLoadCallback> loadCallbackStack = new Stack<ItemLoadCallback> ();

		internal ProjectServiceExtension GetNext (WorkspaceObject item)
		{
			if (Next.SupportsItem (item))
				return Next;
			else
				return Next.GetNext (item);
		}
		
		public virtual bool SupportsItem (WorkspaceObject item)
		{
			return true;
		}
		
		public virtual bool IsSolutionItemFile (string fileName)
		{
			return GetNext (UnknownItem.Instance).IsSolutionItemFile (fileName);
		}
		
		public virtual bool IsWorkspaceItemFile (string fileName)
		{
			return GetNext (UnknownItem.Instance).IsWorkspaceItemFile (fileName);
		}
		
		internal async virtual Task<SolutionItem> LoadSolutionItem (ProgressMonitor monitor, string fileName, ItemLoadCallback callback)
		{
			loadCallbackStack.Push (callback);
			try {
				return await LoadSolutionItem (monitor, fileName);
			} finally {
				loadCallbackStack.Pop ();
			}
		}
		
		protected virtual Task<SolutionItem> LoadSolutionItem (ProgressMonitor monitor, string fileName)
		{
			return GetNext (UnknownItem.Instance).LoadSolutionItem (monitor, fileName, loadCallbackStack.Peek ());
		}
		
		public virtual Task<WorkspaceItem> LoadWorkspaceItem (ProgressMonitor monitor, string fileName)
		{
			return GetNext (UnknownItem.Instance).LoadWorkspaceItem (monitor, fileName);
		}
	}

	public class BuildData
	{
		public ProjectItemCollection Items { get; internal set; }
		public DotNetProjectConfiguration Configuration { get; internal set; }
		public ConfigurationSelector ConfigurationSelector { get; internal set; }
	}
	
	class UnknownItem: WorkspaceObject, IBuildTarget
	{
		public static UnknownItem Instance = new UnknownItem ();

		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false)
		{
			return Task.FromResult (BuildResult.Success);
		}
		
		public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Task.FromResult (BuildResult.Success);
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
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

		protected override string OnGetName ()
		{
			return "Unknown";
		}

		protected override string OnGetBaseDirectory ()
		{
			return FilePath.Empty;
		}

		protected override string OnGetItemDirectory ()
		{
			return FilePath.Empty;
		}
	}
}
