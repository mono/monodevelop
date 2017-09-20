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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects.MSBuild
{
	/// <summary>
	/// A frontend for a project loaded in a remote build engine
	/// </summary>
	/// <remarks>
	/// Clients of RemoteBuildEngineManager don't use this class directly,
	/// they instead use it through a IRemoteProjectBuilder, which is
	/// IDisposable, so it can be disposed after use (which will bring
	/// back this builder to the pool).
	/// </remarks>
	class RemoteProjectBuilder
	{
		RemoteProcessConnection connection;
		RemoteBuildEngine engine;
		string file;
		int projectId;
		static int lastTaskId;

		internal RemoteProjectBuilder (string file, int projectId, RemoteBuildEngine engine, RemoteProcessConnection connection)
		{
			this.file = file;
			this.projectId = projectId;
			this.engine = engine;
			this.connection = connection;
		}

		/// <summary>
		/// Project file
		/// </summary>
		/// <value>The file.</value>
		public string File => file;

		async Task CheckDisconnected ()
		{
			if (engine != null)
				await engine.CheckDisconnected ().ConfigureAwait (false);
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
				var res = await SendRun (configurations, loggerId, logger.EnabledEvents, verbosity, runTargets, evaluateItems, evaluateProperties, globalProperties, taskId).ConfigureAwait (false);
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

		/// <summary>
		/// Reloads a project in the remote builder
		/// </summary>
		public async Task Refresh ()
		{
			try {
				BeginOperation ();
				await SendRefresh ().ConfigureAwait (false);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild refresh failed", ex);
				await CheckDisconnected ().ConfigureAwait (false);
			} finally {
				EndOperation ();
			}
		}
		
		/// <summary>
		/// Updates the content of a project in the remote builder
		/// </summary>
		public async Task RefreshWithContent (string projectContent)
		{
			try {
				BeginOperation ();
				await SendRefreshWithContent (projectContent).ConfigureAwait (false);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild refresh failed", ex);
				await CheckDisconnected ().ConfigureAwait (false);
			} finally {
				EndOperation ();
			}
		}

#region IPC

		Task SendRefresh ()
		{
			return connection.SendMessage (new RefreshProjectRequest { ProjectId = projectId });
		}

		Task SendRefreshWithContent (string projectContent)
		{
			return connection.SendMessage (new RefreshWithContentRequest { ProjectId = projectId, Content = projectContent });
		}

		async Task<MSBuildResult> SendRun (ProjectConfigurationInfo [] configurations, int loggerId, MSBuildEvent enabledLogEvents, MSBuildVerbosity verbosity, string [] runTargets, string [] evaluateItems, string [] evaluateProperties, Dictionary<string, string> globalProperties, int taskId)
		{
			var msg = new RunProjectRequest {
				ProjectId = projectId,
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

#endregion

		void BeginOperation ()
		{
			// Do nothing for now
        }

		void EndOperation ()
		{
			// Do nothing for now
		}

		public void SetBusy ()
		{
			engine.SetBusy ();
        }

		public void ResetBusy ()
		{
			if (engine != null)
				engine.ResetBusy ();
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
				if (--references == 0) {
					RemoteBuildEngineManager.ReleaseProjectBuilder (engine).Ignore ();
					if (shuttingDown)
						Dispose ();
				}
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

		async void Dispose ()
		{
			if (!MSBuildProjectService.ShutDown && engine != null) {
				try {
					await engine.UnloadProject (this, projectId).ConfigureAwait (false);
				} catch {
					// Ignore
				}
				GC.SuppressFinalize (this);
				engine = null;
			}
		}
	}

	/// <summary>
	/// Proxy class used to access a RemoteProjectBuilder.
	/// It takes care of releasing the builder reference once it is
	/// disposed.
	/// </summary>
	class RemoteProjectBuilderProxy: IRemoteProjectBuilder
	{
		RemoteProjectBuilder builder;
		bool busySet;

		public RemoteProjectBuilderProxy (RemoteProjectBuilder builder, bool busySet)
		{
			this.builder = builder;
			this.busySet = busySet;
		}

		public Task<MSBuildResult> Run (
			ProjectConfigurationInfo [] configurations,
			TextWriter logWriter,
			MSBuildLogger logger,
			MSBuildVerbosity verbosity,
			string [] runTargets,
			string [] evaluateItems,
			string [] evaluateProperties,
			Dictionary<string, string> globalProperties,
			CancellationToken cancellationToken
		) {
			return builder.Run (configurations, logWriter, logger, verbosity, runTargets, evaluateItems, evaluateProperties, globalProperties, cancellationToken);
		}

		public void Dispose ()
		{
			if (busySet)
				builder.ResetBusy ();
			builder.ReleaseReference ();
		}
	}

	interface IRemoteProjectBuilder : IDisposable
	{
		Task<MSBuildResult> Run (
			ProjectConfigurationInfo [] configurations,
			TextWriter logWriter,
			MSBuildLogger logger,
			MSBuildVerbosity verbosity,
			string [] runTargets,
			string [] evaluateItems,
			string [] evaluateProperties,
			Dictionary<string, string> globalProperties,
			CancellationToken cancellationToken
		);
	}
}
