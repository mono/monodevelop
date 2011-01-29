// 
// RemoteSoftDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc.
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
using Mono.Debugger.Soft;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft
{
	public class RemoteDebuggerStartInfo : RemoteSoftDebuggerStartInfo
	{
		static RemoteDebuggerStartInfo ()
		{
			SoftDebuggerEngine.EnsureSdbLoggingService ();
		}
		
		public RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort)
			: base (appName, address, debugPort) {}
		
		public RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort, int outputPort)
			: base (appName, address, debugPort, outputPort) {}
		
		public void SetUserAssemblies (IList<string> files)
		{
			string error;
			UserAssemblyNames = SoftDebuggerEngine.GetAssemblyNames (files, out error);
			LogMessage = error;
		}
	}
}
