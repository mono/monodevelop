//
// DebuggerEngineBackend.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public abstract class DebuggerEngineBackend
	{
		public abstract bool CanDebugCommand (ExecutionCommand cmd);

		/// <summary>
		/// Determines whether this instance is default debugger for the provided command
		/// </summary>
		/// <remarks>The default implementation returns false.</remarks>
		public virtual bool IsDefaultDebugger (ExecutionCommand cmd)
		{
			return false;
		}

		public abstract DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand cmd);

		public virtual ProcessInfo[] GetAttachableProcesses ()
		{
			return new ProcessInfo[0];
		}

		/// <summary>
		/// <see cref="GetProcessAttacher"/> has two advantages over <see cref="GetAttachableProcesses"/>:
		/// 1) it gives debugger engine ability to update processes list as soon as it discovers new processes via <see cref="ProcessAttacher.AttachableProcessesChanged"/>
		/// 2) it gives debugger engine lifetime information via <see cref="ProcessAttacher.Dispose"/>, so debugger engine can clean resources used to look for processes e.g. loop thread, sockets...
		/// </summary>
		/// <remarks>
		/// If this method returns valid <see cref="ProcessAttacher"/>, <see cref="GetAttachableProcesses"/> won't be called. 
		/// </remarks>
		/// <returns>The process attacher.</returns>
		public virtual ProcessAttacher GetProcessAttacher ()
		{
			return null;
		}

		public abstract DebuggerSession CreateSession ();
	}

	#pragma warning disable 618
	class LegacyDebuggerEngineBackend: DebuggerEngineBackend
	{
		IDebuggerEngine engine;

		public LegacyDebuggerEngineBackend (IDebuggerEngine engine)
		{
			this.engine = engine;
		}

		public override bool CanDebugCommand (ExecutionCommand cmd)
		{
			return engine.CanDebugCommand (cmd);
		}

		public override bool IsDefaultDebugger (ExecutionCommand cmd)
		{
			return false;
		}

		public override DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand cmd)
		{
			return engine.CreateDebuggerStartInfo (cmd);
		}

		public override ProcessInfo[] GetAttachableProcesses ()
		{
			return engine.GetAttachableProcesses ();
		}

		public override DebuggerSession CreateSession ()
		{
			return engine.CreateSession ();
		}
	}
	#pragma warning restore 618
}

