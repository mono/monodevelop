// DebuggingService.cs
//
// Author:
//   Mike Kestner <mkesner@ximian.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2004-2008 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using Mono.Addins;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Debugger.Viewers;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */
using MonoDevelop.Ide.TextEditing;

namespace MonoDevelop.Debugger
{

	public static class DebuggingService
	{
		const string FactoriesPath = "/MonoDevelop/Debugging/DebuggerEngines";
		static DebuggerEngine[] engines;
		
		const string EvaluatorsPath = "/MonoDevelop/Debugging/Evaluators";
		static Dictionary<string, ExpressionEvaluatorExtensionNode> evaluators;
		
		static BreakpointStore breakpoints = new BreakpointStore ();
		static PinnedWatchStore pinnedWatches = new PinnedWatchStore ();
		
		static IConsole console;
		static DebugExecutionHandlerFactory executionHandlerFactory;
		
		static string oldLayout;
		
		static DebuggerEngine currentEngine;
		static DebuggerSession session;
		static Backtrace currentBacktrace;
		static int currentFrame;
		
		static ExceptionCaughtMessage exceptionDialog;
		
		static BusyEvaluatorDialog busyDialog;
		static bool isBusy;
		static StatusBarIcon busyStatusIcon;

		static public event EventHandler DebugSessionStarted;
		static public event EventHandler PausedEvent;
		static public event EventHandler ResumedEvent;
		static public event EventHandler StoppedEvent;
		
		static public event EventHandler CallStackChanged;
		static public event EventHandler CurrentFrameChanged;
		static public event EventHandler ExecutionLocationChanged;
		static public event EventHandler DisassemblyRequested;
		static public event EventHandler<DocumentEventArgs> DisableConditionalCompilation;
			
		static public event EventHandler EvaluationOptionsChanged;
		
		static DebuggingService ()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory ();
			TextEditorService.LineCountChanged += OnLineCountChanged;
			IdeApp.Initialized += delegate {
				IdeApp.Workspace.StoringUserPreferences += OnStoreUserPrefs;
				IdeApp.Workspace.LoadingUserPreferences += OnLoadUserPrefs;
				IdeApp.Workspace.LastWorkspaceItemClosed += OnSolutionClosed;
				busyDialog = new BusyEvaluatorDialog ();
				busyDialog.TransientFor = MessageService.RootWindow;
				busyDialog.DestroyWithParent = true;
			};
			AddinManager.AddExtensionNodeHandler (FactoriesPath, delegate {
				// Regresh the engine list
				engines = null;
			});
			AddinManager.AddExtensionNodeHandler (EvaluatorsPath, delegate {
				// Regresh the engine list
				evaluators = null;
			});
        }

		public static IExecutionHandler GetExecutionHandler ()
		{
			return executionHandlerFactory;
		}
		
		public static DebuggerSession DebuggerSession {
			get { return session; }
		}
		
		public static BreakpointStore Breakpoints {
			get { return breakpoints; }
		}
		
		public static PinnedWatchStore PinnedWatches {
			get { return pinnedWatches; }
		}
		
		public static void SetLiveUpdateMode (PinnedWatch watch, bool liveUpdate)
		{
			if (watch.LiveUpdate == liveUpdate)
				return;
			
			watch.LiveUpdate = liveUpdate;
			if (liveUpdate) {
				Breakpoint bp = new Breakpoint (watch.File, watch.Line);
				bp.TraceExpression = "{" + watch.Expression + "}";
				bp.HitAction = HitAction.PrintExpression;
				breakpoints.Add (bp);
				pinnedWatches.Bind (watch, bp);
			} else {
				pinnedWatches.Bind (watch, null);
				breakpoints.Remove (watch.BoundTracer);
			}
		}
		
		static void BreakpointTraceHandler (BreakEvent be, string trace)
		{
			if (be is Breakpoint) {
				if (pinnedWatches.UpdateLiveWatch ((Breakpoint) be, trace))
					return; // No need to log the value. It is shown in the watch.
			}
			console.Log.Write (trace + "\n");
		}
		
