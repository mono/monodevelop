
using System;

namespace Stetic
{
	public delegate void ComponentSignalEventHandler (object sender, ComponentSignalEventArgs args);
	
	public class ComponentSignalEventArgs: ComponentEventArgs
	{
		public Signal oldSignal;
		public Signal signal;
		
		public ComponentSignalEventArgs (Project p, Component c, Signal oldSignal, Signal signal): base (p, c)
		{
			this.oldSignal = oldSignal;
			this.signal = signal;
		}
		
		public Signal Signal {
			get { return signal; }
		}
		
		public Signal OldSignal {
			get { return oldSignal; }
		}
	}
}
