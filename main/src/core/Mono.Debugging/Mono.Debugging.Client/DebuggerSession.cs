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

namespace Mono.Debugging.Client
{
	public delegate void TargetEventHandler (object sender, TargetEventArgs args);
	public delegate void ProcessEventHandler(int process_id);
	public delegate void ThreadEventHandler(int thread_id);
	
	public abstract class DebuggerSession: IDisposable
	{
		InternalDebuggerSession frontend;
		Dictionary<Breakpoint,int> breakpoints = new Dictionary<Breakpoint,int> ();
		bool isRunning;
		bool started;
		BreakpointStore breakpointStore;
		OutputWriterDelegate outputWriter;
		OutputWriterDelegate logWriter;
		bool disposed;
		bool attached;
		object slock = new object ();
		
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
		
		public DebuggerSession ()
		{
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
							foreach (Breakpoint bp in breakpointStore)
								RemoveBreakpoint (bp);
						}
						breakpointStore.BreakpointAdded -= OnBreakpointAdded;
						breakpointStore.BreakpointRemoved -= OnBreakpointRemoved;
						breakpointStore.BreakpointStatusChanged -= OnBreakpointStatusChanged;
					}
					
					breakpointStore = value;
					
					if (breakpointStore != null) {
						if (started) {
							foreach (Breakpoint bp in breakpointStore)
								AddBreakpoint (bp);
						}
						breakpointStore.BreakpointAdded += OnBreakpointAdded;
						breakpointStore.BreakpointRemoved += OnBreakpointRemoved;
						breakpointStore.BreakpointStatusChanged += OnBreakpointStatusChanged;
					}
				}
			}
		}
		
		public void Run (DebuggerStartInfo startInfo)
		{
			lock (slock) {
				OnRunning ();
				try {
					OnRun (startInfo);
				} catch {
					ForceStop ();
					throw;
				}
			}
		}
		
		public void AttachToProcess (ProcessInfo proc)
		{
			lock (slock) {
				OnRunning ();
				try {
					OnAttachToProcess (proc.Id);
					attached = true;
				} catch {
					ForceStop ();
					throw;
				}
			}
		}
		
		public void Detach ()
		{
			lock (slock) {
				OnDetach ();
			}
		}
		
		public bool AttachedToProcess {
			get {
				lock (slock) {
					return attached; 
				}
			}
		}
		
		public void NextLine ()
		{
			lock (slock) {
				OnRunning ();
				try {
					OnNextLine ();
				} catch {
					ForceStop ();
					throw;
				}
			}
		}

		public void StepLine ()
		{
			lock (slock) {
				OnRunning ();
				try {
					OnStepLine ();
				} catch {
					ForceStop ();
					throw;
				}
			}
		}
		
		public void NextInstruction ()
		{
			lock (slock) {
				OnRunning ();
				try {
					OnNextInstruction ();
				} catch {
					ForceStop ();
					throw;
				}
			}
		}

		public void StepInstruction ()
		{
			lock (slock) {
				OnRunning ();
				try {
					OnStepInstruction ();
				} catch {
					ForceStop ();
					throw;
				}
			}
		}

		public void Finish ()
		{
			lock (slock) {
				OnRunning ();
				try {
					OnFinish ();
				} catch {
					ForceStop ();
					throw;
				}
			}
		}

		void AddBreakpoint (Breakpoint bp)
		{
			int handle = OnInsertBreakpoint (bp.FileName, bp.Line, bp.Enabled);
			breakpoints.Add (bp, handle);
		}

		void RemoveBreakpoint (Breakpoint bp)
		{
			int handle;
			if (GetHandle (bp, out handle)) {
				breakpoints.Remove (bp);
				OnRemoveBreakpoint (handle);
			}
		}
		
		void UpdateBreakpoint (Breakpoint bp)
		{
			int handle;
			if (GetHandle (bp, out handle))
				OnEnableBreakpoint (handle, bp.Enabled);
		}
		
		void OnBreakpointAdded (object s, BreakpointEventArgs args)
		{
			lock (slock) {
				if (started)
					AddBreakpoint (args.Breakpoint);
			}
		}
		
		void OnBreakpointRemoved (object s, BreakpointEventArgs args)
		{
			lock (slock) {
				if (started)
					RemoveBreakpoint (args.Breakpoint);
			}
		}
		
		void OnBreakpointStatusChanged (object s, BreakpointEventArgs args)
		{
			lock (slock) {
				if (started)
					UpdateBreakpoint (args.Breakpoint);
			}
		}
		
		bool GetHandle (Breakpoint bp, out int handle)
		{
			return breakpoints.TryGetValue (bp, out handle);
		}

		public void Continue ()
		{
			lock (slock) {
				OnRunning ();
				try {
					OnContinue ();
				} catch {
					ForceStop ();
					throw;
				}
			}
		}

		public void Stop ()
		{
			lock (slock) {
				OnStop ();
			}
		}

		public void Exit ()
		{
			lock (slock) {
				OnExit ();
			}
		}

		public bool IsRunning {
			get {
				lock (slock) {
					return isRunning;
				}
			}
		}
		
		public ProcessInfo[] GetPocesses ()
		{
			lock (slock) {
				if (currentProcesses == null) {
					currentProcesses = OnGetPocesses ();
					foreach (ProcessInfo p in currentProcesses)
						p.Attach (this);
				}
				return currentProcesses;
			}
		}
		
		public OutputWriterDelegate OutputWriter {
			get { return outputWriter; }
			set { outputWriter = value; }
		}
		
		public OutputWriterDelegate LogWriter {
			get { return logWriter; }
			set {
				lock (slock) {
					logWriter = value; 
				}
			}
		}

		internal ThreadInfo[] GetThreads (int processId)
		{
			lock (slock) {
				ThreadInfo[] threads = OnGetThreads (processId);
				foreach (ThreadInfo t in threads)
					t.Attach (this);
				return threads;
			}
		}
		
		internal Backtrace GetBacktrace (int threadId)
		{
			lock (slock) {
				Backtrace bt = OnGetThreadBacktrace (threadId);
				bt.Attach (this);
				return bt;
			}
		}
		
		void ForceStop ()
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetStopped);
			OnTargetEvent (args);
		}
		
		internal protected void OnTargetEvent (TargetEventArgs args)
		{
			if (args.Process != null)
				args.Process.Attach (this);
			if (args.Thread != null)
				args.Thread.Attach (this);
			
			switch (args.Type) {
				case TargetEventType.Exception:
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
				foreach (Breakpoint bp in breakpointStore)
					AddBreakpoint (bp);
			}
		}
		
		internal protected void OnTargetOutput (bool isStderr, string text)
		{
			lock (slock) {
				if (outputWriter != null)
					outputWriter (isStderr, text);
			}
		}
		
		internal protected void OnDebuggerOutput (bool isStderr, string text)
		{
			lock (slock) {
				if (logWriter != null)
					logWriter (isStderr, text);
			}
		}
		
		protected abstract void OnRun (DebuggerStartInfo startInfo);

		protected abstract void OnAttachToProcess (int processId);
		
		protected abstract void OnDetach ();

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
		protected abstract int OnInsertBreakpoint (string filename, int line, bool activate);

		protected abstract void OnRemoveBreakpoint (int handle);
		
		protected abstract void OnEnableBreakpoint (int handle, bool enable);

		protected abstract void OnContinue ();
		
		protected abstract ThreadInfo[] OnGetThreads (int processId);
		
		protected abstract ProcessInfo[] OnGetPocesses ();
		
		protected abstract Backtrace OnGetThreadBacktrace (int threadId);
		
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
	}

	public delegate void OutputWriterDelegate (bool isStderr, string text);
}
