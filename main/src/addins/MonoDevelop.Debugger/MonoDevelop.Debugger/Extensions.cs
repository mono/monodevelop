// Extensions.cs
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

using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Debugger
{
	public static class Extensions
	{
		public static bool CanDebug (this ProjectOperations opers, IBuildTarget entry)
		{
			ExecutionContext context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);
			return opers.CanExecute (entry, context);
		}

		public static AsyncOperation Debug (this ProjectOperations opers, IBuildTarget entry)
		{
			if (opers.CurrentRunOperation != null && !opers.CurrentRunOperation.IsCompleted)
				return opers.CurrentRunOperation;

			ExecutionContext context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);

			AsyncOperation op = opers.Execute (entry, context);
			SwitchToDebugLayout (op);
			return op;
		}

		public static bool CanDebugFile (this ProjectOperations opers, string file)
		{
			var context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);
			return opers.CanExecuteFile (file, context);
		}

		public static AsyncOperation DebugFile (this ProjectOperations opers, string file)
		{
			var context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);
			return opers.ExecuteFile (file, context);
		}

		public static AsyncOperation DebugApplication (this ProjectOperations opers, string executableFile)
		{
			if (opers.CurrentRunOperation != null && !opers.CurrentRunOperation.IsCompleted)
				return opers.CurrentRunOperation;

			string oldLayout = IdeApp.Workbench.CurrentLayout;
			IdeApp.Workbench.CurrentLayout = "Debug";

			var monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();

			var oper = DebuggingService.Run (executableFile, (IConsole) monitor);
			opers.CurrentRunOperation = oper;

			oper.Task.ContinueWith (t => {
				monitor.Dispose ();
				IdeApp.Workbench.CurrentLayout = oldLayout;
			});

			return oper;
		}

		public static AsyncOperation AttachToProcess (this ProjectOperations opers, DebuggerEngine debugger, ProcessInfo proc)
		{
			if (opers.CurrentRunOperation != null && !opers.CurrentRunOperation.IsCompleted)
				return opers.CurrentRunOperation;

			var oper = DebuggingService.AttachToProcess (debugger, proc);
			SwitchToDebugLayout (oper);

			opers.CurrentRunOperation = oper;
			return opers.CurrentRunOperation;
		}

		static void SwitchToDebugLayout (AsyncOperation oper)
		{
			string oldLayout = IdeApp.Workbench.CurrentLayout;
			IdeApp.Workbench.CurrentLayout = "Debug";

			oper.Task.ContinueWith (t => {
				IdeApp.Workbench.CurrentLayout = oldLayout;
			});
		}
	}
}
