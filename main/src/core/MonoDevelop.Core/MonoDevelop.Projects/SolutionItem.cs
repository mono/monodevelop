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
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	public abstract class SolutionItem: IExtendedDataItem, IBuildTarget, ILoadController, IPolicyProvider
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
		
		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.Projects.SolutionItem"/> class.
		/// </summary>
		public SolutionItem()
		{
			ProjectExtensionUtil.LoadControl (this);
		}
		
		/// <summary>
		/// Initializes a new instance of this item, using an xml element as template
		/// </summary>
		/// <param name='template'>
		/// The template
		/// </param>
		public virtual void InitializeFromTemplate (XmlElement template)
		{
		}
		
		/// <summary>
		/// Gets the handler for this solution item
		/// </summary>
		/// <value>
		/// The solution item handler.
		/// </value>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown if there isn't a ISolutionItemHandler for this solution item
		/// </exception>
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
		
		/// <summary>
		/// Sets the handler for this solution item
		/// </summary>
		/// <param name='handler'>
		/// A handler.
		/// </param>
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
		
		/// <summary>
		/// Gets a service instance of a given type
		/// </summary>
		/// <returns>
		/// The service.
		/// </returns>
		/// <typeparam name='T'>
		/// Type of the service
		/// </typeparam>
		/// <remarks>
		/// This method looks for an imlpementation of a service of the given type.
		/// </remarks>
		public T GetService<T> () where T: class
		{
			return (T) GetService (typeof(T));
		}

		/// <summary>
		/// Gets a service instance of a given type
		/// </summary>
		/// <returns>
		/// The service.
		/// </returns>
		/// <param name='t'>
		/// Type of the service
		/// </param>
		/// <remarks>
		/// This method looks for an imlpementation of a service of the given type.
		/// The default implementation this instance if the type is an interface
		/// implemented by this instance. Otherwise, it looks for a service in
		/// the project extension chain.
		/// </remarks>
		public virtual object GetService (Type t)
		{
			if (t.IsInstanceOfType (this))
				return this;
			return Services.ProjectService.GetExtensionChain (this).GetService (this, t);
		}
		
		/// <summary>
		/// Gets the solution to which this item belongs
		/// </summary>
		public Solution ParentSolution {
			get {
				if (parentFolder != null)
					return parentFolder.ParentSolution;
				return parentSolution; 
			}
			internal set {
				parentSolution = value;
				NotifyBoundToSolution (true);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this item is currently being loaded from a file
		/// </summary>
		/// <remarks>
		/// While an item is loading, some events such as project file change events may be fired.
		/// This flag can be used to check if change events are caused by data being loaded.
		/// </remarks>
		public bool Loading {
			get { return loading > 0; }
		}
		
		/// <summary>
		/// Saves the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor.
		/// </param>
		public abstract void Save (IProgressMonitor monitor);
		
		/// <summary>
		/// Name of the solution item
		/// </summary>
		public abstract string Name { get; set; }
		
		/// <summary>
		/// Gets or sets the base directory of this solution item
		/// </summary>
		/// <value>
		/// The base directory.
		/// </value>
		/// <remarks>
		/// The base directory is the directory where files belonging to this project
		/// are placed. Notice that this directory may be different than the directory
		/// where the project file is placed.
		/// </remarks>
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
		
		/// <summary>
		/// Gets the directory where this solution item is placed
		/// </summary>
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
		
		/// <summary>
		/// Gets the default base directory.
		/// </summary>
		/// <remarks>
		/// The base directory is the directory where files belonging to this project
		/// are placed. Notice that this directory may be different than the directory
		/// where the project file is placed.
		/// </remarks>
		protected virtual FilePath GetDefaultBaseDirectory ( )
		{
			return ParentSolution.BaseDirectory;
		}

		/// <summary>
		/// Gets the identifier of this solution item
		/// </summary>
		/// <remarks>
		/// The identifier is unique inside the solution
		/// </remarks>
		public string ItemId {
			get { return ItemHandler.ItemId; }
		}
		
		/// <summary>
		/// Gets extended properties.
		/// </summary>
		/// <remarks>
		/// This dictionary can be used by add-ins to store arbitrary information about this solution item.
		/// Keys and values can be of any type.
		/// If a value implements IDisposable, the value will be disposed when this solution item is disposed.
		/// Values in this dictionary won't be serialized, unless they are registered as serializable using
		/// the /MonoDevelop/ProjectModel/ExtendedProperties extension point.
		/// </remarks>
		public IDictionary ExtendedProperties {
			get { return InternalGetExtendedProperties; }
		}
		
		/// <summary>
		/// Gets policies.
		/// </summary>
		/// <remarks>
		/// Returns a policy container which can be used to query policies specific for this
		/// solution item. If a policy is not defined for this item, the inherited value will be returned.
		/// </remarks>
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
		
		PolicyContainer IPolicyProvider.Policies {
			get {
				return Policies;
			}
		}
		
		/// <summary>
		/// Gets solution item properties specific to the current user
		/// </summary>
		/// <remarks>
		/// These properties are not stored in the project file, but in a separate file which is not to be shared
		/// with other users.
		/// User properties are only loaded when the project is loaded inside the IDE.
		/// </remarks>
		public PropertyBag UserProperties {
			get {
				if (userProperties == null)
					userProperties = new PropertyBag ();
				return userProperties; 
			}
		}
		
		/// <summary>
		/// Initializes the user properties of the item
		/// </summary>
		/// <param name='properties'>
		/// Properties to be set
		/// </param>
		/// <exception cref='InvalidOperationException'>
		/// The user properties have already been set
		/// </exception>
		/// <remarks>
		/// This method is used by the IDE to initialize the user properties when a project is loaded.
		/// </remarks>
		public void LoadUserProperties (PropertyBag properties)
		{
			if (userProperties != null)
				throw new InvalidOperationException ("User properties already loaded.");
			userProperties = properties;
		}
		
		/// <summary>
		/// Gets the parent solution folder.
		/// </summary>
		public SolutionFolder ParentFolder {
			get {
				return parentFolder;
			}
			internal set {
				parentFolder = value;
				if (internalChildren != null) {
					internalChildren.ParentFolder = value;
				}
				if (value != null && value.ParentSolution != null) {
					NotifyBoundToSolution (false);
				}
			}
		}

		// Normally, the ParentFolder setter fires OnBoundToSolution. However, when deserializing, child
		// ParentFolder hierarchies can become connected before the ParentSolution becomes set. This method
		// enables us to recursively fire the OnBoundToSolution call in those cases.
		void NotifyBoundToSolution (bool includeInternalChildren)
		{
			var folder = this as SolutionFolder;
			if (folder != null) {
				var items = folder.GetItemsWithoutCreating ();
				if (items != null) {
					foreach (var item in items) {
						item.NotifyBoundToSolution (includeInternalChildren);
					}
				}
			}
			if (includeInternalChildren && internalChildren != null) {
				internalChildren.NotifyBoundToSolution (includeInternalChildren);
			}
			OnBoundToSolution ();
		}


		/// <summary>
		/// Gets a value indicating whether this <see cref="MonoDevelop.Projects.SolutionItem"/> has been disposed.
		/// </summary>
		/// <value>
		/// <c>true</c> if disposed; otherwise, <c>false</c>.
		/// </value>
		internal protected bool Disposed { get; private set; }

		/// <summary>
		/// Releases all resource used by the <see cref="MonoDevelop.Projects.SolutionItem"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="MonoDevelop.Projects.SolutionItem"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="MonoDevelop.Projects.SolutionItem"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="MonoDevelop.Projects.SolutionItem"/> so the garbage collector can reclaim the memory that the
		/// <see cref="MonoDevelop.Projects.SolutionItem"/> was occupying.
		/// </remarks>
		public virtual void Dispose ()
		{
			Disposed = true;
			
			if (extendedProperties != null) {
				foreach (object ob in extendedProperties.Values) {
					IDisposable disp = ob as IDisposable;
					if (disp != null)
						disp.Dispose ();
				}
				extendedProperties = null;
			}
			if (handler != null) {
				handler.Dispose ();
				// handler = null;
			}
			if (userProperties != null) {
				((IDisposable)userProperties).Dispose ();
				userProperties = null;
			}
			
			// parentFolder = null;
			// parentSolution = null;
			// internalChildren = null;
			// policies = null;
		}
		
		/// <summary>
		/// Gets solution items referenced by this instance (items on which this item depends)
		/// </summary>
		/// <returns>
		/// The referenced items.
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to get the referenced items
		/// </param>
		public virtual IEnumerable<SolutionItem> GetReferencedItems (ConfigurationSelector configuration)
		{
			return new SolutionItem [0];
		}
		
		/// <summary>
		/// Runs a build or execution target.
		/// </summary>
		/// <returns>
		/// The result of the operation
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='target'>
		/// Name of the target
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to run the target
		/// </param>
		public BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			return Services.ProjectService.GetExtensionChain (this).RunTarget (monitor, this, target, configuration);
		}
		
		/// <summary>
		/// Cleans the files produced by this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to clean the project
		/// </param>
		public void Clean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ITimeTracker tt = Counters.BuildProjectTimer.BeginTiming ("Cleaning " + Name);
			try {
				//SolutionFolder handles the begin/end task itself, don't duplicate
				if (this is SolutionFolder) {
					RunTarget (monitor, ProjectService.CleanTarget, configuration);
					return;
				}
				
				try {
					SolutionEntityItem it = this as SolutionEntityItem;
					SolutionItemConfiguration iconf = it != null ? it.GetConfiguration (configuration) : null;
					string confName = iconf != null ? iconf.Id : configuration.ToString ();
					monitor.BeginTask (GettextCatalog.GetString ("Cleaning: {0} ({1})", Name, confName), 1);
					RunTarget (monitor, ProjectService.CleanTarget, configuration);
				} finally {
					monitor.EndTask ();
				}
			}
			finally {
				tt.End ();
			}
		}
		
		/// <summary>
		/// Builds the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to build the project
		/// </param>
		public BuildResult Build (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Build (monitor, configuration, false);
		}
		
		/// <summary>
		/// Builds the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to build the project
		/// </param>
		/// <param name='buildReferences'>
		/// When set to <c>true</c>, the referenced items will be built before building this item
		/// </param>
		public BuildResult Build (IProgressMonitor monitor, ConfigurationSelector solutionConfiguration, bool buildReferences)
		{
			ITimeTracker tt = Counters.BuildProjectTimer.BeginTiming ("Building " + Name);
			try {
				if (!buildReferences) {
					if (!NeedsBuilding (solutionConfiguration))
						return new BuildResult (new CompilerResults (null), "");
					
					//SolutionFolder's OnRunTarget handles the begin/end task itself, don't duplicate
					if (this is SolutionFolder) {
						return RunTarget (monitor, ProjectService.BuildTarget, solutionConfiguration);
					}
					
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
		
		/// <summary>
		/// Gets the time of the last build
		/// </summary>
		/// <returns>
		/// The last build time.
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to get the last build time.
		/// </param>
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
		
		/// <summary>
		/// Executes this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		public void Execute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			Services.ProjectService.GetExtensionChain (this).Execute (monitor, this, context, configuration);
		}
		
		/// <summary>
		/// Determines whether this solution item can be executed using the specified context and configuration.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can be executed; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return Services.ProjectService.GetExtensionChain (this).CanExecute (this, context, configuration);
		}

		/// <summary>
		/// Gets the execution targets.
		/// </summary>
		/// <returns>The execution targets.</returns>
		/// <param name="configuration">The configuration.</param>
		public IEnumerable<ExecutionTarget> GetExecutionTargets (ConfigurationSelector configuration)
		{
			return Services.ProjectService.GetExtensionChain (this).GetExecutionTargets (this, configuration);
		}

		public event EventHandler ExecutionTargetsChanged;

		protected virtual void OnExecutionTargetsChanged ()
		{
			if (ExecutionTargetsChanged != null)
				ExecutionTargetsChanged (this, EventArgs.Empty);
		}
		
		/// <summary>
		/// Checks if this solution item has modified files and has to be built
		/// </summary>
		/// <returns>
		/// <c>true</c> if the solution item has to be built
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to do the check
		/// </param>
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
		
		/// <summary>
		/// States whether this solution item needs to be built or not
		/// </summary>
		/// <param name='value'>
		/// Whether this solution item needs to be built or not
		/// </param>
		/// <param name='configuration'>
		/// Configuration for which to set the flag
		/// </param>
		public void SetNeedsBuilding (bool value, ConfigurationSelector configuration)
		{
			Services.ProjectService.GetExtensionChain (this).SetNeedsBuilding (this, value, configuration);
		}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Projects.SolutionItem"/> needs to be reload due to changes in project or solution file
		/// </summary>
		/// <value>
		/// <c>true</c> if needs reload; otherwise, <c>false</c>.
		/// </value>
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
		
		/// <summary>
		/// Registers an internal child item.
		/// </summary>
		/// <param name='item'>
		/// An item
		/// </param>
		/// <remarks>
		/// Some kind of projects may be composed of several child projects.
		/// By registering those child projects using this method, the child
		/// projects will be plugged into the parent solution infrastructure
		/// (so for example, the ParentSolution property for those projects
		/// will return the correct value)
		/// </remarks>
		protected void RegisterInternalChild (SolutionItem item)
		{
			if (internalChildren == null) {
				internalChildren = new SolutionFolder ();
				internalChildren.ParentFolder = parentFolder;
			}
			internalChildren.Items.Add (item);
		}
		
		/// <summary>
		/// Unregisters an internal child item.
		/// </summary>
		/// <param name='item'>
		/// The item
		/// </param>
		protected void UnregisterInternalChild (SolutionItem item)
		{
			if (internalChildren != null)
				internalChildren.Items.Remove (item);
		}
		
		/// <summary>
		/// Gets the string tag model description for this solution item
		/// </summary>
		/// <returns>
		/// The string tag model description
		/// </returns>
		/// <param name='conf'>
		/// Configuration for which to get the string tag model description
		/// </param>
		public virtual StringTagModelDescription GetStringTagModelDescription (ConfigurationSelector conf)
		{
			StringTagModelDescription model = new StringTagModelDescription ();
			model.Add (GetType ());
			model.Add (typeof(Solution));
			return model;
		}
		
		/// <summary>
		/// Gets the string tag model for this solution item
		/// </summary>
		/// <returns>
		/// The string tag model
		/// </returns>
		/// <param name='conf'>
		/// Configuration for which to get the string tag model
		/// </param>
		public virtual StringTagModel GetStringTagModel (ConfigurationSelector conf)
		{
			StringTagModel source = new StringTagModel ();
			source.Add (this);
			if (ParentSolution != null)
				source.Add (ParentSolution.GetStringTagModel ());
			return source;
		}
		
		/// <summary>
		/// Sorts a collection of solution items, taking into account the dependencies between them
		/// </summary>
		/// <returns>
		/// The sorted collection of items
		/// </returns>
		/// <param name='items'>
		/// Items to sort
		/// </param>
		/// <param name='configuration'>
		/// A configuration
		/// </param>
		/// <remarks>
		/// This methods sorts a collection of items, ensuring that every item is placed after all the items
		/// on which it depends.
		/// </remarks>
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
		
		/// <summary>
		/// Called when a load operation for this solution item has started
		/// </summary>
		protected virtual void OnBeginLoad ()
		{
		}
		
		/// <summary>
		/// Called when a load operation for this solution item has finished
		/// </summary>
		protected virtual void OnEndLoad ()
		{
		}
		
		/// <summary>
		/// Notifies that this solution item has been modified
		/// </summary>
		/// <param name='hint'>
		/// Hint about which part of the solution item has been modified. This will typically be the property name.
		/// </param>
		protected void NotifyModified (string hint)
		{
			OnModified (new SolutionItemModifiedEventArgs (this, hint));
		}
		
		/// <summary>
		/// Raises the modified event.
		/// </summary>
		/// <param name='args'>
		/// Arguments.
		/// </param>
		protected virtual void OnModified (SolutionItemModifiedEventArgs args)
		{
			if (Modified != null && !Disposed)
				Modified (this, args);
		}
		
		/// <summary>
		/// Raises the name changed event.
		/// </summary>
		/// <param name='e'>
		/// Arguments.
		/// </param>
		protected virtual void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			NotifyModified ("Name");
			if (NameChanged != null && !Disposed)
				NameChanged (this, e);
		}
		
		/// <summary>
		/// Initializes the item handler.
		/// </summary>
		/// <remarks>
		/// This method is called the first time an item handler is requested.
		/// Subclasses should override this method use SetItemHandler to
		/// assign a handler to this item.
		/// </remarks>
		protected virtual void InitializeItemHandler ()
		{
		}
		
		/// <summary>
		/// Runs a build or execution target.
		/// </summary>
		/// <returns>
		/// The result of the operation
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='target'>
		/// Name of the target
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to run the target
		/// </param>
		/// <remarks>
		/// Subclasses can override this method to provide a custom implementation of project operations such as
		/// build or clean. The default implementation delegates the execution to the more specific OnBuild
		/// and OnClean methods, or to the item handler for other targets.
		/// </remarks>
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
		
		/// <summary>
		/// Cleans the files produced by this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to clean the project
		/// </param>
		protected abstract void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration);
		
		/// <summary>
		/// Builds the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to build the project
		/// </param>
		protected abstract BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration);
		
		/// <summary>
		/// Executes this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		internal protected abstract void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration);
		
		/// <summary>
		/// Checks if this solution item has modified files and has to be built
		/// </summary>
		/// <returns>
		/// <c>true</c> if the solution item has to be built
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to do the check
		/// </param>
		internal protected abstract bool OnGetNeedsBuilding (ConfigurationSelector configuration);
		
		/// <summary>
		/// States whether this solution item needs to be built or not
		/// </summary>
		/// <param name='val'>
		/// Whether this solution item needs to be built or not
		/// </param>
		/// <param name='configuration'>
		/// Configuration for which to set the flag
		/// </param>
		internal protected abstract void OnSetNeedsBuilding (bool val, ConfigurationSelector configuration);
		
		/// <summary>
		/// Gets the time of the last build
		/// </summary>
		/// <returns>
		/// The last build time.
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to get the last build time.
		/// </param>
		internal protected virtual DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			return DateTime.MinValue;
		}
		
		/// <summary>
		/// Determines whether this solution item can be executed using the specified context and configuration.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can be executed; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		internal protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}

		internal protected virtual IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration)
		{
			yield break;
		}

		protected virtual void OnBoundToSolution ()
		{
		}
		
		/// <summary>
		/// Occurs when the name of the item changes
		/// </summary>
		public event SolutionItemRenamedEventHandler NameChanged;
		
		/// <summary>
		/// Occurs when the item is modified.
		/// </summary>
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
