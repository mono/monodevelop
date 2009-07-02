// 
// IExecutionModeFactory.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Execution
{
	public delegate bool CanExecuteDelegate (IExecutionHandler handler);
		
	public abstract class ParameterizedExecutionHandler: IExecutionHandler
	{
		public abstract bool CanExecute (ExecutionCommand command);
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			return InternalExecute (new CommandExecutionContext (null, command), null, command, console);
		}
		
		internal IProcessAsyncOperation InternalExecute (CommandExecutionContext ctx, IExecutionMode mode, ExecutionCommand command, IConsole console)
		{
			CustomExecutionMode cmode = ExecutionModeCommandService.ShowParamtersDialog (ctx, mode, null);
			if (cmode == null)
				return new CancelledProcessAsyncOperation ();
			
			return cmode.Execute (command, console, false, false);
		}

		public abstract IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console, CommandExecutionContext ctx, object configurationData);
		
		public abstract IExecutionConfigurationEditor CreateEditor ();
	}
	
	
	class CancelledProcessAsyncOperation: IProcessAsyncOperation
	{
		public int ExitCode {
			get {
				return 1;
			}
		}
		
		public int ProcessId {
			get {
				return 0;
			}
		}

		#region IAsyncOperation implementation
		public event OperationHandler Completed {
			add {
				value (this);
			}
			remove { }
		}
		
		public void Cancel ()
		{
		}
		
		public void WaitForCompleted ()
		{
		}
		
		public bool IsCompleted {
			get { return true; }
		}
		
		public bool Success {
			get { return false; }
		}
		
		public bool SuccessWithWarnings {
			get { return false; }
		}
		
		#endregion
	}
}
