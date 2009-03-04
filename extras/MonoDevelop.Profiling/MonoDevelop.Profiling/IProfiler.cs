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
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Profiling
{
	public interface IProfiler
	{
		string Identifier { get; }
		string Name { get; }
		string IconString { get; }
		
		bool IsSupported { get; }

		string GetSnapshotFileName (string workingDirectory, string filename);
		
		IExecutionHandler GetDefaultExecutionHandlerFactory ();
		IExecutionHandler GetProcessExecutionHandlerFactory (Process process);
		
		event ProfilingSnapshotEventHandler SnapshotTaken;
		event EventHandler SnapshotFailed;
		event ProfilerStateEventHandler StateChanged;		
		
		event EventHandler Started;
		event EventHandler Stopped;
		
		ProfilerState State { get; }
		ProfilingContext Context { get; }
		
		void Start (ProfilingContext context);
		void Stop ();

		void TakeSnapshot ();
		
		bool CanLoad (string fileName);
		IProfilingSnapshot Load (string filename);
		string GetSaveLocation ();
	}
}