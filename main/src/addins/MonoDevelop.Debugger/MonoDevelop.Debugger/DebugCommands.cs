// DebugCommands.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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


using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public enum DebugCommands
	{
		Debug,
		DebugEntry,
		DebugApplication,
		ToggleBreakpoint,
		StepOver,
		StepInto,
		StepOut,
		Pause,
		ClearAllBreakpoints,
		AttachToProcess,
		Detach,
		EnableDisableBreakpoint,
		DisableAllBreakpoints,
		ShowDisassembly,
		NewBreakpoint,
		RemoveBreakpoint,
		ShowBreakpointProperties,
		ExpressionEvaluator,
		SelectExceptions,
		ShowCurrentExecutionLine,
		AddTracepoint
	}

	internal class DebugHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (DebuggingService.IsDebugging && !DebuggingService.IsRunning) {
				DebuggingService.Resume ();
				return;
			}
		
			if (!DebuggingService.IsDebuggingSupported && !IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
				MonoDevelop.Ide.Commands.StopHandler.StopBuildOperations ();
				IdeApp.ProjectOperations.CurrentRunOperation.WaitForCompleted ();
			}
			
			if (!IdeApp.Preferences.BuildBeforeExecuting) {
				if (IdeApp.Workspace.IsOpen) {
					CheckResult cr = CheckBeforeDebugging (IdeApp.Workspace);
					if (cr == DebugHandler.CheckResult.Cancel)
						return;
					if (cr == DebugHandler.CheckResult.Run) {
						ExecuteWorkspace ();
						return;
					}
					// Else continue building
				}
				else {
					ExecuteDocument (IdeApp.Workbench.ActiveDocument);
					return;
				}
			}
			
			if (IdeApp.Workspace.IsOpen) {
				IAsyncOperation op = IdeApp.ProjectOperations.Build (IdeApp.Workspace);
				op.Completed += delegate {
					if (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings)
						return;
					if (op.Success)
						ExecuteWorkspace ();
				};
			} else {
				Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					doc.Save ();
					IAsyncOperation op = doc.Build ();
					op.Completed += delegate {
						if (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings)
							return;
						if (op.Success)
							ExecuteDocument (doc);
					};
				}
			}
		}

		void ExecuteWorkspace ()
		{
			if (IdeApp.ProjectOperations.CanDebug (IdeApp.Workspace))
				IdeApp.ProjectOperations.Debug (IdeApp.Workspace);
			else
				IdeApp.ProjectOperations.Execute (IdeApp.Workspace);
		}

		void ExecuteDocument (Document doc)
		{
			if (doc.CanDebug ())
				doc.Debug ();
			else
				doc.Run ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (DebuggingService.IsDebugging && !DebuggingService.IsRunning) {
				info.Enabled = true;
				info.Text = GettextCatalog.GetString ("_Continue");
				info.Description = GettextCatalog.GetString ("Continue the execution of the application");
				return;
			}

			// If there are no debugger installed, this command will not debug, it will
			// just run, so the label has to be changed accordingly.
			if (!DebuggingService.IsDebuggingSupported) {
				info.Text = IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted ? GettextCatalog.GetString ("_Run") : GettextCatalog.GetString ("_Run again");
				info.Icon = "gtk-execute";
			}

			if (IdeApp.Workspace.IsOpen) {
				bool canExecute = IdeApp.ProjectOperations.CanDebug (IdeApp.Workspace) ||
					 (!DebuggingService.IsDebuggingSupported && IdeApp.ProjectOperations.CanExecute (IdeApp.Workspace));

				info.Enabled = (IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted || !DebuggingService.IsDebuggingSupported) &&
					canExecute &&
					!(IdeApp.ProjectOperations.CurrentSelectedItem is Workspace);
			} else {
				Document doc = IdeApp.Workbench.ActiveDocument;
				info.Enabled = (doc != null && doc.IsBuildTarget) && (doc.CanRun () || doc.CanDebug ());
			}
		}
		
		internal static CheckResult CheckBeforeDebugging (IBuildTarget target)
		{
			if (IdeApp.Preferences.BuildBeforeExecuting)
				return CheckResult.BuildBeforeRun;
			
			if (!IdeApp.Workspace.NeedsBuilding ())
				return CheckResult.Run;
			
			AlertButton bBuild = new AlertButton (GettextCatalog.GetString ("Build"));
			AlertButton bRun = new AlertButton (Gtk.Stock.Execute, true);
			AlertButton res = MessageService.AskQuestion (
			                                 GettextCatalog.GetString ("Outdated Debug Information"), 
			                                 GettextCatalog.GetString ("The project you are executing has changes done after the last time it was compiled. The debug information may be outdated. Do you want to continue?"),
			                                 2,
			                                 AlertButton.Cancel,
			                                 bBuild,
			                                 bRun);
			if (res == AlertButton.Cancel)
				return CheckResult.Cancel;
			else if (res == bRun)
				return CheckResult.Run;
			else
				return CheckResult.BuildBeforeRun;
		}
			
		internal enum CheckResult
		{
			Cancel,
			BuildBeforeRun,
			Run
		}
			
	}
	
	internal class DebugEntryHandler: CommandHandler
	{
		protected override void Run ()
		{
			IBuildTarget entry = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			DebugHandler.CheckResult cr = DebugHandler.CheckBeforeDebugging (entry);
			
			if (cr == DebugHandler.CheckResult.BuildBeforeRun) {
				IAsyncOperation op = IdeApp.ProjectOperations.Build (entry);
				op.Completed += delegate {
					if (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings)
						return;
					if (op.Success)
						IdeApp.ProjectOperations.Debug (entry);
				};
			} else if (cr == DebugHandler.CheckResult.Run)
				IdeApp.ProjectOperations.Debug (entry);
		}
		
		protected override void Update (CommandInfo info)
		{
			IBuildTarget target = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			info.Enabled = target != null &&
					!(target is Workspace) && IdeApp.ProjectOperations.CanDebug (target) &&
					IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
		}
	}
	
	internal class DebugApplicationHandler: CommandHandler
	{
		protected override void Run ()
		{
			FileSelector fs = new FileSelector (GettextCatalog.GetString ("Application to Debug"));
			try {
				fs.TransientFor = IdeApp.Workbench.RootWindow;
				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok)
					IdeApp.ProjectOperations.DebugApplication (name);
			}
			finally {
				fs.Destroy ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.DebugFile);
		}
	}
	
	internal class AttachToProcessHandler: CommandHandler
	{
		protected override void Run ()
		{
			AttachToProcessDialog dlg = new AttachToProcessDialog ();
			try {
				dlg.TransientFor = IdeApp.Workbench.RootWindow;
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					IdeApp.ProjectOperations.AttachToProcess (dlg.SelectedDebugger, dlg.SelectedProcess);
				}
			}
			finally {
				dlg.Destroy ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Attaching);
		}
	}
	
	internal class DetachFromProcessHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (MessageService.Confirm (GettextCatalog.GetString ("Do you want to detach from the process being debugged?"), new AlertButton (GettextCatalog.GetString ("Detach")), true)) {
				DebuggingService.DebuggerSession.Detach ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DebuggingService.IsDebugging && DebuggingService.DebuggerSession.AttachedToProcess;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Attaching);
		}
	}
	
	internal class StepOverHandler : CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.StepOver();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DebuggingService.IsDebugging && !DebuggingService.IsRunning;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Stepping);
		}
	}

	internal class StepIntoHandler : CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.StepInto();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DebuggingService.IsDebugging && !DebuggingService.IsRunning;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Stepping);
		}
	}
	
	internal class StepOutHandler : CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.StepOut ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DebuggingService.IsDebugging && !DebuggingService.IsRunning;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Stepping);
		}
	}
	
	internal class PauseDebugHandler : CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.Pause ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DebuggingService.IsRunning;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Pause);
		}
	}
	
	internal class ClearAllBreakpointsHandler: CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.Breakpoints.Clear ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !DebuggingService.Breakpoints.IsReadOnly;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
		}
	}
	
	internal class ToggleBreakpointHandler: CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.Breakpoints.Toggle (
			    IdeApp.Workbench.ActiveDocument.FileName,
			    IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && 
					IdeApp.Workbench.ActiveDocument.TextEditor != null &&
					IdeApp.Workbench.ActiveDocument.FileName != FilePath.Null &&
					!DebuggingService.Breakpoints.IsReadOnly;
		}
	}
	
	internal class AddTracepointHandler: CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.ShowAddTracepointDialog (
			    IdeApp.Workbench.ActiveDocument.FileName,
			    IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Tracepoints);
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && 
					IdeApp.Workbench.ActiveDocument.TextEditor != null &&
					IdeApp.Workbench.ActiveDocument.FileName != FilePath.Null &&
					!DebuggingService.Breakpoints.IsReadOnly;
		}
	}
	
	internal class EnableDisableBreakpointHandler: CommandHandler
	{
		protected override void Run ()
		{
			IEnumerable brs = DebuggingService.Breakpoints.GetBreakpointsAtFileLine (
			    IdeApp.Workbench.ActiveDocument.FileName,
			    IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine);
			
			foreach (Breakpoint bp in brs)
				bp.Enabled = !bp.Enabled;
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
			if (IdeApp.Workbench.ActiveDocument != null && 
					IdeApp.Workbench.ActiveDocument.TextEditor != null &&
					IdeApp.Workbench.ActiveDocument.FileName != FilePath.Null &&
			        !DebuggingService.Breakpoints.IsReadOnly) {
				info.Enabled = DebuggingService.Breakpoints.GetBreakpointsAtFileLine (
			    	IdeApp.	Workbench.ActiveDocument.FileName,
			    	IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine).Count > 0;
			}
			else
				info.Enabled = false;
		}
	}
	
	internal class DisableAllBreakpointsHandler: CommandHandler
	{
		protected override void Run ()
		{
			foreach (BreakEvent bp in DebuggingService.Breakpoints)
				bp.Enabled = false;
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !DebuggingService.Breakpoints.IsReadOnly;
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
		}
	}
	
	internal class ShowDisassemblyHandler: CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.ShowDisassembly ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Disassembly);
		}
	}
	
	internal class RemoveBreakpointHandler: CommandHandler
	{
		protected override void Run ()
		{
			IEnumerable<Breakpoint> brs = DebuggingService.Breakpoints.GetBreakpointsAtFileLine (
			    IdeApp.Workbench.ActiveDocument.FileName,
			    IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine);
			
			List<Breakpoint> list = new List<Breakpoint> (brs);
			foreach (Breakpoint bp in list)
				DebuggingService.Breakpoints.Remove (bp);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
			if (IdeApp.Workbench.ActiveDocument != null && 
					IdeApp.Workbench.ActiveDocument.TextEditor != null &&
					IdeApp.Workbench.ActiveDocument.FileName != FilePath.Null &&
			        !DebuggingService.Breakpoints.IsReadOnly) {
				info.Enabled = DebuggingService.Breakpoints.GetBreakpointsAtFileLine (
			    	IdeApp.	Workbench.ActiveDocument.FileName,
			    	IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine).Count > 0;
			}
			else
				info.Enabled = false;
		}
	}
	
	internal class NewBreakpointHandler: CommandHandler
	{
		protected override void Run ()
		{
			Breakpoint bp = new Breakpoint (IdeApp.Workbench.ActiveDocument.FileName, IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine);
			if (DebuggingService.ShowBreakpointProperties (bp, true))
				DebuggingService.Breakpoints.Add (bp);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
			if (IdeApp.Workbench.ActiveDocument != null && 
					IdeApp.Workbench.ActiveDocument.TextEditor != null &&
					IdeApp.Workbench.ActiveDocument.FileName != FilePath.Null &&
			        !DebuggingService.Breakpoints.IsReadOnly) {
				info.Enabled = true;
			}
			else
				info.Enabled = false;
		}
	}
	
	internal class ShowBreakpointPropertiesHandler: CommandHandler
	{
		protected override void Run ()
		{
			IList<Breakpoint> brs = DebuggingService.Breakpoints.GetBreakpointsAtFileLine (
			    IdeApp.Workbench.ActiveDocument.FileName,
			    IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine);

			if (brs.Count > 0)
				DebuggingService.ShowBreakpointProperties (brs [0], false);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Breakpoints);
			if (IdeApp.Workbench.ActiveDocument != null && 
					IdeApp.Workbench.ActiveDocument.TextEditor != null &&
					IdeApp.Workbench.ActiveDocument.FileName != FilePath.Null &&
			        !DebuggingService.Breakpoints.IsReadOnly) {
				info.Enabled = DebuggingService.Breakpoints.GetBreakpointsAtFileLine (
			    	IdeApp.	Workbench.ActiveDocument.FileName,
			    	IdeApp.Workbench.ActiveDocument.TextEditor.CursorLine).Count > 0;
			}
			else
				info.Enabled = false;
		}
	}

	internal class ExpressionEvaluatorCommand: CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.ShowExpressionEvaluator (null);
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsDebuggingSupported;
			info.Enabled = DebuggingService.CurrentFrame != null;
		}
	}

	internal class SelectExceptionsCommand: CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.ShowExceptionsFilters ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = DebuggingService.IsFeatureSupported (DebuggerFeatures.Catchpoints);
		}
	}
	
	internal class ShowCurrentExecutionLineCommand : CommandHandler
	{
		protected override void Run ()
		{
			DebuggingService.ShowCurrentExecutionLine ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DebuggingService.IsDebugging && !DebuggingService.IsRunning;
			info.Visible = DebuggingService.IsDebuggingSupported;
		}
	}
}
