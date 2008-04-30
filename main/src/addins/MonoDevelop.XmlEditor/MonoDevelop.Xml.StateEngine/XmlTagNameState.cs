// 
// XmlTagNameState.cs
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
	
	
	public class XmlTagNameState : State, IXmlName
	{
		StringBuilder nameBuilder;
		string namespc;
		string name;
		
		public XmlTagNameState (State parent, int position)
			: base (parent, position)
		{
			nameBuilder = new StringBuilder ();
		}
		
		protected XmlTagNameState (XmlTagNameState copyFrom, bool copyParents)
			: base (copyFrom, copyParents)
		{
			if (copyFrom.nameBuilder != null)
				nameBuilder = new StringBuilder (copyFrom.nameBuilder.ToString ());
			namespc = copyFrom.namespc;
			name = copyFrom.name;
		}

		public override State PushChar (char c, int position)
		{
#if DEBUG
			if (position == StartLocation && !char.IsLetter (c))
				throw new InvalidOperationException ("First character pushed to a XmlTagNameState must be a letter.");
#endif		
			if (c == '<') {
				Close (position);
				return Parent;
			}
			
			if (c == ':' && namespc == null) {
				namespc = nameBuilder.ToString ();
				nameBuilder = new StringBuilder ();
				return null;
			}
			
			if (char.IsWhiteSpace (c) || c == '>' || c == '/') {
				name = nameBuilder.ToString ();
				nameBuilder = null;
				Close (position);
				return Parent;
			}
			
			if (char.IsLetterOrDigit (c) && nameBuilder != null) {
				nameBuilder.Append (c);
				return null;
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
		
		public string Namespace {
			get {
				return namespc?? string.Empty;
			}
		}
		
		public bool Complete {
			get { return name != null; }
		}
		
		public string FullName {
			get {
				return namespc == null? Name : string.Concat (namespc, ":", Name);
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[XmlTagName({0})]", FullName);
		}
		
		public override State DeepCopy (bool copyParents)
		{
			return new XmlTagNameState (this, copyParents);
		}
	}
	
	public interface IXmlName {
		string Namespace { get; }
		string Name { get; }
		string FullName { get; }
		bool Complete { get; }
	}
}
