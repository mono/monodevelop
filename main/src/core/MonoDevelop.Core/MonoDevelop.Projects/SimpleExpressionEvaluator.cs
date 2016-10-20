//
// SimpleExpressionEvaluator.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	class SimpleExpressionEvaluator
	{
		// Grammar:
		// exp <- orExp
		// orExp <- andExp ('|' andExp)*
		// andExp <- notExp (('&'|'+') notExp)*
		// notExp <- ('!') valueExp
		// valueExp <- symbol | '(' orExp ')'

		public static bool Evaluate (string expression, IList<string> symbols)
		{
			int n = 0;
			if (string.IsNullOrWhiteSpace (expression))
				return true;
			return EvaluateOr (expression, ref n, symbols);
		}

		static bool EvaluateOr (string expression, ref int n, IList<string> symbols)
		{
			while (true) {
				if (EvaluateAnd (expression, ref n, symbols))
					return true;
				if (!SkipWhitespace (expression, ref n))
					return false;
				if (expression [n] != '|')
					return false; // False so far
				n++;
			}
		}

		static bool EvaluateAnd (string expression, ref int n, IList<string> symbols)
		{
			while (true) {
				if (!EvaluateValue (expression, ref n, symbols))
					return false;
				if (!SkipWhitespace (expression, ref n))
					return true;
				var c = expression [n];
				if (c != '&' && c != '+')
					return true; // True so far and &/+ is optional
				n++;
			}
		}

		static bool EvaluateNot (string expression, ref int n, IList<string> symbols)
		{
			if (!SkipWhitespace (expression, ref n))
				return false;

			if (expression [n] == '!') {
				n++;
				return !EvaluateValue (expression, ref n, symbols);
			}
			else
				return EvaluateValue (expression, ref n, symbols);
		}

		static bool EvaluateValue (string expression, ref int n, IList<string> symbols)
		{
			if (!SkipWhitespace (expression, ref n))
				return false;
			if (expression [n] == '(') {
				n++;
				if (!EvaluateOr (expression, ref n, symbols))
					return false;
				if (!SkipWhitespace (expression, ref n))
					return false;
				if (expression [n] != ')')
					return false;
				n++;
				return true;
			} else
				return EvaluateSymbol (expression, ref n, symbols);
		}

		static bool EvaluateSymbol (string expression, ref int n, IList<string> symbols)
		{
			if (!SkipWhitespace (expression, ref n))
				return false;
			var sn = n;
			while (n < expression.Length) {
				var c = expression [n];
				if ("\"'`:;,+-*/\\!~|&%$@^()={}[]<>? \t\b\n\r".IndexOf (c) != -1)
					break;
				n++;
			}
			return n > sn && symbols.Contains (expression.Substring (sn, n - sn));
		}

		static bool SkipWhitespace (string expression, ref int n)
		{
			while (n < expression.Length && char.IsWhiteSpace (expression, n))
				n++;
			return n < expression.Length;
		}			       
	}
}
