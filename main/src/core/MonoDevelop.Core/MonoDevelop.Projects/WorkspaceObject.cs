// IWorkspaceObject.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Collections;
using MonoDevelop.Projects.Extensions;
using Mono.Addins;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;
using MonoDevelop.Core.StringParsing;
using System.Threading;
using MonoDevelop.Core.AddIns;


namespace MonoDevelop.Projects
{
	public abstract class WorkspaceObject: IExtendedDataItem, IFolderItem, IDisposable
	{
		Hashtable extendedProperties;
		bool initializeCalled;
		bool isShared;
		object localLock = new object ();
		ExtensionContext extensionContext;
		CancellationTokenSource disposeCancellation = new CancellationTokenSource ();
		HashSet<Task> activeTasks = new HashSet<Task> ();

		internal protected void Initialize<T> (T instance)
		{
			if (instance.GetType () != typeof(T))
				return;
			var delayedInitialize = CallContext.LogicalGetData ("MonoDevelop.DelayItemInitialization");
			if (delayedInitialize != null && (bool)delayedInitialize)
				return;
			EnsureInitialized ();
		}

		internal void EnsureInitialized ()
		{
			if (!initializeCalled) {
				initializeCalled = true;

				extensionContext = CreateExtensionContext (this);

				OnInitialize ();
				InitializeExtensionChain ();
				OnExtensionChainInitialized ();
			}
		}

		protected void AssertMainThread ()
		{
			if (isShared)
				Runtime.AssertMainThread ();
		}

		/// <summary>
		/// This CancellationTokenSource is used to cancel all async operations when the object is disposed.
		/// </summary>
		protected CancellationToken InstanceCancellationToken {
			get { return disposeCancellation.Token; }
		}

		/// <summary>
		/// Binds a task to this object. The object will track the task execution and if the object is disposed,
		/// it will try to cancel the task and will wait for the task to end.
		/// </summary>
		/// <returns>The task returned by the provided lambda.</returns>
		/// <param name="f">A lambda that takes a CancellationToken token as argument and returns the task
		/// to be tracked. The provided CancellationToken will be signalled when the object is disposed.</param>
		/// <typeparam name="T">Task return type</typeparam>
		public Task<T> BindTask<T> (Func<CancellationToken, Task<T>> f)
		{
			var t = f (disposeCancellation.Token);
			lock (activeTasks)
				activeTasks.Add (t);
			t.ContinueWith (tr => {
				lock (activeTasks)
					activeTasks.Remove (t);
			});
			return t;
		}

