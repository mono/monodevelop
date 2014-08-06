// 
// HtmlScriptBodyState.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using MonoDevelop.Xml.Parser;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.Html.Parser
{
	public class HtmlScriptBodyState : XmlParserState
	{
		const string CLOSE = "</script>";
				
		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 0)
				context.StateTag = 0;
			
			if (c == '<') {
				if (context.StateTag == 0) {
					context.StateTag++;
					return null;
				}
			}
			if (context.StateTag > 0) {
				if (CLOSE[context.StateTag] == c) {
					context.StateTag++;
					if (context.StateTag == CLOSE.Length) {
						var el = (XElement) context.Nodes.Pop ();
						var closing = new XClosingTag (new XName ("script"), context.LocationMinus (CLOSE.Length));
						closing.End (context.Location);
						el.Close (closing);
						rollback = string.Empty;
						return Parent;
					}
				} else {
					context.StateTag = 0;
				}
			}
			return null;
		}
	}
}

