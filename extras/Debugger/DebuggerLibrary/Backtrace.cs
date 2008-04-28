using System;
using System.Collections.Generic;

namespace DebuggerLibrary
{
	[Serializable]
	public class Backtrace
	{
		IBacktrace serverBacktrace;
		int count;

		List<StackFrame> frames;

		public Backtrace(IBacktrace serverBacktrace)
		{
			this.serverBacktrace = serverBacktrace;
			count = serverBacktrace.FrameCount;
		}

		public int FrameCount
		{
			get { return count; }
		}

		public StackFrame GetFrame(int n)
		{
			if (frames == null)
				frames = new List<StackFrame>();

			if (n >= frames.Count)
			{
				StackFrame[] newSet = serverBacktrace.GetStackFrames(frames.Count, n + 20);
				frames.AddRange(newSet);
			}
			return frames[n];
		}
	}
}
