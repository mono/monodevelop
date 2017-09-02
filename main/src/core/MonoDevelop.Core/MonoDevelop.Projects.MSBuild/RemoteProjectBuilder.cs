// 
// RemoteProjectBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.IO;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects.MSBuild
{
	class RemoteBuildEngine
	{
		RemoteProcessConnection connection;
		bool alive = true;
		static int count;
		int busy;

		static int loggerIdCounter;
		Dictionary<int, LoggerInfo> loggers = new Dictionary<int, LoggerInfo> ();

		public int ReferenceCount { get; set; }
		public DateTime ReleaseTime { get; set; }

		List<RemoteProjectBuilder> remoteProjectBuilders = new List<RemoteProjectBuilder> ();

		public RemoteBuildEngine (RemoteProcessConnection connection)
		{
			this.connection = connection;
			Interlocked.Increment (ref count);
			connection.AddListener (this);
		}

		public event EventHandler Disconnected;

		public int AciveEngines {
			get {
				return count;
			}
		}

		public async Task<RemoteProjectBuilder> CreateRemoteProjectBuilder (string projectFile)
		{
			var builder = await LoadProject (projectFile).ConfigureAwait (false);
			var pb = new RemoteProjectBuilder (projectFile, builder, this);
			lock (remoteProjectBuilders) {
				remoteProjectBuilders.Add (pb);

				// Unlikely, but it may happen
				if (IsShuttingDown)
					pb.Shutdown ();
			}
			return pb;
		}

		async Task<ProjectBuilder> LoadProject (string projectFile)
		{
			try {
				var pid = (await connection.SendMessage (new LoadProjectRequest { ProjectFile = projectFile })).ProjectId;
				return new ProjectBuilder (connection, pid);
			} catch {
				await CheckDisconnected ();
				throw;
			}
		}
		
		internal async Task UnloadProject (RemoteProjectBuilder remoteBuilder, ProjectBuilder builder)
		{
			lock (remoteProjectBuilders)
				remoteProjectBuilders.Remove (remoteBuilder);
			
			try {
				await connection.SendMessage (new UnloadProjectRequest { ProjectId = ((ProjectBuilder)builder).ProjectId});
			} catch (Exception ex) {
				LoggingService.LogError ("Project unloading failed", ex);
				if (!await CheckDisconnected ())
					throw;
			}
		}

		/// <summary>
		/// Marks this instance as being shutdown, so it should not be used to create new project builders.
		/// </summary>
		public void Shutdown ()
		{
			lock (remoteProjectBuilders) {
				if (IsShuttingDown)
					return;
				IsShuttingDown = true;
				foreach (var pb in remoteProjectBuilders)
					pb.Shutdown ();
			}
		}

		public bool IsShuttingDown { get; private set; }

		public async Task CancelTask (int taskId)
		{
			try {
				await connection.SendMessage (new CancelTaskRequest { TaskId = taskId });
			} catch {
				await CheckDisconnected ();
				throw;
			}
		}

		public async Task SetGlobalProperties (Dictionary<string, string> properties)
		{
			try {
				await connection.SendMessage (new SetGlobalPropertiesRequest { Properties = properties });
			} catch {
				await CheckDisconnected ();
				throw;
			}
		}

		public async Task Ping ()
		{
			await connection.SendMessage (new PingRequest ());
		}

		async Task<bool> CheckAlive ()
		{
			if (!alive)
				return false;
			try {
				await Ping ();
				return true;
			} catch {
				alive = false;
				return false;
			}
		}

		internal async Task<bool> CheckDisconnected ()
		{
			if (!await CheckAlive ()) {
				if (Disconnected != null)
					Disconnected (this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		public void Dispose ()
		{
			Interlocked.Decrement (ref count);
			try {
				alive = false;
				connection.Disconnect ().Ignore ();
			} catch {
				// Ignore
			}
		}

		[MessageHandler]
		void OnLogMessage (LogMessage msg)
		{
			LoggerInfo logger = null;
			lock (loggers) {
				if (!loggers.TryGetValue (msg.LoggerId, out logger))
					return;
			}
			if (msg.LogText != null)
				logger.Writer.Write (msg.LogText);
			if (msg.Events != null && logger.Logger != null) {
				foreach (var e in msg.Events)
					logger.Logger.NotifyEvent (e);
			}
		}

		class LoggerInfo
		{
			public TextWriter Writer;
			public MSBuildLogger Logger;
		}

		public int RegisterLogger (TextWriter writer, MSBuildLogger logger)
		{
			lock (loggers) {
				var i = loggerIdCounter++;
				loggers [i] = new LoggerInfo { Writer = writer, Logger = logger };
				return i;
			}
		}

		public void UnregisterLogger (int id)
		{
			lock (loggers) {
				loggers.Remove (id);
			}
		}

		public bool Lock ()
		{
			return Interlocked.Increment (ref busy) == 1;
		}

		public void Unlock ()
		{
			Interlocked.Decrement (ref busy);
		}

		public bool IsBusy {
			get {
				return busy > 0;
			}
		}
	}

	class ProjectBuilder
	{
		public int ProjectId;
		RemoteProcessConnection connection;

		public ProjectBuilder (RemoteProcessConnection connection, int projectId)
		{
			this.connection = connection;
			this.ProjectId = projectId;
		}

		public void Dispose ()
		{
			connection.SendMessage (new DisposeProjectRequest { ProjectId = ProjectId });
		}

		public Task Refresh ()
		{
			return connection.SendMessage (new RefreshProjectRequest { ProjectId = ProjectId });
		}

		public Task RefreshWithContent (string projectContent)
		{
			return connection.SendMessage (new RefreshWithContentRequest { ProjectId = ProjectId, Content = projectContent });
		}

		public async Task<MSBuildResult> Run (ProjectConfigurationInfo [] configurations, int loggerId, MSBuildEvent enabledLogEvents, MSBuildVerbosity verbosity, string [] runTargets, string [] evaluateItems, string [] evaluateProperties, Dictionary<string, string> globalProperties, int taskId)
		{
			var msg = new RunProjectRequest {
				ProjectId = ProjectId,
				Configurations = configurations,
				LogWriterId = loggerId,
				EnabledLogEvents = enabledLogEvents,
				Verbosity = verbosity,
				RunTargets = runTargets,
				EvaluateItems = evaluateItems,
				EvaluateProperties = evaluateProperties,
				GlobalProperties = globalProperties,
				TaskId = taskId
			};

			var res = await connection.SendMessage (msg);

			// Make sure we get all log messages
			await connection.ProcessPendingMessages ();

			return res.Result;
		}
	}

	class RemoteProjectBuilder: IDisposable
	{
		RemoteBuildEngine engine;
		ProjectBuilder builder;
		Dictionary<string,AssemblyReference[]> referenceCache;
		AsyncCriticalSection referenceCacheLock = new AsyncCriticalSection ();
		Dictionary<string, PackageDependency[]> packageDependenciesCache;
		AsyncCriticalSection packageDependenciesCacheLock = new AsyncCriticalSection ();
		string file;
		static int lastTaskId;

		internal RemoteProjectBuilder (string file, ProjectBuilder builder, RemoteBuildEngine engine)
		{
			this.file = file;
			this.engine = engine;
			this.builder = builder;
			referenceCache = new Dictionary<string, AssemblyReference[]> ();
			packageDependenciesCache = new Dictionary<string, PackageDependency[]> ();
		}

		public event EventHandler Disconnected;

		async Task CheckDisconnected ()
		{
			if (engine != null && await engine.CheckDisconnected ().ConfigureAwait (false)) {
				if (Disconnected != null)
					Disconnected (this, EventArgs.Empty);
			}
		}

		IDisposable RegisterCancellation (CancellationToken cancellationToken, int taskId)
		{
			return cancellationToken.Register (async () => {
				try {
					BeginOperation ();
					await engine.CancelTask (taskId);
				} catch (Exception ex) {
					// Ignore
					LoggingService.LogError ("CancelTask failed", ex);
				} finally {
					EndOperation ();
				}
			});
		}

		public async Task<MSBuildResult> Run (
			ProjectConfigurationInfo[] configurations,
			TextWriter logWriter,
			MSBuildLogger logger,
			MSBuildVerbosity verbosity,
			string[] runTargets,
			string[] evaluateItems,
			string[] evaluateProperties,
			Dictionary<string, string> globalProperties,
			CancellationToken cancellationToken
		)
		{
			// Get an id for the task, and get ready to cancel it if the cancellation token is signalled
			var taskId = Interlocked.Increment (ref lastTaskId);
			var cr = RegisterCancellation (cancellationToken, taskId);
			var loggerId = engine.RegisterLogger (logWriter, logger);

			try {
				BeginOperation ();
				var res = await builder.Run (configurations, loggerId, logger.EnabledEvents, verbosity, runTargets, evaluateItems, evaluateProperties, globalProperties, taskId).ConfigureAwait (false);
				if (res == null && cancellationToken.IsCancellationRequested) {
					MSBuildTargetResult err = new MSBuildTargetResult (file, false, "", "", file, 1, 1, 1, 1, "Build cancelled", "");
					return new MSBuildResult (new [] { err });
				}
				if (res == null)
					throw new Exception ("Unknown failure");
				return res;
			} catch (Exception ex) {
				await CheckDisconnected ().ConfigureAwait (false);
				LoggingService.LogError ("RunTarget failed", ex);
				MSBuildTargetResult err = new MSBuildTargetResult (file, false, "", "", file, 1, 1, 1, 1, "Unknown MSBuild failure. Please try building the project again", "");
				MSBuildResult res = new MSBuildResult (new [] { err });
				return res;
			} finally {
				engine.UnregisterLogger (loggerId);
				EndOperation ();
				cr.Dispose ();
			}
		}

		public async Task<AssemblyReference[]> ResolveAssemblyReferences (ProjectConfigurationInfo[] configurations, Dictionary<string, string> globalProperties, MSBuildProject project, CancellationToken cancellationToken)
		{
			AssemblyReference[] refs = null;
			var id = configurations [0].Configuration + "|" + configurations [0].Platform;

			using (await referenceCacheLock.EnterAsync ().ConfigureAwait (false)) {
				// Check the cache before starting the task
				if (referenceCache.TryGetValue (id, out refs))
					return refs;

				// Get an id for the task, it will be used later on to cancel the task if necessary
				var taskId = Interlocked.Increment (ref lastTaskId);
				IDisposable cr = RegisterCancellation (cancellationToken, taskId);

				MSBuildResult result;
				try {
					BeginOperation ();
					result = await builder.Run (
								configurations, -1, MSBuildEvent.None, MSBuildVerbosity.Quiet,
								new [] { "ResolveAssemblyReferences" }, new [] { "ReferencePath" }, null, globalProperties, taskId
						).ConfigureAwait (false);
				} catch (Exception ex) {
					await CheckDisconnected ().ConfigureAwait (false);
					LoggingService.LogError ("ResolveAssemblyReferences failed", ex);
					return new AssemblyReference [0];
				} finally {
					cr.Dispose ();
					EndOperation ();
				}

				MSBuildEvaluatedItem[] items;
				if (result.Items.TryGetValue ("ReferencePath", out items) && items != null) {
					refs = items.Select (it => CreateAssemblyReference (it, project)).ToArray ();
				} else
					refs = new AssemblyReference [0];

				referenceCache [id] = refs;
			}
			return refs;
		}

		static AssemblyReference CreateAssemblyReference (MSBuildEvaluatedItem it, MSBuildProject project)
		{
			var imd = new MSBuildPropertyGroupEvaluated (project);
			if (it.Metadata.Count > 0) {
				var properties = new Dictionary<string, IMSBuildPropertyEvaluated> ();
				foreach (var m in it.Metadata)
					properties [m.Key] = new MSBuildPropertyEvaluated (project, m.Key, m.Value, m.Value);
				imd.SetProperties (properties);
			}
			return new AssemblyReference (it.ItemSpec, imd);
		}

		public async Task<PackageDependency[]> ResolvePackageDependencies (ProjectConfigurationInfo[] configurations, Dictionary<string, string> globalProperties, CancellationToken cancellationToken)
		{
			PackageDependency[] packageDependencies = null;
			var id = configurations [0].Configuration + "|" + configurations [0].Platform;

			using (await packageDependenciesCacheLock.EnterAsync ().ConfigureAwait (false)) {
				// Check the cache before starting the task
				if (packageDependenciesCache.TryGetValue (id, out packageDependencies))
					return packageDependencies;

				// Get an id for the task, it will be used later on to cancel the task if necessary
				var taskId = Interlocked.Increment (ref lastTaskId);
				IDisposable cr = RegisterCancellation (cancellationToken, taskId);

				MSBuildResult result;
				try {
					BeginOperation ();
					result = await builder.Run (
						configurations, -1, MSBuildEvent.None, MSBuildVerbosity.Quiet,
						new [] { "ResolvePackageDependenciesDesignTime" }, new [] { "_DependenciesDesignTime" }, null, globalProperties, taskId
					);
				} catch (Exception ex) {
					await CheckDisconnected ();
					LoggingService.LogError ("ResolvePackageDependencies failed", ex);
					return new PackageDependency [0];
				} finally {
					cr.Dispose ();
					EndOperation ();
				}

				MSBuildEvaluatedItem[] items;
				if (result == null)
					return new PackageDependency[0];
				else if (result.Items.TryGetValue ("_DependenciesDesignTime", out items) && items != null) {
					packageDependencies = items
						.Select (i => PackageDependency.Create (i))
						.Where (dependency => dependency != null)
						.ToArray ();
				} else
					packageDependencies = new PackageDependency [0];

				packageDependenciesCache [id] = packageDependencies;
			}

			return packageDependencies;
		}

		public async Task Refresh ()
		{
			using (await referenceCacheLock.EnterAsync ().ConfigureAwait (false))
				referenceCache.Clear ();

			using (await packageDependenciesCacheLock.EnterAsync ().ConfigureAwait (false))
				packageDependenciesCache.Clear ();

			try {
				BeginOperation ();
				await builder.Refresh ().ConfigureAwait (false);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild refresh failed", ex);
				await CheckDisconnected ().ConfigureAwait (false);
			} finally {
				EndOperation ();
			}
		}
		
		public async Task RefreshWithContent (string projectContent)
		{
			using (await referenceCacheLock.EnterAsync ().ConfigureAwait (false))
				referenceCache.Clear ();

			using (await packageDependenciesCacheLock.EnterAsync ().ConfigureAwait (false))
				packageDependenciesCache.Clear ();

			try {
				BeginOperation ();
				await builder.RefreshWithContent (projectContent).ConfigureAwait (false);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild refresh failed", ex);
				await CheckDisconnected ().ConfigureAwait (false);
			} finally {
				EndOperation ();
			}
		}

		public async void Dispose ()
		{
			if (!MSBuildProjectService.ShutDown && engine != null) {
				try {
					if (builder != null)
						await engine.UnloadProject (this, builder).ConfigureAwait (false);
					MSBuildProjectService.ReleaseProjectBuilder (engine);
				} catch {
					// Ignore
				}
				GC.SuppressFinalize (this);
				engine = null;
				builder = null;
			}
		}

		void BeginOperation ()
		{
			engine.Lock ();
        }

		void EndOperation ()
		{
			if (engine != null)
				engine.Unlock ();
		}

		public void Lock ()
		{
			BeginOperation ();
        }

		public void Unlock ()
		{
			EndOperation ();
		}

		public bool IsBusy {
			get {
				return engine.IsBusy;
			}
		}

		object usageLock = new object ();
		public int references;
		bool shuttingDown;

		public bool AddReference ()
		{
			lock (usageLock) {
				if (shuttingDown)
					return false;
				references++;
				return true;
			}
		}

		public void ReleaseReference ()
		{
			lock (usageLock) {
				if (--references == 0 && shuttingDown)
					Dispose ();
			}
		}

		public void Shutdown ()
		{
			lock (usageLock) {
				if (!shuttingDown) {
					shuttingDown = true;
					if (references == 0)
						Dispose ();
				}
			}
		}
	}
}
