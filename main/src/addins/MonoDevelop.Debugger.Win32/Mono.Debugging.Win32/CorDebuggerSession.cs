using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorSymbolStore;
using Microsoft.Samples.Debugging.Extensions;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using System.Linq;

namespace Mono.Debugging.Win32
{
	public class CorDebuggerSession: DebuggerSession
	{
		readonly char[] badPathChars;
		readonly object debugLock = new object ();
		readonly object terminateLock = new object ();

		CorDebugger dbg;
		CorProcess process;
		CorThread activeThread;
		CorStepper stepper;
		bool terminated;
		bool evaluating;
		bool autoStepInto;
		bool stepInsideDebuggerHidden=false;
		int processId;
		bool attaching = false;

		static int evaluationTimestamp;

		readonly SymbolBinder symbolBinder = MtaThread.Run (() => new SymbolBinder ());
		readonly object appDomainsLock = new object ();

		Dictionary<int, AppDomainInfo> appDomains = new Dictionary<int, AppDomainInfo> ();
		Dictionary<int, ProcessInfo> processes = new Dictionary<int, ProcessInfo> ();
		Dictionary<int, ThreadInfo> threads = new Dictionary<int,ThreadInfo> ();
		readonly Dictionary<CorBreakpoint, BreakEventInfo> breakpoints = new Dictionary<CorBreakpoint, BreakEventInfo> ();
		readonly Dictionary<long, CorHandleValue> handles = new Dictionary<long, CorHandleValue>();

		readonly BlockingCollection<Action> helperOperationsQueue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
		readonly CancellationTokenSource helperOperationsCancellationTokenSource = new CancellationTokenSource ();

		public CorObjectAdaptor ObjectAdapter = new CorObjectAdaptor();

		class AppDomainInfo
		{
			public CorAppDomain AppDomain;
			public Dictionary<string, DocInfo> Documents;
			public Dictionary<string, ModuleInfo> Modules;
		}

		class DocInfo
		{
			public ISymbolDocument Document;
			public ModuleInfo ModuleInfo;
		}

		class ModuleInfo
		{
			public ISymbolReader Reader;
			public CorModule Module;
			public CorMetadataImport Importer;
		}

		public CorDebuggerSession(char[] badPathChars)
		{
			this.badPathChars = badPathChars;
			var cancellationToken = helperOperationsCancellationTokenSource.Token;
			new Thread (() => {
				try {
					while (!cancellationToken.IsCancellationRequested) {
						var action = helperOperationsQueue.Take(cancellationToken);
						try {
							action ();
						}
						catch (Exception e) {
							DebuggerLoggingService.LogError ("Exception on processing helper thread action", e);
						}
					}

				}
				catch (Exception e) {
					if (e is OperationCanceledException || e is ObjectDisposedException) {
						DebuggerLoggingService.LogMessage ("Helper thread was gracefully interrupted");
					}
					else {
						DebuggerLoggingService.LogError ("Unhandled exception in helper thread. Helper thread is terminated", e);
					}
				}
			}) {Name = "CorDebug helper thread "}.Start();
		}

		public new IDebuggerSessionFrontend Frontend {
			get { return base.Frontend; }
		}

		public static int EvaluationTimestamp {
			get { return evaluationTimestamp; }
		}


		public override void Dispose ( )
		{
			MtaThread.Run (delegate
			{
				TerminateDebugger ();
				ObjectAdapter.Dispose();
			});
			helperOperationsCancellationTokenSource.Dispose ();
			helperOperationsQueue.Dispose ();

			base.Dispose ();

			// There is no explicit way of disposing the metadata objects, so we have
			// to rely on the GC to do it.

			lock (appDomainsLock) {
				foreach (var appDomainInfo in appDomains) {
					foreach (var module in appDomainInfo.Value.Modules.Values) {
						var disposable = module.Reader as IDisposable;
						if (disposable != null)
							disposable.Dispose ();
					}
				}
				appDomains = null;
			}

			threads = null;
			processes = null;
			activeThread = null;

			ThreadPool.QueueUserWorkItem (delegate {
				Thread.Sleep (2000);
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
				Thread.Sleep (20000);
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			});
		}

		void QueueToHelperThread (Action action)
		{
			helperOperationsQueue.Add (action);
		}

		void TerminateDebugger ()
		{
			helperOperationsCancellationTokenSource.Cancel();
			Breakpoints.Clear ();
			lock (terminateLock) {
				if (terminated)
					return;

				terminated = true;

				if (process != null) {
					// Process already running. Stop it. In the ProcessExited event the
					// debugger engine will be terminated
					try {
						process.Stop (0);
						if (attaching) {
							process.Detach ();
						}
						else {
							process.Terminate (1);
						}
					}
					catch (COMException e) {
						// process was terminated, but debugger operation thread doesn't call ProcessExit callback at the time,
						// so we just think that the process is alive but that's wrong.
						// This may happen when e.g. when target process exited and Dispose was called at the same time
						// rethrow the exception in other case
						if (e.ErrorCode != (int) HResult.CORDBG_E_PROCESS_TERMINATED) {
							throw;
						}
					}
				}
			}
		}

		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			MtaThread.Run (delegate
			{
				// Create the debugger

				string dversion;
				try {
					dversion = CorDebugger.GetDebuggerVersionFromFile (startInfo.Command);
				}
				catch {
					dversion = CorDebugger.GetDefaultDebuggerVersion ();
				}
				dbg = new CorDebugger (dversion);

				Dictionary<string, string> env = new Dictionary<string, string> ();
				foreach (DictionaryEntry de in Environment.GetEnvironmentVariables ())
					env[(string)de.Key] = (string)de.Value;

				foreach (KeyValuePair<string, string> var in startInfo.EnvironmentVariables)
					env[var.Key] = var.Value;

				// The second parameter of CreateProcess is the command line, and it includes the application being launched
				string cmdLine = "\"" + startInfo.Command + "\" " + startInfo.Arguments;

				int flags = 0;
				if (!startInfo.UseExternalConsole) {
					flags = (int)CreationFlags.CREATE_NO_WINDOW;
						flags |= DebuggerExtensions.CREATE_REDIRECT_STD;
				}

				var dir = startInfo.WorkingDirectory;
				if (string.IsNullOrEmpty (dir))
					dir = System.IO.Path.GetDirectoryName (startInfo.Command);

				process = dbg.CreateProcess (startInfo.Command, cmdLine, dir, env, flags);
				processId = process.Id;
				SetupProcess (process);
				process.Continue (false);
			});
			OnStarted ();
		}

		void SetupProcess (CorProcess corProcess)
		{
			processId = corProcess.Id;
			corProcess.OnCreateProcess += OnCreateProcess;
			corProcess.OnCreateAppDomain += OnCreateAppDomain;
			corProcess.OnAppDomainExit += OnAppDomainExit;
			corProcess.OnAssemblyLoad += OnAssemblyLoad;
			corProcess.OnAssemblyUnload += OnAssemblyUnload;
			corProcess.OnCreateThread += OnCreateThread;
			corProcess.OnThreadExit += OnThreadExit;
			corProcess.OnModuleLoad += OnModuleLoad;
			corProcess.OnModuleUnload += OnModuleUnload;
			corProcess.OnProcessExit += OnProcessExit;
			corProcess.OnUpdateModuleSymbols += OnUpdateModuleSymbols;
			corProcess.OnDebuggerError += OnDebuggerError;
			corProcess.OnBreakpoint += OnBreakpoint;
			corProcess.OnStepComplete += OnStepComplete;
			corProcess.OnBreak += OnBreak;
			corProcess.OnNameChange += OnNameChange;
			corProcess.OnEvalComplete += OnEvalComplete;
			corProcess.OnEvalException += OnEvalException;
			corProcess.OnLogMessage += OnLogMessage;
			corProcess.OnException2 += OnException2;
			corProcess.RegisterStdOutput (OnStdOutput);
		}

