//
// RazorDirectiveState.cs
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
using System.Diagnostics;
using MonoDevelop.AspNet.Razor.Dom;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public class RazorDirectiveState : RazorCodeFragmentState
	{
		public RazorDirective CorrespondingDirective {
			get { return CorrespondingBlock as RazorDirective; }
		}

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 1) {
				bracketsBuilder.Clear ();
				var directive = context.Nodes.FirstOrDefault (n => n is RazorDirective);
				if (directive == null) {
					if (context.PreviousState is XmlClosingTagState && CorrespondingDirective != null)
						context.Nodes.Push (CorrespondingDirective);
					else {
						Debug.Fail ("Directive should be pushed before changing the state to DirectiveState");
						return Parent;
					}
				} else
					CorrespondingBlock = directive as RazorDirective;
			}

			if (CorrespondingDirective.IsSimpleDirective) {
				if (c == '<')
					IsInsideGenerics = true;
				else if (c == '\n') {
					StateEngineService.EndCodeFragment<RazorDirective> (context);
					return Parent.Parent;
				}
				// using directives can be placed in one line, e.g. @using foo @using bar
				else if (CorrespondingDirective.Name == "using"
						&& !(Char.IsLetterOrDigit (c) || c == ' ' || c == '=' || c == '.')) {
					rollback = String.Empty;
					StateEngineService.EndCodeFragment<RazorDirective> (context, 1);
					return Parent.Parent;
				}
				return null;

			} else {
				switch (c) {
				case '{':
					if (context.StateTag != TRANSITION)
						return ParseOpeningBracket (c, context);
					break;

				case '}':
					return ParseClosingBracket<RazorDirective> (c, context, Parent.Parent);
				}
			}

			return base.PushChar (c, context, ref rollback);
		}
	}
}
