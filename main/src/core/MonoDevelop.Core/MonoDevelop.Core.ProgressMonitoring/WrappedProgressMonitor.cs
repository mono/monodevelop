// 
// ProjectLoadProgressMonitor.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011 Xamarin Inc.
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
using System;

namespace MonoDevelop.Core
{
	public class WrappedProgressMonitor : IProgressMonitor
	{
		public event MonitorHandler CancelRequested {
			add { WrappedMonitor.CancelRequested += value; }
			remove { WrappedMonitor.CancelRequested -= value; }
		}

		public IAsyncOperation AsyncOperation {
			get { return WrappedMonitor.AsyncOperation; }
		}

		public bool IsCancelRequested {
			get { return WrappedMonitor.IsCancelRequested; }
		}
		
		public System.IO.TextWriter Log {
			get { return WrappedMonitor.Log; }
		}

		public object SyncRoot {
			get { return WrappedMonitor.SyncRoot; }
		}
		
		IProgressMonitor WrappedMonitor {
			get; set;
		}
		
		public WrappedProgressMonitor (IProgressMonitor monitor)
		{
			WrappedMonitor = monitor;
		}

		public void BeginStepTask (string name, int totalWork, int stepSize)
		{
			WrappedMonitor.BeginStepTask (name, totalWork, stepSize);
		}

		public void BeginTask (string name, int totalWork)
		{
			WrappedMonitor.BeginTask (name, totalWork);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;

			if (WrappedMonitor != null) {
				WrappedMonitor.Dispose ();
				WrappedMonitor = null;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		public void EndTask ()
		{
			WrappedMonitor.EndTask ();
		}
		
		public void ReportError (string message, Exception exception)
		{
			WrappedMonitor.ReportError (message, exception);
		}
		
		public void ReportSuccess (string message)
		{
			WrappedMonitor.ReportSuccess (message);
		}
		
		public void ReportWarning (string message)
		{
			WrappedMonitor.ReportWarning (message);
		}
		
		public void Step (int work)
		{
			WrappedMonitor.Step (work);
		}
	}
}