		void OnStdOutput (object sender, CorTargetOutputEventArgs e)
		{
			OnTargetOutput (e.IsStdError, e.Text);
		}

		void OnLogMessage (object sender, CorLogMessageEventArgs e)
		{
			OnTargetDebug (e.Level, e.LogSwitchName, e.Message);
			e.Continue = true;
		}

		void OnEvalException (object sender, CorEvalEventArgs e)
		{
			evaluationTimestamp++;
		}

		void OnEvalComplete (object sender, CorEvalEventArgs e)
		{
			evaluationTimestamp++;
		}

		void OnNameChange (object sender, CorThreadEventArgs e)
		{
		}

		void OnStopped ( )
		{
			evaluationTimestamp++;
			lock (threads) {
				threads.Clear ();
			}
		}

		void OnBreak (object sender, CorThreadEventArgs e)
		{
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}
			OnStopped ();
			e.Continue = false;
			SetActiveThread (e.Thread);
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetInterrupted);
			args.Process = GetProcess (process);
			args.Thread = GetThread (e.Thread);
			args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
			OnTargetEvent (args);
		}

		bool StepThrough (MethodInfo methodInfo)
		{
			var m = methodInfo.GetCustomAttributes (true);
			if (Options.ProjectAssembliesOnly) {
				return methodInfo.GetCustomAttributes (true).Union (methodInfo.DeclaringType.GetCustomAttributes (true)).Any (v =>
					v is System.Diagnostics.DebuggerHiddenAttribute ||
					v is System.Diagnostics.DebuggerStepThroughAttribute ||
					v is System.Diagnostics.DebuggerNonUserCodeAttribute);
			} else {
				return methodInfo.GetCustomAttributes (true).Union (methodInfo.DeclaringType.GetCustomAttributes (true)).Any (v =>
					v is System.Diagnostics.DebuggerHiddenAttribute ||
					v is System.Diagnostics.DebuggerStepThroughAttribute);
			}
		}

		bool ContinueOnStepIn(MethodInfo methodInfo)
		{
			return methodInfo.GetCustomAttributes (true).Any (v => v is System.Diagnostics.DebuggerStepperBoundaryAttribute);
		}

		static bool IsPropertyOrOperatorMethod (MethodInfo method)
		{
			if (method == null)
				return false;
			string name = method.Name;

			return method.IsSpecialName &&
			(name.StartsWith ("get_", StringComparison.Ordinal) ||
			name.StartsWith ("set_", StringComparison.Ordinal) ||
			name.StartsWith ("op_", StringComparison.Ordinal));
		}

		static bool IsCompilerGenerated (MethodInfo method)
		{
			return method.GetCustomAttributes (true).Any (v => v is System.Runtime.CompilerServices.CompilerGeneratedAttribute);
		}

		void OnStepComplete (object sender, CorStepCompleteEventArgs e)
		{
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}

			bool localAutoStepInto = autoStepInto;
			autoStepInto = false;
			bool localStepInsideDebuggerHidden = stepInsideDebuggerHidden;
			stepInsideDebuggerHidden = false;

			if (e.AppDomain.Process.HasQueuedCallbacks (e.Thread)) {
				e.Continue = true;
				return;
			}

			if (localAutoStepInto) {
				Step (true);
				e.Continue = true;
				return;
			}

			if (ContinueOnStepIn (e.Thread.ActiveFrame.Function.GetMethodInfo (this))) {
				e.Continue = true;
				return;
			}

			var currentSequence = CorBacktrace.GetSequencePoint (this, e.Thread.ActiveFrame);
			if (currentSequence == null) {
				stepper.StepOut ();
				autoStepInto = true;
				e.Continue = true;
				return;
			}

			if (StepThrough (e.Thread.ActiveFrame.Function.GetMethodInfo (this))) {
				stepInsideDebuggerHidden = e.StepReason == CorDebugStepReason.STEP_CALL;
				RawContinue (true, true);
				e.Continue = true;
				return;
			}

			if ((Options.StepOverPropertiesAndOperators || IsCompilerGenerated(e.Thread.ActiveFrame.Function.GetMethodInfo (this))) &&
			    IsPropertyOrOperatorMethod (e.Thread.ActiveFrame.Function.GetMethodInfo (this)) &&
				e.StepReason == CorDebugStepReason.STEP_CALL) {
				stepper.StepOut ();
				autoStepInto = true;
				e.Continue = true;
				return;
			}

			if (currentSequence.IsSpecial) {
				Step (false);
				e.Continue = true;
				return;
			}

			if (localStepInsideDebuggerHidden && e.StepReason == CorDebugStepReason.STEP_RETURN) {
				Step (true);
				e.Continue = true;
				return;
			}

			OnStopped ();
			e.Continue = false;
			SetActiveThread (e.Thread);
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetStopped);
			args.Process = GetProcess (process);
			args.Thread = GetThread (e.Thread);
			args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
			OnTargetEvent (args);
		}

		void OnThreadExit (object sender, CorThreadEventArgs e)
		{
			lock (threads) {
				threads.Remove (e.Thread.Id);
			}
		}

		void OnBreakpoint (object sender, CorBreakpointEventArgs e)
		{
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}

			// we have to stop an execution and enqueue breakpoint calculations on another thread to release debugger event thread for further events
			// we can't perform any evaluations inside this handler, because the debugger thread is busy and we won't get evaluation events there
			e.Continue = false;

			QueueToHelperThread (() => {
				BreakEventInfo binfo;
				BreakEvent breakEvent = null;
				if (e.Controller.IsRunning ())
					throw new InvalidOperationException ("Debuggee isn't stopped to perform breakpoint calculations");

				var shouldContinue = false;
				if (breakpoints.TryGetValue (e.Breakpoint, out binfo)) {
					breakEvent = (Breakpoint) binfo.BreakEvent;
					try {
						shouldContinue = ShouldContinueOnBreakpoint (e.Thread, binfo);
					}
					catch (Exception ex) {
						DebuggerLoggingService.LogError ("ShouldContinueOnBreakpoint() has thrown an exception", ex);
					}
				}

				if (shouldContinue || e.AppDomain.Process.HasQueuedCallbacks (e.Thread)) {
					e.Controller.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, null);
					e.Controller.Continue (false);
					return;
				}

				OnStopped ();
				// If a breakpoint is hit while stepping, cancel the stepping operation
				if (stepper != null && stepper.IsActive ())
					stepper.Deactivate ();
				autoStepInto = false;
				SetActiveThread (e.Thread);
				var args = new TargetEventArgs (TargetEventType.TargetHitBreakpoint) {
					Process = GetProcess (process),
					Thread = GetThread (e.Thread),
					Backtrace = new Backtrace (new CorBacktrace (e.Thread, this)),
					BreakEvent = breakEvent
				};
				OnTargetEvent (args);
			});
		}

		bool ShouldContinueOnBreakpoint (CorThread thread, BreakEventInfo binfo)
		{
			var bp = (Breakpoint) binfo.BreakEvent;
			binfo.IncrementHitCount();
			if (!binfo.HitCountReached)
				return true;

			if (!string.IsNullOrEmpty (bp.ConditionExpression)) {
				string res = EvaluateExpression (thread, bp.ConditionExpression);
				if (bp.BreakIfConditionChanges) {
					if (res == bp.LastConditionValue)
						return true;
					bp.LastConditionValue = res;
				} else {
					if (res != null && res.ToLower () != "true")
						return true;
				}
			}

			if ((bp.HitAction & HitAction.CustomAction) != HitAction.None) {
				// If custom action returns true, execution must continue
				if (binfo.RunCustomBreakpointAction (bp.CustomActionId))
					return true;
			}

			if ((bp.HitAction & HitAction.PrintTrace) != HitAction.None) {
				OnTargetDebug (0, "", "Breakpoint reached: " + bp.FileName + ":" + bp.Line + Environment.NewLine);
			}

			if ((bp.HitAction & HitAction.PrintExpression) != HitAction.None) {
				string exp = EvaluateTrace (thread, bp.TraceExpression);
				binfo.UpdateLastTraceValue (exp);
			}

			return (bp.HitAction & HitAction.Break) == HitAction.None;
		}

		void OnDebuggerError (object sender, CorDebuggerErrorEventArgs e)
		{
			Exception ex = Marshal.GetExceptionForHR (e.HResult);
			OnDebuggerOutput (true, string.Format ("Debugger Error: {0}\n", ex.Message));
		}

		void OnUpdateModuleSymbols (object sender, CorUpdateModuleSymbolsEventArgs e)
		{
			e.Continue = true;
		}

		void OnProcessExit (object sender, CorProcessEventArgs e)
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetExited);

			// If the main thread stopped, terminate the debugger session
			if (e.Process.Id == process.Id) {
				lock (terminateLock) {
					process.Dispose ();
					process = null;
					ThreadPool.QueueUserWorkItem (delegate
					{
						// The Terminate call will fail if called in the event handler
						dbg.Terminate ();
						dbg = null;
						GC.Collect ();
					});
				}
			}

			OnTargetEvent (args);
		}

		void OnAssemblyUnload (object sender, CorAssemblyEventArgs e)
		{
			OnDebuggerOutput (false, string.Format ("Unloaded Module '{0}'\n", e.Assembly.Name));
			e.Continue = true;
		}

		void OnModuleLoad (object sender, CorModuleEventArgs e)
		{
			var currentModule = e.Module;
			CorMetadataImport mi = new CorMetadataImport (currentModule);

			try {
				// Required to avoid the jit to get rid of variables too early
				currentModule.JITCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
			}
			catch {
				// Some kind of modules don't allow JIT flags to be changed.
			}

			var currentDomain = e.AppDomain;
			OnDebuggerOutput (false, String.Format("Loading module {0} in application domain {1}:{2}\n", currentModule.Name, currentDomain.Id, currentDomain.Name));
			string file = currentModule.Assembly.Name;
			var newDocuments = new Dictionary<string, DocInfo> ();
			var justMyCode = false;
			ISymbolReader reader = null;
			if (file.IndexOfAny (badPathChars) == -1) {
				try {
					reader = symbolBinder.GetReaderForFile (mi.RawCOMObject, file, ".",
						SymSearchPolicies.AllowOriginalPathAccess | SymSearchPolicies.AllowReferencePathAccess);
					if (reader != null) {
						OnDebuggerOutput (false, string.Format ("Symbols for module {0} loaded\n", file));
						// set JMC to true only when we got the reader.
						// When module JMC is true, debugger will step into it
						justMyCode = true;
						foreach (ISymbolDocument doc in reader.GetDocuments ()) {
							if (string.IsNullOrEmpty (doc.URL))
								continue;
							string docFile = System.IO.Path.GetFullPath (doc.URL);
							DocInfo di = new DocInfo ();
							di.Document = doc;
							newDocuments[docFile] = di;
						}
					}
				}
				catch (COMException ex) {
					var hResult = ex.ToHResult<PdbHResult> ();
					if (hResult != null) {
						if (hResult != PdbHResult.E_PDB_OK) {
							OnDebuggerOutput (false, string.Format ("Failed to load pdb for assembly {0}. Error code {1}(0x{2:X})\n", file, hResult, ex.ErrorCode));
						}
					}
					else {
						DebuggerLoggingService.LogError (string.Format ("Loading symbols of module {0} failed", e.Module.Name), ex);
					}
				}
				catch (Exception ex) {
					DebuggerLoggingService.LogError (string.Format ("Loading symbols of module {0} failed", e.Module.Name), ex);
				}
			}
			try {
				currentModule.SetJmcStatus (justMyCode, null);
			}
			catch (COMException ex) {
				// somewhen exceptions is thrown
				DebuggerLoggingService.LogMessage ("Exception during setting JMC: {0}", ex.Message);
			}

			lock (appDomainsLock) {
				AppDomainInfo appDomainInfo;
				if (!appDomains.TryGetValue (currentDomain.Id, out appDomainInfo)) {
				  DebuggerLoggingService.LogMessage ("OnCreatedAppDomain was not fired for domain {0} (id {1})", currentDomain.Name, currentDomain.Id);
					appDomainInfo = new AppDomainInfo {
						AppDomain = currentDomain,
						Documents = new Dictionary<string, DocInfo> (StringComparer.InvariantCultureIgnoreCase),
						Modules = new Dictionary<string, ModuleInfo> (StringComparer.InvariantCultureIgnoreCase)
					};
					appDomains[currentDomain.Id] = appDomainInfo;
				}
				var modules = appDomainInfo.Modules;
				if (modules.ContainsKey (currentModule.Name)) {
				  DebuggerLoggingService.LogMessage ("Module {0} was already added for app domain {1} (id {2}). Replacing\n",
						currentModule.Name, currentDomain.Name, currentDomain.Id);
				}
				var newModuleInfo = new ModuleInfo {
					Module = currentModule,
					Reader = reader,
					Importer = mi,
				};
				modules[currentModule.Name] = newModuleInfo;
				var existingDocuments = appDomainInfo.Documents;
				foreach (var newDocument in newDocuments) {
					var documentFile = newDocument.Key;
					var newDocInfo = newDocument.Value;
					if (existingDocuments.ContainsKey (documentFile)) {
					  DebuggerLoggingService.LogMessage ("Document {0} was already added for module {1} in domain {2} (id {3}). Replacing\n",
							documentFile, currentModule.Name, currentDomain.Name, currentDomain.Id);
					}
					newDocInfo.ModuleInfo = newModuleInfo;
					existingDocuments[documentFile] = newDocInfo;
				}

			}

			foreach (var newFile in newDocuments.Keys) {
				BindSourceFileBreakpoints (newFile);
			}

			e.Continue = true;
		}

		void OnModuleUnload (object sender, CorModuleEventArgs e)
		{
			var currentDomain = e.AppDomain;
			var currentModule = e.Module;
			var documentsToRemove = new List<string> ();
			lock (appDomainsLock) {
				AppDomainInfo appDomainInfo;
				if (!appDomains.TryGetValue (currentDomain.Id, out appDomainInfo)) {
				  DebuggerLoggingService.LogMessage ("Failed unload module {0} for app domain {1} (id {2}) because app domain was not found or already unloaded\n",
							currentModule.Name, currentDomain.Name, currentDomain.Id);
					return;
				}
				ModuleInfo moi;
				if (!appDomainInfo.Modules.TryGetValue (currentModule.Name, out moi)) {
				  DebuggerLoggingService.LogMessage ("Failed unload module {0} for app domain {1} (id {2}) because the module was not found or already unloaded\n",
						currentModule.Name, currentDomain.Name, currentDomain.Id);
				}
				else {
					appDomainInfo.Modules.Remove (currentModule.Name);
					var disposableReader = moi.Reader as IDisposable;
					if (disposableReader != null)
						disposableReader.Dispose ();
				}

				foreach (var docInfo in appDomainInfo.Documents) {
					if (docInfo.Value.ModuleInfo.Module.Name == currentModule.Name)
						documentsToRemove.Add (docInfo.Key);
				}
				foreach (var file in documentsToRemove) {
					appDomainInfo.Documents.Remove (file);
				}
			}
			foreach (var file in documentsToRemove) {
				UnbindSourceFileBreakpoints (file);
			}
		}

		void OnCreateAppDomain (object sender, CorAppDomainEventArgs e)
		{
			var appDomainId = e.AppDomain.Id;
			lock (appDomainsLock) {
				if (!appDomains.ContainsKey (appDomainId)) {
					appDomains[appDomainId] = new AppDomainInfo {
						AppDomain = e.AppDomain,
						Documents = new Dictionary<string, DocInfo> (StringComparer.InvariantCultureIgnoreCase),
						Modules = new Dictionary<string, ModuleInfo> (StringComparer.InvariantCultureIgnoreCase)
					};
				}
				else {
					DebuggerLoggingService.LogMessage ("App domain {0} (id {1}) was already loaded", e.AppDomain.Name, appDomainId);
				}
			}
			e.AppDomain.Attach();
			e.Continue = true;
			OnDebuggerOutput (false, string.Format("Loaded application domain '{0} (id {1})'\n", e.AppDomain.Name, appDomainId));
		}

		private void OnAppDomainExit (object sender, CorAppDomainEventArgs e)
		{
			var appDomainId = e.AppDomain.Id;
			lock (appDomainsLock) {
				if (!appDomains.Remove (appDomainId)) {
				  DebuggerLoggingService.LogMessage ("Failed to unload app domain {0} (id {1}) because it's not found in map. Possibly already unloaded.", e.AppDomain.Name, appDomainId);
				}
			}
			// Detach is not implemented for ICorDebugAppDomain, it's valid only for ICorDebugProcess
			//e.AppDomain.Detach ();
			e.Continue = true;
			OnDebuggerOutput (false, string.Format("Unloaded application domain '{0} (id {1})'\n", e.AppDomain.Name, appDomainId));
		}


		void OnCreateProcess (object sender, CorProcessEventArgs e)
		{
			if (!attaching) {
				// Required to avoid the jit to get rid of variables too early
				// not allowed in attach mode
				e.Process.DesiredNGENCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
			}
			e.Process.EnableLogMessages (true);
			e.Continue = true;
		}
		void OnCreateThread (object sender, CorThreadEventArgs e)
		{
			OnDebuggerOutput (false, string.Format ("Started Thread {0}\n", e.Thread.Id));
			e.Continue = true;
		}

		void OnAssemblyLoad (object sender, CorAssemblyEventArgs e)
		{
			OnDebuggerOutput (false, string.Format ("Loaded Assembly '{0}'\n", e.Assembly.Name));
			e.Continue = true;
		}
		
		void OnException2 (object sender, CorException2EventArgs e)
		{
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}
			
			TargetEventArgs args = null;
			
			switch (e.EventType) {
				case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_FIRST_CHANCE:
					if (!this.Options.ProjectAssembliesOnly && IsCatchpoint (e))
						args = new TargetEventArgs (TargetEventType.ExceptionThrown);
					break;
				case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_USER_FIRST_CHANCE:
					if (IsCatchpoint (e))
						args = new TargetEventArgs (TargetEventType.ExceptionThrown);
					break;
				case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_CATCH_HANDLER_FOUND:
					break;
				case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED:
					args = new TargetEventArgs (TargetEventType.UnhandledException);
					break;
			}
			
			if (args != null) {
				OnStopped ();
				e.Continue = false;
				// If an exception is thrown while stepping, cancel the stepping operation
				if (stepper != null && stepper.IsActive ())
					stepper.Deactivate ();
				autoStepInto = false;
				SetActiveThread (e.Thread);
				
				args.Process = GetProcess (process);
				args.Thread = GetThread (e.Thread);
				args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
				OnTargetEvent (args);	
			}
		}

		public bool IsExternalCode (string fileName)
		{
			if (string.IsNullOrWhiteSpace (fileName))
				return true;
			lock (appDomainsLock) {
				foreach (var appDomainInfo in appDomains) {
					if (appDomainInfo.Value.Documents.ContainsKey (fileName))
						return false;
				}
			}
			return true;
		}

		private bool IsCatchpoint (CorException2EventArgs e)
		{
			// Build up the exception type hierachy
			CorValue v = e.Thread.CurrentException;
			List<string> exceptions = new List<string>();
			CorType t = v.ExactType;
			while (t != null) {
				exceptions.Add(t.GetTypeInfo(this).FullName);
				t = t.Base;
			}
			if (exceptions.Count == 0)
				return false;
			// See if a catchpoint is set for this exception.
			foreach (Catchpoint cp in Breakpoints.GetCatchpoints()) {
				if (cp.Enabled &&
				    ((cp.IncludeSubclasses && exceptions.Contains (cp.ExceptionName)) ||
				    (exceptions [0] == cp.ExceptionName))) {
					return true;
				}
			}
			
			return false;
		}

		protected override void OnAttachToProcess(long procId)
		{
			attaching = true;
			MtaThread.Run(delegate
			{
				var version = CorDebugger.GetProcessLoadedRuntimes((int)procId);
				if (!version.Any())
					throw new InvalidOperationException(string.Format("Process {0} doesn't have .NET loaded runtimes", procId));
				dbg = new CorDebugger(version.Last());
				process = dbg.DebugActiveProcess((int)procId, false);
				SetupProcess(process);
				process.Continue(false);
			});
			OnStarted();
		}

		protected override void OnContinue ( )
		{
			MtaThread.Run (delegate
			{
				ClearEvalStatus ();
				ClearHandles ();
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, null);
				process.Continue (false);
			});
		}

		protected override void OnDetach ( )
		{
			MtaThread.Run (delegate
			{
				TerminateDebugger ();
			});
		}

		protected override void OnEnableBreakEvent (BreakEventInfo binfo, bool enable)
		{
			MtaThread.Run (delegate
			{
				CorBreakpoint bp = binfo.Handle as CorFunctionBreakpoint;
				if (bp != null) {
					try {
						bp.Activate (enable);
					}
					catch (COMException e) {
						HandleBreakpointException (binfo, e);
					}
				}
			});
		}

		protected override void OnExit ( )
		{
			MtaThread.Run (delegate
			{
				TerminateDebugger ();
			});
		}

		protected override void OnFinish ( )
		{
			MtaThread.Run (delegate
			{
				if (stepper != null) {
					stepper.StepOut ();
					ClearEvalStatus ();
					process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, null);
					process.Continue (false);
				}
			});
		}

		protected override ProcessInfo[] OnGetProcesses ( )
		{
			return MtaThread.Run (() => new ProcessInfo[] { GetProcess (process) });
		}

		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			return MtaThread.Run (delegate
			{
				foreach (CorThread t in process.Threads) {
					if (t.Id == threadId) {
						return new Backtrace (new CorBacktrace (t, this));
					}
				}
				return null;
			});
		}

		protected override ThreadInfo[] OnGetThreads (long processId)
		{
			return MtaThread.Run (delegate
			{
				List<ThreadInfo> list = new List<ThreadInfo> ();
				foreach (CorThread t in process.Threads)
					list.Add (GetThread (t));
				return list.ToArray ();
			});
		}

		internal ISymbolReader GetReaderForModule (CorModule module)
		{
			lock (appDomainsLock) {
				AppDomainInfo appDomainInfo;
				if (!appDomains.TryGetValue (module.Assembly.AppDomain.Id, out appDomainInfo))
					return null;
				ModuleInfo moduleInfo;
				if (!appDomainInfo.Modules.TryGetValue (module.Name, out moduleInfo))
					return null;
				return moduleInfo.Reader;
			}
		}

		internal CorMetadataImport GetMetadataForModule (CorModule module)
		{
			lock (appDomainsLock) {
				AppDomainInfo appDomainInfo;
				if (!appDomains.TryGetValue (module.Assembly.AppDomain.Id, out appDomainInfo))
					return null;
				ModuleInfo mod;
				if (!appDomainInfo.Modules.TryGetValue (module.Name, out mod))
					return null;
				return mod.Importer;
			}
		}


		internal IEnumerable<CorAppDomain> GetAppDomains ()
		{
			lock (appDomainsLock) {
				var corAppDomains = new List<CorAppDomain> (appDomains.Count);
				foreach (var appDomainInfo in appDomains) {
					corAppDomains.Add (appDomainInfo.Value.AppDomain);
				}
				return corAppDomains;
			}
		}

		internal IEnumerable<CorModule> GetModules (CorAppDomain appDomain)
		{
			lock (appDomainsLock) {
				List<CorModule> mods = new List<CorModule> ();
				AppDomainInfo appDomainInfo;
				if (appDomains.TryGetValue (appDomain.Id, out appDomainInfo)) {
					foreach (ModuleInfo mod in appDomainInfo.Modules.Values) {
						mods.Add (mod.Module);
					}
				}
				return mods;
			}
		}

		internal IEnumerable<CorModule> GetAllModules ()
		{
			lock (appDomainsLock) {
				var corModules = new List<CorModule> ();
				foreach (var appDomainInfo in appDomains) {
					corModules.AddRange (GetModules (appDomainInfo.Value.AppDomain));
				}
				return corModules;
			}
		}

		internal CorHandleValue GetHandle (CorValue val)
		{
			CorHandleValue handleVal = null;
			if (!handles.TryGetValue (val.Address, out handleVal)) {
				handleVal = val.CastToHandleValue ();
				if (handleVal == null)
				{
					// Create a handle
					CorReferenceValue refVal = val.CastToReferenceValue ();
					CorHeapValue heapVal = refVal.Dereference ().CastToHeapValue ();
					handleVal = heapVal.CreateHandle (CorDebugHandleType.HANDLE_STRONG);
				}
				handles.Add (val.Address, handleVal);	
			}
			return handleVal;
		}

		protected override BreakEventInfo OnInsertBreakEvent (BreakEvent be)
		{
			return MtaThread.Run (delegate {
				var binfo = new BreakEventInfo ();
				var bp = be as Breakpoint;
				if (bp != null) {
					if (bp is FunctionBreakpoint) {
						// FIXME: implement breaking on function name
						binfo.SetStatus (BreakEventStatus.Invalid, "Function breakpoint is not implemented");
						return binfo;
					} else {
						DocInfo doc = null;
						lock (appDomainsLock) {
							foreach (var appDomainInfo in appDomains) {
								var documents = appDomainInfo.Value.Documents;
								if (documents.TryGetValue (Path.GetFullPath (bp.FileName), out doc)) {
									break;
								}
							}
						}
						if (doc == null) {
							binfo.SetStatus (BreakEventStatus.NotBound, string.Format("{0} is not found among the loaded symbol documents", bp.FileName));
							return binfo;
						}
						int line;
						try {
							line = doc.Document.FindClosestLine (bp.Line);
						} catch {
							// Invalid line
							binfo.SetStatus (BreakEventStatus.Invalid, string.Format("Invalid line {0}", bp.Line));
							return binfo;
						}
						ISymbolMethod met = null;
						if (doc.ModuleInfo.Reader is ISymbolReader2) {
							var methods = ((ISymbolReader2)doc.ModuleInfo.Reader).GetMethodsFromDocumentPosition (doc.Document, line, 0);
							if (methods != null && methods.Any ()) {
								if (methods.Count () == 1) {
									met = methods [0];
								} else {
									int deepest = -1;
									foreach (var method in methods) {
										var firstSequence = method.GetSequencePoints ().FirstOrDefault ((sp) => sp.StartLine != 0xfeefee);
										if (firstSequence != null && firstSequence.StartLine >= deepest) {
											deepest = firstSequence.StartLine;
											met = method;
										}
									}
								}
							}
						}
						if (met == null) {
							met = doc.ModuleInfo.Reader.GetMethodFromDocumentPosition (doc.Document, line, 0);
						}
						if (met == null) {
							binfo.SetStatus (BreakEventStatus.Invalid, "Unable to resolve method at position");
							return binfo;
						}

						int offset = -1;
						int firstSpInLine = -1;
						foreach (SequencePoint sp in met.GetSequencePoints ()) {
							if (sp.IsInside (doc.Document.URL, line, bp.Column)) {
								offset = sp.Offset;
								break;
							} else if (firstSpInLine == -1
									   && sp.StartLine == line
									   && sp.Document.URL.Equals (doc.Document.URL, StringComparison.OrdinalIgnoreCase)) {
								firstSpInLine = sp.Offset;
							}
						}
						if (offset == -1) {//No exact match? Use first match in that line
							offset = firstSpInLine;
						}
						if (offset == -1) {
							binfo.SetStatus (BreakEventStatus.Invalid, "Unable to calculate an offset in IL code");
							return binfo;
						}

						CorFunction func = doc.ModuleInfo.Module.GetFunctionFromToken (met.Token.GetToken ());
						try {
							CorFunctionBreakpoint corBp = func.ILCode.CreateBreakpoint (offset);
							breakpoints[corBp] = binfo;
							binfo.Handle = corBp;
							corBp.Activate (bp.Enabled);
							binfo.SetStatus (BreakEventStatus.Bound, null);
						}
						catch (COMException e) {
							HandleBreakpointException (binfo, e);
						}
						return binfo;
					}
				}

				var cp = be as Catchpoint;
				if (cp != null) {
					var bound = false;
					lock (appDomainsLock) {
						foreach (var appDomainInfo in appDomains) {
							foreach (ModuleInfo mod in appDomainInfo.Value.Modules.Values) {
								CorMetadataImport mi = mod.Importer;
								if (mi != null) {
									foreach (Type t in mi.DefinedTypes)
										if (t.FullName == cp.ExceptionName) {
											bound = true;
										}
								}
							}
						}
					}
					if (bound) {
						binfo.SetStatus (BreakEventStatus.Bound, null);
						return binfo;
					}
				}

				binfo.SetStatus (BreakEventStatus.Invalid, null);
				return binfo;
			});
		}

		private static void HandleBreakpointException (BreakEventInfo binfo, COMException e)
		{
			var code = e.ToHResult<HResult> ();
			if (code != null) {
				switch (code) {
					case HResult.CORDBG_E_UNABLE_TO_SET_BREAKPOINT:
						binfo.SetStatus (BreakEventStatus.Invalid, "Invalid breakpoint position");
						break;
					case HResult.CORDBG_E_PROCESS_TERMINATED:
						binfo.SetStatus (BreakEventStatus.BindError, "Process terminated");
						break;
					case HResult.CORDBG_E_CODE_NOT_AVAILABLE:
						binfo.SetStatus (BreakEventStatus.BindError, "Module is not loaded");
						break;
					default:
						binfo.SetStatus (BreakEventStatus.BindError, e.Message);
						break;
				}
			}
			else {
				binfo.SetStatus (BreakEventStatus.BindError, e.Message);
				DebuggerLoggingService.LogError ("Unknown exception when setting breakpoint", e);
			}
		}

		protected override void OnNextInstruction ( )
		{
			MtaThread.Run (delegate {
				Step (false);
			});
		}

		protected override void OnNextLine ( )
		{
			MtaThread.Run (delegate
			{
				Step (false);
			});
		}

		void Step (bool into)
		{
			try {
				ObjectAdapter.CancelAsyncOperations ();
				if (stepper != null) {
					CorFrame frame = activeThread.ActiveFrame;
					ISymbolReader reader = GetReaderForModule (frame.Function.Module);
					if (reader == null) {
						RawContinue (into);
						return;
					}
					ISymbolMethod met = reader.GetMethod (new SymbolToken (frame.Function.Token));
					if (met == null) {
						RawContinue (into);
						return;
					}

					uint offset;
					CorDebugMappingResult mappingResult;
					frame.GetIP (out offset, out mappingResult);

					// Exclude all ranges belonging to the current line
					List<COR_DEBUG_STEP_RANGE> ranges = new List<COR_DEBUG_STEP_RANGE> ();
					var sequencePoints = met.GetSequencePoints ().ToArray ();
					for (int i = 0; i < sequencePoints.Length; i++) {
						if (sequencePoints [i].Offset > offset) {
							var r = new COR_DEBUG_STEP_RANGE ();
							r.startOffset = i == 0 ? 0 : (uint)sequencePoints [i - 1].Offset;
							r.endOffset = (uint)sequencePoints [i].Offset;
							ranges.Add (r);
							break;
						}
					}
					if (ranges.Count == 0 && sequencePoints.Length > 0) {
						var r = new COR_DEBUG_STEP_RANGE ();
						r.startOffset = (uint)sequencePoints [sequencePoints.Length - 1].Offset;
						r.endOffset = uint.MaxValue;
						ranges.Add (r);
					}

					stepper.StepRange (into, ranges.ToArray ());

					ClearEvalStatus ();
					process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, null);
					process.Continue (false);
				}
			} catch (Exception e) {
				DebuggerLoggingService.LogError ("Exception on Step()", e);
			}
		}

		private void RawContinue (bool into, bool stepOverAll = false)
		{
			if (stepOverAll)
				stepper.StepRange (into, new[]{ new COR_DEBUG_STEP_RANGE (){ startOffset = 0, endOffset = uint.MaxValue } });
			else
				stepper.Step (into);
			ClearEvalStatus ();
			process.Continue (false);
		}

		protected override void OnRemoveBreakEvent (BreakEventInfo bi)
		{
			if (terminated)
				return;
			
			if (bi.Status != BreakEventStatus.Bound || bi.Handle == null)
				return;

			MtaThread.Run (delegate
			{
				CorFunctionBreakpoint corBp = (CorFunctionBreakpoint)bi.Handle;
				try {
					corBp.Activate (false);
				}
				catch (COMException e) {
					HandleBreakpointException (bi, e);
				}
			});
		}


		protected override void OnSetActiveThread (long processId, long threadId)
		{
			MtaThread.Run (delegate
			{
				activeThread = null;
				if (stepper != null && stepper.IsActive ())
					stepper.Deactivate ();
				stepper = null;
				foreach (CorThread t in process.Threads) {
					if (t.Id == threadId) {
						SetActiveThread (t);
						break;
					}
				}
			});
		}

		void SetActiveThread (CorThread t)
		{
			activeThread = t;
			if (stepper != null && stepper.IsActive ()) {
				stepper.Deactivate ();
			}
			stepper = activeThread.CreateStepper (); 
			stepper.SetUnmappedStopMask (CorDebugUnmappedStop.STOP_NONE);
			stepper.SetJmcStatus (true);
		}

		protected override void OnStepInstruction ( )
		{
			MtaThread.Run (delegate {
				Step (true);
			});
		}

		protected override void OnStepLine ( )
		{
			MtaThread.Run (delegate
			{
				Step (true);
			});
		}

		protected override void OnStop ( )
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetStopped);

			MtaThread.Run (delegate
			{
				process.Stop (0);
				OnStopped ();
				CorThread currentThread = null;
				foreach (CorThread t in process.Threads) {
					currentThread = t;
					break;
				}
				args.Process = GetProcess (process);
				args.Thread = GetThread (currentThread);
				args.Backtrace = new Backtrace (new CorBacktrace (currentThread, this));
			});
			OnTargetEvent (args);
		}

		protected override void OnUpdateBreakEvent (BreakEventInfo be)
		{
		}

		public CorValue RuntimeInvoke (CorEvaluationContext ctx, CorFunction function, CorType[] typeArgs, CorValue thisObj, CorValue[] arguments)
		{
			if (!ctx.Thread.ActiveChain.IsManaged)
				throw new EvaluatorException ("Cannot evaluate expression because the thread is stopped in native code.");

			CorValue[] args;
			if (thisObj == null)
				args = arguments;
			else {
				args = new CorValue[arguments.Length + 1];
				args[0] = thisObj;
				arguments.CopyTo (args, 1);
			}

			CorMethodCall mc = new CorMethodCall ();
			CorValue exception = null;
			CorEval eval = ctx.Eval;

			DebugEventHandler<CorEvalEventArgs> completeHandler = delegate (object o, CorEvalEventArgs eargs) {
				OnEndEvaluating ();
				mc.DoneEvent.Set ();
				eargs.Continue = false;
			};

			DebugEventHandler<CorEvalEventArgs> exceptionHandler = delegate(object o, CorEvalEventArgs eargs) {
				OnEndEvaluating ();
				exception = eargs.Eval.Result;
				mc.DoneEvent.Set ();
				eargs.Continue = false;
			};

			process.OnEvalComplete += completeHandler;
			process.OnEvalException += exceptionHandler;

			mc.OnInvoke = delegate {
				if (function.GetMethodInfo (this).Name == ".ctor")
					eval.NewParameterizedObject (function, typeArgs, args);
				else
					eval.CallParameterizedFunction (function, typeArgs, args);
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_SUSPEND, ctx.Thread);
				ClearEvalStatus ();
				OnStartEvaluating ();
				process.Continue (false);
			};
			mc.OnAbort = delegate {
				eval.Abort ();
			};
			mc.OnGetDescription = delegate {
				MethodInfo met = function.GetMethodInfo (ctx.Session);
				if (met != null)
					return met.DeclaringType.FullName + "." + met.Name;
				else
					return "<Unknown>";
			};

			try {
				ObjectAdapter.AsyncExecute (mc, ctx.Options.EvaluationTimeout);
			}
			catch (COMException ex) {
				// eval exception is a 'good' exception that should be shown in value box
				// all other exceptions must be thrown to error log
				var evalException = TryConvertToEvalException (ex);
				if (evalException != null)
					throw evalException;
				throw;
			}
			finally {
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;
			}

			WaitUntilStopped ();
			if (exception != null) {
/*				ValueReference<CorValue, CorType> msg = ctx.Adapter.GetMember (ctx, val, "Message");
				if (msg != null) {
					string s = msg.ObjectValue as string;
					mc.ExceptionMessage = s;
				}
				else
					mc.ExceptionMessage = "Evaluation failed.";*/
				CorValRef vref = new CorValRef (exception);
				throw new EvaluatorException ("Evaluation failed: " + ObjectAdapter.GetValueTypeName (ctx, vref));
			}

			return eval.Result;
		}

		void OnStartEvaluating ( )
		{
			lock (debugLock) {
				evaluating = true;
			}
		}

		void OnEndEvaluating ( )
		{
			lock (debugLock) {
				evaluating = false;
				Monitor.PulseAll (debugLock);
			}
		}

		CorValue NewSpecialObject (CorEvaluationContext ctx, Action<CorEval> createCall)
		{
			ManualResetEvent doneEvent = new ManualResetEvent (false);
			CorValue result = null;
			var eval = ctx.Eval;
			DebugEventHandler<CorEvalEventArgs> completeHandler = delegate (object o, CorEvalEventArgs eargs) {
				if (eargs.Eval != eval)
					return;
				result = eargs.Eval.Result;
				doneEvent.Set ();
				eargs.Continue = false;
			};

			DebugEventHandler<CorEvalEventArgs> exceptionHandler = delegate(object o, CorEvalEventArgs eargs)
			{
				if (eargs.Eval != eval)
					return;
				result = eargs.Eval.Result;
				doneEvent.Set ();
				eargs.Continue = false;
			};
			process.OnEvalComplete += completeHandler;
			process.OnEvalException += exceptionHandler;

			try {
				createCall (eval);
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_SUSPEND, ctx.Thread);
				OnStartEvaluating ();
				ClearEvalStatus ();
				process.Continue (false);

				if (doneEvent.WaitOne (ctx.Options.EvaluationTimeout, false))
					return result;
				else {
					eval.Abort ();
					return null;
				}
			}
			catch (COMException ex) {
				var evalException = TryConvertToEvalException (ex);
				// eval exception is a 'good' exception that should be shown in value box
				// all other exceptions must be thrown to error log
				if (evalException != null)
					throw evalException;
				throw;
			}
			finally {
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;
				OnEndEvaluating ();
			}
		}

		public CorValue NewString (CorEvaluationContext ctx, string value)
		{
			return NewSpecialObject (ctx, eval => eval.NewString (value));
		}

		public CorValue NewArray (CorEvaluationContext ctx, CorType elemType, int size)
		{
			return NewSpecialObject (ctx, eval => eval.NewParameterizedArray (elemType, 1, 1, 0));
		}

		private static EvaluatorException TryConvertToEvalException (COMException ex)
		{
			var hResult = ex.ToHResult<HResult> ();
			string message = null;
			switch (hResult) {
				case HResult.CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT:
					message = "The thread is not at a GC-safe point";
					break;
				case HResult.CORDBG_E_ILLEGAL_IN_PROLOG:
					message = "The thread is in the prolog";
					break;
				case HResult.CORDBG_E_ILLEGAL_IN_NATIVE_CODE:
					message = "The thread is in native code";
					break;
				case HResult.CORDBG_E_ILLEGAL_IN_OPTIMIZED_CODE:
					message = "The thread is in optimized code";
					break;
				case HResult.CORDBG_E_FUNC_EVAL_BAD_START_POINT:
					message = "Bad starting point to perform evaluation";
					break;
			}
			if (message != null)
				return new EvaluatorException ("Evaluation is not allowed: {0}", message);
			return null;
		}


		public void WaitUntilStopped ()
		{
			lock (debugLock) {
				while (evaluating)
					Monitor.Wait (debugLock);
			}
		}

		void ClearEvalStatus ( )
		{
			foreach (CorProcess p in dbg.Processes) {
				if (p.Id == processId) {
					process = p;
					break;
				}
			}
		}
		
		void ClearHandles ( )
		{
			foreach (CorHandleValue handle in handles.Values) {
				handle.Dispose ();
			}
			handles.Clear ();
		}

		ProcessInfo GetProcess (CorProcess proc)
		{
			ProcessInfo info;
			lock (processes) {
				if (!processes.TryGetValue (proc.Id, out info)) {
					info = new ProcessInfo (proc.Id, "");
					processes[proc.Id] = info;
				}
			}
			return info;
		}

		ThreadInfo GetThread (CorThread thread)
		{
			ThreadInfo info;
			lock (threads) {
				if (!threads.TryGetValue (thread.Id, out info)) {
					string loc = string.Empty;
					try {
						if (thread.ActiveFrame != null) {
							StackFrame frame = CorBacktrace.CreateFrame (this, thread.ActiveFrame);
							loc = frame.ToString ();
						}
						else {
							loc = "<Unknown>";
						}
					}
					catch {
						loc = "<Unknown>";
					}
					
					info = new ThreadInfo (thread.Process.Id, thread.Id, GetThreadName (thread), loc);
					threads[thread.Id] = info;
				}
				return info;
			}
		}

		public CorThread GetThread (int id)
		{
			try {
				WaitUntilStopped ();
				foreach (CorThread t in process.Threads)
					if (t.Id == id)
						return t;
				throw new InvalidOperationException ("Invalid thread id " + id);
			}
			catch {
				throw;
			}
		}

		string GetThreadName (CorThread thread)
		{
			// From http://social.msdn.microsoft.com/Forums/en/netfxtoolsdev/thread/461326fe-88bd-4a6b-82a9-1a66b8e65116
		    try 
		    { 
		        CorReferenceValue refVal = thread.ThreadVariable.CastToReferenceValue(); 
		        if (refVal.IsNull) 
		            return string.Empty; 
		        
		        CorObjectValue val = refVal.Dereference().CastToObjectValue(); 
		        if (val != null) 
		        { 
					Type classType = val.ExactType.GetTypeInfo (this);
		            // Loop through all private instance fields in the thread class 
		            foreach (FieldInfo fi in classType.GetFields (BindingFlags.NonPublic | BindingFlags.Instance))
		            { 
		                if (fi.Name == "m_Name")
						{
		                        CorReferenceValue fieldValue = val.GetFieldValue(val.Class, fi.MetadataToken).CastToReferenceValue(); 
							
								if (fieldValue.IsNull)
									return string.Empty;
								else
									return fieldValue.Dereference().CastToStringValue().String;
		                } 
		            } 
		        } 
		    } catch (Exception) {
				// Ignore
			}
			
			return string.Empty;
		}
		
		string EvaluateTrace (CorThread thread, string exp)
		{
			StringBuilder sb = new StringBuilder ();
			int last = 0;
			int i = exp.IndexOf ('{');
			while (i != -1) {
				if (i < exp.Length - 1 && exp [i+1] == '{') {
					sb.Append (exp.Substring (last, i - last + 1));
					last = i + 2;
					i = exp.IndexOf ('{', i + 2);
					continue;
				}
				int j = exp.IndexOf ('}', i + 1);
				if (j == -1)
					break;
				string se = exp.Substring (i + 1, j - i - 1);
				se = EvaluateExpression (thread, se);
				sb.Append (exp.Substring (last, i - last));
				sb.Append (se);
				last = j + 1;
				i = exp.IndexOf ('{', last);
			}
			sb.Append (exp.Substring (last, exp.Length - last));
			return sb.ToString ();
		}
		
		string EvaluateExpression (CorThread thread, string exp)
		{
			try {
				if (thread.ActiveFrame == null)
					return string.Empty;
				EvaluationOptions ops = Options.EvaluationOptions.Clone ();
				ops.AllowTargetInvoke = true;
				CorEvaluationContext ctx = new CorEvaluationContext (this, new CorBacktrace (thread, this), 0, ops);
				ctx.Thread = thread;
				ValueReference val = ctx.Evaluator.Evaluate (ctx, exp);
				return val.CreateObjectValue (false).Value;
			} catch (Exception ex) {
				OnDebuggerOutput (true, ex.ToString ());
				return string.Empty;
			}
		}

		protected override T OnWrapDebuggerObject<T> (T obj)
		{
			if (obj is IBacktrace)
				return (T) (object) new MtaBacktrace ((IBacktrace)obj);
			if (obj is IObjectValueSource)
				return (T)(object)new MtaObjectValueSource ((IObjectValueSource)obj);
			if (obj is IObjectValueUpdater)
				return (T)(object)new MtaObjectValueUpdater ((IObjectValueUpdater)obj);
			if (obj is IRawValue)
				return (T)(object)new MtaRawValue ((IRawValue)obj);
			if (obj is IRawValueArray)
				return (T)(object)new MtaRawValueArray ((IRawValueArray)obj);
			if (obj is IRawValueString)
				return (T)(object)new MtaRawValueString ((IRawValueString)obj);
			return obj;
		}

		public override bool CanSetNextStatement {
			get {
				return true;
			}
		}

		protected override void OnSetNextStatement (long threadId, string fileName, int line, int column)
		{
			if (!CanSetNextStatement)
				throw new NotSupportedException ();
			MtaThread.Run (delegate {
				var thread = GetThread ((int)threadId);
				if (thread == null)
					throw new ArgumentException ("Unknown thread.");

				CorFrame frame = thread.ActiveFrame;
				if (frame == null)
					throw new NotSupportedException ();

				ISymbolMethod met = frame.Function.GetSymbolMethod (this);
				if (met == null) {
					throw new NotSupportedException ();
				}

				int offset = -1;
				int firstSpInLine = -1;
				foreach (SequencePoint sp in met.GetSequencePoints ()) {
					if (sp.IsInside (fileName, line, column)) {
						offset = sp.Offset;
						break;
					} else if (firstSpInLine == -1
					           && sp.StartLine == line
					           && sp.Document.URL.Equals (fileName, StringComparison.OrdinalIgnoreCase)) {
						firstSpInLine = sp.Offset;
					}
				}
				if (offset == -1) {//No exact match? Use first match in that line
					offset = firstSpInLine;
				}
				if (offset == -1) {
					throw new NotSupportedException ();
				}
				try {
					frame.SetIP (offset);
					OnStopped ();
					RaiseStopEvent ();
				} catch {
					throw new NotSupportedException ();
				}
			});
		}
	}

	class SequencePoint
	{
		public int StartLine;
		public int EndLine;
		public int StartColumn;
		public int EndColumn;
		public int Offset;
		public bool IsSpecial;
		public ISymbolDocument Document;

		public bool IsInside (string fileUrl, int line, int column)
		{
			if (!Document.URL.Equals (fileUrl, StringComparison.OrdinalIgnoreCase))
				return false;
			if (line < StartLine || (line == StartLine && column < StartColumn))
				return false;
			if (line > EndLine || (line == EndLine && column > EndColumn))
				return false;
			return true;
		}
	}

	static class SequencePointExt
	{
		public static IEnumerable<SequencePoint> GetSequencePoints (this ISymbolMethod met)
		{
			int sc = met.SequencePointCount;
			int[] offsets = new int[sc];
			int[] lines = new int[sc];
			int[] endLines = new int[sc];
			int[] columns = new int[sc];
			int[] endColumns = new int[sc];
			ISymbolDocument[] docs = new ISymbolDocument[sc];
			met.GetSequencePoints (offsets, docs, lines, columns, endLines, endColumns);

			for (int n = 0; n < sc; n++) {
				SequencePoint sp = new SequencePoint ();
				sp.Document = docs[n];
				sp.StartLine = lines[n];
				sp.EndLine = endLines[n];
				sp.StartColumn = columns[n];
				sp.EndColumn = endColumns[n];
				sp.Offset = offsets[n];
				yield return sp;
			}
		}

		public static Type GetTypeInfo (this CorType type, CorDebuggerSession session)
		{
			Type t;
			if (MetadataHelperFunctionsExtensions.CoreTypes.TryGetValue (type.Type, out t))
				return t;

			if (type.Type == CorElementType.ELEMENT_TYPE_ARRAY || type.Type == CorElementType.ELEMENT_TYPE_SZARRAY) {
				List<int> sizes = new List<int> ();
				List<int> loBounds = new List<int> ();
				for (int n = 0; n < type.Rank; n++) {
					sizes.Add (1);
					loBounds.Add (0);
				}
				return MetadataExtensions.MakeArray (type.FirstTypeParameter.GetTypeInfo (session), sizes, loBounds);
			}

			if (type.Type == CorElementType.ELEMENT_TYPE_BYREF)
				return MetadataExtensions.MakeByRef (type.FirstTypeParameter.GetTypeInfo (session));

			if (type.Type == CorElementType.ELEMENT_TYPE_PTR)
				return MetadataExtensions.MakePointer (type.FirstTypeParameter.GetTypeInfo (session));

			CorMetadataImport mi = session.GetMetadataForModule (type.Class.Module);
			if (mi != null) {
				t = mi.GetType (type.Class.Token);
				CorType[] targs = type.TypeParameters;
				if (targs.Length > 0) {
					List<Type> types = new List<Type> ();
					foreach (CorType ct in targs)
						types.Add (ct.GetTypeInfo (session));
					return MetadataExtensions.MakeGeneric (t, types);
				}
				else
					return t;
			}
			else
				return null;
		}

		public static ISymbolMethod GetSymbolMethod (this CorFunction func, CorDebuggerSession session)
		{
			ISymbolReader reader = session.GetReaderForModule (func.Module);
			if (reader == null)
				return null;
			return reader.GetMethod (new SymbolToken (func.Token));
		}

		public static MethodInfo GetMethodInfo (this CorFunction func, CorDebuggerSession session)
		{
			CorMetadataImport mi = session.GetMetadataForModule (func.Module);
			if (mi != null)
				return mi.GetMethodInfo (func.Token);
			else
				return null;
		}

		public static void SetValue (this CorValRef thisVal, EvaluationContext ctx, CorValRef val)
		{
			CorEvaluationContext cctx = (CorEvaluationContext) ctx;
			CorObjectAdaptor actx = (CorObjectAdaptor) ctx.Adapter;
			if (actx.IsEnum (ctx, thisVal.Val.ExactType) && !actx.IsEnum (ctx, val.Val.ExactType)) {
				ValueReference vr = actx.GetMember (ctx, null, thisVal, "value__");
				vr.Value = val;
				// Required to make sure that var returns an up-to-date value object
				thisVal.Invalidate ();
				return;
			}
				
			CorReferenceValue s = thisVal.Val.CastToReferenceValue ();
			if (s != null) {
				CorReferenceValue v = val.Val.CastToReferenceValue ();
				if (v != null) {
					s.Value = v.Value;
					return;
				}
			}
			CorGenericValue gv = CorObjectAdaptor.GetRealObject (cctx, thisVal.Val) as CorGenericValue;
			if (gv != null)
				gv.SetValue (ctx.Adapter.TargetObjectToObject (ctx, val));
		}
	}
}
