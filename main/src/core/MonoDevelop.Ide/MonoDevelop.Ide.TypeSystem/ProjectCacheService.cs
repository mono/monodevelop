// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Linq;
namespace MonoDevelop.Ide.TypeSystem
{
/*
	class ProjectCacheService : IProjectCacheHostService
	{
		internal const int ImplicitCacheSize = 3;

		private readonly object _gate = new object ();

		private readonly Workspace _workspace;
		private readonly Dictionary<ProjectId, Cache> _activeCaches = new Dictionary<ProjectId, Cache> ();

		private readonly SimpleMRUCache _implicitCache = new SimpleMRUCache ();
		private readonly ImplicitCacheMonitor _implicitCacheMonitor;

		public ProjectCacheService (Workspace workspace, int implicitCacheTimeout, bool forceCleanup = false)
		{
			_workspace = workspace;

			// forceCleanup is for testing
			if (workspace?.Kind == WorkspaceKind.Host || forceCleanup) {
				// monitor implicit cache for host
				_implicitCacheMonitor = new ImplicitCacheMonitor (this, implicitCacheTimeout);
			}
		}

		public bool IsImplicitCacheEmpty {
			get {
				lock (_gate) {
					return _implicitCache.Empty;
				}
			}
		}

		public void ClearImplicitCache ()
		{
			lock (_gate) {
				_implicitCache.Clear ();
			}
		}

		public void ClearExpiredImplicitCache (DateTime expirationTime)
		{
			lock (_gate) {
				_implicitCache.ClearExpiredItems (expirationTime);
			}
		}

		public IDisposable EnableCaching (ProjectId key)
		{
			lock (_gate) {
				Cache cache;
				if (!_activeCaches.TryGetValue (key, out cache)) {
					cache = new Cache (this, key);
					_activeCaches.Add (key, cache);
				}

				cache.Count++;
				return cache;
			}
		}

		public T CacheObjectIfCachingEnabledForKey<T> (ProjectId key, object owner, T instance) where T : class
		{
			lock (_gate) {
				Cache cache;
				if (_activeCaches.TryGetValue (key, out cache)) {
					cache.CreateStrongReference (owner, instance);
				} else if (!PartOfP2PReferences (key)) {
					_implicitCache.Touch (instance);
					_implicitCacheMonitor?.Touch ();
				}

				return instance;
			}
		}

		private bool PartOfP2PReferences (ProjectId key)
		{
			if (_activeCaches.Count == 0 || _workspace == null) {
				return false;
			}

			var solution = _workspace.CurrentSolution;
			var graph = solution.GetProjectDependencyGraph ();

			foreach (var projectId in _activeCaches.Keys) {
				// this should be cheap. graph is cached everytime project reference is updated.
				var p2pReferences = (ImmutableHashSet<ProjectId>)graph.GetProjectsThatThisProjectTransitivelyDependsOn (projectId);
				if (p2pReferences.Contains (key)) {
					return true;
				}
			}

			return false;
		}

		public T CacheObjectIfCachingEnabledForKey<T> (ProjectId key, ICachedObjectOwner owner, T instance) where T : class
		{
			lock (_gate) {
				Cache cache;
				if (owner.CachedObject == null && _activeCaches.TryGetValue (key, out cache)) {
					owner.CachedObject = instance;
					cache.CreateOwnerEntry (owner);
				}

				return instance;
			}
		}

		private void DisableCaching (ProjectId key, Cache cache)
		{
			lock (_gate) {
				cache.Count--;
				if (cache.Count == 0) {
					_activeCaches.Remove (key);
					cache.FreeOwnerEntries ();
				}
			}
		}

		private class Cache : IDisposable
		{
			internal int Count;
			private readonly ProjectCacheService _cacheService;
			private readonly ProjectId _key;
			private readonly ConditionalWeakTable<object, object> _cache = new ConditionalWeakTable<object, object> ();
			private readonly List<WeakReference<ICachedObjectOwner>> _ownerObjects = new List<WeakReference<ICachedObjectOwner>> ();

			public Cache (ProjectCacheService cacheService, ProjectId key)
			{
				_cacheService = cacheService;
				_key = key;
			}

			public void Dispose ()
			{
				_cacheService.DisableCaching (_key, this);
			}

			internal void CreateStrongReference (object key, object instance)
			{
				object o;
				if (!_cache.TryGetValue (key, out o)) {
					_cache.Add (key, instance);
				}
			}

			internal void CreateOwnerEntry (ICachedObjectOwner owner)
			{
				_ownerObjects.Add (new WeakReference<ICachedObjectOwner> (owner));
			}

			internal void FreeOwnerEntries ()
			{
				foreach (var entry in _ownerObjects) {
					ICachedObjectOwner owner;
					if (entry.TryGetTarget (out owner)) {
						owner.CachedObject = null;
					}
				}
			}
		}

		private class SimpleMRUCache
		{
			private const int CacheSize = 3;

			private readonly Node[] nodes = new Node[CacheSize];

			public bool Empty {
				get {
					for (var i = 0; i < nodes.Length; i++) {
						if (nodes [i].Data != null) {
							return false;
						}
					}

					return true;
				}
			}

			public void Touch (object instance)
			{
				var oldIndex = -1;
				var oldTime = DateTime.UtcNow;

				for (var i = 0; i < nodes.Length; i++) {
					if (instance == nodes [i].Data) {
						nodes [i].LastTouched = DateTime.UtcNow;
						return;
					}

					if (oldTime >= nodes [i].LastTouched) {
						oldTime = nodes [i].LastTouched;
						oldIndex = i;
					}
				}

				Contract.Requires (oldIndex >= 0);
				nodes [oldIndex] = new Node (instance, DateTime.UtcNow);
			}

			public void ClearExpiredItems (DateTime expirationTime)
			{
				for (var i = 0; i < nodes.Length; i++) {
					if (nodes [i].Data != null && nodes [i].LastTouched < expirationTime) {
						nodes [i] = default(Node);
					}
				}
			}

			public void Clear ()
			{
				Array.Clear (nodes, 0, nodes.Length);
			}

			private struct Node
			{
				public readonly object Data;
				public DateTime LastTouched;

				public Node (object data, DateTime lastTouched)
				{
					Data = data;
					LastTouched = lastTouched;
				}
			}
		}

		private class ImplicitCacheMonitor : IdleProcessor
		{
			private readonly ProjectCacheService _owner;
			private readonly SemaphoreSlim _gate;

			public ImplicitCacheMonitor (ProjectCacheService owner, int backOffTimeSpanInMS) :
			base (//AggregateAsynchronousOperationListener.CreateEmptyListener(),
				backOffTimeSpanInMS,
				CancellationToken.None)
			{
				_owner = owner;
				_gate = new SemaphoreSlim (0);

				Start ();
			}

			protected override Task ExecuteAsync ()
			{
				_owner.ClearExpiredImplicitCache (DateTime.UtcNow - TimeSpan.FromMilliseconds (BackOffTimeSpanInMS));

				return SpecializedTasks.EmptyTask;
			}

			public void Touch ()
			{
				UpdateLastAccessTime ();

				if (_gate.CurrentCount == 0) {
					_gate.Release ();
				}
			}

			protected override Task WaitAsync (CancellationToken cancellationToken)
			{
				if (_owner.IsImplicitCacheEmpty) {
					return _gate.WaitAsync (cancellationToken);
				}

				return SpecializedTasks.EmptyTask;
			}
		}
	}

	internal abstract class IdleProcessor
	{
		private const int MinimumDelayInMS = 50;

		//		protected readonly IAsynchronousOperationListener Listener;
		protected readonly CancellationToken CancellationToken;
		protected readonly int BackOffTimeSpanInMS;

		// points to processor task
		private Task _processorTask;

		// there is one thread that writes to it and one thread reads from it
		private int _lastAccessTimeInMS;

		public IdleProcessor (
//			IAsynchronousOperationListener listener,
			int backOffTimeSpanInMS,
			CancellationToken cancellationToken)
		{
//			this.Listener = listener;
			this.CancellationToken = cancellationToken;

			BackOffTimeSpanInMS = backOffTimeSpanInMS;
			_lastAccessTimeInMS = Environment.TickCount;
		}

		protected abstract Task WaitAsync (CancellationToken cancellationToken);

		protected abstract Task ExecuteAsync ();

		protected void Start ()
		{
			if (_processorTask == null) {
				_processorTask = Task.Factory.SafeStartNewFromAsync (ProcessAsync, this.CancellationToken, TaskScheduler.Default);
			}
		}

		protected void UpdateLastAccessTime ()
		{
			_lastAccessTimeInMS = Environment.TickCount;
		}

		protected async Task WaitForIdleAsync ()
		{
			while (true) {
				if (this.CancellationToken.IsCancellationRequested) {
					return;
				}

				var diffInMS = Environment.TickCount - _lastAccessTimeInMS;
				if (diffInMS >= BackOffTimeSpanInMS) {
					return;
				}

				// TODO: will safestart/unwarp capture cancellation exception?
				var timeLeft = BackOffTimeSpanInMS - diffInMS;
				await Task.Delay (Math.Max (MinimumDelayInMS, timeLeft), this.CancellationToken).ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		private async Task ProcessAsync ()
		{
			while (true) {
				try {
					if (this.CancellationToken.IsCancellationRequested) {
						return;
					}

					// wait for next item available
					await WaitAsync (this.CancellationToken).ConfigureAwait (continueOnCapturedContext: false);

//					using (this.Listener.BeginAsyncOperation("ProcessAsync"))
//					{
					// we have items but workspace is busy. wait for idle.
					await WaitForIdleAsync ().ConfigureAwait (continueOnCapturedContext: false);

					await ExecuteAsync ().ConfigureAwait (continueOnCapturedContext: false);
//					}
				} catch (OperationCanceledException) {
					// ignore cancellation exception
				}
			}
		}

		public virtual Task AsyncProcessorTask {
			get {
				if (_processorTask == null) {
					return SpecializedTasks.EmptyTask;
				}

				return _processorTask;
			}
		}
	}
*/
	internal static class SpecializedTasks
	{
		public static readonly Task<bool> True = Task.FromResult<bool> (true);
		public static readonly Task<bool> False = Task.FromResult<bool> (false);
		public static readonly Task EmptyTask = Empty<object>.Default;

