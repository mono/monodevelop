// 
// XmlCDataState.cs
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
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Xml.StateEngine
{
	public class XmlCDataState : State
	{
		const int NOMATCH = 0;
		const int SINGLE_BRACKET = 1;
		const int DOUBLE_BRACKET = 2;
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 1) {
				int start = context.Position - "<![CDATA[".Length - 1;
				context.Nodes.Push (new XCData (start));
			}
			
			if (c == ']') {
				//make sure we know when there are two ']' chars together
				if (context.StateTag == NOMATCH)
					context.StateTag = SINGLE_BRACKET;
				else
					context.StateTag = DOUBLE_BRACKET;
				
			} else if (c == '>' && context.StateTag == DOUBLE_BRACKET) {
				// if the ']]' is followed by a '>', the state has ended
				// so attach a node to the DOM and end the state
				XCData cdata = (XCData) context.Nodes.Pop ();
				
				if (context.BuildTree) {
					cdata.End (context.Position);
					((XContainer) context.Nodes.Peek ()).AddChildNode (cdata); 
				}
				return Parent;
			} else {
				// not any part of a ']]>', so make sure matching is reset
				context.StateTag = NOMATCH;
			}
			
			return null;
		}
	}
}
