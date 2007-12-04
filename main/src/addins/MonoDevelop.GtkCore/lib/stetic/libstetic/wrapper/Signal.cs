
using System;

namespace Stetic
{
	[Serializable]
	public class Signal
	{
		SignalDescriptor descriptor;
		string handlerName;
		bool after;
		
		[NonSerialized]
		internal ObjectWrapper Owner;
		
		public Signal (SignalDescriptor descriptor): this (descriptor, null, false)
		{
		}
		
		public Signal (SignalDescriptor descriptor, string handlerName, bool after)
		{
			this.descriptor = descriptor;
			this.handlerName = handlerName;
			this.after = after;
		}
		
		void NotifyChanged (Signal oldData)
		{
			if (Owner != null)
				Owner.OnSignalChanged (new SignalChangedEventArgs (Owner, oldData, this)); 
		}
		
		Signal Clone ()
		{
			return new Signal (descriptor, handlerName, after);
		}
		
		public SignalDescriptor SignalDescriptor {
			get { return descriptor; }
		}
		
		public string Handler {
			get { return handlerName; }
			set {
				Signal data = Clone ();
				handlerName = value;
				NotifyChanged (data);
			}
		}
		
		public bool After {
			get { return after; }
			set {
				Signal data = Clone ();
				after = value;
				NotifyChanged (data);
			}
		}
	}
}
