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

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem]
	public class Workspace: WorkspaceItem, ICustomDataItem
	{
		WorkspaceItemCollection items;
		
		public override void Dispose ()
		{
			base.Dispose ();
			foreach (WorkspaceItem it in Items)
				it.Dispose ();
		}
		
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
		
		public WorkspaceItemCollection Items {
			get {
				if (items == null)
					items = new WorkspaceItemCollection (this);
				return items; 
			}
		}
		
		public override ReadOnlyCollection<T> GetAllItems<T> ()
		{
			List<T> list = new List<T> ();
			GetAllItems<T> (list, this);
			return list.AsReadOnly ();
		}
		
		void GetAllItems<T> (List<T> list, WorkspaceItem item) where T: WorkspaceItem
		{
			if (item is T)
				list.Add ((T) item);
			
			if (item is Workspace) {
				foreach (WorkspaceItem citem in ((Workspace)item).Items)
					GetAllItems<T> (list, citem);
			}
		}
		
		public override SolutionEntityItem FindSolutionItem (string fileName)
		{
			foreach (WorkspaceItem it in Items) {
				SolutionEntityItem si = it.FindSolutionItem (fileName);
				if (si != null)
					return si;
			}
			return null;
		}

		[Obsolete("Use GetProjectsContainingFile() (plural) instead")]
		public override Project GetProjectContainingFile (FilePath fileName)
		{
			foreach (WorkspaceItem it in Items) {
				Project p = it.GetProjectContainingFile (fileName);
				if (p != null)
					return p;
			}
			return null;
		}

		public override IEnumerable<Project> GetProjectsContainingFile (FilePath fileName)
		{
			foreach (WorkspaceItem it in Items) {
				foreach (Project p in it.GetProjectsContainingFile (fileName)) {
					yield return p;
				}
			}
		}

		public override bool ContainsItem (IWorkspaceObject obj)
		{
			if (base.ContainsItem (obj))
				return true;
			
			foreach (WorkspaceItem it in Items) {
				if (it.ContainsItem (obj))
					return true;
			}
			return false;
		}
		
		
		public override ReadOnlyCollection<T> GetAllSolutionItems<T> ()
		{
			List<T> list = new List<T> ();
			foreach (WorkspaceItem it in Items) {
				list.AddRange (it.GetAllSolutionItems<T> ());
			}
			return list.AsReadOnly ();
		}

		public override void ConvertToFormat (FileFormat format, bool convertChildren)
		{
			base.ConvertToFormat (format, convertChildren);
			if (convertChildren) {
				foreach (WorkspaceItem it in Items)
					it.ConvertToFormat (format, true);
			}
		}

		
		internal protected override BuildResult OnRunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			BuildResult result = null;
			monitor.BeginTask (null, Items.Count);
			try {
				foreach (WorkspaceItem it in Items) {
					BuildResult res = it.RunTarget (monitor, target, configuration);
					if (res != null) {
						if (result == null) {
							result = new BuildResult ();
							result.BuildCount = 0;
						}
						result.Append (res);
					}
					monitor.Step (1);
				}
			} finally {
				monitor.EndTask ();
			}
			return result;
		}
		
		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}
		
		public WorkspaceItem ReloadItem (IProgressMonitor monitor, WorkspaceItem item)
		{
			if (Items.IndexOf (item) == -1)
				throw new InvalidOperationException ("Item '" + item.Name + "' does not belong to workspace '" + Name + "'");

			// Load the new item
			
			WorkspaceItem newItem;
			try {
				newItem = Services.ProjectService.ReadWorkspaceItem (monitor, item.FileName);
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
		}

		public override List<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> list = base.GetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (WorkspaceItem it in Items)
					list.AddRange (it.GetItemFiles (true));
			}
			return list;
		}

		
		internal void NotifyItemAdded (WorkspaceItemChangeEventArgs args)
		{
			OnItemAdded (args);
			OnConfigurationsChanged ();
		}
		
		internal void NotifyItemRemoved (WorkspaceItemChangeEventArgs args)
		{
			OnItemRemoved (args);
			OnConfigurationsChanged ();
		}
		
		protected virtual void OnItemAdded (WorkspaceItemChangeEventArgs args)
		{
			if (ItemAdded != null)
				ItemAdded (this, args);
			OnDescendantItemAdded (args);
		}
		
		protected virtual void OnItemRemoved (WorkspaceItemChangeEventArgs args)
		{
			if (ItemRemoved != null)
				ItemRemoved (this, args);
			OnDescendantItemRemoved (args);
		}
		
		protected virtual void OnDescendantItemAdded (WorkspaceItemChangeEventArgs args)
		{
			if (DescendantItemAdded != null)
				DescendantItemAdded (this, args);
			if (ParentWorkspace != null)
				ParentWorkspace.OnDescendantItemAdded (args);
		}
		
		protected virtual void OnDescendantItemRemoved (WorkspaceItemChangeEventArgs args)
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
			IProgressMonitor monitor = handler.SerializationContext.ProgressMonitor;
			if (monitor == null)
				monitor = new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ();
			if (items != null) {
				string baseDir = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
				monitor.BeginTask (null, items.ItemData.Count);
				try {
					foreach (DataValue item in items.ItemData) {
						string file = Path.Combine (baseDir, item.Value);
						WorkspaceItem it = Services.ProjectService.ReadWorkspaceItem (monitor, file);
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
