// GdbSession.cs
//
// Author:
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
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Unix.Native;

namespace MonoDevelop.Debugger.Gdb
{
	class GdbSession: DebuggerSession
	{
		Process proc;
		StreamReader sout;
		StreamWriter sin;
		ProcessWrapper console;
		GdbCommandResult lastResult;
		bool running;
		Thread thread;
		int currentThread = -1;
		int activeThread = -1;
		string currentProcessName;
		
		bool internalStop;
			
		object syncLock = new object ();
		object eventLock = new object ();
		object gdbLock = new object ();
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			lock (gdbLock) {
				// Create a script to be run in a terminal
				string script = Path.GetTempFileName ();
				string ttyfile = Path.GetTempFileName ();
				string ttyfileDone = ttyfile + "_done";
				string tty;
				
				try {
					File.WriteAllText (script, "tty > " + ttyfile + "\ntouch " + ttyfileDone + "\nsleep 10000d");
					Mono.Unix.Native.Syscall.chmod (script, FilePermissions.ALLPERMS);
					
					console = Runtime.ProcessService.StartConsoleProcess (script, "", ".", ExternalConsoleFactory.Instance.CreateConsole (true), null);
					DateTime tim = DateTime.Now;
					while (!File.Exists (ttyfileDone)) {
						System.Threading.Thread.Sleep (100);
						if ((DateTime.Now - tim).TotalSeconds > 4)
							throw new InvalidOperationException ("Console could not be created.");
					}
					tty = File.ReadAllText (ttyfile).Trim (' ','\n');
				} finally {
					try {
						if (File.Exists (script))
							File.Delete (script);
						if (File.Exists (ttyfile))
							File.Delete (ttyfile);
						if (File.Exists (ttyfileDone))
							File.Delete (ttyfileDone);
					} catch {
						// Ignore
					}
				}
				
				StartGdb ();
				
				// Initialize the terminal
				RunCommand ("-inferior-tty-set", tty);
				RunCommand ("-file-exec-and-symbols", startInfo.Command);
				
				// Set inferior arguments
				if (!string.IsNullOrEmpty (startInfo.Arguments))
					RunCommand ("-exec-arguments", startInfo.Arguments);
				
				currentProcessName = startInfo.Command + " " + startInfo.Arguments;
				
				OnStarted ();
				
				RunCommand ("-exec-run");
			}
		}
		
		protected override void OnAttachToProcess (int processId)
		{
			lock (gdbLock) {
				StartGdb ();
				currentProcessName = "PID " + processId.ToString ();
				RunCommand ("attach", processId.ToString ());
				currentThread = 1;
				OnStarted ();
				FireTargetEvent (TargetEventType.TargetStopped, null);
			}
		}
		
		void StartGdb ()
		{
			proc = new Process ();
			proc.StartInfo.FileName = "gdb";
			proc.StartInfo.Arguments = "-quiet -fullname -i=mi2";
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardInput = true;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.Start ();
			
			sout = proc.StandardOutput;
			sin = proc.StandardInput;
			
			thread = new Thread (OutputInterpreter);
			thread.IsBackground = true;
			thread.Start ();
		}
		
		public override void Dispose ()
		{
			if (console != null && !console.HasExited) {
				console.Kill ();
				console = null;
			}
				
			if (thread != null)
				thread.Abort ();
		}
		
		protected override void OnSetActiveThread (int threadId)
		{
			activeThread = threadId;
		}
		
		protected override void OnStop ()
		{
			Syscall.kill (proc.Id, Signum.SIGINT);
		}
		
		protected override void OnDetach ()
		{
			lock (gdbLock) {
				InternalStop ();
				RunCommand ("detach");
				FireTargetEvent (TargetEventType.TargetExited, null);
			}
		}
		
