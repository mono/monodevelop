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
	
		static Hashtable allValues;
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
		
		public override float NumberEvaluate (IExpressionContext context)
		{
			string evaluatedString = StringEvaluate (context);
			return Single.Parse (evaluatedString, CultureInfo.InvariantCulture);
		}
		
		public override string StringEvaluate (IExpressionContext context)
		{
			var evaluated = context.EvaluateString (token.Value);
			return evaluated;
		}
		
		// FIXME: check if we really can do it
		public override bool TryEvaluateToBool (IExpressionContext context, out bool result)
		{
			string evaluatedToken = StringEvaluate (context);
		
			if (token.Type != TokenType.String) {
				result = false;
				return false;
			}

			if (trueValues [evaluatedToken] != null)
				result = true;
			else if (falseValues [evaluatedToken] != null)
				result = false;
			else
				throw new ExpressionEvaluationException (
						String.Format ("Expression \"{0}\" evaluated to \"{1}\" instead of a boolean value",
								token.Value, evaluatedToken));
			return true;
		}
		
		public override bool CanEvaluateToNumber (IExpressionContext context)
		{
			if (token.Type == TokenType.Number)
				return true;
			else if (token.Type == TokenType.String) {
				var text = StringEvaluate (context);

				// Use same styles used by Single.TryParse by default when culture not specified.
				var styles = NumberStyles.Float | NumberStyles.AllowThousands;
				Single number;
				return Single.TryParse (text, styles, CultureInfo.InvariantCulture, out number);
			}
			else
				return false;
		}
		
		public override bool CanEvaluateToString (IExpressionContext context)
		{
			return true;
		}

		public override bool CanEvaluateToVersion (IExpressionContext context)
		{
			var text = StringEvaluate (context);
			return Version.TryParse (text, out var version);
		}

		public override Version VersionEvaluate (IExpressionContext context)
		{
			var text = StringEvaluate (context);
			Version.TryParse (text, out var version);
			return version;
		}

		internal Token Token => token;
	}
}
