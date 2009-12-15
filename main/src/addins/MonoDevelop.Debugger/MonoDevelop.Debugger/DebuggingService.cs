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
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui.Dialogs;

using MonoDevelop.Ide.Gui;

using Mono.Debugging.Client;
using Mono.Debugging.Backend;

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */

namespace MonoDevelop.Debugger
{

	public static class DebuggingService
	{
		const string FactoriesPath = "/MonoDevelop/Debugging/DebuggerFactories";
		static IDebuggerEngine[] engines;
		
		static BreakpointStore breakpoints = new BreakpointStore ();
		
		static IConsole console;
		static DebugExecutionHandlerFactory executionHandlerFactory;
		
		static IDebuggerEngine currentEngine;
		static DebuggerSession session;
		static Backtrace currentBacktrace;
		static int currentFrame;

		static BusyEvaluatorDialog busyDialog;

			
		static public event EventHandler PausedEvent;
		static public event EventHandler ResumedEvent;
		static public event EventHandler StoppedEvent;
		
		static public event EventHandler CallStackChanged;
		static public event EventHandler CurrentFrameChanged;
		static public event EventHandler ExecutionLocationChanged;
		static public event EventHandler DisassemblyRequested;
		static public event EventHandler<DocumentEventArgs> DisableConditionalCompilation;
			
