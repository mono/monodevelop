//
// SynchronizedProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.IO;

namespace MonoDevelop.Services
{
	public sealed class SynchronizedProgressMonitor: IProgressMonitor
	{
		IProgressMonitor monitor;
		
		public SynchronizedProgressMonitor (IProgressMonitor monitor)
		{
			this.monitor = monitor;
		}
		
		public void BeginTask (string name, int totalWork)
		{
			lock (monitor.SyncRoot) {
				monitor.BeginTask (name, totalWork);
			}
		}
		
		public void EndTask ()
		{
			lock (monitor.SyncRoot) {
				monitor.EndTask ();
			}
		}
		
		public void Step (int work)
		{
			lock (monitor.SyncRoot) {
				monitor.Step (work);
			}
		}
		
		public TextWriter Log {
			get { return monitor.Log; }
		}
		
		public void ReportSuccess (string message)
		{
			lock (monitor.SyncRoot) {
				monitor.ReportSuccess (message);
			}
		}
		
		public void ReportWarning (string message)
		{
			lock (monitor.SyncRoot) {
				monitor.ReportWarning (message);
			}
		}
		
		public void ReportError (string message, Exception ex)
		{
			lock (monitor.SyncRoot) {
				monitor.ReportError (message, ex);
			}
		}
		
		public bool IsCancelRequested {
			get {
				lock (monitor.SyncRoot) {
					return monitor.IsCancelRequested;
				}
			}
		}
		
		public void Dispose ()
		{
			lock (monitor.SyncRoot) {
				monitor.Dispose ();
			}
		}
		
		public IAsyncOperation AsyncOperation
		{
			get {
				lock (monitor.SyncRoot) {
					return monitor.AsyncOperation;
				}
			}
		}
		
		public event MonitorHandler CancelRequested {
			add {
				lock (monitor.SyncRoot) {
					monitor.CancelRequested += value;
				}
			}
			remove {
				lock (monitor.SyncRoot) {
					monitor.CancelRequested -= value;
				}
			}
		}
		
		public object SyncRoot {
			get { return monitor.SyncRoot; }
		}
	}
}
