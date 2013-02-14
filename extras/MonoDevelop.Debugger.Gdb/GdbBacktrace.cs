// GdbBacktrace.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Gdb
{
	class GdbBacktrace: IBacktrace, IObjectValueSource
	{
		int fcount;
		StackFrame firstFrame;
		GdbSession session;
		DissassemblyBuffer[] disBuffers;
		int currentFrame = -1;
		long threadId;
		
		public GdbBacktrace (GdbSession session, long threadId, int count, ResultData firstFrame)
		{
			fcount = count;
			this.threadId = threadId;
			if (firstFrame != null)
				this.firstFrame = CreateFrame (firstFrame);
			this.session = session;
		}
		
		public int FrameCount {
			get {
				return fcount;
			}
		}
		
		public StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			List<StackFrame> frames = new List<StackFrame> ();
			if (firstIndex == 0 && firstFrame != null) {
				frames.Add (firstFrame);
				firstIndex++;
			}
			
			if (lastIndex >= fcount)
				lastIndex = fcount - 1;
			
			if (firstIndex > lastIndex)
				return frames.ToArray ();
			
			session.SelectThread (threadId);
			GdbCommandResult res = session.RunCommand ("-stack-list-frames", firstIndex.ToString (), lastIndex.ToString ());
			ResultData stack = res.GetObject ("stack");
			for (int n=0; n<stack.Count; n++) {
				ResultData frd = stack.GetObject (n);
				frames.Add (CreateFrame (frd.GetObject ("frame")));
			}
			return frames.ToArray ();
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> values = new List<ObjectValue> ();
			SelectFrame (frameIndex);
			
			GdbCommandResult res = session.RunCommand ("-stack-list-locals", "0");
			foreach (ResultData data in res.GetObject ("locals"))
				values.Add (CreateVarObject (data.GetValue ("name")));
			
			return values.ToArray ();
		}

		public ObjectValue[] GetParameters (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> values = new List<ObjectValue> ();
			SelectFrame (frameIndex);
			GdbCommandResult res = session.RunCommand ("-stack-list-arguments", "0", frameIndex.ToString (), frameIndex.ToString ());
			foreach (ResultData data in res.GetObject ("stack-args").GetObject (0).GetObject ("frame").GetObject ("args"))
				values.Add (CreateVarObject (data.GetValue ("name")));
			
			return values.ToArray ();
		}

		public ObjectValue GetThisReference (int frameIndex, EvaluationOptions options)
		{
			return null;
		}
		
		public ObjectValue[] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> locals = new List<ObjectValue> ();
			locals.AddRange (GetParameters (frameIndex, options));
			locals.AddRange (GetLocalVariables (frameIndex, options));
			return locals.ToArray ();
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, EvaluationOptions options)
		{
			List<ObjectValue> values = new List<ObjectValue> ();
			SelectFrame (frameIndex);
			foreach (string exp in expressions)
				values.Add (CreateVarObject (exp));
			return values.ToArray ();
		}
		
		public ExceptionInfo GetException (int frameIndex, EvaluationOptions options)
		{
			return null;
		}
		
		public ValidationResult ValidateExpression (int frameIndex, string expression, EvaluationOptions options)
		{
			return new ValidationResult (true, null);
		}
		
		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			SelectFrame (frameIndex);
			
			bool pointer = exp.EndsWith ("->");
			int i;
			
			if (pointer || exp.EndsWith (".")) {
				exp = exp.Substring (0, exp.Length - (pointer ? 2 : 1));
				i = 0;
				while (i < exp.Length) {
					ObjectValue val = CreateVarObject (exp);
					if (!val.IsUnknown && !val.IsError) {
						CompletionData data = new CompletionData ();
						foreach (ObjectValue cv in val.GetAllChildren ())
							data.Items.Add (new CompletionItem (cv.Name, cv.Flags));
						data.ExpressionLength = 0;
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
				
				CompletionData cdata = new CompletionData ();
				cdata.ExpressionLength = partialWord.Length;
				
				// Local variables
				
				GdbCommandResult res = session.RunCommand ("-stack-list-locals", "0");
				foreach (ResultData data in res.GetObject ("locals")) {
					string name = data.GetValue ("name");
					if (name.StartsWith (partialWord))
						cdata.Items.Add (new CompletionItem (name, ObjectValueFlags.Variable));
				}
				
				// Parameters
				
				res = session.RunCommand ("-stack-list-arguments", "0", frameIndex.ToString (), frameIndex.ToString ());
				foreach (ResultData data in res.GetObject ("stack-args").GetObject (0).GetObject ("frame").GetObject ("args")) {
					string name = data.GetValue ("name");
					if (name.StartsWith (partialWord))
						cdata.Items.Add (new CompletionItem (name, ObjectValueFlags.Parameter));
				}
				
				if (cdata.Items.Count > 0)
					return cdata;
			}			
			return null;
		}

		
		ObjectValue CreateVarObject (string exp)
		{
			try {
				session.SelectThread (threadId);
				exp = exp.Replace ("\"", "\\\"");
				GdbCommandResult res = session.RunCommand ("-var-create", "-", "*", "\"" + exp + "\"");
				string vname = res.GetValue ("name");
				session.RegisterTempVariableObject (vname);
				return CreateObjectValue (exp, res);
			} catch {
				return ObjectValue.CreateUnknown (exp);
			}
		}

		ObjectValue CreateObjectValue (string name, ResultData data)
		{
			string vname = data.GetValue ("name");
			string typeName = data.GetValue ("type");
			string value = data.GetValue ("value");
			int nchild = data.GetInt ("numchild");
			
			ObjectValue val;
			ObjectValueFlags flags = ObjectValueFlags.Variable;
			
			// There can be 'public' et al children for C++ structures
			if (typeName == null)
				typeName = "none";
			
			if (typeName.EndsWith ("]")) {
				val = ObjectValue.CreateArray (this, new ObjectPath (vname), typeName, nchild, flags, null);
			} else if (value == "{...}" || typeName.EndsWith ("*") || nchild > 0) {
				val = ObjectValue.CreateObject (this, new ObjectPath (vname), typeName, value, flags, null);
			} else {
				val = ObjectValue.CreatePrimitive (this, new ObjectPath (vname), typeName, new EvaluationResult (value), flags);
			}
			val.Name = name;
			return val;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			List<ObjectValue> children = new List<ObjectValue> ();
			session.SelectThread (threadId);
			GdbCommandResult res = session.RunCommand ("-var-list-children", "2", path.Join ("."));
			ResultData cdata = res.GetObject ("children");
			
			// The response may not contain the "children" list at all.
			if (cdata == null)
				return children.ToArray ();
			
			if (index == -1) {
				index = 0;
				count = cdata.Count;
			}
			
			for (int n=index; n<cdata.Count && n<index+count; n++) {
				ResultData data = cdata.GetObject (n);
				ResultData child = data.GetObject ("child");
				
				string name = child.GetValue ("exp");
				if (name.Length > 0 && char.IsNumber (name [0]))
					name = "[" + name + "]";
				
				// C++ structures may contain typeless children named
				// "public", "private" and "protected".
				if (child.GetValue("type") == null) {
					ObjectPath childPath = new ObjectPath (child.GetValue ("name").Split ('.'));
					ObjectValue[] subchildren = GetChildren (childPath, -1, -1, options);
					children.AddRange(subchildren);
				} else {
					ObjectValue val = CreateObjectValue (name, child);
					children.Add (val);
				}
			}
			return children.ToArray ();
		}
		
		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			session.SelectThread (threadId);
			session.RunCommand ("-var-assign", path.Join ("."), value);
			return new EvaluationResult (value);
		}
		
		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			throw new NotSupportedException ();
		}
		
		void SelectFrame (int frame)
		{
			session.SelectThread (threadId);
			if (frame != currentFrame) {
				session.RunCommand ("-stack-select-frame", frame.ToString ());
				currentFrame = frame;
			}
		}
		
		StackFrame CreateFrame (ResultData frameData)
		{
			string lang = "Native";
			string func = frameData.GetValue ("func");
			string sadr = frameData.GetValue ("addr");
			
			if (func == "??" && session.IsMonoProcess) {
				// Try to get the managed func name
				try {
					ResultData data = session.RunCommand ("-data-evaluate-expression", "mono_pmip(" + sadr + ")");
					string val = data.GetValue ("value");
					if (val != null) {
						int i = val.IndexOf ('"');
						if (i != -1) {
							func = val.Substring (i).Trim ('"',' ');
							lang = "Mono";
						}
					}
				} catch {
				}
			}

			int line = -1;
			string sline = frameData.GetValue ("line");
			if (sline != null)
				line = int.Parse (sline);
			
			string sfile = frameData.GetValue ("fullname");
			if (sfile == null)
				sfile = frameData.GetValue ("file");
			if (sfile == null)
				sfile = frameData.GetValue ("from");
			SourceLocation loc = new SourceLocation (func ?? "?", sfile, line);
			
			long addr;
			if (!string.IsNullOrEmpty (sadr))
				addr = long.Parse (sadr.Substring (2), NumberStyles.HexNumber);
			else
				addr = 0;
			
			return new StackFrame (addr, loc, lang);
		}

		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			SelectFrame (frameIndex);
			if (disBuffers == null)
				disBuffers = new DissassemblyBuffer [fcount];
			
			DissassemblyBuffer buffer = disBuffers [frameIndex];
			if (buffer == null) {
				ResultData data = session.RunCommand ("-stack-info-frame");
				long addr = long.Parse (data.GetObject ("frame").GetValue ("addr").Substring (2), NumberStyles.HexNumber);
				buffer = new GdbDissassemblyBuffer (session, addr);
				disBuffers [frameIndex] = buffer;
			}
			
			return buffer.GetLines (firstLine, firstLine + count - 1);
		}
		
		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			return null;
		}
		
		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
		}
	}
	
	class GdbDissassemblyBuffer: DissassemblyBuffer
	{
		GdbSession session;
		
		public GdbDissassemblyBuffer (GdbSession session, long addr): base (addr)
		{
			this.session = session;
		}
		
		public override AssemblyLine[] GetLines (long startAddr, long endAddr)
		{
			try {
				ResultData data = session.RunCommand ("-data-disassemble", "-s", startAddr.ToString (), "-e", endAddr.ToString (), "--", "0");
				ResultData ins = data.GetObject ("asm_insns");
				
				AssemblyLine[] alines = new AssemblyLine [ins.Count];
				for (int n=0; n<ins.Count; n++) {
					ResultData aline = ins.GetObject (n);
					long addr = long.Parse (aline.GetValue ("address").Substring (2), NumberStyles.HexNumber);
					AssemblyLine line = new AssemblyLine (addr, aline.GetValue ("inst"));
					alines [n] = line;
				}
				return alines;
			} catch {
				return new AssemblyLine [0];
			}
		}
	}
}
