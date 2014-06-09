//
// RazorSpeculativeState.cs
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
using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Razor.Parser;
using MonoDevelop.AspNet.Razor.Dom;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public class RazorSpeculativeState : RazorState
	{
		protected const int UNKNOWN = 0;
		protected const int POSSIBLE_DIRECTIVE = 1;
		protected const int POSSIBLE_STATEMENT = 2;
		protected const int USING = 3;

		RazorExpressionState expressionState;
		RazorDirectiveState directiveState;
		RazorStatementState statementState;

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			string key;

			switch (context.StateTag) {
			case UNKNOWN:
				context.KeywordBuilder.Append (c);
				key = context.KeywordBuilder.ToString ();
				if (!RazorSymbols.CanBeStatementOrDirective (key)) {
					context.Nodes.Push (new RazorImplicitExpression (context.LocationMinus (key.Length + 1)));
					rollback = String.Empty;
					return EnsureSetAndAdopted<RazorExpressionState> (ref expressionState);
				}
				if (key == "using")
					context.StateTag = USING;
				else if (RazorSymbols.IsDirective (key))
					context.StateTag = POSSIBLE_DIRECTIVE;
				else if (RazorSymbols.IsStatement (key))
					context.StateTag = POSSIBLE_STATEMENT;

				break;

			// Using can be either statement: @using (resource) {}, or directive: @using System.IO
			case USING:
				if (c == '(' || c == '\n')
					return SwitchToStatement (context, ref rollback);
				else if (Char.IsLetterOrDigit(c))
					return SwitchToDirective (context, ref rollback);

				context.KeywordBuilder.Append (c);
				break;

			case POSSIBLE_STATEMENT:
				if (Char.IsWhiteSpace (c) || c == '{' || c == '(')
					return SwitchToStatement(context, ref rollback);

				context.KeywordBuilder.Append (c);
				context.StateTag = UNKNOWN;
				break;

			case POSSIBLE_DIRECTIVE:
				if (Char.IsWhiteSpace (c) || c == '{')
					return SwitchToDirective (context, ref rollback);

				context.KeywordBuilder.Append (c);
				context.StateTag = UNKNOWN;
				break;
			}

			return null;
		}

		XmlParserState SwitchToDirective (IXmlParserContext context, ref string rollback)
		{
			string key = context.KeywordBuilder.ToString ();
			string name = key.Trim ();
			var dir = new RazorDirective (context.LocationMinus (key.Length + 2)) {
				Name = name,
				IsSimpleDirective = RazorSymbols.IsSimpleDirective (name)
			};
			context.Nodes.Push (dir);
			rollback = String.Empty;
			return EnsureSetAndAdopted<RazorDirectiveState> (ref directiveState);
		}

		XmlParserState SwitchToStatement (IXmlParserContext context, ref string rollback)
		{
			string key = context.KeywordBuilder.ToString ();
			var stm = new RazorStatement (context.LocationMinus (key.Length + 2)) { Name = key.Trim () };
			context.Nodes.Push (stm);
			rollback = String.Empty;
			return EnsureSetAndAdopted<RazorStatementState> (ref statementState);
		}
	}
}
