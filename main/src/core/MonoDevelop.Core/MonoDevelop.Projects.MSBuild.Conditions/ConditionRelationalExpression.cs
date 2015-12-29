//
// ConditionRelationalExpression.cs
//
// Author:
//   Marek Sieradzki (marek.sieradzki@gmail.com)
// 
// (C) 2006 Marek Sieradzki
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Xml;

namespace MonoDevelop.Projects.MSBuild.Conditions {
	internal sealed class ConditionRelationalExpression : ConditionExpression {
	
		readonly ConditionExpression left;
		readonly ConditionExpression right;
		readonly RelationOperator op;
		
		public ConditionRelationalExpression (ConditionExpression left,
						      ConditionExpression right,
						      RelationOperator op)
		{
			this.left = left;
			this.right = right;
			this.op = op;
		}
		
		public override  bool BoolEvaluate (IExpressionContext context)
		{
			if (left.CanEvaluateToNumber (context) && right.CanEvaluateToNumber (context)) {
				float l,r;
				
				l = left.NumberEvaluate (context);
				r = right.NumberEvaluate (context);
				
				return NumberCompare (l, r, op);
			} else if (left.CanEvaluateToBool (context) && right.CanEvaluateToBool (context)) {
				bool l,r;
				
				l = left.BoolEvaluate (context);
				r = right.BoolEvaluate (context);
				
				return BoolCompare (l, r, op);
			} else {
				string l,r;
				
				l = left.StringEvaluate (context);
				r = right.StringEvaluate (context);
				
				return StringCompare (l, r, op);
			}
		}
		
		public override float NumberEvaluate (IExpressionContext context)
		{
			throw new NotSupportedException ();
		}
		
		public override string StringEvaluate (IExpressionContext context)
		{
			throw new NotSupportedException ();
		}
		
		// FIXME: check if we really can do it
		public override bool CanEvaluateToBool (IExpressionContext context)
		{
			return true;
		}
		
		public override bool CanEvaluateToNumber (IExpressionContext context)
		{
			return false;
		}
		
		public override bool CanEvaluateToString (IExpressionContext context)
		{
			return false;
		}
		
		static bool NumberCompare (float l,
					   float r,
					   RelationOperator op)
		{
			IComparer comparer = CaseInsensitiveComparer.DefaultInvariant;
			
			switch (op) {
			case RelationOperator.Equal:
				return comparer.Compare (l, r) == 0;
			case RelationOperator.NotEqual:
				return comparer.Compare (l, r) != 0;
			case RelationOperator.Greater:
				return comparer.Compare (l, r) > 0;
			case RelationOperator.GreaterOrEqual:
				return comparer.Compare (l, r) >= 0;
			case RelationOperator.Less:
				return comparer.Compare (l, r) < 0;
			case RelationOperator.LessOrEqual:
				return comparer.Compare (l, r) <= 0;
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		static bool BoolCompare (bool l,
					 bool r,
					 RelationOperator op)
		{
			IComparer comparer = CaseInsensitiveComparer.DefaultInvariant;
			
			switch (op) {
			case RelationOperator.Equal:
				return comparer.Compare (l, r) == 0;
			case RelationOperator.NotEqual:
				return comparer.Compare (l, r) != 0;
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		static bool StringCompare (string l,
					   string r,
					   RelationOperator op)
		{
			IComparer comparer = CaseInsensitiveComparer.DefaultInvariant;
			
			switch (op) {
			case RelationOperator.Equal:
				return comparer.Compare (l, r) == 0;
			case RelationOperator.NotEqual:
				return comparer.Compare (l, r) != 0;
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		public override void CollectConditionProperties (ConditionedPropertyCollection properties)
		{
			if ((op == RelationOperator.Equal || op == RelationOperator.NotEqual) && left is ConditionFactorExpression && right is ConditionFactorExpression) {
				var leftString = ((ConditionFactorExpression)left).Token.Value;
				var rightString = ((ConditionFactorExpression)right).Token.Value;

				int il = 0;
				int rl = 0;
				while (il < leftString.Length && rl < rightString.Length) {
					if (il < leftString.Length - 2 && leftString [il] == '$' && leftString [il + 1] == '(')
						ReadPropertyCondition (properties, leftString, ref il, rightString, ref rl);
					else if (rl < rightString.Length - 2 && rightString [rl] == '$' && rightString [rl + 1] == '(')
						ReadPropertyCondition (properties, rightString, ref rl, leftString, ref il);
					else if (leftString [il] != rightString [rl])
						return; // Condition can't be true
					il++; rl++;
				}
			}
		}

		void ReadPropertyCondition (ConditionedPropertyCollection properties, string propString, ref int i, string valString, ref int j)
		{ 
			var prop = ReadPropertyTag (propString, ref i);
			string val;
			if (i < propString.Length)
				val = ReadPropertyValue (valString, ref j, propString [i]);
			else
				val = valString.Substring (j);

			properties.AddProperty (prop, val);
		}

		string ReadPropertyValue (string valString, ref int j, char v)
		{
			var s = j;
			while (j < valString.Length && valString [j] != v)
				j++;
			return valString.Substring (s, j - s);
		}

		string ReadPropertyTag (string propString, ref int i)
		{
			i += 2;
			var s = i;
			while (i < propString.Length && propString [i] != ')')
				i++;
			if (i < propString.Length)
				return propString.Substring (s, (i++) - s);
			return propString.Substring (s);
		}
	}
	
	internal enum RelationOperator {
		Equal,
		NotEqual,
		Less,
		Greater,
		LessOrEqual,
		GreaterOrEqual
	}
}
