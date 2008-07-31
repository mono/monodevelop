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

using System;
using System.Text;
using System.Diagnostics;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlAttributeState : State
	{
		XmlNameState XmlNameState;
		XmlAttributeValueState XmlAttributeValueState;
		XmlMalformedTagState MalformedTagState;
		
		const int NAMING = 0;
		const int GETTINGEQ = 1;
		const int GETTINGVAL = 2;
		
		public XmlAttributeState ()
			: this (new XmlNameState (), new XmlAttributeValueState (), new XmlMalformedTagState ())
		{}
		
		public XmlAttributeState (
			XmlNameState nameState,
			XmlAttributeValueState valueState,
			XmlMalformedTagState malformedTagState)
		{
			this.XmlNameState = nameState;
			this.XmlAttributeValueState = valueState;
			this.MalformedTagState = malformedTagState;
			Adopt (this.XmlNameState);
			Adopt (this.XmlAttributeValueState);
			Adopt (this.MalformedTagState);
		}

		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			XAttribute att = context.Nodes.Peek () as XAttribute;
			
			if (c == '<') {
				//MalformedTagState handles errors and cleanup
				//context.LogError ("Attribute ended unexpectedly with '<' character.");
				//if (att != null)
				//	context.Nodes.Pop ();
				rollback = string.Empty;
				return MalformedTagState;
			}
			
			//state has just been entered
			if (context.CurrentStateLength == 1)  {
				
				//starting a new attribute?
				if (att == null) {
					Debug.Assert (context.StateTag == NAMING);
					att = new XAttribute (context.Position - 1);
					context.Nodes.Push (att);
					rollback = string.Empty;
					return XmlNameState;
				} else {
					Debug.Assert (att.IsNamed);
					if (att.Value == null) {
						context.StateTag = GETTINGEQ;
					} else {
						//Got value, so end attribute
						context.Nodes.Pop ();
						att.End (context.Position - 1);
						IAttributedXObject element = (IAttributedXObject) context.Nodes.Peek ();
						element.Attributes.AddAttribute (att);
						rollback = string.Empty;
						return Parent;
					}
				}
			}
			
			if (c == '>') {
				//MalformedTagState handles errors and cleanup
				//context.LogWarning ("Attribute ended unexpectedly with '>' character.");
				//if (att != null)
				//	context.Nodes.Pop ();
				rollback = string.Empty;
				return MalformedTagState;
			}
			
			if (context.StateTag == GETTINGEQ) {
				if (char.IsWhiteSpace (c)) {
					return null;
				} else if (c == '=') {
					context.StateTag = GETTINGVAL;
					return null;
				}
			} else if (context.StateTag == GETTINGVAL) {
				if (char.IsWhiteSpace (c)) {
					return null;
				} else if (char.IsLetterOrDigit (c) || c== '\'' || c== '"') {
					rollback = string.Empty;
					return XmlAttributeValueState;
				}
			}
			
			//MalformedTagState handles errors and cleanup
			//context.LogError ("Unexpected character '" + c + "' in attribute.");
			//if (att != null)
			//	context.Nodes.Pop ();
			rollback = string.Empty;
			return MalformedTagState;
		}
	}
}
