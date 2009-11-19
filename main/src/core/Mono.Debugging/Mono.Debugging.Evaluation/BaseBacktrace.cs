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
using System.Collections.Generic;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace Mono.Debugging.Evaluation
{
	public abstract class BaseBacktrace: RemoteFrameObject, IBacktrace
	{
		Dictionary<int, FrameInfo> frameInfo = new Dictionary<int, FrameInfo> ();
		
		public BaseBacktrace (ObjectValueAdaptor adaptor)
		{
			Adaptor = adaptor;
		}
		
		public abstract StackFrame[] GetStackFrames (int firstIndex, int lastIndex);
		
		public ObjectValueAdaptor Adaptor { get; set; }
		
		protected abstract EvaluationContext GetEvaluationContext (int frameIndex, EvaluationOptions options);
		
		public abstract int FrameCount { get; }
	
		public virtual ObjectValue[] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			FrameInfo frame = GetFrameInfo (frameIndex, options, false);
			List<ObjectValue> list = new List<ObjectValue> ();
			
			if (frame == null) {
				ObjectValue val = Adaptor.CreateObjectValueAsync ("Local Variables", ObjectValueFlags.EvaluatingGroup, delegate {
					frame = GetFrameInfo (frameIndex, options, true);
					foreach (ValueReference var in frame.LocalVariables)
						list.Add (var.CreateObjectValue (false, options));
					return ObjectValue.CreateArray (null, new ObjectPath ("Local Variables"), "", list.Count, ObjectValueFlags.EvaluatingGroup, list.ToArray ());
				});
				return new ObjectValue [] { val };
			}
			
			foreach (ValueReference var in frame.LocalVariables)
				list.Add (var.CreateObjectValue (true, options));
			return list.ToArray ();
		}
		
		public virtual ObjectValue[] GetParameters (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> vars = new List<ObjectValue> ();
			
			FrameInfo frame = GetFrameInfo (frameIndex, options, false);
			if (frame == null) {
				ObjectValue val = Adaptor.CreateObjectValueAsync ("Parameters", ObjectValueFlags.EvaluatingGroup, delegate {
					frame = GetFrameInfo (frameIndex, options, true);
					foreach (ValueReference var in frame.Parameters)
						vars.Add (var.CreateObjectValue (false, options));
					return ObjectValue.CreateArray (null, new ObjectPath ("Parameters"), "", vars.Count, ObjectValueFlags.EvaluatingGroup, vars.ToArray ());
				});
				return new ObjectValue [] { val };
			}
			
			foreach (ValueReference var in frame.Parameters)
				vars.Add (var.CreateObjectValue (true, options));
			return vars.ToArray ();
		}
		
		public virtual ObjectValue GetThisReference (int frameIndex, EvaluationOptions options)
		{
			FrameInfo frame = GetFrameInfo (frameIndex, options, false);
			if (frame == null) {
				return Adaptor.CreateObjectValueAsync ("this", ObjectValueFlags.EvaluatingGroup, delegate {
					frame = GetFrameInfo (frameIndex, options, true);
					ObjectValue[] vals;
					if (frame.This != null)
						vals = new ObjectValue[] { frame.This.CreateObjectValue (false, options) };
					else
						vals = new ObjectValue [0];
					return ObjectValue.CreateArray (null, new ObjectPath ("this"), "", vals.Length, ObjectValueFlags.EvaluatingGroup, vals);
				});
			}
			if (frame.This != null)
				return frame.This.CreateObjectValue ();
			else
				return null;
		}
		
		public virtual ObjectValue[] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> locals = new List<ObjectValue> ();
			
			locals.AddRange (GetLocalVariables (frameIndex, options));
			locals.AddRange (GetParameters (frameIndex, options));
			
			locals.Sort (delegate (ObjectValue v1, ObjectValue v2) {
				return v1.Name.CompareTo (v2.Name);
			});

			ObjectValue thisObj = GetThisReference (frameIndex, options);
			if (thisObj != null)
				locals.Insert (0, thisObj);
			
			return locals.ToArray ();
		}
		
		public virtual ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, EvaluationOptions options)
		{
			if (Adaptor.IsEvaluating) {
				List<ObjectValue> vals = new List<ObjectValue> ();
				foreach (string exp in expressions) {
					ObjectValue val = Adaptor.CreateObjectValueAsync (exp, ObjectValueFlags.Field, delegate {
						EvaluationContext cctx = GetEvaluationContext (frameIndex, options);
						return Adaptor.GetExpressionValue (cctx, exp);
					});
					vals.Add (val);
				}
				return vals.ToArray ();
			}
			EvaluationContext ctx = GetEvaluationContext (frameIndex, options);
			return ctx.Adapter.GetExpressionValuesAsync (ctx, expressions);
		}
		
		public virtual CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			EvaluationContext ctx = GetEvaluationContext (frameIndex, EvaluationOptions.DefaultOptions);
			return ctx.Adapter.GetExpressionCompletionData (ctx, exp);
		}
		
		public virtual AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			throw new System.NotImplementedException();
		}
		
		FrameInfo GetFrameInfo (int frameIndex, EvaluationOptions options, bool ignoreEvalStatus)
		{
			FrameInfo finfo;
			if (frameInfo.TryGetValue (frameIndex, out finfo))
				return finfo;
			
			if (!ignoreEvalStatus && Adaptor.IsEvaluating)
				return null;
			
			EvaluationContext ctx = GetEvaluationContext (frameIndex, options);
			finfo = new FrameInfo ();
			finfo.Context = ctx;
			finfo.LocalVariables.AddRange (ctx.Adapter.GetLocalVariables (ctx));
			finfo.Parameters.AddRange (ctx.Adapter.GetParameters (ctx));
			finfo.This = ctx.Adapter.GetThisReference (ctx);
			frameInfo [frameIndex] = finfo;
			return finfo;
		}
	}
	
	class FrameInfo
	{
		public EvaluationContext Context;
		public List<ValueReference> LocalVariables = new List<ValueReference> ();
		public List<ValueReference> Parameters = new List<ValueReference> ();
		public ValueReference This;
	}
}
