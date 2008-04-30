// 
// XmlAttributeState.cs
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
	
	
	public class XmlAttributeState : State
	{
		StringBuilder nameBuilder;
		string name;
		Mode mode = Mode.Naming;
		
		public XmlAttributeState (XmlTagState parent, int position)
			: base (parent, position)
		{
			nameBuilder = new StringBuilder ();
		}
		
		protected XmlAttributeState (XmlAttributeState copyFrom, bool copyParents)
			: base (copyFrom, copyParents)
		{
			if (copyFrom.nameBuilder != null)
				nameBuilder = new StringBuilder (copyFrom.nameBuilder.ToString ());
			name = copyFrom.name;
			mode = copyFrom.mode;
		}

		public override State PushChar (char c, int position)
		{
			if (c == '<' || c == '>') {
				Close (position);
				return this.Parent;
			}
			
			switch (mode) {
			case Mode.Naming:
				if (position == StartLocation) {
					if (char.IsLetter (c)) {
						nameBuilder.Append (c);
						return null;
					} else {
						throw new InvalidOperationException ("The first char pushed onto an XmlAttributeState must be a letter.");
					}
				} else {
					if (char.IsLetterOrDigit (c) || c == ':') {
						nameBuilder.Append (c);
						return null;
						
					} else if (char.IsWhiteSpace (c)) {
						mode = Mode.GettingEq;
						name = nameBuilder.ToString ();
						nameBuilder = null;
						return null;
						
					} else if (c == '=') {
						mode = Mode.GettingVal;
						name = nameBuilder.ToString ();
						nameBuilder = null;
						return null;
					}
				}
				break;
				
			case Mode.GettingEq:
				if (char.IsWhiteSpace (c)) {
					return null;
				} else if (c == '=') {
					mode = Mode.GettingVal;
					return null;
				}
				break;
				
			case Mode.GettingVal:
				if (char.IsWhiteSpace (c)) {
					return null;
				} else if (char.IsLetterOrDigit (c) || c== '\'' || c== '"') {
					mode = Mode.GotVal;
					return new XmlAttributeValueState (this, position);
				}
				break;
				
			case Mode.GotVal:
				return Parent;
			}
			
			return new XmlMalformedTagState (this, position);
		}
		
		public string Name {
			get {
				if (nameBuilder != null)
					return nameBuilder.ToString ();
				else
					return name?? string.Empty;
			}
		}
		
		public IXmlName TagName {
			get { return ((XmlTagState)Parent).Name; }
		}
		
		enum Mode {
			Naming,
			GettingEq,
			GettingVal,
			GotVal,
		}

		public override string ToString ()
		{
			return string.Format ("[XmlAttribute({0})]", Name);
		}
		
		public override State DeepCopy (bool copyParents)
		{
			return new XmlAttributeState (this, copyParents);
		}

	}
}
