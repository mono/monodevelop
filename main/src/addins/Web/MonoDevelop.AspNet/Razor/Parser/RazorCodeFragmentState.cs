//
// RazorCodeFragmentState.cs
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
using System.Text;
using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Html.Parser;
using MonoDevelop.AspNet.Razor.Dom;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public abstract class RazorCodeFragmentState : RazorState
	{
		protected const int NONE = 0;
		protected const int BRACKET = 1;
		protected const int SOLIDUS = 2;
		protected const int INSIDE_PARENTHESES = 3;
		protected const int TRANSITION = 4;
		protected const int MAXCONST = TRANSITION;

		char previousChar;
		protected StringBuilder bracketsBuilder;

		HtmlTagState htmlTagState;
		HtmlClosingTagState htmlClosingTagState;
		RazorCommentState razorCommentState;
		RazorSpeculativeState speculativeState;
		RazorExpressionState expressionState;
		RazorCodeBlockState childBlockState;

		public RazorCodeFragment CorrespondingBlock { get; set; }
		public bool IsInsideParentheses { get; protected set; }
		public bool IsInsideGenerics { get; protected set; }

		public RazorCodeFragmentState ()
			: this (new HtmlTagState (), new HtmlClosingTagState (true),
			new RazorCommentState (), new RazorExpressionState (), new RazorSpeculativeState ())
		{
		}

		public RazorCodeFragmentState (HtmlTagState html, HtmlClosingTagState htmlClosing,
			RazorCommentState comment, RazorExpressionState expression, RazorSpeculativeState speculative)
		{
			htmlTagState = html;
			htmlClosingTagState = htmlClosing;
			razorCommentState = comment;
			expressionState = expression;
			speculativeState = speculative;

			Adopt (htmlTagState);
			Adopt (htmlClosingTagState);
			Adopt (razorCommentState);
			Adopt (expressionState);
			Adopt (speculativeState);

			bracketsBuilder = new StringBuilder ();
		}

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			switch (context.StateTag) {
			case SOLIDUS:
				if (Char.IsLetter (c)) {
					rollback = String.Empty;
					return htmlClosingTagState;
				}
				context.StateTag = NONE;
				break;

			case BRACKET:
				if (Char.IsLetter (c)) {
					rollback = String.Empty;
					return htmlTagState;
				}
				else if (c == '/')
					context.StateTag = SOLIDUS;
				else
					context.StateTag = NONE;
				break;

			case TRANSITION:
				rollback = String.Empty;
				switch (c) {
				case '{':
					return EnsureSetAndAdopted<RazorCodeBlockState> (ref childBlockState);
				case '*':
					return razorCommentState;
				case '(':
					return expressionState;
				default:
					if (context.CurrentStateLength <= 2 || (!Char.IsLetterOrDigit (previousChar) 
							&& (Char.IsLetter (c) || c == '_')))
						return speculativeState;
					else
						context.StateTag = NONE;
					break;
				}

				break;

			case INSIDE_PARENTHESES:
				if (c == '(')
					context.KeywordBuilder.Append (c);
				else if (c == ')') {
					if (context.KeywordBuilder.Length > 0)
						context.KeywordBuilder.Remove (0, 1);
					if (context.KeywordBuilder.Length == 0) {
						context.StateTag = NONE;
						IsInsideParentheses = false;
					}
				}
				break;

			case NONE:
				switch (c) {
				case '(':
					context.KeywordBuilder.Append (c);
					context.StateTag = INSIDE_PARENTHESES;
					IsInsideParentheses = true;
					break;

				case '<':
					if (context.Nodes.Peek () is XElement || !Char.IsLetterOrDigit (previousChar)) {
						context.StateTag = BRACKET;
						IsInsideGenerics = false;
					} else
						IsInsideGenerics = true;
					break;

				case '>':
					IsInsideGenerics = false;
					break;

				case '@':
					context.StateTag = TRANSITION;
					break;

				default:
					previousChar = c;
					break;
				}

				break;
			}

			return null;
		}

		// ParseOpeningBracket and ParseClosingBracket can use simplified method of tracking brackets.
		// It's fast, and works correctly when parsing char by char, but sometimes may incorrectly determine
		// the end of a block in nested subblocks when we click inside the block from another one,
		// because the parent block isn't parsed from end to end then.
		// The simplified version is used mostly for testing. In real environment finding matching brackets
		// precisely is necessary for code completion.

		public virtual XmlParserState ParseOpeningBracket (char c, IXmlParserContext context)
		{
			var rootState = RootState as RazorRootState;
			if (!rootState.UseSimplifiedBracketTracker && !CorrespondingBlock.FirstBracket.HasValue)
				CorrespondingBlock.FindFirstBracket (context.Location);
			else if (rootState.UseSimplifiedBracketTracker)
				bracketsBuilder.Append (c);
			return null;
		}

		public virtual XmlParserState ParseClosingBracket<T> (char c, IXmlParserContext context, XmlParserState stateToReturn) where T : XNode
		{
			bool isEnding = false;
			var rootState = RootState as RazorRootState;
			if (rootState.UseSimplifiedBracketTracker) {
				if (bracketsBuilder.Length > 0)
					bracketsBuilder.Remove (0, 1);
				if (bracketsBuilder.Length == 0)
					isEnding = true;
			}
			else if (!rootState.UseSimplifiedBracketTracker && CorrespondingBlock.IsEndingBracket (context.LocationMinus (1)))
				isEnding = true;

			if (isEnding) {
				StateEngineService.EndCodeFragment<T> (context);
				return stateToReturn;
			}

			return null;
		}
	}
}