		static DebuggingService()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory ();
			TextFileService.LineCountChanged += OnLineCountChanged;
			if (IdeApp.IsInitialized) {
				IdeApp.Workspace.StoringUserPreferences += OnStoreUserPrefs;
				IdeApp.Workspace.LoadingUserPreferences += OnLoadUserPrefs;
				busyDialog = new BusyEvaluatorDialog ();
			}
			AddinManager.AddExtensionNodeHandler (FactoriesPath, delegate {
				// Regresh the engine list
				engines = null;
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
		
		public static string[] EnginePriority {
			get {
				string s = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.EnginePriority", "");
				if (s.Length == 0) {
					// Set the initial priorities
					List<string> prios = new List<string> ();
					int i = 0;
					foreach (IDebuggerEngine de in AddinManager.GetExtensionObjects (FactoriesPath, typeof(IDebuggerEngine), true)) {
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
		
		public static bool ShowBreakpointProperties (Breakpoint bp, bool editNew)
		{
			BreakpointPropertiesDialog dlg = new BreakpointPropertiesDialog (bp, editNew);
			try {
				dlg.TransientFor = IdeApp.Workbench.RootWindow;
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					return true;
				}
			} finally {
				dlg.Destroy ();
			}
			return false;
		}
		
		public static void ShowAddTracepointDialog (string file, int line)
		{
			AddTracePointDialog dlg = new AddTracePointDialog ();
			if (dlg.Run () == (int) Gtk.ResponseType.Ok && dlg.Text.Length > 0) {
				Breakpoint bp = new Breakpoint (file, line);
				bp.HitAction = HitAction.PrintExpression;
				bp.TraceExpression = dlg.Text;
				bp.ConditionExpression = dlg.Condition;
				Breakpoints.Add (bp);
			}
			dlg.Destroy ();
		}

		public static bool IsFeatureSupported (IBuildTarget target, DebuggerFeatures feature)
		{
			return (GetSupportedFeatures (target) & feature) == feature;
		}

		public static bool IsDebuggingSupported {
			get {
				return GetDebuggerEngines ().Length > 0;
			}
		}

		public static bool CurrentSessionSupportsFeature (DebuggerFeatures feature)
		{
			return (currentEngine.SupportedFeatures & feature) == feature;
		}

		public static bool IsFeatureSupported (DebuggerFeatures feature)
		{
			foreach (IDebuggerEngine engine in GetDebuggerEngines ())
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
			IDebuggerEngine engine = GetFactoryForCommand (command);
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
			dlg.Run ();
			dlg.Destroy ();
		}

		public static void ShowExceptionsFilters ()
		{
			ExceptionsDialog dlg = new ExceptionsDialog ();
			dlg.Run ();
			dlg.Destroy ();
		}
		
		static void Cleanup ()
		{
			currentBacktrace = null;
			
			if (!IsDebugging)
				return;

			// Dispose the session at the end, since it may take a while.
			DebuggerSession oldSession = session;
			session = null;

			if (StoppedEvent != null)
				StoppedEvent (null, new EventArgs ());
			
			if (console != null) {
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

		public static bool IsRunning {
			get {
				return IsDebugging && session.IsRunning;
			}
		}

		static void KillApplication (object obj)
		{
			Cleanup ();
		}

		public static void Pause ()
		{
			session.Stop ();
		}

		public static void Resume ()
		{
			session.Continue ();
			NotifyLocationChanged ();
		}

		public static IProcessAsyncOperation Run (string file, IConsole console)
		{
			DebugExecutionHandler h = new DebugExecutionHandler (null);
			ExecutionCommand cmd = Runtime.ProcessService.CreateCommand (file);
			return h.Execute (cmd, console);
		}
		
		public static IAsyncOperation AttachToProcess (IDebuggerEngine debugger, ProcessInfo proc)
		{
			currentEngine = debugger;
			session = debugger.CreateSession ();
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
			EvaluationOptions eops = EvaluationOptions.DefaultOptions;
			eops.AllowTargetInvoke = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AllowTargetInvoke", true);
			eops.AllowToStringCalls = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AllowToStringCalls", true);
			eops.EvaluationTimeout = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.EvaluationTimeout", 2500);
			eops.FlattenHierarchy = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.FlattenHierarchy", false);
			eops.GroupPrivateMembers = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.GroupPrivateMembers", true);
			eops.GroupStaticMembers = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.GroupStaticMembers", true);
			eops.MemberEvaluationTimeout = eops.EvaluationTimeout * 2;
			return new DebuggerSessionOptions () {
				ProjectAssembliesOnly = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.ProjectAssembliesOnly", true),
				EvaluationOptions = eops,
			};
		}
		
		public static void SetUserOptions (DebuggerSessionOptions options)
		{
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.ProjectAssembliesOnly", options.ProjectAssembliesOnly);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.AllowTargetInvoke", options.EvaluationOptions.AllowTargetInvoke);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.AllowToStringCalls", options.EvaluationOptions.AllowToStringCalls);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.EvaluationTimeout", options.EvaluationOptions.EvaluationTimeout);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.FlattenHierarchy", options.EvaluationOptions.FlattenHierarchy);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.GroupPrivateMembers", options.EvaluationOptions.GroupPrivateMembers);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.GroupStaticMembers", options.EvaluationOptions.GroupStaticMembers);
		}
		
		public static void ShowDisassembly ()
		{
			if (DisassemblyRequested != null)
				DisassemblyRequested (null, EventArgs.Empty);
		}
		
		internal static void InternalRun (ExecutionCommand cmd, IDebuggerEngine factory, IConsole c)
		{
			if (factory == null) {
				factory = GetFactoryForCommand (cmd);
				if (factory == null)
					throw new InvalidOperationException ("Unsupported command: " + cmd);
			}
			
			DebuggerStartInfo startInfo = factory.CreateDebuggerStartInfo (cmd);
			startInfo.UseExternalConsole = c is ExternalConsole;
			startInfo.CloseExternalConsoleOnExit = c.CloseOnDispose;
			currentEngine = factory;
			session = factory.CreateSession ();
			session.Initialize ();
			console = c;
			
			SetupSession ();

			try {
				session.Run (startInfo, GetUserOptions ());
			} catch {
				Cleanup ();
				throw;
			}
		}
		
		static void SetupSession ()
		{
			session.Breakpoints = breakpoints;
			session.TargetEvent += OnTargetEvent;
			session.TargetStarted += OnStarted;
			session.OutputWriter = delegate (bool iserr, string text) {
				if (iserr)
					console.Error.Write (text);
				else
					console.Out.Write (text);
			};
			session.LogWriter = delegate (bool iserr, string text) {
				// Events may come with a bit of delay, so the debug session
				// may already have been cleaned up
				if (console != null)
					console.Log.Write (text);
			};
			session.ExceptionHandler = delegate (Exception ex) {
				Gtk.Application.Invoke (delegate {
					if (ex is DebuggerException)
						MessageService.ShowError (ex.Message);
					else
						MessageService.ShowException (ex);
				});
				return true;
			};
			session.BusyStateChanged += OnBusyStateChanged;

			console.CancelRequested += new EventHandler (OnCancelRequested);
			NotifyLocationChanged ();
		}
		
		static void OnBusyStateChanged (object s, BusyStateEventArgs args)
		{
			DispatchService.GuiDispatch (delegate {
				busyDialog.UpdateBusyState (args);
			});
		}
		
		static void OnStarted (object s, EventArgs a)
		{
			currentBacktrace = null;
			DispatchService.GuiDispatch (delegate {
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
				Console.WriteLine ("OnTargetEvent, type - {0}", args.Type);
				if (args.Type != TargetEventType.TargetExited) {
					SetCurrentBacktrace (args.Backtrace);
				}

				switch (args.Type) {
					case TargetEventType.TargetExited:
						KillApplication (null);
						break;
					case TargetEventType.TargetSignaled:
					case TargetEventType.TargetStopped:
					case TargetEventType.TargetRunning:
					case TargetEventType.TargetHitBreakpoint:
					case TargetEventType.TargetInterrupted:
					case TargetEventType.UnhandledException:
					case TargetEventType.ExceptionThrown:
						NotifyPaused ();
						break;
					default:
						break;
				}
			} catch (Exception e) {
				Console.WriteLine ("OnTargetEvent, {0}", e.ToString ());
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
				IdeApp.Workbench.Present ();
			});
		}
		
		static void NotifyLocationChanged ()
		{
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (null, EventArgs.Empty);
		}
		
		static void NotifyCurrentFrameChanged ()
		{
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
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				throw new Exception ("Can't step unless paused.");

			session.StepLine ();
			NotifyLocationChanged ();
		}

		public static void StepOver ()
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

		public static void StepOut ()
		{
			if (!IsDebugging)
				return;

			if (IsRunning)
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
				if (!string.IsNullOrEmpty (sf.SourceLocation.Filename) && System.IO.File.Exists (sf.SourceLocation.Filename) && sf.SourceLocation.Line != -1) {
					Document document = IdeApp.Workbench.OpenDocument (sf.SourceLocation.Filename, sf.SourceLocation.Line, 1, true, false);
					OnDisableConditionalCompilation (new DocumentEventArgs (document));
				}
			}
		}
		
		public static bool CanDebugCommand (ExecutionCommand command)
		{
			return GetFactoryForCommand (command) != null;
		}
		
		public static IDebuggerEngine[] GetDebuggerEngines ()
		{
			if (engines == null) {
				IDebuggerEngine[] engs = (IDebuggerEngine[]) AddinManager.GetExtensionObjects (FactoriesPath, typeof(IDebuggerEngine), true);
				string[] priorities = EnginePriority;
				Array.Sort (engs, delegate (IDebuggerEngine d1, IDebuggerEngine d2) {
					int i1 = Array.IndexOf (priorities, d1.Id);
					int i2 = Array.IndexOf (priorities, d2.Id);
					
					//ensure that soft debugger is prioritised over newly installed debuggers
					if (i1 < 0 )
						i1 = d1.Id.StartsWith ("Mono.Debugger.Soft")? 0 : engs.Length;
					if (i2 < 0)
						i2 = d2.Id.StartsWith ("Mono.Debugger.Soft")? 0 : engs.Length;
					
					if (i1 == i2)
						return d1.Name.CompareTo (d2.Name);
					else
						return i1.CompareTo (i2);
				});
				engines = engs;
			}
			return engines;
		}		
		
		static IDebuggerEngine GetFactoryForCommand (ExecutionCommand cmd)
		{
			foreach (IDebuggerEngine factory in GetDebuggerEngines ()) {
				if (factory.CanDebugCommand (cmd))
					return factory;
			}
			return null;
		}
		
		static void OnLineCountChanged (object ob, LineCountEventArgs a)
		{
			List<Breakpoint> bps = new List<Breakpoint> (breakpoints.GetBreakpoints ());
			foreach (Breakpoint bp in bps) {
				if (bp.FileName == a.TextFile.Name) {
					if (bp.Line > a.LineNumber) {
						// If the line that has the breakpoint is deleted, delete the breakpoint
						breakpoints.Remove (bp);
						if (bp.Line + a.LineCount >= a.LineNumber)
							breakpoints.Add (bp.FileName, bp.Line + a.LineCount);
					}
					else if (bp.Line == a.LineNumber && a.LineCount < 0)
						breakpoints.Remove (bp);
				}
			}
		}
		
		static void OnStoreUserPrefs (object s, UserPreferencesEventArgs args)
		{
			args.Properties.SetValue ("MonoDevelop.Ide.DebuggingService", breakpoints.Save ());
		}
		
		static void OnLoadUserPrefs (object s, UserPreferencesEventArgs args)
		{
			XmlElement elem = args.Properties.GetValue<XmlElement> ("MonoDevelop.Ide.DebuggingService");
			if (elem != null)
				breakpoints.Load (elem);
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
		IDebuggerEngine engine;
		
		public InternalDebugExecutionHandler (IDebuggerEngine engine)
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
}
