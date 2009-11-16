// 
// SoftEvaluationContext.cs
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
using Mono.Debugging.Evaluation;
using Mono.Debugger;

namespace MonoDevelop.Debugger.Soft
{
	public class SoftEvaluationContext: EvaluationContext
	{
		SoftDebuggerSession session;
		int stackVersion;
		StackFrame frame;
		
		public ThreadMirror Thread { get; set; }
		
		public SoftEvaluationContext (SoftDebuggerSession session, StackFrame frame, int tiemout)
		{
			Frame = frame;
			Thread = frame.Thread;
			Evaluator = session.Evaluator;
			Adapter = session.Adaptor;
			this.session = session;
			Timeout = tiemout;
			this.stackVersion = session.StackVersion;
		}
		
		public StackFrame Frame {
			get {
				if (stackVersion != session.StackVersion)
					UpdateFrame ();
				return frame;
			}
			set {
				frame = value;
			}
		}
		
		public SoftDebuggerSession Session {
			get { return session; }
		}
		
		public override void WriteDebuggerError (Exception ex)
		{
			session.WriteDebuggerOutput (true, ex.ToString ());
		}
		
		public override void WriteDebuggerOutput (string message, params object[] values)
		{
			session.WriteDebuggerOutput (false, string.Format (message, values));
		}

		public override void CopyFrom (EvaluationContext ctx)
		{
			base.CopyFrom (ctx);
			SoftEvaluationContext other = (SoftEvaluationContext) ctx;
			Frame = other.Frame;
			Thread = other.Thread;
		}
		
		public Value RuntimeInvoke (MethodMirror method, object target, Value[] values)
		{
			session.StackVersion++;
			MethodCall mc = new MethodCall (this, method, target, values);
			Adapter.AsyncExecute (mc, Timeout);
			return mc.ReturnValue;
		}
		
		void UpdateFrame ()
		{
			stackVersion = session.StackVersion;
			foreach (StackFrame f in Thread.GetFrames ()) {
				if (f.FileName == Frame.FileName && f.LineNumber == Frame.LineNumber && f.ILOffset == Frame.ILOffset) {
					Frame = f;
					break;
				}
			}
		}
	}
}
