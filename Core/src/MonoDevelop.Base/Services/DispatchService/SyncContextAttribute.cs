// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>


using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Activation;

namespace MonoDevelop.Services
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
			return new SyncContextDispatchSink (nextSink, syncContext);
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
		
		class MsgData
		{
			public IMessage InMessage;
			public IMessage OutMessage;
			public IMessageSink ReplySink;
		}
		
		public SyncContextDispatchSink (IMessageSink nextSink, SyncContext syncContext)
		{
			this.nextSink = nextSink;
			this.syncContext = syncContext;
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

			MsgData md = new MsgData ();
			md.InMessage = msg;
			md.ReplySink = replySink;
			syncContext.AsyncDispatch (new StatefulMessageHandler (AsyncDispatchMessage), md);
			return null;
		}
		
		void AsyncDispatchMessage (object data)
		{
			MsgData md = (MsgData)data;
			md.ReplySink.SyncProcessMessage (nextSink.SyncProcessMessage (md.InMessage));
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
