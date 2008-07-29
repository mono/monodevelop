// 
// AspNetDirectiveState.cs
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
using System.Diagnostics;

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	
	
	public class AspNetDirectiveState : State
	{
		XmlAttributeState AttributeState;
		XmlNameState NameState;
		XmlMalformedTagState MalformedTagState;
		
		public AspNetDirectiveState () : this (new XmlAttributeState ()) {}
		
		public AspNetDirectiveState (XmlAttributeState attributeState)
			: this (attributeState, new XmlNameState ()) {}
		
		public AspNetDirectiveState (XmlAttributeState attributeState, XmlNameState nameState)
			: this (attributeState, nameState, new XmlMalformedTagState ()) {}
		
		public AspNetDirectiveState (XmlAttributeState attributeState, XmlNameState nameState,
			XmlMalformedTagState malformedTagState)
		{
			this.AttributeState = attributeState;
			this.NameState = nameState;
			this.MalformedTagState = malformedTagState;
			
			Adopt (this.AttributeState);
			Adopt (this.NameState);
			Adopt (this.MalformedTagState);
		}
		
		const int ENDING = 1;
		
		public override State PushChar (char c, IParseContext context, ref bool reject)
		{
			AspNetDirective directive = context.Nodes.Peek () as AspNetDirective;
			
			if (directive == null || directive.IsComplete) {
				directive = new AspNetDirective (context.Position - 3); // 3 == <% + current char
				context.Nodes.Push (directive);
			}
			
			if (c == '<') {
				context.LogError ("Unexpected '<' in directive.");
				reject = true;
				return MalformedTagState;
			}
			
			Debug.Assert (!directive.IsComplete);
			
			if (context.StateTag != ENDING && c == '%') {
				context.StateTag = ENDING;
				return null;
			}
			
			
			if (context.StateTag == ENDING) {
				if (c == '>') {
					//have already checked that directive is not null, i.e. top of stack is our directive
					context.Nodes.Pop ();
					
					if (!directive.IsNamed) {
						context.LogError ("Directive closed prematurely.");
					} else {
						directive.End (context.Position);
						if (context.BuildTree) {
							XContainer container = (XContainer) context.Nodes.Peek ();
							container.AddChildNode (directive);
						}
					}
					return Parent;
				}
				//ending but not '>'? Error; go to end.
			}
			else if (char.IsLetter (c)) {
				reject = true;
				if (!directive.IsNamed) {
					return NameState;
				} else {
					return AttributeState;
				}
			}
			else if (char.IsWhiteSpace (c))
				return null;
			
			reject = true;
			context.LogError ("Unexpected character '" + c + "' in tag.");
			return MalformedTagState;
		}
	}
}
