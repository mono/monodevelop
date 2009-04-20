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
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Profiling
{
	public class ProcessProfilerExecutionHandler : NativePlatformExecutionHandler
	{
		private IProfiler profiler;
		private Process process;
		
		public ProcessProfilerExecutionHandler (IProfiler profiler, Process process)
		{
			if (profiler == null)
				throw new ArgumentNullException ("profiler");
			if (process == null)
				throw new ArgumentNullException ("process");
			
			this.profiler = profiler;
			this.process = process;
		}

		public override IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			DummyProcessAsyncOperation dpao = new DummyProcessAsyncOperation (process);
			string profilerIdentifier, tempFile, snapshotFile;
			ProfilingService.GetProfilerInformation (process.Id, out profilerIdentifier, out tempFile);
			DotNetExecutionCommand dotcmd = (DotNetExecutionCommand) command;
			snapshotFile = profiler.GetSnapshotFileName (dotcmd.WorkingDirectory, tempFile);
			
			ProfilingService.ActiveProfiler = profiler;
			ProfilingContext profContext = new ProfilingContext (dpao, snapshotFile);
			profiler.Start (profContext);

			return dpao;
		}
		
		public override bool CanExecute (ExecutionCommand command)
		{
			return command is DotNetExecutionCommand;
		}
	}
}