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
using System.Globalization;
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
		IProcessAsyncOperation console;
		GdbCommandResult lastResult;
		bool running;
		Thread thread;
		long currentThread = -1;
		long activeThread = -1;
		bool isMonoProcess;
		string currentProcessName;
		List<string> tempVariableObjects = new List<string> ();
		Dictionary<int,Breakpoint> breakpoints = new Dictionary<int,Breakpoint> ();
		List<Breakpoint> breakpointsWithHitCount = new List<Breakpoint> ();
		
		DateTime lastBreakEventUpdate = DateTime.Now;
		Dictionary<int, WaitCallback> breakUpdates = new Dictionary<int,WaitCallback> ();
		bool breakUpdateEventsQueued;
		const int BreakEventUpdateNotifyDelay = 500;

		bool internalStop;
		bool logGdb;
			
		object syncLock = new object ();
		object eventLock = new object ();
		object gdbLock = new object ();
		
		public GdbSession ()
		{
			logGdb = !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MONODEVELOP_GDB_LOG"));
		}
		
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
						if ((DateTime.Now - tim).TotalSeconds > 10)
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
				RunCommand ("-inferior-tty-set", Escape (tty));
				
				try {
					RunCommand ("-file-exec-and-symbols", Escape (startInfo.Command));
				} catch {
					FireTargetEvent (TargetEventType.TargetExited, null);
					throw;
				}

				RunCommand ("-environment-directory", Escape (startInfo.WorkingDirectory));
				
				// Set inferior arguments
				if (!string.IsNullOrEmpty (startInfo.Arguments))
					RunCommand ("-exec-arguments", startInfo.Arguments);
				
				currentProcessName = startInfo.Command + " " + startInfo.Arguments;
				
				CheckIsMonoProcess ();
				OnStarted ();
				
				RunCommand ("-exec-run");
			}
		}
		
		protected override void OnAttachToProcess (long processId)
		{
			lock (gdbLock) {
				StartGdb ();
				currentProcessName = "PID " + processId.ToString ();
				RunCommand ("attach", processId.ToString ());
				currentThread = activeThread = 1;
				CheckIsMonoProcess ();
				OnStarted ();
				FireTargetEvent (TargetEventType.TargetStopped, null);
			}
		}

		public bool IsMonoProcess {
			get { return isMonoProcess; }
		}
		
		void CheckIsMonoProcess ()
		{
			try {
				RunCommand ("-data-evaluate-expression", "mono_pmip");
				isMonoProcess = true;
			} catch {
				isMonoProcess = false;
				// Ignore
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
			thread.Name = "GDB output interpeter";
			thread.IsBackground = true;
			thread.Start ();
		}
		
		public override void Dispose ()
		{
			if (console != null && !console.IsCompleted) {
				console.Cancel ();
				console = null;
			}
				
			if (thread != null)
				thread.Abort ();
		}
		
		protected override void OnSetActiveThread (long processId, long threadId)
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

		protected override object OnInsertBreakEvent (BreakEvent be, bool activate)
		{
			Breakpoint bp = be as Breakpoint;
			if (bp == null)
				return null;
			
			lock (gdbLock) {
				bool dres = InternalStop ();
				try {
					string extraCmd = string.Empty;
					if (bp.HitCount > 0) {
						extraCmd += "-i " + bp.HitCount;
						breakpointsWithHitCount.Add (bp);
					}
					if (!string.IsNullOrEmpty (bp.ConditionExpression)) {
						if (!bp.BreakIfConditionChanges)
							extraCmd += " -c " + bp.ConditionExpression;
					}
					
					// Breakpoint locations must be double-quoted if files contain spaces.
					// For example: -break-insert "\"C:/Documents and Settings/foo.c\":17"
					
					RunCommand ("-environment-directory", Escape (Path.GetDirectoryName (bp.FileName)));
					GdbCommandResult res = null;
					string errorMsg = null;
					try {
						res = RunCommand ("-break-insert", extraCmd.Trim (), Escape (Escape (bp.FileName) + ":" + bp.Line));
					} catch (Exception ex) {
						errorMsg = ex.Message;
					}
					
					if (res == null) {
						try {
							res = RunCommand ("-break-insert", extraCmd.Trim (), Escape (Escape (Path.GetFileName (bp.FileName)) + ":" + bp.Line));
						}
						catch {
						// Ignore
						}
					}
					if (res == null) {
						OnDebuggerOutput (true, "Could not set breakpoint: " + errorMsg);
						return null;
					}
					int bh = res.GetObject ("bkpt").GetInt ("number");
					if (!activate)
						RunCommand ("-break-disable", bh.ToString ());
					breakpoints [bh] = bp;
					return bh;
				} finally {
					InternalResume (dres);
				}
			}
		}
		
		bool CheckBreakpoint (int handle)
		{
			Breakpoint bp = breakpoints [handle];
			if (bp == null)
				return true;
			
			if (!string.IsNullOrEmpty (bp.ConditionExpression) && bp.BreakIfConditionChanges) {
				// Update the condition expression
				GdbCommandResult res = RunCommand ("-data-evaluate-expression", Escape (bp.ConditionExpression));
				string val = res.GetValue ("value");
				RunCommand ("-break-condition", handle.ToString (), "(" + bp.ConditionExpression + ") != " + val);
			}
			
			if (bp.HitAction == HitAction.PrintExpression) {
				GdbCommandResult res = RunCommand ("-data-evaluate-expression", Escape (bp.TraceExpression));
				string val = res.GetValue ("value");
				NotifyBreakEventUpdate (handle, 0, val);
				return false;
			}
			return true;
		}
		
		void NotifyBreakEventUpdate (int eventHandle, int hitCount, string lastTrace)
		{
			bool notify = false;
			
			WaitCallback nc = delegate {
				if (hitCount != -1)
					UpdateHitCount (eventHandle, hitCount);
				if (lastTrace != null)
					UpdateLastTraceValue (eventHandle, lastTrace);
			};
			
			lock (breakUpdates)
			{
				int span = (int) (DateTime.Now - lastBreakEventUpdate).TotalMilliseconds;
				if (span >= BreakEventUpdateNotifyDelay && !breakUpdateEventsQueued) {
					// Last update was more than 0.5s ago. The update can be sent.
					lastBreakEventUpdate = DateTime.Now;
					notify = true;
				} else {
					// Queue the event notifications to avoid wasting too much time
					breakUpdates [eventHandle] = nc;
					if (!breakUpdateEventsQueued) {
						breakUpdateEventsQueued = true;
						
						ThreadPool.QueueUserWorkItem (delegate {
							Thread.Sleep (BreakEventUpdateNotifyDelay - span);
							List<WaitCallback> copy;
							lock (breakUpdates) {
								copy = new List<WaitCallback> (breakUpdates.Values);
								breakUpdates.Clear ();
								breakUpdateEventsQueued = false;
								lastBreakEventUpdate = DateTime.Now;
							}
							foreach (WaitCallback wc in copy)
								wc (null);
						});
					}
				}
			}
			if (notify)
				nc (null);
		}
		
		void UpdateHitCountData ()
		{
			foreach (Breakpoint bp in breakpointsWithHitCount) {
				object h;
				if (GetBreakpointHandle (bp, out h)) {
					GdbCommandResult res = RunCommand ("-break-info", h.ToString ());
					string val = res.GetObject ("BreakpointTable").GetObject ("body").GetObject (0).GetObject ("bkpt").GetValue ("ignore");
					if (val != null)
						NotifyBreakEventUpdate ((int)h, int.Parse (val), null);
					else
						NotifyBreakEventUpdate ((int)h, 0, null);
				}
			}
			breakpointsWithHitCount.Clear ();
		}
		
		protected override void OnRemoveBreakEvent (object handle)
		{
			lock (gdbLock) {
				bool dres = InternalStop ();
				Breakpoint bp = breakpoints [(int)handle];
				breakpointsWithHitCount.Remove (bp);
				breakpoints.Remove ((int)handle);
				try {
					RunCommand ("-break-delete", handle.ToString ());
				} finally {
					InternalResume (dres);
				}
			}
		}
		
		protected override void OnEnableBreakEvent (object handle, bool enable)
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
		
		protected override object OnUpdateBreakEvent (object handle, BreakEvent be)
		{
			Breakpoint bp = be as Breakpoint;
			if (bp == null)
				return null;

			bool ss = InternalStop ();
			
			try {
				if (bp.HitCount > 0) {
					RunCommand ("-break-after", handle.ToString (), bp.HitCount.ToString ());
					breakpointsWithHitCount.Add (bp);
				} else
					breakpointsWithHitCount.Remove (bp);
				
				if (!string.IsNullOrEmpty (bp.ConditionExpression) && !bp.BreakIfConditionChanges)
					RunCommand ("-break-condition", handle.ToString (), bp.ConditionExpression);
				else
					RunCommand ("-break-condition", handle.ToString ());
			} finally {
				InternalResume (ss);
			}

			return handle;
		}

		protected override void OnContinue ()
		{
			SelectThread (activeThread);
			RunCommand ("-exec-continue");
		}
		
		protected override ThreadInfo[] OnGetThreads (long processId)
		{
			List<ThreadInfo> list = new List<ThreadInfo> ();
			ResultData data = RunCommand ("-thread-list-ids").GetObject ("thread-ids");
			foreach (string id in data.GetAllValues ("thread-id"))
				list.Add (GetThread (long.Parse (id)));
			return list.ToArray ();
		}
		
		protected override ProcessInfo[] OnGetProcesses ()
		{
			ProcessInfo p = new ProcessInfo (0, currentProcessName);
			return new ProcessInfo [] { p };
		}
		
		ThreadInfo GetThread (long id)
		{
			return new ThreadInfo (0, id, "Thread #" + id, null);
		}
		
		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			ResultData data = SelectThread (threadId);
			GdbCommandResult res = RunCommand ("-stack-info-depth");
			int fcount = int.Parse (res.GetValue ("depth"));
			GdbBacktrace bt = new GdbBacktrace (this, threadId, fcount, data != null ? data.GetObject ("frame") : null);
			return new Backtrace (bt);
		}
		
		protected override AssemblyLine[] OnDisassembleFile (string file)
		{
			List<AssemblyLine> lines = new List<AssemblyLine> ();
			int cline = 1;
			do {
				ResultData data = null;
				try {
					data = RunCommand ("-data-disassemble", "-f", file, "-l", cline.ToString (), "--", "1");
				} catch {
					break;
				}
				ResultData asm_insns = data.GetObject ("asm_insns");
				int newLine = cline;
				for (int n=0; n<asm_insns.Count; n++) {
					ResultData src_and_asm_line = asm_insns.GetObject (n).GetObject ("src_and_asm_line");
					newLine = src_and_asm_line.GetInt ("line");
					ResultData line_asm_insn = src_and_asm_line.GetObject ("line_asm_insn");
					for (int i=0; i<line_asm_insn.Count; i++) {
						ResultData asm = line_asm_insn.GetObject (i);
						long addr = long.Parse (asm.GetValue ("address").Substring (2), NumberStyles.HexNumber);
						string code = asm.GetValue ("inst");
						lines.Add (new AssemblyLine (addr, code, newLine));
					}
				}
				if (newLine <= cline)
					break;
				cline = newLine + 1;
				
			} while (true);
			
			return lines.ToArray ();
		}
		
		public ResultData SelectThread (long id)
		{
			if (id == currentThread)
				return null;
			currentThread = id;
			return RunCommand ("-thread-select", id.ToString ());
		}
		
		string Escape (string str)
		{
			if (str == null)
				return null;
			else if (str.IndexOf (' ') != -1 || str.IndexOf ('"') != -1) {
				str = str.Replace ("\"", "\\\"");
				return "\"" + str + "\"";
			}
			else
				return str;
		}
		
		public GdbCommandResult RunCommand (string command, params string[] args)
		{
			lock (gdbLock) {
				lock (syncLock) {
					lastResult = null;
					
					lock (eventLock) {
						running = true;
					}
					
					if (logGdb)
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
				try {
					ProcessOutput (line);
				} catch (Exception ex) {
					Console.WriteLine (ex);
				}
			}
		}
		
		void ProcessOutput (string line)
		{
			if (logGdb)
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
						string ti = ev.GetValue ("thread-id");
						if (ti != null && ti != "all")
							currentThread = activeThread = int.Parse (ti);
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
			
			CleanTempVariableObjects ();
			
			TargetEventType type;
			switch (ev.Reason) {
				case "breakpoint-hit":
					type = TargetEventType.TargetHitBreakpoint;
					if (!CheckBreakpoint (ev.GetInt ("bkptno"))) {
						RunCommand ("-exec-continue");
						return;
					}
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
			UpdateHitCountData ();

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
		
		internal void RegisterTempVariableObject (string var)
		{
			tempVariableObjects.Add (var);
		}
		
		void CleanTempVariableObjects ()
		{
			foreach (string s in tempVariableObjects)
				RunCommand ("-var-delete", s);
			tempVariableObjects.Clear ();
		}
	}
}
