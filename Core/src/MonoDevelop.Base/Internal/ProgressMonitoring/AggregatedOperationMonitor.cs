//
// AggregatedOperationMonitor.cs
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
using System.Collections;

namespace MonoDevelop.Services
{
	public class AggregatedOperationMonitor: IDisposable
	{
		ArrayList list = new ArrayList ();
		IProgressMonitor monitor;
		
		public AggregatedOperationMonitor (IProgressMonitor monitor, params IAsyncOperation[] operations)
		{
			this.monitor = monitor;
			
			if (operations != null) {
				lock (list) {
					foreach (IAsyncOperation operation in operations)
						AddOperation (operation);
				}
			}
			
			monitor.CancelRequested += new MonitorHandler (OnCancel);
		}
		
		public void AddOperation (IAsyncOperation operation)
		{
			lock (list) {
				if (monitor.IsCancelRequested)
					operation.Cancel ();
				else
					list.Add (operation);
			}
		}
		
		void OnCancel (IProgressMonitor m)
		{
			lock (list) {
				foreach (IAsyncOperation operation in list)
					operation.Cancel ();
			}
		}
		
		public void Dispose ()
		{
			monitor.CancelRequested -= new MonitorHandler (OnCancel);
		}
	}
}
