// 
// AspNetServerCommentState.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	public class AspNetServerCommentState : XmlCommentState
	{
		
		const int NOMATCH = 0;
		const int SINGLE_DASH = 1;
		const int DOUBLE_DASH = 2;
		const int PERCENT = 3;
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 1) {
				context.Nodes.Push (new AspNetServerComment (context.LocationMinus (context.CurrentStateLength + "<%--".Length)));
			}
			
			switch (context.StateTag) {
			case NOMATCH:
				if (c == '-')
					context.StateTag = SINGLE_DASH;
				break;
				
			case SINGLE_DASH:
				if (c == '-')
					context.StateTag = DOUBLE_DASH;
				else
					context.StateTag = NOMATCH;
				break;
				
			case DOUBLE_DASH:
				if (c == '%')
					context.StateTag = PERCENT;
				else
					context.StateTag = NOMATCH;
				break;
				
			case PERCENT:
				if (c == '>') {
					AspNetServerComment comment = (AspNetServerComment) context.Nodes.Pop ();
					comment.End (context.Location);
					if (context.BuildTree) {
						XObject ob = context.Nodes.Peek ();
						if (ob is XContainer) {
							((XContainer)ob).AddChildNode (comment);
						}
					 	//FIXME: add to other kinds of node, e.g. if used within a tag
					}
					return Parent;
				}
				break;
			}
			
			return null;
		}
	}
}
