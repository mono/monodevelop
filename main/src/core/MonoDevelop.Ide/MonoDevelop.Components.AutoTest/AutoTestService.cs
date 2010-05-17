// 
// AutoTestService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.Commands;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Runtime.Remoting.Channels.Ipc;

namespace MonoDevelop.Components.AutoTest
{
	public static class AutoTestService
	{
		static CommandManager manager;
		static AutoTestSession currentSession;
		static AutoTestClientSession currentClient;
		
		public static void Start (CommandManager manager)
		{
			Console.WriteLine ("pp111:");
			if (currentSession != null)
				throw new InvalidOperationException ("Test session already started");
			
			AutoTestService.manager = manager;
			
			string sref = Environment.GetEnvironmentVariable ("MONO_AUTOTEST_CLIENT");
			if (!string.IsNullOrEmpty (sref)) {
				Console.WriteLine ("AutoTest service starting");
				SetupRemoting ();
				byte[] data = Convert.FromBase64String (sref);
				MemoryStream ms = new MemoryStream (data);
				BinaryFormatter bf = new BinaryFormatter ();
				currentClient = (AutoTestClientSession) bf.Deserialize (ms);
				currentSession = new AutoTestSession ();
				currentClient.Connect (currentSession);
			}
		}
		
		internal static CommandManager CommandManager {
			get { return manager; }
		}
		
		public static void NotifyEvent (string eventName)
		{
			if (currentClient != null) {
				try {
					currentClient.NotifyEvent (eventName);
				} catch {}
			}
		}
		
		internal static void SetupRemoting ()
		{
			IChannel ch = ChannelServices.GetChannel ("ipc");
			if (ch == null) {
				IDictionary dict = new Hashtable ();
				BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
				BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
				string unixRemotingFile = Path.GetTempFileName ();
				dict ["portName"] = Path.GetFileName (unixRemotingFile);
				serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
				ChannelServices.RegisterChannel (new IpcChannel (dict, clientProvider, serverProvider), false);
			}
		}
	}
}

