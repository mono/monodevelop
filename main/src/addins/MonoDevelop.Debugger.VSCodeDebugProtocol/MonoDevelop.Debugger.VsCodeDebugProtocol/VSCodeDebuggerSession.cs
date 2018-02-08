//
// VSCodeDebuggerSession.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using Mono.Debugging.Client;
using System.Diagnostics;
using Mono.Debugging.Backend;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	public abstract class VSCodeDebuggerSession : DebuggerSession
	{
		int currentThreadId;

		protected override void OnContinue ()
		{
			protocolClient.SendRequestSync (new ContinueRequest (currentThreadId));
		}

		protected override void OnDetach ()
		{
			protocolClient.SendRequestSync (new DisconnectRequest ());
		}

		protected override void OnEnableBreakEvent (BreakEventInfo eventInfo, bool enable)
		{
			UpdateExceptions ();
			UpdateBreakpoints ();
		}

		protected override void OnExit ()
		{
			protocolClient?.SendRequestSync (new DisconnectRequest ());
		}

		protected override void OnFinish ()
		{
			protocolClient.SendRequestSync (new StepOutRequest (currentThreadId));
		}

		List<ProcessInfo> processInfo = new List<ProcessInfo>();
		protected override ProcessInfo [] OnGetProcesses ()
		{
			return processInfo.ToArray();
		}

		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			return GetThreadBacktrace (threadId);
		}

		protected override ThreadInfo [] OnGetThreads (long processId)
		{
			var threadsResponse = protocolClient.SendRequestSync (new ThreadsRequest ());
			var threads = new ThreadInfo [threadsResponse.Threads.Count];
			for (int i = 0; i < threads.Length; i++) {
				threads [i] = new ThreadInfo (processId,
											  threadsResponse.Threads [i].Id,
											  threadsResponse.Threads [i].Name,
											  null);
			}
			return threads;
		}

		Dictionary<BreakEvent, BreakEventInfo> breakpoints = new Dictionary<BreakEvent, BreakEventInfo> ();

		protected override BreakEventInfo OnInsertBreakEvent (BreakEvent breakEvent)
		{
			if (breakEvent is Mono.Debugging.Client.Breakpoint) {
				var breakEventInfo = new BreakEventInfo ();
				breakpoints.Add ((Mono.Debugging.Client.Breakpoint)breakEvent, breakEventInfo);
				UpdateBreakpoints ();
				return breakEventInfo;
			} else if (breakEvent is Catchpoint) {
				var catchpoint = (Catchpoint)breakEvent;
				var breakEventInfo = new BreakEventInfo ();
				breakpoints.Add (breakEvent, breakEventInfo);
				UpdateExceptions ();
				return breakEventInfo;
			}
			throw new NotImplementedException (breakEvent.GetType ().FullName);
		}
		bool currentExceptionState = false;
		void UpdateExceptions ()
		{
			//Disposed
			if (protocolClient == null)
				return;

			var hasCustomExceptions = breakpoints.Select (b => b.Key).OfType<Catchpoint> ().Any (e => e.Enabled);
			if (currentExceptionState != hasCustomExceptions) {
				currentExceptionState = hasCustomExceptions;
				protocolClient.SendRequest (new SetExceptionBreakpointsRequest (
					Capabilities.ExceptionBreakpointFilters.Where (f => hasCustomExceptions || (f.Default ?? false)).Select (f => f.Filter).ToList ()
				), null);
			}
		}

		protected override void OnNextInstruction ()
		{
			protocolClient.SendRequestSync (new NextRequest (currentThreadId));
		}

		protected override void OnNextLine ()
		{
			protocolClient.SendRequestSync (new NextRequest (currentThreadId));
		}

		protected override void OnRemoveBreakEvent (BreakEventInfo eventInfo)
		{
			breakpoints.Remove (breakpoints.Single (b => b.Value == eventInfo).Key);
			UpdateBreakpoints ();
			UpdateExceptions ();
		}

		void DebugAgentProcess_Exited (object sender, EventArgs e)
		{
			if (protocolClient != null) {
				protocolClient.RequestReceived -= OnDebugAdaptorRequestReceived;
				protocolClient.Stop ();
				protocolClient = null;
			}
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
		}

		Process debugAgentProcess;

		protected virtual void OnDebugAdaptorRequestReceived (object sender, RequestReceivedEventArgs e)
		{
			if (e.Command == "runInTerminal") {
				var args = (RunInTerminalArguments)e.Args;
				var consoleOptions = OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (args.Title).WithPauseWhenFinished (pauseWhenFinished);
				Runtime.ProcessService.StartConsoleProcess (
					args.Args [0],
					string.Join (" ", args.Args.Skip (1).ToArray ()),
					args.Cwd,
					ExternalConsoleFactory.Instance.CreateConsole (consoleOptions),
					args.Env.ToDictionary ((i) => i.Key, (i) => i.Value.ToString ()));
				e.Response = new RunInTerminalResponse ();
			}
		}

		void StartDebugAgent ()
		{
			var startInfo = new ProcessStartInfo (GetDebugAdapterPath (), GetDebugAdapterArguments ());
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardInput = true;
			startInfo.StandardOutputEncoding = Encoding.UTF8;
			startInfo.StandardOutputEncoding = Encoding.UTF8;
			startInfo.UseShellExecute = false;
			if (!MonoDevelop.Core.Platform.IsWindows)
				startInfo.EnvironmentVariables ["PATH"] = Environment.GetEnvironmentVariable ("PATH") + ":/usr/local/share/dotnet/";
			debugAgentProcess = Process.Start (startInfo);
			debugAgentProcess.EnableRaisingEvents = true;
			debugAgentProcess.Exited += DebugAgentProcess_Exited;
			protocolClient = new DebugProtocolHost (debugAgentProcess.StandardInput.BaseStream, debugAgentProcess.StandardOutput.BaseStream);
			protocolClient.RequestReceived += OnDebugAdaptorRequestReceived;
			protocolClient.Run ();
			protocolClient.EventReceived += HandleEvent;
			InitializeRequest initRequest = CreateInitRequest ();
			Capabilities = protocolClient.SendRequestSync (initRequest);
		}

		protected abstract InitializeRequest CreateInitRequest ();
		protected abstract LaunchRequest CreateLaunchRequest (DebuggerStartInfo startInfo);
		protected abstract AttachRequest CreateAttachRequest (long processId);
		protected abstract string GetDebugAdapterPath ();

		protected virtual string GetDebugAdapterArguments ()
		{
			return "";
		}

		protected override void OnAttachToProcess (long processId)
		{
			Attach (processId);
		}

		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			Launch (startInfo);
		}

		bool pauseWhenFinished;
		protected void Launch (DebuggerStartInfo startInfo)
		{
			pauseWhenFinished = !startInfo.CloseExternalConsoleOnExit;
			StartDebugAgent ();
			LaunchRequest launchRequest = CreateLaunchRequest (startInfo);
			protocolClient.SendRequestSync (launchRequest);
			protocolClient.SendRequestSync (new ConfigurationDoneRequest ());
		}

		protected void Attach (long processId)
		{
			StartDebugAgent ();
			var attachRequest = CreateAttachRequest (processId);
			protocolClient.SendRequestSync (attachRequest);
			OnStarted ();
			protocolClient.SendRequestSync (new ConfigurationDoneRequest ());
		}

		protected internal DebugProtocolHost protocolClient;

		Backtrace GetThreadBacktrace (long threadId)
		{
			return new Backtrace (new VSCodeDebuggerBacktrace (this, (int)threadId));
		}

		string EvaluateTrace(int frameId, string exp)
		{
			var sb = new StringBuilder();
			int last = 0;
			int i = exp.IndexOf('{');
			while (i != -1)
			{
				if (i < exp.Length - 1 && exp[i + 1] == '{')
				{
					sb.Append(exp, last, i - last + 1);
					last = i + 2;
					i = exp.IndexOf('{', i + 2);
					continue;
				}
				int j = exp.IndexOf('}', i + 1);
				if (j == -1)
					break;
				string se = exp.Substring(i + 1, j - i - 1);
				se = protocolClient.SendRequestSync(new EvaluateRequest(se, frameId)).Result;
				sb.Append(exp, last, i - last);
				sb.Append(se);
				last = j + 1;
				i = exp.IndexOf('{', last);
			}
			sb.Append(exp, last, exp.Length - last);
			return sb.ToString();
		}

		protected void HandleEvent (object sender, EventReceivedEventArgs obj)
		{
			Task.Run (() => {
				switch (obj.EventType) {
				case "initialized":
					//OnStarted();
					break;
				case "stopped":
					TargetEventArgs args;
					var body = (StoppedEvent)obj.Body;
					switch (body.Reason) {
					case StoppedEvent.ReasonValue.Breakpoint:
						var stackFrame = (VsCodeStackFrame)this.GetThreadBacktrace (body.ThreadId ?? -1).GetFrame (0);
						args = new TargetEventArgs (TargetEventType.TargetHitBreakpoint);
						var bp = breakpoints.Select (b => b.Key).OfType<Mono.Debugging.Client.Breakpoint> ().FirstOrDefault (b => b.FileName == stackFrame.SourceLocation.FileName && b.Line == stackFrame.SourceLocation.Line);
						if (bp == null)
							bp = breakpoints.Select (b => b.Key).OfType<Mono.Debugging.Client.Breakpoint> ().FirstOrDefault (b => Path.GetFileName (b.FileName) == Path.GetFileName (stackFrame.SourceLocation.FileName) && b.Line == stackFrame.SourceLocation.Line);
						if (bp == null) {
							//None of breakpoints is matching, this is probably Debugger.Break();
							args = new TargetEventArgs (TargetEventType.TargetStopped);
						} else {
							args.BreakEvent = bp;
							if (breakpoints.TryGetValue (bp, out var binfo)) {
								if ((bp.HitAction & HitAction.PrintExpression) != HitAction.None) {
									string exp = EvaluateTrace (stackFrame.frameId, bp.TraceExpression);
									binfo.UpdateLastTraceValue (exp);
									OnContinue ();
									return;
								}
							}
						}
						break;
					case StoppedEvent.ReasonValue.Step:
					case StoppedEvent.ReasonValue.Pause:
						args = new TargetEventArgs (TargetEventType.TargetStopped);
						break;
					case StoppedEvent.ReasonValue.Exception:
						args = new TargetEventArgs (TargetEventType.ExceptionThrown);
						break;
					default:
						throw new NotImplementedException (body.Reason.ToString ());
					}
					currentThreadId = body.ThreadId ?? -1;
					//TODO: what happens if thread is not specified?
					args.Process = GetProcesses () [0];
					args.Thread = args.Process.GetThreads ().Single (t => t.Id == currentThreadId);
					args.Backtrace = args.Thread.Backtrace;

					OnTargetEvent (args);
					break;
				case "terminated":
					OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
					break;
				case "exited":
					OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited) {
						ExitCode = ((ExitedEvent)obj.Body).ExitCode
					});
					break;
				case "process":
						var processEvent = (ProcessEvent)obj.Body;
						processInfo.Add(new ProcessInfo(processEvent.SystemProcessId ?? 1, processEvent.Name));
						OnStarted();
					break;
				case "output":
					var outputBody = (OutputEvent)obj.Body;
					switch (outputBody.Category) {
					case OutputEvent.CategoryValue.Stdout:
						OnTargetOutput (false, outputBody.Output);
						break;
					case OutputEvent.CategoryValue.Console:
						OnDebuggerOutput (false, outputBody.Output);
						break;
					case OutputEvent.CategoryValue.Stderr:
						OnTargetOutput (true, outputBody.Output);
						break;
					}
					break;
				}
			});
		}

		List<string> pathsWithBreakpoints = new List<string> ();

		void UpdateBreakpoints ()
		{
			//Disposed
			if (protocolClient == null)
				return;

			var bks = breakpoints.Select (b => b.Key).OfType<Mono.Debugging.Client.Breakpoint> ().Where (b => b.Enabled).GroupBy (b => b.FileName).ToArray ();
			var filesForRemoval = pathsWithBreakpoints.Where (path => !bks.Any (b => b.Key == path)).ToArray ();
			pathsWithBreakpoints = bks.Select (b => b.Key).ToList ();

			foreach (var path in filesForRemoval)
				protocolClient.SendRequest (new SetBreakpointsRequest (new Source (Path.GetFileName (path), path), new List<SourceBreakpoint> ()), null);

			foreach (var sourceFile in bks) {
				var source = new Source (Path.GetFileName (sourceFile.Key), sourceFile.Key);
				protocolClient.SendRequest (new SetBreakpointsRequest (
					source,
					sourceFile.Select (b => new SourceBreakpoint {
						Line = b.Line,
						Column = b.Column,
						Condition = b.ConditionExpression
						//TODO: HitCondition = b.HitCountMode + b.HitCount, wait for .Net Core Debugger
					}).ToList ()), (obj) => {
						Task.Run (() => {
							for (int i = 0; i < obj.Breakpoints.Count; i++) {
								breakpoints [sourceFile.ElementAt (i)].SetStatus (obj.Breakpoints [i].Line != -1 ? BreakEventStatus.Bound : BreakEventStatus.NotBound, "");
								if (obj.Breakpoints [i].Line != sourceFile.ElementAt (i).OriginalLine)
									breakpoints [sourceFile.ElementAt (i)].AdjustBreakpointLocation (obj.Breakpoints [i].Line, obj.Breakpoints [i].Column ?? 1);
							}
						});
					});
			}
		}

		protected InitializeResponse Capabilities;

		protected override void OnSetActiveThread (long processId, long threadId)
		{
			currentThreadId = (int)threadId;
		}

		protected override void OnStepInstruction ()
		{
			protocolClient.SendRequestSync (new StepInRequest (currentThreadId));
		}

		protected override void OnStepLine ()
		{
			protocolClient.SendRequestSync (new StepInRequest (currentThreadId));
		}

		protected override void OnStop ()
		{
			protocolClient.SendRequestSync (new PauseRequest ());
		}

		protected override void OnUpdateBreakEvent (BreakEventInfo eventInfo)
		{
			breakpoints [breakpoints.Single (b => b.Value == eventInfo).Key] = eventInfo;
			UpdateBreakpoints ();
			UpdateExceptions ();
		}

		public override void Dispose ()
		{
			base.Dispose ();
			if (protocolClient != null) {
				protocolClient.RequestReceived -= OnDebugAdaptorRequestReceived;
				protocolClient.SendRequestSync (new DisconnectRequest ());
				protocolClient.Stop ();
				protocolClient = null;
			}
		}
	}
}
