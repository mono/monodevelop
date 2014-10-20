//
// XDocType.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Xml.Dom
{
	public class XDocType : XNode, INamedXObject
	{
		public XDocType (TextLocation start) : base (start) {}
		public XDocType (DomRegion region) : base (region) {}

		protected XDocType () {}
		protected override XObject NewInstance () { return new XDocType (); }

		public XName RootElement { get; set; }
		public string PublicFpi { get; set; }
		public bool IsPublic { get { return PublicFpi != null; } }
		public DomRegion InternalDeclarationRegion { get; set; }
		public string Uri { get; set; }

		public override string FriendlyPathRepresentation {
			get { return "<!DOCTYPE>"; }
		}

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XDocType copyFromDT = (XDocType) copyFrom;
			//immutable types
			RootElement = copyFromDT.RootElement;
			PublicFpi = copyFromDT.PublicFpi;
			InternalDeclarationRegion = copyFromDT.InternalDeclarationRegion;
			Uri = copyFromDT.Uri;
		}

		XName INamedXObject.Name {
			get { return RootElement; }
			set { RootElement = value; }
		}

		bool INamedXObject.IsNamed {
			get { return RootElement.IsValid; }
		}

		public override string ToString ()
		{
			return string.Format("[DocType: RootElement='{0}', PublicFpi='{1}',  InternalDeclarationRegion='{2}', Uri='{3}']",
			                     RootElement.FullName, PublicFpi, InternalDeclarationRegion, Uri);
		}
	}
}
