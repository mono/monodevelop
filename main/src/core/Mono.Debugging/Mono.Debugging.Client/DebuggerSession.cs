// DebuggerSession.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
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
using Mono.Debugging.Backend;
using System.Diagnostics;
using System.Threading;

namespace Mono.Debugging.Client
{
	public delegate void TargetEventHandler (object sender, TargetEventArgs args);
	public delegate void ProcessEventHandler(int process_id);
	public delegate void ThreadEventHandler(int thread_id);
	public delegate bool ExceptionHandler (Exception ex);
	public delegate string TypeResolverHandler (string identifier, SourceLocation location);
	public delegate void BreakpointTraceHandler (BreakEvent be, string trace);
	public delegate IExpressionEvaluator GetExpressionEvaluatorHandler (string extension);
	public delegate IConnectionDialog ConnectionDialogCreator ();
	
	public abstract class DebuggerSession: IDisposable
	{
		InternalDebuggerSession frontend;
		Dictionary<BreakEvent,BreakEventInfo> breakpoints = new Dictionary<BreakEvent,BreakEventInfo> ();
		BreakpointStore breakpointStore;
		OutputWriterDelegate outputWriter;
		OutputWriterDelegate logWriter;
		bool disposed;
		bool attached;
		bool ownedBreakpointStore;
		object slock = new object ();
		object olock = new object ();
		ThreadInfo activeThread;
		BreakEventHitHandler customBreakpointHitHandler;
		ExceptionHandler exceptionHandler;
		DebuggerSessionOptions options;
		Dictionary<string,string> resolvedExpressionCache = new Dictionary<string, string> ();
		bool adjustingBreakpoints;
		
		ProcessInfo[] currentProcesses;

		/// <summary>
		/// Reports a debugger event
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetEvent;
		
		/// <summary>
		/// Raised when the debugger resumes execution after being stopped
		/// </summary>
		public event EventHandler TargetStarted;
		
		/// <summary>
		/// Raised when the underlying debugging engine has been initialized and it is ready to start execution.
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetReady;
		
		/// <summary>
		/// Raised when the debugging session is paused
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetStopped;
		
		/// <summary>
		/// Raised when the execution is interrupted by an external event
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetInterrupted;
		
		/// <summary>
		/// Raised when a breakpoint is hit
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetHitBreakpoint;
		
		/// <summary>
		/// Raised when the execution is interrupted due to receiving a signal
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetSignaled;
		
		/// <summary>
		/// Raised when the debugged process exits
		/// </summary>
		public event EventHandler TargetExited;
		
		/// <summary>
		/// Raised when an exception for which there is a catchpoint is thrown
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetExceptionThrown;
		
		/// <summary>
		/// Raised when an exception is unhandled
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetUnhandledException;
		
		/// <summary>
		/// Raised when a thread is started in the debugged process
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetThreadStarted;
		
		/// <summary>
		/// Raised when a thread is stopped in the debugged process
		/// </summary>
		public event EventHandler<TargetEventArgs> TargetThreadStopped;
		
		/// <summary>
		/// Raised when the 'busy state' of the debugger changes.
		/// The debugger may switch to busy state if it is in the middle
		/// of an expression evaluation which can't be aborted.
		/// </summary>
		public event EventHandler<BusyStateEventArgs> BusyStateChanged;
		
