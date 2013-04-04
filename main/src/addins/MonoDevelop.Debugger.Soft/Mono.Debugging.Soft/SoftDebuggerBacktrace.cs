// 
// SoftDebuggerBacktrace.cs
//  
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using MDB = Mono.Debugger.Soft;
using DC = Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.Soft
{
	internal class SoftDebuggerStackFrame : Mono.Debugging.Client.StackFrame {
		public Mono.Debugger.Soft.StackFrame StackFrame {
			get; private set;
		}
		
		public SoftDebuggerStackFrame (Mono.Debugger.Soft.StackFrame frame, string addressSpace, SourceLocation location, string language, bool isExternalCode, bool hasDebugInfo, bool isDebuggerHidden, string fullModuleName, string fullTypeName)
			: base (frame.ILOffset, addressSpace, location, language, isExternalCode, hasDebugInfo, isDebuggerHidden, fullModuleName, fullTypeName)
		{
			StackFrame = frame;
		}
	}
	
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
				list.Add (CreateStackFrame (frames[n], n));

			return list.ToArray ();
		}
		
		public override int FrameCount {
			get {
				ValidateStack ();
				return frames.Length;
			}
		}
		
		DC.StackFrame CreateStackFrame (MDB.StackFrame frame, int frameIndex)
		{
			MDB.MethodMirror method = frame.Method;
			MDB.TypeMirror type = method.DeclaringType;
			string fileName = frame.FileName;
			string typeFullName = null;
			string typeFQN = null;
			string methodName;
			
			if (fileName != null)
				fileName = SoftDebuggerSession.NormalizePath (fileName);
			
			if (method.VirtualMachine.Version.AtLeast (2, 12) && method.IsGenericMethod) {
				StringBuilder name = new StringBuilder (method.Name);
				
				name.Append ('<');
				
				if (method.VirtualMachine.Version.AtLeast (2, 15)) {
					bool first = true;
					
					foreach (var argumentType in method.GetGenericArguments ()) {
						if (!first)
							name.Append (", ");
						
						name.Append (session.Adaptor.GetDisplayTypeName (argumentType.FullName));
						first = false;
					}
				}
				
				name.Append ('>');
				
				methodName = name.ToString ();
			} else {
				methodName = method.Name;
			}
			
			// Compiler generated anonymous/lambda methods
			bool special_method = false;
			if (methodName [0] == '<' && methodName.Contains (">m__")) {
				int nidx = methodName.IndexOf (">m__", StringComparison.Ordinal) + 2;
				methodName = "AnonymousMethod" + methodName.Substring (nidx, method.Name.Length - nidx);
				special_method = true;
			}
			
			if (type != null) {
				string typeDisplayName = session.Adaptor.GetDisplayTypeName (type.FullName);
				
				if (SoftDebuggerAdaptor.IsGeneratedType (type)) {
					// The user-friendly method name is embedded in the generated type name
					var mn = SoftDebuggerAdaptor.GetNameFromGeneratedType (type);
					
					// Strip off the generated type name
					int dot = typeDisplayName.LastIndexOf ('.');
					var tname = typeDisplayName.Substring (0, dot);

					// Keep any type arguments
					int targs = typeDisplayName.LastIndexOf ('<');
					if (targs > dot + 1)
						mn += typeDisplayName.Substring (targs, typeDisplayName.Length - targs);

					typeDisplayName = tname;
					
					if (special_method)
						typeDisplayName += "." + mn;
					else
						methodName = mn;
				}
				
				methodName = typeDisplayName + "." + methodName;
				
				typeFQN = type.Module.FullyQualifiedName;
				typeFullName = type.FullName;
			}

			bool hidden = false;
			if (session.VirtualMachine.Version.AtLeast (2, 21)) {
				var ctx = GetEvaluationContext (frameIndex, session.EvaluationOptions);
				var hiddenAttr = session.Adaptor.GetType (ctx, "System.Diagnostics.DebuggerHiddenAttribute") as MDB.TypeMirror;
			
				hidden = method.GetCustomAttributes (hiddenAttr, true).Any ();
			}

			var location = new DC.SourceLocation (methodName, fileName, frame.LineNumber, frame.ColumnNumber);
			var external = session.IsExternalCode (frame);
			string addressSpace = string.Empty;
			string language;

			if (frame.Method != null) {
				if (frame.IsNativeTransition) {
					language = "Transition";
				} else {
					addressSpace = method.FullName;
					language = "Managed";
				}
			} else {
				language = "Native";
			}

			return new SoftDebuggerStackFrame (frame, addressSpace, location, language, external, true, hidden, typeFQN, typeFullName);
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
