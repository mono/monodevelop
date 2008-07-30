// 
// XState.cs
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
	
	
	public enum XState
	{
		/// <summary>Not within a tag definition or special section</summary>
		Free,
		Malformed,

		//--------------------parts of tag and attribute definitions---------

		/// <summary>A tag, the type of which has not been determined.</summary>
		OpeningBracket,
		/// <summary>An opening tag's declaration.</summary>
		Tag,
		/// <summary>A closing tag's declaration.</summary>
		ClosingTag,
		/// <summary>A self-closing tag , after the forward slash.</summary>
		SelfClosing,
		/// <summary>A tag's name.</summary>
		TagName,
		/// <summary>A closing tag's name.</summary>
		ClosingTagName,
		/// <summary>An attribute name.</summary>
		AttributeName,
		/// <summary>An attribute seeking an equals sign.</summary>
		AttributeNamed,
		/// <summary>A named attribute seeking a value.</summary>
		AttributeValue,
		/// <summary>A single-quoted attribute value.</summary>
		AttributeQuotes,
		/// <summary>An unquoted attribute value</summary>
		AttributeUnquoted,
		/// <summary>A double-quoted attribute value.</summary>
		AttributeDoubleQuotes,

		//-----------------special XML sections--------------------------------

		/// <summary>An XML/HTML comment.</summary>
		Comment,
		CommentOpening,
		CommentClosing,
		/// <summary>A CDATA section.</summary>
		CData,
		CDataOpening,
		CDataClosing,
		/// <summary>An XML/HTML character entity.</summary>
		Entity,
		/// <summary>An XML/HTML processing instruction.</summary>
		ProcessingInstruction,
		ProcessingInstructionClosing,
	}
}
