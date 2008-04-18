// 
// AspNetFreeState.cs
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

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	
	
	public class AspNetFreeState : XmlFreeState
	{
		
		bool openTag;
		
		public AspNetFreeState ()
			: base (null, 0)
		{
		}
		
		public AspNetFreeState (State parent, int position)
			: base (parent, position)
		{
		}
		
		protected AspNetFreeState (AspNetFreeState copyFrom)
			: base (copyFrom)
		{
			openTag = copyFrom.openTag;
		}

		public override State PushChar (char c, int location)
		{
			if (c == '<') {
				openTag = true;
				return null;
			}
			
			if (c == '>') {
				XmlTagState tsParent = Parent as XmlTagState;
				if (tsParent != null && tsParent.Closing)
					return Parent;
			}
			
			if (openTag) {
				openTag = false;
				if (c == '/')
					return new XmlClosingTagState (this, location);
				else if (c == '!')
					return new XmlSpecialTagState (this, location);
				else if (c =='%')
					return new AspNetSpecialState (this, location);
				else if (char.IsLetter (c))
					return new XmlTagState (this, location);
				else
					return new XmlMalformedTagState (this, location);
			}
			
			return null;
		}

		public override string ToString ()
		{
			return "[AspNetFree]";
		}
		
		public override State DeepCopy ()
		{
			return new AspNetFreeState ();
		}
	}
}
