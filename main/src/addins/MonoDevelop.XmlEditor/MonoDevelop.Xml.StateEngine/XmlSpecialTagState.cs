// 
// XmlSpecialTagState.cs
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
using System.Text;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlSpecialTagState : State
	{
		Mode mode = Mode.None;
		
		const string cdata = "[CDATA[";
		
		public XmlSpecialTagState (State parent, int position)
			: base (parent, position)
		{
		}
		
		protected XmlSpecialTagState (XmlSpecialTagState copyFrom, bool copyParents)
			: base (copyFrom, copyParents)
		{
			mode = copyFrom.mode;
		}

		public override State PushChar (char c, int position)
		{
			int index = position - StartLocation;
			
			if (c == '<' || c == '>')
				return Parent;
			
			switch (index) {
			case 0:
				if (c== '!') {
					mode = Mode.Exclam;
					return null;
				} else if (char.IsLetter (c)) {
					return new XmlTagState (this, position);
				} else if (c == '?') {
					return new XmlProcessingInstructionState (this, position);
				}
				break;
			case 1:
				if (mode == Mode.Exclam) {
					if (c == '-') {
						mode = Mode.Comment;
						return null;
					} else if (c == '[') {
						mode = Mode.CData;
						return null;
					}
				}
				break;
			case 2:
				if (mode == Mode.Comment && c== '-')
					return new XmlCommentState (this, position);
				else goto default;
			default:
				if (mode == Mode.CData && c == cdata [index - 1]) {
					if (index > cdata.Length)
						return new CDataState (this, position);
					else
						return null;
				}
				break;
			}
			
			return new XmlMalformedTagState (this, position);
		}
		
		enum Mode {
			None,
			Exclam,
			CData,
			Comment
		}

		public override string ToString ()
		{
			return string.Format ("[XmlSpecialTag]");
		}
		
		public override State DeepCopy (bool copyParents)
		{
			return new XmlSpecialTagState (this, copyParents);
		}

	}
}
