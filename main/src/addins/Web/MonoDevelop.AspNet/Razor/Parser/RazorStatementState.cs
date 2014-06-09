//
// RazorStatementState.cs
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
using MonoDevelop.Xml.Parser;
using System.Diagnostics;
using MonoDevelop.AspNet.Razor.Parser;
using MonoDevelop.AspNet.Razor.Dom;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public class RazorStatementState : RazorCodeFragmentState
	{
		protected const int POSSIBLE_CONTINUATION = MAXCONST + 1;
		protected const int ELSE = POSSIBLE_CONTINUATION + 1;

		IEnumerable<String> keys;
		RazorStatementState statementState;

		public RazorStatement CorrespondingStatement {
			get { return CorrespondingBlock as RazorStatement; }
		}

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 1) {
				bracketsBuilder.Clear ();
				var stm = context.Nodes.FirstOrDefault (n => n is RazorStatement);
				if (stm == null) {
					if (context.PreviousState is XmlClosingTagState && CorrespondingStatement != null)
						context.Nodes.Push (CorrespondingStatement);
					else {
						Debug.Fail ("Statement should be pushed before changing the state to StatementState");
						return Parent;
					}
				} else
					CorrespondingBlock = stm as RazorCodeFragment;
			}

			if (context.StateTag == POSSIBLE_CONTINUATION) {
				if (!Char.IsWhiteSpace (c))
					context.KeywordBuilder.Append (c);

				string currentKey = context.KeywordBuilder.ToString ();

				if (!keys.Any (s => s.StartsWith (currentKey))) {
					rollback = String.Empty;
					var p = Parent;
					while (!(p is RazorSpeculativeState))
						p = p.Parent;
					return p.Parent;
				}

				if (keys.Any (s => s == currentKey)) {
					if (currentKey != "else")
						return SwitchToContinuationStatement (context, currentKey);
					else
						context.StateTag = ELSE;
				}

				return null;
			}

			if (context.StateTag == ELSE) {
				if (c != ' ') {
					string currentKey = context.KeywordBuilder.ToString ();
					if (c == 'i' && currentKey.Trim () == "else")
						currentKey = "else if";
					return SwitchToContinuationStatement (context, currentKey);
				}

				context.KeywordBuilder.Append (c);
				return null;
			}

			switch (c) {
			case '{':
				if (context.StateTag != TRANSITION)
					return ParseOpeningBracket (c, context);
				break;

			case '}':
				return ParseClosingBracket<RazorStatement> (c, context, Parent.Parent);
			}

			return base.PushChar (c, context, ref rollback);
		}

		XmlParserState SwitchToContinuationStatement (IXmlParserContext context, string key)
		{
			string name = key.Trim ();
			int length = key.Length;
			if (name == "else if")
				length = key.Length - 1;
			else if (name == "else")
				length = key.Length + 1;
			var stm = new RazorStatement (context.LocationMinus (length)) { Name = name };
			context.Nodes.Push (stm);
			return EnsureSetAndAdopted<RazorStatementState> (ref statementState);
		}

		public override XmlParserState ParseClosingBracket<T> (char c, IXmlParserContext context, XmlParserState stateToReturn)
		{
			if (base.ParseClosingBracket<T> (c, context, stateToReturn) != null) {
				if (RazorSymbols.CanBeContinued (CorrespondingBlock.Name)) {
					keys = RazorSymbols.PossibleKeywordsAfter (CorrespondingBlock.Name);
					context.StateTag = POSSIBLE_CONTINUATION;
				} else
					return stateToReturn;
			}
			return null;
		}
	}
}
