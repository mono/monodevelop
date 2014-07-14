//
// WebFormsHtmlEncodedExpression.cs
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
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
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.WebForms.Dom
{
	
	public class WebFormsHtmlEncodedExpression : WebFormsExpression
	{
		public WebFormsHtmlEncodedExpression (DomRegion region) : base (region)
		{
		}

		public WebFormsHtmlEncodedExpression (TextLocation start) : base (start)
		{
		}

		protected WebFormsHtmlEncodedExpression ()
		{
		}

		protected override XObject NewInstance ()
		{
			return new WebFormsHtmlEncodedExpression ();
		}

		public override string ToString ()
		{
			return string.Format ("[WebFormsHtmlEncodedExpression Location='{0}'", Region);
		}

		public override string FriendlyPathRepresentation {
			get { return "<%: %>"; }
		}
	}
	
}
