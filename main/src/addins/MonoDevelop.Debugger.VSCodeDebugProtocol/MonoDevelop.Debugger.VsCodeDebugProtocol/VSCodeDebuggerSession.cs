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
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Mono.Debugging.Client;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

using MonoFunctionBreakpoint = Mono.Debugging.Client.FunctionBreakpoint;
using VsCodeFunctionBreakpoint = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.FunctionBreakpoint;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	public abstract class VSCodeDebuggerSession : DebuggerSession
	{
		readonly Dictionary<string, bool> enumTypes = new Dictionary<string, bool> ();
		int currentThreadId;

		internal bool IsEnum (string type, int frameId)
		{
			if (enumTypes.TryGetValue (type, out var isEnum))
				return isEnum;

			var request = new EvaluateRequest ($"typeof ({type}).IsEnum") { FrameId = frameId };
			var response = protocolClient.SendRequestSync (request);

			isEnum = response.Result.Equals ("true", StringComparison.OrdinalIgnoreCase);
			enumTypes.Add (type, isEnum);

			return isEnum;
		}

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
			try {
				HasExited = true;
				protocolClient.SendRequestSync (new DisconnectRequest ());
			} catch {
			}
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

		public override bool CanSetNextStatement {
			get { return true; }
		}

		protected override void OnSetNextStatement (long threadId, string fileName, int line, int column)
		{
			var source = new Source { Name = Path.GetFileName (fileName), Path = fileName };
			var request = new GotoTargetsRequest (source, line) { Column = column };
			var response = protocolClient.SendRequestSync (request);
			GotoTarget target = null;

			foreach (var location in response.Targets) {
				if (location.Line <= line && location.EndLine >= line && location.Column <= column && location.EndColumn >= column) {
					// exact match for location
					target = location;
					break;
				}

				if (target == null) {
					// closest match so far...
					target = location;
				}
			}

			if (target == null)
				throw new NotImplementedException ();

			protocolClient.SendRequestSync (new GotoRequest ((int) threadId, target.Id));
			RaiseStopEvent ();
		}

		Dictionary<BreakEvent, BreakEventInfo> breakpoints = new Dictionary<BreakEvent, BreakEventInfo> ();

		protected override BreakEventInfo OnInsertBreakEvent (BreakEvent breakEvent)
		{
			BreakEventInfo breakEventInfo;

			if (breakpoints.TryGetValue (breakEvent, out breakEventInfo))
				return breakEventInfo;

			breakEventInfo = new BreakEventInfo ();

			if (breakEvent is Mono.Debugging.Client.Breakpoint) {
				breakpoints.Add (breakEvent, breakEventInfo);
				UpdateBreakpoints ();
			} else if (breakEvent is Catchpoint) {
				breakpoints.Add (breakEvent, breakEventInfo);
				UpdateExceptions ();
			} else {
				throw new NotImplementedException (breakEvent.GetType ().FullName);
			}

			return breakEventInfo;
		}

		bool currentExceptionState = false;
		bool unhandleExceptionRegistered = false;
		void UpdateExceptions ()
		{
			//Disposed
			if (protocolClient == null)
				return;

			var hasCustomExceptions = breakpoints.Select (b => b.Key).OfType<Catchpoint> ().Any (e => e.Enabled);
			if (currentExceptionState != hasCustomExceptions || !unhandleExceptionRegistered) {
				currentExceptionState = hasCustomExceptions;
				var exceptionRequest = new SetExceptionBreakpointsRequest (
					Capabilities.ExceptionBreakpointFilters.Where (f => hasCustomExceptions || (f.Default ?? false)).Select (f => f.Filter).ToList ());
				exceptionRequest.ExceptionOptions = new List<ExceptionOptions> () {new ExceptionOptions(ExceptionBreakMode.UserUnhandled)};
				protocolClient.SendRequest (exceptionRequest, null);
				unhandleExceptionRegistered = true;
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
				try {
					HasExited = true;
					protocolClient.RequestReceived -= OnDebugAdaptorRequestReceived;
					protocolClient.Stop ();
				} catch {
				}
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
			if (!Platform.IsWindows)
				startInfo.EnvironmentVariables ["PATH"] = Environment.GetEnvironmentVariable ("PATH") + ":/usr/local/share/dotnet/";
			debugAgentProcess = Process.Start (startInfo);
			debugAgentProcess.EnableRaisingEvents = true;
			debugAgentProcess.Exited += DebugAgentProcess_Exited;
			protocolClient = new DebugProtocolHost (debugAgentProcess.StandardInput.BaseStream, debugAgentProcess.StandardOutput.BaseStream);
			protocolClient.RequestReceived += OnDebugAdaptorRequestReceived;
			protocolClient.Run ();
			protocolClient.EventReceived += HandleEvent;
			var initRequest = CreateInitRequest ();
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
			var launchRequest = CreateLaunchRequest (startInfo);
			protocolClient.SendRequestSync (launchRequest);
			protocolClient.SendRequestSync (new ConfigurationDoneRequest ());
			UpdateExceptions ();
		}

		protected void Attach (long processId)
		{
			StartDebugAgent ();
			var attachRequest = CreateAttachRequest (processId);
			protocolClient.SendRequestSync (attachRequest);
			OnStarted ();
			protocolClient.SendRequestSync (new ConfigurationDoneRequest ());
			UpdateExceptions ();
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
				se = protocolClient.SendRequestSync(new EvaluateRequest (se) { FrameId = frameId }).Result;
				sb.Append(exp, last, i - last);
				sb.Append(se);
				last = j + 1;
				i = exp.IndexOf('{', last);
			}
			sb.Append(exp, last, exp.Length - last);
			return sb.ToString();
		}

		bool? EvaluateCondition (int frameId, string exp)
		{
			var response = protocolClient.SendRequestSync (new EvaluateRequest (exp) { FrameId = frameId }).Result;

			if (bool.TryParse (response, out var result))
				return result;

			OnDebuggerOutput (false, $"The condition for an exception catchpoint failed to execute. The condition was '{exp}'. The error returned was '{response}'.\n");

			return null;
		}

		bool ShouldStopOnExceptionCatchpoint (Catchpoint catchpoint, int frameId)
		{
			if (!catchpoint.Enabled)
				return false;

			// global:: is necessary if the exception type is contained in current namespace,
			// and it also contains a class with the same name as the namespace itself.
			// Example: "Tests.Tests" and "Tests.TestException"
			var qualifiedExceptionType = catchpoint.ExceptionName.Contains ("::") ? catchpoint.ExceptionName : $"global::{catchpoint.ExceptionName}";

			if (catchpoint.IncludeSubclasses) {
				if (EvaluateCondition (frameId, $"$exception is {qualifiedExceptionType}") == false)
					return false;
			} else {
				if (EvaluateCondition (frameId, $"$exception.GetType() == typeof({qualifiedExceptionType})") == false)
					return false;
			}

			return string.IsNullOrWhiteSpace (catchpoint.ConditionExpression) || EvaluateCondition (frameId, catchpoint.ConditionExpression) != false;
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
						stackFrame = null;
						var backtrace = GetThreadBacktrace (body.ThreadId ?? -1);
						if (Options.ProjectAssembliesOnly) {
							// We can't evaluate expressions in external code frames, the debugger will hang
							for (int i = 0; i < backtrace.FrameCount; i++) {
								var frame = stackFrame = (VsCodeStackFrame)backtrace.GetFrame (i);
								if (!frame.IsExternalCode) {
									stackFrame = frame;
									break;
								}
							}
							if (stackFrame == null) {
								OnContinue ();
								return;
							}
						} else {
							// It's OK to evaluate expressions in external code
							stackFrame = (VsCodeStackFrame)backtrace.GetFrame (0);
						}
						var response = protocolClient.SendRequestSync (new ExceptionInfoRequest (body.ThreadId ?? -1));
						if (response.BreakMode.Equals (ExceptionBreakMode.UserUnhandled)) {
							args = new TargetEventArgs (TargetEventType.UnhandledException);
						} else {
							if (!breakpoints.Select (b => b.Key).OfType<Catchpoint> ().Any (c => ShouldStopOnExceptionCatchpoint (c, stackFrame.frameId))) {
								OnContinue ();
								return;
							}
							args = new TargetEventArgs (TargetEventType.ExceptionThrown);
						}
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
				case "module":
					var moduleEvent = (ModuleEvent)obj.Body;
					if (moduleEvent.Reason == ModuleEvent.ReasonValue.New) {
						OnAssemblyLoaded (moduleEvent.Module.Path);
					}
					break;
				}
			}).Ignore ();
		}

		List<string> pathsWithBreakpoints = new List<string> ();

		static readonly Dictionary<HitCountMode, string> conditions = new Dictionary<HitCountMode, string> {
			{ HitCountMode.EqualTo, "=" },
			{ HitCountMode.GreaterThan, ">" },
			{ HitCountMode.GreaterThanOrEqualTo, ">=" },
			{ HitCountMode.LessThan, "<" },
			{ HitCountMode.LessThanOrEqualTo, "<=" },
			{ HitCountMode.MultipleOf, "%" }};

		string GetHitCondition (Mono.Debugging.Client.Breakpoint breakpoint)
		{
			if (breakpoint.HitCountMode == HitCountMode.None)
				return null;

			return conditions [breakpoint.HitCountMode] + breakpoint.HitCount;
		}

		void UpdateBreakpoints ()
		{
			var bks = breakpoints.Select (b => b.Key).OfType<Mono.Debugging.Client.Breakpoint> ().Where (b => b.Enabled && !string.IsNullOrEmpty (b.FileName)).GroupBy (b => b.FileName).ToArray ();
			var filesForRemoval = pathsWithBreakpoints.Where (path => !bks.Any (b => b.Key == path)).ToArray ();
			pathsWithBreakpoints = bks.Select (b => b.Key).ToList ();

			foreach (var path in filesForRemoval) {
				//Disposed
				if (protocolClient == null)
					return;

				protocolClient.SendRequest (
						new SetBreakpointsRequest (
							new Source { Name = Path.GetFileName (path), Path = path }) {
							Breakpoints = new List<SourceBreakpoint> ()
						},
						null);
			}

			foreach (var sourceFile in bks) {
				var source = new Source { Name = Path.GetFileName (sourceFile.Key), Path = sourceFile.Key };
				//Disposed
				if (protocolClient == null)
					return;

				protocolClient.SendRequest (
					new SetBreakpointsRequest (source) {
						Breakpoints = sourceFile.Select (b => new SourceBreakpoint {
							Line = b.OriginalLine,
							Column = b.OriginalColumn,
							Condition = b.ConditionExpression,
							HitCondition = GetHitCondition(b)
						}).ToList ()
					}, (obj) => {
						Task.Run (() => {
							for (int i = 0; i < obj.Breakpoints.Count; i++) {
								breakpoints [sourceFile.ElementAt (i)].SetStatus (obj.Breakpoints [i].Line != -1 ? BreakEventStatus.Bound : BreakEventStatus.NotBound, "");
								if (obj.Breakpoints [i].Line != sourceFile.ElementAt (i).OriginalLine)
									breakpoints [sourceFile.ElementAt (i)].AdjustBreakpointLocation (obj.Breakpoints [i].Line, obj.Breakpoints [i].Column ?? 1);
							}
						}).Ignore ();
					});
			}

			//Disposed
			if (protocolClient == null)
				return;

			//Notice that .NET Core adapter doesn't support Functions breakpoints yet: https://github.com/OmniSharp/omnisharp-vscode/issues/295
			protocolClient.SendRequest (
				new SetFunctionBreakpointsRequest (
					breakpoints.Select (b => b.Key).OfType<MonoFunctionBreakpoint> ()
					.Where (b => b.Enabled)
					.Select (b => new VsCodeFunctionBreakpoint (b.FunctionName))
					.ToList ()),
				(obj) => { });
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
			HasExited = true;
			base.Dispose ();
			if (protocolClient != null) {
				protocolClient.RequestReceived -= OnDebugAdaptorRequestReceived;
				try {
					protocolClient.SendRequestSync (new DisconnectRequest ());
					protocolClient.Stop ();
				} catch {
				}
				protocolClient = null;
			}
		}

		protected override bool HandleException (Exception ex)
		{
			if (HasExited)
				return true;
			return base.HandleException (ex);
		}
	}
}