		protected override void OnExit ()
		{
			lock (gdbLock) {
				InternalStop ();
				RunCommand ("kill");
				TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetExited);
				OnTargetEvent (args);
/*				proc.Kill ();
				TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetExited);
				OnTargetEvent (args);
*/			}
		}

		protected override void OnStepLine ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-step");
		}

		protected override void OnNextLine ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-next");
		}

		protected override void OnStepInstruction ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-step-instruction");
		}

		protected override void OnNextInstruction ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-next-instruction");
		}
		
		protected override void OnFinish ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-finish");
		}

		protected override int OnInsertBreakpoint (string filename, int line, bool activate)
		{
			lock (gdbLock) {
				bool dres = InternalStop ();
				try {
					GdbCommandResult res = RunCommand ("-break-insert", filename + ":" + line);
					int bh = res.GetObject ("bkpt").GetInt ("number");
					if (!activate)
						RunCommand ("-break-disable", bh.ToString ());
					return bh;
				} finally {
					InternalResume (dres);
				}
			}
		}
		
		protected override void OnRemoveBreakpoint (int handle)
		{
			lock (gdbLock) {
				bool dres = InternalStop ();
				try {
					RunCommand ("-break-delete", handle.ToString ());
				} finally {
					InternalResume (dres);
				}
			}
		}
		
		protected override void OnEnableBreakpoint (int handle, bool enable)
		{
			lock (gdbLock) {
				bool dres = InternalStop ();
				try {
					if (enable)
						RunCommand ("-break-enable", handle.ToString ());
					else
						RunCommand ("-break-disable", handle.ToString ());
				} finally {
					InternalResume (dres);
				}
			}
		}

		protected override void OnContinue ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-continue");
		}
		
		protected override ThreadInfo[] OnGetThreads (int processId)
		{
			List<ThreadInfo> list = new List<ThreadInfo> ();
			ResultData data = RunCommand ("-thread-list-ids").GetObject ("thread-ids");
			foreach (string id in data.GetAllValues ("thread-id"))
				list.Add (GetThread (int.Parse (id)));
			return list.ToArray ();
		}
		
		protected override ProcessInfo[] OnGetPocesses ()
		{
			ProcessInfo p = new ProcessInfo (0, currentProcessName);
			return new ProcessInfo [] { p };
		}
		
		ThreadInfo GetThread (int id)
		{
			return new ThreadInfo (0, id, "Thread #" + id);
		}
		
		protected override Backtrace OnGetThreadBacktrace (int processId, int threadId)
		{
			ResultData data = SelectThread (threadId);
			GdbCommandResult res = RunCommand ("-stack-info-depth");
			int fcount = int.Parse (res.GetValue ("depth"));
			GdbBacktrace bt = new GdbBacktrace (this, threadId, fcount, data != null ? data.GetObject ("frame") : null);
			return new Backtrace (bt);
		}
		
		public ResultData SelectThread (int id)
		{
			if (id == currentThread)
				return null;
			currentThread = id;
			return RunCommand ("-thread-select", id.ToString ());
		}
		
		public GdbCommandResult RunCommand (string command, params string[] args)
		{
			lock (gdbLock) {
				lock (syncLock) {
					lastResult = null;
					
					lock (eventLock) {
						running = true;
					}
					
					Console.WriteLine ("gdb<: " + command + " " + string.Join (" ", args));
					sin.WriteLine (command + " " + string.Join (" ", args));
					
					if (!Monitor.Wait (syncLock, 4000))
						throw new InvalidOperationException ("Command execution timeout.");
					if (lastResult.Status == CommandStatus.Error)
						throw new InvalidOperationException (lastResult.ErrorMessage);
					return lastResult;
				}
			}
		}
		
		bool InternalStop ()
		{
			lock (eventLock) {
				if (!running)
					return false;
				internalStop = true;
				Syscall.kill (proc.Id, Signum.SIGINT);
				if (!Monitor.Wait (eventLock, 4000))
					throw new InvalidOperationException ("Target could not be interrupted.");
			}
			return true;
		}
		
		void InternalResume (bool resume)
		{
			if (resume)
				RunCommand ("-exec-continue");
		}

		void OutputInterpreter ()
		{
			string line;
			while ((line = sout.ReadLine ()) != null) {
				ProcessOutput (line);
			}
		}
		
		void ProcessOutput (string line)
		{
			Console.WriteLine ("dbg>: '" + line + "'");
			switch (line [0]) {
				case '^':
					lock (syncLock) {
						lastResult = new GdbCommandResult (line);
						running = (lastResult.Status == CommandStatus.Running);
						Monitor.PulseAll (syncLock);
					}
					break;
					
				case '~':
				case '&':
					if (line.Length > 1 && line[1] == '"')
						line = line.Substring (2, line.Length - 5);
					ThreadPool.QueueUserWorkItem (delegate {
						OnTargetOutput (false, line + "\n");
					});
					break;
					
				case '*':
					GdbEvent ev;
					lock (eventLock) {
						running = false;
						ev = new GdbEvent (line);
						currentThread = activeThread = ev.GetInt ("thread-id");
						Monitor.PulseAll (eventLock);
						if (internalStop) {
							internalStop = false;
							return;
						}
					}
					ThreadPool.QueueUserWorkItem (delegate {
						try {
							HandleEvent (ev);
						} catch (Exception ex) {
							Console.WriteLine (ex);
						}
					});
					break;
			}
		}
		
		void HandleEvent (GdbEvent ev)
		{
			if (ev.Name != "stopped") {
				Console.WriteLine ("Unknown event: " + ev.Name);
				return;
			}
			
			TargetEventType type;
			switch (ev.Reason) {
				case "breakpoint-hit":
					type = TargetEventType.TargetHitBreakpoint;
					break;
				case "signal-received":
					if (ev.GetValue ("signal-name") == "SIGINT")
						type = TargetEventType.TargetInterrupted;
					else
						type = TargetEventType.TargetSignaled;
					break;
				case "exited":
				case "exited-signalled":
				case "exited-normally":
					type = TargetEventType.TargetExited;
					break;
				default:
					type = TargetEventType.TargetStopped;
					break;
			}
			
			ResultData curFrame = ev.GetObject ("frame");
			FireTargetEvent (type, curFrame);
		}
		
		void FireTargetEvent (TargetEventType type, ResultData curFrame)
		{
			TargetEventArgs args = new TargetEventArgs (type);
			
			if (type != TargetEventType.TargetExited) {
				GdbCommandResult res = RunCommand ("-stack-info-depth");
				int fcount = int.Parse (res.GetValue ("depth"));
				
				GdbBacktrace bt = new GdbBacktrace (this, activeThread, fcount, curFrame);
				args.Backtrace = new Backtrace (bt);
				args.Thread = GetThread (activeThread);
			}
			OnTargetEvent (args);
		}
	}
}
