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

namespace MonoDevelop.Projects.MSBuild.Conditions
{
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
		
		public override bool TryEvaluateToBool (IExpressionContext context, out bool result)
		{
			if (left.TryEvaluateToBool (context, out bool l) && right.TryEvaluateToBool (context, out bool r)) {
				result = BoolCompare (l, r, op);
				return true;
			}

			if (left.TryEvaluateToVersion (context, out Version vl)) {
				if (right.TryEvaluateToVersion (context, out Version vr)) {
					result = VersionCompare (vl, vr, op);
					return true;
				}
				else if (right.TryEvaluateToNumber (context, out float fr)) {
					result = VersionCompare (vl, fr, op);
					return true;
				}
			}
			else if (left.TryEvaluateToNumber (context, out float fl)) {
				if (right.TryEvaluateToNumber (context, out float fr)) {
					result = NumberCompare (fl, fr, op);
					return true;
				}
				else if (right.TryEvaluateToVersion (context, out Version vr)) {
					result = VersionCompare (fl, vr, op);
					return true;
				}
			}

			if (!left.TryEvaluateToString (context, out string ls) || !right.TryEvaluateToString (context, out string rs)) {
				result = false;
				return false;
			}

			result = StringCompare (ls, rs, op);
			return true;
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
			default:
				throw new NotSupportedException (String.Format ("Relational operator {0} is not supported.", op));
			}
		}

		// see https://github.com/Microsoft/msbuild/blob/03d1435c95e6a85fbf949f94958e743bc44c4186/src/Build/Evaluation/Conditionals/NumericComparisonExpressionNode.cs#L42
		static bool VersionCompare (Version l,
					   Version r,
					   RelationOperator op)
		{
			switch (op) {
			case RelationOperator.Equal:
				return l == r;
			case RelationOperator.NotEqual:
				return l != r;
			case RelationOperator.Less:
				return l < r;
			case RelationOperator.Greater:
				return l > r;
			case RelationOperator.LessOrEqual:
				return l <= r;
			case RelationOperator.GreaterOrEqual:
				return l >= r;
			default:
				throw new NotSupportedException ($"Relational operator {op} is not supported.");
			}
		}

		static bool VersionCompare (Version l,
					   float r,
					   RelationOperator op)
		{
			switch (op) {
			case RelationOperator.Equal:
				return l.Major == r && l.Minor == 0 && l.Build == 0 && l.Revision == 0;
			case RelationOperator.NotEqual:
				return l.Major != r || l.Minor != 0 && l.Build != 0 || l.Revision != 0;
			case RelationOperator.Less:
				return l.Major != r ? l.Major < r : false;
			case RelationOperator.Greater:
				return l.Major != r ? l.Major > r : true;
			case RelationOperator.LessOrEqual:
				return l.Major != r ? l.Major <= r : false;
			case RelationOperator.GreaterOrEqual:
				return l.Major != r ? l.Major >= r : true;
			default:
				throw new NotSupportedException ($"Relational operator {op} is not supported.");
			}
		}

		static bool VersionCompare (float l,
					   Version r,
					   RelationOperator op)
		{
			switch (op) {
			case RelationOperator.Equal:
				return r.Major == l && r.Minor == 0 && r.Build == 0 && r.Revision == 0;
			case RelationOperator.NotEqual:
				return r.Major != l || r.Minor != 0 && r.Build != 0 || r.Revision != 0;
			case RelationOperator.Less:
				return r.Major != l ? l < r.Major : true;
			case RelationOperator.Greater:
				return r.Major != l ? l > r.Major : false;
			case RelationOperator.LessOrEqual:
				return r.Major != l ? l <= r.Major : true;
			case RelationOperator.GreaterOrEqual:
				return r.Major != l ? l >= r.Major : false;
			default:
				throw new NotSupportedException ($"Relational operator {op} is not supported.");
			}
		}

        // PERF: Cache this value to prevent recalculation.
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