		/// <summary>
		/// Binds a task to this object. The object will track the task execution and if the object is disposed,
		/// it will try to cancel the task and will wait for the task to end.
		/// </summary>
		/// <returns>The task returned by the provided lambda.</returns>
		/// <param name="f">A lambda that takes a CancellationToken token as argument and returns the task
		/// to be tracked. The provided CancellationToken will be signalled when the object is disposed.</param>
		public Task BindTask (Func<CancellationToken, Task> f)
		{
			var t = f (disposeCancellation.Token);
			lock (activeTasks)
				activeTasks.Add (t);
			t.ContinueWith (tr => {
				lock (activeTasks)
					activeTasks.Remove (t);
			});
			return t;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is shared.
		/// </summary>
		/// <remarks>Shared objects can only be modified in the main thread</remarks>
		public bool IsShared {
			get { return isShared; }
		}

		/// <summary>
		/// Sets this object as shared, which means that it is accessible from several threads for reading,
		/// but it can only be modified in the main thread
		/// </summary>
		public void SetShared ()
		{
			OnSetShared ();
		}

		protected virtual void OnSetShared ()
		{
			isShared = true;
			ItemExtension.NotifyShared ();
		}

		public string Name {
			get { return OnGetName (); }
		}

		public FilePath ItemDirectory {
			get { return OnGetItemDirectory (); }
		}

		public FilePath BaseDirectory {
			get { return OnGetBaseDirectory (); }
		}

		public WorkspaceObject ParentObject { get; protected set; }

		public IEnumerable<WorkspaceObject> GetChildren ()
		{
			return OnGetChildren ();
		}

		public IEnumerable<T> GetAllItems<T> () where T: WorkspaceObject
		{
			if (this is T)
				yield return (T)this;
			foreach (var c in OnGetChildren ()) {
				foreach (var r in c.GetAllItems<T> ())
					yield return r;
			}
		}

		/// <summary>
		/// Gets extended properties.
		/// </summary>
		/// <remarks>
		/// This dictionary can be used by add-ins to store arbitrary information about this solution item.
		/// Keys and values can be of any type.
		/// If a value implements IDisposable, the value will be disposed when this item is disposed.
		/// </remarks>
		public IDictionary ExtendedProperties {
			get {
				return OnGetExtendedProperties ();
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="MonoDevelop.Projects.SolutionItem"/> has been disposed.
		/// </summary>
		/// <value>
		/// <c>true</c> if disposed; otherwise, <c>false</c>.
		/// </value>
		internal protected bool Disposed { get; private set; }

		public void Dispose ()
		{
			AssertMainThread ();

			if (Disposed)
				return;

			Disposed = true;

			disposeCancellation.Cancel ();

			Task[] allTasks;

			lock (activeTasks)
				allTasks = activeTasks.ToArray ();

			if (allTasks.Length > 0)
				Task.WhenAll (allTasks).ContinueWith (t => OnDispose (), TaskScheduler.FromCurrentSynchronizationContext ());
			else
				OnDispose ();
		}

		protected virtual void OnDispose ()
		{
			if (extensionChain != null) {
				extensionChain.Dispose ();
				extensionChain = null;
			}

			if (extendedProperties != null) {
				foreach (object ob in extendedProperties.Values) {
					IDisposable disp = ob as IDisposable;
					if (disp != null)
						disp.Dispose ();
				}
				extendedProperties = null;
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
		public T GetService<T> ()
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
		/// </remarks>
		public object GetService (Type t)
		{
			return ItemExtension.GetService (t);
		}

		public StringTagModelDescription GetStringTagModelDescription (ConfigurationSelector conf)
		{
			return ItemExtension.OnGetStringTagModelDescription (conf);
		}

		protected virtual StringTagModelDescription OnGetStringTagModelDescription (ConfigurationSelector conf)
		{
			var m = new StringTagModelDescription ();
			m.Add (GetType ());
			return m;
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
		public StringTagModel GetStringTagModel (ConfigurationSelector conf)
		{
			return ItemExtension.OnGetStringTagModel (conf);
		}

		protected virtual StringTagModel OnGetStringTagModel (ConfigurationSelector conf)
		{
			var m = new StringTagModel ();
			m.Add (this);
			return m;
		}

		/// <summary>
		/// Gets a value indicating whether the extension chain for this object has already been created and initialized
		/// </summary>
		protected bool IsExtensionChainCreated {
			get { return extensionChain != null; }
		}

		ExtensionChain extensionChain;
		protected ExtensionChain ExtensionChain {
			get {
				AssertExtensionChainCreated ();
				return extensionChain;
			}
		}

		protected void AssertExtensionChainCreated ()
		{
			if (extensionChain == null) {
				if (!initializeCalled)
					throw new InvalidOperationException ("The constructor of type " + GetType () + " must call Initialize(this)");
				else
					throw new InvalidOperationException ("The extension chain can't be used before OnExtensionChainInitialized() method is called");
			}
		}

		public ExtensionContext ExtensionContext {
			get {
				return this.extensionContext;
			}
		}

		WorkspaceObjectExtension itemExtension;

		WorkspaceObjectExtension ItemExtension {
			get {
				if (itemExtension == null)
					AssertExtensionChainCreated ();
				return itemExtension;
			}
		}

		public void AttachExtension (WorkspaceObjectExtension ext)
		{
			AssertMainThread ();
			ExtensionChain.AddExtension (ext);
			ext.Init (this);
		}

		public void DetachExtension (WorkspaceObjectExtension ext)
		{
			AssertMainThread ();
			ExtensionChain.RemoveExtension (ext);
		}

		void InitializeExtensionChain ()
		{
			// Create an initial empty extension chain. This avoid crashes in case a call to SupportsObject ends
			// calling methods from the extension

			var tempExtensions = new List<WorkspaceObjectExtension> ();
			tempExtensions.AddRange (CreateDefaultExtensions ().Reverse ());
			extensionChain = ExtensionChain.Create (tempExtensions.ToArray ());
			foreach (var e in tempExtensions)
				e.Init (this);

			// Collect extensions that support this object

			var extensions = new List<WorkspaceObjectExtension> ();
			foreach (ProjectModelExtensionNode node in GetModelExtensions (extensionContext)) {
				if (node.CanHandleObject (this)) {
					var ext = node.CreateExtension ();
					if (ext.SupportsObject (this)) {
						ext.SourceExtensionNode = node;
						extensions.Add (ext);
					}
				}
			}

			foreach (var e in tempExtensions)
				e.Dispose ();

			// Now create the final extension chain

			extensions.Reverse ();
			var defaultExts = CreateDefaultExtensions ().ToList ();
			defaultExts.Reverse ();
			extensionChain.SetDefaultInsertionPosition (defaultExts.FirstOrDefault ());
			extensions.AddRange (defaultExts);
			extensionChain = ExtensionChain.Create (extensions.ToArray ());
			foreach (var e in extensions)
				e.Init (this);
			
			itemExtension = ExtensionChain.GetExtension<WorkspaceObjectExtension> ();

			foreach (var e in extensions)
				e.OnExtensionChainCreated ();
		}

		static ProjectModelExtensionNode[] modelExtensions;

		internal static IEnumerable<ProjectModelExtensionNode> GetModelExtensions (ExtensionContext ctx)
		{
			if (ctx != null) {
				return Runtime.RunInMainThread (() => ctx.GetExtensionNodes (ProjectService.ProjectModelExtensionsPath)).Result.Cast<ProjectModelExtensionNode> ().Concat (customNodes).ToArray ();
			}
			else {
				if (modelExtensions == null)
					Runtime.RunInMainThread ((Action)InitExtensions).Wait ();
				return modelExtensions;
			}
		}

		static void InitExtensions ()
		{
			AddinManager.ExtensionChanged += OnExtensionsChanged;
			LoadExtensions ();
		}

		static void OnExtensionsChanged (object sender, ExtensionEventArgs args)
		{
			if (args.Path == ProjectService.ProjectModelExtensionsPath)
				LoadExtensions ();
		}

		static void LoadExtensions ()
		{
			// Create a context for loading the default extensions. The context is necessary because
			// the conditions declared in the extension point must always be present.
			var extensionContext = CreateExtensionContext (null);
			modelExtensions = extensionContext.GetExtensionNodes<ProjectModelExtensionNode> (ProjectService.ProjectModelExtensionsPath).Concat (customNodes).ToArray ();
		}

		static ExtensionContext CreateExtensionContext (WorkspaceObject targetObject)
		{
			var extensionContext = AddinManager.CreateExtensionContext ();
			if (targetObject == null) {
				extensionContext.RegisterCondition ("ItemType", FalseCondition.Instance);
				extensionContext.RegisterCondition ("AppliesTo", FalseCondition.Instance);
			} else {
				extensionContext.RegisterCondition ("ItemType", new ItemTypeCondition (targetObject.GetType ()));
				extensionContext.RegisterCondition ("AppliesTo", new AppliesToCondition (targetObject));
			}
			return extensionContext;
		}

		/// <summary>
		/// Ensures that this project has the extensions it requires according to its current state
		/// </summary>
		/// <remarks>
		/// This method will load new extensions that this project supports and will unload extensions that are not supported anymore.
		/// The set of extensions that a project supports may change over time, depending on the status of the project.
		/// </remarks>
		public void RefreshExtensions ()
		{
			// First of all look for new extensions that should be attached

			// Get the list of nodes for which an extension has been created

			var allExtensions = extensionChain.GetAllExtensions ().OfType<WorkspaceObjectExtension> ().ToList ();
			var loadedNodes = allExtensions.Where (ex => ex.SourceExtensionNode != null)
				.Select (ex => ex.SourceExtensionNode.Id).ToList ();
			var newExtensions = new List <WorkspaceObjectExtension> ();

			ProjectModelExtensionNode lastAddedNode = null;

			// Ensure conditions are re-evaluated.
			extensionContext = CreateExtensionContext (this);

			foreach (ProjectModelExtensionNode node in GetModelExtensions (extensionContext)) {
				// If the node already generated an extension, skip it
				if (loadedNodes.Contains (node.Id)) {
					lastAddedNode = node;
					loadedNodes.Remove (node.Id);
					continue;
				}

				// Maybe the node can now generate an extension for this project
				if (node.CanHandleObject (this)) {
					var ext = node.CreateExtension ();
					if (ext.SupportsObject (this)) {
						ext.SourceExtensionNode = node;
						newExtensions.Add (ext);
						if (lastAddedNode != null) {
							// There is an extension before this one. Find it and add the new extension after it.
							var prevExtension = allExtensions.FirstOrDefault (ex => ex.SourceExtensionNode?.Id == lastAddedNode.Id);
							extensionChain.AddExtension (ext, prevExtension);
						} else
							extensionChain.AddExtension (ext);
						ext.Init (this);
					}
				}
			}

			// Now dispose extensions that are not supported anymore

			foreach (var ext in allExtensions) {
				if (!ext.SupportsObject (this))
					ext.Dispose ();
			}

			if (loadedNodes.Any ()) {
				foreach (var ext in allExtensions.Where (ex => ex.SourceExtensionNode != null)) {
					if (loadedNodes.Contains (ext.SourceExtensionNode.Id)) {
						ext.Dispose ();
						loadedNodes.Remove (ext.SourceExtensionNode.Id);
					}
				}
			}

			foreach (var e in newExtensions)
				e.OnExtensionChainCreated ();
			foreach (var e in newExtensions)
				OnExtensionActivated (e);
		}

		static List<ProjectModelExtensionNode> customNodes = new List<ProjectModelExtensionNode> ();

		internal static void RegisterCustomExtension (ProjectModelExtensionNode node)
		{
			customNodes.Add (node);
			LoadExtensions ();
		}

		internal static void UnregisterCustomExtension (ProjectModelExtensionNode node)
		{
			customNodes.Remove (node);
			LoadExtensions ();
		}

		protected virtual IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			yield return new DefaultWorkspaceObjectExtension ();
		}

		/// <summary>
		/// Called after the object is created, but before the extension chain has been created.
		/// </summary>
		protected virtual void OnInitialize ()
		{
		}

		/// <summary>
		/// Called when the extension chain for this object has been created. This method can be overriden
		/// to do initializations on the object that require access to the extension chain
		/// </summary>
		protected virtual void OnExtensionChainInitialized ()
		{
		}

		/// <summary>
		/// Called when an extension is dynamically loaded after object initialization
		/// </summary>
		/// <param name="extension">The extension.</param>
		/// <remarks>
		/// This method is called when an extension is loaded after the object initialization process.
		/// This may happen for example when calling RefreshExtensions.
		/// </remarks>
		protected virtual void OnExtensionActivated (WorkspaceObjectExtension extension)
		{
		}

		internal virtual IDictionary OnGetExtendedProperties ()
		{
			lock (localLock) {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}

		protected virtual IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			yield break;
		}

		protected virtual object OnGetService (Type t)
		{
			return t.IsInstanceOfType (this) ? this : null;
		}

		protected abstract string OnGetName ();

		protected abstract string OnGetItemDirectory ();

		protected abstract string OnGetBaseDirectory ();

		internal class DefaultWorkspaceObjectExtension: WorkspaceObjectExtension
		{
			internal protected override object GetService (Type t)
			{
				return Owner.OnGetService (t);
			}

			internal protected override StringTagModelDescription OnGetStringTagModelDescription (ConfigurationSelector conf)
			{
				return Owner.OnGetStringTagModelDescription (conf);
			}

			internal protected override StringTagModel OnGetStringTagModel (ConfigurationSelector conf)
			{
				return Owner.OnGetStringTagModel (conf);
			}
		}

		protected Task<IDisposable> ReadLock ()
		{
			lock (lockLock) {
				var ts = new TaskCompletionSource<IDisposable> ();
				var ol = new ObjectLock { Object = this, IsWriteLock = false, TaskSource = ts };
				if (writeLockTaken) {
					if (lockRequests == null)
						lockRequests = new Queue<ObjectLock> ();
					lockRequests.Enqueue (ol);
				} else {
					readLocksTaken++;
					ts.SetResult (ol);
				}
				return ts.Task;
			}
		}

		protected Task<IDisposable> WriteLock ()
		{
			lock (lockLock) {
				var ts = new TaskCompletionSource<IDisposable> ();
				var ol = new ObjectLock { Object = this, IsWriteLock = true, TaskSource = ts };
				if (writeLockTaken || readLocksTaken > 0) {
					if (lockRequests == null)
						lockRequests = new Queue<ObjectLock> ();
					lockRequests.Enqueue (ol);
				} else {
					writeLockTaken = true;
					ts.SetResult (ol);
				}
				return ts.Task;
			}
		}

		void ReleaseLock (bool isWriteLock)
		{
			lock (lockLock) {
				if (!isWriteLock) {
					// If there are readers still running, we can't release the lock
					if (--readLocksTaken > 0)
						return;
				}

				while (lockRequests != null && lockRequests.Count > 0) {
					// If readers have been awakened, we can't awaken a writer
					if (readLocksTaken > 0 && lockRequests.Peek ().IsWriteLock)
						return;
					var next = lockRequests.Dequeue ();
					if (next.IsWriteLock) {
						// Only one writer at a time
						next.TaskSource.SetResult (next);
						return;
					} else {
						// All readers can be awakened at once
						writeLockTaken = false;
						readLocksTaken++;
						next.TaskSource.SetResult (next);
					}
				}
				writeLockTaken = false;
			}
		}

		class ObjectLock: IDisposable
		{
			public WorkspaceObject Object;
			public bool IsWriteLock;
			public TaskCompletionSource<IDisposable> TaskSource;

			public void Dispose ()
			{
				Object.ReleaseLock (IsWriteLock);
			}
		}

		Queue<ObjectLock> lockRequests;

		int readLocksTaken;
		bool writeLockTaken;

		object lockLock = new object ();
	}

	public static class WorkspaceObjectExtensions
	{
		public static T As<T> (this WorkspaceObject ob) where T:class
		{
			return ob != null ? ob.GetService<T> () : null;
		}
	}
	
	public interface IWorkspaceFileObject: IFileItem, IDisposable
	{
		IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles);
		new FilePath FileName { get; set; }
		bool NeedsReload { get; set; }
		bool ItemFilesChanged { get; }
		Task SaveAsync (ProgressMonitor monitor);
		string Name { get; set; }
		FilePath BaseDirectory { get; }
		FilePath ItemDirectory { get; }
	}
}