		public static Task<T> Default<T> ()
		{
			return Empty<T>.Default;
		}

		public static Task<ImmutableArray<T>> EmptyImmutableArray<T> ()
		{
			return Empty<T>.EmptyImmutableArray;
		}

		public static Task<T> FromResult<T> (T t) where T : class
		{
			return FromResultCache<T>.FromResult (t);
		}


		public static Task<IEnumerable<T>> EmptyEnumerable<T>()
		{
			return Empty<T>.EmptyEnumerable;
		}

		private static class Empty<T>
		{
			public static readonly Task<T> Default = Task.FromResult<T> (default(T));
			public static readonly Task<ImmutableArray<T>> EmptyImmutableArray = Task.FromResult (ImmutableArray<T>.Empty);
			public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(Enumerable.Empty<T> ());
		}

		private static class FromResultCache<T> where T : class
		{
			private static readonly ConditionalWeakTable<T, Task<T>> s_fromResultCache = new ConditionalWeakTable<T, Task<T>> ();
			private static readonly ConditionalWeakTable<T, Task<T>>.CreateValueCallback s_taskCreationCallback = Task.FromResult<T>;

			public static Task<T> FromResult (T t)
			{
				return s_fromResultCache.GetValue (t, s_taskCreationCallback);
			}
		}
	}

