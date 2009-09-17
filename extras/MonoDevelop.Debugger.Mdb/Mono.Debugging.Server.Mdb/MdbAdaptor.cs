// 
// MdbAdaptor.cs
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
	public abstract class MdbAdaptor
	{
		public MdbAdaptor ()
		{
			MdbVersion = "2.0";
		}
		
		public string MdbVersion { get; internal set; }
		
		public MDB.DebuggerSession Session;
		public MDB.DebuggerConfiguration Configuration;
		public MDB.Process Process;
		public MonoDebuggerStartInfo StartInfo;
		
		public virtual void SetupXsp ()
		{
			ThrowNotSupported ("ASP.NET debugging not supported");
		}
		
		public virtual void InitializeBreakpoint (MDB.SourceBreakpoint bp)
		{
		}
		
		public virtual void InitializeConfiguration ()
		{
		}
		
		public virtual void InitializeSession ()
		{
		}
		
		public abstract void AbortThread (MDB.Thread thread, MDB.RuntimeInvokeResult result);
		
		public virtual void EnableEvent (MDB.Event ev, bool enable)
		{
		}
		
		public virtual void ActivateEvent (MDB.Event ev)
		{
		}
		
		public virtual void RemoveEvent (MDB.Event ev)
		{
		}
		
		public void ThrowNotSupported (string feature)
		{
			throw new Mono.Debugging.Client.DebuggerException (feature + ". You need to install a more recent Mono Debugger version.");
		}
		
		public virtual bool AllowBreakEventChanges {
			get {
				return true;
			}
		}
	}
}
