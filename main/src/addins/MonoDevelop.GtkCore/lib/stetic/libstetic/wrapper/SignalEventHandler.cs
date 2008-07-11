
namespace Stetic
{
	public delegate void SignalEventHandler (object sender, SignalEventArgs args);
	
	public class SignalEventArgs: ObjectWrapperEventArgs
	{
		Signal signal;
		
		public SignalEventArgs (ObjectWrapper wrapper, Signal signal): base (wrapper)
		{
			this.signal = signal;
		}
		
		public Signal Signal {
			get { return signal; }
		}
	}
}
