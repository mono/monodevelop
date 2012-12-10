//
// RazorSymbols.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Linq;
using System.Text;

namespace MonoDevelop.AspNet.Mvc.Parser
{
	public class RazorSymbols
	{
		// Single line directives
		static string[] simpleDirectives = new string[] {
			"inherits",
			"sessionstate",
			"model",
			"layout",
			"using",

			//FIXME: these should be for RazorHostKind.Template only
			"__class",
			"__property",
		};

		// Block directives
		static string[] complexDirectives = new string[] {
			"functions",
			"section",
			"helper",
		};

		static IEnumerable<string> directives = complexDirectives.Concat (simpleDirectives);

		static string[] statements = new string[] {
			"for",
			"foreach",
			"while",
			"switch",
			"lock",
			"if",
			"try",
			"do",
		};

		public static bool IsDirective (string name)
		{
			return directives.Any (d => d == name);
		}

		public static bool IsSimpleDirective (string name)
		{
			return simpleDirectives.Any (d => d == name);
		}

		public static bool IsComplexDirective (string name)
		{
			return !IsSimpleDirective (name);
		}

		public static bool IsStatement (string name)
		{
			return statements.Any (s => s == name);
		}

		public static bool CanBeStatementOrDirective(string name)
		{
			if (statements.Any (s => s.StartsWith (name)))
				return true;
			else
				return directives.Any (d => d.StartsWith (name));
		}

		public static string[] continuedKeywords = new string[] {
			"if", "else", "else if", "try", "catch"
		};

		public static bool CanBeContinued (string name)
		{
			return continuedKeywords.Any (w => w == name);
		}

		public static IEnumerable<string> PossibleKeywordsAfter (string name)
		{
			switch (name) {
			case "if":
			case "else if":
				yield return "else if";
				yield return "else";
				break;
			case "try":
			case "catch":
				yield return "catch";
				yield return "finally";
				break;
			}
		}
	}
}
