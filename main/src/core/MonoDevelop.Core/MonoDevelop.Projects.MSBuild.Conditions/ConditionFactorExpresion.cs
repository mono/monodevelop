//
// ConditionFactorExpression.cs
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
using System.Collections.Specialized;
using System.Globalization;
using System.Xml;

namespace MonoDevelop.Projects.MSBuild.Conditions {
	internal sealed class ConditionFactorExpression : ConditionExpression {

		static Hashtable trueValues;
		static Hashtable falseValues;
		
		static ConditionFactorExpression ()
		{
			string[] trueValuesArray = new string[] {"true", "on", "yes"};
			string[] falseValuesArray = new string[] {"false", "off", "no"};
			

			trueValues = CollectionsUtil.CreateCaseInsensitiveHashtable ();
			falseValues = CollectionsUtil.CreateCaseInsensitiveHashtable ();
			
			foreach (string s in trueValuesArray) {
				trueValues.Add (s, s);
			}
			
			foreach (string s in falseValuesArray) {
				falseValues.Add (s, s);
			}
		}

		readonly Token token;
		public ConditionFactorExpression (Token token)
		{
			this.token = token;
		}

		public override bool TryEvaluateToBool (IExpressionContext context, out bool result)
		{
			result = false;
			if (token.Type != TokenType.String) {
				return false;
			}

			bool canEvaluate = TryEvaluateToString (context, out string evaluatedToken);
			if (!canEvaluate)
				return false;

			if (trueValues [evaluatedToken] != null)
				result = true;
			else if (falseValues [evaluatedToken] != null)
				result = false;
			else
				return false;
			return true;
		}
		
		public override bool TryEvaluateToNumber (IExpressionContext context, out float result)
		{
			result = 0;
			if (token.Type != TokenType.Number && token.Type != TokenType.String)
				return false;

			// Use same styles used by Single.TryParse by default when culture not specified.
			const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;
			return TryEvaluateToString (context, out string evaluatedString) &&
				Single.TryParse (evaluatedString, styles, CultureInfo.InvariantCulture, out result);
		}
		
		public override bool TryEvaluateToString (IExpressionContext context, out string result)
		{
			result = context.EvaluateString (token.Value);
			return true;
		}

		public override bool TryEvaluateToVersion (IExpressionContext context, out Version result)
		{
			result = null;
			return TryEvaluateToString (context, out string text) && Version.TryParse (text, out result);
		}

		internal Token Token => token;
	}
}
