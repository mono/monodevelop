using System;
using System.Collections.Generic;
	
using MD = Mono.Debugger;
using DL = DebuggerLibrary;

using DebuggerLibrary;

namespace DebuggerServer
{
	class BacktraceWrapper: MarshalByRefObject, IBacktrace
	{
		MD.Backtrace backtrace;
		MD.StackFrame [] frames;
	       
		public BacktraceWrapper (MD.Backtrace backtrace)
		{
			this.backtrace = backtrace;
		}
	       
		public int FrameCount {
			get { return backtrace.Count; }
		}
	       
		public DL.StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			if (frames == null)
				frames = backtrace.Frames;
			
			//FIXME: validate indices

			List<DL.StackFrame> list = new List<DL.StackFrame> ();
			for (int i = firstIndex; i < lastIndex && i < backtrace.Count; i ++) {
				MD.SourceLocation md_location = frames [i].SourceLocation;
				DL.SourceLocation dl_location;
				string method = null;
				string filename = null;
				int line = -1;
				
				if (frames [i].Method != null) {
					method = frames [i].Method.Name;
				} else {
					//FIXME: What is .Name here?
					method = frames [i].Name.Name;
				}
				
				if (frames [i].SourceAddress != null) {
					if (frames [i].SourceAddress.SourceFile != null)
						filename = frames [i].SourceAddress.SourceFile.Name;
					line = frames [i].SourceAddress.Row;
				}
				
				list.Add (new DL.StackFrame (frames [i].TargetAddress.Address, new DL.SourceLocation (method, filename, line)));
			}
			
			return list.ToArray ();
		}
	} 
}
