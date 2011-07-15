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
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;

namespace MonoDevelop.Core.Execution
{
	public static class RemotingService
	{
		static string unixRemotingFile;
		static Dictionary<string,CallbackData> callbacks = new Dictionary<string, CallbackData> ();
		static bool channelRegistered;
		static HashSet<string> simpleResolveAssemblies = new HashSet<string> ();
		
		internal class CallbackData
		{
			public object Target;
			public int Timeout;
			public string Method;
			public CallingMethodCallback Calling;
			public CalledMethodCallback Called;
		}
		
		public static void RegisterRemotingChannel ()
		{
			if (!channelRegistered) {
				channelRegistered = true;
				
				IDictionary formatterProps = new Hashtable ();
				formatterProps ["includeVersions"] = false;
				formatterProps ["strictBinding"] = false;
				
				// Don't reuse ipc channels registered by add-ins. That's not supported.
				IChannel ch = ChannelServices.GetChannel ("ipc");
				if (ch != null) {
					LoggingService.LogFatalError ("IPC channel already registered. An add-in may have registered it");
					throw new InvalidOperationException ("IPC channel already registered. An add-in may have registered it.");
				}
				
				BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider(formatterProps, null);
				serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
				DisposerFormatterSinkProvider clientProvider = new DisposerFormatterSinkProvider();
				clientProvider.Next = new BinaryClientFormatterSinkProvider(formatterProps, null);
				
				unixRemotingFile = Path.GetTempFileName ();
				IDictionary dict = new Hashtable ();
				dict ["portName"] = Path.GetFileName (unixRemotingFile);
				ChannelServices.RegisterChannel (new IpcChannel (dict, clientProvider, serverProvider), false);
				
				// Register the TCP channel too. It is used for communication of Mono -> .NET. The IPC channel
				// has interoperabilitu issues.
				
				// Don't reuse tcp channels registered by add-ins. That's not supported.
				ch = ChannelServices.GetChannel ("tcp");
				if (ch != null) {
					LoggingService.LogFatalError ("TCP channel already registered. An add-in may have registered it");
					throw new InvalidOperationException ("TCP channel already registered. An add-in may have registered it.");
				}
				
				serverProvider = new BinaryServerFormatterSinkProvider(formatterProps, null);
				serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
				clientProvider = new DisposerFormatterSinkProvider();
				clientProvider.Next = new BinaryClientFormatterSinkProvider(formatterProps, null);
				
				dict = new Hashtable ();
				dict ["port"] = 0;
				dict ["rejectRemoteRequests"] = true;
				
				ChannelServices.RegisterChannel (new TcpChannel (dict, clientProvider, serverProvider), false);

				// This is a workaround to a serialization interoperability issue between Mono and .NET
				// For some reason, .NET is unable to resolve add-in assemblies referenced in
				// serialized objects, when the assemblies are not in the main bin directory
				if (Platform.IsWindows) {
					AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs args) {
						if (!simpleResolveAssemblies.Contains (args.Name))
							return null;
						foreach (Assembly am in AppDomain.CurrentDomain.GetAssemblies ()) {
							if (am.GetName ().FullName == args.Name || args.Name == am.GetName ().Name) {
								Console.WriteLine (Environment.StackTrace);
								return am;
							}
						}
						return null;
					};
				}
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
		
		// This method is used in Windows to allow the provided assembly name to be loaded
		// using the name only, discarding version info. This is a workaround to a serialization
		// interoperability issue.
		internal static void RegisterAssemblyForSimpleResolve (string name)
		{
			simpleResolveAssemblies.Add (name);
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
