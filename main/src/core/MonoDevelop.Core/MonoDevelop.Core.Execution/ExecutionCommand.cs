// 
// ExecutionCommand.cs
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

using MonoDevelop.Projects;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// Set of parameters to be used to execute a file or project
	/// </summary>
	/// <remarks>
	/// This is the base class for types of commands that can be used
	/// to run a project or file. This class only contains the data
	/// required to run the project, but not the actual execution logic.
	/// The execution logic is provided by classes that implement
	/// IExecutionHandler. A project generates an ExecutionCommand
	/// instance, and a user can select a IExecutionHandler to
	/// run it.
	/// </remarks>
	public abstract class ExecutionCommand
	{
		/// <summary>
		/// Execution target. For example, a specific device.
		/// </summary>
		public ExecutionTarget Target { get; set; }

		/// <summary>
		/// IRunTarget item associated with this execution command. This allows the DebuggerSession to be
		/// associated with an IRunTarget (typically a Project).
		/// </summary>
		public IRunTarget RunTarget { get; set; }
	}
}
