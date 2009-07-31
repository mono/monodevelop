// 
// RemotingServices.cs
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
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Messaging;

namespace MonoDevelop.Core.Execution
{
	public static class RemotingService
	{
		static string unixRemotingFile;
		static DisposerFormatterSinkProvider clientProvider;
		static Dictionary<string,CallbackData> callbacks = new Dictionary<string, CallbackData> ();
		
		internal class CallbackData
		{
			public object Target;
			public int Timeout;
			public string Method;
			public CallingMethodCallback Calling;
			public CalledMethodCallback Called;
		}
		
		static RemotingService ()
		{
			clientProvider = new DisposerFormatterSinkProvider();
			clientProvider.Next = new BinaryClientFormatterSinkProvider();
		}
		
		public static void RegisterRemotingChannel ()
		{
			IChannel ch = ChannelServices.GetChannel ("ipc");
			if (ch == null) {
				IDictionary dict = new Hashtable ();
				BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
				unixRemotingFile = Path.GetTempFileName ();
				dict ["portName"] = Path.GetFileName (unixRemotingFile);
				serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
				ChannelServices.RegisterChannel (new IpcChannel (dict, clientProvider, serverProvider), false);
			}
		}
		
		public static void RegisterMethodCallback (object proxy, string method, CallingMethodCallback calling, CalledMethodCallback called)
		{
			RegisterMethodCallback (proxy, method, calling, called, -1);
		}
		
		public static void RegisterMethodCallback (object proxy, string method, CallingMethodCallback calling, CalledMethodCallback called, int timeout)
		{
			string uri = RemotingServices.GetObjectUri ((MarshalByRefObject)proxy);
			CallbackData data = new CallbackData ();
			data.Target = proxy;
			data.Calling = calling;
			data.Called = called;
			data.Timeout = timeout;
			data.Method = method;
			callbacks [uri + " " + method] = data;
		}
		
		public static void UnregisterMethodCallback (object proxy, string method)
		{
			string uri = RemotingServices.GetObjectUri ((MarshalByRefObject)proxy);
			callbacks.Remove (uri + " " + method);
		}
		
		internal static CallbackData GetCallbackData (string uri, string method)
		{
			CallbackData data;
			callbacks.TryGetValue (uri + " " + method, out data);
			return data;
		}
		
		internal static void Dispose ()
		{
			if (unixRemotingFile != null)
				File.Delete (unixRemotingFile);
		}
	}
}