	[SuppressMessage ("ApiDesign", "RS0011", Justification = "Matching TPL Signatures")]
	internal static partial class TaskFactoryExtensions
	{
		public static Task SafeStartNew (this TaskFactory factory, Action action, CancellationToken cancellationToken, TaskScheduler scheduler)
		{
			return factory.SafeStartNew (action, cancellationToken, TaskCreationOptions.None, scheduler);
		}

		public static Task SafeStartNew (
			this TaskFactory factory,
			Action action,
			CancellationToken cancellationToken,
			TaskCreationOptions creationOptions,
			TaskScheduler scheduler)
		{
			Action wrapped = () => {
				try {
					action ();
				} catch (Exception) {
					throw new InvalidOperationException ("This program location is thought to be unreachable.");
				}
			};

			// The one and only place we can call StartNew().
			return factory.StartNew (wrapped, cancellationToken, creationOptions, scheduler);
		}

		public static Task<TResult> SafeStartNew<TResult> (this TaskFactory factory, Func<TResult> func, CancellationToken cancellationToken, TaskScheduler scheduler)
		{
			return factory.SafeStartNew (func, cancellationToken, TaskCreationOptions.None, scheduler);
		}

		public static Task<TResult> SafeStartNew<TResult> (
			this TaskFactory factory,
			Func<TResult> func,
			CancellationToken cancellationToken,
			TaskCreationOptions creationOptions,
			TaskScheduler scheduler)
		{
			Func<TResult> wrapped = () => {
				try {
					return func ();
				} catch (Exception) {
					throw new InvalidOperationException ("This program location is thought to be unreachable.");
				}
			};

			// The one and only place we can call StartNew<>().
			return factory.StartNew (wrapped, cancellationToken, creationOptions, scheduler);
		}

