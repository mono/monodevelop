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
using MonoDevelop.Core.Collections;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Projects
{
	public abstract class SolutionItem: IExtendedDataItem, IBuildTarget, ILoadController
	{
		SolutionFolder parentFolder;
		Solution parentSolution;
		ISolutionItemHandler handler;
		int loading;
		SolutionFolder internalChildren;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		string baseDirectory;
		
		Hashtable extendedProperties;
		
		[ItemProperty ("Policies", IsExternal = true, SkipEmpty = true)]
		MonoDevelop.Projects.Policies.PolicyBag policies;
		
		PropertyBag userProperties;
		
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
		
		/// <summary>
		/// Gets the author information for this solution item, inherited from the solution and global settings.
		/// </summary>
		public AuthorInformation AuthorInformation {
			get {
				if (ParentSolution != null)
					return ParentSolution.AuthorInformation;
				else
					return AuthorInformation.Default;
			}
		}
		
		public T GetService<T> () where T: class
		{
			return (T) GetService (typeof(T));
		}
		
		public virtual object GetService (Type t)
		{
			if (t.IsInstanceOfType (this))
				return this;
			return Services.ProjectService.GetExtensionChain (this).GetService (this, t);
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
		
		public bool Loading {
			get { return loading > 0; }
		}
		
		public abstract void Save (IProgressMonitor monitor);
		
		public abstract string Name { get; set; }
		
		public FilePath BaseDirectory {
			get {
				if (baseDirectory == null) {
					FilePath dir = GetDefaultBaseDirectory ();
					if (dir.IsNullOrEmpty)
						dir = ".";
					return dir.FullPath;
				}
				else
					return baseDirectory;
			}
			set {
				FilePath def = GetDefaultBaseDirectory ();
				if (value != FilePath.Null && def != FilePath.Null && value.FullPath == def.FullPath)
					baseDirectory = null;
				else if (string.IsNullOrEmpty (value))
					baseDirectory = null;
				else
					baseDirectory = value.FullPath;
				NotifyModified ("BaseDirectory");
			}
		}
		
		public FilePath ItemDirectory {
			get {
				FilePath dir = GetDefaultBaseDirectory ();
				if (string.IsNullOrEmpty (dir))
					dir = ".";
				return dir.FullPath;
			}
		}
		
		internal bool HasCustomBaseDirectory {
			get { return baseDirectory != null; }
		}

		protected virtual FilePath GetDefaultBaseDirectory ( )
		{
			return ParentSolution.BaseDirectory;
		}
		
		public string ItemId {
			get { return ItemHandler.ItemId; }
		}
		
		public IDictionary ExtendedProperties {
			get { return InternalGetExtendedProperties; }
		}
		
		public MonoDevelop.Projects.Policies.PolicyBag Policies {
			get {
				//newly created (i.e. not deserialised) SolutionItems may have a null PolicyBag
				if (policies == null)
					policies = new MonoDevelop.Projects.Policies.PolicyBag ();
				//this is the easiest reliable place to associate a deserialised Policybag with its owner
				policies.Owner = this;
				return policies;
			}
			//setter so that a solution can deserialise the PropertyBag on its RootFolder
			internal set {
				policies = value;
			}
		}
		
		// User properties are only loaded when the project is loaded in the IDE.
		public PropertyBag UserProperties {
			get {
				if (userProperties == null)
					userProperties = new PropertyBag ();
				return userProperties; 
			}
		}
		
		// Initializes the user properties of the item
		public void LoadUserProperties (PropertyBag properties)
		{
			if (userProperties != null)
				throw new InvalidOperationException ("User properties already loaded.");
			userProperties = properties;
		}
		
		public SolutionFolder ParentFolder {
			get {
				return parentFolder;
			}
			internal set {
				parentFolder = value;
				if (internalChildren != null)
					internalChildren.ParentFolder = value;
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
			if (userProperties != null)
				((IDisposable)userProperties).Dispose ();
		}
		
		public virtual IEnumerable<SolutionItem> GetReferencedItems (ConfigurationSelector configuration)
		{
			return new SolutionItem [0];
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			return Services.ProjectService.GetExtensionChain (this).RunTarget (monitor, this, target, configuration);
		}
		
		public void Clean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			RunTarget (monitor, ProjectService.CleanTarget, configuration);
		}
		
		public BuildResult Build (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Build (monitor, configuration, false);
		}
		
		public BuildResult Build (IProgressMonitor monitor, ConfigurationSelector solutionConfiguration, bool buildReferences)
		{
			ITimeTracker tt = Counters.BuildProjectTimer.BeginTiming ("Building " + Name);
			try {
				if (!buildReferences) {
					if (!NeedsBuilding (solutionConfiguration))
						return new BuildResult (new CompilerResults (null), "");
						
					try {
						SolutionEntityItem it = this as SolutionEntityItem;
						SolutionItemConfiguration iconf = it != null ? it.GetConfiguration (solutionConfiguration) : null;
						string confName = iconf != null ? iconf.Id : solutionConfiguration.ToString ();
						monitor.BeginTask (GettextCatalog.GetString ("Building: {0} ({1})", Name, confName), 1);
						
						// This will end calling OnBuild ()
						return RunTarget (monitor, ProjectService.BuildTarget, solutionConfiguration);
						
					} finally {
						monitor.EndTask ();
					}
				}
					
				// Get a list of all items that need to be built (including this),
				// and build them in the correct order
				
				List<SolutionItem> referenced = new List<SolutionItem> ();
				Set<SolutionItem> visited = new Set<SolutionItem> ();
				GetBuildableReferencedItems (visited, referenced, this, solutionConfiguration);
				
				ReadOnlyCollection<SolutionItem> sortedReferenced = SolutionFolder.TopologicalSort (referenced, solutionConfiguration);
				
				BuildResult cres = new BuildResult ();
				cres.BuildCount = 0;
				HashSet<SolutionItem> failedItems = new HashSet<SolutionItem> ();
				
				monitor.BeginTask (null, sortedReferenced.Count);
				foreach (SolutionItem p in sortedReferenced) {
					if (p.NeedsBuilding (solutionConfiguration) && !p.ContainsReferences (failedItems, solutionConfiguration)) {
						BuildResult res = p.Build (monitor, solutionConfiguration, false);
						cres.Append (res);
						if (res.ErrorCount > 0)
							failedItems.Add (p);
					} else
						failedItems.Add (p);
					monitor.Step (1);
					if (monitor.IsCancelRequested)
						break;
				}
				monitor.EndTask ();
				return cres;
			} finally {
				tt.End ();
			}
		}
		
		internal bool ContainsReferences (HashSet<SolutionItem> items, ConfigurationSelector conf)
		{
			foreach (SolutionItem it in GetReferencedItems (conf))
				if (items.Contains (it))
					return true;
			return false;
		}
		
		public DateTime GetLastBuildTime (ConfigurationSelector configuration)
		{
			return OnGetLastBuildTime (configuration);
		}
		
		void GetBuildableReferencedItems (Set<SolutionItem> visited, List<SolutionItem> referenced, SolutionItem item, ConfigurationSelector configuration)
		{
			if (!visited.Add(item))
				return;
			
			if (item.NeedsBuilding (configuration))
				referenced.Add (item);

			foreach (SolutionItem ritem in item.GetReferencedItems (configuration))
				GetBuildableReferencedItems (visited, referenced, ritem, configuration);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			Services.ProjectService.GetExtensionChain (this).Execute (monitor, this, context, configuration);
		}
		
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return Services.ProjectService.GetExtensionChain (this).CanExecute (this, context, configuration);
		}
		
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			using (Counters.NeedsBuildingTimer.BeginTiming ("NeedsBuilding check for " + Name)) {
				if (ParentSolution != null && this is SolutionEntityItem) {
					SolutionConfiguration sconf = ParentSolution.GetConfiguration (configuration);
					if (sconf != null && !sconf.BuildEnabledForItem ((SolutionEntityItem) this))
						return false;
				}
				return Services.ProjectService.GetExtensionChain (this).GetNeedsBuilding (this, configuration);
			}
		}
		
		public void SetNeedsBuilding (bool value, ConfigurationSelector configuration)
		{
			Services.ProjectService.GetExtensionChain (this).SetNeedsBuilding (this, value, configuration);
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
		
		protected void RegisterInternalChild (SolutionItem item)
		{
			if (internalChildren == null) {
				internalChildren = new SolutionFolder ();
				internalChildren.ParentFolder = parentFolder;
			}
			internalChildren.Items.Add (item);
		}
		
		protected void UnregisterInternalChild (SolutionItem item)
		{
			if (internalChildren != null)
				internalChildren.Items.Remove (item);
		}
		
		public virtual StringTagModelDescription GetStringTagModelDescription (ConfigurationSelector conf)
		{
			StringTagModelDescription model = new StringTagModelDescription ();
			model.Add (GetType ());
			model.Add (typeof(Solution));
			return model;
		}
		
		public virtual StringTagModel GetStringTagModel (ConfigurationSelector conf)
		{
			StringTagModel source = new StringTagModel ();
			source.Add (this);
			if (ParentSolution != null)
				source.Add (ParentSolution.GetStringTagModel ());
			return source;
		}
		
		public static ReadOnlyCollection<T> TopologicalSort<T> (IEnumerable<T> items, ConfigurationSelector configuration) where T: SolutionItem
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
		
		static void Insert<T> (int index, IList<T> allItems, List<T> sortedItems, bool[] inserted, bool[] triedToInsert, ConfigurationSelector solutionConfiguration) where T: SolutionItem
		{
			if (triedToInsert[index]) {
				throw new CyclicDependencyException ();
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
		
		internal protected virtual BuildResult OnRunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget)
				return OnBuild (monitor, configuration);
			else if (target == ProjectService.CleanTarget) {
				OnClean (monitor, configuration);
				return new BuildResult ();
			}
			return ItemHandler.RunTarget (monitor, target, configuration) ?? new BuildResult ();
		}
		
		protected abstract void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration);
		protected abstract BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration);
		internal protected abstract void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration);
		internal protected abstract bool OnGetNeedsBuilding (ConfigurationSelector configuration);
		internal protected abstract void OnSetNeedsBuilding (bool val, ConfigurationSelector configuration);
		
		internal protected virtual DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			return DateTime.MinValue;
		}
		
		internal protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}
		
		public event SolutionItemRenamedEventHandler NameChanged;
		public event SolutionItemModifiedEventHandler Modified;
	}
	
	[Mono.Addins.Extension]
	class SolutionItemTagProvider: StringTagProvider<SolutionItem>, IStringTagProvider
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("ProjectName", "Project Name");
			yield return new StringTagDescription ("ProjectDir", "Project Directory");
			yield return new StringTagDescription ("AuthorName", "Project Author Name");
			yield return new StringTagDescription ("AuthorEmail", "Project Author Email");
			yield return new StringTagDescription ("AuthorCopyright", "Project Author Copyright");
			yield return new StringTagDescription ("AuthorCompany", "Project Author Company");
			yield return new StringTagDescription ("AuthorTrademark", "Project Trademark");
		}
		
		public override object GetTagValue (SolutionItem item, string tag)
		{
			switch (tag) {
				case "ITEMNAME":
				case "PROJECTNAME":
					return item.Name;
				case "AUTHORCOPYRIGHT":
					AuthorInformation authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
					return authorInfo.Copyright;
				case "AUTHORCOMPANY":
					authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
					return authorInfo.Company;
				case "AUTHORTRADEMARK":
					authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
					return authorInfo.Trademark;
				case "AUTHOREMAIL":
					authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
					return authorInfo.Email;
				case "AUTHORNAME":
					authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
					return authorInfo.Name;
				case "ITEMDIR":
				case "PROJECTDIR":
					return item.BaseDirectory;
			}
			throw new NotSupportedException ();
		}
	}
}
