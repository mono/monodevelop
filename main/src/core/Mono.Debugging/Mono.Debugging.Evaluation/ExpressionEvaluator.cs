// IExpressionEvaluator.cs
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
using System.Text;

namespace Mono.Debugging.Evaluation
{
	public class ExpressionEvaluator<TValue, TType>
		where TValue: class
		where TType: class
	{
		public virtual ValueReference<TValue, TType> Evaluate (EvaluationContext<TValue, TType> ctx, string exp, EvaluationOptions<TType> options)
		{
			foreach (ValueReference<TValue, TType> var in ctx.Adapter.GetLocalVariables (ctx))
				if (var.Name == exp)
					return var;

			foreach (ValueReference<TValue, TType> var in ctx.Adapter.GetParameters (ctx))
				if (var.Name == exp)
					return var;

			ValueReference<TValue, TType> thisVar = ctx.Adapter.GetThisReference (ctx);
			if (thisVar != null) {
				if (thisVar.Name == exp)
					return thisVar;
				foreach (ValueReference<TValue, TType> cv in thisVar.GetChildReferences ())
					if (cv.Name == exp)
						return cv;
			}
			throw new EvaluatorException ("Invalid Expression: '{0}'", exp);
		}

		public string TargetObjectToString (EvaluationContext<TValue, TType> ctx, TValue obj)
		{
			object res = ctx.Adapter.TargetObjectToObject (ctx, obj);
			if (res == null)
				return null;
			else
				return res.ToString ();
		}

		public string TargetObjectToExpression (EvaluationContext<TValue, TType> ctx, TValue obj)
		{
			return ToExpression (ctx.Adapter.TargetObjectToObject (ctx, obj));
		}
		
		public virtual string ToExpression (object obj)
		{
			if (obj == null)
				return "null";
			else if (obj is IntPtr) {
				IntPtr p = (IntPtr) obj;
				return "0x" + p.ToInt64 ().ToString ("x");
			} else if (obj is char)
				return "'" + obj + "'";
			else if (obj is string)
				return "\"" + EscapeString ((string)obj) + "\"";
			
			return obj.ToString ();
		}

		public static string EscapeString (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				string txt;
				switch (c) {
					case '\\': txt = @"\\"; break;
					case '\a': txt = @"\a"; break;
					case '\b': txt = @"\b"; break;
					case '\f': txt = @"\f"; break;
					case '\v': txt = @"\v"; break;
					case '\n': txt = @"\n"; break;
					case '\r': txt = @"\r"; break;
					case '\t': txt = @"\t"; break;
					default:
						sb.Append (c);
						continue;
				}
				sb.Append (txt);
			}
			return sb.ToString ();
		}
	}
	
	public class LiteralExp
	{
		public readonly string Exp;
		
		public LiteralExp (string exp)
		{
			Exp = exp;
		}
		
		public override string ToString ()
		{
			return Exp;
		}

	}
	
	public class EvaluationOptions<TType>
	{
		bool canEvaluateMethods;

		public TType ExpectedType { get; set; }
		
		public bool CanEvaluateMethods {
			get { return canEvaluateMethods; }
			set { canEvaluateMethods = value; }
		}
	}
	
	public class EvaluatorException: Exception
	{
		public EvaluatorException (string msg, params object[] args): base (string.Format (msg, args))
		{
		}
	}

	public class NotSupportedExpressionException: EvaluatorException
	{
		public NotSupportedExpressionException ( )
			: base ("Expression not supported.")
		{
		}
	}
}