		public static Task SafeStartNewFromAsync (this TaskFactory factory, Func<Task> actionAsync, CancellationToken cancellationToken, TaskScheduler scheduler)
		{
			return factory.SafeStartNewFromAsync (actionAsync, cancellationToken, TaskCreationOptions.None, scheduler);
		}

		public static Task SafeStartNewFromAsync (
			this TaskFactory factory,
			Func<Task> actionAsync,
			CancellationToken cancellationToken,
			TaskCreationOptions creationOptions,
			TaskScheduler scheduler)
		{
			// The one and only place we can call StartNew<>().
			var task = factory.StartNew (actionAsync, cancellationToken, creationOptions, scheduler).Unwrap ();
			ReportFatalError (task, actionAsync);
			return task;
		}

		public static Task<TResult> SafeStartNewFromAsync<TResult> (this TaskFactory factory, Func<Task<TResult>> funcAsync, CancellationToken cancellationToken, TaskScheduler scheduler)
		{
			return factory.SafeStartNewFromAsync (funcAsync, cancellationToken, TaskCreationOptions.None, scheduler);
		}

		public static Task<TResult> SafeStartNewFromAsync<TResult> (
			this TaskFactory factory,
			Func<Task<TResult>> funcAsync,
			CancellationToken cancellationToken,
			TaskCreationOptions creationOptions,
			TaskScheduler scheduler)
		{
			// The one and only place we can call StartNew<>().
			var task = factory.StartNew (funcAsync, cancellationToken, creationOptions, scheduler).Unwrap ();
			ReportFatalError (task, funcAsync);
			return task;
		}

