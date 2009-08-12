// 
// ElementTypes.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Html
{
	
	
	public class ElementTypes
	{
		//element listings found at http://gigamonkeys.com/book/practical-an-html-generation-library-the-interpreter.html
		//(but they are simply a transcription of the element names in the HTML specs)
		
		public static readonly ICollection<string> Block = new string[] {
			"body", "colgroup", "dl", "fieldset",
			"form", "head", "html", "map", "noscript",
			"object", "ol", "optgroup", "pre", "script",
			"select", "style", "table", "tbody", "tfoot",
			"thead", "tr", "ul"
		};
		
		public static readonly ICollection<string> Paragraph = new string[] {
			"area", "base", "blockquote", "br", "button", "caption",
			"col", "dd", "div", "dt", "h1", "h2", "h3", "h4", "h5",
			"h6", "hr", "input", "li", "link", "meta", "option", "p",
			"param", "td", "textarea", "th", "title"
		};
		
		public static readonly ICollection<string> Inline = new string[] {
			"a", "abbr", "acronym", "address", "b", "bdo", "big",
			"cite", "code", "del", "dfn", "em", "i", "img", "ins",
			"kbd", "label", "legend", "q", "samp", "small", "span",
			"strong", "sub", "sup", "tt", "var"
		};
		
		public static readonly ICollection<string> Empty = new string[] {
			"area", "base", "br", "col", "hr", "img", "input", "link", "meta", "param"
		};
		
		public static readonly ICollection<string> PreserveWhiteSpace = new string[] {
			"pre", "script", "style"
		};
		
		public static bool IsInline (string elementName)
		{
			return Inline.Contains (elementName.ToLower ());
		}
		
		public static bool IsBlock (string elementName)
		{
			return Block.Contains (elementName.ToLower ());
		}
		
		public static bool IsParagraph (string elementName)
		{
			return Paragraph.Contains (elementName.ToLower ());
		}
		
		public static bool IsEmpty (string elementName)
		{
			return Empty.Contains (elementName.ToLower ());
		}
	}
}
