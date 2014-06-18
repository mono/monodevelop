//
// RazorCommentState.cs
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

using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Razor.Dom;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public class RazorCommentState : XmlCommentState
	{
		protected const int NOMATCH = 0;
		protected const int STAR = 1;

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 1) {
				context.Nodes.Push (new RazorComment (context.LocationMinus (2)));
				return null;
			}

			switch (context.StateTag) {
			case NOMATCH:
				if (c == '*')
					context.StateTag = STAR;
				break;

			case STAR:
				if (c == '@') {
					StateEngineService.EndCodeFragment<RazorComment> (context);
					return Parent;
				} else
					context.StateTag = NOMATCH;
				break;
			}

			return null;
		}
	}
}
