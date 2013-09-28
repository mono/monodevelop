using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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

namespace MonoDevelop.Debugger.Win32
{
	public class CorDebuggerSession: DebuggerSession
	{
		readonly object debugLock = new object ();
		readonly object terminateLock = new object ();

		CorDebugger dbg;
		CorProcess process;
		CorThread activeThread;
		CorStepper stepper;
		bool terminated;
		bool evaluating;
		int processId;

		static int evaluationTimestamp;

		readonly SymbolBinder symbolBinder = new SymbolBinder ();
		Dictionary<string, DocInfo> documents;
		Dictionary<int, ProcessInfo> processes = new Dictionary<int, ProcessInfo> ();
		Dictionary<int, ThreadInfo> threads = new Dictionary<int,ThreadInfo> ();
		Dictionary<string, ModuleInfo> modules;
		readonly Dictionary<CorBreakpoint, BreakEventInfo> breakpoints = new Dictionary<CorBreakpoint, BreakEventInfo> ();
		readonly Dictionary<long, CorHandleValue> handles = new Dictionary<long, CorHandleValue>();
		

		public CorObjectAdaptor ObjectAdapter;

		class DocInfo
		{
			public ISymbolReader Reader;
			public ISymbolDocument Document;
			public CorModule Module;
		}

		class ModuleInfo
		{
			public ISymbolReader Reader;
			public CorModule Module;
			public CorMetadataImport Importer;
			public int References;
		}

		public CorDebuggerSession ( )
		{
			documents = new Dictionary<string, DocInfo> (StringComparer.CurrentCultureIgnoreCase);
			modules = new Dictionary<string, ModuleInfo> (StringComparer.CurrentCultureIgnoreCase);

			ObjectAdapter = new CorObjectAdaptor ();
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
				ObjectAdapter.Dispose ();
			});

			base.Dispose ();

			// There is no explicit way of disposing the metadata objects, so we have
			// to rely on the GC to do it.

