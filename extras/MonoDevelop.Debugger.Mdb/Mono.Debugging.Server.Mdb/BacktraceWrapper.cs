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
	class BacktraceWrapper: RemoteFrameObject, IBacktrace
	{
		MD.StackFrame[] frames;
		DissassemblyBuffer[] disBuffers;
	       
		public BacktraceWrapper (MD.StackFrame[] frames)
		{
			this.frames = frames;
			Connect ();
		}
	       
		public int FrameCount {
			get { return frames.Length; }
		}
	       
		public DL.StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
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
		
		public ObjectValue[] GetLocalVariables (int frameIndex)
		{
			MD.StackFrame frame = frames [frameIndex];
			List<ObjectValue> vars = new List<ObjectValue> ();
			foreach (VariableReference vref in Util.GetLocalVariables (frame))
				vars.Add (vref.CreateObjectValue ());
			return vars.ToArray ();
		}
		
		public ObjectValue[] GetParameters (int frameIndex)
		{
			try {
				MD.StackFrame frame = frames [frameIndex];
				List<ObjectValue> vars = new List<ObjectValue> ();
				foreach (VariableReference vref in Util.GetParameters (frame)) {
					vars.Add (vref.CreateObjectValue ());
				}
				return vars.ToArray ();
			} catch {
				return new ObjectValue [0];
			}
		}
		
		public ObjectValue GetThisReference (int frameIndex)
		{
			MD.StackFrame frame = frames [frameIndex];
			if (frame.Method != null && frame.Method.HasThis) {
				ObjectValueFlags flags = ObjectValueFlags.Field | ObjectValueFlags.ReadOnly;
				TargetVariable var = frame.Method.GetThis (frame.Thread);
				VariableReference vref = new VariableReference (frame, var, flags);
				return vref.CreateObjectValue ();
			}
			else
				return null;
		}
		
		public ObjectValue[] GetAllLocals (int frameIndex)
		{
			MD.StackFrame frame = frames [frameIndex];
			
			List<ObjectValue> locals = new List<ObjectValue> ();
			
			// 'This' reference, or a reference to the type if the method is static
			
			ObjectValue val = GetThisReference (frameIndex);
			if (val != null)
				locals.Add (val);
			else if (frame.Method != null) {
				TargetType t = frame.Method.GetDeclaringType (frame.Thread);
				ValueReference vr = new TypeValueReference (frame.Thread, t);
				locals.Add (vr.CreateObjectValue ());
			}
			
			// Parameters
			locals.AddRange (GetParameters (frameIndex));
			
			// Local variables
			locals.AddRange (GetLocalVariables (frameIndex));
			
			return locals.ToArray ();
		}
		
		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods)
		{
			ObjectValue[] values = new ObjectValue [expressions.Length];
			for (int n=0; n<values.Length; n++) {
				string exp = expressions[n];
				
				ValueReference var;
				try {
					EvaluationOptions ops = new EvaluationOptions ();
					ops.CanEvaluateMethods = evaluateMethods;
					var = (ValueReference) Server.Instance.Evaluator.Evaluate (frames[frameIndex], exp, ops);
				} catch (EvaluatorException ex) {
					values [n] = ObjectValue.CreateError (exp, ex.Message, ObjectValueFlags.None);
					continue;
				} catch (Exception ex) {
					Server.Instance.WriteDebuggerError (ex);
					values [n] = ObjectValue.CreateUnknown (exp);
					continue;
				}
				
				if (var != null)
					values [n] = var.CreateObjectValue ();
				else
					values [n] = ObjectValue.CreateUnknown (exp);
			}
			return values;
		}
		
		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			MD.StackFrame frame = frames[frameIndex];
			int i;
			
			if (exp [exp.Length - 1] == '.') {
				exp = exp.Substring (0, exp.Length - 1);
				i = 0;
				while (i < exp.Length) {
					ValueReference vr = null;
					try {
						vr = Server.Instance.Evaluator.Evaluate (frame, exp.Substring (i), null);
					} catch {
					}
					if (vr != null) {
						DL.CompletionData data = new DL.CompletionData ();
						foreach (ValueReference cv in vr.GetChildReferences ())
							data.Items.Add (new CompletionItem (cv.Name, cv.Flags));
						data.ExpressionLenght = 0;
						return data;
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
				
				foreach (ValueReference vc in Util.GetLocalVariables (frame))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Parameters
				
				foreach (ValueReference vc in Util.GetParameters (frame))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Members
				
				TargetStructObject thisobj = null;
				
				if (frame.Method.HasThis) {
					TargetObject ob = frame.Method.GetThis (frame.Thread).GetObject (frame);
					thisobj = ObjectUtil.GetRealObject (frame.Thread, ob) as TargetStructObject;
					data.Items.Add (new CompletionItem ("this", DL.ObjectValueFlags.Field | DL.ObjectValueFlags.ReadOnly));
				}
				
				TargetStructType type = frame.Method.GetDeclaringType (frame.Thread);
				
				foreach (ValueReference vc in Util.GetMembers (frame.Thread, type, thisobj))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				if (data.Items.Count > 0)
					return data;
			}
			return null;
		}
	
		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
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