		public static string[] EnginePriority {
			get {
				string s = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.EnginePriority", "");
				if (s.Length == 0) {
					// Set the initial priorities
					List<string> prios = new List<string> ();
					int i = 0;
					foreach (DebuggerEngineExtensionNode de in AddinManager.GetExtensionNodes (FactoriesPath)) {
						if (de.Id.StartsWith ("Mono.Debugger.Soft")) // Give priority to soft debugger by default
							prios.Insert (i++, de.Id);
						else
							prios.Add (de.Id);
					}
					string[] parray = prios.ToArray ();
					EnginePriority = parray;
					return parray;
				}
				return s.Split (new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
			}
			set {
				string s = string.Join (",", value);
				PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.EnginePriority", s);
				engines = null;
			}
		}
		
		internal static IEnumerable<IValueVisualizer> GetValueVisualizers (ObjectValue val)
		{
			foreach (IValueVisualizer v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/ValueVisualizers", false))
				if (v.CanVisualize (val))
					yield return v;
		}
		
		internal static bool HasValueVisualizers (ObjectValue val)
		{
			foreach (IValueVisualizer v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/ValueVisualizers", false))
				if (v.CanVisualize (val))
					return true;
			return false;
		}
		
		public static void ShowValueVisualizer (ObjectValue val)
		{
			var dlg = new ValueVisualizerDialog ();
			dlg.Show (val);
			MessageService.ShowCustomDialog (dlg);
		}
		
		public static bool ShowBreakpointProperties (Breakpoint bp, bool editNew)
		{
			var dlg = new BreakpointPropertiesDialog (bp, editNew);
			if (MessageService.ShowCustomDialog (dlg) == (int) Gtk.ResponseType.Ok)
				return true;
			return false;
		}
		
		public static void ShowAddTracepointDialog (string file, int line)
		{
			AddTracePointDialog dlg = new AddTracePointDialog ();
			if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok && dlg.Text.Length > 0) {
				Breakpoint bp = new Breakpoint (file, line);
				bp.HitAction = HitAction.PrintExpression;
				bp.TraceExpression = dlg.Text;
				bp.ConditionExpression = dlg.Condition;
				Breakpoints.Add (bp);
			}
			dlg.Destroy ();
		}
		
		public static void AddWatch (string expression)
		{
			Pad pad = IdeApp.Workbench.GetPad<WatchPad> ();
			pad.BringToFront (false);
			WatchPad wp = (WatchPad) pad.Content;
			wp.AddWatch (expression);
		}

		public static bool IsFeatureSupported (IBuildTarget target, DebuggerFeatures feature)
		{
			return (GetSupportedFeatures (target) & feature) == feature;
		}

		public static bool IsDebuggingSupported {
			get {
				return AddinManager.GetExtensionNodes (FactoriesPath).Count > 0;
			}
		}

		public static bool CurrentSessionSupportsFeature (DebuggerFeatures feature)
		{
			return (currentEngine.SupportedFeatures & feature) == feature;
		}

		public static bool IsFeatureSupported (DebuggerFeatures feature)
		{
			foreach (DebuggerEngine engine in GetDebuggerEngines ())
				if ((engine.SupportedFeatures & feature) == feature)
					return true;
			return false;
		}

		public static DebuggerFeatures GetSupportedFeatures (IBuildTarget target)
		{
			FeatureCheckerHandlerFactory fc = new FeatureCheckerHandlerFactory ();
			ExecutionContext ctx = new ExecutionContext (fc, null);
			target.CanExecute (ctx, IdeApp.Workspace.ActiveConfiguration);
			return fc.SupportedFeatures;
		}

		public static DebuggerFeatures GetSupportedFeaturesForCommand (ExecutionCommand command)
		{
			DebuggerEngine engine = GetFactoryForCommand (command);
			if (engine != null)
				return engine.SupportedFeatures;
			else
				return DebuggerFeatures.None;
		}

		public static void ShowExpressionEvaluator (string expression)
		{
			ExpressionEvaluatorDialog dlg = new ExpressionEvaluatorDialog ();
			if (expression != null)
				dlg.Expression = expression;
			MessageService.ShowCustomDialog (dlg);
		}
		
		public static void ShowExceptionCaughtDialog ()
		{
			EvaluationOptions ops = session.EvaluationOptions.Clone ();
			ops.MemberEvaluationTimeout = 0;
			ops.EvaluationTimeout = 0;
			ops.EllipsizeStrings = false;
			
			ExceptionInfo val = CurrentFrame.GetException (ops);
			if (val != null) {
				HideExceptionCaughtDialog ();
				exceptionDialog = new ExceptionCaughtMessage (val, CurrentFrame.SourceLocation.FileName, CurrentFrame.SourceLocation.Line, CurrentFrame.SourceLocation.Column);
				exceptionDialog.ShowButton ();
				exceptionDialog.Closed += (o, args) => {
					exceptionDialog = null;
				};
			}
		}

		static void HideExceptionCaughtDialog ()
		{
			if (exceptionDialog != null) {
				exceptionDialog.Dispose ();
				exceptionDialog = null;
			}
		}

		internal static ExceptionCaughtMessage ExceptionCaughtMessage {
			get {
				return exceptionDialog;
			}
		}

		public static void ShowExceptionsFilters ()
		{
			MessageService.ShowCustomDialog (new ExceptionsDialog ());
		}
		
		static void SetupSession ()
		{
			isBusy = false;
			session.Breakpoints = breakpoints;
			session.TargetEvent += OnTargetEvent;
			session.TargetStarted += OnStarted;
			session.OutputWriter = OutputWriter;
			session.LogWriter = LogWriter;
			session.BusyStateChanged += OnBusyStateChanged;
			session.TypeResolverHandler = ResolveType;
			session.BreakpointTraceHandler = BreakpointTraceHandler;
			session.GetExpressionEvaluator = OnGetExpressionEvaluator;
			session.ConnectionDialogCreator = delegate {
				return new StatusBarConnectionDialog ();
			};

			console.CancelRequested += OnCancelRequested;
			
			DispatchService.GuiDispatch (delegate {
				if (DebugSessionStarted != null)
					DebugSessionStarted (null, EventArgs.Empty);
				NotifyLocationChanged ();
			});

		}
		
		static void Cleanup ()
		{
			if (oldLayout != null) {
				string layout = oldLayout;
				oldLayout = null;
				// Dispatch asynchronously to avoid start/stop races
				DispatchService.GuiSyncDispatch (delegate {
					IdeApp.Workbench.HideCommandBar ("Debug");
					if (IdeApp.Workbench.CurrentLayout == "Debug")
						IdeApp.Workbench.CurrentLayout = layout;
				});
			}
			
			currentBacktrace = null;
			
			if (!IsDebugging)
				return;

			DispatchService.GuiSyncDispatch (delegate {
				HideExceptionCaughtDialog ();
			});

			if (busyStatusIcon != null) {
				busyStatusIcon.Dispose ();
				busyStatusIcon = null;
			}
			
			session.TargetEvent -= OnTargetEvent;
			session.TargetStarted -= OnStarted;
			session.OutputWriter = null;
			session.LogWriter = null;
			session.BusyStateChanged -= OnBusyStateChanged;
			session.TypeResolverHandler = null;
			session.BreakpointTraceHandler = null;
			session.GetExpressionEvaluator = null;
			
			// Dispose the session at the end, since it may take a while.
			DebuggerSession oldSession = session;
			session = null;
			
			DispatchService.GuiDispatch (delegate {
				if (StoppedEvent != null)
					StoppedEvent (null, new EventArgs ());
			});
			
			if (console != null) {
				console.CancelRequested -= OnCancelRequested;
				console.Dispose ();
				console = null;
			}
			
			DispatchService.GuiDispatch (delegate {
				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
				NotifyLocationChanged ();
			});
			
			if (oldSession != null) {
				oldSession.BusyStateChanged -= OnBusyStateChanged;
				oldSession.Dispose ();
			}
		}

		public static bool IsDebugging {
			get {
				return session != null;
			}
		}

		public static bool IsConnected {
			get {
				return IsDebugging && session.IsConnected;
			}
		}

		public static bool IsRunning {
			get {
				return IsDebugging && session.IsRunning;
			}
		}

		public static bool IsPaused {
			get {
				return IsDebugging && !IsRunning && currentBacktrace != null;
			}
		}

		public static void Pause ()
		{
			session.Stop ();
		}

		public static void Resume ()
		{
			if (CheckIsBusy ())
				return;
			session.Continue ();
			NotifyLocationChanged ();
		}

		public static IProcessAsyncOperation Run (string file, IConsole console)
		{
			DebugExecutionHandler h = new DebugExecutionHandler (null);
			ExecutionCommand cmd = Runtime.ProcessService.CreateCommand (file);
			return h.Execute (cmd, console);
		}
		
		public static IAsyncOperation AttachToProcess (DebuggerEngine debugger, ProcessInfo proc)
		{
			currentEngine = debugger;
			session = debugger.CreateSession ();
			session.ExceptionHandler = ExceptionHandler;
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			console = monitor as IConsole;
			SetupSession ();
			session.TargetExited += delegate {
				monitor.Dispose ();
			};
			session.AttachToProcess (proc, GetUserOptions ());
			return monitor.AsyncOperation;
		}
		
		public static DebuggerSessionOptions GetUserOptions ()
		{
			EvaluationOptions eval = EvaluationOptions.DefaultOptions;
			eval.AllowTargetInvoke = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AllowTargetInvoke", true);
			eval.AllowToStringCalls = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AllowToStringCalls", true);
			eval.EvaluationTimeout = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.EvaluationTimeout", 2500);
			eval.FlattenHierarchy = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.FlattenHierarchy", false);
			eval.GroupPrivateMembers = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.GroupPrivateMembers", true);
			eval.GroupStaticMembers = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.GroupStaticMembers", true);
			eval.MemberEvaluationTimeout = eval.EvaluationTimeout * 2;
			return new DebuggerSessionOptions () {
				StepOverPropertiesAndOperators = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.StepOverPropertiesAndOperators", true),
				ProjectAssembliesOnly = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.ProjectAssembliesOnly", true),
				EvaluationOptions = eval,
			};
		}
		
		public static void SetUserOptions (DebuggerSessionOptions options)
		{
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.StepOverPropertiesAndOperators", options.StepOverPropertiesAndOperators);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.ProjectAssembliesOnly", options.ProjectAssembliesOnly);
			
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.AllowTargetInvoke", options.EvaluationOptions.AllowTargetInvoke);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.AllowToStringCalls", options.EvaluationOptions.AllowToStringCalls);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.EvaluationTimeout", options.EvaluationOptions.EvaluationTimeout);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.FlattenHierarchy", options.EvaluationOptions.FlattenHierarchy);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.GroupPrivateMembers", options.EvaluationOptions.GroupPrivateMembers);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.GroupStaticMembers", options.EvaluationOptions.GroupStaticMembers);
			
			if (session != null) {
				session.Options.EvaluationOptions = GetUserOptions ().EvaluationOptions;
				if (EvaluationOptionsChanged != null)
					EvaluationOptionsChanged (null, EventArgs.Empty);
			}
		}
		
		public static void ShowDisassembly ()
		{
			if (DisassemblyRequested != null)
				DisassemblyRequested (null, EventArgs.Empty);
		}
		
		internal static void InternalRun (ExecutionCommand cmd, DebuggerEngine factory, IConsole c)
		{
			if (factory == null) {
				factory = GetFactoryForCommand (cmd);
				if (factory == null)
					throw new InvalidOperationException ("Unsupported command: " + cmd);
			}
			
			if (session != null)
				throw new InvalidOperationException ("A debugger session is already started");

			DebuggerStartInfo startInfo = factory.CreateDebuggerStartInfo (cmd);
			startInfo.UseExternalConsole = c is ExternalConsole;
			startInfo.CloseExternalConsoleOnExit = c.CloseOnDispose;
			currentEngine = factory;
			session = factory.CreateSession ();
			session.ExceptionHandler = ExceptionHandler;
			
			// When using an external console, create a new internal console which will be used
			// to show the debugger log
			if (startInfo.UseExternalConsole)
				console = (IConsole) IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			else
				console = c;
			
			SetupSession ();
			
			// Dispatch synchronously to avoid start/stop races
			DispatchService.GuiSyncDispatch (delegate {
				oldLayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = "Debug";
				IdeApp.Workbench.ShowCommandBar ("Debug");
			});
			
			try {
				session.Run (startInfo, GetUserOptions ());
			} catch {
				Cleanup ();
				throw;
			}

		}
		
		static bool ExceptionHandler (Exception ex)
		{
			LoggingService.LogError ("Error in debugger", ex);
			Gtk.Application.Invoke (delegate {
				if (ex is DebuggerException)
					MessageService.ShowError (ex.Message);
				else
					MessageService.ShowException (ex);
			});
			return true;
		}
		
		static void LogWriter (bool iserr, string text)
		{
			// Events may come with a bit of delay, so the debug session
			// may already have been cleaned up
			if (console != null)
				console.Log.Write (text);
		}
		
		static void OutputWriter (bool iserr, string text)
		{
			if (iserr)
				console.Error.Write (text);
			else
				console.Out.Write (text);
		}

		static void OnBusyStateChanged (object s, BusyStateEventArgs args)
		{
			isBusy = args.IsBusy;
			DispatchService.GuiDispatch (delegate {
				busyDialog.UpdateBusyState (args);
				if (args.IsBusy) {
					if (busyStatusIcon == null) {
						busyStatusIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (ImageService.GetPixbuf ("md-execute-debug", Gtk.IconSize.Menu));
						busyStatusIcon.SetAlertMode (100);
						busyStatusIcon.ToolTip = GettextCatalog.GetString ("The Debugger is waiting for an expression evaluation to finish.");
						busyStatusIcon.EventBox.ButtonPressEvent += delegate {
							MessageService.PlaceDialog (busyDialog, MessageService.RootWindow);
						};
					}
				} else {
					if (busyStatusIcon != null) {
						busyStatusIcon.Dispose ();
						busyStatusIcon = null;
					}
				}
			});
		}
		
		static bool CheckIsBusy ()
		{
			if (isBusy && !busyDialog.Visible)
				MessageService.PlaceDialog (busyDialog, MessageService.RootWindow);
			return isBusy;
		}
		
		static void OnStarted (object s, EventArgs a)
		{
			currentBacktrace = null;
			DispatchService.GuiDispatch (delegate {
				HideExceptionCaughtDialog ();
				if (ResumedEvent != null)
					ResumedEvent (null, a);
				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
				NotifyLocationChanged ();
			});
		}
		
		static void OnTargetEvent (object sender, TargetEventArgs args)
		{
			try {
				switch (args.Type) {
					case TargetEventType.TargetExited:
						Cleanup ();
						break;
					case TargetEventType.TargetSignaled:
					case TargetEventType.TargetStopped:
					case TargetEventType.TargetHitBreakpoint:
					case TargetEventType.TargetInterrupted:
					case TargetEventType.UnhandledException:
					case TargetEventType.ExceptionThrown:
						SetCurrentBacktrace (args.Backtrace);
						NotifyPaused ();
						NotifyException (args);
						break;
					default:
						break;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error handling debugger target event", ex);
			}
		}
		
		static void OnDisableConditionalCompilation (DocumentEventArgs e)
		{
			EventHandler<DocumentEventArgs> handler = DisableConditionalCompilation;
			if (handler != null)
				handler (null, e);
		}

		static void NotifyPaused ()
		{
			DispatchService.GuiDispatch (delegate {
				if (PausedEvent != null)
					PausedEvent (null, EventArgs.Empty);
				NotifyLocationChanged ();
				IdeApp.Workbench.GrabDesktopFocus ();
			});
		}
		
		static void NotifyException (TargetEventArgs args)
		{
			if (args.Type == TargetEventType.UnhandledException || args.Type == TargetEventType.ExceptionThrown) {
				DispatchService.GuiDispatch (delegate {
					if (CurrentFrame != null) {
						ShowExceptionCaughtDialog ();
					}
				});
			}
		}
		
		static void NotifyLocationChanged ()
		{
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (null, EventArgs.Empty);
		}
		
		static void NotifyCurrentFrameChanged ()
		{
			if (currentBacktrace != null)
				pinnedWatches.InvalidateAll ();
			if (CurrentFrameChanged != null)
				CurrentFrameChanged (null, EventArgs.Empty);
		}
		
		static void NotifyCallStackChanged ()
		{
			if (CallStackChanged != null)
				CallStackChanged (null, EventArgs.Empty);
		}
		
		static void OnCancelRequested (object sender, EventArgs args)
		{
			Stop ();
		}

		public static void Stop ()
		{
			if (!IsDebugging)
				return;

			session.Exit ();
			Cleanup ();
		}

		public static void StepInto ()
		{
			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.StepLine ();
			NotifyLocationChanged ();
		}

		public static void StepOver ()
		{
			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.NextLine ();
			NotifyLocationChanged ();
		}

		public static void StepOut ()
		{
			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.Finish ();
			NotifyLocationChanged ();
		}

		public static Backtrace CurrentCallStack {
			get { return currentBacktrace; }
		}

		public static StackFrame CurrentFrame {
			get {
				if (currentBacktrace != null && currentFrame != -1)
					return currentBacktrace.GetFrame (currentFrame);
				else
					return null;
			}
		}
		
		/// <summary>
		/// The deepest stack frame with source above the CurrentFrame
		/// </summary>
		public static StackFrame GetCurrentVisibleFrame ()
		{
			if (currentBacktrace != null && currentFrame != -1) {
				for (int idx = currentFrame; idx < currentBacktrace.FrameCount; idx++) {
					var frame = currentBacktrace.GetFrame (currentFrame);
					if (!frame.IsExternalCode)
						return frame;
				}
			}
			return null;
		}
		
		public static int CurrentFrameIndex {
			get {
				return currentFrame;
			}
			set {
				if (currentBacktrace != null && value < currentBacktrace.FrameCount) {
					currentFrame = value;
					DispatchService.GuiDispatch (delegate {
						NotifyCurrentFrameChanged ();
					});
				}
				else
					currentFrame = -1;
			}
		}
		
		public static ThreadInfo ActiveThread {
			get {
				return session.ActiveThread;
			}
			set {
				session.ActiveThread = value;
				SetCurrentBacktrace (session.ActiveThread.Backtrace);
			}
		}
		
		static void SetCurrentBacktrace (Backtrace bt)
		{
			currentBacktrace = bt;
			if (currentBacktrace != null)
				currentFrame = 0;
			else
				currentFrame = -1;

			DispatchService.GuiDispatch (delegate {
				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
			});
		}
		
		public static void ShowCurrentExecutionLine ()
		{
			if (currentBacktrace != null) {
				var sf = GetCurrentVisibleFrame ();
				if (!string.IsNullOrEmpty (sf.SourceLocation.FileName) && System.IO.File.Exists (sf.SourceLocation.FileName) && sf.SourceLocation.Line != -1) {
					Document document = IdeApp.Workbench.OpenDocument (sf.SourceLocation.FileName, sf.SourceLocation.Line, 1, OpenDocumentOptions.Debugger);
					OnDisableConditionalCompilation (new DocumentEventArgs (document));
				}
			}
		}
		
		public static bool CanDebugCommand (ExecutionCommand command)
		{
			return GetFactoryForCommand (command) != null;
		}
		
		public static DebuggerEngine[] GetDebuggerEngines ()
		{
			if (engines == null) {
				List<DebuggerEngine> engs = new List<DebuggerEngine> ();
				foreach (DebuggerEngineExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath))
					engs.Add (new DebuggerEngine (node));
				
				string[] priorities = EnginePriority;
				engs.Sort (delegate (DebuggerEngine d1, DebuggerEngine d2) {
					int i1 = Array.IndexOf (priorities, d1.Id);
					int i2 = Array.IndexOf (priorities, d2.Id);
					
					//ensure that soft debugger is prioritised over newly installed debuggers
					if (i1 < 0 )
						i1 = d1.Id.StartsWith ("Mono.Debugger.Soft")? 0 : engs.Count;
					if (i2 < 0)
						i2 = d2.Id.StartsWith ("Mono.Debugger.Soft")? 0 : engs.Count;
					
					if (i1 == i2)
						return d1.Name.CompareTo (d2.Name);
					else
						return i1.CompareTo (i2);
				});
				engines = engs.ToArray ();
			}
			return engines;
		}

		public static Dictionary<string, ExpressionEvaluatorExtensionNode> GetExpressionEvaluators()
		{
			if (evaluators == null) {
				Dictionary<string, ExpressionEvaluatorExtensionNode> evgs = new Dictionary<string, ExpressionEvaluatorExtensionNode> (StringComparer.InvariantCultureIgnoreCase);
				foreach (ExpressionEvaluatorExtensionNode node in AddinManager.GetExtensionNodes (EvaluatorsPath))
					evgs.Add (node.extension, node);
				
				evaluators = evgs;
			}
			return evaluators;
		}
		
		static DebuggerEngine GetFactoryForCommand (ExecutionCommand cmd)
		{
			foreach (DebuggerEngine factory in GetDebuggerEngines ()) {
				if (factory.CanDebugCommand (cmd))
					return factory;
			}
			return null;
		}
		
		static void OnLineCountChanged (object ob, LineCountEventArgs a)
		{
			foreach (Breakpoint bp in breakpoints.GetBreakpoints ()) {
				if (bp.FileName == a.TextFile.Name) {
					if (bp.Line > a.LineNumber) {
						// If the line that has the breakpoint is deleted, delete the breakpoint, otherwise update the line #.
						if (bp.Line + a.LineCount >= a.LineNumber)
							breakpoints.UpdateBreakpointLine (bp, bp.Line + a.LineCount);
						else
							breakpoints.Remove (bp);
					}
					else if (bp.Line == a.LineNumber && a.LineCount < 0)
						breakpoints.Remove (bp);
				}
			}
		}
		
		static void OnStoreUserPrefs (object s, UserPreferencesEventArgs args)
		{
			args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService.Breakpoints", breakpoints.Save ());
			args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService.PinnedWatches", pinnedWatches);
		}
		
		static void OnLoadUserPrefs (object s, UserPreferencesEventArgs args)
		{
			XmlElement elem = args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService.Breakpoints");
			if (elem == null)
				elem = args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService");
			if (elem != null)
				breakpoints.Load (elem);
			PinnedWatchStore wstore = args.Properties.GetValue<PinnedWatchStore> ("MonoDevelop.Ide.DebuggingService.PinnedWatches");
			if (wstore != null)
				pinnedWatches.LoadFrom (wstore);
			pinnedWatches.BindAll (breakpoints);
		}
		
		static void OnSolutionClosed (object s, EventArgs args)
		{
			breakpoints.Clear ();
		}
		
		static string ResolveType (string identifier, SourceLocation location)
		{
			Document doc = IdeApp.Workbench.GetDocument (location.FileName);
			if (doc != null) {
				ITextEditorResolver textEditorResolver = doc.GetContent <ITextEditorResolver> ();
				if (textEditorResolver != null) {
					var rr = textEditorResolver.GetLanguageItem (doc.Editor.Document.LocationToOffset (location.Line, 1), identifier);
					var ns = rr as NamespaceResolveResult;
					if (ns != null)
						return ns.NamespaceName;
					var result = rr as TypeResolveResult;
					if (result != null && !result.IsError)
						return result.Type.FullName;
				}
			}
			return null;
		}
		
		public static ExpressionEvaluatorExtensionNode EvaluatorForExtension (string extension)
		{
			ExpressionEvaluatorExtensionNode result;
			if (GetExpressionEvaluators ().TryGetValue (extension, out result))
				return result;
			return null;
		}

		static IExpressionEvaluator OnGetExpressionEvaluator (string extension)
		{
			ExpressionEvaluatorExtensionNode info = EvaluatorForExtension (extension);
			if (info != null)
				return info.Evaluator;
			return null;
		}
	}
	
	internal class FeatureCheckerHandlerFactory: IExecutionHandler
	{
		public DebuggerFeatures SupportedFeatures { get; set; }
		
		public bool CanExecute (ExecutionCommand command)
		{
			SupportedFeatures = DebuggingService.GetSupportedFeaturesForCommand (command);
			return SupportedFeatures != DebuggerFeatures.None;
		}

		public IProcessAsyncOperation Execute (ExecutionCommand cmd, IConsole console)
		{
			// Never called
			throw new System.NotImplementedException();
		}
	}
	
	internal class InternalDebugExecutionHandler: IExecutionHandler
	{
		DebuggerEngine engine;
		
		public InternalDebugExecutionHandler (DebuggerEngine engine)
		{
			this.engine = engine;
		}
		
		public bool CanExecute (ExecutionCommand command)
		{
			return engine.CanDebugCommand (command);
		}

		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			DebugExecutionHandler h = new DebugExecutionHandler (engine);
			return h.Execute (command, console);
		}
	}

	internal class StatusBarConnectionDialog : IConnectionDialog
	{
		public event EventHandler UserCancelled;

		public void SetMessage (DebuggerStartInfo dsi, string message, bool listening, int attemptNumber)
		{
			Gtk.Application.Invoke (delegate {
				IdeApp.Workbench.StatusBar.ShowMessage (MonoDevelop.Ide.Gui.Stock.StatusConnecting, message);
			});
		}

		public void Dispose ()
		{
			Gtk.Application.Invoke (delegate {
				IdeApp.Workbench.StatusBar.ShowReady ();
			});
		}
	}
	
	internal class GtkConnectionDialog : IConnectionDialog
	{
		bool disposed;
		System.Threading.CancellationTokenSource cts;
		
		public event EventHandler UserCancelled;
		
		string DefaultListenMessage {
			get { return GettextCatalog.GetString ("Waiting for debugger to connect..."); }
		}

		public void SetMessage (DebuggerStartInfo dsi, string message, bool listening, int attemptNumber)
		{
			//FIXME: we don't support changing the message
			if (disposed || cts != null)
				return;
			
			cts = new System.Threading.CancellationTokenSource ();
			
			//MessageService is threadsafe but we want this to be async
			Gtk.Application.Invoke (delegate {
				RunDialog (message);
			});
		}
		
		void RunDialog (string message)
		{
			if (disposed)
				return;
			
			string title;
			
			if (message == null) {
				title = GettextCatalog.GetString ("Waiting for debugger");
			} else {
				message = message.Trim ();
				int i = message.IndexOfAny (new char[] { '\n', '\r' });
				if (i > 0) {
					title = message.Substring (0, i).Trim ();
					message = message.Substring (i).Trim ();
				} else {
					title = message;
					message = null;
				}
			}

			var gm = new GenericMessage (title, message, cts.Token);
			gm.Buttons.Add (AlertButton.Cancel);
			gm.DefaultButton = 0;
			MessageService.GenericAlert (gm);
			cts = null;
			
			if (!disposed && UserCancelled != null) {
				UserCancelled (null, null);
			}
		}

		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			var c = cts;
			if (c != null)
				c.Cancel ();
		}
	}
}
