// RemoteFrameObject.cs
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
using System.Collections.Generic;

namespace DebuggerServer
{
	public class RemoteFrameObject: MarshalByRefObject
	{
		static List<RemoteFrameObject> connectedValues = new List<RemoteFrameObject> ();
		
		public void Connect ()
		{
			// Registers the value reference. Once a remote reference of this object
			// is created, it will never be released, until DisconnectAll is called,
			// which is done every time the current backtrace changes
			lock (connectedValues) {
				connectedValues.Add (this);
			}
		}
		
		public static void DisconnectAll ()
		{
			lock (connectedValues) {
				foreach (RemoteFrameObject val in connectedValues)
					System.Runtime.Remoting.RemotingServices.Disconnect (val);
				connectedValues.Clear ();
			}
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}
