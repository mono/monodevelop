// EvaluationContext.cs
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

namespace Mono.Debugging.Evaluation
{
	public class EvaluationContext<TValue, TType>
		where TValue: class
		where TType: class
	{
		public ExpressionEvaluator<TValue, TType> Evaluator { get; set; }
		public ObjectValueAdaptor<TValue, TType> Adapter { get; set; }

		public virtual void WriteDebuggerError (Exception ex)
		{
		}

		public virtual void WriteDebuggerOutput (string message, params object[] values)
		{
		}

		public void WaitRuntimeInvokes ( )
		{
		}
		
		public int Timeout { get; set; }
		
		public EvaluationContext ()
		{
		}

		public EvaluationContext<TValue, TType> Clone ( )
		{
			EvaluationContext<TValue, TType> clone = (EvaluationContext<TValue, TType>) MemberwiseClone ();
			clone.CopyFrom (this);
			return clone;
		}

		public virtual void CopyFrom (EvaluationContext<TValue, TType> ctx)
		{
			Timeout = ctx.Timeout;
			Evaluator = ctx.Evaluator;
			Adapter = ctx.Adapter;
		}
	}
}
