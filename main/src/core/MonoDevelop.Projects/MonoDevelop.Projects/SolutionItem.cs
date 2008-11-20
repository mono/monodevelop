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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public abstract class SolutionItem: IExtendedDataItem, IBuildTarget, ILoadController
	{
		SolutionFolder parentFolder;
		Solution parentSolution;
		ISolutionItemHandler handler;
		int loading;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		string baseDirectory;
		
		Hashtable extendedProperties;
		
		public SolutionItem()
		{
			ProjectExtensionUtil.LoadControl (this);
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
		
		protected bool Loading {
			get { return loading > 0; }
		}
		
		public abstract void Save (IProgressMonitor monitor);
		
		public abstract string Name { get; set; }
		
		public string BaseDirectory {
			get {
				if (baseDirectory == null)
					return System.IO.Path.GetFullPath (GetDefaultBaseDirectory ());
				else
					return baseDirectory;
			}
			set {
				string def = GetDefaultBaseDirectory ();
				if (value != null && def != null && System.IO.Path.GetFullPath (value) == System.IO.Path.GetFullPath (def))
					baseDirectory = null;
				else if (string.IsNullOrEmpty (value))
					baseDirectory = null;
				else
					baseDirectory = System.IO.Path.GetFullPath (value);
				NotifyModified ("BaseDirectory");
			}
		}
		
		internal bool HasCustomBaseDirectory {
			get { return baseDirectory != null; }
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
		
		public virtual IEnumerable<SolutionItem> GetReferencedItems (string solutionConfiguration)
		{
			return new SolutionItem [0];
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, string solutionConfiguration)
		{
			return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, target, solutionConfiguration);
		}
		
		public void Clean (IProgressMonitor monitor, string solutionConfiguration)
		{
			Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.CleanTarget, solutionConfiguration);
		}
		
		public BuildResult Build (IProgressMonitor monitor, string solutionConfiguration)
		{
			return Build (monitor, solutionConfiguration, false);
		}
		
		public BuildResult Build (IProgressMonitor monitor, string solutionConfiguration, bool buildReferences)
		{
			if (!buildReferences) {
				if (!NeedsBuilding (solutionConfiguration))
					return new BuildResult (new CompilerResults (null), "");
					
				try {
					SolutionEntityItem it = this as SolutionEntityItem;
					string confName = it != null ? it.GetActiveConfigurationId (solutionConfiguration) : solutionConfiguration;
					monitor.BeginTask (GettextCatalog.GetString ("Building: {0} ({1})", Name, confName), 1);
					
					// This will end calling OnBuild ()
					return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.BuildTarget, solutionConfiguration);
					
				} finally {
					monitor.EndTask ();
				}
			}
				
			// Get a list of all items that need to be built (including this),
			// and build them in the correct order
			
			List<SolutionItem> referenced = new List<SolutionItem> ();
			GetBuildableReferencedItems (referenced, this, solutionConfiguration);
			
			ReadOnlyCollection<SolutionItem> sortedReferenced = SolutionFolder.TopologicalSort (referenced, solutionConfiguration);
			
			BuildResult cres = new BuildResult ();
			cres.BuildCount = 0;
			
			monitor.BeginTask (null, sortedReferenced.Count);
			foreach (SolutionItem p in sortedReferenced) {
				if (p.NeedsBuilding (solutionConfiguration)) {
					BuildResult res = p.Build (monitor, solutionConfiguration, false);
					cres.Append (res);
					if (res.ErrorCount > 0)
						break;
				}
				monitor.Step (1);
				if (monitor.IsCancelRequested)
					break;
			}
			monitor.EndTask ();
			return cres;
		}
		
		public DateTime GetLastBuildTime (string itemConfiguration)
		{
			return OnGetLastBuildTime (itemConfiguration);
		}
		
		void GetBuildableReferencedItems (List<SolutionItem> referenced, SolutionItem item, string solutionConfiguration)
		{
			if (referenced.Contains (item)) return;
			
			if (item.NeedsBuilding (solutionConfiguration))
				referenced.Add (item);

			foreach (SolutionItem ritem in item.GetReferencedItems (solutionConfiguration))
				GetBuildableReferencedItems (referenced, ritem, solutionConfiguration);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, string solutionConfiguration)
		{
			Services.ProjectService.ExtensionChain.Execute (monitor, this, context, solutionConfiguration);
		}
		
		public bool CanExecute (ExecutionContext context, string solutionConfiguration)
		{
			return Services.ProjectService.ExtensionChain.CanExecute (this, context, solutionConfiguration);
		}
		
		public bool NeedsBuilding (string solutionConfiguration)
		{
			return Services.ProjectService.ExtensionChain.GetNeedsBuilding (this, solutionConfiguration);
		}
		
		public void SetNeedsBuilding (bool value, string solutionConfiguration)
		{
			Services.ProjectService.ExtensionChain.SetNeedsBuilding (this, value, solutionConfiguration);
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
		
		public static ReadOnlyCollection<T> TopologicalSort<T> (IEnumerable<T> items, string solutionConfiguration) where T: SolutionItem
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
					Insert<T> (i, allItems, sortedEntries, inserted, triedToInsert, solutionConfiguration);
			}
			return sortedEntries.AsReadOnly ();
		}
		
		static void Insert<T> (int index, IList<T> allItems, List<T> sortedItems, bool[] inserted, bool[] triedToInsert, string solutionConfiguration) where T: SolutionItem
		{
			if (triedToInsert[index]) {
				throw new CyclicBuildOrderException();
			}
			triedToInsert[index] = true;
			SolutionItem insertItem = allItems[index];
			
			foreach (SolutionItem reference in insertItem.GetReferencedItems (solutionConfiguration)) {
				for (int j=0; j < allItems.Count; ++j) {
					SolutionItem checkItem = allItems[j];
					if (reference == checkItem) {
						if (!inserted[j])
							Insert (j, allItems, sortedItems, inserted, triedToInsert, solutionConfiguration);
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
		
		void ILoadController.BeginLoad ()
		{
			loading++;
			OnBeginLoad ();
		}
		
		void ILoadController.EndLoad ()
		{
			loading--;
			OnEndLoad ();
		}
		
		protected virtual void OnBeginLoad ()
		{
		}
		
		protected virtual void OnEndLoad ()
		{
		}
		
		protected void NotifyModified (string hint)
		{
			OnModified (new SolutionItemModifiedEventArgs (this, hint));
		}
		
		protected virtual void OnModified (SolutionItemModifiedEventArgs args)
		{
			if (Modified != null)
				Modified (this, args);
		}
		
		protected virtual void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			NotifyModified ("Name");
			if (NameChanged != null)
				NameChanged (this, e);
		}
		
		protected virtual void InitializeItemHandler ()
		{
		}
		
		
		internal protected abstract void OnClean (IProgressMonitor monitor, string solutionConfiguration);
		internal protected abstract BuildResult OnBuild (IProgressMonitor monitor, string solutionConfiguration);
		internal protected abstract void OnExecute (IProgressMonitor monitor, ExecutionContext context, string solutionConfiguration);
		internal protected abstract bool OnGetNeedsBuilding (string solutionConfiguration);
		internal protected abstract void OnSetNeedsBuilding (bool val, string solutionConfiguration);
		
		internal protected virtual DateTime OnGetLastBuildTime (string solutionConfiguration)
		{
			return DateTime.MinValue;
		}
		
		internal protected virtual bool OnGetCanExecute (ExecutionContext context, string solutionConfiguration)
		{
			return false;
		}
		
		public event SolutionItemRenamedEventHandler NameChanged;
		public event SolutionItemModifiedEventHandler Modified;
	}
}
