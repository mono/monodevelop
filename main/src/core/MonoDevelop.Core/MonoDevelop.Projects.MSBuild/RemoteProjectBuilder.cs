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

namespace MonoDevelop.Projects.MSBuild
{
	class RemoteBuildEngine: IBuildEngine
	{
		IBuildEngine engine;
		Process proc;
		bool alive = true;
		static int count;
		int busy;

		public int ReferenceCount { get; set; }
		public DateTime ReleaseTime { get; set; }
		
		public RemoteBuildEngine (Process proc, IBuildEngine engine)
		{
			this.proc = proc;
			this.engine = engine;

			Interlocked.Increment (ref count);
		}

		public event EventHandler Disconnected;

		public int AciveEngines {
			get {
				return count;
			}
		}

		public IProjectBuilder LoadProject (string projectFile)
		{
			try {
				return engine.LoadProject (projectFile);
			} catch {
				CheckDisconnected ();
				throw;
			}
		}
		
		public void UnloadProject (IProjectBuilder pb)
		{
			try {
				engine.UnloadProject (pb);
			} catch (Exception ex) {
				LoggingService.LogError ("Project unloading failed", ex);
				if (!CheckDisconnected ())
					throw;
			}
		}

		public void CancelTask (int taskId)
		{
			try {
				engine.CancelTask (taskId);
			} catch {
				CheckDisconnected ();
				throw;
			}
		}

		public void SetCulture (CultureInfo uiCulture)
		{
			try {
				engine.SetCulture (uiCulture);
			} catch {
				CheckDisconnected ();
				throw;
			}
		}

		public void SetGlobalProperties (IDictionary<string, string> properties)
		{
			try {
				engine.SetGlobalProperties (properties);
			} catch {
				CheckDisconnected ();
				throw;
			}
		}

		void IBuildEngine.Ping ()
		{
			engine.Ping ();
		}

		bool CheckAlive ()
		{
			if (!alive)
				return false;
			try {
				engine.Ping ();
				return true;
			} catch {
				alive = false;
				return false;
			}
		}

