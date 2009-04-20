// 
// PythonExecutionHandler.cs
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
using System.IO;
using MonoDevelop.Core.Execution;

namespace PyBinding
{
	public class PythonExecutionHandler: NativePlatformExecutionHandler
	{
		public override bool CanExecute (ExecutionCommand command)
		{
			return command is PythonExecutionCommand;
		}
		
		public override IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			PythonExecutionCommand cmd = (PythonExecutionCommand) command;
			
			string[] args = cmd.Configuration.Runtime.GetArguments (cmd.Configuration);
			string dir = Path.GetFullPath (cmd.Configuration.OutputDirectory);
			
			NativeExecutionCommand ncmd = new NativeExecutionCommand (cmd.Configuration.Runtime.Path, string.Join (" ", args), dir, null);
			return base.Execute (ncmd, console);
		}
		
	}
}
