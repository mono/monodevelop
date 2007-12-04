/* 
* HtmlParsingObject.cs - A ParsingObject for html tags
* 
* Authors: 
*  Michael Hutchinson <m.j.hutchinson@gmail.com>
*  
* Copyright (C) 2005 Michael Hutchinson
*
* This sourcecode is licenced under The MIT License:
* 
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to permit
* persons to whom the Software is furnished to do so, subject to the
* following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
* OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
* NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
* DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
* USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Text;

namespace AspNetEdit.Editor.Persistence
{
	internal class HtmlParsingObject : ParsingObject
	{
		public HtmlParsingObject (string tagText, string tagid, ParsingObject parent)
			: base (tagid, parent)
		{
			AddText (tagText);
		}

		public override ParsingObject CloseObject (string closingTagText)
		{
			AddText (closingTagText);
			return base.CloseObject (closingTagText);
		}
}

	internal class ServerFormParsingObject : HtmlParsingObject
	{
		public ServerFormParsingObject (string tagText, string tagid, ParsingObject parent)
			: base (tagText, tagid, parent)
		{
		}

		public override bool InServerForm
		{
			get { return true; }
		}
	}
}
