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
using Mono.Debugger;
using Mono.Debugger.Languages;

namespace DebuggerServer
{
	public abstract class ExpressionEvaluator
	{
		public abstract ValueReference Evaluate (StackFrame frame, string exp, EvaluationOptions options);
		
		public virtual string TargetObjectToString (Thread thead, TargetObject obj)
		{
			return TargetObjectToString (thead, obj, true);
		}

		public virtual string TargetObjectToString (Thread thread, TargetObject obj, bool recurseOb)
		{
			try {
				switch (obj.Kind) {
					case Mono.Debugger.Languages.TargetObjectKind.Array:
						TargetArrayObject arr = obj as TargetArrayObject;
						if (arr == null)
							return "null";
						StringBuilder tn = new StringBuilder (arr.Type.ElementType.Name);
						tn.Append ("{[");
						TargetArrayBounds ab = arr.GetArrayBounds (thread);
						if (ab.IsMultiDimensional) {
							for (int n=0; n<ab.Rank; n++) {
								if (n>0)
									tn.Append (',');
								tn.Append (ab.UpperBounds [n] - ab.LowerBounds [n] + 1);
							}
						}
						else if (!ab.IsUnbound) {
							tn.Append (ab.Length.ToString ());
						}
						tn.Append ("]}");
						return tn.ToString ();
						
					case TargetObjectKind.Struct:
					case TargetObjectKind.Class:
						TargetStructObject co = obj as TargetStructObject;
						if (co == null)
							return "null";
						if (recurseOb) {
							TargetObject currob = co.GetCurrentObject (thread);
							if (currob != null)
								return TargetObjectToString (thread, currob, false);
						}
						if (co.TypeName == "System.Decimal")
							return Util.CallToString (thread, co);
						return "{" + co.TypeName + "}";
						
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						return TargetObjectToString (thread, eob.GetValue (thread));
						
					case TargetObjectKind.Fundamental:
						TargetFundamentalObject fob = obj as TargetFundamentalObject;
						if (fob == null)
							return "null";
						object val = fob.GetObject (thread);
						return ToExpression (val);
						
					case TargetObjectKind.Pointer:
						return ToExpression (new IntPtr (obj.GetAddress (thread).Address));
						
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null)
							return "null";
						if (recurseOb)
							return TargetObjectToString (thread, oob.GetDereferencedObject (thread), false);
						else
							return "{" + oob.TypeName + "}";
				}
			}
			catch (Exception ex) {
				return "? (" + ex.GetType () + ": " + ex.Message + ")";
			}
			return "?";
		}
		
		public virtual string ToExpression (object obj)
		{
			if (obj == null)
				return "null";
			
			if (obj is IntPtr) {
				IntPtr p = (IntPtr) obj;
				return "0x" + p.ToInt64 ().ToString ("x");
			}
			
			if (obj is string)
				return "\"" + Util.EscapeString ((string)obj) + "\"";
			
			return obj.ToString ();
		}
	}
	
	public class EvaluationOptions
	{
		bool canEvaluateMethods;
		TargetType expectedType;
		
		public bool CanEvaluateMethods {
			get { return canEvaluateMethods; }
			set { canEvaluateMethods = value; }
		}

		public TargetType ExpectedType {
			get {
				return expectedType;
			}
			set {
				expectedType = value;
			}
		}
	}
}
