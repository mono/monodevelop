// 
// XmlTagState.cs
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

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlTagState : State
	{
		XmlAttributeState AttributeState;
		XmlNameState NameState;
		XmlMalformedTagState MalformedTagState;
		
		public XmlTagState () : this (new XmlAttributeState ()) {}
		
		public XmlTagState  (XmlAttributeState attributeState)
			: this (attributeState, new XmlNameState ()) {}
		
		public XmlTagState  (XmlAttributeState attributeState, XmlNameState nameState)
			: this (attributeState, nameState, new XmlMalformedTagState ()) {}
		
		public XmlTagState (XmlAttributeState attributeState, XmlNameState nameState,
			XmlMalformedTagState malformedTagState)
		{
			this.AttributeState = attributeState;
			this.NameState = nameState;
			this.MalformedTagState = malformedTagState;
			
			Adopt (this.AttributeState);
			Adopt (this.NameState);
			Adopt (this.MalformedTagState);
		}
		
		const int SELFCLOSING = 1;
		
		public override State PushChar (char c, IParseContext context, ref bool reject)
		{
			if (c == '<') {
				context.LogError ("Unexpected '<' in tag.");
				reject = true;
				return Parent;
			}
			
			XElement element = context.Nodes.Peek () as XElement;
			
			if (element == null || element.IsComplete) {
				Debug.Assert (context.CurrentStateLength == 1,
					"IncompleteNode must not be an XClosingTag when CurrentStateLength is 1");
				Debug.Assert (c == '/', "First character pushed to a XmlClosingTagState must be '/'");
				Debug.Assert (context.Nodes.Peek () is XElement);
				
				element = new XElement (context.Position - 1);
				context.Nodes.Push (element);
			}
			
			Debug.Assert (!element.IsComplete);
			
			if (element.IsClosed && c != '>') {
				if (char.IsWhiteSpace (c)) {
					context.LogWarning ("Unexpected whitespace after '/' in self-closing tag.");
					return null;
				} else {
					context.LogError ("Unexpected character '" + c + "' after '/' in self-closing tag.");
					return MalformedTagState;
				}
			}
			
			//if tag closed
			if (c == '>') {
				//have already checked that element is not null, i.e. top of stack is our element
				if (element.IsClosed)
					context.Nodes.Pop ();
				
				if (!element.IsNamed) {
					context.LogError ("Tag closed prematurely.");
				} else {
					element.End (context.Position);
					if (context.BuildTree) {
						XContainer container = (XContainer) context.Nodes.Peek (1);
						container.AddChildNode (element);
					}
				}
				return Parent;
			}
			
			if (char.IsLetter (c) || c == '_') {
				reject = true;
				if (!element.IsNamed) {
					return NameState;
				} else {
					return AttributeState;
				}
			}
			
			if (c == '/') {
				element.Close (element);
				return null;
			}
			
			if (char.IsWhiteSpace (c))
				return null;
			
			reject = true;
			context.LogError ("Unexpected character '" + c + "' in tag.");
			return MalformedTagState;
		}
	}
}
