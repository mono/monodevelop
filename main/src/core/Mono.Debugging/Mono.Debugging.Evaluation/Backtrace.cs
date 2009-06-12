// 
// Backtrace.cs
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

using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.Evaluation
{
	public abstract class Backtrace<TValue,TType>: RemoteFrameObject, IBacktrace
		where TValue: class
		where TType: class
	{
		protected abstract EvaluationContext<TValue,TType> GetEvaluationContext (int frameIndex, int timeout);
	
		#region IBacktrace Members

		public abstract AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count);

		public abstract int FrameCount { get; }

		public abstract StackFrame[] GetStackFrames (int firstIndex, int lastIndex);

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
			EvaluationContext<TValue,TType> ctx = GetEvaluationContext (frameIndex, timeout);
			return ctx.Adapter.GetExpressionValuesAsync (ctx, expressions, evaluateMethods, timeout);
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, int timeout)
		{
			EvaluationContext<TValue,TType> ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> list = new List<ObjectValue> ();
			foreach (ValueReference<TValue,TType> var in ctx.Adapter.GetLocalVariables (ctx))
				list.Add (var.CreateObjectValue (true));
			return list.ToArray ();
		}

		public ObjectValue[] GetParameters (int frameIndex, int timeout)
		{
			EvaluationContext<TValue,TType> ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> vars = new List<ObjectValue> ();
			foreach (ValueReference<TValue,TType> var in ctx.Adapter.GetParameters (ctx))
				vars.Add (var.CreateObjectValue (true));
			return vars.ToArray ();
		}

		public ObjectValue GetThisReference (int frameIndex, int timeout)
		{
			EvaluationContext<TValue,TType> ctx = GetEvaluationContext (frameIndex, timeout);
			ValueReference<TValue, TType> var = ctx.Adapter.GetThisReference (ctx);
			if (var != null)
				return var.CreateObjectValue ();
			else
				return null;
		}

		public virtual CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			EvaluationContext<TValue,TType> ctx = GetEvaluationContext (frameIndex, 400);
			int i;

			if (exp [exp.Length - 1] == '.') {
				exp = exp.Substring (0, exp.Length - 1);
				i = 0;
				while (i < exp.Length) {
					ValueReference<TValue,TType> vr = null;
					try {
						vr = ctx.Evaluator.Evaluate (ctx, exp.Substring (i), null);
						if (vr != null) {
							CompletionData data = new CompletionData ();
							foreach (ValueReference<TValue,TType> cv in vr.GetChildReferences ())
								data.Items.Add (new CompletionItem (cv.Name, cv.Flags));
							data.ExpressionLenght = 0;
							return data;
						}
					} catch (Exception ex) {
						Console.WriteLine (ex);
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
				
				CompletionData data = new CompletionData ();
				data.ExpressionLenght = partialWord.Length;
				
				// Local variables
				
				foreach (ValueReference<TValue,TType> vc in ctx.Adapter.GetLocalVariables (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Parameters
				
				foreach (ValueReference<TValue,TType> vc in ctx.Adapter.GetParameters (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Members
				
				ValueReference<TValue,TType> thisobj = ctx.Adapter.GetThisReference (ctx);
				
				if (thisobj != null)
					data.Items.Add (new CompletionItem ("this", ObjectValueFlags.Field | ObjectValueFlags.ReadOnly));

				TType type = ctx.Adapter.GetEnclosingType (ctx);
				
				foreach (ValueReference<TValue,TType> vc in ctx.Adapter.GetMembers (ctx, type, thisobj != null ? thisobj.Value : null))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				if (data.Items.Count > 0)
					return data;
			}
			return null;
		}
			
		#endregion
	}
}
