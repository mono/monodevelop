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
	
	
	class GdbBacktrace: IBacktrace
	{
		int fcount;
		StackFrame firstFrame;
		GdbSession session;
		
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
			throw new NotImplementedException();
		}

		public ObjectValue[] GetParameters (int frameIndex)
		{
			throw new NotImplementedException();
		}

		public ObjectValue GetThisReference (int frameIndex)
		{
			throw new NotImplementedException();
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions)
		{
			throw new NotImplementedException();
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
			
			return new StackFrame (addr, loc);
		}

	}
}
