
namespace Stetic
{
	public delegate void SignalChangedEventHandler (object sender, SignalChangedEventArgs args);
	
	public class SignalChangedEventArgs: SignalEventArgs
	{
		Signal oldSignal;
		
		public SignalChangedEventArgs (ObjectWrapper wrapper, Signal oldSignal, Signal signal): base (wrapper, signal)
		{
			this.oldSignal = oldSignal;
		}
		
		public Signal OldSignal {
			get { return oldSignal; }
		}
	}
}
