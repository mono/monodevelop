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
		State closingTag;
		
		public XmlTagState (State parent, int position)
			: base (parent, position)
		{
		}	
		
		public override State PushChar (char c, int position, out bool reject)
		{
			if (name == null) {
				reject = true;
				name = new XmlTagNameState (this, position);
				return name;
			}
			
			if (c == '<') {
				//FIXME: warning
				reject = true;
				if (EndLocation < 0)
					Close (position);
				return Parent;
			}
			
			if (c == '>') {
				if (EndLocation < 0)
					Close (position);
				if (closing) {
					reject = true;
					return Parent;
				} else {
					reject = false;
					return new XmlFreeState (this, position);
				}
			}
			
			if (c == '/') {
				reject = false;
				ClosingTag = this;
				closing = true;
				return null;
			}
			
			if (char.IsWhiteSpace (c)) {
				reject = false;
				return null;
			} else if (closing) {
				reject = true;
				return new XmlMalformedTagState (this, position);
			}
			
			if (char.IsLetter (c)) {
				reject = true;
				return new XmlAttributeState (this, position);
			}
			
			//FIXME: should we be strict about this?
			reject = true;
			return new XmlMalformedTagState (this, position);
		}
		
		public bool Closing {
			get { return closing; }
			set {
#if DEBUG
				if (!value || closing)
					throw new InvalidOperationException ("The tag can only be closed once");
#endif
				closing = value;
			}
		}
		
		public State ClosingTag {
			get { return closingTag; }
			set {
#if DEBUG
				if (closingTag != null)
					throw new InvalidOperationException ("The closing tag has already been assigned");
#endif
				closingTag = value;
			}
		}
		
		public IXmlName Name {
			get { return name; }
		}

		public override string ToString ()
		{
			return string.Format ("[XmlTag({0})]", name != null? name.FullName.ToString () : string.Empty);
		}
		
		#region Cloning API
		
		public override State ShallowCopy ()
		{
			return new XmlTagState (this);
		}
		
		protected XmlTagState (XmlTagState copyFrom) : base (copyFrom)
		{
			if (copyFrom.name != null)
				name = (XmlTagNameState) copyFrom.name.ShallowCopy ();
			closing = copyFrom.closing;
		}
		
		#endregion
		
	}
}
