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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.Debugger
{
	public static class Extensions
	{
		public static bool CanDebug (this ProjectOperations opers, IBuildTarget entry)
		{
			ExecutionContext context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors.ConsoleFactory, IdeApp.Workspace.ActiveExecutionTarget);
			return opers.CanExecute (entry, context);
		}

		public static AsyncOperation Debug (this ProjectOperations opers, IBuildTarget entry, bool buildBeforeExecuting = true)
		{
			ExecutionContext context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors.ConsoleFactory, IdeApp.Workspace.ActiveExecutionTarget);

			return opers.Execute (entry, context, buildBeforeExecuting);
		}

		public static bool CanDebugFile (this ProjectOperations opers, string file)
		{
			var context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors.ConsoleFactory, IdeApp.Workspace.ActiveExecutionTarget);
			return opers.CanExecuteFile (file, context);
		}

		public static AsyncOperation DebugFile (this ProjectOperations opers, string file)
		{
			var context = new ExecutionContext (DebuggingService.GetExecutionHandler (), IdeApp.Workbench.ProgressMonitors.ConsoleFactory, IdeApp.Workspace.ActiveExecutionTarget);
			return opers.ExecuteFile (file, context);
		}

		public static AsyncOperation DebugApplication (this ProjectOperations opers, string executableFile, string args, string workingDir, IDictionary<string,string> envVars)
		{
			if (opers.CurrentRunOperation != null && !opers.CurrentRunOperation.IsCompleted)
				return opers.CurrentRunOperation;

			var monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor (System.IO.Path.GetFileName (executableFile));

			var oper = DebuggingService.Run (executableFile, args, workingDir, envVars, monitor.Console);
			opers.AddRunOperation (oper);

			oper.Task.ContinueWith (t => {
				monitor.Dispose ();
			});

			return oper;
		}

		public static AsyncOperation AttachToProcess (this ProjectOperations opers, DebuggerEngine debugger, ProcessInfo proc)
		{
			if (opers.CurrentRunOperation != null && !opers.CurrentRunOperation.IsCompleted)
				return opers.CurrentRunOperation;

			var oper = DebuggingService.AttachToProcess (debugger, proc);

			opers.AddRunOperation (oper);
			return opers.CurrentRunOperation;
		}
	}
}
