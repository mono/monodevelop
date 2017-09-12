//
// IProcessAsyncOperation.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Core.Execution
{
	public class ProcessAsyncOperation: AsyncOperation
	{
		protected ProcessAsyncOperation ()
		{
		}

		public ProcessAsyncOperation (Task task, CancellationTokenSource cancellationTokenSource): base (task, cancellationTokenSource)
		{
		}

		public int ExitCode { get; set; }

		int processId;
		public int ProcessId {
			get {
				return processId;
			}
			set {
				if (processId != value) {
					processId = value;
					ProcessIdSet?.Invoke (this);
				}
			}
		}

		public event Action<ProcessAsyncOperation> ProcessIdSet;
	}

	public class NullProcessAsyncOperation : ProcessAsyncOperation
	{
		public NullProcessAsyncOperation (int exitCode)
		{
			ExitCode = exitCode;
		}

		public static NullProcessAsyncOperation Success = new NullProcessAsyncOperation (0);
		public static NullProcessAsyncOperation Failure = new NullProcessAsyncOperation (1);
	}
}
