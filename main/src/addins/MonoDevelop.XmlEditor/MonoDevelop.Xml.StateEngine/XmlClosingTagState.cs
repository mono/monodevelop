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

		public override State PushChar (char c, int position, out bool reject)
		{
			reject = false;
			
			if (position == StartLocation) {
				if (c != '/')
					throw new InvalidOperationException ("First character pushed to a XmlClosingTagState must be '/'");
				return null;
			}
			
			//if tag closed
			if (c == '>' && name != null) {
				reject = true;
				//walk up tree of parents looking for matching tag
				foreach (XmlTagState ts in TagParents) {
					//when found, walk back up closing all tags
					if (ts.Name.FullName == this.Name.FullName) {
						ts.ClosingTag = this;
						foreach (XmlTagState ts2 in TagParents) {
							ts2.Closing = true;
							if (ts == ts2)
								break;
						}
						break;
					}
				}
			}
			
			if (c == '>') {
				if (EndLocation < 0)
					Close (position);
				return Parent;
			}
			
			if (c == '<') {
				reject = true;
				return Parent;
			}
			
			if (position == StartLocation + 1 && char.IsLetter (c)) {
				reject = true;
				name = new XmlTagNameState (this, position);
				return name;
			}
			
			reject = true;
			return new XmlMalformedTagState (this, position);
		}
		
		public IXmlName Name {
			get { return name; }
		}
		
		public IEnumerable<XmlTagState> TagParents {
			get {
				foreach (State s in ParentStack) {
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
		
		#region Cloning API
		
		public override State ShallowCopy ()
		{
			return new XmlClosingTagState (this);
		}
		
		protected XmlClosingTagState (XmlClosingTagState copyFrom) : base (copyFrom)
		{
			if (copyFrom.name != null)
				name = (XmlTagNameState) copyFrom.name.ShallowCopy ();
		}
		
		#endregion

	}
}
