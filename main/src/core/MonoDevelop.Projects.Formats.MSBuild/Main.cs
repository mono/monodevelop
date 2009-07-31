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
using System.Runtime.Remoting.Channels.Ipc;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class MainClass
	{
		static string unixRemotingFile;
		
		public static void Main (string[] args)
		{
			Console.WriteLine ("pp1:");
			try {
				RegisterRemotingChannel ();
				
				ProjectBuilder builder = new ProjectBuilder ();
				BinaryFormatter bf = new BinaryFormatter ();
				ObjRef oref = RemotingServices.Marshal (builder);
				MemoryStream ms = new MemoryStream ();
				bf.Serialize (ms, oref);
				Console.Error.WriteLine (Convert.ToBase64String (ms.ToArray ()));
				Console.WriteLine ("pp2:");
				builder.WaitForDone ();
				// Wait before exiting, so that the remote call that disposed the builder can be completed
				System.Threading.Thread.Sleep (400);
				Console.WriteLine ("pp3:");
				
				if (unixRemotingFile != null && File.Exists (unixRemotingFile))
					File.Delete (unixRemotingFile);
			} catch (Exception ex) {
				Console.WriteLine ("pp2:" + ex);
			}
		}
		
		public static void RegisterRemotingChannel ()
		{
			IDictionary dict = new Hashtable ();
			BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
			BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
			unixRemotingFile = Path.GetTempFileName ();
			dict ["portName"] = Path.GetFileName (unixRemotingFile);
			serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
			Console.WriteLine ("pp22:" + dict ["portName"]);
			ChannelServices.RegisterChannel (new IpcChannel (dict, clientProvider, serverProvider), false);
		}
	}
}
