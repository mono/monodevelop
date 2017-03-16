// 
// XmlTagNameState.cs
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
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlNameState : XmlParserState
	{
		internal static bool IsValidNameStart (char c)
		{
			return char.IsLetter (c) || c == '_';
		}

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			var namedObject = context.Nodes.Peek () as INamedXObject;
			if (namedObject == null || namedObject.Name.Prefix != null)
				throw new InvalidOperationException ("Invalid state");
			
			Debug.Assert (context.CurrentStateLength > 1 || IsValidNameStart (c), 
				"First character pushed to a XmlTagNameState must be a letter.");
			Debug.Assert (context.CurrentStateLength > 1 || context.KeywordBuilder.Length == 0,
				"Keyword builder must be empty when state begins.");
			
			if (XmlChar.IsWhitespace (c) || c == '<' || c == '>' || c == '/' || c == '=') {
				rollback = string.Empty;
				if (context.KeywordBuilder.Length == 0) {
					context.LogError ("Zero-length name.");
				} else {
					string s = context.KeywordBuilder.ToString ();
					int i = s.IndexOf (':');
					if (i < 0) {
						namedObject.Name = new XName (s);
					} else {
						namedObject.Name = new XName (s.Substring (0, i), s.Substring (i + 1));
					}
				}
				
				return Parent;
			}
			if (c == ':') {
				if (context.KeywordBuilder.ToString ().IndexOf (':') > 0)
					context.LogError ("Unexpected ':' in name.");
				context.KeywordBuilder.Append (c);
				return null;
			}
			
			if (XmlChar.IsNameChar (c)) {
				context.KeywordBuilder.Append (c);
				return null;
			}
			
			rollback = string.Empty;
			context.LogError ("Unexpected character '" + c +"' in name");
			return Parent;
		}
	}
}
