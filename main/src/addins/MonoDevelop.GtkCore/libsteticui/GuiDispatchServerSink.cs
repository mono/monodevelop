
using System;
using System.Threading;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Collections;

namespace Stetic
{
	public class GuiDispatchServerSink: IServerChannelSink, IChannelSinkBase
	{
		IServerChannelSink nextSink;
		
		public GuiDispatchServerSink (IServerChannelSink nextSink, IChannelReceiver receiver)
		{
			this.nextSink = nextSink;
		}

		public IServerChannelSink NextChannelSink {
			get { return nextSink; }
		}

		public IDictionary Properties {
			get { return null; }
		}

		public void AsyncProcessResponse (IServerResponseChannelSinkStack sinkStack, object state,
						  IMessage msg, ITransportHeaders headers, Stream stream)
						  
		{
			sinkStack.AsyncProcessResponse (msg, headers, stream);
		}

		public Stream GetResponseStream (IServerResponseChannelSinkStack sinkStack, object state,
						IMessage msg, ITransportHeaders headers)
		{
			// this method shouldn't be called
			throw new NotSupportedException ();
		}
		
		public ServerProcessing ProcessMessage (IServerChannelSinkStack sinkStack,
							IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream,
							out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			IMethodCallMessage msg = (IMethodCallMessage) requestMsg;
//			Console.WriteLine ("MESSAGE: " + msg.TypeName + " - " + msg.MethodName);
			
			sinkStack.Push (this, null);

			if (Attribute.IsDefined (msg.MethodBase, typeof(NoGuiDispatchAttribute))) {
				ServerProcessing ret;
				try {
					ret = nextSink.ProcessMessage (sinkStack,
								 requestMsg,
								 requestHeaders,
								 requestStream,
								 out responseMsg,
								 out responseHeaders,
								 out responseStream);
				} finally {
					sinkStack.Pop (this);
				}
				return ret;
			}
			else
			{
				Dispatcher d = new Dispatcher ();
				d.nextSink = nextSink;
				d.sinkStack = sinkStack;
				d.requestMsg = requestMsg;
				d.requestHeaders = requestHeaders;
				d.requestStream = requestStream;
				
				Gtk.Application.Invoke (d.Dispatch);
				responseMsg = null;
				responseHeaders = null;
				responseStream = null;
				
				return ServerProcessing.Async;
			}
		}
		
		class Dispatcher
		{
			public IServerChannelSink nextSink;
			
			public IServerChannelSinkStack sinkStack;
			public IMessage requestMsg;
			public ITransportHeaders requestHeaders;
			public Stream requestStream;
			
			public void Dispatch (object o, EventArgs a)
			{
				IMessage responseMsg;
				ITransportHeaders responseHeaders = null;
				Stream responseStream = null;
				
				try {
					nextSink.ProcessMessage (sinkStack,
									 requestMsg,
									 requestHeaders,
									 requestStream,
									 out responseMsg,
									 out responseHeaders,
									 out responseStream);
				}
				catch (Exception ex) {
					responseMsg = new ReturnMessage (ex, (IMethodCallMessage)requestMsg);
				}
				
				sinkStack.AsyncProcessResponse (responseMsg, responseHeaders, responseStream);
			}
		}
	}
	
	class GuiDispatch
	{
		public static void InvokeSync (EventHandler h)
		{
			if (GLib.MainContext.Depth > 0)
				h (null, null);
			else {
				object wo = new object ();
				lock (wo) {
					Gtk.Application.Invoke ((o, args) => {
						try {
							h (null, null);
						} finally {
							lock (wo) {
								System.Threading.Monitor.PulseAll (wo);
							}
						}
					});
					System.Threading.Monitor.Wait (wo);
				}
			}
		}
	}
	
}
