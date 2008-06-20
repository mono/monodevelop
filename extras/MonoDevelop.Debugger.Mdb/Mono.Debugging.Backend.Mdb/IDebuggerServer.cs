// IDebuggerServer.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend.Mdb
{
	public interface IDebuggerServer
	{
		void Run (DebuggerStartInfo startInfo);

		void Stop ();
		
		void AttachToProcess (int id);
		
		void Detach ();
		
		void Exit ();

		// Step one source line
		void StepLine ();

		// Step one source line, but step over method calls
		void NextLine ();

		// Step one instruction
		void StepInstruction ();

		// Step one instruction, but step over method calls
		void NextInstruction ();
		
		// Continue until leaving the current method
		void Finish ();

		//breakpoints etc

		// returns a handle
		int InsertBreakpoint (string filename, int line, bool activate);

		void RemoveBreakpoint (int handle);
		
		void EnableBreakpoint (int handle, bool enable);

		void Continue ();
	}
}
