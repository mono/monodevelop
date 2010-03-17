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

using Gtk;
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
 
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Profiling
{
	public enum ToolCommands
	{
		ProfileExecutable,
		ProfileProcess
	}
	
	internal class ProfileExecutableHandler : CommandHandler
	{
		protected override void Run ()
		{
			SelectExecutableDialog dlg = new SelectExecutableDialog ();
			if (dlg.Run () == (int)ResponseType.Ok) {
				IProfiler profiler = dlg.Profiler;
				string executable = dlg.Executable;
				string arguments = dlg.Arguments;
				string workingDir = Path.GetDirectoryName (executable);

				ProfilingOperations.ProfileApplication (profiler, executable, workingDir, arguments);
			}
			dlg.Destroy ();
		}
		
		protected override void Update (CommandInfo info)
		{
			//TODO: disable when currently running/building a project or when already profiling
			info.Enabled = ProfilingService.SupportedProfilerCount > 0;
		}
	}
	
	internal class ProfileProcessHandler : CommandHandler
	{
		protected override void Run ()
		{
			SelectProcessDialog dlg = new SelectProcessDialog ();
			if (dlg.Run () == (int)ResponseType.Ok) {
				IProfiler profiler = dlg.Profiler;
				Process process = dlg.Process;
				
				ProfilingOperations.ProfileProcess (profiler, process);
			}
			dlg.Destroy ();
		}
		
		protected override void Update (CommandInfo info)
		{
			//TODO: disable when currently running/building a project or when already profiling
			info.Enabled = ProfilingService.SupportedProfilerCount > 0;
		}
	}
}