		internal bool CheckDisconnected ()
		{
			if (!CheckAlive ()) {
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
				if (proc != null) {
					try {
						proc.Kill ();
					} catch {
					}
				}
				else
					engine.Dispose ();
			} catch {
				// Ignore
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
	
	public class RemoteProjectBuilder: IDisposable
	{
		RemoteBuildEngine engine;
		IProjectBuilder builder;
		Dictionary<string,string[]> referenceCache;
		AsyncCriticalSection referenceCacheLock = new AsyncCriticalSection ();
		string file;
		static int lastTaskId;

		internal RemoteProjectBuilder (string file, RemoteBuildEngine engine)
		{
			this.file = file;
			this.engine = engine;
			builder = engine.LoadProject (file);
			referenceCache = new Dictionary<string, string[]> ();
		}

		public event EventHandler Disconnected;

		void CheckDisconnected ()
		{
			if (engine != null && engine.CheckDisconnected ()) {
				if (Disconnected != null)
					Disconnected (this, EventArgs.Empty);
			}
		}

		IDisposable RegisterCancellation (CancellationToken cancellationToken, int taskId)
		{
			return cancellationToken.Register (() => {
				try {
					BeginOperation ();
					engine.CancelTask (taskId);
				} catch (Exception ex) {
					// Ignore
					LoggingService.LogError ("CancelTask failed", ex);
				} finally {
					EndOperation ();
				}
			});
		}

		public Task<MSBuildResult> Run (
			ProjectConfigurationInfo[] configurations,
			ILogWriter logWriter,
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

			var t = Task.Run (() => {
				try {
					BeginOperation ();
					var res = builder.Run (configurations, logWriter, verbosity, runTargets, evaluateItems, evaluateProperties, globalProperties, taskId);
					if (res == null && cancellationToken.IsCancellationRequested) {
						MSBuildTargetResult err = new MSBuildTargetResult (file, false, "", "", file, 1, 1, 1, 1, "Build cancelled", "");
						return new MSBuildResult (new [] { err });
					}
					if (res == null)
						throw new Exception ("Unknown failure");
					return res;
				} catch (Exception ex) {
					CheckDisconnected ();
					LoggingService.LogError ("RunTarget failed", ex);
					MSBuildTargetResult err = new MSBuildTargetResult (file, false, "", "", file, 1, 1, 1, 1, "Unknown MSBuild failure. Please try building the project again", "");
					MSBuildResult res = new MSBuildResult (new [] { err });
					return res;
				} finally {
					EndOperation ();
				}
			});

			// Dispose the cancel registration
			t.ContinueWith (r => cr.Dispose ());

			return t;
		}

		public async Task<string[]> ResolveAssemblyReferences (ProjectConfigurationInfo[] configurations, CancellationToken cancellationToken)
		{
			string[] refs = null;
			var id = configurations [0].Configuration + "|" + configurations [0].Platform;

			using (await referenceCacheLock.EnterAsync ()) {
				// Check the cache before starting the task
				if (referenceCache.TryGetValue (id, out refs))
					return refs;
			}

			// Get an id for the task, it will be used later on to cancel the task if necessary
			var taskId = Interlocked.Increment (ref lastTaskId);
			IDisposable cr = null;

			refs = await Task.Run (() => {
				using (referenceCacheLock.Enter ()) {
					// Check again the cache, maybe the value was set while the task was starting
					if (referenceCache.TryGetValue (id, out refs))
						return refs;

					// Get ready to cancel the task if the cancellation token is signalled
					cr = RegisterCancellation (cancellationToken, taskId);

					MSBuildResult result;
					try {
						BeginOperation ();
						lock (engine) {
							// FIXME: This lock should not be necessary, but remoting seems to have problems when doing many concurrent calls.
							result = builder.Run (
										configurations, null, MSBuildVerbosity.Normal,
										new [] { "ResolveAssemblyReferences" }, new [] { "ReferencePath" }, null, null, taskId
									);
						}
					} catch (Exception ex) {
						CheckDisconnected ();
						LoggingService.LogError ("ResolveAssemblyReferences failed", ex);
						return new string [0];
					} finally {
						EndOperation ();
					}

					List<MSBuildEvaluatedItem> items;
					if (result.Items.TryGetValue ("ReferencePath", out items) && items != null) {
						refs = items.Select (i => i.ItemSpec).ToArray ();
					} else
						refs = new string[0];

					referenceCache [id] = refs;
				}
				return refs;
			});

			// Dispose the cancel registration
			if (cr != null)
				cr.Dispose ();
			
			return refs;
		}

		public async Task Refresh ()
		{
			using (await referenceCacheLock.EnterAsync ())
				referenceCache.Clear ();

			await Task.Run (() => {
				try {
					BeginOperation ();
					builder.Refresh ();
				} catch (Exception ex) {
					LoggingService.LogError ("MSBuild refresh failed", ex);
					CheckDisconnected ();
				} finally {
					EndOperation ();
				}
			});
		}
		
		public async Task RefreshWithContent (string projectContent)
		{
			using (await referenceCacheLock.EnterAsync ())
				referenceCache.Clear ();

			await Task.Run (() => {
				try {
					BeginOperation ();
					builder.RefreshWithContent (projectContent);
				} catch (Exception ex) {
					LoggingService.LogError ("MSBuild refresh failed", ex);
					CheckDisconnected ();
				} finally {
					EndOperation ();
				}
			});
		}

		public void Dispose ()
		{
			if (!MSBuildProjectService.ShutDown && engine != null) {
				try {
					if (builder != null)
						engine.UnloadProject (builder);
					MSBuildProjectService.ReleaseProjectBuilder (engine);
				} catch {
					// Ignore
				}
				GC.SuppressFinalize (this);
				engine = null;
				builder = null;
			}
		}
		
		~RemoteProjectBuilder ()
		{
			// Using the logging service when shutting down MD can cause exceptions
			Console.WriteLine ("RemoteProjectBuilder not disposed");
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
