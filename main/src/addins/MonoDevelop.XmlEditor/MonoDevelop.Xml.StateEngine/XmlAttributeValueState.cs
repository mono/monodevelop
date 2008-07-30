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
	
	
	public class XmlAttributeValueState : State
	{
		const int NOTSET = 0;
		const int UNDELIMITED = 1;
		const int SINGLEQUOTE = 2;
		const int DOUBLEQUOTE = 3;
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			XAttribute att = (XAttribute) context.Nodes.Peek ();
			System.Diagnostics.Debug.Assert (att.Value == null);
			
			if (context.CurrentStateLength == 1) {
				System.Diagnostics.Debug.Assert (context.KeywordBuilder.Length == 0);
				if (c == '"') {
					context.StateTag = DOUBLEQUOTE;
					return null;
				} else if (c == '\'') {
					context.StateTag = SINGLEQUOTE;
					return null;
				} else if (char.IsLetterOrDigit (c) || c == '_') {
					context.LogWarning ("Unquoted attribute value");
					context.StateTag = UNDELIMITED;
				} else {
					context.LogWarning ("Unexpected character '" + c + "' getting attribute value");
					rollback = string.Empty;
					return Parent;
				}
			}
			
			if (c == '<') {
				context.LogError  ("Attribute value ended unexpectedly.");
				rollback = string.Empty;
				return this.Parent;
			}
			
			//special handling for "undelimited" values
			if (context.StateTag == UNDELIMITED) {
				if (char.IsLetterOrDigit (c) || c == '_' || c == '.') {
					context.KeywordBuilder.Append (c);
					return null;
				} else if (char.IsWhiteSpace (c) || c == '>' || c == '\\') {
					att.Value = context.KeywordBuilder.ToString ();
				} else {
					context.LogWarning ("Unexpected character '" + c + "' getting attribute value");
				}
				rollback = string.Empty;
				return Parent;
			}
			
			//ending the value
			if ((c == '"' && context.StateTag == DOUBLEQUOTE)  ||
				(c == '\'' && context.StateTag == SINGLEQUOTE) ||
				(char.IsWhiteSpace (c) && context.StateTag == UNDELIMITED))
			{
				att.Value = context.KeywordBuilder.ToString ();
				return Parent;
			}
			
			context.KeywordBuilder.Append (c);
			return null;
		}
	}
}
