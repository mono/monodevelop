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
using System.Collections.Concurrent;
using System.Threading;

namespace MonoDevelop.Debugger
{
	public static class DebuggingService
	{
		const string FactoriesPath = "/MonoDevelop/Debugging/DebuggerEngines";
		static DebuggerEngine [] engines;

		const string EvaluatorsPath = "/MonoDevelop/Debugging/Evaluators";
		static Dictionary<string, ExpressionEvaluatorExtensionNode> evaluators;

		static readonly PinnedWatchStore pinnedWatches = new PinnedWatchStore ();
		static readonly BreakpointStore breakpoints = new BreakpointStore ();
		static readonly DebugExecutionHandlerFactory executionHandlerFactory;

		static Dictionary<long, SourceLocation> nextStatementLocations = new Dictionary<long, SourceLocation> ();
		static Dictionary<DebuggerSession, SessionManager> sessions = new Dictionary<DebuggerSession, SessionManager> ();
		static Backtrace currentBacktrace;
		static SessionManager currentSession;
		static int currentFrame;

		static ExceptionCaughtMessage exceptionDialog;

		static BusyEvaluator busyEvaluator;
		static StatusBarIcon busyStatusIcon;
		static bool isBusy;

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
				busyEvaluator = new BusyEvaluator ();
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
			get { return currentSession?.Session ?? sessions.Values.FirstOrDefault ()?.Session; }
		}


		public static DebuggerSession [] GetSessions ()
		{
			return sessions.Keys.ToArray ();
		}

		public static ProcessInfo [] GetProcesses ()
		{
			return sessions.Keys.Where (s => !s.IsRunning).SelectMany (s => s.GetProcesses ()).ToArray ();
		}

