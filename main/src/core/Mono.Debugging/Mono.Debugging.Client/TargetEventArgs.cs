using System;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class TargetEventArgs: EventArgs
	{
		TargetEventType type;
		Backtrace backtrace;

		public TargetEventArgs(TargetEventType type)
		{
			this.type = type;
		}

		public TargetEventType Type
		{
			get { return type; }
			set { type = value; }
		}

		public Backtrace Backtrace
		{
			get { return backtrace; }
			set { backtrace = value; }
		}
	}
}
