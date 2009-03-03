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
		
		static BreakpointStore breakpoints = new BreakpointStore ();
		
		static IConsole console;
		static DebugExecutionHandlerFactory executionHandlerFactory;
		
		static DebuggerSession session;
		static Backtrace currentBacktrace;
		static int currentFrame;

		static public event EventHandler PausedEvent;
		static public event EventHandler ResumedEvent;
		static public event EventHandler StoppedEvent;
		
		static public event EventHandler CallStackChanged;
		static public event EventHandler CurrentFrameChanged;
		static public event EventHandler ExecutionLocationChanged;
		static public event EventHandler DisassemblyRequested;

		static DebuggingService()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory ();
			TextFileService.LineCountChanged += OnLineCountChanged;
			IdeApp.Workspace.StoringUserPreferences += OnStoreUserPrefs;
			IdeApp.Workspace.LoadingUserPreferences += OnLoadUserPrefs;
		}

		public static IExecutionHandlerFactory GetExecutionHandlerFactory ()
		{
			return executionHandlerFactory;
		}
		
		public static DebuggerSession DebuggerSession {
			get { return session; }
		}
		
		public static BreakpointStore Breakpoints {
			get { return breakpoints; }
		}
		
		public static bool ShowBreakpointProperties (Breakpoint bp, bool editNew)
		{
			BreakpointPropertiesDialog dlg = new BreakpointPropertiesDialog (bp, editNew);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					return true;
				}
			} finally {
				dlg.Destroy ();
			}
			return false;
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

		public static DebuggerFeatures GetSupportedFeaturesForPlatform (string plaformId)
		{
			IDebuggerEngine engine = GetFactoryForPlatform (plaformId);
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

			if (StoppedEvent != null)
				StoppedEvent (null, new EventArgs ());

			// Dispose the session at the end, since it may take a while.
			DebuggerSession oldSession = session;
			session = null;
			
			if (console != null) {
				console.Dispose ();
				console = null;
			}
			
			DispatchService.GuiDispatch (delegate {
				NotifyCallStackChanged ();
				NotifyCurrentFrameChanged ();
				NotifyLocationChanged ();
			});
			
			if (oldSession != null)
				oldSession.Dispose ();
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
			return h.Execute (file, null, null, null, console);
		}
		
		public static IAsyncOperation AttachToProcess (IDebuggerEngine debugger, ProcessInfo proc)
		{
			session = debugger.CreateSession ();
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			console = monitor as IConsole;
			SetupSession ();
			session.TargetExited += delegate {
				monitor.Dispose ();
			};
			session.AttachToProcess (proc);
			return monitor.AsyncOperation;
		}
		
		public static void ShowDisassembly ()
		{
			if (DisassemblyRequested != null)
				DisassemblyRequested (null, EventArgs.Empty);
		}
		
		internal static void InternalRun (string platform, DebuggerStartInfo startInfo, IConsole c)
		{
			console = c;
			
			if (platform != null)
				session = CreateDebugSessionForPlatform (platform);
			else
				session = CreateDebugSessionForFile (startInfo.Command);
			
			SetupSession ();

			try {
				session.Run (startInfo);
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
				console.Log.Write (text);
			};

			console.CancelRequested += new EventHandler (OnCancelRequested);
			NotifyLocationChanged ();
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

		static void NotifyPaused ()
		{
			DispatchService.GuiDispatch (delegate {
				if (PausedEvent != null)
					PausedEvent (null, EventArgs.Empty);
				NotifyLocationChanged ();
				IdeApp.Workbench.RootWindow.Present ();
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

		public static string CurrentFilename {
			get {
				StackFrame sf = CurrentFrame;
				if (sf != null)
					return sf.SourceLocation.Filename;
				else
					return null;
			}
		}

		public static int CurrentLineNumber {
			get {
				StackFrame sf = CurrentFrame;
				if (sf != null)
					return sf.SourceLocation.Line;
				else
					return -1;
			}
		}

		public static StackFrame CurrentFrame {
			get {
				if (currentBacktrace != null && currentFrame != -1)
					return currentBacktrace.GetFrame (currentFrame);
				else
					return null;
			}
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
		
		public static bool CanDebugPlatform (string platform)
		{
			return GetFactoryForPlatform (platform) != null;
		}
		
		public static bool CanDebugFile (string file)
		{
			return GetFactoryForFile (file) != null;
		}
		
		public static DebuggerSession CreateDebugSessionForPlatform (string platform)
		{
			IDebuggerEngine factory = GetFactoryForPlatform (platform);
			if (factory != null) {
				DebuggerSession ds = factory.CreateSession ();
				ds.Initialize ();
				return ds;
			} else
				throw new InvalidOperationException ("Unsupported platform: " + platform);
		}
		
		public static DebuggerSession CreateDebugSessionForFile (string file)
		{
			IDebuggerEngine factory = GetFactoryForFile (file);
			if (factory != null) {
				DebuggerSession ds = factory.CreateSession ();
				ds.Initialize ();
				return ds;
			} else
				throw new InvalidOperationException ("Unsupported file: " + file);
		}
		
		public static IDebuggerEngine[] GetDebuggerEngines ()
		{
			return (IDebuggerEngine[]) AddinManager.GetExtensionObjects (FactoriesPath, typeof(IDebuggerEngine), true);
		}		
		
		static IDebuggerEngine GetFactoryForPlatform (string platform)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath)) {
				IDebuggerEngine factory = (IDebuggerEngine) node.GetInstance ();
				if (factory.CanDebugPlatform (platform))
					return factory;
			}
			return null;
		}
		
		static IDebuggerEngine GetFactoryForFile (string file)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath)) {
				IDebuggerEngine factory = (IDebuggerEngine) node.GetInstance ();
				if (factory.CanDebugFile (file))
					return factory;
			}
			return null;
		}
		
		static void OnLineCountChanged (object ob, LineCountEventArgs a)
		{
			List<Breakpoint> bps = new List<Breakpoint> (breakpoints.GetBreakpoints ());
			foreach (Breakpoint bp in bps) {
				if (bp.FileName == a.TextFile.Name && bp.Line >= a.LineNumber) {
					breakpoints.Remove (bp);
					breakpoints.Add (bp.FileName, bp.Line + a.LineCount);
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
	
	internal class FeatureCheckerHandlerFactory: IExecutionHandlerFactory
	{
		public DebuggerFeatures SupportedFeatures { get; set; }
		
		public bool SupportsPlatform (string platformId)
		{
			SupportedFeatures = DebuggingService.GetSupportedFeaturesForPlatform (platformId);
			return SupportedFeatures != DebuggerFeatures.None;
		}

		public IExecutionHandler CreateExecutionHandler (string platformId)
		{
			return null;
		}
	}
}
