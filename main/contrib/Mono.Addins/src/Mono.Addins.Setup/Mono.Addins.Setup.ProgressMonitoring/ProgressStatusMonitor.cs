//
// ProgressStatusMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace Mono.Addins.Setup.ProgressMonitoring
{
	internal class ProgressStatusMonitor: MarshalByRefObject, IProgressMonitor
	{
		IProgressStatus status;
		LogTextWriter logger;
		ProgressTracker tracker = new ProgressTracker ();
		
		public ProgressStatusMonitor (IProgressStatus status)
		{
			this.status = status;
			logger = new LogTextWriter ();
			logger.TextWritten += new LogTextEventHandler (WriteLog);
		}
		
		public static IProgressMonitor GetProgressMonitor (IProgressStatus status)
		{
			if (status == null)
				return new NullProgressMonitor ();
			else
				return new ProgressStatusMonitor (status);
		}
		
		public void BeginTask (string name, int totalWork)
		{
			tracker.BeginTask (name, totalWork);
			status.SetMessage (tracker.CurrentTask);
			status.SetProgress (tracker.GlobalWork);
		}
		
		public void BeginStepTask (string name, int totalWork, int stepSize)
		{
			tracker.BeginStepTask (name, totalWork, stepSize);
			status.SetMessage (tracker.CurrentTask);
			status.SetProgress (tracker.GlobalWork);
		}
		
		public void Step (int work)
		{
			tracker.Step (work);
			status.SetProgress (tracker.GlobalWork);
		}
		
		public void EndTask ()
		{
			tracker.EndTask ();
			status.SetMessage (tracker.CurrentTask);
			status.SetProgress (tracker.GlobalWork);
		}
		
		void WriteLog (string text)
		{
			status.Log (text);
		}
		
		public TextWriter Log {
			get { return logger; }
		}
		
		public void ReportWarning (string message)
		{
			status.ReportWarning (message);
		}
		
		public void ReportError (string message, Exception ex)
		{
			status.ReportError (message, ex);
		}
		
		public bool IsCancelRequested { 
			get { return status.IsCanceled; }
		}
		
		public void Cancel ()
		{
			status.Cancel ();
		}
		
		public int LogLevel {
			get { return status.LogLevel; }
		}
		
		public void Dispose ()
		{
		}
	}
}
