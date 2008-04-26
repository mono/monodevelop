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

using System;
using System.Collections.Generic;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlClosingTagState : State
	{
		XmlTagNameState name;
		
		public XmlClosingTagState (State parent, int position)
			: base (parent, position)
		{
		}
		
		protected XmlClosingTagState (XmlClosingTagState copyFrom, bool copyParents)
			: base (copyFrom, copyParents)
		{
			if (copyFrom.name != null)
				name = (XmlTagNameState) copyFrom.name.DeepCopy (false);
		}

		public override State PushChar (char c, int position)
		{
			if (position == StartLocation) {
				if (c != '/')
					throw new InvalidOperationException ("First character pushed to a XmlClosingTagState must be '/'");
				return null;
			}
			
			//if tag closed
			if (c == '>' && name != null) {
				
				//walk up tree of parents looking for matching tag
				foreach (XmlTagState ts in TagParents) {
					
					//when found, walk back up closing all tags
					if (ts.Name.FullName == this.Name.FullName) {
						foreach (XmlTagState ts2 in TagParents) {
							ts2.Closing = true;
							if (ts == ts2)
								break;
						}
						break;
					}
				}
			}
			
			if (c == '<' || c == '>')
				return Parent;
			
			if (position == StartLocation + 1 && char.IsLetter (c)) {
				name = new XmlTagNameState (this, position);
				return name;
			}
			
			return new XmlMalformedTagState (this, position);
		}
		
		public IXmlName Name {
			get { return name; }
		}
		
		public IEnumerable<XmlTagState> TagParents {
			get {
				foreach (State s in Parents) {
					XmlTagState ts = s as XmlTagState;
					if (ts != null)
						yield return ts;
				}
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[XmlClosingTag({0})]", name != null? name.ToString () : string.Empty);
		}
		
		public override State DeepCopy (bool copyParents)
		{
			return new XmlClosingTagState (this, copyParents);
		}

	}
}
