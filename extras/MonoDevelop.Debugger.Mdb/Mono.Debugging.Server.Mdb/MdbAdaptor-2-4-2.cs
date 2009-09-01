// 
// MdbAdaptor22.cs
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
using MDB=Mono.Debugger;
using Mono.Debugging.Backend.Mdb;

namespace DebuggerServer
{
	public class MdbAdaptor_2_4_2: MdbAdaptor
	{
		public override void InitializeConfiguration ()
		{
			base.InitializeConfiguration ();
			Configuration.RedirectOutput = true;
		}

		public override void SetupXsp ()
		{
			Configuration.SetupXSP ();
			Configuration.StopOnManagedSignals = true;
		}
		
		public override void InitializeSession ()
		{
			if (StartInfo.UserCodeOnly) {
				Session.AddUserModulePath (StartInfo.WorkingDirectory);
				if (StartInfo.UserModules != null) {
					foreach (string path in StartInfo.UserModules)
						Session.AddUserModule (path);
				}
			}
		}
		
		public override void InitializeBreakpoint (MDB.SourceBreakpoint bp)
		{
		//	bp.IsUserModule = true;
		}
		
		public override void AbortThread (MDB.Thread thread, MDB.RuntimeInvokeResult result)
		{
			result.Abort ();
		}
		
		public override void ActivateEvent (Mono.Debugger.Event ev)
		{
			if (Process.MainThread.IsStopped)
				ev.Activate (Process.MainThread);
			else
				ThrowNotSupported ("Breakpoints can't be changed while the process is running.");
		}
		
		public override void RemoveEvent (Mono.Debugger.Event ev)
		{
			if (!Process.MainThread.IsStopped)
				ThrowNotSupported ("Breakpoints can't be changed while the process is running.");
			Session.DeleteEvent (ev);
		}
		
		public override void EnableEvent (Mono.Debugger.Event ev, bool enable)
		{
			if (enable)
				ev.Activate (Process.MainThread);
			else
				ev.Deactivate (Process.MainThread);
		}
	}
}
