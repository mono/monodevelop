//
// RazorExpressionState.cs
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
using System.Linq;
using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Razor.Dom;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public class RazorExpressionState : RazorState
	{
		protected const int NONE_EXPLICIT = 0;
		protected const int NONE_IMPLICIT = 1;
		protected const int INSIDE_BRACKET_EXPLICIT = 2;
		protected const int INSIDE_BRACKET_IMPLICIT = 3;

		static char[] allowedChars = { '.', '_', '[', ']', '"' };

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 1) {
				switch (c) {
				case '(':
					context.StateTag = NONE_EXPLICIT;
					context.Nodes.Push (new RazorExplicitExpression (context.LocationMinus (2)));
					return null;

				default:
					context.StateTag = NONE_IMPLICIT;
					if (!(context.PreviousState is RazorSpeculativeState))
						context.Nodes.Push (new RazorImplicitExpression (context.LocationMinus (2)));
					break;
				}
			}

			switch (c)
			{
				case '(':
					if (context.StateTag == NONE_EXPLICIT)
						context.StateTag = INSIDE_BRACKET_EXPLICIT;
					else if (context.StateTag == NONE_IMPLICIT)
						context.StateTag = INSIDE_BRACKET_IMPLICIT;
					context.KeywordBuilder.Append (c);
					break;

				case ')':
					if (context.StateTag == NONE_EXPLICIT) {
						StateEngineService.EndCodeFragment<RazorExplicitExpression> (context);
						return Parent;
					}
					else if (context.StateTag != NONE_IMPLICIT) {
						if (context.KeywordBuilder.Length > 0)
							context.KeywordBuilder.Remove (0, 1);
						if (context.KeywordBuilder.Length == 0)
							context.StateTag = (context.StateTag == INSIDE_BRACKET_IMPLICIT ? NONE_IMPLICIT : NONE_EXPLICIT);
					}
					break;

				default:
					// Space between function's name and open bracket not allowed in razor, e.g. @Html.Raw (foo) 
					if (!(Char.IsLetterOrDigit (c) || allowedChars.Any (ch => ch == c))
							&& context.StateTag == NONE_IMPLICIT) {
						StateEngineService.EndCodeFragment<RazorImplicitExpression> (context, 1);
						rollback = String.Empty;
						if (Parent is RazorSpeculativeState)
							return Parent.Parent;
						return Parent;
					}
					break;
			}

			return null;
		}
	}
}
