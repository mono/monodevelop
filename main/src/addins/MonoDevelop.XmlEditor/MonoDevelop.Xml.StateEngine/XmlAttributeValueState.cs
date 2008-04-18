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
		char startChar;
		bool done;

		public XmlAttributeValueState (XmlAttributeState parent, int position)
			: base (parent, position)
		{
		}
		
		public XmlAttributeValueState (XmlAttributeValueState copyFrom)
			: base (copyFrom)
		{
			startChar = copyFrom.startChar;
			done = copyFrom.done;
		}

		public override State PushChar (char c, int position)
		{
			if (c == '<' || done)
				return Parent;
			
			if (position == StartLocation) {
				if (c == '\'' || c == '"' || char.IsLetterOrDigit (c)) {
					startChar = c;
					return null;
				} else {
					throw new InvalidOperationException ("The first char passed to a XmlAttributeValue must be a single or double quote, ot a letter or digit.");
				}
			}
			
			switch (startChar) {
			case '"':
			case '\'':
				if (c == startChar)
					done = true;
				break;
			default:
				if (!char.IsLetterOrDigit (c)) {
					done = true;
				}
				break;
			}
			return null;
		}
		
		public string AttributeName {
			get { return ((XmlAttributeState)Parent).Name; }
		}
		
		public IXmlName TagName {
			get { return ((XmlAttributeState)Parent).TagName; }
		}

		public override string ToString ()
		{
			return "[XmlAttributeValue]";
		}
		
		public override State DeepCopy ()
		{
			return new XmlAttributeValueState (this);
		}
	}
}
