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
	class BacktraceWrapper: MarshalByRefObject, IBacktrace, IObjectValueSource
	{
		MD.Backtrace backtrace;
		MD.StackFrame[] frames;
		DissassemblyBuffer[] disBuffers;
	       
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
			for (int i = firstIndex; i <= lastIndex && i < backtrace.Count; i ++) {
				MD.StackFrame frame = frames [i];
				string method = null;
				string filename = null;
				int line = -1;
				
				if (frame.Method != null) {
					method = frame.Method.Name;
					int p = method.IndexOf ('(');
					if (p != -1) {
						StringBuilder sb = new StringBuilder ();
						foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread)) {
							if (sb.Length > 0)
								sb.Append (", ");
							sb.Append (var.Name).Append (" = ").Append (Util.TargetObjectToString (frame.Thread, var.GetObject (frame)));
						}
						sb.Append (')');
						method = method.Substring (0, p+1) + sb.ToString ();
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
			if (frames == null)
				frames = backtrace.Frames;
			
			List<ObjectValue> vars = new List<ObjectValue> ();
			MD.StackFrame frame = frames [frameIndex];
			foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread))
				vars.Add (Util.CreateObjectValue (frame.Thread, this, new ObjectPath ("FR", frameIndex.ToString(), "LV", var.Name), var.GetObject (frame), true));
			
			return vars.ToArray ();
		}
		
		public ObjectValue[] GetParameters (int frameIndex)
		{
			List<ObjectValue> vars = new List<ObjectValue> ();
			MD.StackFrame frame = frames [frameIndex];
			if (frame.Method != null) {
				foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread))
					vars.Add (Util.CreateObjectValue (frame.Thread, this, new ObjectPath ("FR", frameIndex.ToString (), "PS", var.Name), var.GetObject (frame), true));
			}
			
			return vars.ToArray ();
		}
		
		public ObjectValue GetThisReference (int frameIndex)
		{
			MD.StackFrame frame = frames [frameIndex];
			if (frame.Method.HasThis)
				return Util.CreateObjectValue (frame.Thread, this, new ObjectPath ("FR", frameIndex.ToString (), "TR"), frame.Method.GetThis (frame.Thread).GetObject (frame), false);
			else
				return null;
		}
		
		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions)
		{
			ObjectValue[] values = new ObjectValue [expressions.Length];
			for (int n=0; n<values.Length; n++) {
				string exp = expressions[n];
				TargetObject ob = null;
				IValueReference var = GetExpressionTargetObjectReference (frames[frameIndex], exp);
				if (var != null)
					ob = var.Value;
				
				if (ob != null)
					values [n] = Util.CreateObjectValue (frames[frameIndex].Thread, this, new ObjectPath ("FR", frameIndex.ToString(), "EXP", exp), ob, var.CanWrite);
				else
					values [n] = ObjectValue.CreateUnknown (exp);
			}
			return values;
		}
		
		public IValueReference GetExpressionTargetObjectReference (MD.StackFrame frame, string exp)
		{
			FrameExpressionValueSource source = new FrameExpressionValueSource (frame);
			return source.GetValueReference (exp);
		}
		
		public IValueReference GetObjectReference (ObjectPath path, out MD.StackFrame frame)
		{
			IValueReference rootObj = null;
			frame = null;
			int pathPos = -1;
			
			// Frames query
			if (frames == null)
				frames = backtrace.Frames;
			
			if (path [0] == "FR") {
				
				frame = frames [int.Parse (path [1])];
				
				if (path [2] == "LV") {
					// Local variables query
					foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread)) {
						if (var.Name == path [3]) {
							pathPos = 4;
							rootObj = new VariableReference (frame, var);
							break;
						}
					}
				}
				else if (path [2] == "PS") {
					// Parameters query
					foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread)) {
						if (var.Name == path [3]) {
							pathPos = 4;
							rootObj = new VariableReference (frame, var);
							break;
						}
					}
				}
				else if (path [2] == "TR") {
					// This reference
					pathPos = 3;
					TargetVariable var = frame.Method.GetThis (frame.Thread);
					rootObj = new VariableReference (frame, var);
				}
				else if (path [2] == "EXP") {
					pathPos = 4;
					rootObj = GetExpressionTargetObjectReference (frame, path[3]);
				}
			}
			
			if (rootObj == null)
				return null;
			
			return Util.GetTargetObjectReference (frame.Thread, rootObj, path, pathPos);
		}
		
		public ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			MD.StackFrame frame;
			TargetObject ob = null;
			
			IValueReference vref = GetObjectReference (path, out frame);
			if (vref != null)
				ob = vref.Value;
			
			if (ob != null)
				return Util.GetObjectValueChildren (frame.Thread, this, ob, path, index, count);
			else
				return new ObjectValue [0];
			
		}

		public string SetValue (ObjectPath path, string value)
		{
			MD.StackFrame frame;
			IValueReference var = GetObjectReference (path, out frame);

			if (var != null) {
				object ovalue = Util.StringToObject (var.Type, value);
				TargetObject newValue = frame.Language.CreateInstance (frame.Thread, ovalue);
				var.Value = newValue;
			}
			return value;
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
	
	class FrameExpressionValueSource: IExpressionValueSource
	{
		MD.StackFrame frame;
		
		public FrameExpressionValueSource (MD.StackFrame frame)
		{
			this.frame = frame;
		}
		
		public IValueReference GetValueReference (string name)
		{
			if (frame.Method == null)
				return null;
			
			// Look in variables
			
			foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread)) {
				if (var.Name == name)
					return new VariableReference (frame, var);
			}
			
			// Look in parameters
			
			foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread))
				if (var.Name == name)
					return new VariableReference (frame, var);
			
			// Look in fields
			
			TargetStructObject thisobj = null;
			
			if (frame.Method.HasThis) {
				TargetObject ob = frame.Method.GetThis (frame.Thread).GetObject (frame);
				thisobj = Util.GetRealObject (frame.Thread, ob) as TargetStructObject;
			}
			
			TargetStructType type = frame.Method.GetDeclaringType (frame.Thread);
			
			while (type != null)
			{
				foreach (TargetPropertyInfo prop in type.ClassType.Properties) {
					if (prop.Name == name && prop.CanRead && (prop.IsStatic || thisobj != null)) {
						return new PropertyReference (frame.Thread, prop, thisobj);
					}
				}
				foreach (TargetFieldInfo field in type.ClassType.Fields) {
					if (field.Name == name && (field.IsStatic || thisobj != null))
						return new FieldReference (frame.Thread, thisobj, type, field);
				}
				type = type.GetParentType (frame.Thread);
			}
			return null;
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
