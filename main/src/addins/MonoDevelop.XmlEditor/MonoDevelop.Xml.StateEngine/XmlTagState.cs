// 
// XmlTagState.cs
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
	
	
	public class XmlTagState : State
	{
		XmlTagNameState name;
		bool closing = false;
		
		public XmlTagState (State parent, int position)
			: base (parent, position)
		{
		}
		
		protected XmlTagState (XmlTagState copyFrom)
			: base (copyFrom)
		{
			if (copyFrom.name != null)
				name = (XmlTagNameState) copyFrom.name.DeepCopy ();
			closing = copyFrom.closing;
		}	
		
		public override State PushChar (char c, int position)
		{
			if (name == null) {
				name = new XmlTagNameState (this, position);
				return name;
			}
			
			if (c == '<')
				return Parent;
			
			if (c == '>')
				return closing? Parent : new XmlFreeState (this, position);
			
			if (c == '/') {
				closing = true;
				return null;
			}
			
			if (closing)
				return new XmlMalformedTagState (this, position);
			
			if (char.IsWhiteSpace (c))
				return null;
			
			if (char.IsLetter (c))
				return new XmlAttributeState (this, position);
			
			//FIXME: should we be strict about this?
			return new XmlMalformedTagState (this, position);
		}
		
		public bool Closing {
			get { return closing; }
			set { closing = value; }
		}
		
		public IXmlName Name {
			get { return name; }
		}

		public override string ToString ()
		{
			return string.Format ("[XmlTag({0})]", name != null? name.FullName.ToString () : string.Empty);
		}
		
		public override State DeepCopy ()
		{
			return new XmlTagState (this);
		}
		
	}
}
