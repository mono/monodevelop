//
// ExecutionContext.cs
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
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	[Serializable]
	public sealed class ExecutionContext
	{
		IExecutionHandler executionHandler;
		IConsoleFactory consoleFactory;
		ExecutionTarget executionTarget;
		
		public ExecutionContext (IExecutionMode executionMode, IConsoleFactory consoleFactory, ExecutionTarget target = null)
		{
			this.executionHandler = executionMode.ExecutionHandler;
			this.consoleFactory = consoleFactory;
			this.executionTarget = target;
		}
		
		public ExecutionContext (IExecutionHandler executionHandler, IConsoleFactory consoleFactory, ExecutionTarget target = null)
		{
			this.executionHandler = executionHandler;
			this.consoleFactory = consoleFactory;
			this.executionTarget = target;
		}
		
		public IExecutionHandler ExecutionHandler {
			get { return executionHandler; }
		}
		
		public ExecutionTarget ExecutionTarget {
			get { return executionTarget; }
		}
		
		public IConsoleFactory ConsoleFactory {
			get { return consoleFactory; }
		}
		
		public IConsoleFactory ExternalConsoleFactory {
			get { return MonoDevelop.Core.Execution.ExternalConsoleFactory.Instance; }
		}
	}
}
