// 
// XmlAttributeState.cs
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

using System.Diagnostics;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlAttributeState : XmlParserState
	{
		readonly XmlNameState XmlNameState;
		readonly XmlAttributeValueState AttributeValueState;
		
		const int NAMING = 0;
		const int GETTINGEQ = 1;
		const int GETTINGVAL = 2;
		
		public XmlAttributeState () : this (
			new XmlNameState (),
			new XmlAttributeValueState ())
		{}
		
		public XmlAttributeState (
			XmlNameState nameState,
			XmlAttributeValueState attributeValueState)
		{
			this.XmlNameState = nameState;
			this.AttributeValueState = attributeValueState;
			
			Adopt (this.XmlNameState);
			Adopt (this.AttributeValueState);
		}

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			var att = context.Nodes.Peek () as XAttribute;

			//state has just been entered
			if (context.CurrentStateLength == 1)  {
				if (context.PreviousState is XmlNameState) {
					//error parsing name
					if (!att.IsNamed) {
						context.Nodes.Pop ();
						rollback = string.Empty;
						return Parent;
					}
					context.StateTag = GETTINGEQ;
				}
				else if (context.PreviousState is XmlAttributeValueState) {
					//Got value, so end attribute
					context.Nodes.Pop ();
					att.End (context.LocationMinus (1));
					IAttributedXObject element = (IAttributedXObject) context.Nodes.Peek ();
					if (element.Attributes.Get (att.Name, false) != null) {
						context.LogError ("'" + att.Name + "' is a duplicate attribute name.", att.Region);
					}
					element.Attributes.AddAttribute (att);
					rollback = string.Empty;
					return Parent;
				}
				else {
					//starting a new attribute
					Debug.Assert (att == null);
					Debug.Assert (context.StateTag == NAMING);
					att = new XAttribute (context.LocationMinus (1));
					context.Nodes.Push (att);
					rollback = string.Empty;
					return XmlNameState;
				}
			}

			if (context.StateTag == GETTINGEQ) {
				if (char.IsWhiteSpace (c)) {
					return null;
				}
				if (c == '=') {
					context.StateTag = GETTINGVAL;
					return null;
				}
				context.LogError ("Expecting = in attribute, got '" + c + "'.");
			} else if (context.StateTag == GETTINGVAL) {
				if (char.IsWhiteSpace (c)) {
					return null;
				}
				rollback = string.Empty;
				return AttributeValueState;
			} else if (c != '<') {
				//parent handles message for '<'
				context.LogError ("Unexpected character '" + c + "' in attribute.");
			}

			if (att != null)
				context.Nodes.Pop ();
			
			rollback = string.Empty;
			return Parent;
		}
	}
}
