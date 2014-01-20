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
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Execution
{
	public delegate bool CanExecuteDelegate (IExecutionHandler handler);
		
	/// <summary>
	/// This class can be used to implement an execution handler that needs
	/// arguments to run.
	/// </summary>
	public abstract class ParameterizedExecutionHandler: IExecutionHandler
	{
		public abstract bool CanExecute (ExecutionCommand command);
		
		public ProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			return InternalExecute (new CommandExecutionContext (null, command), new ExecutionMode ("", "", this), command, console);
		}
		
		internal ProcessAsyncOperation InternalExecute (CommandExecutionContext ctx, IExecutionMode mode, ExecutionCommand command, IConsole console)
		{
			CustomExecutionMode cmode = ExecutionModeCommandService.ShowParamtersDialog (ctx, mode, null);
			if (cmode == null)
				return new CancelledProcessAsyncOperation ();
			
			return cmode.Execute (command, console, false, false);
		}

		/// <summary>
		/// Runs a command
		/// </summary>
		/// <param name="command">
		/// Command to run
		/// </param>
		/// <param name="console">
		/// The console where to redirect the output
		/// </param>
		/// <param name="ctx">
		/// Context with execution information
		/// </param>
		/// <param name="configurationData">
		/// Configuration information. Created by the IExecutionConfigurationEditor object.
		/// </param>
		public abstract ProcessAsyncOperation Execute (ExecutionCommand command, IConsole console, CommandExecutionContext ctx, object configurationData);
		
		/// <summary>
		/// Creates an editor to be used to edit the execution handler arguments.
		/// </summary>
		/// <returns>
		/// A <see cref="IExecutionConfigurationEditor"/>
		/// </returns>
		public abstract IExecutionConfigurationEditor CreateEditor ();
	}
	
	
	class CancelledProcessAsyncOperation: ProcessAsyncOperation
	{
		public CancelledProcessAsyncOperation ()
		{
			ExitCode = 1;
		}
	}
}
