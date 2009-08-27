// 
// ProcessService.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Messaging;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Execution
{
	class DisposerFormatterSink : IClientFormatterSink
	{
		IClientChannelSink nextSink;
		
		public DisposerFormatterSink (IClientChannelSink nextSink)
		{
			this.nextSink = nextSink;
		}
		
		public IMessage SyncProcessMessage (IMessage msg)
		{
			IMethodCallMessage mcm = (IMethodCallMessage) msg;
			int timeout = -1;
			bool timedOut = false;
			
			RemotingService.CallbackData data = RemotingService.GetCallbackData (mcm.Uri, mcm.MethodName);
			if (data != null) {
				timeout = data.Timeout;
				if (data.Calling != null) {
					IMessage r = data.Calling (data.Target, mcm);
					if (r != null)
						return r;
				}
			}
			
			IMessage res = null;
			
			if (timeout != -1) {
				ManualResetEvent ev = new ManualResetEvent (false);
				ThreadPool.QueueUserWorkItem (delegate {
					res = ((IMessageSink)nextSink).SyncProcessMessage (msg);
				});
				if (!ev.WaitOne (timeout, false)) {
					timedOut = true;
					res = new ReturnMessage (null, null, 0, mcm.LogicalCallContext, mcm);
				}
			}
			else {
				res = ((IMessageSink)nextSink).SyncProcessMessage (msg);
			}
			
			if (data != null && data.Called != null) {
				IMessage cr = data.Called (data.Target, mcm, res as IMethodReturnMessage, timedOut);
				if (cr != null)
					res = cr;
			}
			
			return res;
		}
		
		public IMessageCtrl AsyncProcessMessage (IMessage msg, IMessageSink replySink)
		{
			return ((IMessageSink)nextSink).AsyncProcessMessage (msg, replySink);
		}
		
		public IMessageSink NextSink {
			get { return (IMessageSink)nextSink; }
		}
		
		public void AsyncProcessResponse (IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			nextSink.AsyncProcessResponse (sinkStack, state, headers, stream);
		}
		
		public Stream GetRequestStream (IMessage msg, ITransportHeaders headers)
		{
			// never called
			throw new NotSupportedException ();
		}
		
		public void ProcessMessage (IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// never called because the formatter sink is
			// always the first in the chain
			throw new NotSupportedException ();
		}
		
		public void AsyncProcessRequest (IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			// never called because the formatter sink is
			// always the first in the chain
			throw new NotSupportedException ();
		}
		
		public IClientChannelSink NextChannelSink {
			get { return nextSink; }
		}
		
		public IDictionary Properties {
			get { return null; }
		}
	}
	
	class DisposerFormatterSinkProvider: IClientFormatterSinkProvider
	{
		IClientChannelSinkProvider next;
		
		public IClientChannelSink CreateSink (IChannelSender channel, string url, object remoteChannelData)
		{
			IClientChannelSink nextSink = null;
			if (next != null)
				nextSink = next.CreateSink (channel, url, remoteChannelData);
			
			return new DisposerFormatterSink (nextSink);
		}
		
		public IClientChannelSinkProvider Next {
			get { return next; }
			set { next = value; }
		}
	}
	
	public delegate IMethodReturnMessage CallingMethodCallback (object obj, IMethodCallMessage msg);
	public delegate IMethodReturnMessage CalledMethodCallback (object obj, IMethodCallMessage msg, IMethodReturnMessage ret, bool timedOut);
}
