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
using MonoDevelop.Core;
 
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Profiling
{
	public enum PadCommands
	{
		StopProfiling,
		TakeSnapshot,
		OpenSnapshot
	}
	
	internal class StopProfilingHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (ProfilingService.IsProfilerActive) {
				ProfilingService.ActiveProfiler.Stop ();
				ProfilingService.ActiveProfiler = null;
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ProfilingService.IsProfilerActive &&
				ProfilingService.ActiveProfiler.State != ProfilerState.Inactive;
		}
	}
	
	internal class TakeSnapshotHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (ProfilingService.IsProfilerActive)
				ProfilingService.ActiveProfiler.TakeSnapshot ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ProfilingService.IsProfilerActive &&
				ProfilingService.ActiveProfiler.State == ProfilerState.Profiling;
		}
	}
	
	internal class OpenSnapshotHandler : CommandHandler
	{
		protected override void Run ()
		{
			//TODO: patch the FileSelectorDialog to allow specific addin paths to be used ?
			
			FileChooserDialog dlg = new FileChooserDialog (
				GettextCatalog.GetString ("Select Executable"), null, FileChooserAction.Open,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-open", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
			dlg.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));

			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = GettextCatalog.GetString ("All files");
			dlg.AddFilter (filterAll);

			if (dlg.Run () == (int)ResponseType.Accept)
				ProfilingService.LoadSnapshot (dlg.Filename);
			dlg.Destroy ();
		}
	}
}