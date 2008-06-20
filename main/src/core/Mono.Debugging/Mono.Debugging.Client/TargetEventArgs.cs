using System;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class TargetEventArgs: EventArgs
	{
		TargetEventType type;
		Backtrace backtrace;
		ProcessInfo process;
		ThreadInfo thread;

		public TargetEventArgs (TargetEventType type)
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

		public ThreadInfo Thread {
			get {
				return thread;
			}
			set {
				thread = value;
			}
		}

		public ProcessInfo Process {
			get {
				return process;
			}
			set {
				process = value;
			}
		}
	}
}
