// 
// CommandExecutionContext.cs
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Execution
{
	public class CommandExecutionContext
	{
		CanExecuteDelegate runCheckDelegate;
		SolutionEntityItem project;
		ExecutionCommand cmd;
		
		public CommandExecutionContext (SolutionEntityItem project, CanExecuteDelegate runCheckDelegate)
		{
			this.project = project;
			this.runCheckDelegate = runCheckDelegate;
		}
		
		public CommandExecutionContext (SolutionEntityItem project, ExecutionCommand cmd)
		{
			this.project = project;
			this.cmd = cmd;
		}
		
		public SolutionEntityItem Project {
			get { return project; }
		}
		
		public bool CanExecute (IExecutionHandler handler)
		{
			if (runCheckDelegate != null)
				return runCheckDelegate (handler);
			else if (cmd != null)
				return handler.CanExecute (cmd);
			else
				return false;
		}
		
		public ExecutionCommand GetTargetCommand ()
		{
			if (cmd != null)
				return cmd;
			SpyHandler sh = new SpyHandler ();
			runCheckDelegate (sh);
			return cmd = sh.Command;
		}
		
		class SpyHandler: IExecutionHandler
		{
			public ExecutionCommand Command;
			
			public bool CanExecute (ExecutionCommand command)
			{
				Command = command;
				return true;
			}
			
			public IProcessAsyncOperation Execute (MonoDevelop.Core.Execution.ExecutionCommand command, MonoDevelop.Core.Execution.IConsole console)
			{
				throw new InvalidOperationException ();
			}
		}
	}
}
