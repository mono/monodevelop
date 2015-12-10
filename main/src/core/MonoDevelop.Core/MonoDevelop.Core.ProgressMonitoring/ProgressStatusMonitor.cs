// ConsoleProgressStatus.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;

namespace MonoDevelop.Core.ProgressMonitoring
{
	public class ProgressStatusMonitor: MarshalByRefObject, IProgressStatus, IDisposable
	{
		ProgressMonitor monitor;
		int step;
		int logLevel;
		bool canceled;
		
		public ProgressStatusMonitor (ProgressMonitor monitor): this (monitor, 1)
		{
		}
		
		public ProgressStatusMonitor (ProgressMonitor monitor, int logLevel)
		{
			this.logLevel = logLevel;
			this.monitor = monitor;
			monitor.BeginTask ("", 100);
		}
		
		public void SetMessage (string msg)
		{
			monitor.EndTask ();
			monitor.BeginTask (msg, 100 - step);
		}
		
		public void SetProgress (double progress)
		{
			int ns = (int) (progress * 100);
			monitor.Step (ns - step);
			step = ns;
		}
		
		public void Log (string msg)
		{
			monitor.Log.WriteLine (msg);
		}
		
		public void ReportWarning (string message)
		{
			monitor.ReportWarning (message);
		}
		
		public void ReportError (string message, Exception exception)
		{
			monitor.ReportError (message, exception);
		}
		
		public bool IsCanceled {
			get { return monitor.CancellationToken.IsCancellationRequested || canceled; }
		}
		
		public int LogLevel {
			get { return logLevel; }
			set { logLevel = value; }
		}
		
		public void Cancel ()
		{
			canceled = true;
		}
		
		public void Dispose ()
		{
			monitor.EndTask ();
		}
	}
}

