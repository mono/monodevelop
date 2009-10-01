// 
// AggregatedAsyncOperation.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace MonoDevelop.Core.ProgressMonitoring
{
	public class AggregatedAsyncOperation: IAsyncOperation
	{
		public event OperationHandler Completed;
		
		List<IAsyncOperation> operations = new List<IAsyncOperation> ();
		bool started;
		bool success = true;
		bool successWithWarnings = true;
		int completed;
		
		public void Add (IAsyncOperation oper)
		{
			if (started)
				throw new InvalidOperationException ("Can't add more operations after calling StartMonitoring");
			operations.Add (oper);
		}
		
		public void StartMonitoring ()
		{
			started = true;
			foreach (IAsyncOperation oper in operations)
				oper.Completed += OperCompleted;
		}

		void OperCompleted (IAsyncOperation op)
		{
			bool raiseEvent;
			lock (operations) {
				completed++;
				success = success && op.Success;
				successWithWarnings = success && op.SuccessWithWarnings;
				raiseEvent = (completed == operations.Count);
			}
			if (raiseEvent && Completed != null)
				Completed (this);
		}
		
		public void Cancel ()
		{
			CheckStarted ();
			lock (operations) {
				foreach (IAsyncOperation op in operations)
					op.Cancel ();
			}
		}
		
		public void WaitForCompleted ()
		{
			CheckStarted ();
			foreach (IAsyncOperation op in operations)
				op.WaitForCompleted ();
		}
		
		public bool IsCompleted {
			get {
				CheckStarted ();
				return completed == operations.Count;
			}
		}
		
		public bool Success {
			get {
				CheckStarted ();
				return success;
			}
		}
		
		public bool SuccessWithWarnings {
			get {
				CheckStarted ();
				return successWithWarnings;
			}
		}
		
		void CheckStarted ()
		{
			if (!started)
				throw new InvalidOperationException ("Operation not started");
		}
	}
}
