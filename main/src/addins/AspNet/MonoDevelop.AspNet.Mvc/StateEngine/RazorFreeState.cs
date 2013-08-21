//
// RazorFreeState.cs
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
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;

namespace MonoDevelop.AspNet.Mvc.StateEngine
{
	public class RazorFreeState : XmlFreeState
	{
		protected const int TRANSITION = MAXCONST + 1;

		public RazorCodeBlockState CodeBlockState { get; private set; }
		public RazorExpressionState ExpressionState { get; private set; }
		public RazorCommentState ServerCommentState { get; private set; }
		public RazorSpeculativeState SpeculativeState { get; private set; }

		public bool UseSimplifiedBracketTracker { get; set; }
		char previousChar;

		public RazorFreeState ()
			: this (
				new HtmlTagState (true),
				new HtmlClosingTagState (true),
				new XmlCommentState (),
				new XmlCDataState (),
				new XmlDocTypeState (),
				new XmlProcessingInstructionState (),
				new RazorCodeBlockState (),
				new RazorExpressionState (),
				new RazorCommentState (),
				new RazorSpeculativeState ()
				) { }

		public RazorFreeState (
			HtmlTagState tagState,
			HtmlClosingTagState closingTagState,
			XmlCommentState commentState,
			XmlCDataState cDataState,
			XmlDocTypeState docTypeState,
			XmlProcessingInstructionState processingInstructionState,
			RazorCodeBlockState codeBlockState,
			RazorExpressionState expressionState,
			RazorCommentState razorCommentState,
			RazorSpeculativeState speculativeState
			)
			: base (tagState, closingTagState, commentState, cDataState, docTypeState, processingInstructionState)
		{
			CodeBlockState = codeBlockState;
			ExpressionState = expressionState;
			ServerCommentState = razorCommentState;
			SpeculativeState = speculativeState;

			Adopt (CodeBlockState);
			Adopt (ExpressionState);
			Adopt (ServerCommentState);
			Adopt (SpeculativeState);

			UseSimplifiedBracketTracker = false;
		}

		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			if (c == '@' && context.StateTag == FREE) {
				context.StateTag = TRANSITION;
				return null;
			} else if (context.StateTag == TRANSITION) {
				rollback = String.Empty;
				switch (c) {
				case '{': // Code block @{
					return CodeBlockState;
				case '*': // Comment @*
					return ServerCommentState;
				case '(': // Explicit expression @(
					return ExpressionState;
				default:
					// If char preceding @ was a letter or a digit, don't switch to expression, e.g. foo@bar.com
					if (context.CurrentStateLength <= 2 || (!Char.IsLetterOrDigit (previousChar)
							&& (Char.IsLetter (c) || c == '_'))) // Statement, directive or implicit expression
						return SpeculativeState;
					else
						context.StateTag = FREE;
					break;
				}
			}

			previousChar = c;
			return base.PushChar (c, context, ref rollback);
		}
	}
}