		public static BreakEventStatus GetBreakpointStatus (Breakpoint bp)
		{
			var result = BreakEventStatus.Disconnected;
			foreach (var sesion in sessions.Keys.ToArray ()) {
				var status = bp.GetStatus (sesion);
				if (status == BreakEventStatus.Bound)
					return BreakEventStatus.Bound;
				else
					result = status;
			}
			return result;
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
				var bp = pinnedWatches.CreateLiveUpdateBreakpoint (watch);
				pinnedWatches.Bind (watch, bp);
				lock (breakpoints)
					breakpoints.Add(bp);
			} else {
				pinnedWatches.Bind (watch, null);
				lock (breakpoints)
					breakpoints.Remove (watch.BoundTracer);
			}
		}

		[Obsolete]
		public static string [] EnginePriority {
			get { return new string [0]; }
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

		public static bool ShowValueVisualizer (ObjectValue val)
		{
			using (var dlg = new ValueVisualizerDialog ()) {
				dlg.Show (val);
				return MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok;
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
			var wp = (WatchPad)pad.Content;

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
			return (currentSession.Engine.SupportedFeatures & feature) == feature;
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
			var ctx = new Projects.ExecutionContext (fc, null, IdeApp.Workspace.ActiveExecutionTarget);

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
			var dlg = new ExpressionEvaluatorDialog ();
			if (expression != null)
				dlg.Expression = expression;
			dlg.TransientFor = MessageService.RootWindow;
			dlg.Show ();
			MessageService.PlaceDialog (dlg, MessageService.RootWindow);
		}

		public static void ShowExceptionCaughtDialog ()
		{
			var ops = GetUserOptions ().EvaluationOptions;
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

		static void SetupSession (SessionManager sessionManager)
		{
			sessions.Add (sessionManager.Session, sessionManager);
			isBusy = false;
			var session = sessionManager.Session;
			session.Breakpoints = breakpoints;
			session.TargetEvent += OnTargetEvent;
			session.TargetStarted += OnStarted;
			session.OutputWriter = sessionManager.OutputWriter;
			session.LogWriter = sessionManager.LogWriter;
			session.DebugWriter = sessionManager.DebugWriter;
			session.BusyStateChanged += OnBusyStateChanged;
			session.TypeResolverHandler = ResolveType;
			session.BreakpointTraceHandler = sessionManager.BreakpointTraceHandler;
			session.GetExpressionEvaluator = OnGetExpressionEvaluator;
			session.ConnectionDialogCreatorExtended = delegate (DebuggerStartInfo dsi) {
				if (dsi.RequiresManualStart)
					return new GtkConnectionDialog ();

				return new StatusBarConnectionDialog ();
			};

			Runtime.RunInMainThread (delegate {
				if (DebugSessionStarted != null)
					DebugSessionStarted (session, EventArgs.Empty);
				NotifyLocationChanged ();
			});
		}

		static readonly object cleanup_lock = new object ();
		static void Cleanup (SessionManager sessionManager)
		{
			StatusBarIcon currentIcon;

			var cleaningCurrentSession = sessionManager == currentSession;
			lock (cleanup_lock) {
				if (!IsDebugging)
					return;

				currentIcon = busyStatusIcon;

				nextStatementLocations.Clear ();
				if (cleaningCurrentSession) {
					currentSession = null;
					currentBacktrace = null;
				}
				busyStatusIcon = null;
				sessions.Remove (sessionManager.Session);
				pinnedWatches.InvalidateAll ();
			}

			if (sessions.Count == 0)
				UnsetDebugLayout ();
			var session = sessionManager.Session;
			session.BusyStateChanged -= OnBusyStateChanged;
			session.TargetEvent -= OnTargetEvent;
			session.TargetStarted -= OnStarted;

			session.BreakpointTraceHandler = null;
			session.GetExpressionEvaluator = null;
			session.TypeResolverHandler = null;
			session.OutputWriter = null;
			session.LogWriter = null;

			Runtime.RunInMainThread (delegate {
				if (cleaningCurrentSession)
					HideExceptionCaughtDialog ();

				if (currentIcon != null) {
					currentIcon.Dispose ();
					currentIcon = null;
				}

				if (StoppedEvent != null)
					StoppedEvent (session, new EventArgs ());

				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
				NotifyLocationChanged ();
			}).ContinueWith ((t) => {
				sessionManager.Dispose ();
			});
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
				return sessions.Count > 0;
			}
		}

		public static bool IsConnected {
			get {
				return IsDebugging && sessions.Keys.Any (s => s.IsConnected);
			}
		}

		public static bool IsRunning {
			get {
				return IsDebugging && sessions.Keys.Any (s => s.IsRunning);
			}
		}

		public static bool IsPaused {
			get {
				return IsDebugging && currentSession != null && currentBacktrace != null;
			}
		}

		public static void Pause ()
		{
			foreach (var session in sessions.Keys.ToArray ()) {
				if (session.IsRunning)
					session.Stop ();
			}
		}

		static ConcurrentQueue<Func<bool>> StopsQueue = new ConcurrentQueue<Func<bool>> ();

		static bool HandleStopQueue ()
		{
			Func<bool> delayedStop;
			while (StopsQueue.TryDequeue (out delayedStop)) {
				//Returns false if session which scheduled stop is terminated
				//So we just ignore it's stop entry and keep processing others or resume
				if (delayedStop ())
					return true;
			}
			return false;
		}

		public static void Resume ()
		{
			Runtime.AssertMainThread ();
			if (CheckIsBusy ())
				return;
			if (HandleStopQueue ())
				return;

			foreach (var session in sessions.Keys.ToArray ()) {
				if (!session.IsRunning)
					session.Continue ();
			}
			NotifyLocationChanged ();
		}

		public static void RunToCursor (string fileName, int line, int column)
		{
			Runtime.AssertMainThread ();
			if (CheckIsBusy ())
				return;

			var bp = new RunToCursorBreakpoint (fileName, line, column);
			Breakpoints.Add (bp);

			Resume ();
			NotifyLocationChanged ();
		}

		public static void SetNextStatement (string fileName, int line, int column)
		{
			Runtime.AssertMainThread ();
			if (!IsDebugging || !IsPaused || CheckIsBusy ())
				return;

			currentSession.Session.SetNextStatement (fileName, line, column);

			var location = new SourceLocation (CurrentFrame.SourceLocation.MethodName, fileName, line, column, -1, -1, null);
			nextStatementLocations [ActiveThread.Id] = location;
			NotifyLocationChanged ();
		}

		public static ProcessAsyncOperation Run (string file, OperationConsole console)
		{
			var cmd = Runtime.ProcessService.CreateCommand (file);
			return Run (cmd, console);
		}

		public static ProcessAsyncOperation Run (string file, string args, string workingDir, IDictionary<string, string> envVars, OperationConsole console)
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
			return InternalRun (cmd, engine, console);
		}

		public static AsyncOperation AttachToProcess (DebuggerEngine debugger, ProcessInfo proc)
		{
			var session = debugger.CreateSession ();
			var monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor (proc.Name);
			var sessionManager = new SessionManager (session, monitor.Console, debugger);
			SetupSession (sessionManager);
			session.TargetExited += delegate {
				monitor.Dispose ();
			};
			SetDebugLayout ();
			session.AttachToProcess (proc, GetUserOptions ());
			return sessionManager.debugOperation;
		}

		public static DebuggerSessionOptions GetUserOptions ()
		{
			EvaluationOptions eval = EvaluationOptions.DefaultOptions;
			eval.AllowTargetInvoke = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AllowTargetInvoke", true);
			eval.AllowToStringCalls = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AllowToStringCalls", true);
			eval.EvaluationTimeout = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.EvaluationTimeout", 2500);
			eval.FlattenHierarchy = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.FlattenHierarchy", false);
			eval.GroupPrivateMembers = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.GroupPrivateMembers", true);
			eval.EllipsizedLength = 260; // Instead of random default(100), lets use 260 which should cover 99.9% of file path cases
			eval.GroupStaticMembers = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.GroupStaticMembers", true);
			eval.MemberEvaluationTimeout = eval.EvaluationTimeout * 2;
			eval.StackFrameFormat = new StackFrameFormat () {
				Module = PropertyService.Get ("Monodevelop.StackTrace.ShowModuleName", eval.StackFrameFormat.Module),
				ParameterTypes = PropertyService.Get ("Monodevelop.StackTrace.ShowParameterType", eval.StackFrameFormat.ParameterTypes),
				ParameterNames = PropertyService.Get ("Monodevelop.StackTrace.ShowParameterName", eval.StackFrameFormat.ParameterNames),
				ParameterValues = PropertyService.Get ("Monodevelop.StackTrace.ShowParameterValue", eval.StackFrameFormat.ParameterValues),
				Line = PropertyService.Get ("Monodevelop.StackTrace.ShowLineNumber", eval.StackFrameFormat.Line),
				ExternalCode = PropertyService.Get ("Monodevelop.StackTrace.ShowExternalCode", eval.StackFrameFormat.ExternalCode)
			};
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


			PropertyService.Set ("Monodevelop.StackTrace.ShowModuleName", options.EvaluationOptions.StackFrameFormat.Module);
			PropertyService.Set ("Monodevelop.StackTrace.ShowParameterType", options.EvaluationOptions.StackFrameFormat.ParameterTypes);
			PropertyService.Set ("Monodevelop.StackTrace.ShowParameterName", options.EvaluationOptions.StackFrameFormat.ParameterNames);
			PropertyService.Set ("Monodevelop.StackTrace.ShowParameterValue", options.EvaluationOptions.StackFrameFormat.ParameterValues);
			PropertyService.Set ("Monodevelop.StackTrace.ShowLineNumber", options.EvaluationOptions.StackFrameFormat.Line);
			PropertyService.Set ("Monodevelop.StackTrace.ShowExternalCode", options.EvaluationOptions.StackFrameFormat.ExternalCode);

			foreach (var session in sessions.Keys.ToArray ()) {
				session.Options.EvaluationOptions = GetUserOptions ().EvaluationOptions;
			}
			if (EvaluationOptionsChanged != null)
				EvaluationOptionsChanged (null, EventArgs.Empty);
		}

		public static void ShowDisassembly ()
		{
			if (DisassemblyRequested != null)
				DisassemblyRequested (null, EventArgs.Empty);
		}

		internal static ProcessAsyncOperation InternalRun (ExecutionCommand cmd, DebuggerEngine factory, OperationConsole c)
		{
			if (factory == null) {
				factory = GetFactoryForCommand (cmd);
				if (factory == null)
					throw new InvalidOperationException ("Unsupported command: " + cmd);
			}

			DebuggerStartInfo startInfo = factory.CreateDebuggerStartInfo (cmd);
			startInfo.UseExternalConsole = c is ExternalConsole;
			if (startInfo.UseExternalConsole)
				startInfo.CloseExternalConsoleOnExit = ((ExternalConsole)c).CloseOnDispose;

			var session = factory.CreateSession ();

			SessionManager sessionManager;
			// When using an external console, create a new internal console which will be used
			// to show the debugger log
			if (startInfo.UseExternalConsole)
				sessionManager = new SessionManager (session, IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor (System.IO.Path.GetFileNameWithoutExtension (startInfo.Command)).Console, factory);
			else
				sessionManager = new SessionManager (session, c, factory);
			SetupSession (sessionManager);

			SetDebugLayout ();

			try {
				sessionManager.PrepareForRun ();
				session.Run (startInfo, GetUserOptions ());
			} catch {
				sessionManager.SessionError = true;
				Cleanup (sessionManager);
				throw;
			}
			return sessionManager.debugOperation;
		}

		static bool ExceptionHandler (Exception ex)
		{
			Gtk.Application.Invoke ((o, args) => {
				if (ex is DebuggerException)
					MessageService.ShowError (ex.Message, ex);
				else
					MessageService.ShowError ("Debugger operation failed", ex);
			});
			return true;
		}

		class SessionManager : IDisposable
		{
			OperationConsole console;
			IDisposable cancelRegistration;
			System.Diagnostics.Stopwatch firstAssemblyLoadTimer;

			public readonly DebuggerSession Session;
			public readonly DebugAsyncOperation debugOperation;
			public readonly DebuggerEngine Engine;

			public SessionManager (DebuggerSession session, OperationConsole console, DebuggerEngine engine)
			{
				Engine = engine;
				Session = session;
				session.ExceptionHandler = ExceptionHandler;
				session.AssemblyLoaded += OnAssemblyLoaded;
				this.console = console;
				cancelRegistration = console.CancellationToken.Register (Cancel);
				debugOperation = new DebugAsyncOperation (session);
			}

			void Cancel ()
			{
				Session.Exit ();
				Cleanup (this);
			}

			public void LogWriter (bool iserr, string text)
			{
				console?.Log.Write (text);
			}

			public void DebugWriter (int level, string category, string message)
			{
				console?.Debug (level, category, message);
			}

			public void OutputWriter (bool iserr, string text)
			{
				if (iserr)
					console?.Error.Write (text);
				else
					console?.Out.Write (text);
			}

			public void BreakpointTraceHandler (BreakEvent be, string trace)
			{
				if (be is Breakpoint) {
					if (pinnedWatches.UpdateLiveWatch ((Breakpoint)be, trace))
						return; // No need to log the value. It is shown in the watch.
				}
				DebugWriter (0, "", trace + Environment.NewLine);
			}

			public void Dispose ()
			{
				UpdateDebugSessionCounter ();
				UpdateEvaluationStatsCounter ();

				console?.Dispose ();
				console = null;
				Session.AssemblyLoaded -= OnAssemblyLoaded;
				Session.Dispose ();
				debugOperation.Cleanup ();
				cancelRegistration?.Dispose ();
				cancelRegistration = null;
			}

			/// <summary>
			/// Indicates whether the debug session failed to an exception or any debugger
			/// operation failed and was reported to the user.
			/// </summary>
			public bool SessionError { get; set; }

			void UpdateDebugSessionCounter ()
			{
				var metadata = new Dictionary<string, string> ();
				metadata ["Success"] = (!SessionError).ToString ();
				metadata ["DebuggerType"] = Engine.Id;

				if (firstAssemblyLoadTimer != null) {
					if (firstAssemblyLoadTimer.IsRunning) {
						// No first assembly load event.
						firstAssemblyLoadTimer.Stop ();
					} else {
						metadata ["AssemblyFirstLoadDuration"] = firstAssemblyLoadTimer.ElapsedMilliseconds.ToString ();
					}
				}

				Counters.DebugSession.Inc (metadata);
			}

			void UpdateEvaluationStatsCounter ()
			{
				if (Session.EvaluationStats.TimingsCount == 0 && Session.EvaluationStats.FailureCount == 0) {
					// No timings or failures recorded.
					return;
				}

				var metadata = new Dictionary<string, string> ();
				metadata ["DebuggerType"] = Engine.Id;
				metadata ["AverageDuration"] = Session.EvaluationStats.AverageTime.ToString ();
				metadata ["MaximumDuration"] = Session.EvaluationStats.MaxTime.ToString ();
				metadata ["MinimumDuration"] = Session.EvaluationStats.MinTime.ToString ();
				metadata ["FailureCount"] = Session.EvaluationStats.FailureCount.ToString ();
				metadata ["SuccessCount"] = Session.EvaluationStats.TimingsCount.ToString ();

				Counters.EvaluationStats.Inc (metadata);
			}

			bool ExceptionHandler (Exception ex)
			{
				SessionError = true;
				return DebuggingService.ExceptionHandler (ex);
			}

			/// <summary>
			/// Called just before DebugSession.Run is called.
			/// </summary>
			public void PrepareForRun ()
			{
				firstAssemblyLoadTimer = new System.Diagnostics.Stopwatch ();
				firstAssemblyLoadTimer.Start ();
			}

			void OnAssemblyLoaded (object sender, AssemblyEventArgs e)
			{
				DebuggerSession.AssemblyLoaded -= OnAssemblyLoaded;
				firstAssemblyLoadTimer?.Stop ();
			}
		}

		static async void OnBusyStateChanged (object s, BusyStateEventArgs args)
		{
			isBusy = args.IsBusy;
			await Runtime.RunInMainThread (delegate {
				busyEvaluator.UpdateBusyState (args);
				if (args.IsBusy) {
					if (busyStatusIcon == null) {
						busyStatusIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (ImageService.GetIcon ("md-bug", Gtk.IconSize.Menu));
						busyStatusIcon.SetAlertMode (100);
						busyStatusIcon.Title = GettextCatalog.GetString ("Debugger");
						busyStatusIcon.ToolTip = GettextCatalog.GetString ("The debugger runtime is not responding. You can wait for it to recover, or stop debugging.");
						busyStatusIcon.Help = GettextCatalog.GetString ("Debugger information");
						busyStatusIcon.Clicked += OnBusyStatusIconClicked;
					}
				} else {
					if (busyStatusIcon != null) {
						busyStatusIcon.Clicked -= OnBusyStatusIconClicked;
						busyStatusIcon.Dispose ();
						busyStatusIcon = null;
					}
				}
			});
		}

		static void OnBusyStatusIconClicked (object sender, StatusBarIconClickedEventArgs args)
		{
			MessageService.PlaceDialog (busyEvaluator.Dialog, MessageService.RootWindow);
		}

		static bool CheckIsBusy ()
		{
			if (isBusy && !busyEvaluator.Dialog.Visible)
				MessageService.PlaceDialog (busyEvaluator.Dialog, MessageService.RootWindow);
			return isBusy;
		}

		static void OnStarted (object s, EventArgs a)
		{
			nextStatementLocations.Clear ();
			if (currentSession?.Session == s) {
				currentBacktrace = null;
				currentSession = null;
			}

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
			var session = (DebuggerSession)sender;
			if (args.BreakEvent != null && args.BreakEvent.NonUserBreakpoint)
				return;
			nextStatementLocations.Clear ();

			try {
				switch (args.Type) {
				case TargetEventType.TargetExited:
					Breakpoints.RemoveRunToCursorBreakpoints ();
					SessionManager sessionToCleanup;
					if (sessions.TryGetValue (session, out sessionToCleanup))//It was already cleanedUp by Stop command
						Cleanup (sessionToCleanup);
					break;
				case TargetEventType.TargetSignaled:
				case TargetEventType.TargetStopped:
				case TargetEventType.TargetHitBreakpoint:
				case TargetEventType.TargetInterrupted:
				case TargetEventType.UnhandledException:
				case TargetEventType.ExceptionThrown:
					var action = new Func<bool> (delegate {
						SessionManager sessionManager;
						if (!sessions.TryGetValue (session, out sessionManager))
							return false;
						Breakpoints.RemoveRunToCursorBreakpoints ();
						currentSession = sessionManager;
						ActiveThread = args.Thread;
						NotifyPaused ();
						NotifyException (args);
						return true;
					});
					if (currentSession != null && currentSession != sessions [session]) {
						StopsQueue.Enqueue (action);
						NotifyPaused ();//Notify about pause again, so ThreadsPad can update, to show all processes
					} else {
						action ();
					}
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
				stepSwitchCts?.Cancel ();
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

			foreach (var pair in sessions.ToArray ()) {
				pair.Key.Exit ();
				Cleanup (pair.Value);
			}
		}

		public static void StepInto ()
		{
			Runtime.AssertMainThread ();

			if (!IsDebugging || !IsPaused || CheckIsBusy ())
				return;
			currentSession.Session.StepLine ();
			NotifyLocationChanged ();
			DelayHandleStopQueue ();
		}

		public static void StepOver ()
		{
			Runtime.AssertMainThread ();

			if (!IsDebugging || !IsPaused || CheckIsBusy ())
				return;
			currentSession.Session.NextLine ();
			NotifyLocationChanged ();
			DelayHandleStopQueue ();
		}

		public static void StepOut ()
		{
			Runtime.AssertMainThread ();

			if (!IsDebugging || !IsPaused || CheckIsBusy ())
				return;
			currentSession.Session.Finish ();
			NotifyLocationChanged ();
			DelayHandleStopQueue ();
		}

		static CancellationTokenSource stepSwitchCts;
		static void DelayHandleStopQueue ()
		{
			stepSwitchCts?.Cancel ();
			if (StopsQueue.Count > 0) {
				stepSwitchCts = new CancellationTokenSource ();
				var token = stepSwitchCts.Token;
				Task.Delay (500, token).ContinueWith ((t) => {
					if (token.IsCancellationRequested)
						return;
					Runtime.RunInMainThread (() => {
						if (token.IsCancellationRequested)
							return;
						if (IsPaused)//If session is already paused(stepping finished in time), don't switch
							return;
						HandleStopQueue ();
					});
				});
			}
		}

		public static Backtrace CurrentCallStack {
			get { return currentBacktrace; }
		}

		public static SourceLocation NextStatementLocation {
			get {
				SourceLocation location = null;

				if (IsPaused)
					nextStatementLocations.TryGetValue (ActiveThread.Id, out location);

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
				} else
					currentFrame = -1;
			}
		}

		public static ThreadInfo ActiveThread {
			get {
				return currentSession?.Session.ActiveThread;
			}
			set {
				if (currentSession != null && currentSession.Session.GetProcesses () [0].GetThreads ().Contains (value)) {
					currentSession.Session.ActiveThread = value;
					SetCurrentBacktrace (value.Backtrace);
				} else {
					foreach (var session in sessions) {
						if (session.Key.GetProcesses () [0].GetThreads ().Contains (value)) {
							currentSession = session.Value;
							currentSession.Session.ActiveThread = value;
							SetCurrentBacktrace (value.Backtrace);
							return;
						}
					}
					throw new Exception ("Thread not found in any of active sessions.");
				}
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

		public static async void ShowCurrentExecutionLine ()
		{
			Runtime.AssertMainThread ();
			if (currentBacktrace != null) {
				var sf = GetCurrentVisibleFrame ();
				if (sf != null && !string.IsNullOrEmpty (sf.SourceLocation.FileName) && System.IO.File.Exists (sf.SourceLocation.FileName) && sf.SourceLocation.Line != -1) {
					Document document = await IdeApp.Workbench.OpenDocument (sf.SourceLocation.FileName, null, sf.SourceLocation.Line, 1, OpenDocumentOptions.Debugger);
					OnDisableConditionalCompilation (new DocumentEventArgs (document));
				}
			}
		}

		public static async void ShowNextStatement ()
		{
			Runtime.AssertMainThread ();
			var location = NextStatementLocation;

			if (location != null && System.IO.File.Exists (location.FileName)) {
				Document document = await IdeApp.Workbench.OpenDocument (location.FileName, null, location.Line, 1, OpenDocumentOptions.Debugger);
				OnDisableConditionalCompilation (new DocumentEventArgs (document));
			} else {
				ShowCurrentExecutionLine ();
			}
		}

		public static bool CanDebugCommand (ExecutionCommand command)
		{
			return GetFactoryForCommand (command) != null;
		}

		public static DebuggerEngine [] GetDebuggerEngines ()
		{
			if (engines == null) {
				var list = new List<DebuggerEngine> ();

				foreach (DebuggerEngineExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath))
					list.Add (new DebuggerEngine (node));

				engines = list.ToArray ();
			}

			return engines;
		}

		public static Dictionary<string, ExpressionEvaluatorExtensionNode> GetExpressionEvaluators ()
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
			var baseDir = (args.Item as Solution)?.BaseDirectory;
			lock (breakpoints)
				args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService.Breakpoints", breakpoints.Save (baseDir));
			args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService.PinnedWatches", pinnedWatches);
		}

		static Task OnLoadUserPrefs (object s, UserPreferencesEventArgs args)
		{
			var elem = args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService.Breakpoints") ?? args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService");

			if (elem != null) {
				var baseDir = (args.Item as Solution)?.BaseDirectory;
				lock (breakpoints)
					breakpoints.Load (elem, baseDir);
			}

			PinnedWatchStore wstore = args.Properties.GetValue<PinnedWatchStore> ("MonoDevelop.Ide.DebuggingService.PinnedWatches");
			if (wstore != null)
				pinnedWatches.LoadFrom (wstore);

			lock (breakpoints)
				pinnedWatches.BindAll (breakpoints);

			lock (breakpoints)
				pinnedWatches.SetAllLiveUpdateBreakpoints (breakpoints);

			return Task.FromResult (true);
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
				ITextEditorResolver textEditorResolver = doc.GetContent<ITextEditorResolver> ();
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

	class FeatureCheckerHandlerFactory : IExecutionHandler
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

	class InternalDebugExecutionHandler : IExecutionHandler
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
			Gtk.Application.Invoke ((o, args) => {
				IdeApp.Workbench.StatusBar.ShowMessage (Ide.Gui.Stock.StatusConnecting, message);
			});
		}

		public void Dispose ()
		{
			Gtk.Application.Invoke ((o, args) => {
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
			Gtk.Application.Invoke ((o, args) => {
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
