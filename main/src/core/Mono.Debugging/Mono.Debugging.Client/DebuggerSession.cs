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
		IDebuggerSessionBackend debugger;
		InternalDebuggerSession frontend;
		Dictionary<Breakpoint,int> breakpoints = new Dictionary<Breakpoint,int> ();
		bool isRunning;
		bool started;
		BreakpointStore breakpointStore;
		
		public event EventHandler<TargetEventArgs> TargetEvent;
		
		public event EventHandler TargetStarted;
		public event EventHandler<TargetEventArgs> TargetStopped;
		public event EventHandler<TargetEventArgs> TargetInterrupted;
		public event EventHandler<TargetEventArgs> TargetHitBreakpoint;
		public event EventHandler<TargetEventArgs> TargetSignaled;
		public event EventHandler TargetExited;
		public event EventHandler<TargetEventArgs> TargetFrameChanged;
		public event EventHandler<TargetEventArgs> TargetExceptionThrown;
		public event EventHandler<TargetEventArgs> TargetUnhandledException;
		
		public event EventHandler<ProcessEventArgs> MainProcessCreated;
		public event EventHandler<ProcessEventArgs> ProcessCreated;
		public event EventHandler<ProcessEventArgs> ProcessExited;
		public event EventHandler<ProcessEventArgs> ProcessExecd;
		
		public event EventHandler<ThreadEventArgs> ThreadCreated;
		public event EventHandler<ThreadEventArgs> ThreadExited;
		
		public DebuggerSession ()
		{
			frontend = new InternalDebuggerSession (this);
		}
		
		public void Initialize ()
		{
			debugger = CreateBackend ();
		}
		
		public void Dispose ()
		{
			Breakpoints = null;
		}
		
		public BreakpointStore Breakpoints {
			get {
				if (breakpointStore == null)
					Breakpoints = new BreakpointStore ();
				return breakpointStore;
			}
			set {
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
		
		public void Run (DebuggerStartInfo startInfo)
		{
			OnRunning ();
			debugger.Run (startInfo);
		}
		
		public void NextLine ()
		{
			OnRunning ();
			debugger.NextLine ();
		}

		public void StepLine ()
		{
			OnRunning ();
			debugger.StepLine ();
		}

		public void Finish ()
		{
			OnRunning ();
			debugger.Finish ();
		}

		void AddBreakpoint (Breakpoint bp)
		{
			int handle = debugger.InsertBreakpoint (bp.FileName, bp.Line, bp.Enabled);
			breakpoints.Add (bp, handle);
		}

		void RemoveBreakpoint (Breakpoint bp)
		{
			int handle;
			if (GetHandle (bp, out handle)) {
				breakpoints.Remove (bp);
				debugger.RemoveBreakpoint (handle);
			}
		}
		
		void UpdateBreakpoint (Breakpoint bp)
		{
			int handle;
			if (GetHandle (bp, out handle))
				debugger.EnableBreakpoint (handle, bp.Enabled);
		}
		
		void OnBreakpointAdded (object s, BreakpointEventArgs args)
		{
			if (started)
				AddBreakpoint (args.Breakpoint);
		}
		
		void OnBreakpointRemoved (object s, BreakpointEventArgs args)
		{
			if (started)
				RemoveBreakpoint (args.Breakpoint);
		}
		
		void OnBreakpointStatusChanged (object s, BreakpointEventArgs args)
		{
			if (started)
				UpdateBreakpoint (args.Breakpoint);
		}
		
/*		bool GetBreakpoint (int handle, out Breakpoint bp)
		{
			foreach (KeyValuePair<Breakpoint,int> entry in breakpoints) {
				if (entry.Value == handle) {
					bp = entry.Key;
					return true;
				}
			}
			bp = null;
			return false;
		}
*/			
		bool GetHandle (Breakpoint bp, out int handle)
		{
			return breakpoints.TryGetValue (bp, out handle);
		}

		public void Continue ()
		{
			OnRunning ();
			debugger.Continue ();
		}

		public void Stop ()
		{
			debugger.Stop ();
		}

		public void Exit ()
		{
			debugger.Exit ();
		}

		internal void OnTargetEvent (TargetEventArgs args)
		{
			switch (args.Type) {
				case TargetEventType.Exception:
					isRunning = false;
					if (TargetExceptionThrown != null)
						TargetExceptionThrown (this, args);
					break;
				case TargetEventType.FrameChanged:
					if (TargetFrameChanged != null)
						TargetFrameChanged (this, args);
					break;
				case TargetEventType.TargetExited:
					isRunning = false;
					started = false;
					if (TargetExited != null)
						TargetExited (this, args);
					break;
				case TargetEventType.TargetHitBreakpoint:
					isRunning = false;
					if (TargetHitBreakpoint != null)
						TargetHitBreakpoint (this, args);
					break;
				case TargetEventType.TargetInterrupted:
					isRunning = false;
					if (TargetInterrupted != null)
						TargetInterrupted (this, args);
					break;
				case TargetEventType.TargetRunning:
					isRunning = true;
					if (TargetStarted != null)
						TargetStarted (this, args);
					break;
				case TargetEventType.TargetSignaled:
					isRunning = false;
					if (TargetSignaled != null)
						TargetSignaled (this, args);
					break;
				case TargetEventType.TargetStopped:
					isRunning = false;
					if (TargetStopped != null)
						TargetStopped (this, args);
					break;
				case TargetEventType.UnhandledException:
					isRunning = false;
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
		
		internal void OnStarted ()
		{
			started = true;
			foreach (Breakpoint bp in breakpointStore) {
				AddBreakpoint (bp);
			}
		}
		
		internal void OnProcessCreated (ProcessEventArgs args)
		{
			if (ProcessCreated != null)
				ProcessCreated (this, args);
		}
		
		internal void OnProcessExited (ProcessEventArgs args)
		{
			if (ProcessExited != null)
				ProcessExited (this, args);
		}
		
		internal void OnProcessExecd (ProcessEventArgs args)
		{
			if (ProcessExecd != null)
				ProcessExecd (this, args);
		}
		
		internal void OnThreadCreated (ThreadEventArgs args)
		{
			if (ThreadCreated != null)
				ThreadCreated (this, args);
		}
		
		internal void OnThreadExited (ThreadEventArgs args)
		{
			if (ThreadExited != null)
				ThreadExited (this, args);
			Dispose ();
		}
		
/*		internal void OnTargetOutput (TargetOutputEventArgs args)
		{
			if (TargetOutput != null)
				TargetOutput (this, args);
		}*/
		
		protected abstract IDebuggerSessionBackend CreateBackend ();

		protected IDebuggerSessionFrontend Frontend {
			get {
				return frontend;
			}
		}

		public bool IsRunning {
			get {
				return isRunning;
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

		public void NotifyProcessCreated (ProcessEventArgs args)
		{
			session.OnProcessCreated (args);
		}

		public void NotifyProcessExited (ProcessEventArgs args)
		{
			session.OnProcessExited (args);
		}

		public void NotifyProcessExecd (ProcessEventArgs args)
		{
			session.OnProcessExecd (args);
		}

		public void NotifyThreadCreated (ThreadEventArgs args)
		{
			session.OnThreadCreated (args);
		}

		public void NotifyThreadExited (ThreadEventArgs args)
		{
			session.OnThreadExited (args);
		}
		
		public void NotifyTargetOutput (bool isStderr, string line)
		{
//			session.OnTargetOutput (isStderr, line);
		}
		
		public void NotifyStarted ()
		{
			session.OnStarted ();
		}
	}

}
