// 
// HtmlTagState.cs
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

using MonoDevelop.Html;
using MonoDevelop.Xml.StateEngine;
	
namespace MonoDevelop.AspNet.StateEngine
{
	
	public class HtmlTagState : XmlTagState
	{
		XmlAttributeState AttributeState;
		XmlNameState NameState;
		XmlMalformedTagState MalformedTagState;
		bool warnAutoClose;
		
		public HtmlTagState (bool warnAutoClose) : this (warnAutoClose, new XmlAttributeState ()) {}
		
		public HtmlTagState (bool warnAutoClose, XmlAttributeState attributeState)
			: this (warnAutoClose, attributeState, new XmlNameState ()) {}
		
		public HtmlTagState (bool warnAutoClose, XmlAttributeState attributeState, XmlNameState nameState)
			: this (warnAutoClose, attributeState, nameState, new XmlMalformedTagState ()) {}
		
		public HtmlTagState (bool warnAutoClose, XmlAttributeState attributeState, XmlNameState nameState,
			XmlMalformedTagState malformedTagState)
			: base (attributeState, nameState, malformedTagState)
		{
			this.warnAutoClose = warnAutoClose;
		}
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			//NOTE: This is (mostly) duplicated in HtmlClosingTagState
			//handle "paragraph" tags implicitly closed by block-level elements
			if (context.CurrentStateLength == 1 && context.PreviousState is XmlNameState)
			{
				XElement element = (XElement) context.Nodes.Peek ();
				//Note: the node stack will always be at least 1 deep due to the XDocument
				XElement parent = context.Nodes.Peek (1) as XElement;
				
				while (parent != null && parent.Name.IsValid && !parent.Name.HasPrefix && !element.Name.HasPrefix
				    && element.Name.IsValid
				    && !ElementTypes.IsInline (element.Name.Name)
				    && (ElementTypes.IsInline (parent.Name.Name) || ElementTypes.IsParagraph (parent.Name.Name))
				    )
				{
					
					context.Nodes.Pop ();
					context.Nodes.Pop ();
					if (warnAutoClose) {
						context.LogWarning (string.Format ("Tag '{0}' implicitly closed by tag '{1}'.",
							parent.Name.Name, element.Name.Name), parent.Region);
					}
					//parent.Region.End = element.Region.Start;
					//parent.Region.End.Column = Math.Max (parent.Region.End.Column - 1, 1);
					parent.Close (parent);
					context.Nodes.Push (element);
					
					parent = context.Nodes.Peek (1) as XElement;
				}
			}
			
			//handle implicitly empty tags
			if (c == '>')
			{
				XElement element = context.Nodes.Peek () as XElement;
				if (element != null && !element.Name.HasPrefix && element.Name.IsValid
				    && ElementTypes.IsEmpty (element.Name.Name))
				{
					element.Close (element);
					if (warnAutoClose) {
						context.LogWarning (string.Format ("Implicitly closed empty tag '{0}'", element.Name.Name),
						                    element.Region);
					}
				}
			}
			
			return base.PushChar (c, context, ref rollback);
		}
	}
}
