// SyncContextAttribute.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Activation;

namespace MonoDevelop.Core.Gui
{
	public class SyncContextAttribute: ContextAttribute, IContributeObjectSink
	{
		Type contextType;
		SyncContext syncContext;
		
		public SyncContextAttribute (Type contextType): base ("syncContextProperty")
		{
			this.contextType = contextType;
		}
		
		public override bool IsContextOK (Context ctx, IConstructionCallMessage msg)
		{
			SyncContext sctx = SyncContext.GetContext ();
			if (sctx == null || (sctx.GetType() != contextType)) {
				syncContext = (SyncContext) Activator.CreateInstance (contextType);
				return false;
			}
			else {
				syncContext = sctx;
				return true;
			}
		}
		
		public IMessageSink GetObjectSink (MarshalByRefObject ob, IMessageSink nextSink)
		{
			return new SyncContextDispatchSink (nextSink, syncContext, ob);
		}
		
		public Type ConextType
		{
			get { return contextType; }
		}
	}
	
	internal class SyncContextDispatchSink: IMessageSink
	{
		IMessageSink nextSink;
		SyncContext syncContext;
		MarshalByRefObject target;
		static bool isMono;
		
		static SyncContextDispatchSink ()
		{
			isMono = Type.GetType ("Mono.Runtime") != null;
		}
		
		class MsgData
		{
			public IMessage InMessage;
			public IMessage OutMessage;
			public IMessageSink ReplySink;
		}

		public SyncContextDispatchSink (IMessageSink nextSink, SyncContext syncContext, MarshalByRefObject ob)
		{
			this.nextSink = nextSink;
			this.syncContext = syncContext;
			target = ob;
		}
		
        public IMessage SyncProcessMessage (IMessage msg)
		{
			if (syncContext == null) return nextSink.SyncProcessMessage (msg);
			
			IMethodMessage mm = (IMethodMessage)msg;
			
			if ((mm.MethodBase.Name == "FieldGetter" || mm.MethodBase.Name == "FieldSetter") && mm.MethodBase.DeclaringType == typeof(object)) {
				return nextSink.SyncProcessMessage (msg);
			}
			
			if (mm.MethodBase.IsDefined (typeof(FreeDispatchAttribute), true)) {
				return nextSink.SyncProcessMessage (msg);
			}

			if (mm.MethodBase.IsDefined (typeof(AsyncDispatchAttribute), true)) {
				AsyncProcessMessage (msg, DummySink.Instance);
				return new ReturnMessage (null, null, 0, null, (IMethodCallMessage)mm);
			}

			MsgData md = new MsgData ();
			md.InMessage = msg;
			SyncContext oldCtx = SyncContext.GetContext ();
			try {
				SyncContext.SetContext (syncContext);
				syncContext.Dispatch (new StatefulMessageHandler (DispatchMessage), md);
			} finally {
				SyncContext.SetContext (oldCtx);
			}
			
			return md.OutMessage;
		}
		
		void DispatchMessage (object data)
		{
			MsgData md = (MsgData)data;
			md.OutMessage = nextSink.SyncProcessMessage (md.InMessage);
		}
		
        public IMessageCtrl AsyncProcessMessage (IMessage msg, IMessageSink replySink)
		{
			if (syncContext == null) return nextSink.AsyncProcessMessage (msg, replySink);

			// Make a copy of the message since MS.NET seems to free the original message
			// once it has been dispatched.
			if (!isMono)
				msg = new MethodCall (msg);

			MsgData md = new MsgData ();
			md.InMessage = msg;
			md.ReplySink = replySink;
			syncContext.AsyncDispatch (new StatefulMessageHandler (AsyncDispatchMessage), md);
			return null;
		}
		
		void AsyncDispatchMessage (object data)
		{
			MsgData md = (MsgData)data;
			if (isMono) {
				md.ReplySink.SyncProcessMessage (nextSink.SyncProcessMessage (md.InMessage));
			} else {
				// In MS.NET, async calls have to be dispatched using ExecuteMessage,
				// but this doesn't work in mono because mono will route the message
				// through the remoting context sink again causing an infinite loop.
				IMethodCallMessage msg = (IMethodCallMessage) md.InMessage;
				msg.MethodBase.Invoke (target, msg.Args);
			}
		}
		
        public IMessageSink NextSink
		{
			get { return nextSink; }
		}
	}
	
	internal class DummySink: IMessageSink
	{
		public static DummySink Instance = new DummySink();
		
        public IMessage SyncProcessMessage (IMessage msg)
		{
			// Ignore
			return null;
		}
		
        public IMessageCtrl AsyncProcessMessage (IMessage msg, IMessageSink replySink)
		{
			// Ignore
			return null;
		}
		
        public IMessageSink NextSink
		{
			get { return null; }
		}
	}
}
