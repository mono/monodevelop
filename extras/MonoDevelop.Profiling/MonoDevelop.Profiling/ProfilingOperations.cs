//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Profiling
{
	public static class ProfilingOperations
	{
		private static string previousContext;
		
		public static IAsyncOperation Profile (IProfiler profiler, IBuildTarget entry)
		{
			if (IdeApp.ProjectOperations.CurrentRunOperation != null
			       && !IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
			       return IdeApp.ProjectOperations.CurrentRunOperation;
			
			SwitchWorkbenchContext (ProfileWorkbenchContext);
			ExecutionContext context = new ExecutionContext (profiler.GetDefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			
			return IdeApp.ProjectOperations.Execute (entry, context);
		}
		
		public static IAsyncOperation ProfileFile (IProfiler profiler, string fileName)
		{
			if (IdeApp.ProjectOperations.CurrentRunOperation != null
			       && !IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
			       return IdeApp.ProjectOperations.CurrentRunOperation;
			
			SwitchWorkbenchContext (ProfileWorkbenchContext);
			ExecutionContext context = new ExecutionContext (profiler.GetDefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);

			return IdeApp.ProjectOperations.ExecuteFile (fileName, context);
		}
		
		public static IAsyncOperation ProfileApplication (IProfiler profiler, string executable, string workingDirectory, string args)
		{
			if (IdeApp.ProjectOperations.CurrentRunOperation != null
			       && !IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
			       return IdeApp.ProjectOperations.CurrentRunOperation;
			
			SwitchWorkbenchContext (ProfileWorkbenchContext);
			ExecutionContext context = new ExecutionContext (profiler.GetDefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);

			//TODO: 
			return NullAsyncOperation.Failure;
		}
		
		public static IAsyncOperation ProfileProcess (IProfiler profiler, Process process)
		{
			if (IdeApp.ProjectOperations.CurrentRunOperation != null
			       && !IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
			       return IdeApp.ProjectOperations.CurrentRunOperation;
			
			SwitchWorkbenchContext (ProfileWorkbenchContext);

			string workingDir = ProfilingService.GetProcessDirectory (process.Id);
			IExecutionHandler handler = profiler.GetProcessExecutionHandlerFactory (process);
			return handler.Execute (null, null, workingDir, null, null /*context.ConsoleFactory.CreateConsole (true)*/);
		}
		
		public static void RestoreWorkbenchContext ()
		{
			if (previousContext != null)
				SwitchWorkbenchContext (previousContext);
		}
		
		private static string ProfileWorkbenchContext {
			get { return "Profile"; }
		}
		
		private static void SwitchWorkbenchContext (string context)
		{
			if (IdeApp.Workbench.CurrentLayout != context)
				previousContext = IdeApp.Workbench.CurrentLayout;
			
			DispatchService.GuiDispatch (delegate () {
				IdeApp.Workbench.CurrentLayout = context;

				Pad pad = IdeApp.Workbench.GetPad<ProfilingPad> ();
				pad.Visible = true;
			});
		}
	}
}