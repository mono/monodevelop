// SolutionItem.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.CodeDom.Compiler;
using MonoDevelop.Core;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public abstract class SolutionItem: IExtendedDataItem, IBuildTarget
	{
		SolutionFolder parentFolder;
		Solution parentSolution;
		ISolutionItemHandler handler;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		string baseDirectory;
		
		Hashtable extendedProperties;
		
		public SolutionItem()
		{
		}
		
		public virtual void InitializeFromTemplate (XmlElement template)
		{
		}
		
		protected internal ISolutionItemHandler ItemHandler {
			get {
				if (handler == null) {
					InitializeItemHandler ();
					if (handler == null)
						throw new InvalidOperationException ("No handler found for solution item of type: " + GetType ());
				}
				return handler; 
			}
		}
		
		internal virtual void SetItemHandler (ISolutionItemHandler handler)
		{
			if (this.handler != null)
				this.handler.Dispose ();
			this.handler = handler;
		}
		
		internal ISolutionItemHandler GetItemHandler ()
		{
			// Used to get the handler without lazy loading it
			return this.handler;
		}
		
		public Solution ParentSolution {
			get {
				if (parentFolder != null)
					return parentFolder.ParentSolution;
				return parentSolution; 
			}
			internal set {
				parentSolution = value;
			}
		}
		
		public abstract void Save (IProgressMonitor monitor);
		
		public abstract string Name { get; set; }
		
		public string BaseDirectory {
			get {
				if (baseDirectory == null)
					return GetDefaultBaseDirectory ();
				else
					return baseDirectory;
			}
			set {
				string def = GetDefaultBaseDirectory ();
				if (value != null && def != null && System.IO.Path.GetFullPath (value) == System.IO.Path.GetFullPath (def))
					baseDirectory = null;
				else if (value == string.Empty)
					baseDirectory = null;
				else
					baseDirectory = value;
			}
		}
		
		protected virtual string GetDefaultBaseDirectory ()
		{
			return ParentSolution.BaseDirectory;
		}
		
		public string ItemId {
			get { return ItemHandler.ItemId; }
		}
		
		public IDictionary ExtendedProperties {
			get { return InternalGetExtendedProperties; }
		}
		
		public SolutionFolder ParentFolder {
			get {
				return parentFolder;
			}
			internal set {
				parentFolder = value;
			}
		}
		
		public virtual void Dispose ()
		{
			if (extendedProperties != null) {
				foreach (object ob in extendedProperties.Values) {
					IDisposable disp = ob as IDisposable;
					if (disp != null)
						disp.Dispose ();
				}
			}
			if (handler != null)
				handler.Dispose ();
		}
		
		public virtual IEnumerable<SolutionItem> GetReferencedItems (string configuration)
		{
			return new SolutionItem [0];
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, target, configuration);
		}
		
		public void Clean (IProgressMonitor monitor, string configuration)
		{
			Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.CleanTarget, configuration);
		}
		
		public BuildResult Build (IProgressMonitor monitor, string configuration)
		{
			return Build (monitor, configuration, false);
		}
		
		public BuildResult Build (IProgressMonitor monitor, string configuration, bool buildReferences)
		{
			if (!buildReferences) {
				if (!NeedsBuilding (configuration))
					return new BuildResult (new CompilerResults (null), "");
					
				try {
					monitor.BeginTask (GettextCatalog.GetString ("Building: {0} ({1})", Name, configuration), 1);
					
					// This will end calling OnBuild ()
					return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.BuildTarget, configuration);
					
				} finally {
					monitor.EndTask ();
				}
			}
				
			// Get a list of all items that need to be built (including this),
			// and build them in the correct order
			
			List<SolutionItem> referenced = new List<SolutionItem> ();
			GetBuildableReferencedItems (referenced, this, configuration);
			
			ReadOnlyCollection<SolutionItem> sortedReferenced = SolutionFolder.TopologicalSort (referenced, configuration);
			
			BuildResult cres = new BuildResult ();
			
			monitor.BeginTask (null, sortedReferenced.Count);
			foreach (SolutionItem p in sortedReferenced) {
				if (p.NeedsBuilding (configuration)) {
					BuildResult res = p.Build (monitor, configuration, false);
					cres.Append (res);
				}
				monitor.Step (1);
				if (monitor.IsCancelRequested)
					break;
			}
			monitor.EndTask ();
			return cres;
		}
		
		void GetBuildableReferencedItems (List<SolutionItem> referenced, SolutionItem item, string configuration)
		{
			if (referenced.Contains (item)) return;
			
			if (item.NeedsBuilding (configuration))
				referenced.Add (item);

			foreach (SolutionItem ritem in item.GetReferencedItems (configuration))
				GetBuildableReferencedItems (referenced, ritem, configuration);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			Services.ProjectService.ExtensionChain.Execute (monitor, this, context, configuration);
		}
		
		public bool NeedsBuilding (string configuration)
		{
			return Services.ProjectService.ExtensionChain.GetNeedsBuilding (this, configuration);
		}
		
		public void SetNeedsBuilding (bool value, string configuration)
		{
			Services.ProjectService.ExtensionChain.SetNeedsBuilding (this, value, configuration);
		}
		
		public virtual bool NeedsReload {
			get {
				if (ParentSolution != null)
					return ParentSolution.NeedsReload;
				else
					return false;
			}
			set {
			}
		}
		
		public static ReadOnlyCollection<T> TopologicalSort<T> (IEnumerable<T> items, string configuration) where T: SolutionItem
		{
			IList<T> allItems;
			allItems = items as IList<T>;
			if (allItems == null)
				allItems = new List<T> (items);
			
			List<T> sortedEntries = new List<T> ();
			bool[] inserted = new bool[allItems.Count];
			bool[] triedToInsert = new bool[allItems.Count];
			for (int i = 0; i < allItems.Count; ++i) {
				if (!inserted[i])
					Insert<T> (i, allItems, sortedEntries, inserted, triedToInsert, configuration);
			}
			return sortedEntries.AsReadOnly ();
		}
		
		static void Insert<T> (int index, IList<T> allItems, List<T> sortedItems, bool[] inserted, bool[] triedToInsert, string configuration) where T: SolutionItem
		{
			if (triedToInsert[index]) {
				throw new CyclicBuildOrderException();
			}
			triedToInsert[index] = true;
			SolutionItem insertItem = allItems[index];
			
			foreach (SolutionItem reference in insertItem.GetReferencedItems (configuration)) {
				for (int j=0; j < allItems.Count; ++j) {
					SolutionItem checkItem = allItems[j];
					if (reference == checkItem) {
						if (!inserted[j])
							Insert (j, allItems, sortedItems, inserted, triedToInsert, configuration);
						break;
					}
				}
			}
			sortedItems.Add ((T)insertItem);
			inserted[index] = true;
		}
		
		internal virtual IDictionary InternalGetExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}
		
		protected void NotifyModified ()
		{
			OnModified (new SolutionItemEventArgs (this));
		}
		
		protected virtual void OnModified (SolutionItemEventArgs args)
		{
			if (Modified != null)
				Modified (this, args);
		}
		
		protected virtual void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			NotifyModified ();
			if (NameChanged != null)
				NameChanged (this, e);
		}
		
		protected virtual void InitializeItemHandler ()
		{
		}
		
		
		internal protected abstract void OnClean (IProgressMonitor monitor, string configuration);
		internal protected abstract BuildResult OnBuild (IProgressMonitor monitor, string configuration);
		internal protected abstract void OnExecute (IProgressMonitor monitor, ExecutionContext context, string configuration);
		internal protected abstract bool OnGetNeedsBuilding (string configuration);
		internal protected abstract void OnSetNeedsBuilding (bool val, string configuration);
		
		public event SolutionItemRenamedEventHandler NameChanged;
		public event SolutionItemEventHandler Modified;
	}
}
