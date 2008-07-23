// 
// XmlProcessingInstructionState.cs
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

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlProcessingInstructionState : State
	{
		const int NOMATCH = 0;
		const int QUESTION = 1;
		
		public override State PushChar (char c, IParseContext context, ref bool reject)
		{
			if (c == '?') {
				if (context.StateTag == NOMATCH) {
					context.StateTag = QUESTION;
					return null;
				}
			} else if (c == '>' && context.StateTag == QUESTION) {
				// if the '?' is followed by a '>', the state has ended
				// so attach a node to the DOM and end the state
				if (context.BuildTree) {
					int start = context.Position - (context.CurrentStateLength + 2); // <? is 2 chars
					((XContainer) context.Nodes.Peek ()).AddChildNode (
						new XProcessingInstruction (start, context.Position));
				}
				return Parent;
			} else {
				context.StateTag = NOMATCH;
			}
			
			return null;
		}
	}
}
