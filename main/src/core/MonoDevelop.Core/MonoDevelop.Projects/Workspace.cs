//
// Workspace.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects.MD1;

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem]
	public sealed class Workspace: WorkspaceItem, ICustomDataItem, IBuildTarget
	{
		WorkspaceItemCollection items;

		public Workspace ()
		{
			Initialize (this);
			items = new WorkspaceItemCollection (this);
		}

		public override void SetLocation (FilePath baseDirectory, string name)
		{
			FileName = baseDirectory.Combine (name + ".mdw");
		}

		internal protected override Task OnSave (ProgressMonitor monitor)
		{
			return MD1FileFormat.Instance.WriteFile (FileName, this, monitor);
		}

		protected override void OnSetShared ()
		{
			base.OnSetShared ();
			items.SetShared ();
		}
		
		protected override void OnDispose ()
		{
			foreach (WorkspaceItem it in Items)
				it.Dispose ();
			base.OnDispose ();
		}

		bool IBuildTarget.CanBuild (ConfigurationSelector configuration)
		{
			return true;
		}

		[ThreadSafe]
		public async Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false, OperationContext operationContext = null)
		{
			AssertMainThread ();
			var res = new BuildResult { BuildCount = 0 };
			foreach (var bt in Items.OfType<IBuildTarget> ())
				res.Append (await bt.Build (monitor, configuration, operationContext:operationContext));
			return res;
		}

		[ThreadSafe]
		public async Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
		{
			AssertMainThread ();
			var res = new BuildResult { BuildCount = 0 };
			foreach (var bt in Items.OfType<IBuildTarget> ())
				res.Append (await bt.Clean (monitor, configuration, operationContext));
			return res;
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotSupportedException ();
		}

		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotSupportedException ();
		}

		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}

		[Obsolete ("This method will be removed in future releases")]
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return Items.OfType<IBuildTarget> ().Any (t => t.NeedsBuilding (configuration));
		}
		
		[ThreadSafe]
		public override ReadOnlyCollection<string> GetConfigurations ()
		{
			List<string> configs = new List<string> ();
			foreach (WorkspaceItem it in Items) {
				foreach (string conf in it.GetConfigurations ()) {
					if (!configs.Contains (conf))
						configs.Add (conf);
				}
			}
			return configs.AsReadOnly ();
		}
		
		[ThreadSafe]
		public WorkspaceItemCollection Items {
			get {
				return items; 
			}
		}

		[ThreadSafe]
		protected override IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			return Items;
		}
		
		[ThreadSafe]
		public override IEnumerable<Project> GetProjectsContainingFile (FilePath fileName)
		{
			foreach (WorkspaceItem it in Items) {
				foreach (Project p in it.GetProjectsContainingFile (fileName)) {
					yield return p;
				}
			}
		}

		[ThreadSafe]
		public override bool ContainsItem (WorkspaceObject obj)
		{
			if (base.ContainsItem (obj))
				return true;
			
			foreach (WorkspaceItem it in Items) {
				if (it.ContainsItem (obj))
					return true;
			}
			return false;
		}
		
		[ThreadSafe]
		public Task<WorkspaceItem> ReloadItem (ProgressMonitor monitor, WorkspaceItem item)
		{
			return Runtime.RunInMainThread (async delegate {
				if (Items.IndexOf (item) == -1)
					throw new InvalidOperationException ("Item '" + item.Name + "' does not belong to workspace '" + Name + "'");

				// Load the new item
				
				WorkspaceItem newItem;
				try {
					newItem = await Services.ProjectService.ReadWorkspaceItem (monitor, item.FileName);
				} catch (Exception ex) {
					UnknownWorkspaceItem e = new UnknownWorkspaceItem ();
					e.LoadError = ex.Message;
					e.FileName = item.FileName;
					newItem = e;
				}
				
				// Replace in the file list
				Items.Replace (item, newItem);
				
				NotifyModified ();
				NotifyItemRemoved (new WorkspaceItemChangeEventArgs (item, true));
				NotifyItemAdded (new WorkspaceItemChangeEventArgs (newItem, true));
				
				item.Dispose ();
				return newItem;
			});
		}

		[ThreadSafe]
		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> list = base.OnGetItemFiles (includeReferencedFiles).ToList ();
			if (includeReferencedFiles) {
				foreach (WorkspaceItem it in Items)
					list.AddRange (it.GetItemFiles (true));
			}
			return list;
		}

		
		internal void NotifyItemAdded (WorkspaceItemChangeEventArgs args)
		{
			AssertMainThread ();
			OnItemAdded (args);
			OnConfigurationsChanged ();
		}
		
		internal void NotifyItemRemoved (WorkspaceItemChangeEventArgs args)
		{
			AssertMainThread ();
			OnItemRemoved (args);
			OnConfigurationsChanged ();
		}
		
		/*protected virtual*/ void OnItemAdded (WorkspaceItemChangeEventArgs args)
		{
			if (ItemAdded != null)
				ItemAdded (this, args);
			OnDescendantItemAdded (args);
		}
		
		/*protected virtual*/ void OnItemRemoved (WorkspaceItemChangeEventArgs args)
		{
			if (ItemRemoved != null)
				ItemRemoved (this, args);
			OnDescendantItemRemoved (args);
		}
		
		/*protected virtual*/ void OnDescendantItemAdded (WorkspaceItemChangeEventArgs args)
		{
			if (DescendantItemAdded != null)
				DescendantItemAdded (this, args);
			if (ParentWorkspace != null)
				ParentWorkspace.OnDescendantItemAdded (args);
		}
		
		/*protected virtual*/ void OnDescendantItemRemoved (WorkspaceItemChangeEventArgs args)
		{
			if (DescendantItemRemoved != null)
				DescendantItemRemoved (this, args);
			if (ParentWorkspace != null)
				ParentWorkspace.OnDescendantItemRemoved (args);
		}

		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			DataCollection data = handler.Serialize (this);
			DataItem items = new DataItem ();
			items.Name = "Items";
			items.UniqueNames = false;
			string baseDir = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
			foreach (WorkspaceItem it in Items) {
				DataValue item = new DataValue ("Item", FileService.AbsoluteToRelativePath (baseDir, it.FileName));
				items.ItemData.Add (item);
			}
			data.Add (items);
			return data;
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			DataItem items = (DataItem) data.Extract ("Items");
			handler.Deserialize (this, data);
			ProgressMonitor monitor = handler.SerializationContext.ProgressMonitor;
			if (monitor == null)
				monitor = new ProgressMonitor ();
			if (items != null) {
				string baseDir = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
				monitor.BeginTask (null, items.ItemData.Count);
				try {
					foreach (DataValue item in items.ItemData) {
						string file = Path.Combine (baseDir, item.Value);
						WorkspaceItem it = Services.ProjectService.ReadWorkspaceItem (monitor, file).Result;
						if (it != null)
							Items.Add (it);
						monitor.Step (1);
					}
				} finally {
					monitor.EndTask ();
				}
			}
		}
		
		public event EventHandler<WorkspaceItemChangeEventArgs> ItemAdded;
		public event EventHandler<WorkspaceItemChangeEventArgs> ItemRemoved;
		public event EventHandler<WorkspaceItemChangeEventArgs> DescendantItemAdded;   // Fired if added in child workspaces
		public event EventHandler<WorkspaceItemChangeEventArgs> DescendantItemRemoved; // Fired if added in child workspaces
	}
}
