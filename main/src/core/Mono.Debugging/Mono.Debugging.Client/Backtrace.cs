using System;
using System.Collections.Generic;
using Mono.Debugging.Backend;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class Backtrace
	{
		IBacktrace serverBacktrace;
		int count;

		List<StackFrame> frames;

		public Backtrace (IBacktrace serverBacktrace)
		{
			this.serverBacktrace = serverBacktrace;
			count = serverBacktrace.FrameCount;
		}

		public int FrameCount
		{
			get { return count; }
		}

		public StackFrame GetFrame (int n)
		{
			if (frames == null)
				frames = new List<StackFrame>();

			if (n >= frames.Count)
			{
				StackFrame[] newSet = serverBacktrace.GetStackFrames(frames.Count, n + 20);
				foreach (StackFrame sf in newSet) {
					sf.SourceBacktrace = serverBacktrace;
					sf.Index = frames.Count;
					frames.Add (sf);
				}
			}
			return frames[n];
		}
	}
}
