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
using System.Text;
using System.Collections.Generic;

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
			switch (op) {
			case RelationOperator.Equal:
				return l == r;
			case RelationOperator.NotEqual:
				return l != r;
			case RelationOperator.Greater:
				return l > r;
			case RelationOperator.GreaterOrEqual:
				return l >= r;
			case RelationOperator.Less:
				return l < r;
			case RelationOperator.LessOrEqual:
				return l <= r;
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		static bool BoolCompare (bool l,
					 bool r,
					 RelationOperator op)
		{
			switch (op) {
			case RelationOperator.Equal:
				return l == r;
			case RelationOperator.NotEqual:
				return l != r;
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		static bool StringCompare (string l,
					   string r,
					   RelationOperator op)
		{
			switch (op) {
			case RelationOperator.Equal:
				return string.Equals (l, r, StringComparison.OrdinalIgnoreCase);
			case RelationOperator.NotEqual:
				return !string.Equals (l, r, StringComparison.OrdinalIgnoreCase);
			case RelationOperator.GreaterOrEqual:
			case RelationOperator.LessOrEqual:
				if (string.Equals (l, r, StringComparison.OrdinalIgnoreCase))
					return true;
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		List<string> combinedProperty = null;
		List<string> combinedValue = null;
		bool combinedPropertySet;
		object conditionPropertiesLock = new object ();
		public override void CollectConditionProperties (ConditionedPropertyCollection properties)
		{
			lock (conditionPropertiesLock) {
				if (!combinedPropertySet) {
					combinedPropertySet = true;
					if ((op == RelationOperator.Equal || op == RelationOperator.NotEqual) && left is ConditionFactorExpression && right is ConditionFactorExpression) {
						var leftString = ((ConditionFactorExpression)left).Token.Value;
						var rightString = ((ConditionFactorExpression)right).Token.Value;

						int il = 0;
						int rl = 0;
						while (il < leftString.Length && rl < rightString.Length) {
							if (il < leftString.Length - 2 && leftString [il] == '$' && leftString [il + 1] == '(')
								ReadPropertyCondition (leftString, ref combinedProperty, ref combinedValue, ref il, rightString, ref rl);
							else if (rl < rightString.Length - 2 && rightString [rl] == '$' && rightString [rl + 1] == '(')
								ReadPropertyCondition (rightString, ref combinedProperty, ref combinedValue, ref rl, leftString, ref il);
							else if (leftString [il] != rightString [rl])
								return; // Condition can't be true
							il++; rl++;
						}
					}
				}
			}

			// This condition sets values for more that one property. In addition to the individual values, also register
			// the combination of values. So for example if the condition has "$(Configuration)|$(Platform) == Foo|Bar",
			// the conditioned property collection would contain Configuration=Foo, Platform=Bar, (Configuration|Platfrom)=Foo|Bar
			if (combinedProperty != null)
				properties.AddPropertyValues (combinedProperty, combinedValue);
		}

		void ReadPropertyCondition (string propString, ref List<string> combinedProperty, ref List<string> combinedValue, ref int i, string valString, ref int j)
		{ 
			var prop = ReadPropertyTag (propString, ref i);
			string val;
			if (i < propString.Length)
				val = ReadPropertyValue (valString, ref j, propString [i]);
			else
				val = valString.Substring (j);

			if (combinedProperty == null) {
				combinedProperty = new List<string> ();
				combinedValue = new List<string> ();
			}

			combinedProperty.Add (prop);
			combinedValue.Add (val);
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
