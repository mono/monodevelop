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
using Mono.Debugger.Soft;
using DC = Mono.Debugging.Client;

namespace Mono.Debugging.Soft
{
	public class SoftEvaluationContext: EvaluationContext
	{
		SoftDebuggerSession session;
		int stackVersion;
		StackFrame frame;
		bool sourceAvailable;
		
		public ThreadMirror Thread { get; set; }
		
		public SoftEvaluationContext (SoftDebuggerSession session, StackFrame frame, DC.EvaluationOptions options): base (options)
		{
			Frame = frame;
			Thread = frame.Thread;

			string method = frame.Method.Name;
			if (frame.Method.DeclaringType != null)
				method = frame.Method.DeclaringType.FullName + "." + method;
			var location = new DC.SourceLocation (method, frame.FileName, frame.LineNumber);
			var lang = frame.Method != null? "Managed" : "Native";
			
			Evaluator = session.GetEvaluator (new DC.StackFrame (frame.ILOffset, location, lang, session.IsExternalCode (frame), true));
			Adapter = session.Adaptor;
			this.session = session;
			this.stackVersion = session.StackVersion;
			sourceAvailable = !string.IsNullOrEmpty (frame.FileName) && System.IO.File.Exists (frame.FileName);
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
		
		public bool SourceCodeAvailable {
			get {
				if (stackVersion != session.StackVersion)
					sourceAvailable = !string.IsNullOrEmpty (Frame.FileName) && System.IO.File.Exists (Frame.FileName);
				return sourceAvailable;
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
			frame = other.frame;
			stackVersion = other.stackVersion;
			Thread = other.Thread;
			session = other.session;
		}

		static bool IsValueTypeOrPrimitive (TypeMirror type)
		{
			return type != null && (type.IsValueType || type.IsPrimitive);
		}

		static bool IsValueTypeOrPrimitive (Type type)
		{
			return type != null && (type.IsValueType || type.IsPrimitive);
		}
		
		public Value RuntimeInvoke (MethodMirror method, object target, Value[] values)
		{
			if (values != null) {
				// Some arguments may need to be boxed
				ParameterInfoMirror[] mparams = method.GetParameters ();
				if (mparams.Length != values.Length)
					throw new EvaluatorException ("Invalid number of arguments when calling: " + method.Name);
				
				for (int n = 0; n < mparams.Length; n++) {
					TypeMirror tm = mparams [n].ParameterType;
					if (tm.IsValueType || tm.IsPrimitive)
						continue;

					object type = Adapter.GetValueType (this, values [n]);
					TypeMirror argTypeMirror = type as TypeMirror;
					Type argType = type as Type;

					if (IsValueTypeOrPrimitive (argTypeMirror) || IsValueTypeOrPrimitive (argType)) {
						// A value type being assigned to a parameter which is not a value type. The value has to be boxed.
						try {
							values [n] = Thread.Domain.CreateBoxedValue (values [n]);
						} catch (NotSupportedException) {
							// This runtime doesn't support creating boxed values
							throw new EvaluatorException ("This runtime does not support creating boxed values.");
						}
					}
				}
			}

			if (!method.IsStatic && method.DeclaringType.IsClass && !IsValueTypeOrPrimitive (method.DeclaringType)) {
				object type = Adapter.GetValueType (this, target);
				TypeMirror targetTypeMirror = type as TypeMirror;
				Type targetType = type as Type;

				if ((target is StructMirror && ((StructMirror) target).Type != method.DeclaringType) ||
				    (IsValueTypeOrPrimitive (targetTypeMirror) || IsValueTypeOrPrimitive (targetType))) {
					// A value type being assigned to a parameter which is not a value type. The value has to be boxed.
					try {
						target = Thread.Domain.CreateBoxedValue ((Value) target);
					} catch (NotSupportedException) {
						// This runtime doesn't support creating boxed values
						throw new EvaluatorException ("This runtime does not support creating boxed values.");
					}
				}
			}
			
			MethodCall mc = new MethodCall (this, method, target, values);
			Adapter.AsyncExecute (mc, Options.EvaluationTimeout);
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
