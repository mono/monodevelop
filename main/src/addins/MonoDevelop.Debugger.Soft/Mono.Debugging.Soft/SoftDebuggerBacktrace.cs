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
using MDB = Mono.Debugger.Soft;
using DC = Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.Soft
{
	public class SoftDebuggerBacktrace: BaseBacktrace
	{
		MDB.StackFrame[] frames;
		SoftDebuggerSession session;
		MDB.ThreadMirror thread;
		int stackVersion;
		
		public SoftDebuggerBacktrace (SoftDebuggerSession session, MDB.ThreadMirror thread): base (session.Adaptor)
		{
			this.session = session;
			this.thread = thread;
			stackVersion = session.StackVersion;
			if (thread != null)
				this.frames = thread.GetFrames ();
			else
				this.frames = new MDB.StackFrame[0];
		}
		
		void ValidateStack ()
		{
			if (stackVersion != session.StackVersion && thread != null)
				frames = thread.GetFrames ();
		}

		public override DC.StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			ValidateStack ();
			if (lastIndex < 0)
				lastIndex = frames.Length - 1;
			List<DC.StackFrame> list = new List<DC.StackFrame> ();
			for (int n = firstIndex; n <= lastIndex && n < frames.Length; n++)
				list.Add (CreateStackFrame (frames [n]));
			return list.ToArray ();
		}
		
		public override int FrameCount {
			get {
				ValidateStack ();
				return frames.Length;
			}
		}
		
		DC.StackFrame CreateStackFrame (MDB.StackFrame frame)
		{
			MDB.MethodMirror method = frame.Method;
			MDB.TypeMirror type = method.DeclaringType;
			string fileName = frame.FileName;
			string methodName = method.Name;
			string typeFullName = null;
			string typeFQN = null;
			
			if (fileName != null)
				fileName = SoftDebuggerSession.NormalizePath (fileName);
			
			if (type != null) {
				methodName = session.Adaptor.GetDisplayTypeName (type.FullName) + "." + methodName;
				typeFQN = type.Module.FullyQualifiedName;
				typeFullName = type.FullName;
			}
			
			var location = new DC.SourceLocation (methodName, fileName, frame.LineNumber);
			var lang = frame.Method != null ? "Managed" : "Native";
			var external = session.IsExternalCode (frame);
			
			return new DC.StackFrame (frame.ILOffset, method.FullName, location, lang, external, true, typeFQN, typeFullName);
		}
		
		protected override EvaluationContext GetEvaluationContext (int frameIndex, EvaluationOptions options)
		{
			ValidateStack ();
			if (frameIndex >= frames.Length)
				return null;
			
			MDB.StackFrame frame = frames [frameIndex];
			return new SoftEvaluationContext (session, frame, options);
		}
		
		public override AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			return session.Disassemble (frames [frameIndex], firstLine, count);
		}
	}
}
