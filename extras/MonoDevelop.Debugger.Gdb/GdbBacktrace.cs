// GdbBacktrace.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
		List<string> variableObjects = new List<string> ();
		DissassemblyBuffer[] disBuffers;
		int currentFrame = -1;
		
		public GdbBacktrace (GdbSession session, int count, ResultData firstFrame)
		{
			fcount = count;
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
			
			GdbCommandResult res = session.RunCommand ("-stack-list-frames", firstIndex.ToString (), lastIndex.ToString ());
			ResultData stack = res.GetObject ("stack");
			for (int n=0; n<stack.Count; n++) {
				ResultData frd = stack.GetObject (n);
				frames.Add (CreateFrame (frd.GetObject ("frame")));
			}
			return frames.ToArray ();
		}

		public ObjectValue[] GetLocalVariables (int frameIndex)
		{
			List<ObjectValue> values = new List<ObjectValue> ();
			SelectFrame (frameIndex);
			
			GdbCommandResult res = session.RunCommand ("-stack-list-locals", "0");
			foreach (ResultData data in res.GetObject ("locals"))
				values.Add (CreateVarObject (data.GetValue ("name")));
			
			return values.ToArray ();
		}

		public ObjectValue[] GetParameters (int frameIndex)
		{
			List<ObjectValue> values = new List<ObjectValue> ();
			GdbCommandResult res = session.RunCommand ("-stack-list-arguments", "0", frameIndex.ToString (), frameIndex.ToString ());
			SelectFrame (frameIndex);
			foreach (ResultData data in res.GetObject ("stack-args").GetObject (0).GetObject ("frame").GetObject ("args"))
				values.Add (CreateVarObject (data.GetValue ("name")));
			
			return values.ToArray ();
		}

		public ObjectValue GetThisReference (int frameIndex)
		{
			return null;
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions)
		{
			List<ObjectValue> values = new List<ObjectValue> ();
			SelectFrame (frameIndex);
			foreach (string exp in expressions)
				values.Add (CreateVarObject (exp));
			return values.ToArray ();
		}
		
		ObjectValue CreateVarObject (string name)
		{
			GdbCommandResult res = session.RunCommand ("-var-create", "-", "*", name);
			string vname = res.GetValue ("name");
			variableObjects.Add (vname);
			return CreateObjectValue (name, res);
		}

		ObjectValue CreateObjectValue (string name, ResultData data)
		{
			string vname = data.GetValue ("name");
			string typeName = data.GetValue ("type");
			string value = data.GetValue ("value");
			int nchild = data.GetInt ("numchild");
			
			ObjectValue val;
			if (typeName.EndsWith ("]")) {
				val = ObjectValue.CreateArray (this, new ObjectPath (vname), typeName, nchild, null);
			} else if (value == "{...}" || typeName.EndsWith ("*") || nchild > 0) {
				val = ObjectValue.CreateObject (this, new ObjectPath (vname), typeName, value, null);
			} else {
				val = ObjectValue.CreatePrimitive (this, new ObjectPath (vname), typeName, value);
			}
			val.Name = name;
			return val;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			List<ObjectValue> children = new List<ObjectValue> ();
			GdbCommandResult res = session.RunCommand ("-var-list-children", "2", path.Join ("."));
			ResultData cdata = res.GetObject ("children");
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
				
				ObjectValue val = CreateObjectValue (name, child);
				children.Add (val);
			}
			return children.ToArray ();
		}
		
		void SelectFrame (int frame)
		{
			if (frame != currentFrame) {
				session.RunCommand ("-stack-select-frame", frame.ToString ());
				currentFrame = frame;
			}
		}
		
		StackFrame CreateFrame (ResultData frameData)
		{
			int line = -1;
			string sline = frameData.GetValue ("line");
			if (sline != null)
				line = int.Parse (sline);
			
			string sfile = frameData.GetValue ("fullname");
			if (sfile == null)
				sfile = frameData.GetValue ("file");
			if (sfile == null)
				sfile = frameData.GetValue ("from");
			SourceLocation loc = new SourceLocation (frameData.GetValue ("func") ?? "?", sfile, line);
			
			long addr;
			string sadr = frameData.GetValue ("addr");
			if (!string.IsNullOrEmpty (sadr))
				addr = long.Parse (sadr.Substring (2), NumberStyles.HexNumber);
			else
				addr = 0;
			
			return new StackFrame (addr, loc, "");
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
