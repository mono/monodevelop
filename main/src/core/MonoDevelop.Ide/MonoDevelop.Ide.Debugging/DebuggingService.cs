// DebuggingService.cs - Debugging service frontend for MonoDebugger
//
//  Author: Mike Kestner <mkesner@ximian.com>
//
// Copyright (c) 2004 Novell, Inc.

using System;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;

using MonoDevelop.Ide.Gui;

using Mono.Debugging.Client;

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */

namespace MonoDevelop.Ide.Debugging
{

	public class DebuggingService
	{
		BreakpointStore breakpoints = new BreakpointStore ();
		
		IConsole console;
		DebugExecutionHandlerFactory executionHandlerFactory;
		
		DebuggerSession session;
		Backtrace currentBacktrace;
		int currentFrame;

		public event EventHandler PausedEvent;
		public event EventHandler ResumedEvent;
		public event EventHandler StoppedEvent;
		public event EventHandler CurrentFrameChanged;
		public event EventHandler ExecutionLocationChanged;

		internal DebuggingService()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory ();
		}

		public IExecutionHandlerFactory GetExecutionHandlerFactory ()
		{
			return executionHandlerFactory;
		}
		
		public DebuggerSession DebuggerSession {
			get { return session; }
		}
		
		public BreakpointStore Breakpoints {
			get { return breakpoints; }
		}
		
		void Cleanup ()
		{
			if (!IsDebugging)
				return;

			if (StoppedEvent != null)
				StoppedEvent (null, new EventArgs ());

			if (session != null) {
				session.Dispose ();
				session = null;
			}
			
			if (console != null) {
				console.Dispose ();
				console = null;
			}
			
			NotifyLocationChanged ();
		}

		public bool IsDebugging {
			get {
				return session != null;
			}
		}

		public bool IsRunning {
			get {
				return IsDebugging && session.IsRunning;
			}
		}

		void target_output (bool is_stderr, string line)
		{
			Console.WriteLine (line);
			console.Out.Write (line);
		}

		void KillApplication (object obj)
		{
			Cleanup ();
		}

		public void Pause ()
		{
			session.Stop ();
		}

		public void Resume ()
		{
			session.Continue ();
			NotifyLocationChanged ();
		}

		public IProcessAsyncOperation Run (string file, IConsole console)
		{
			DebugExecutionHandler h = new DebugExecutionHandler (null);
			return h.Execute (file, null, null, null, console);
		}
		
		internal void InternalRun (string platform, DebuggerStartInfo startInfo, IConsole console)
		{
			this.console = console;
			
			if (platform != null)
				session = DebuggerEngine.CreateDebugSessionForPlatform (platform);
			else
				session = DebuggerEngine.CreateDebugSessionForFile (startInfo.Command);
			
			session.Breakpoints = breakpoints;
			session.Run (startInfo);
			session.TargetEvent += OnTargetEvent;
			session.TargetStarted += OnStarted;
			session.OutputWriter = delegate (bool iserr, string text) {
				if (iserr)
					console.Error.Write (text);
				else
					console.Out.Write (text);
			};

			console.CancelRequested += new EventHandler (OnCancelRequested);
			NotifyLocationChanged ();
		}
		
		void OnStarted (object s, EventArgs a)
		{
			Gtk.Application.Invoke (delegate {
				if (ResumedEvent != null)
					ResumedEvent (null, a);
			});
		}
		
		void OnTargetEvent (object sender, TargetEventArgs args)
		{
			try {
				Console.WriteLine ("OnTargetEvent, type - {0}", args.Type);
				if (args.Type != TargetEventType.TargetExited) {
					SetCurrentBacktrace (args.Backtrace);
				}
				
				switch (args.Type) {
					case TargetEventType.TargetExited:
					case TargetEventType.TargetSignaled:
						KillApplication (null);
						break;
					case TargetEventType.TargetStopped:
					case TargetEventType.TargetRunning:
					case TargetEventType.TargetHitBreakpoint:
					case TargetEventType.TargetInterrupted:
					case TargetEventType.UnhandledException:
						NotifyPaused ();
						break;
					default:
						break;
				}
			} catch (Exception e) {
				Console.WriteLine ("OnTargetEvent, {0}", e.ToString ());
			}

		}

		void NotifyPaused ()
		{
			if (PausedEvent != null)
				PausedEvent (null, EventArgs.Empty);
			NotifyLocationChanged ();
			
			Gtk.Application.Invoke (delegate {
				if (!string.IsNullOrEmpty (CurrentFilename))
					IdeApp.Workbench.OpenDocument (CurrentFilename, CurrentLineNumber, 1, true);
			});
		}
		
		void NotifyLocationChanged ()
		{
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (null, EventArgs.Empty);
		}
		
		void OnCancelRequested (object sender, EventArgs args)
		{
			Stop ();
		}

		public void Stop ()
		{
			if (!IsDebugging)
				return;

			session.Exit ();
			Cleanup ();
		}

		public void StepInto ()
		{
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				throw new Exception ("Can't step unless paused.");

			session.StepLine ();
			NotifyLocationChanged ();
		}

		public void StepOver ()
		{
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				//throw new Exception ("Can't step unless paused.");
				return;

			session.NextLine ();
			NotifyLocationChanged ();
		}

		public void StepOut ()
		{
			if (!IsDebugging)
				return;

			if (IsRunning)
				return;

			session.Finish ();
			NotifyLocationChanged ();
		}

		public string[] Backtrace {
			get {
				if (currentBacktrace == null)
					return null;
				string [] result = new string [currentBacktrace.FrameCount];
				for (int i = 0; i < currentBacktrace.FrameCount; i ++)
					result [i] = currentBacktrace.GetFrame (i).ToString ();
				
				return result;
			}
		}
		
		public Backtrace CurrentBacktrace {
			get { return currentBacktrace; }
		}

		public string CurrentFilename {
			get {
				StackFrame sf = CurrentFrame;
				if (sf != null)
					return sf.SourceLocation.Filename;
				else
					return null;
			}
		}

		public int CurrentLineNumber {
			get {
				StackFrame sf = CurrentFrame;
				if (sf != null)
					return sf.SourceLocation.Line;
				else
					return -1;
			}
		}

		public StackFrame CurrentFrame {
			get {
				if (currentBacktrace != null && currentFrame != -1)
					return currentBacktrace.GetFrame (currentFrame);
				else
					return null;
			}
		}
		
		public int CurrentFrameIndex {
			get {
				return currentFrame;
			}
			set {
				if (currentBacktrace != null && value < currentBacktrace.FrameCount) {
					currentFrame = value;
					Gtk.Application.Invoke (delegate {
						if (CurrentFrameChanged != null)
							CurrentFrameChanged (this, EventArgs.Empty);
					});
				}
				else
					currentFrame = -1;
			}
		}
		
		void SetCurrentBacktrace (Backtrace bt)
		{
			currentBacktrace = bt;
			if (currentBacktrace != null) {
				for (int n=0; n<currentBacktrace.FrameCount; n++) {
					StackFrame sf = currentBacktrace.GetFrame (n);
					if (!string.IsNullOrEmpty (sf.SourceLocation.Filename)) {
						CurrentFrameIndex = n;
						return;
					}
				}
			}
			currentFrame = -1;
		}
	}
}
