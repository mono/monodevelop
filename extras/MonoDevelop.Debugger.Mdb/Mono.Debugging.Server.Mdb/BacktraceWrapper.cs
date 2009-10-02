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
	class BacktraceWrapper: RemoteFrameObject, IBacktrace, IDisposable
	{
		MD.StackFrame[] frames;
		DissassemblyBuffer[] disBuffers;
		bool disposed;
	       
		public BacktraceWrapper (MD.StackFrame[] frames)
		{
			this.frames = frames;
			Connect ();
		}

		public void Dispose ()
		{
			disposed = true;
		}
	       
		public int FrameCount {
			get { return frames.Length; }
		}
	       
		public DL.StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
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

		protected EvaluationContext GetEvaluationContext (int frameIndex, int timeout)
		{
			CheckDisposed ();
			if (timeout == -1)
				timeout = DebuggerServer.DefaultEvaluationTimeout;
			MD.StackFrame frame = frames [frameIndex];
			return new MdbEvaluationContext (frame.Thread, frame, timeout);
		}
	
		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
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
		
		public ObjectValue[] GetAllLocals (int frameIndex, int timeout)
		{
			List<ObjectValue> locals = new List<ObjectValue> ();

			locals.AddRange (GetLocalVariables (frameIndex, timeout));
			locals.AddRange (GetParameters (frameIndex, timeout));
			locals.Sort (delegate (ObjectValue v1, ObjectValue v2) {
				return v1.Name.CompareTo (v2.Name);
			});

			ObjectValue thisObj = GetThisReference (frameIndex, timeout);
			if (thisObj != null)
				locals.Insert (0, thisObj);
			
			return locals.ToArray ();
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			return ctx.Adapter.GetExpressionValuesAsync (ctx, expressions, evaluateMethods, timeout);
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> list = new List<ObjectValue> ();
			foreach (ValueReference var in ctx.Adapter.GetLocalVariables (ctx))
				list.Add (var.CreateObjectValue (true));
			return list.ToArray ();
		}

		public ObjectValue[] GetParameters (int frameIndex, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> vars = new List<ObjectValue> ();
			foreach (ValueReference var in ctx.Adapter.GetParameters (ctx))
				vars.Add (var.CreateObjectValue (true));
			return vars.ToArray ();
		}

		public ObjectValue GetThisReference (int frameIndex, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			ValueReference var = ctx.Adapter.GetThisReference (ctx);
			if (var != null)
				return var.CreateObjectValue ();
			else
				return null;
		}
		
		public virtual CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, 400);
			return ctx.Adapter.GetExpressionCompletionData (ctx, exp);
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
