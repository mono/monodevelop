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
	
	public abstract class DebuggerSession: IDisposable
	{
		InternalDebuggerSession frontend;
		Dictionary<BreakEvent,object> breakpoints = new Dictionary<BreakEvent,object> ();
		bool isRunning;
		bool started;
		BreakpointStore breakpointStore;
		OutputWriterDelegate outputWriter;
		OutputWriterDelegate logWriter;
		bool disposed;
		bool attached;
		object slock = new object ();
		object olock = new object ();
		ThreadInfo activeThread;
		BreakEventHitHandler customBreakpointHitHandler;
		ExceptionHandler exceptionHandler;
		DebuggerSessionOptions options;
		Dictionary<string,string> resolvedExpressionCache = new Dictionary<string, string> ();
		
		ProcessInfo[] currentProcesses;
		
		public event EventHandler<TargetEventArgs> TargetEvent;
		
		public event EventHandler TargetStarted;
		public event EventHandler<TargetEventArgs> TargetStopped;
		public event EventHandler<TargetEventArgs> TargetInterrupted;
		public event EventHandler<TargetEventArgs> TargetHitBreakpoint;
		public event EventHandler<TargetEventArgs> TargetSignaled;
		public event EventHandler TargetExited;
		public event EventHandler<TargetEventArgs> TargetExceptionThrown;
		public event EventHandler<TargetEventArgs> TargetUnhandledException;
		
		public event EventHandler<BusyStateEventArgs> BusyStateChanged;
		
		public DebuggerSession ()
		{
			UseOperationThread = true;
			frontend = new InternalDebuggerSession (this);
		}
		
		public void Initialize ()
		{
		}
		
		public virtual void Dispose ()
		{
			lock (slock) {
				if (!disposed) {
					disposed = true;
					Breakpoints = null;
				}
			}
		}
		
		public ExceptionHandler ExceptionHandler {
			get { return exceptionHandler; }
			set { exceptionHandler = value; }
		}
		
		public TypeResolverHandler TypeResolverHandler { get; set; }

		public BreakpointStore Breakpoints {
			get {
				lock (slock) {
					if (breakpointStore == null)
						Breakpoints = new BreakpointStore ();
					return breakpointStore;
				}
			}
			set {
				lock (slock) {
					if (breakpointStore != null) {
						if (started) {
							foreach (BreakEvent bp in breakpointStore)
								RemoveBreakEvent (bp);
						}
						breakpointStore.BreakEventAdded -= OnBreakpointAdded;
						breakpointStore.BreakEventRemoved -= OnBreakpointRemoved;
						breakpointStore.BreakEventModified -= OnBreakpointModified;
						breakpointStore.BreakEventEnableStatusChanged -= OnBreakpointStatusChanged;
						breakpointStore.CheckingReadOnly -= BreakpointStoreCheckingReadOnly;
					}
					
					breakpointStore = value;
					
					if (breakpointStore != null) {
						if (started) {
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

		public BreakEventHitHandler CustomBreakEventHitHandler {
			get {
				return customBreakpointHitHandler;
			}
			set {
				customBreakpointHitHandler = value;
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
		
		public bool AttachedToProcess {
			get {
				lock (slock) {
					return attached; 
				}
			}
		}
		
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
		
		public bool IsBreakEventValid (BreakEvent be)
		{
			if (!started)
				return true;
			
			object handle;
			lock (breakpoints) {
				return (breakpoints.TryGetValue (be, out handle) && handle != null);
			}
		}

		void AddBreakEvent (BreakEvent be)
		{
			object handle = null;
			
			try {
				handle = OnInsertBreakEvent (be, be.Enabled);
			} catch (Exception ex) {
				Breakpoint bp = be as Breakpoint;
				if (bp != null)
					OnDebuggerOutput (false, "Could not set breakpoint at location '" + bp.FileName + ":" + bp.Line + "' (" + ex.Message + ")\n");
				else
					OnDebuggerOutput (false, "Could not set catchpoint for exception '" + ((Catchpoint)be).ExceptionName + "' (" + ex.Message + ")\n");
				HandleException (ex);
				return;
			}

			lock (breakpoints) {
				breakpoints.Add (be, handle);
				Breakpoints.NotifyStatusChanged (be);
			}
		}

		bool RemoveBreakEvent (BreakEvent be)
		{
			lock (breakpoints) {
				object handle;
				if (GetBreakpointHandle (be, out handle)) {
					try {
						if (handle != null)
							OnRemoveBreakEvent (handle);
						breakpoints.Remove (be);
					} catch (Exception ex) {
						if (started)
							OnDebuggerOutput (false, ex.Message);
						HandleException (ex);
						return false;
					}
				}
				return true;
			}
		}
		
		void UpdateBreakEventStatus (BreakEvent be)
		{
			lock (breakpoints) {
				object handle;
				if (GetBreakpointHandle (be, out handle) && handle != null) {
					try {
						OnEnableBreakEvent (handle, be.Enabled);
					} catch (Exception ex) {
						if (started)
							OnDebuggerOutput (false, ex.Message);
						HandleException (ex);
					}
				}
			}
		}
		
		void UpdateBreakEvent (BreakEvent be)
		{
			lock (breakpoints) {
				object handle;
				if (GetBreakpointHandle (be, out handle)) {
					if (handle != null) {
						object newHandle = OnUpdateBreakEvent (handle, be);
						if (newHandle != handle && (newHandle == null || !newHandle.Equals (handle))) {
							// Update the handle if it has changed, and notify the status change
							breakpoints [be] = newHandle;
						}
						Breakpoints.NotifyStatusChanged (be);
					} else {
						// Try inserting the breakpoint again
						try {
							handle = OnInsertBreakEvent (be, be.Enabled);
							if (handle != null) {
								// This time worked
								breakpoints [be] = handle;
								Breakpoints.NotifyStatusChanged (be);
							}
						} catch (Exception ex) {
							Breakpoint bp = be as Breakpoint;
							if (bp != null)
								OnDebuggerOutput (false, "Could not set breakpoint at location '" + bp.FileName + ":" + bp.Line + " (" + ex.Message + ")\n");
							else
								OnDebuggerOutput (false, "Could not set catchpoint for exception '" + ((Catchpoint)be).ExceptionName + "' (" + ex.Message + ")\n");
							HandleException (ex);
						}
					}
				}
			}
		}
		
		void OnBreakpointAdded (object s, BreakEventArgs args)
		{
			lock (slock) {
				if (started)
					AddBreakEvent (args.BreakEvent);
			}
		}
		
		void OnBreakpointRemoved (object s, BreakEventArgs args)
		{
			lock (slock) {
				if (started)
					RemoveBreakEvent (args.BreakEvent);
			}
		}
		
		void OnBreakpointModified (object s, BreakEventArgs args)
		{
			lock (slock) {
				if (started)
					UpdateBreakEvent (args.BreakEvent);
			}
		}
		
		void OnBreakpointStatusChanged (object s, BreakEventArgs args)
		{
			lock (slock) {
				if (started)
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
		
		protected bool GetBreakpointHandle (BreakEvent be, out object handle)
		{
			return breakpoints.TryGetValue (be, out handle);
		}
		
		public DebuggerSessionOptions Options {
			get { return options; }
		}

		public EvaluationOptions EvaluationOptions {
			get { return options.EvaluationOptions; }
		}

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

		public bool IsRunning {
			get {
				return isRunning;
			}
		}
		
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
		
		public OutputWriterDelegate OutputWriter {
			get { return outputWriter; }
			set {
				lock (olock) {
					outputWriter = value;
				}
			}
		}
		
		public OutputWriterDelegate LogWriter {
			get { return logWriter; }
			set {
				lock (olock) {
					logWriter = value;
				}
			}
		}

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
					resolved = OnResolveExpression (expression, location);
					resolvedExpressionCache [key] = resolved;
				}
				return resolved;
			}
		}
		
		Mono.Debugging.Evaluation.NRefactoryEvaluator defaultResolver = new Mono.Debugging.Evaluation.NRefactoryEvaluator ();
		
		protected virtual string OnResolveExpression (string expression, SourceLocation location)
		{
			return defaultResolver.Resolve (this, location, expression);
		}
		
		internal protected string ResolveIdentifierAsType (string identifier, SourceLocation location)
		{
			if (TypeResolverHandler != null)
				return TypeResolverHandler (identifier, location);
			else
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
			
			switch (args.Type) {
				case TargetEventType.ExceptionThrown:
					lock (slock) {
						isRunning = false;
					}
					if (TargetExceptionThrown != null)
						TargetExceptionThrown (this, args);
					break;
				case TargetEventType.TargetExited:
					lock (slock) {
						isRunning = false;
						started = false;
						foreach (BreakEvent bp in Breakpoints)
							Breakpoints.NotifyStatusChanged (bp);
					}
					if (TargetExited != null)
						TargetExited (this, args);
					break;
				case TargetEventType.TargetHitBreakpoint:
					lock (slock) {
						isRunning = false;
					}
					if (TargetHitBreakpoint != null)
						TargetHitBreakpoint (this, args);
					break;
				case TargetEventType.TargetInterrupted:
					lock (slock) {
						isRunning = false;
					}
					if (TargetInterrupted != null)
						TargetInterrupted (this, args);
					break;
				case TargetEventType.TargetSignaled:
					lock (slock) {
						isRunning = false;
					}
					if (TargetSignaled != null)
						TargetSignaled (this, args);
					break;
				case TargetEventType.TargetStopped:
					lock (slock) {
						isRunning = false;
					}
					if (TargetStopped != null)
						TargetStopped (this, args);
					break;
				case TargetEventType.UnhandledException:
					lock (slock) {
						isRunning = false;
					}
					if (TargetUnhandledException != null)
						TargetUnhandledException (this, args);
					break;
			}
			if (TargetEvent != null)
				TargetEvent (this, args);
		}
		
		internal void OnRunning ()
		{
			isRunning = true;
			if (TargetStarted != null)
				TargetStarted (this, EventArgs.Empty);
		}
		
		internal protected void OnStarted ()
		{
			lock (slock) {
				started = true;
				foreach (BreakEvent bp in breakpointStore)
					AddBreakEvent (bp);
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
		
		internal protected void NotifySourceFileLoaded (string fullFilePath)
		{
			lock (breakpoints) {
				// Make a copy of the breakpoints table since it can be modified while iterating
				Dictionary<BreakEvent, object> breakpointsCopy = new Dictionary<BreakEvent, object> (breakpoints);
				foreach (KeyValuePair<BreakEvent, object> bps in breakpointsCopy) {
					Breakpoint bp = bps.Key as Breakpoint;
					if (bp != null && bps.Value == null) {
						if (string.Compare (System.IO.Path.GetFullPath (bp.FileName), fullFilePath, System.IO.Path.DirectorySeparatorChar == '\\') == 0)
							UpdateBreakEvent (bp);
					}
				}
			}
		}

		internal protected void NotifySourceFileUnloaded (string fullFilePath)
		{
			List<BreakEvent> toUpdate = new List<BreakEvent> ();
			lock (breakpoints) {
				// Make a copy of the breakpoints table since it can be modified while iterating
				Dictionary<BreakEvent, object> breakpointsCopy = new Dictionary<BreakEvent, object> (breakpoints);
				foreach (KeyValuePair<BreakEvent, object> bps in breakpointsCopy) {
					Breakpoint bp = bps.Key as Breakpoint;
					if (bp != null && bps.Value != null) {
						if (System.IO.Path.GetFullPath (bp.FileName) == fullFilePath)
							toUpdate.Add (bp);
					}
				}
				foreach (BreakEvent be in toUpdate) {
					breakpoints[be] = null;
					Breakpoints.NotifyStatusChanged (be);
				}
			}
		}

		BreakEvent GetBreakEvent (object handle)
		{
			foreach (KeyValuePair<BreakEvent,object> e in breakpoints) {
				if (handle == e.Value || handle.Equals (e.Value))
					return e.Key;
			}
			return null;
		}
		
		protected virtual bool HandleException (Exception ex)
		{
			if (exceptionHandler != null)
				return exceptionHandler (ex);
			else
				return false;
		}
		
		internal protected bool OnCustomBreakpointAction (string actionId, object handle)
		{
			BreakEvent ev = GetBreakEvent (handle);
			return ev != null && customBreakpointHitHandler (actionId, ev);
		}
		
		protected void UpdateHitCount (object breakEventHandle, int count)
		{
			BreakEvent ev = GetBreakEvent (breakEventHandle);
			if (ev != null) {
				ev.HitCount = count;
				ev.NotifyUpdate ();
			}
		}
		
		protected void UpdateLastTraceValue (object breakEventHandle, string value)
		{
			BreakEvent ev = GetBreakEvent (breakEventHandle);
			if (ev != null) {
				ev.LastTraceValue = value;
				ev.NotifyUpdate ();
			}
		}
		
		/// <summary>
		/// When set, operations such as OnRun, OnAttachToProcess, OnStepLine, etc, are run in
		/// a background thread, so it will not block the caller of the corresponding public methods.
		/// </summary>
		protected bool UseOperationThread { get; set; }
		
		protected abstract void OnRun (DebuggerStartInfo startInfo);

		protected abstract void OnAttachToProcess (long processId);
		
		protected abstract void OnDetach ();
		
		protected abstract void OnSetActiveThread (long processId, long threadId);

		protected abstract void OnStop ();
		
		protected abstract void OnExit ();

		// Step one source line
		protected abstract void OnStepLine ();

		// Step one source line, but step over method calls
		protected abstract void OnNextLine ();

		// Step one instruction
		protected abstract void OnStepInstruction ();

		// Step one instruction, but step over method calls
		protected abstract void OnNextInstruction ();

		// Continue until leaving the current method
		protected abstract void OnFinish ();

		//breakpoints etc

		// returns a handle
		protected abstract object OnInsertBreakEvent (BreakEvent be, bool activate);

		protected abstract void OnRemoveBreakEvent (object handle);
		
		protected abstract object OnUpdateBreakEvent (object handle, BreakEvent be);
		
		protected abstract void OnEnableBreakEvent (object handle, bool enable);
		
		protected virtual bool AllowBreakEventChanges { get { return true; } }

		protected abstract void OnContinue ();
		
		protected abstract ThreadInfo[] OnGetThreads (long processId);
		
		protected abstract ProcessInfo[] OnGetProcesses ();
		
		protected abstract Backtrace OnGetThreadBacktrace (long processId, long threadId);

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
		
		public void NotifyStarted ()
		{
			session.OnStarted ();
		}
		
		public bool NotifyCustomBreakpointAction (string actionId, object handle)
		{
			return session.OnCustomBreakpointAction (actionId, handle);
		}
		
		public void NotifySourceFileLoaded (string fullFilePath)
		{
			session.NotifySourceFileLoaded (fullFilePath);
		}

		public void NotifySourceFileUnloaded (string fullFilePath)
		{
			session.NotifySourceFileUnloaded (fullFilePath);
		}
	}

	public delegate void OutputWriterDelegate (bool isStderr, string text);

	public class BusyStateEventArgs: EventArgs
	{
		public bool IsBusy { get; internal set; }
		
		public string Description { get; internal set; }
	}
}
