// 
// RemoteLogger.cs
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
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Mono.Remoting.Channels.Unix;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class RemoteLogger: Logger
	{
		RemoteLoggerController controller;
		IEventSource eventSource;
		
		public override void Initialize (IEventSource eventSource)
		{
			this.eventSource = eventSource;
			
			string unixPath = null;
			if (Parameters[0] == 'u') {
				unixPath = System.IO.Path.GetTempFileName ();
				Hashtable props = new Hashtable ();
				props ["path"] = unixPath;
				props ["name"] = "__internal_unix";
				ChannelServices.RegisterChannel (new UnixChannel (props, null, null), false);
			} else {
				Hashtable props = new Hashtable ();
				props ["port"] = 0;
				props ["name"] = "__internal_tcp";
				BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
				BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
				serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
				ChannelServices.RegisterChannel (new TcpChannel (props, clientProvider, serverProvider), false);
			}
			
			byte[] data = Convert.FromBase64String (Parameters.Substring (1));
			MemoryStream ms = new MemoryStream (data);
			BinaryFormatter bf = new BinaryFormatter ();
			controller = (RemoteLoggerController) bf.Deserialize (ms);
			
			eventSource.WarningRaised += EventSourceWarningRaised;
			eventSource.ErrorRaised += EventSourceErrorRaised;
		}
		
		public override void Shutdown ()
		{
			controller = null;
			eventSource.ErrorRaised -= EventSourceErrorRaised;
			eventSource.WarningRaised -= EventSourceWarningRaised;
		}


		void EventSourceWarningRaised (object sender, BuildWarningEventArgs e)
		{
			if (controller != null)
				controller.WarningRaised (e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		void EventSourceErrorRaised (object sender, BuildErrorEventArgs e)
		{
			if (controller != null)
				controller.ErrorRaised (e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}
	}
}
