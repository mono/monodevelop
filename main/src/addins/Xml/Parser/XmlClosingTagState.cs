// 
// XmlClosingTagState.cs
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
	public class XmlClosingTagState : XmlParserState
	{
		readonly XmlNameState NameState;
		
		public XmlClosingTagState ()
			: this (new XmlNameState ())
		{
		}
		
		public XmlClosingTagState (XmlNameState nameState)
		{
			NameState = nameState;
			Adopt (NameState);
		}

		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			var ct = context.Nodes.Peek () as XClosingTag;
			
			if (ct == null) {
				Debug.Assert (context.CurrentStateLength == 1,
					"IncompleteNode must not be an XClosingTag when CurrentStateLength is 1");
				
				ct = new XClosingTag (context.LocationMinus (3)); //3 = </ and the current char
				context.Nodes.Push (ct);
			}
			
			//if tag closed
			if (c == '>') {
				context.Nodes.Pop ();
				
				if (ct.IsNamed) {
					ct.End (context.Location);
					
					// walk up tree of parents looking for matching tag
					int popCount = 0;
					bool found = false;
					foreach (XObject node in context.Nodes) {
						popCount++;
						XElement element = node as XElement;
						if (element != null && element.Name == ct.Name) {
							found = true;
							break;
						}
					}
					if (!found)
						popCount = 0;
					
					//clear the stack of intermediate unclosed tags
					while (popCount > 1) {
						XElement el = context.Nodes.Pop () as XElement;
						if (el != null)
							context.LogError (string.Format (
								"Unclosed tag '{0}' at line {1}, column {2}.", el.Name.FullName, el.Region.BeginLine, el.Region.BeginColumn),
								ct.Region);
						popCount--;
					}
					
					//close the start tag, if we found it
					if (popCount > 0) {
						if (context.BuildTree)
							((XElement) context.Nodes.Pop ()).Close (ct);
						else
							context.Nodes.Pop ();
					} else {
						context.LogError (
							"Closing tag '" + ct.Name.FullName + "' does not match any currently open tag.",
							ct.Region
						);
					}
				} else {
					context.LogError ("Closing tag ended prematurely.");
				}
				return Parent;
			}
			
			if (c == '<') {
				context.LogError ("Unexpected '<' in tag.", context.LocationMinus (1));
				context.Nodes.Pop ();
				rollback = string.Empty;
				return Parent;
			}

			if (XmlChar.IsWhitespace (c)) {
				return null;
			}
			
			if (!ct.IsNamed && (char.IsLetter (c) || c == '_')) {
				rollback = string.Empty;
				return NameState;
			}
			
			rollback = string.Empty;
			context.LogError ("Unexpected character '" + c + "' in closing tag.", context.LocationMinus (1));
			context.Nodes.Pop ();
			return Parent;
		}
	}
}
