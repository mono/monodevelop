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
			eventInfo.BreakEvent.Enabled = enable;
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

		ProcessInfo [] processInfo = { new ProcessInfo (1, "debugee") };
		protected override ProcessInfo [] OnGetProcesses ()
		{
			return processInfo;
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
				var backtrace = this.GetThreadBacktrace (threadsResponse.Threads [i].Id);
				threads [i] = new ThreadInfo (processId,
										  threadsResponse.Threads [i].Id,
											  threadsResponse.Threads [i].Name,
											  backtrace.FrameCount > 0 ? backtrace.GetFrame (0).ToString () : "");
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
			var hasCustomExceptions = breakpoints.Select (b => b.Key).OfType<Catchpoint> ().Any ();
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
			protocolClient.Stop ();
		}

		Process debugAgentProcess;

		void StartDebugAgent ()
		{
			var startInfo = new ProcessStartInfo (GetDebugAdapterPath ());
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardInput = true;
			startInfo.StandardOutputEncoding = Encoding.UTF8;
			startInfo.StandardOutputEncoding = Encoding.UTF8;
			startInfo.UseShellExecute = false;
			if (!MonoDevelop.Core.Platform.IsWindows)
				startInfo.EnvironmentVariables ["PATH"] = Environment.GetEnvironmentVariable ("PATH") + ":/usr/local/share/dotnet/";
			debugAgentProcess = Process.Start (startInfo);
			debugAgentProcess.Exited += DebugAgentProcess_Exited;
			protocolClient = new DebugProtocolHost (debugAgentProcess.StandardInput.BaseStream, debugAgentProcess.StandardOutput.BaseStream);
			protocolClient.Run ();
			protocolClient.TraceCallback = (obj) => {
				Debug.WriteLine (obj);
			};
			protocolClient.EventReceived += HandleEvent;
			InitializeRequest initRequest = CreateInitRequest ();
			Capabilities = protocolClient.SendRequestSync (initRequest);
		}

		protected abstract InitializeRequest CreateInitRequest ();
		protected abstract LaunchRequest CreateLaunchRequest (DebuggerStartInfo startInfo);
		protected abstract AttachRequest CreateAttachRequest (long processId);
		protected abstract string GetDebugAdapterPath ();

		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			Launch (startInfo);
		}

		protected void Launch (DebuggerStartInfo startInfo)
		{
			StartDebugAgent ();
			LaunchRequest launchRequest = CreateLaunchRequest (startInfo);
			protocolClient.SendRequestSync (launchRequest);
			OnStarted ();
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

		protected DebugProtocolHost protocolClient;

		class VSCodeDebuggerBacktrace : IBacktrace
		{
			long threadId;
			VSCodeDebuggerSession vsCodeDebuggerSession;
			List<Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame> frames;
			Mono.Debugging.Client.StackFrame [] stackFrames;

			public VSCodeDebuggerBacktrace (VSCodeDebuggerSession vsCodeDebuggerSession, int threadId)
			{
				this.vsCodeDebuggerSession = vsCodeDebuggerSession;
				this.threadId = threadId;
				var body = vsCodeDebuggerSession.protocolClient.SendRequestSync (new StackTraceRequest (threadId, 0, 20));
				frames = body.StackFrames;
			}

			public int FrameCount {
				get {
					return frames.Count;
				}
			}

			public AssemblyLine [] Disassemble (int frameIndex, int firstLine, int count)
			{
				throw new NotImplementedException ();
			}

			public ObjectValue [] GetAllLocals (int frameIndex, EvaluationOptions options)
			{
				List<ObjectValue> results = new List<ObjectValue> ();
				var scopeBody = vsCodeDebuggerSession.protocolClient.SendRequestSync (new ScopesRequest (frames [frameIndex].Id));
				foreach (var variablesGroup in scopeBody.Scopes) {
					var varibles = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (variablesGroup.VariablesReference));
					foreach (var variable in varibles.Variables) {
						results.Add (VsCodeVariableToObjectValue (vsCodeDebuggerSession, variable.Name, variable.Type, variable.Value, variable.VariablesReference));
					}
				}
				return results.ToArray ();
			}

			public ExceptionInfo GetException (int frameIndex, EvaluationOptions options)
			{
				return new ExceptionInfo (GetAllLocals (frameIndex, options).Where (o => o.Name == "$exception").FirstOrDefault ());
			}

			public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
			{
				return new CompletionData ();
			}

			class VSCodeObjectSource : IObjectValueSource
			{
				int variablesReference;
				VSCodeDebuggerSession vsCodeDebuggerSession;

				public VSCodeObjectSource (VSCodeDebuggerSession vsCodeDebuggerSession, int variablesReference)
				{
					this.vsCodeDebuggerSession = vsCodeDebuggerSession;
					this.variablesReference = variablesReference;
				}

				public ObjectValue [] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
				{
					var children = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (
						variablesReference
					)).Variables;
					return children.Select (c => VsCodeVariableToObjectValue (vsCodeDebuggerSession, c.Name, c.Type, c.Value, c.VariablesReference)).ToArray ();
				}

				public object GetRawValue (ObjectPath path, EvaluationOptions options)
				{
					throw new NotImplementedException ();
				}

				public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
				{
					throw new NotImplementedException ();
				}

				public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
				{
					throw new NotImplementedException ();
				}

				public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
				{
					throw new NotImplementedException ();
				}
			}

			public ObjectValue [] GetExpressionValues (int frameIndex, string [] expressions, EvaluationOptions options)
			{
				var results = new List<ObjectValue> ();
				foreach (var expr in expressions) {
					var responseBody = vsCodeDebuggerSession.protocolClient.SendRequestSync (new EvaluateRequest (
						expr,
						frames [frameIndex].Id));
					results.Add (VsCodeVariableToObjectValue (vsCodeDebuggerSession, expr, responseBody.Type, responseBody.Result, responseBody.VariablesReference));
				}
				return results.ToArray ();
			}

			static ObjectValue VsCodeVariableToObjectValue (VSCodeDebuggerSession vsCodeDebuggerSession, string name, string type, string value, int variablesReference)
			{
				if (type == null)
					return ObjectValue.CreateError (null, new ObjectPath (name), "", value, ObjectValueFlags.None);
				if (variablesReference == 0)//This is some kind of primitive...
					return ObjectValue.CreatePrimitive (null, new ObjectPath (name), type, new EvaluationResult (value), ObjectValueFlags.ReadOnly);
				return ObjectValue.CreateObject (new VSCodeObjectSource (vsCodeDebuggerSession, variablesReference), new ObjectPath (name), type, new EvaluationResult (value), ObjectValueFlags.ReadOnly, null);
			}

			public ObjectValue [] GetLocalVariables (int frameIndex, EvaluationOptions options)
			{
				List<ObjectValue> results = new List<ObjectValue> ();
				var scopeBody = vsCodeDebuggerSession.protocolClient.SendRequestSync (new ScopesRequest (frames [frameIndex].Id));
				foreach (var variablesGroup in scopeBody.Scopes) {
					var varibles = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (variablesGroup.VariablesReference));
					foreach (var variable in varibles.Variables) {
						results.Add (ObjectValue.CreatePrimitive (null, new ObjectPath (variable.Name), variable.Type ?? "<unknown>", new EvaluationResult (variable.Value), ObjectValueFlags.None));
					}
				}
				return results.ToArray ();
			}

			public ObjectValue [] GetParameters (int frameIndex, EvaluationOptions options)
			{
				return new ObjectValue [0];//TODO: Find out how to seperate Params from other Locals
			}

			public Mono.Debugging.Client.StackFrame [] GetStackFrames (int firstIndex, int lastIndex)
			{
				if (stackFrames == null) {
					stackFrames = new Mono.Debugging.Client.StackFrame [Math.Min (lastIndex - firstIndex, frames.Count - firstIndex)];
					for (int i = firstIndex; i < stackFrames.Length + firstIndex; i++) {
						stackFrames [i] = new Mono.Debugging.Client.StackFrame (frames [i].Id,
																			 new SourceLocation (
																					frames [i].Name,
																					frames [i].Source?.Path,
																					frames [i].Line,
																					frames [i].Column,
																					frames [i].EndLine ?? -1,
																					frames [i].EndColumn ?? -1),
																				"C#");
					}
				}
				return stackFrames;
			}

			public ObjectValue GetThisReference (int frameIndex, EvaluationOptions options)
			{
				return GetAllLocals (frameIndex, options).FirstOrDefault (l => l.Name == "this");
			}

			public ValidationResult ValidateExpression (int frameIndex, string expression, EvaluationOptions options)
			{
				return new ValidationResult (true, null);
			}
		}

		Backtrace GetThreadBacktrace (long threadId)
		{
			return new Backtrace (new VSCodeDebuggerBacktrace (this, (int)threadId));
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
						var stackFrame = this.GetThreadBacktrace (body.ThreadId ?? -1).GetFrame (0);
						args = new TargetEventArgs (TargetEventType.TargetHitBreakpoint);
						var bp = breakpoints.Select (b => b.Key).OfType<Mono.Debugging.Client.Breakpoint> ().FirstOrDefault (b => b.FileName == stackFrame.SourceLocation.FileName && b.Line == stackFrame.SourceLocation.Line);
						if (bp == null) {
							//None of breakpoints is matching, this is probably Debugger.Break();
							args = new TargetEventArgs (TargetEventType.TargetStopped);
						} else {
							args.BreakEvent = bp;
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
					args.Process = OnGetProcesses () [0];
					args.Thread = GetThread (args.Process, (long)body.ThreadId);
					args.Backtrace = GetThreadBacktrace ((long)body.ThreadId);

					OnTargetEvent (args);
					break;
				case "terminated":
					OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
					break;
				case "exited":
					OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited) {
						ExitCode = (int)((ExitedEvent)obj.Body).ExitCode
					});
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

		ThreadInfo GetThread (ProcessInfo process, long threadId)
		{
			foreach (var threadInfo in OnGetThreads (process.Id)) {
				if (threadInfo.Id == threadId)
					return threadInfo;
			}
			return null;
		}

		List<Source> existingSourcesWithBreakpoints = new List<Source> ();

		void UpdateBreakpoints ()
		{
			//First clear all old breakpoints
			foreach (var source in existingSourcesWithBreakpoints)
				protocolClient.SendRequest (new SetBreakpointsRequest (source, new List<SourceBreakpoint> ()), null);
			existingSourcesWithBreakpoints.Clear ();

			var bks = breakpoints.Select (b => b.Key).OfType<Mono.Debugging.Client.Breakpoint> ().GroupBy (b => b.FileName).ToArray ();
			foreach (var sourceFile in bks) {
				var source = new Source (Path.GetFileName (sourceFile.Key), sourceFile.Key);
				existingSourcesWithBreakpoints.Add (source);
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
			protocolClient.SendRequestSync (new PauseRequest (currentThreadId));
		}

		protected override void OnUpdateBreakEvent (BreakEventInfo eventInfo)
		{
			breakpoints [breakpoints.Single (b => b.Value == eventInfo).Key] = eventInfo;
			UpdateBreakpoints ();
			UpdateExceptions ();
		}
	}
}