		public DebuggerSession ()
		{
			UseOperationThread = true;
			frontend = new InternalDebuggerSession (this);
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="Mono.Debugging.Client.DebuggerSession"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Mono.Debugging.Client.DebuggerSession"/>.
		/// The <see cref="Dispose"/> method leaves the <see cref="Mono.Debugging.Client.DebuggerSession"/> in an unusable
		/// state. After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Mono.Debugging.Client.DebuggerSession"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Mono.Debugging.Client.DebuggerSession"/> was occupying.
		/// </remarks>
		public virtual void Dispose ()
		{
			Dispatch (delegate {
				if (!disposed) {
					disposed = true;
					if (!ownedBreakpointStore)
						Breakpoints = null;
				}
			});
		}
		
		/// <summary>
		/// Gets or sets an exception handler to be invoked when an exception is raised by the debugger engine.
		/// </summary>
		/// <remarks>
		/// Notice that this handler will be used to report exceptions in the debugger, not exceptions raised
		/// in the debugged process.
		/// </remarks>
		public ExceptionHandler ExceptionHandler {
			get { return exceptionHandler; }
			set { exceptionHandler = value; }
		}
		
		/// <summary>
		/// Gets or sets the connection dialog creator callback.
		/// </summary>
		public ConnectionDialogCreator ConnectionDialogCreator { get; set; }

		/// <summary>
		/// Gets or sets the breakpoint trace handler.
		/// </summary>
		/// <remarks>
		/// This handler is invoked when the value of a tracepoint has to be printed
		/// </remarks>
		public BreakpointTraceHandler BreakpointTraceHandler { get; set; }
		
		/// <summary>
		/// Gets or sets the type resolver handler.
		/// </summary>
		/// <remarks>
		/// This handler is invoked when the expression evaluator needs to resolve a type name.
		/// </remarks>
		public TypeResolverHandler TypeResolverHandler { get; set; }
		
		/// <summary>
		/// Gets or sets the an expression evaluator provider
		/// </summary>
		/// <remarks>
		/// This handler is invoked when the debugger needs to get an evaluator for a specific type of file
		/// </remarks>
		public GetExpressionEvaluatorHandler GetExpressionEvaluator { get; set; }		

		/// <summary>
		/// Gets or sets the custom break event hit handler.
		/// </summary>
		/// <remarks>
		/// This handler is invoked when a custom breakpoint is hit to determine if the debug session should
		/// continue or stop.
		/// </remarks>
		public BreakEventHitHandler CustomBreakEventHitHandler {
			get {
				return customBreakpointHitHandler;
			}
			set {
				customBreakpointHitHandler = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the breakpoint store for the debugger session.
		/// </summary>
		public BreakpointStore Breakpoints {
			get {
				lock (slock) {
					if (breakpointStore == null) {
						Breakpoints = new BreakpointStore ();
						ownedBreakpointStore = true;
					}
					return breakpointStore;
				}
			}
			set {
				lock (slock) {
					if (breakpointStore != null) {
						foreach (BreakEvent bp in breakpointStore) {
							RemoveBreakEvent (bp);
							NotifyBreakEventStatusChanged (bp);
						}
						breakpointStore.BreakEventAdded -= OnBreakpointAdded;
						breakpointStore.BreakEventRemoved -= OnBreakpointRemoved;
						breakpointStore.BreakEventModified -= OnBreakpointModified;
						breakpointStore.BreakEventEnableStatusChanged -= OnBreakpointStatusChanged;
						breakpointStore.CheckingReadOnly -= BreakpointStoreCheckingReadOnly;
						breakpointStore.ResetAdjustedBreakpoints ();
					}
					
					breakpointStore = value;
					ownedBreakpointStore = false;
					
					if (breakpointStore != null) {
						if (IsConnected) {
							foreach (BreakEvent bp in breakpointStore)
								AddBreakEvent (bp);
						}
						breakpointStore.BreakEventAdded += OnBreakpointAdded;
						breakpointStore.BreakEventRemoved += OnBreakpointRemoved;
						breakpointStore.BreakEventModified += OnBreakpointModified;
						breakpointStore.BreakEventEnableStatusChanged += OnBreakpointStatusChanged;
						breakpointStore.CheckingReadOnly += BreakpointStoreCheckingReadOnly;
					}
				}
			}
		}
		
		void Dispatch (Action action)
		{
			if (UseOperationThread) {
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
					lock (slock) {
						action ();
					}
				});
			} else {
				lock (slock) {
					action ();
				}
			}
		}
		
		/// <summary>
		/// Starts a debugging session
		/// </summary>
		/// <param name='startInfo'>
		/// Startup information
		/// </param>
		/// <param name='options'>
		/// Session options
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		public void Run (DebuggerStartInfo startInfo, DebuggerSessionOptions options)
		{
			if (startInfo == null)
				throw new ArgumentNullException ("startInfo");
			if (options == null)
				throw new ArgumentNullException ("options");
			
			lock (slock) {
				this.options = options;
				OnRunning ();
				Dispatch (delegate {
					try {
						OnRun (startInfo);
					} catch (Exception ex) {
						ForceExit ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Starts a debugging session by attaching the debugger to a running process
		/// </summary>
		/// <param name='proc'>
		/// Process information
		/// </param>
		/// <param name='options'>
		/// Debugging options
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		public void AttachToProcess (ProcessInfo proc, DebuggerSessionOptions options)
		{
			if (proc == null)
				throw new ArgumentNullException ("proc");
			if (options == null)
				throw new ArgumentNullException ("options");
			
			lock (slock) {
				this.options = options;
				OnRunning ();
				Dispatch (delegate {
					try {
						OnAttachToProcess (proc.Id);
						attached = true;
					} catch (Exception ex) {
						ForceExit ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Detaches this debugging session from the debugged process
		/// </summary>
		public void Detach ()
		{
			lock (slock) {
				try {
					OnDetach ();
				} catch (Exception ex) {
					if (!HandleException (ex))
						throw;
				}
			}
		}
		
		/// <summary>
		/// Gets a value indicating whether this <see cref="Mono.Debugging.Client.DebuggerSession"/> has been attached to a process using the Attach method.
		/// </summary>
		/// <value>
		/// <c>true</c> if attached to process; otherwise, <c>false</c>.
		/// </value>
		public bool AttachedToProcess {
			get {
				lock (slock) {
					return attached; 
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the active thread.
		/// </summary>
		/// <remarks>
		/// This property can only be used when the debugger is paused
		/// </remarks>
		public ThreadInfo ActiveThread {
			get {
				lock (slock) {
					return activeThread;
				}
			}
			set {
				lock (slock) {
					try {
						activeThread = value;
						OnSetActiveThread (activeThread.ProcessId, activeThread.Id);
					} catch (Exception ex) {
						if (!HandleException (ex))
							throw;
					}
				}
			}
		}
		
		
		/// <summary>
		/// Executes one line of code
		/// </summary>
		public void NextLine ()
		{
			lock (slock) {
				OnRunning ();
				Dispatch (delegate {
					try {
						OnNextLine ();
					} catch (Exception ex) {
						ForceStop ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Executes one line of code, stepping into method invocations
		/// </summary>
		public void StepLine ()
		{
			lock (slock) {
				OnRunning ();
				Dispatch (delegate {
					try {
						OnStepLine ();
					} catch (Exception ex) {
						ForceStop ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Executes one low level instruction
		/// </summary>
		public void NextInstruction ()
		{
			lock (slock) {
				OnRunning ();
				Dispatch (delegate {
					try {
						OnNextInstruction ();
					} catch (Exception ex) {
						ForceStop ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}

		/// <summary>
		/// Executes one low level instruction, stepping into method invocations
		/// </summary>
		public void StepInstruction ()
		{
			lock (slock) {
				OnRunning ();
				Dispatch (delegate {
					try {
						OnStepInstruction ();
					} catch (Exception ex) {
						ForceStop ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Resumes the execution of the debugger and stops when the current method is exited
		/// </summary>
		public void Finish ()
		{
			lock (slock) {
				OnRunning ();
				Dispatch (delegate {
					try {
						OnFinish ();
					} catch (Exception ex) {
						ForceExit ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Returns the status of a breakpoint for this debugger session.
		/// </summary>
		public BreakEventStatus GetBreakEventStatus (BreakEvent be)
		{
			if (IsConnected) {
				BreakEventInfo binfo;
				lock (breakpoints) {
					if (breakpoints.TryGetValue (be, out binfo))
						return binfo.Status;
				}
			}
			return BreakEventStatus.NotBound;
		}

		/// <summary>
		/// Returns a status message of a breakpoint for this debugger session.
		/// </summary>
		public string GetBreakEventStatusMessage (BreakEvent be)
		{
			if (IsConnected) {
				BreakEventInfo binfo;
				lock (breakpoints) {
					if (breakpoints.TryGetValue (be, out binfo)) {
						if (binfo.StatusMessage != null)
							return binfo.StatusMessage;
						switch (binfo.Status) {
						case BreakEventStatus.BindError: return "The breakpoint could not be bound";
						case BreakEventStatus.Bound: return "";
						case BreakEventStatus.Disconnected: return "";
						case BreakEventStatus.Invalid: return "The breakpoint location is invalid. Perhaps the source line does " +
							"not contain any statements, or the source does not correspond to the current binary";
						case BreakEventStatus.NotBound: return "The breakpoint could not yet be bound to a valid location";
						}
					}
				}
			}
			return "The breakpoint will not currently be hit";
		}
		
		void AddBreakEvent (BreakEvent be)
		{
			try {
				var eventInfo = OnInsertBreakEvent (be);
				if (eventInfo == null)
					throw new InvalidOperationException ("OnInsertBreakEvent can't return a null value. If the breakpoint can't be bound or is invalid, a BreakEventInfo with the corresponding status must be returned");
				lock (breakpoints) {
					breakpoints [be] = eventInfo;
				}
				eventInfo.AttachSession (this, be);
			} catch (Exception ex) {
				string msg;
				
				if (be is FunctionBreakpoint)
					msg = "Could not set breakpoint at location '" + ((FunctionBreakpoint) be).FunctionName + ":" + ((FunctionBreakpoint) be).Line + "'";
				else if (be is Breakpoint)
					msg = "Could not set breakpoint at location '" + ((Breakpoint) be).FileName + ":" + ((Breakpoint) be).Line + "'";
				else
					msg = "Could not set catchpoint for exception '" + ((Catchpoint) be).ExceptionName + "'";
				
				msg += " (" + ex.Message + ")";
				OnDebuggerOutput (false, msg + "\n");
				HandleException (ex);
				return;
			}
		}

		bool RemoveBreakEvent (BreakEvent be)
		{
			lock (breakpoints) {
				BreakEventInfo binfo;
				if (breakpoints.TryGetValue (be, out binfo)) {
					try {
						OnRemoveBreakEvent (binfo);
					} catch (Exception ex) {
						if (IsConnected)
							OnDebuggerOutput (false, ex.Message);
						HandleException (ex);
						return false;
					}
					breakpoints.Remove (be);
				}
				return true;
			}
		}
		
		void UpdateBreakEventStatus (BreakEvent be)
		{
			lock (breakpoints) {
				BreakEventInfo binfo;
				if (breakpoints.TryGetValue (be, out binfo)) {
					try {
						OnEnableBreakEvent (binfo, be.Enabled);
					} catch (Exception ex) {
						if (IsConnected)
							OnDebuggerOutput (false, ex.Message);
						HandleException (ex);
					}
				}
			}
		}
		
		void UpdateBreakEvent (BreakEvent be)
		{
			lock (breakpoints) {
				BreakEventInfo binfo;
				if (breakpoints.TryGetValue (be, out binfo))
					OnUpdateBreakEvent (binfo);
			}
		}
		
		void OnBreakpointAdded (object s, BreakEventArgs args)
		{
			lock (breakpoints) {
				if (adjustingBreakpoints)
					return;
			}
			lock (slock) {
				if (IsConnected)
					AddBreakEvent (args.BreakEvent);
			}
		}
		
		void OnBreakpointRemoved (object s, BreakEventArgs args)
		{
			lock (breakpoints) {
				if (adjustingBreakpoints)
					return;
			}
			lock (slock) {
				if (IsConnected)
					RemoveBreakEvent (args.BreakEvent);
			}
		}
		
		void OnBreakpointModified (object s, BreakEventArgs args)
		{
			lock (slock) {
				if (IsConnected)
					UpdateBreakEvent (args.BreakEvent);
			}
		}
		
		void OnBreakpointStatusChanged (object s, BreakEventArgs args)
		{
			lock (slock) {
				if (IsConnected)
					UpdateBreakEventStatus (args.BreakEvent);
			}
		}

		void BreakpointStoreCheckingReadOnly (object sender, ReadOnlyCheckEventArgs e)
		{
			// When this used 'lock', it was a common cause of deadlocks, as it is called on a timeout from the GUI 
			// thread, so if something else held the session lock, the GUI would deadlock. Instead we use TryEnter,
			// so the worst that can happen is that users won't be able to modify breakpoints.
			//FIXME: why do we always lock accesses to AllowBreakEventChanges? Only MonoDebuggerSession needs it locked.
			bool entered = false;
			try {
				entered = Monitor.TryEnter (slock, TimeSpan.FromMilliseconds (10));
				e.SetReadOnly (!entered || !AllowBreakEventChanges);
			} finally {
				if (entered)
					Monitor.Exit (slock);
			}
		}
		
		/// <summary>
		/// Gets the debugger options object
		/// </summary>
		public DebuggerSessionOptions Options {
			get { return options; }
		}
		
		/// <summary>
		/// Gets or sets the evaluation options.
		/// </summary>
		public EvaluationOptions EvaluationOptions {
			get { return options.EvaluationOptions; }
			set { options.EvaluationOptions = value; }
		}
		
		/// <summary>
		/// Resumes the execution of the debugger
		/// </summary>
		public void Continue ()
		{
			lock (slock) {
				OnRunning ();
				Dispatch (delegate {
					try {
						OnContinue ();
					} catch (Exception ex) {
						ForceStop ();
						if (!HandleException (ex))
							throw;
					}
				});
			}
		}
		
		/// <summary>
		/// Pauses the execution of the debugger
		/// </summary>
		public void Stop ()
		{
			Dispatch (delegate {
				try {
					OnStop ();
				} catch (Exception ex) {
					if (!HandleException (ex))
						throw;
				}
			});
		}
		
		/// <summary>
		/// Stops the execution of the debugger by killing the debugged process
		/// </summary>
		public void Exit ()
		{
			Dispatch (delegate {
				try {
					OnExit ();
				} catch (Exception ex) {
					if (!HandleException (ex))
						throw;
				}
			});
		}

		/// <summary>
		/// Gets a value indicating whether the debuggee is currently connected
		/// </summary>
		public bool IsConnected {
			get; private set;
		}
		
		/// <summary>
		/// Gets a value indicating whether the debuggee is currently running (not paused by the debugger)
		/// </summary>
		public bool IsRunning {
			get; private set;
		}

		/// <summary>
		/// Gets a value indicating whether the debuggee has exited.
		/// </summary>
		public bool HasExited {
			get; protected set;
		}
		
		/// <summary>
		/// Gets a list of all processes
		/// </summary>
		/// <remarks>
		/// This method can only be used when the debuggee is stopped by the debugger
		/// </remarks>
		public ProcessInfo[] GetProcesses ()
		{
			lock (slock) {
				if (currentProcesses == null) {
					currentProcesses = OnGetProcesses ();
					foreach (ProcessInfo p in currentProcesses)
						p.Attach (this);
				}
				return currentProcesses;
			}
		}
		
		/// <summary>
		/// Gets or sets the output writer callback.
		/// </summary>
		/// <remarks>
		/// This callback is invoked to print debuggee output
		/// </remarks>
		public OutputWriterDelegate OutputWriter {
			get { return outputWriter; }
			set {
				lock (olock) {
					outputWriter = value;
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the log writer.
		/// </summary>
		/// <remarks>
		/// This callback is invoked to print debugger log messages
		/// </remarks>
		public OutputWriterDelegate LogWriter {
			get { return logWriter; }
			set {
				lock (olock) {
					logWriter = value;
				}
			}
		}
		
		/// <summary>
		/// Gets the disassembly of a source code file
		/// </summary>
		/// <returns>
		/// An array of AssemblyLine, with one element for each source code line that could be disassembled
		/// </returns>
		/// <param name='file'>
		/// The file.
		/// </param>
		/// <remarks>
		/// This method can only be used when the debuggee is stopped by the debugger
		/// </remarks>
		public AssemblyLine[] DisassembleFile (string file)
		{
			lock (slock) {
				return OnDisassembleFile (file);
			}
		}
		
		public string ResolveExpression (string expression, string file, int line, int column)
		{
			return ResolveExpression (expression, new SourceLocation (null, file, line, column));
		}
		
		public virtual string ResolveExpression (string expression, SourceLocation location)
		{
			if (TypeResolverHandler == null)
				return expression;
			else {
				string key = expression + " " + location;
				string resolved;
				if (!resolvedExpressionCache.TryGetValue (key, out resolved)) {
					try {
						resolved = OnResolveExpression (expression, location);
					} catch (Exception ex) {
						OnDebuggerOutput (true, "Error while resolving expression: " + ex.Message);
					}
					resolvedExpressionCache [key] = resolved;
				}
				return resolved ?? expression;
			}
		}
		
		/// <summary>
		/// Stops the execution of background evaluations being done by the debugger
		/// </summary>
		/// <remarks>
		/// This method can only be used when the debuggee is stopped by the debugger
		/// </remarks>
		public void CancelAsyncEvaluations ()
		{
			if (UseOperationThread) {
				ThreadPool.QueueUserWorkItem (delegate {
					OnCancelAsyncEvaluations ();
				});
			} else
				OnCancelAsyncEvaluations ();
		}

		/// <summary>
		/// Gets a value indicating whether there are background evaluations being done by the debugger
		/// which can be cancelled.
		/// </summary>
		/// <remarks>
		/// This method can only be used when the debuggee is stopped by the debugger
		/// </remarks>
		public virtual bool CanCancelAsyncEvaluations {
			get { return false; }
		}
		
		/// <summary>
		/// Override to stop the execution of background evaluations being done by the debugger
		/// </summary>
		protected virtual void OnCancelAsyncEvaluations ()
		{
		}
		
		Mono.Debugging.Evaluation.ExpressionEvaluator defaultResolver = new Mono.Debugging.Evaluation.NRefactoryExpressionEvaluator ();
		Dictionary <string, IExpressionEvaluator> evaluators = new Dictionary <string, IExpressionEvaluator> ();

		internal IExpressionEvaluator FindExpressionEvaluator (StackFrame frame)
		{
			if (GetExpressionEvaluator == null)
				return null;

			string fn = frame.SourceLocation == null ? null : frame.SourceLocation.FileName;
			if (String.IsNullOrEmpty (fn))
				return null;

			fn = System.IO.Path.GetExtension (fn);
			IExpressionEvaluator result;
			if (evaluators.TryGetValue (fn, out result))
				return result;

			result = GetExpressionEvaluator(fn);

			evaluators[fn] = result;

			return result;
		}

		public Mono.Debugging.Evaluation.ExpressionEvaluator GetEvaluator (StackFrame frame)
		{
			IExpressionEvaluator result = FindExpressionEvaluator (frame);
			if (result == null)
				return defaultResolver;
			return result.Evaluator;
		}
		
		
		/// <summary>
		/// Called when an expression needs to be resolved
		/// </summary>
		/// <param name='expression'>
		/// The expression
		/// </param>
		/// <param name='location'>
		/// The source code location
		/// </param>
		/// <returns>
		/// The resolved expression
		/// </returns>
		protected virtual string OnResolveExpression (string expression, SourceLocation location)
		{
			return defaultResolver.Resolve (this, location, expression);
		}
		
		internal protected string ResolveIdentifierAsType (string identifier, SourceLocation location)
		{
			if (TypeResolverHandler != null)
				return TypeResolverHandler (identifier, location);

			return null;
		}
		
		internal ThreadInfo[] GetThreads (long processId)
		{
			lock (slock) {
				ThreadInfo[] threads = OnGetThreads (processId);
				foreach (ThreadInfo t in threads)
					t.Attach (this);
				return threads;
			}
		}
		
		internal Backtrace GetBacktrace (long processId, long threadId)
		{
			lock (slock) {
				Backtrace bt = OnGetThreadBacktrace (processId, threadId);
				if (bt != null)
					bt.Attach (this);
				return bt;
			}
		}
		
		void ForceStop ()
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetStopped);
			OnTargetEvent (args);
		}
		
		void ForceExit ()
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetExited);
			OnTargetEvent (args);
		}
		
		internal protected void OnTargetEvent (TargetEventArgs args)
		{
			currentProcesses = null;
			
			if (args.Process != null)
				args.Process.Attach (this);
			if (args.Thread != null) {
				args.Thread.Attach (this);
				activeThread = args.Thread;
			}
			if (args.Backtrace != null)
				args.Backtrace.Attach (this);

			EventHandler<TargetEventArgs> evnt = null;
			switch (args.Type) {
			case TargetEventType.ExceptionThrown:
				lock (slock) {
					IsRunning = false;
					args.IsStopEvent = true;
				}
				evnt = TargetExceptionThrown;
				break;
			case TargetEventType.TargetExited:
				lock (slock) {
					IsRunning = false;
					IsConnected = false;
					HasExited = true;
				}
				EventHandler handler = TargetExited;
				if (handler != null)
					handler (this, args);
				break;
			case TargetEventType.TargetHitBreakpoint:
				lock (slock) {
					IsRunning = false;
					args.IsStopEvent = true;
				}
				evnt = TargetHitBreakpoint;
				break;
			case TargetEventType.TargetInterrupted:
				lock (slock) {
					IsRunning = false;
					args.IsStopEvent = true;
				}
				evnt = TargetInterrupted;
				break;
			case TargetEventType.TargetSignaled:
				lock (slock) {
					IsRunning = false;
					args.IsStopEvent = true;
				}
				evnt = TargetSignaled;
				break;
			case TargetEventType.TargetStopped:
				lock (slock) {
					IsRunning = false;
					args.IsStopEvent = true;
				}
				evnt = TargetStopped;
				break;
			case TargetEventType.UnhandledException:
				lock (slock) {
					IsRunning = false;
					args.IsStopEvent = true;
				}
				evnt = TargetUnhandledException;
				break;
			case TargetEventType.TargetReady:
				evnt = TargetReady;
				break;
			case TargetEventType.ThreadStarted:
				evnt = TargetThreadStarted;
				break;
			case TargetEventType.ThreadStopped:
				evnt = TargetThreadStopped;
				break;
			}

			if (evnt != null)
				evnt (this, args);

			EventHandler<TargetEventArgs> targetEvent = TargetEvent;
			if (targetEvent != null)
				targetEvent (this, args);
		}
		
		internal void OnRunning ()
		{
			IsRunning = true;
			if (TargetStarted != null)
				TargetStarted (this, EventArgs.Empty);
		}
		
		internal protected void OnStarted ()
		{
			OnStarted (null);
		}
		
		internal protected virtual void OnStarted (ThreadInfo t)
		{
			if (HasExited)
				return;

			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetReady) { Thread = t });

			lock (slock) {
				if (!HasExited) {
					IsConnected = true;
					foreach (BreakEvent bp in breakpointStore)
						AddBreakEvent (bp);
				}
			}
		}
		
		internal protected void OnTargetOutput (bool isStderr, string text)
		{
			lock (olock) {
				if (outputWriter != null)
					outputWriter (isStderr, text);
			}
		}
		
		internal protected void OnDebuggerOutput (bool isStderr, string text)
		{
			lock (olock) {
				if (logWriter != null)
					logWriter (isStderr, text);
			}
		}
		
		internal protected void SetBusyState (BusyStateEventArgs args)
		{
			if (BusyStateChanged != null)
				BusyStateChanged (this, args);
		}
		
		/// <summary>
		/// Tries to bind all unbound breakpoints of a source file
		/// </summary>
		/// <param name='fullFilePath'>
		/// Source file path
		/// </param>
		/// <remarks>
		/// This method can be called by a subclass to ask the debugger session to attempt
		/// to bind all unbound breakpoints defined on the given file. This method could
		/// be called, for example, when a new assembly that contains this file is loaded
		/// into memory. It is not necessary to use this method if the subclass keeps
		/// track of unbound breakpoints by itself.
		/// </remarks>
		internal protected void BindSourceFileBreakpoints (string fullFilePath)
		{
			lock (breakpoints) {
				// Make a copy of the breakpoints table since it can be modified while iterating
				Dictionary<BreakEvent, BreakEventInfo> breakpointsCopy = new Dictionary<BreakEvent, BreakEventInfo> (breakpoints);
				foreach (KeyValuePair<BreakEvent, BreakEventInfo> bps in breakpointsCopy) {
					Breakpoint bp = bps.Key as Breakpoint;
					if (bp != null && bps.Value.Status == BreakEventStatus.NotBound) {
						if (string.Compare (System.IO.Path.GetFullPath (bp.FileName), fullFilePath, System.IO.Path.DirectorySeparatorChar == '\\') == 0)
							RetryEventBind (bps.Value);
					}
				}
			}
		}
		
		void RetryEventBind (BreakEventInfo binfo)
		{
			// Try inserting the breakpoint again
			BreakEvent be = binfo.BreakEvent;
			try {
				binfo = OnInsertBreakEvent (be);
				if (binfo == null)
					throw new InvalidOperationException ("OnInsertBreakEvent can't return a null value. If the breakpoint can't be bound or is invalid, a BreakEventInfo with the corresponding status must be returned");
				lock (breakpoints) {
					breakpoints [be] = binfo;
				}
				binfo.AttachSession (this, be);
			} catch (Exception ex) {
				Breakpoint bp = be as Breakpoint;
				if (bp != null)
					OnDebuggerOutput (false, "Could not set breakpoint at location '" + bp.FileName + ":" + bp.Line + " (" + ex.Message + ")\n");
				else
					OnDebuggerOutput (false, "Could not set catchpoint for exception '" + ((Catchpoint)be).ExceptionName + "' (" + ex.Message + ")\n");
				HandleException (ex);
			}
		}
		
		/// <summary>
		/// Unbinds all bound breakpoints of a source file
		/// </summary>
		/// <param name='fullFilePath'>
		/// The source file path
		/// </param>
		/// <remarks>
		/// This method can be called by a subclass to ask the debugger session to
		/// unbind all bound breakpoints defined on the given file. This method could
		/// be called, for example, when an assembly that contains this file is unloaded
		/// from memory. It is not necessary to use this method if the subclass keeps
		/// track of unbound breakpoints by itself.
		/// </remarks>
		internal protected void UnbindSourceFileBreakpoints (string fullFilePath)
		{
			List<BreakEventInfo> toUpdate = new List<BreakEventInfo> ();
			lock (breakpoints) {
				// Make a copy of the breakpoints table since it can be modified while iterating
				Dictionary<BreakEvent, BreakEventInfo> breakpointsCopy = new Dictionary<BreakEvent, BreakEventInfo> (breakpoints);
				foreach (KeyValuePair<BreakEvent, BreakEventInfo> bps in breakpointsCopy) {
					Breakpoint bp = bps.Key as Breakpoint;
					if (bp != null && bps.Value.Status == BreakEventStatus.Bound) {
						if (System.IO.Path.GetFullPath (bp.FileName) == fullFilePath)
							toUpdate.Add (bps.Value);
					}
				}
				foreach (BreakEventInfo be in toUpdate) {
					breakpoints.Remove (be.BreakEvent);
					NotifyBreakEventStatusChanged (be.BreakEvent);
				}
			}
		}
		
		internal void NotifyBreakEventStatusChanged (BreakEvent be)
		{
			var s = GetBreakEventStatus (be);
			if (s == BreakEventStatus.BindError || s == BreakEventStatus.Invalid)
				OnDebuggerOutput (true, GetBreakEventErrorMessage (be) + ": " + GetBreakEventStatusMessage (be) + "\n");
			Breakpoints.NotifyStatusChanged (be);
		}
		
		string GetBreakEventErrorMessage (BreakEvent be)
		{
			Breakpoint bp = be as Breakpoint;
			if (bp != null)
				return string.Format ("Could not insert breakpoint at '{0}:{1}'", bp.FileName, bp.Line);
			Catchpoint cp = (Catchpoint) be;
			return string.Format ("Could not enable catchpoint for exception '{0}'", cp.ExceptionName);
		}
		
		/// <summary>
		/// Reports an unhandled exception in the debugger
		/// </summary>
		/// <returns>
		/// True if the debugger engine handles the exception. False otherwise.
		/// </returns>
		/// <param name='ex'>
		/// The exception
		/// </param>
		/// <remarks>
		/// This method can be used by subclasses to report errors in the debugger that must be reported
		/// to the user.
		/// </remarks>
		protected virtual bool HandleException (Exception ex)
		{
			if (exceptionHandler != null)
				return exceptionHandler (ex);

			return false;
		}
		
		internal void AdjustBreakpointLocation (Breakpoint b, int newLine, int newColumn)
		{
			lock (breakpoints) {
				try {
					adjustingBreakpoints = true;
					Breakpoints.AdjustBreakpointLine (b, newLine, newColumn);
				} finally {
					adjustingBreakpoints = false;
				}
			}
		}

		/// <summary>
		/// When set, operations such as OnRun, OnAttachToProcess, OnStepLine, etc, are run in
		/// a background thread, so it will not block the caller of the corresponding public methods.
		/// </summary>
		protected bool UseOperationThread { get; set; }
		
		/// <summary>
		/// Called to start the execution of the debugger
		/// </summary>
		/// <param name='startInfo'>
		/// Startup information
		/// </param>
		protected abstract void OnRun (DebuggerStartInfo startInfo);
		
		/// <summary>
		/// Called to attach the debugger to a running process
		/// </summary>
		/// <param name='processId'>
		/// Process identifier.
		/// </param>
		protected abstract void OnAttachToProcess (long processId);

		/// <summary>
		/// Called to detach the debugging session from the running process
		/// </summary>
		protected abstract void OnDetach ();
		
		/// <summary>
		/// Called when the active thread has to be changed
		/// </summary>
		/// <param name='processId'>
		/// Process identifier.
		/// </param>
		/// <param name='threadId'>
		/// Thread identifier.
		/// </param>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnSetActiveThread (long processId, long threadId);
		
		/// <summary>
		/// Called when the debug session has to be paused
		/// </summary>
		protected abstract void OnStop ();
		
		/// <summary>
		/// Called when the target process has to be exited
		/// </summary>
		protected abstract void OnExit ();
		
		
		/// <summary>
		/// Called to step one source code line
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnStepLine ();

		/// <summary>
		/// Called to step one source line, but step over method calls
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnNextLine ();

		/// <summary>
		/// Called to step one instruction
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnStepInstruction ();

		/// <summary>
		// Called to step one instruction, but step over method calls
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnNextInstruction ();

		/// <summary>
		/// Called to continue execution until leaving the current method
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnFinish ();

		/// <summary>
		/// Called to resume execution
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract void OnContinue ();
		
		//breakpoints etc

		/// <summary>
		/// Called to insert a new breakpoint or catchpoint
		/// </summary>
		/// <param name='breakEvent'>
		/// The break event.
		/// </param>
		/// <remarks>
		/// Implementations of this method must: (1) create (and return) an instance of BreakEventInfo
		/// (or a subclass of it). (2) Attempt to activate a breakpoint at the location
		/// specified in breakEvent. If the breakpoint cannot be activated at the time this
		/// method is called, it is the responsibility of the DebuggerSession subclass
		/// to attempt it later on.
		/// The BreakEventInfo object can be used to change the status of the breakpoint,
		/// update the hit point, etc.
		/// </remarks>
		protected abstract BreakEventInfo OnInsertBreakEvent (BreakEvent breakEvent);
		
		/// <summary>
		/// Called when a breakpoint has been removed.
		/// </summary>
		/// <param name='eventInfo'>
		/// The breakpoint
		/// </param>
		/// <remarks>
		/// Implementations of this method should remove or disable the breakpoint
		/// in the debugging engine.
		/// </remarks>
		protected abstract void OnRemoveBreakEvent (BreakEventInfo eventInfo);

		/// <summary>
		/// Called when information about a breakpoint has changed
		/// </summary>
		/// <param name='eventInfo'>
		/// The break event
		/// </param>
		/// <remarks>
		/// This method is called when some information about the breakpoint changes.
		/// Notice that the file and line of a breakpoint or the exception name of
		/// a catchpoint can't be modified. Changes of the Enabled property are
		/// notified by calling OnEnableBreakEvent. 
		/// </remarks>
		protected abstract void OnUpdateBreakEvent (BreakEventInfo eventInfo);
		
		/// <summary>
		/// Called when a break event is enabled or disabled
		/// </summary>
		/// <param name='eventInfo'>
		/// The break event
		/// </param>
		/// <param name='enable'>
		/// The new status
		/// </param>
		protected abstract void OnEnableBreakEvent (BreakEventInfo eventInfo, bool enable);
		
		/// <summary>
		/// Queried when the debugger session has to check if changes in breakpoints are allowed or not
		/// </summary>
		/// <value>
		/// <c>true</c> if break event changes are allowed; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		/// This property should return false if at the time it is invoked the debugger engine doesn't support
		/// adding, removing or doing changes in breakpoints.
		/// </remarks>
		protected virtual bool AllowBreakEventChanges { get { return true; } }
		
		/// <summary>
		/// Called to get a list of the threads of a process
		/// </summary>
		/// <param name='processId'>
		/// Process identifier.
		/// </param>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract ThreadInfo[] OnGetThreads (long processId);
		
		/// <summary>
		/// Called to get a list of all debugee processes
		/// </summary>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract ProcessInfo[] OnGetProcesses ();
		
		/// <summary>
		/// Called to get the backtrace of a thread
		/// </summary>
		/// <param name='processId'>
		/// Process identifier.
		/// </param>
		/// <param name='threadId'>
		/// Thread identifier.
		/// </param>
		/// <remarks>
		/// This method can only be called when the debuggee is stopped by the debugger
		/// </remarks>
		protected abstract Backtrace OnGetThreadBacktrace (long processId, long threadId);
		
		/// <summary>
		/// Called to gets the disassembly of a source code file
		/// </summary>
		/// <returns>
		/// An array of AssemblyLine, with one element for each source code line that could be disassembled
		/// </returns>
		/// <param name='file'>
		/// The file.
		/// </param>
		/// <remarks>
		/// This method can only be used when the debuggee is stopped by the debugger
		/// </remarks>
		protected virtual AssemblyLine[] OnDisassembleFile (string file)
		{
			return null;
		}
		
		protected IDebuggerSessionFrontend Frontend {
			get {
				return frontend;
			}
		}
	}
	
	class InternalDebuggerSession: IDebuggerSessionFrontend
	{
		DebuggerSession session;
		
		public InternalDebuggerSession (DebuggerSession session)
		{
			this.session = session;
		}
		
		public void NotifyTargetEvent (TargetEventArgs args)
		{
			session.OnTargetEvent (args);
		}

		public void NotifyTargetOutput (bool isStderr, string text)
		{
			session.OnTargetOutput (isStderr, text);
		}
		
		public void NotifyDebuggerOutput (bool isStderr, string text)
		{
			session.OnDebuggerOutput (isStderr, text);
		}
		
		public void NotifyStarted (ThreadInfo t)
		{
			session.OnStarted (t);
		}
		
		public void NotifyStarted ()
		{
			session.OnStarted ();
		}
		
		public void BindSourceFileBreakpoints (string fullFilePath)
		{
			session.BindSourceFileBreakpoints (fullFilePath);
		}

		public void UnbindSourceFileBreakpoints (string fullFilePath)
		{
			session.UnbindSourceFileBreakpoints (fullFilePath);
		}
	}

	public delegate void OutputWriterDelegate (bool isStderr, string text);

	public class BusyStateEventArgs: EventArgs
	{
		public bool IsBusy { get; internal set; }
		
		public string Description { get; internal set; }
	}
	
	public interface IConnectionDialog : IDisposable
	{
		event EventHandler UserCancelled;
		
		//message may be null in which case the dialog should construct a default
		void SetMessage (DebuggerStartInfo dsi, string message, bool listening, int attemptNumber);
	}
}

