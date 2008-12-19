using System;
using System.Text;
using System.Collections.Generic;
	
using MD = Mono.Debugger;
using DL = Mono.Debugging.Client;

using Mono.Debugging.Client;
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

		EvaluationContext GetEvaluationContext (int frameIndex, int timeout)
		{
			CheckDisposed ();
			if (timeout == -1)
				timeout = DebuggerServer.DefaultEvaluationTimeout;
			MD.StackFrame frame = frames [frameIndex];
			return new EvaluationContext (frame.Thread, frame, timeout);
		}
		
		public ObjectValue[] GetLocalVariables (int frameIndex, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> vars = new List<ObjectValue> ();
			foreach (VariableReference vref in Util.GetLocalVariables (ctx))
				vars.Add (vref.CreateObjectValue (true));
			return vars.ToArray ();
		}
		
		public ObjectValue[] GetParameters (int frameIndex, int timeout)
		{
			try {
				EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
				List<ObjectValue> vars = new List<ObjectValue> ();
				foreach (VariableReference vref in Util.GetParameters (ctx)) {
					vars.Add (vref.CreateObjectValue (true));
				}
				return vars.ToArray ();
			} catch {
				return new ObjectValue [0];
			}
		}
		
		public ObjectValue GetThisReference (int frameIndex, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			if (ctx.Frame.Method != null && ctx.Frame.Method.HasThis) {
				ObjectValueFlags flags = ObjectValueFlags.Field | ObjectValueFlags.ReadOnly;
				TargetVariable var = ctx.Frame.Method.GetThis (ctx.Thread);
				VariableReference vref = new VariableReference (ctx, var, flags);
				return vref.CreateObjectValue ();
			}
			else
				return null;
		}
		
		public ObjectValue[] GetAllLocals (int frameIndex, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			
			List<ObjectValue> locals = new List<ObjectValue> ();
			
			// 'This' reference, or a reference to the type if the method is static
			
			ObjectValue val = GetThisReference (frameIndex, timeout);
			if (val != null)
				locals.Add (val);
			else if (ctx.Frame.Method != null) {
				TargetType t = ctx.Frame.Method.GetDeclaringType (ctx.Thread);
				if (t != null) {
					ValueReference vr = new TypeValueReference (ctx, t);
					locals.Add (vr.CreateObjectValue (true));
				}
			}
			
			// Parameters
			locals.AddRange (GetParameters (frameIndex, timeout));
			
			// Local variables
			locals.AddRange (GetLocalVariables (frameIndex, timeout));
			
			return locals.ToArray ();
		}
		
		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			ObjectValue[] values = new ObjectValue [expressions.Length];
			for (int n=0; n<values.Length; n++) {
				string exp = expressions[n];
				values[n] = Server.Instance.AsyncEvaluationTracker.Run (exp, ObjectValueFlags.Literal, delegate {
					return GetExpressionValue (ctx, exp, evaluateMethods);
				});
			}
			return values;
		}
		
		ObjectValue GetExpressionValue (EvaluationContext ctx, string exp, bool evaluateMethods)
		{
			try {
				EvaluationOptions ops = new EvaluationOptions ();
				ops.CanEvaluateMethods = evaluateMethods;
				ValueReference var = (ValueReference) Server.Instance.Evaluator.Evaluate (ctx, exp, ops);
				if (var != null)
					return var.CreateObjectValue ();
				else
					return ObjectValue.CreateUnknown (exp);
			} catch (NotSupportedExpressionException ex) {
				return ObjectValue.CreateNotSupported (exp, ex.Message, ObjectValueFlags.None);
			} catch (EvaluatorException ex) {
				return ObjectValue.CreateError (exp, ex.Message, ObjectValueFlags.None);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				return ObjectValue.CreateUnknown (exp);
			}
		}
		
		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, -1);
			int i;

			if (exp [exp.Length - 1] == '.') {
				exp = exp.Substring (0, exp.Length - 1);
				i = 0;
				while (i < exp.Length) {
					ValueReference vr = null;
					try {
						vr = Server.Instance.Evaluator.Evaluate (ctx, exp.Substring (i), null);
						if (vr != null) {
							DL.CompletionData data = new DL.CompletionData ();
							foreach (ValueReference cv in vr.GetChildReferences ())
								data.Items.Add (new CompletionItem (cv.Name, cv.Flags));
							data.ExpressionLenght = 0;
							return data;
						}
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
					i++;
				}
				return null;
			}
			
			i = exp.Length - 1;
			bool lastWastLetter = false;
			while (i >= 0) {
				char c = exp [i--];
				if (!char.IsLetterOrDigit (c) && c != '_')
					break;
				lastWastLetter = !char.IsDigit (c);
			}
			if (lastWastLetter) {
				string partialWord = exp.Substring (i+1);
				
				DL.CompletionData data = new DL.CompletionData ();
				data.ExpressionLenght = partialWord.Length;
				
				// Local variables
				
				foreach (ValueReference vc in Util.GetLocalVariables (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Parameters
				
				foreach (ValueReference vc in Util.GetParameters (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Members
				
				TargetStructObject thisobj = null;
				
				if (ctx.Frame.Method.HasThis) {
					TargetObject ob = ctx.Frame.Method.GetThis (ctx.Thread).GetObject (ctx.Frame);
					thisobj = ObjectUtil.GetRealObject (ctx, ob) as TargetStructObject;
					data.Items.Add (new CompletionItem ("this", DL.ObjectValueFlags.Field | DL.ObjectValueFlags.ReadOnly));
				}
				
				TargetStructType type = ctx.Frame.Method.GetDeclaringType (ctx.Thread);
				
				foreach (ValueReference vc in Util.GetMembers (ctx, type, thisobj))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				if (data.Items.Count > 0)
					return data;
			}
			return null;
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
