// 
// XmlAttributeValueState.cs
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
	
	
	public abstract class XmlAttributeValueState : State
	{
		protected XmlMalformedTagState MalformedTagState { get; private set; }
		
		public XmlAttributeValueState () : this (new XmlMalformedTagState ())
		{}
		
		public XmlAttributeValueState (XmlMalformedTagState malformedTagState)
		{
			this.MalformedTagState = malformedTagState;
			Adopt (this.MalformedTagState);
		}
	}
	
	public class XmlUnquotedAttributeValueState : XmlAttributeValueState
	{
		public XmlUnquotedAttributeValueState () : this (new XmlMalformedTagState ()) {}
		public XmlUnquotedAttributeValueState (XmlMalformedTagState malformedTagState) : base (malformedTagState) {}
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			System.Diagnostics.Debug.Assert (((XAttribute) context.Nodes.Peek ()).Value == null);
			
			if (c == '<' || c == '>') {
				//MalformedTagState handles error reporting
				//context.LogError  ("Attribute value ended unexpectedly.");
				rollback = string.Empty;
				return MalformedTagState;
			} else if (char.IsLetterOrDigit (c) || c == '_' || c == '.') {
				context.KeywordBuilder.Append (c);
				return null;
			} else if (char.IsWhiteSpace (c) || c == '>' || c == '\\') {
				//ending the value
				XAttribute att = (XAttribute) context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
			} else {
				//MalformedTagState handles error reporting
				//context.LogWarning ("Unexpected character '" + c + "' getting attribute value");
				return MalformedTagState;
			}
			
			rollback = string.Empty;
			return Parent;
		}
	}
	
	public class XmlSingleQuotedAttributeValueState : XmlAttributeValueState
	{
		public XmlSingleQuotedAttributeValueState () : this (new XmlMalformedTagState ()) {}
		public XmlSingleQuotedAttributeValueState (XmlMalformedTagState malformedTagState) : base (malformedTagState) {}
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			System.Diagnostics.Debug.Assert (((XAttribute) context.Nodes.Peek ()).Value == null);
			
			if (c == '<' || c == '>') {
				//MalformedTagState handles error reporting
				//context.LogError  ("Attribute value ended unexpectedly.");
				rollback = string.Empty;
				return MalformedTagState;
			} 
			else if (c == '\'') {
				//ending the value
				XAttribute att = (XAttribute) context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
				return Parent;
			}
			else {
				context.KeywordBuilder.Append (c);
				return null;
			}
		}
	}
	
	public class XmlDoubleQuotedAttributeValueState : XmlAttributeValueState
	{
		public XmlDoubleQuotedAttributeValueState () : this (new XmlMalformedTagState ()) {}
		public XmlDoubleQuotedAttributeValueState (XmlMalformedTagState malformedTagState) : base (malformedTagState) {}
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			System.Diagnostics.Debug.Assert (((XAttribute) context.Nodes.Peek ()).Value == null);
			
			if (c == '<' || c == '>') {
				//MalformedTagState handles error reporting
				//context.LogError  ("Attribute value ended unexpectedly.");
				rollback = string.Empty;
				return MalformedTagState;
			} 
			else if (c == '"') {
				//ending the value
				XAttribute att = (XAttribute) context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
				return Parent;
			}
			else {
				context.KeywordBuilder.Append (c);
				return null;
			}
		}
	}
}
