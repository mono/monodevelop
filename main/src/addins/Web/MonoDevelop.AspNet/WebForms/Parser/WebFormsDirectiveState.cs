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

using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.WebForms.Dom;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.WebForms.Parser
{	
	public class WebFormsDirectiveState : XmlParserState
	{
		readonly XmlAttributeState AttributeState;
		readonly XmlNameState NameState;
		
		public WebFormsDirectiveState () : this (
			new XmlNameState (),
			new XmlAttributeState (new XmlNameState (), new WebFormsDirectiveAttributeState ())
			)
		{
		}
		
		public WebFormsDirectiveState (XmlNameState nameState, XmlAttributeState attributeState)
		{
			this.AttributeState = attributeState;
			this.NameState = nameState;
			
			Adopt (this.AttributeState);
			Adopt (this.NameState);

		}
		
		const int ENDING = 1;
		
		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			var directive = context.Nodes.Peek () as WebFormsDirective;
			
			if (directive == null || directive.IsComplete) {
				directive = new WebFormsDirective (context.LocationMinus (4)); // 4 == <%@ + current char
				context.Nodes.Push (directive);
			}
			
			if (c == '<') {
				context.LogError ("Unexpected '<' in directive.");
				rollback = string.Empty;
				return Parent;
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
						directive.End (context.Location);
						if (context.BuildTree) {
							var container = (XContainer)context.Nodes.Peek ();
							container.AddChildNode (directive);
						}
					}
					return Parent;
				}
				//ending but not '>'? Error; go to end.
			} else if (char.IsLetter (c)) {
				rollback = string.Empty;
				if (!directive.IsNamed) {
					return NameState;
				} else {
					return AttributeState;
				}
			} else if (char.IsWhiteSpace (c))
				return null;
			
			rollback = string.Empty;
			context.LogError ("Unexpected character '" + c + "' in tag.");
			return Parent;
		}
	}
}
