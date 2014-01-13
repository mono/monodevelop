// 
// Main.cs
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
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Diagnostics;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class MainClass
	{
		static ManualResetEvent exitEvent = new ManualResetEvent (false);
		
		[STAThread]
		public static void Main (string[] args)
		{
			try {
				RegisterRemotingChannel ();
				WatchProcess (Console.ReadLine ());
				
				BuildEngine builderEngine = new BuildEngine ();
				BinaryFormatter bf = new BinaryFormatter ();
				ObjRef oref = RemotingServices.Marshal (builderEngine);
				MemoryStream ms = new MemoryStream ();
				bf.Serialize (ms, oref);
				Console.Error.WriteLine (Convert.ToBase64String (ms.ToArray ()));
				
				if (WaitHandle.WaitAny (new WaitHandle[] { builderEngine.WaitHandle, exitEvent }) == 0) {
					// Wait before exiting, so that the remote call that disposed the builder can be completed
					System.Threading.Thread.Sleep (400);
				}
				
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public static void RegisterRemotingChannel ()
		{
			IDictionary dict = new Hashtable ();
			BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
			BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
			dict ["port"] = 0;
			dict ["rejectRemoteRequests"] = true;
			serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
			ChannelServices.RegisterChannel (new TcpChannel (dict, clientProvider, serverProvider), false);
		}
		
		public static void WatchProcess (string procId)
		{
			int id = int.Parse (procId);
			Thread t = new Thread (delegate () {
				while (true) {
					Thread.Sleep (1000);
					try {
						// Throws exception if process is not running.
						// When watching a .NET process from Mono, GetProcessById may
						// return the process with HasExited=true
						Process p = Process.GetProcessById (id);
						if (p.HasExited)
							break;
					}
					catch {
						break;
					}
				}
				exitEvent.Set ();
			});
			t.IsBackground = true;
			t.Start ();
		}
	}
}
