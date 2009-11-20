using System;
using System.Text;
using System.Collections.Generic;
	
using MD = Mono.Debugger;
using DL = Mono.Debugging.Client;

using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Backend;
using Mono.Debugger.Languages;

namespace DebuggerServer
{
	class BacktraceWrapper: BaseBacktrace, IBacktrace, IDisposable
	{
		MD.StackFrame[] frames;
		DissassemblyBuffer[] disBuffers;
		bool disposed;
	       
		public BacktraceWrapper (MD.StackFrame[] frames): base (Server.Instance.MdbObjectValueAdaptor)
		{
			this.frames = frames;
			Connect ();
		}

		public void Dispose ()
		{
			disposed = true;
		}
	       
		public override int FrameCount {
			get { return frames.Length; }
		}
	       
		public override DL.StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			CheckDisposed ();
			
			//FIXME: validate indices

			List<DL.StackFrame> list = new List<DL.StackFrame> ();
			for (int i = firstIndex; i <= lastIndex && i < frames.Length; i ++) {
				MD.StackFrame frame = frames [i];
				string method = null;
				string filename = null;
				int line = -1;
				
				if (frame.Method != null) {
					method = frame.Method.Name;
					if (!method.StartsWith ("<")) {
						int p = method.IndexOf ('(');
						if (p != -1)
							method = method.Substring (0, p).Trim ();
					}
				} else if (frame.Name != null) {
					method = frame.Name.Name;
				} else {
					method = "?";
				}
				
				if (frame.SourceAddress != null) {
					if (frame.SourceAddress.SourceFile != null)
						filename = frame.SourceAddress.SourceFile.FileName;
					line = frame.SourceAddress.Row;
				}
				
				string lang = frame.Language != null ? frame.Language.Name : string.Empty;
				list.Add (new DL.StackFrame (frame.TargetAddress.Address, new DL.SourceLocation (method, filename, line), lang));
			}
			
			return list.ToArray ();
		}

		protected override EvaluationContext GetEvaluationContext (int frameIndex, EvaluationOptions options)
		{
			CheckDisposed ();
			MD.StackFrame frame = frames [frameIndex];
			return new MdbEvaluationContext (frame.Thread, frame, options);
		}
	
		public override AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			CheckDisposed ();
			if (disBuffers == null)
				disBuffers = new MdbDissassemblyBuffer [frames.Length];
			
			MD.StackFrame frame = frames [frameIndex];
			DissassemblyBuffer buffer = disBuffers [frameIndex];
			if (buffer == null) {
				buffer = new MdbDissassemblyBuffer (frame.Thread, frame.TargetAddress);
				disBuffers [frameIndex] = buffer;
			}
			
			return buffer.GetLines (firstLine, firstLine + count - 1);
		}

		void CheckDisposed ()
		{
			if (disposed)
				throw new InvalidOperationException ("Invalid stack frame");
		}
	}
	
	class MdbDissassemblyBuffer: DissassemblyBuffer
	{
		MD.Thread thread;
		MD.TargetAddress baseAddr;
		
		public MdbDissassemblyBuffer (MD.Thread thread, MD.TargetAddress addr): base (addr.Address)
		{
			this.thread = thread;
			this.baseAddr = addr;
		}
		
		public override AssemblyLine[] GetLines (long startAddr, long endAddr)
		{
			List<AssemblyLine> lines = new List<AssemblyLine> ();
			
			MD.TargetAddress addr = baseAddr + (startAddr - baseAddr.Address);
			while (addr.Address <= endAddr) {
				try {
					MD.AssemblerLine line = thread.DisassembleInstruction (null, addr);
					lines.Add (new AssemblyLine (addr.Address, line.Text));
					addr += line.InstructionSize;
				} catch {
					Console.WriteLine ("failed " + addr.Address);
					lines.Add (new AssemblyLine (addr.Address, "??"));
					addr++;
				}
			}
			return lines.ToArray ();
		}
	}
}
