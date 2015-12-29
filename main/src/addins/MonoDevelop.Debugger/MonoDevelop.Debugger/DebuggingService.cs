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

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */
using MonoDevelop.Ide.TextEditing;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Debugger
{
	public static class DebuggingService
	{
		const string FactoriesPath = "/MonoDevelop/Debugging/DebuggerEngines";
		static DebuggerEngine[] engines;
		
		const string EvaluatorsPath = "/MonoDevelop/Debugging/Evaluators";
		static Dictionary<string, ExpressionEvaluatorExtensionNode> evaluators;

		static readonly PinnedWatchStore pinnedWatches = new PinnedWatchStore ();
		static readonly BreakpointStore breakpoints = new BreakpointStore ();
		static readonly DebugExecutionHandlerFactory executionHandlerFactory;
		
		static OperationConsole console;
		static IDisposable cancelRegistration;

		static Dictionary<long, SourceLocation> nextStatementLocations = new Dictionary<long, SourceLocation> ();
		static DebuggerEngine currentEngine;
		static DebuggerSession session;
		static Backtrace currentBacktrace;
		static int currentFrame;
		
		static ExceptionCaughtMessage exceptionDialog;
		
		static BusyEvaluatorDialog busyDialog;
		static StatusBarIcon busyStatusIcon;
		static bool isBusy;

		static DebugAsyncOperation currentDebugOperation = new DebugAsyncOperation ();

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
				busyDialog.Modal = true;
				busyDialog.TransientFor = MessageService.RootWindow;
				busyDialog.DestroyWithParent = true;
			};
			AddinManager.AddExtensionNodeHandler (FactoriesPath, delegate {
				// Refresh the engines list
				engines = null;
			});
			AddinManager.AddExtensionNodeHandler (EvaluatorsPath, delegate {
				// Refresh the evaluators list
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
				var bp = new Breakpoint (watch.File, watch.Line);
				bp.TraceExpression = "{" + watch.Expression + "}";
				bp.HitAction |= HitAction.PrintExpression;
				lock (breakpoints)
					breakpoints.Add (bp);
				pinnedWatches.Bind (watch, bp);
			} else {
				pinnedWatches.Bind (watch, null);
				lock (breakpoints)
					breakpoints.Remove (watch.BoundTracer);
			}
		}
		
		static void BreakpointTraceHandler (BreakEvent be, string trace)
		{
			if (be is Breakpoint) {
				if (pinnedWatches.UpdateLiveWatch ((Breakpoint) be, trace))
					return; // No need to log the value. It is shown in the watch.
			}
			DebugWriter (0, "", trace + Environment.NewLine);
		}

		[Obsolete]
		public static string[] EnginePriority {
			get { return new string[0]; }
			set {
			}
		}

		internal static IEnumerable<ValueVisualizer> GetValueVisualizers (ObjectValue val)
		{
			foreach (object v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/ValueVisualizers", false)) {
				if (v is ValueVisualizer) {
					var vv = (ValueVisualizer)v;
					if (vv.CanVisualize (val))
						yield return vv;
				}
			}
		}

		internal static bool HasValueVisualizers (ObjectValue val)
		{
			return GetValueVisualizers (val).Any ();
		}

		internal static InlineVisualizer GetInlineVisualizer (ObjectValue val)
		{
			foreach (object v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/InlineVisualizers", true)) {
				var cv = v as InlineVisualizer;
				if (cv != null && cv.CanInlineVisualize (val)) {
					return cv;
				}
			}
			return null;
		}

		internal static bool HasInlineVisualizer (ObjectValue val)
		{
			return GetInlineVisualizer (val) != null;
		}

		internal static PreviewVisualizer GetPreviewVisualizer (ObjectValue val)
		{
			foreach (object v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/PreviewVisualizers", true)) {
				var cv = v as PreviewVisualizer;
				if (cv != null && cv.CanVisualize (val)) {
					return cv;
				}
			}
			return null;
		}

		internal static bool HasPreviewVisualizer (ObjectValue val)
		{
			return GetPreviewVisualizer (val) != null;
		}

		public static DebugValueConverter<T> GetGetConverter<T> (ObjectValue val)
		{
			foreach (object v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/DebugValueConverters", true)) {
				var cv = v as DebugValueConverter<T>;
				if (cv != null && cv.CanGetValue (val)) {
					return cv;
				}
			}
			return null;
		}

		public static bool HasGetConverter<T> (ObjectValue val)
		{
			return GetGetConverter<T> (val) != null;
		}

		public static DebugValueConverter<T> GetSetConverter<T> (ObjectValue val)
		{
			foreach (object v in AddinManager.GetExtensionObjects ("/MonoDevelop/Debugging/DebugValueConverters", true)) {
				var cv = v as DebugValueConverter<T>;
				if (cv != null && cv.CanSetValue (val)) {
					return cv;
				}
			}
			return null;
		}

		public static bool HasSetConverter<T> (ObjectValue val)
		{
			return GetSetConverter<T> (val) != null;
		}
		
		public static void ShowValueVisualizer (ObjectValue val)
		{
			using (var dlg = new ValueVisualizerDialog ()) {
				dlg.Show (val);
				MessageService.ShowCustomDialog (dlg);
			}
		}

		public static void ShowPreviewVisualizer (ObjectValue val, MonoDevelop.Components.Control widget, Gdk.Rectangle previewButtonArea)
		{
			PreviewWindowManager.Show (val, widget, previewButtonArea);
		}
		
		public static bool ShowBreakpointProperties (ref BreakEvent bp, BreakpointType breakpointType = BreakpointType.Location)
		{
			using (var dlg = new BreakpointPropertiesDialog (bp, breakpointType)) {
				Xwt.Command response = dlg.Run ();
				if (bp == null)
					bp = dlg.GetBreakEvent ();
				return response == Xwt.Command.Ok;
			}
		}

		public static void AddWatch (string expression)
		{
			var pad = IdeApp.Workbench.GetPad<WatchPad> ();
			var wp = (WatchPad) pad.Content;

			pad.BringToFront (false);
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
			foreach (var engine in GetDebuggerEngines ())
				if ((engine.SupportedFeatures & feature) == feature)
					return true;
			return false;
		}

		public static DebuggerFeatures GetSupportedFeatures (IBuildTarget target)
		{
			var fc = new FeatureCheckerHandlerFactory ();
			var ctx = new ExecutionContext (fc, null, IdeApp.Workspace.ActiveExecutionTarget);

			target.CanExecute (ctx, IdeApp.Workspace.ActiveConfiguration);

			return fc.SupportedFeatures;
		}

		public static DebuggerFeatures GetSupportedFeaturesForCommand (ExecutionCommand command)
		{
			var engine = GetFactoryForCommand (command);

			return engine != null ? engine.SupportedFeatures : DebuggerFeatures.None;
		}

		public static void ShowExpressionEvaluator (string expression)
		{
			using (var dlg = new ExpressionEvaluatorDialog ()) {
				if (expression != null)
					dlg.Expression = expression;

				MessageService.ShowCustomDialog (dlg);
			}
		}

		public static void ShowExceptionCaughtDialog ()
		{
			var ops = session.EvaluationOptions.Clone ();
			ops.MemberEvaluationTimeout = 0;
			ops.EvaluationTimeout = 0;
			ops.EllipsizeStrings = false;

			var val = CurrentFrame.GetException (ops);
			if (val != null) {
				HideExceptionCaughtDialog ();
				exceptionDialog = new ExceptionCaughtMessage (val, CurrentFrame.SourceLocation.FileName, CurrentFrame.SourceLocation.Line, CurrentFrame.SourceLocation.Column);
				if (CurrentFrame.SourceLocation.FileName != null) {
					exceptionDialog.ShowButton ();
				} else {
					exceptionDialog.ShowDialog ();
				}
				exceptionDialog.Closed += (o, args) => exceptionDialog = null;
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
		
		static void SetupSession ()
		{
			isBusy = false;
			session.Breakpoints = breakpoints;
			session.TargetEvent += OnTargetEvent;
			session.TargetStarted += OnStarted;
			session.OutputWriter = OutputWriter;
			session.LogWriter = LogWriter;
			session.DebugWriter = DebugWriter;
			session.BusyStateChanged += OnBusyStateChanged;
			session.TypeResolverHandler = ResolveType;
			session.BreakpointTraceHandler = BreakpointTraceHandler;
			session.GetExpressionEvaluator = OnGetExpressionEvaluator;
			session.ConnectionDialogCreator = delegate {
				return new StatusBarConnectionDialog ();
			};
			currentDebugOperation = new DebugAsyncOperation ();

			cancelRegistration = console.CancellationToken.Register (Stop);
			
			Runtime.RunInMainThread (delegate {
				if (DebugSessionStarted != null)
					DebugSessionStarted (null, EventArgs.Empty);
				NotifyLocationChanged ();
			});

		}

		static readonly object cleanup_lock = new object ();
		static void Cleanup ()
		{
			DebuggerSession currentSession;
			StatusBarIcon currentIcon;
			OperationConsole currentConsole;

			lock (cleanup_lock) {
				if (!IsDebugging)
					return;

				currentIcon = busyStatusIcon;
				currentSession = session;
				currentConsole = console;

				nextStatementLocations.Clear ();
				currentBacktrace = null;
				busyStatusIcon = null;
				session = null;
				console = null;
				pinnedWatches.InvalidateAll ();
			}

			UnsetDebugLayout ();

			currentSession.BusyStateChanged -= OnBusyStateChanged;
			currentSession.TargetEvent -= OnTargetEvent;
			currentSession.TargetStarted -= OnStarted;

			currentSession.BreakpointTraceHandler = null;
			currentSession.GetExpressionEvaluator = null;
			currentSession.TypeResolverHandler = null;
			currentSession.OutputWriter = null;
			currentSession.LogWriter = null;
			currentDebugOperation.Cleanup ();

			if (currentConsole != null) {
				cancelRegistration.Dispose ();
				currentConsole.Dispose ();
			}
			
			Runtime.RunInMainThread (delegate {
				HideExceptionCaughtDialog ();

				if (currentIcon != null) {
					currentIcon.Dispose ();
					currentIcon = null;
				}

				if (StoppedEvent != null)
					StoppedEvent (null, new EventArgs ());

				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
				NotifyLocationChanged ();
			});

			currentSession.Dispose ();
		}

		static string oldLayout;
		static void UnsetDebugLayout ()
		{
			// Dispatch synchronously to avoid start/stop races
			Runtime.RunInMainThread (delegate {
				IdeApp.Workbench.HideCommandBar ("Debug");
				if (IdeApp.Workbench.CurrentLayout == "Debug") {
					IdeApp.Workbench.CurrentLayout = oldLayout ?? "Solution";
				}
				oldLayout = null;
			}).Wait ();
		}

		static void SetDebugLayout ()
		{
			// Dispatch synchronously to avoid start/stop races
			Runtime.RunInMainThread (delegate {
				oldLayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = "Debug";
				IdeApp.Workbench.ShowCommandBar ("Debug");
			}).Wait ();
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
			Runtime.AssertMainThread ();
			if (CheckIsBusy ())
				return;

			session.Continue ();
			NotifyLocationChanged ();
		}

		public static void RunToCursor (string fileName, int line, int column)
		{
			Runtime.AssertMainThread ();
			if (CheckIsBusy ())
				return;

			var bp = new RunToCursorBreakpoint (fileName, line, column);
			Breakpoints.Add (bp);

			session.Continue ();
			NotifyLocationChanged ();
		}

		public static void SetNextStatement (string fileName, int line, int column)
		{
			Runtime.AssertMainThread ();
			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.SetNextStatement (fileName, line, column);

			var location = new SourceLocation (CurrentFrame.SourceLocation.MethodName, fileName, line);
			nextStatementLocations[session.ActiveThread.Id] = location;
			NotifyLocationChanged ();
		}

		public static ProcessAsyncOperation Run (string file, OperationConsole console)
		{
			var cmd = Runtime.ProcessService.CreateCommand (file);
			return Run (cmd, console);
		}

		public static ProcessAsyncOperation Run (string file, string args, string workingDir, IDictionary<string,string> envVars, OperationConsole console)
		{
			var cmd = Runtime.ProcessService.CreateCommand (file);
			if (args != null) 
				cmd.Arguments = args;
			if (workingDir != null)
				cmd.WorkingDirectory = workingDir;
			if (envVars != null)
				cmd.EnvironmentVariables = envVars;
			return Run (cmd, console);
		}

		public static ProcessAsyncOperation Run (ExecutionCommand cmd, OperationConsole console, DebuggerEngine engine = null)
		{
			InternalRun (cmd, engine, console);
			return currentDebugOperation;
		}
		
		public static AsyncOperation AttachToProcess (DebuggerEngine debugger, ProcessInfo proc)
		{
			currentEngine = debugger;
			session = debugger.CreateSession ();
			session.ExceptionHandler = ExceptionHandler;
			var monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			console = monitor.Console;
			SetupSession ();
			session.TargetExited += delegate {
				monitor.Dispose ();
			};
			SetDebugLayout ();
			session.AttachToProcess (proc, GetUserOptions ());
			return currentDebugOperation;
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
			return new DebuggerSessionOptions {
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
		
		internal static void InternalRun (ExecutionCommand cmd, DebuggerEngine factory, OperationConsole c)
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
			if (startInfo.UseExternalConsole)
				startInfo.CloseExternalConsoleOnExit = ((ExternalConsole)c).CloseOnDispose;
			currentEngine = factory;
			session = factory.CreateSession ();
			session.ExceptionHandler = ExceptionHandler;
			
			// When using an external console, create a new internal console which will be used
			// to show the debugger log
			if (startInfo.UseExternalConsole)
				console = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ().Console;
			else
				console = c;
			
			SetupSession ();
			
			SetDebugLayout ();
			
			try {
				session.Run (startInfo, GetUserOptions ());
			} catch {
				Cleanup ();
				throw;
			}
		}
		
		static bool ExceptionHandler (Exception ex)
		{
			Gtk.Application.Invoke (delegate {
				if (ex is DebuggerException)
					MessageService.ShowError (ex.Message, ex);
				else
					MessageService.ShowError ("Debugger operation failed", ex);
			});
			return true;
		}
		
		static void LogWriter (bool iserr, string text)
		{
			// Events may come with a bit of delay, so the debug session
			// may already have been cleaned up
			var logger = console;

			if (logger != null)
				logger.Log.Write (text);
		}

		static void DebugWriter (int level, string category, string message)
		{
			var logger = console;

			if (logger != null)
				logger.Debug (level, category, message);
		}

		static void OutputWriter (bool iserr, string text)
		{
			var logger = console;

			if (logger != null) {
				if (iserr)
					logger.Error.Write (text);
				else
					logger.Out.Write (text);
			}
		}

		static void OnBusyStateChanged (object s, BusyStateEventArgs args)
		{
			isBusy = args.IsBusy;
			Runtime.RunInMainThread (delegate {
				busyDialog.UpdateBusyState (args);
				if (args.IsBusy) {
					if (busyStatusIcon == null) {
						busyStatusIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (ImageService.GetIcon ("md-execute-debug", Gtk.IconSize.Menu));
						busyStatusIcon.SetAlertMode (100);
						busyStatusIcon.ToolTip = GettextCatalog.GetString ("The debugger runtime is not responding. You can wait for it to recover, or stop debugging.");
						busyStatusIcon.Clicked += delegate {
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
			nextStatementLocations.Clear ();
			currentBacktrace = null;

			Runtime.RunInMainThread (delegate {
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
			if (args.BreakEvent != null && args.BreakEvent.NonUserBreakpoint)
				return;
			nextStatementLocations.Clear ();

			try {
				switch (args.Type) {
				case TargetEventType.TargetExited:
					Breakpoints.RemoveRunToCursorBreakpoints ();
					Cleanup ();
					break;
				case TargetEventType.TargetSignaled:
				case TargetEventType.TargetStopped:
				case TargetEventType.TargetHitBreakpoint:
				case TargetEventType.TargetInterrupted:
				case TargetEventType.UnhandledException:
				case TargetEventType.ExceptionThrown:
					Breakpoints.RemoveRunToCursorBreakpoints ();
					SetCurrentBacktrace (args.Backtrace);
					NotifyPaused ();
					NotifyException (args);
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
			Runtime.RunInMainThread (delegate {
				if (PausedEvent != null)
					PausedEvent (null, EventArgs.Empty);
				NotifyLocationChanged ();
				IdeApp.Workbench.GrabDesktopFocus ();
			});
		}
		
		static void NotifyException (TargetEventArgs args)
		{
			if (args.Type == TargetEventType.UnhandledException || args.Type == TargetEventType.ExceptionThrown) {
				Runtime.RunInMainThread (delegate {
					if (CurrentFrame != null) {
						ShowExceptionCaughtDialog ();
					}
				});
			}
		}
		
		static void NotifyLocationChanged ()
		{
			Runtime.AssertMainThread ();
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
		
		public static void Stop ()
		{
			if (!IsDebugging)
				return;

			session.Exit ();
			Cleanup ();
		}

		public static void StepInto ()
		{
			Runtime.AssertMainThread ();

			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.StepLine ();
			NotifyLocationChanged ();
		}

		public static void StepOver ()
		{
			Runtime.AssertMainThread ();

			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.NextLine ();
			NotifyLocationChanged ();
		}

		public static void StepOut ()
		{
			Runtime.AssertMainThread ();

			if (!IsDebugging || IsRunning || CheckIsBusy ())
				return;

			session.Finish ();
			NotifyLocationChanged ();
		}

		public static Backtrace CurrentCallStack {
			get { return currentBacktrace; }
		}

		public static SourceLocation NextStatementLocation {
			get {
				SourceLocation location = null;

				if (IsPaused)
					nextStatementLocations.TryGetValue (session.ActiveThread.Id, out location);

				return location;
			}
		}

		public static StackFrame CurrentFrame {
			get {
				if (currentBacktrace != null && currentFrame != -1)
					return currentBacktrace.GetFrame (currentFrame);

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
					Runtime.RunInMainThread (delegate {
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

			Runtime.RunInMainThread (delegate {
				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
				NotifyLocationChanged ();
			});
		}
		
		public static void ShowCurrentExecutionLine ()
		{
			Runtime.AssertMainThread ();
			if (currentBacktrace != null) {
				var sf = GetCurrentVisibleFrame ();
				if (sf != null && !string.IsNullOrEmpty (sf.SourceLocation.FileName) && System.IO.File.Exists (sf.SourceLocation.FileName) && sf.SourceLocation.Line != -1) {
					Document document = IdeApp.Workbench.OpenDocument (sf.SourceLocation.FileName, null, sf.SourceLocation.Line, 1, OpenDocumentOptions.Debugger);
					OnDisableConditionalCompilation (new DocumentEventArgs (document));
				}
			}
		}

		public static void ShowNextStatement ()
		{
			Runtime.AssertMainThread ();
			var location = NextStatementLocation;

			if (location != null && System.IO.File.Exists (location.FileName)) {
				Document document = IdeApp.Workbench.OpenDocument (location.FileName, null, location.Line, 1, OpenDocumentOptions.Debugger);
				OnDisableConditionalCompilation (new DocumentEventArgs (document));
			} else {
				ShowCurrentExecutionLine ();
			}
		}
		
		public static bool CanDebugCommand (ExecutionCommand command)
		{
			return GetFactoryForCommand (command) != null;
		}
		
		public static DebuggerEngine[] GetDebuggerEngines ()
		{
			if (engines == null) {
				var list = new List<DebuggerEngine> ();

				foreach (DebuggerEngineExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath))
					list.Add (new DebuggerEngine (node));

				engines = list.ToArray ();
			}

			return engines;
		}

		public static Dictionary<string, ExpressionEvaluatorExtensionNode> GetExpressionEvaluators()
		{
			if (evaluators == null) {
				var evgs = new Dictionary<string, ExpressionEvaluatorExtensionNode> (StringComparer.InvariantCultureIgnoreCase);
				foreach (ExpressionEvaluatorExtensionNode node in AddinManager.GetExtensionNodes (EvaluatorsPath))
					evgs.Add (node.extension, node);
				
				evaluators = evgs;
			}
			return evaluators;
		}
		
		static DebuggerEngine GetFactoryForCommand (ExecutionCommand cmd)
		{
			DebuggerEngine supportedEngine = null;

			// Get the default engine for the command if available,
			// or the first engine that supports the command otherwise

			foreach (DebuggerEngine factory in GetDebuggerEngines ()) {
				if (factory.CanDebugCommand (cmd)) {
					if (factory.IsDefaultDebugger (cmd))
						return factory;
					if (supportedEngine == null)
						supportedEngine = factory;
				}
			}
			return supportedEngine;
		}
		
		static void OnLineCountChanged (object ob, LineCountEventArgs a)
		{
			lock (breakpoints) {
				foreach (Breakpoint bp in breakpoints.GetBreakpoints ()) {
					if (bp.FileName == a.TextFile.Name) {
						if (bp.Line > a.LineNumber) {
							// If the line that has the breakpoint is deleted, delete the breakpoint, otherwise update the line #.
							if (bp.Line + a.LineCount >= a.LineNumber)
								breakpoints.UpdateBreakpointLine (bp, bp.Line + a.LineCount);
							else
								breakpoints.Remove (bp);
						} else if (bp.Line == a.LineNumber && a.LineCount < 0)
							breakpoints.Remove (bp);
					}
				}
			}
		}
		
		static void OnStoreUserPrefs (object s, UserPreferencesEventArgs args)
		{
			lock (breakpoints)
				args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService.Breakpoints", breakpoints.Save ());
			args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService.PinnedWatches", pinnedWatches);
		}
		
		static void OnLoadUserPrefs (object s, UserPreferencesEventArgs args)
		{
			var elem = args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService.Breakpoints") ?? args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService");

			if (elem != null) {
				lock (breakpoints)
					breakpoints.Load (elem);
			}

			PinnedWatchStore wstore = args.Properties.GetValue<PinnedWatchStore> ("MonoDevelop.Ide.DebuggingService.PinnedWatches");
			if (wstore != null)
				pinnedWatches.LoadFrom (wstore);

			lock (breakpoints)
				pinnedWatches.BindAll (breakpoints);
		}
		
		static void OnSolutionClosed (object s, EventArgs args)
		{
			lock (breakpoints)
				breakpoints.Clear ();
		}
		
		static string ResolveType (string identifier, SourceLocation location)
		{
			Document doc = IdeApp.Workbench.GetDocument (location.FileName);
			if (doc != null) {
				ITextEditorResolver textEditorResolver = doc.GetContent <ITextEditorResolver> ();
				if (textEditorResolver != null) {
					var rr = textEditorResolver.GetLanguageItem (doc.Editor.LocationToOffset (location.Line, 1), identifier);
					var ns = rr as Microsoft.CodeAnalysis.INamespaceSymbol;
					if (ns != null)
						return ns.ToDisplayString (Microsoft.CodeAnalysis.SymbolDisplayFormat.CSharpErrorMessageFormat);
					var result = rr as Microsoft.CodeAnalysis.INamedTypeSymbol;
					if (result != null && !(result.TypeKind == Microsoft.CodeAnalysis.TypeKind.Dynamic && result.ToDisplayString (Microsoft.CodeAnalysis.SymbolDisplayFormat.CSharpErrorMessageFormat) == "dynamic")) {
						return result.ToDisplayString (new Microsoft.CodeAnalysis.SymbolDisplayFormat (
							typeQualificationStyle: Microsoft.CodeAnalysis.SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
							miscellaneousOptions:
							Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
							Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
					}
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
			var info = EvaluatorForExtension (extension);

			return info != null ? info.Evaluator : null;
		}
	}
	
	class FeatureCheckerHandlerFactory: IExecutionHandler
	{
		public DebuggerFeatures SupportedFeatures { get; set; }
		
		public bool CanExecute (ExecutionCommand command)
		{
			SupportedFeatures = DebuggingService.GetSupportedFeaturesForCommand (command);
			return SupportedFeatures != DebuggerFeatures.None;
		}

		public ProcessAsyncOperation Execute (ExecutionCommand cmd, OperationConsole console)
		{
			// Never called
			throw new NotImplementedException ();
		}
	}
	
	class InternalDebugExecutionHandler: IExecutionHandler
	{
		readonly DebuggerEngine engine;
		
		public InternalDebugExecutionHandler (DebuggerEngine engine)
		{
			this.engine = engine;
		}
		
		public bool CanExecute (ExecutionCommand command)
		{
			return engine.CanDebugCommand (command);
		}

		public ProcessAsyncOperation Execute (ExecutionCommand command, OperationConsole console)
		{
			return DebuggingService.Run (command, console, engine);
		}
	}

	class StatusBarConnectionDialog : IConnectionDialog
	{
		public event EventHandler UserCancelled;

		public void SetMessage (DebuggerStartInfo dsi, string message, bool listening, int attemptNumber)
		{
			Gtk.Application.Invoke (delegate {
				IdeApp.Workbench.StatusBar.ShowMessage (Ide.Gui.Stock.StatusConnecting, message);
			});
		}

		public void Dispose ()
		{
			Gtk.Application.Invoke (delegate {
				IdeApp.Workbench.StatusBar.ShowReady ();
			});
		}
	}
	
	class GtkConnectionDialog : IConnectionDialog
	{
		static readonly string DefaultListenMessage = GettextCatalog.GetString ("Waiting for debugger to connect...");
		System.Threading.CancellationTokenSource cts;
		bool disposed;
		
		public event EventHandler UserCancelled;

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
				int i = message.IndexOfAny (new [] { '\n', '\r' });
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