		internal static void ReportFatalError (Task task, object continuationFunction)
		{
			task.ContinueWith (ReportFatalErrorWorker, continuationFunction,
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}

		[MethodImpl (MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
		private static void ReportFatalErrorWorker (Task task, object continuationFunction)
		{
			var exception = task.Exception;
			var methodInfo = ((Delegate)continuationFunction).GetMethodInfo ();
			exception.Data ["ContinuationFunction"] = methodInfo.DeclaringType.FullName + "::" + methodInfo.Name;

			// In case of a crash with ExecutionEngineException w/o call stack it might be possible to get the stack trace using WinDbg:
			// > !threads // find thread with System.ExecutionEngineException
			//   ...
			//   67   65 4760 692b5d60   1029220 Preemptive  CD9AE70C:FFFFFFFF 012ad0f8 0     MTA (Threadpool Worker) System.ExecutionEngineException 03c51108 
			//   ...
			// > ~67s     // switch to thread 67
			// > !dso     // dump stack objects
			FatalError.Report (exception);
		}
	}

	internal static class FatalError
	{
		private static Action<Exception> s_fatalHandler;
		private static Action<Exception> s_nonFatalHandler;

		//private static Exception s_reportedException;
		//private static string s_reportedExceptionMessage;

		/// <summary>
		/// Set by the host to a fail fast trigger, 
		/// if the host desires to crash the process on a fatal exception.
		/// </summary>
		public static Action<Exception> Handler {
			get {
				return s_fatalHandler;
			}

			set {
				if (s_fatalHandler != value) {
					Debug.Assert (s_fatalHandler == null, "Handler already set");
					s_fatalHandler = value;
				}
			}
		}

		/// <summary>
		/// Set by the host to a fail fast trigger, 
		/// if the host desires to NOT crash the process on a non fatal exception.
		/// </summary>
		public static Action<Exception> NonFatalHandler {
			get {
				return s_nonFatalHandler;
			}

			set {
				if (s_nonFatalHandler != value) {
					Debug.Assert (s_nonFatalHandler == null, "Handler already set");
					s_nonFatalHandler = value;
				}
			}
		}

		// Same as setting the Handler property except that it avoids the assert.  This is useful in
		// test code which needs to verify the handler is called in specific cases and will continually
		// overwrite this value.
		public static void OverwriteHandler (Action<Exception> value)
		{
			s_fatalHandler = value;
		}

		/// <summary>
		/// Use in an exception filter to report a fatal error. 
		/// Unless the exception is <see cref="OperationCanceledException"/> 
		/// it calls <see cref="Handler"/>. The exception is passed through (the method returns false).
		/// </summary>
		/// <returns>False to avoid catching the exception.</returns>
		[DebuggerHidden]
		public static bool ReportUnlessCanceled (Exception exception)
		{
			if (exception is OperationCanceledException) {
				return false;
			}

			return Report (exception);
		}

		/// <summary>
		/// Use in an exception filter to report a non fatal error. 
		/// Unless the exception is <see cref="OperationCanceledException"/> 
		/// it calls <see cref="NonFatalHandler"/>. The exception isn't passed through (the method returns true).
		/// </summary>
		/// <returns>True to catch the exception.</returns>
		[DebuggerHidden]
		public static bool ReportWithoutCrashUnlessCanceled (Exception exception)
		{
			if (exception is OperationCanceledException) {
				return false;
			}

			return ReportWithoutCrash (exception);
		}

		/// <summary>
		/// Use in an exception filter to report a fatal error. 
		/// Unless the exception is <see cref="NotImplementedException"/> 
		/// it calls <see cref="Handler"/>. The exception is passed through (the method returns false).
		/// </summary>
		/// <returns>False to avoid catching the exception.</returns>
		[DebuggerHidden]
		public static bool ReportUnlessNotImplemented (Exception exception)
		{
			if (exception is NotImplementedException) {
				return false;
			}

			return Report (exception);
		}

		/// <summary>
		/// Use in an exception filter to report a fatal error.
		/// Calls <see cref="Handler"/> and passes the exception through (the method returns false).
		/// </summary>
		/// <returns>False to avoid catching the exception.</returns>
		[DebuggerHidden]
		public static bool Report (Exception exception)
		{
			Report (exception, s_fatalHandler);
			return false;
		}

		/// <summary>
		/// Use in an exception filter to report a non fatal error.
		/// Calls <see cref="NonFatalHandler"/> and doesn't pass the exception through (the method returns true).
		/// </summary>
		/// <returns>True to catch the exception.</returns>
		[DebuggerHidden]
		public static bool ReportWithoutCrash (Exception exception)
		{
			Report (exception, s_nonFatalHandler);
			return true;
		}

		private static void Report (Exception exception, Action<Exception> handler)
		{
			// hold onto last exception to make investigation easier
			//s_reportedException = exception;
			//s_reportedExceptionMessage = exception.ToString ();

			handler?.Invoke (exception);
		}
	}
}

