using System;
using System.Collections.Generic;
	
using MD = Mono.Debugger;
using DL = Mono.Debugging.Client;

using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using Mono.Debugger.Languages;

namespace DebuggerServer
{
	class BacktraceWrapper: MarshalByRefObject, IBacktrace, IObjectValueSource
	{
		MD.Backtrace backtrace;
		MD.StackFrame[] frames;
	       
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
				} else if (frames [i].Name != null) {
					method = frames [i].Name.Name;
				} else {
					method = "?";
				}
				
				if (frames [i].SourceAddress != null) {
					if (frames [i].SourceAddress.SourceFile != null)
						filename = frames [i].SourceAddress.SourceFile.FileName;
					line = frames [i].SourceAddress.Row;
				}
				
				list.Add (new DL.StackFrame (frames [i].TargetAddress.Address, new DL.SourceLocation (method, filename, line)));
			}
			
			return list.ToArray ();
		}
		
		public ObjectValue[] GetLocalVariables (int frameIndex)
		{
			if (frames == null)
				frames = backtrace.Frames;
			
			List<ObjectValue> vars = new List<ObjectValue> ();
			MD.StackFrame frame = frames [frameIndex];
			foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread))
				vars.Add (Util.CreateObjectValue (frame.Thread, this, "FR/" + frameIndex + "/LV/" + var.Name, var.GetObject (frame)));
			
			return vars.ToArray ();
		}
		
		public ObjectValue[] GetParameters (int frameIndex)
		{
			List<ObjectValue> vars = new List<ObjectValue> ();
			MD.StackFrame frame = frames [frameIndex];
			foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread))
				vars.Add (Util.CreateObjectValue (frame.Thread, this, "FR/" + frameIndex + "/PS/" + var.Name, var.GetObject (frame)));
			
			return vars.ToArray ();
		}
		
		public ObjectValue GetThisReference (int frameIndex)
		{
			MD.StackFrame frame = frames [frameIndex];
			if (frame.Method.HasThis)
				return Util.CreateObjectValue (frame.Thread, this, "FR/" + frameIndex + "/TR", frame.Method.GetThis (frame.Thread).GetObject (frame));
			else
				return null;
		}
		
		public ObjectValue[] GetChildren (string pathStr, int index, int count)
		{
			string[] path = pathStr.Split ('/');
			
			if (path [0] == "FR") {
				
				// Frames query
				if (frames == null)
					frames = backtrace.Frames;
				
				MD.StackFrame frame = frames [int.Parse (path [1])];
				
				if (path [2] == "LV") {
					// Local variables query
					foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread)) {
						if (var.Name == path [3])
							return Util.GetObjectValueChildren (this, var.GetObject (frame), path, 4, 0, index, count);
					}
				}
				else if (path [2] == "PS") {
					// Parameters query
					foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread)) {
						if (var.Name == path [3])
							return Util.GetObjectValueChildren (this, var.GetObject (frame), path, 4, 0, index, count);
					}
				}
				else if (path [2] == "TR") {
					// This reference
					TargetVariable var = frame.Method.GetThis (frame.Thread);
					return Util.GetObjectValueChildren (this, var.GetObject (frame), path, 3, 0, index, count);
				}
			}
			return null;
		}
	} 
}
