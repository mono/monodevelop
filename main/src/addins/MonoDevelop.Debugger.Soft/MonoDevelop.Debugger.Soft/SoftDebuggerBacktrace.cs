// 
// SoftDebuggerBacktrace.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using MDB = Mono.Debugger;
using DC = Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.Soft
{
	public class SoftDebuggerBacktrace: IBacktrace
	{
		MDB.StackFrame[] frames;
		SoftDebuggerSession session;
		
		const int DefaultEvaluationTimeout = 1000;
		
		public SoftDebuggerBacktrace (SoftDebuggerSession session, MDB.StackFrame[] frames)
		{
			this.session = session;
			this.frames = frames;
		}

		public DC.StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			if (lastIndex == -1)
				lastIndex = frames.Length - 1;
			List<DC.StackFrame> list = new List<DC.StackFrame> ();
			for (int n = firstIndex; n <= lastIndex && n < frames.Length; n++)
				list.Add (CreateStackFrame (frames [n]));
			return list.ToArray ();
		}
		
		DC.StackFrame CreateStackFrame (MDB.StackFrame frame)
		{
			return new DC.StackFrame (frame.ILOffset, "", frame.Method.Name, frame.FileName, frame.LineNumber, "Managed");
		}
		
		protected EvaluationContext GetEvaluationContext (int frameIndex, int timeout)
		{
			if (timeout == -1)
				timeout = DefaultEvaluationTimeout;
			MDB.StackFrame frame = frames [frameIndex];
			return new SoftEvaluationContext (session, frame, timeout);
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
		
		public ObjectValue[] GetAllLocals (int frameIndex, int timeout)
		{
			List<ObjectValue> locals = new List<ObjectValue> ();

			ObjectValue thisObj = GetThisReference (frameIndex, timeout);
			if (thisObj != null)
				locals.Add (thisObj);

			locals.AddRange (GetLocalVariables (frameIndex, timeout));
			locals.AddRange (GetParameters (frameIndex, timeout));

			return locals.ToArray ();
		}
		
		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods, int timeout)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			return ctx.Adapter.GetExpressionValuesAsync (ctx, expressions, evaluateMethods, timeout);
		}
		
		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, 400);
			return ctx.Adapter.GetExpressionCompletionData (ctx, exp);
		}
		
		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			throw new System.NotImplementedException();
		}
		
		public int FrameCount {
			get {
				return frames.Length;
			}
		}
	}
}