			modules = null;
			documents = null;
			threads = null;
			processes = null;
			activeThread = null;
			GC.Collect ();
		}

		void TerminateDebugger ()
		{
			lock (terminateLock) {
				if (terminated)
					return;

				terminated = true;

				ThreadPool.QueueUserWorkItem (delegate
				{
					if (process != null) {
						// Process already running. Stop it. In the ProcessExited event the
						// debugger engine will be terminated
						process.Stop (4000);
						process.Terminate (1);
					}
				});
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

				process = dbg.CreateProcess (startInfo.Command, cmdLine, startInfo.WorkingDirectory, env, flags);
				processId = process.Id;

				process.OnCreateProcess += new CorProcessEventHandler (OnCreateProcess);
				process.OnCreateAppDomain += new CorAppDomainEventHandler (OnCreateAppDomain);
				process.OnAssemblyLoad += new CorAssemblyEventHandler (OnAssemblyLoad);
				process.OnAssemblyUnload += new CorAssemblyEventHandler (OnAssemblyUnload);
				process.OnCreateThread += new CorThreadEventHandler (OnCreateThread);
				process.OnThreadExit += new CorThreadEventHandler (OnThreadExit);
				process.OnModuleLoad += new CorModuleEventHandler (OnModuleLoad);
				process.OnModuleUnload += new CorModuleEventHandler (OnModuleUnload);
				process.OnProcessExit += new CorProcessEventHandler (OnProcessExit);
				process.OnUpdateModuleSymbols += new UpdateModuleSymbolsEventHandler (OnUpdateModuleSymbols);
				process.OnDebuggerError += new DebuggerErrorEventHandler (OnDebuggerError);
				process.OnBreakpoint += new BreakpointEventHandler (OnBreakpoint);
				process.OnStepComplete += new StepCompleteEventHandler (OnStepComplete);
				process.OnBreak += new CorThreadEventHandler (OnBreak);
				process.OnNameChange += new CorThreadEventHandler (OnNameChange);
				process.OnEvalComplete += new EvalEventHandler (OnEvalComplete);
				process.OnEvalException += new EvalEventHandler (OnEvalException);
				process.OnLogMessage += new LogMessageEventHandler (OnLogMessage);
				process.OnException2 += new CorException2EventHandler (OnException2);

				process.RegisterStdOutput (OnStdOutput);

				process.Continue (false);
			});
			OnStarted ();
		}

		void OnStdOutput (object sender, CorTargetOutputEventArgs e)
		{
			OnTargetOutput (e.IsStdError, e.Text);
		}

		void OnLogMessage (object sender, CorLogMessageEventArgs e)
		{
			OnTargetOutput (false, e.Message);
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

		void OnStepComplete (object sender, CorStepCompleteEventArgs e)
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

			BreakEventInfo binfo;
			if (breakpoints.TryGetValue (e.Breakpoint, out binfo)) {
				e.Continue = true;
				Breakpoint bp = (Breakpoint)binfo.BreakEvent;

				binfo.IncrementHitCount();
				if (!binfo.HitCountReached)
					return;
				
				if (!string.IsNullOrEmpty (bp.ConditionExpression)) {
					string res = EvaluateExpression (e.Thread, bp.ConditionExpression);
					if (bp.BreakIfConditionChanges) {
						if (res == bp.LastConditionValue)
							return;
						bp.LastConditionValue = res;
					} else {
						if (res != null && res.ToLower () == "false")
							return;
					}
				}
				switch (bp.HitAction) {
					case HitAction.CustomAction:
						// If custom action returns true, execution must continue
						if (binfo.RunCustomBreakpointAction (bp.CustomActionId))
							return;
						break;
					case HitAction.PrintExpression: {
						string exp = EvaluateTrace (e.Thread, bp.TraceExpression);
						binfo.UpdateLastTraceValue (exp);
						return;
					}
				}
			}
			
			OnStopped ();
			e.Continue = false;
			// If a breakpoint is hit while stepping, cancel the stepping operation
			if (stepper != null && stepper.IsActive ())
				stepper.Deactivate ();
			SetActiveThread (e.Thread);
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetHitBreakpoint);
			args.Process = GetProcess (process);
			args.Thread = GetThread (e.Thread);
			args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
			OnTargetEvent (args);
		}

		void OnDebuggerError (object sender, CorDebuggerErrorEventArgs e)
		{
			Exception ex = Marshal.GetExceptionForHR (e.HResult);
			OnDebuggerOutput (true, string.Format ("Debugger Error: {0}\n", ex.Message));
		}

		void OnUpdateModuleSymbols (object sender, CorUpdateModuleSymbolsEventArgs e)
		{
			SymbolBinder binder = new SymbolBinder ();
			CorMetadataImport mi = new CorMetadataImport (e.Module);
			ISymbolReader reader = binder.GetReaderFromStream (mi.RawCOMObject, e.Stream);
			foreach (ISymbolDocument doc in reader.GetDocuments ()) {
				Console.WriteLine (doc.URL);
			}
			e.Continue = true;
		}

		void OnProcessExit (object sender, CorProcessEventArgs e)
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetExited);

			// If the main thread stopped, terminate the debugger session
			if (e.Process.Id == process.Id) {
				lock (terminateLock) {
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
			CorMetadataImport mi = new CorMetadataImport (e.Module);

			try {
				// Required to avoid the jit to get rid of variables too early
				e.Module.JITCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
			}
			catch {
				// Some kind of modules don't allow JIT flags to be changed.
			}

			string file = e.Module.Assembly.Name;
			lock (documents) {
				ISymbolReader reader = null;
				char[] badPathChars = System.IO.Path.GetInvalidPathChars();
				if (file.IndexOfAny (badPathChars) == -1 && System.IO.File.Exists (System.IO.Path.ChangeExtension (file, ".pdb"))) {
					try {
						reader = symbolBinder.GetReaderForFile (mi.RawCOMObject, file, ".");
						foreach (ISymbolDocument doc in reader.GetDocuments ()) {
							if (string.IsNullOrEmpty (doc.URL))
								continue;
							string docFile = System.IO.Path.GetFullPath (doc.URL);
							DocInfo di = new DocInfo ();
							di.Document = doc;
							di.Reader = reader;
							di.Module = e.Module;
							documents[docFile] = di;
							BindSourceFileBreakpoints (docFile);
						}
					}
					catch (Exception ex) {
						OnDebuggerOutput (true, string.Format ("Debugger Error: {0}\n", ex.Message));
					}
					e.Module.SetJmcStatus (true, null);
				}
				else {
					// Flag modules without debug info as not JMC. In this way
					// the debugger won't try to step into them
					e.Module.SetJmcStatus (false, null);
				}

				ModuleInfo moi;

				if (modules.TryGetValue (e.Module.Name, out moi)) {
					moi.References++;
				}
				else {
					moi = new ModuleInfo ();
					moi.Module = e.Module;
					moi.Reader = reader;
					moi.Importer = mi;
					moi.References = 1;
					modules[e.Module.Name] = moi;
				}
			}
			e.Continue = true;
		}

		void OnModuleUnload (object sender, CorModuleEventArgs e)
		{
			lock (documents) {
				ModuleInfo moi;
				modules.TryGetValue (e.Module.Name, out moi);
				if (moi == null || --moi.References > 0)
					return;

				modules.Remove (e.Module.Name);
				List<string> toRemove = new List<string> ();
				foreach (KeyValuePair<string, DocInfo> di in documents) {
					if (di.Value.Module.Name == e.Module.Name)
						toRemove.Add (di.Key);
				}
				foreach (string file in toRemove) {
					documents.Remove (file);
					UnbindSourceFileBreakpoints (file);
				}
			}
		}

		void OnCreateAppDomain (object sender, CorAppDomainEventArgs e)
		{
            e.AppDomain.Attach();
			e.Continue = true;
		}

		void OnCreateProcess (object sender, CorProcessEventArgs e)
		{
			// Required to avoid the jit to get rid of variables too early
			e.Process.DesiredNGENCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
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
			OnDebuggerOutput (false, string.Format ("Loaded Module '{0}'\n", e.Assembly.Name));
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

				SetActiveThread (e.Thread);
				
				args.Process = GetProcess (process);
				args.Thread = GetThread (e.Thread);
				args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
				OnTargetEvent (args);	
			}
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
			
			// See if a catchpoint is set for this exception.
			foreach (Catchpoint cp in Breakpoints.GetCatchpoints()) {
				if (cp.Enabled && exceptions.Contains(cp.ExceptionName)) {
					return true;
				}
			}
			
			return false;
		}

		protected override void OnAttachToProcess (long processId)
		{
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
				process.Detach ();
			});
		}

		protected override void OnEnableBreakEvent (BreakEventInfo binfo, bool enable)
		{
			MtaThread.Run (delegate
			{
				CorBreakpoint bp = binfo.Handle as CorFunctionBreakpoint;
				if (bp != null)
					bp.Activate (enable);
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

		internal ISymbolReader GetReaderForModule (string file)
		{
			lock (documents) {
				ModuleInfo mod;
				if (!modules.TryGetValue (System.IO.Path.GetFullPath (file), out mod))
					return null;
				return mod.Reader;
			}
		}

		internal CorMetadataImport GetMetadataForModule (string file)
		{
			lock (documents) {
				ModuleInfo mod;
				if (!modules.TryGetValue (System.IO.Path.GetFullPath (file), out mod))
					return null;
				return mod.Importer;
			}
		}

		internal IEnumerable<CorModule> GetModules ( )
		{
			List<CorModule> mods = new List<CorModule> ();
			lock (documents) {
				foreach (ModuleInfo mod in modules.Values)
					mods.Add (mod.Module);
			}
			return mods;
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
			return MtaThread.Run (delegate
			{
				var binfo = new BreakEventInfo ();

				lock (documents) {
					var bp = be as Breakpoint;
					if (bp != null) {
						if (bp is FunctionBreakpoint) {
							// FIXME: implement breaking on function name
							binfo.SetStatus (BreakEventStatus.Invalid, null);
							return binfo;
						}
						else {
							DocInfo doc;
							if (!documents.TryGetValue (System.IO.Path.GetFullPath (bp.FileName), out doc)) {
								binfo.SetStatus (BreakEventStatus.NotBound, null);
								return binfo;
							}

							int line;
							try {
								line = doc.Document.FindClosestLine (bp.Line);
							}
							catch {
								// Invalid line
								binfo.SetStatus (BreakEventStatus.Invalid, null);
								return binfo;
							}
							ISymbolMethod met = doc.Reader.GetMethodFromDocumentPosition (doc.Document, line, 0);
							if (met == null) {
								binfo.SetStatus (BreakEventStatus.Invalid, null);
								return binfo;
							}

							int offset = -1;
							foreach (SequencePoint sp in met.GetSequencePoints ()) {
								if (sp.Line == line && sp.Document.URL == doc.Document.URL) {
									offset = sp.Offset;
									break;
								}
							}
							if (offset == -1) {
								binfo.SetStatus (BreakEventStatus.Invalid, null);
								return binfo;
							}

							CorFunction func = doc.Module.GetFunctionFromToken (met.Token.GetToken ());
							CorFunctionBreakpoint corBp = func.ILCode.CreateBreakpoint (offset);
							corBp.Activate (bp.Enabled);
							breakpoints[corBp] = binfo;

							binfo.Handle = corBp;
							binfo.SetStatus (BreakEventStatus.Bound, null);
							return binfo;
						}
					}

					var cp = be as Catchpoint;
					if (cp != null) {
						foreach (ModuleInfo mod in modules.Values) {
							CorMetadataImport mi = mod.Importer;
							if (mi != null) {
								foreach (Type t in mi.DefinedTypes)
									if (t.FullName == cp.ExceptionName) {
										binfo.SetStatus (BreakEventStatus.Bound, null);
										return binfo;
									}
							}
						}
					}
				}

				binfo.SetStatus (BreakEventStatus.Invalid, null);
				return binfo;
			});
		}

		protected override void OnNextInstruction ( )
		{
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
				if (stepper != null) {
					stepper.IsActive ();
					CorFrame frame = activeThread.ActiveFrame;
					ISymbolReader reader = GetReaderForModule (frame.Function.Module.Name);
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

					// Find the current line
					SequencePoint currentSeq = null;
					foreach (SequencePoint sp in met.GetSequencePoints ()) {
						if (sp.Offset > offset)
							break;
						currentSeq = sp;
					}

					if (currentSeq == null) {
						RawContinue (into);
						return;
					}

					// Exclude all ranges belonging to the current line
					List<COR_DEBUG_STEP_RANGE> ranges = new List<COR_DEBUG_STEP_RANGE> ();
					SequencePoint lastSeq = null;
					foreach (SequencePoint sp in met.GetSequencePoints ()) {
						if (lastSeq != null && lastSeq.Line == currentSeq.Line) {
							COR_DEBUG_STEP_RANGE r = new COR_DEBUG_STEP_RANGE ();
							r.startOffset = (uint) lastSeq.Offset;
							r.endOffset = (uint) sp.Offset;
							ranges.Add (r);
						}
						lastSeq = sp;
					}

					stepper.StepRange (into, ranges.ToArray ());

					ClearEvalStatus ();
					process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, null);
					process.Continue (false);
				}
			} catch (Exception e) {
				OnDebuggerOutput (true, e.ToString ());
			}
		}

		private void RawContinue (bool into)
		{
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
				corBp.Activate (false);
			});
		}


		protected override void OnSetActiveThread (long processId, long threadId)
		{
			MtaThread.Run (delegate
			{
				activeThread = null;
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
			stepper = activeThread.CreateStepper (); 
			stepper.SetUnmappedStopMask (CorDebugUnmappedStop.STOP_NONE);
			stepper.SetJmcStatus (true);
		}

		protected override void OnStepInstruction ( )
		{
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

			EvalEventHandler completeHandler = delegate (object o, CorEvalEventArgs eargs) {
				OnEndEvaluating ();
				mc.DoneEvent.Set ();
				eargs.Continue = false;
			};

			EvalEventHandler exceptionHandler = delegate (object o, CorEvalEventArgs eargs) {
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
			finally {
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;
			}

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

		public CorValue NewString (CorEvaluationContext ctx, string value)
		{
			ManualResetEvent doneEvent = new ManualResetEvent (false);
			CorValue result = null;

			EvalEventHandler completeHandler = delegate (object o, CorEvalEventArgs eargs) {
				OnEndEvaluating ();
				result = eargs.Eval.Result;
				doneEvent.Set ();
				eargs.Continue = false;
			};

			EvalEventHandler exceptionHandler = delegate (object o, CorEvalEventArgs eargs) {
				OnEndEvaluating ();
				result = eargs.Eval.Result;
				doneEvent.Set ();
				eargs.Continue = false;
			};

			try {
				process.OnEvalComplete += completeHandler;
				process.OnEvalException += exceptionHandler;

				ctx.Eval.NewString (value);
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_SUSPEND, ctx.Thread);
				OnStartEvaluating ();
				ClearEvalStatus ();
				process.Continue (false);

				if (doneEvent.WaitOne (ctx.Options.EvaluationTimeout, false))
					return result;
				else
					return null;
			} finally {
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;
			}
		}

		public CorValue NewArray (CorEvaluationContext ctx, CorType elemType, int size)
		{
			ManualResetEvent doneEvent = new ManualResetEvent (false);
			CorValue result = null;

			EvalEventHandler completeHandler = delegate (object o, CorEvalEventArgs eargs)
			{
				OnEndEvaluating ();
				result = eargs.Eval.Result;
				doneEvent.Set ();
				eargs.Continue = false;
			};

			EvalEventHandler exceptionHandler = delegate (object o, CorEvalEventArgs eargs)
			{
				OnEndEvaluating ();
				result = eargs.Eval.Result;
				doneEvent.Set ();
				eargs.Continue = false;
			};

			try {
				process.OnEvalComplete += completeHandler;
				process.OnEvalException += exceptionHandler;

				ctx.Eval.NewParameterizedArray (elemType, 1, 1, 0);
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_SUSPEND, ctx.Thread);
				OnStartEvaluating ();
				ClearEvalStatus ();
				process.Continue (false);

				if (doneEvent.WaitOne (ctx.Options.EvaluationTimeout, false))
					return result;
				else
					return null;
			}
			finally {
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;
			}
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
	}

	class SequencePoint
	{
		public int Line;
		public int Offset;
		public ISymbolDocument Document;
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
				if (columns[n] == 0)
					continue;
				SequencePoint sp = new SequencePoint ();
				sp.Document = docs[n];
				sp.Line = lines[n];
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

			CorMetadataImport mi = session.GetMetadataForModule (type.Class.Module.Name);
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
			ISymbolReader reader = session.GetReaderForModule (func.Module.Name);
			if (reader == null)
				return null;
			return reader.GetMethod (new SymbolToken (func.Token));
		}

		public static MethodInfo GetMethodInfo (this CorFunction func, CorDebuggerSession session)
		{
			CorMetadataImport mi = session.GetMetadataForModule (func.Module.Name);
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
				thisVal.IsValid = false;
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
