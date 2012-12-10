//
// RazorPageInfo.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Web.Razor.Parser.SyntaxTree;
using MonoDevelop.AspNet.Parser.Dom;
using System.Web.Razor;

namespace MonoDevelop.AspNet.Mvc.Parser
{
	public class RazorPageInfo
	{
		public IEnumerable<Error> Errors { get; set; }
		public IEnumerable<FoldingRegion> FoldingRegions { get; set; }
		public IEnumerable<Comment> Comments { get; set; }
		public GeneratorResults GeneratorResults { get; set; }
		public Block RazorRoot { get { return GeneratorResults.Document; } }
		public RootNode HtmlRoot { get; set; }
		public IEnumerable<Span> Spans { get; set; }
		public string DocType { get; set; }
		public RazorHostKind HostKind { get; set; }

		public RazorPageInfo ()
		{
			// TODO: extract doctype from view or layout page
			DocType = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
		}
	}

	public class RazorCSharpPageInfo : RazorPageInfo
	{
		public ParsedDocumentDecorator CSharpParsedFile { get; set; }
		public ICompilation Compilation { get; set; }
		public string CSharpCode { get; set; }
	}

	public enum RazorHostKind
	{
		Template,
		WebPage,
		WebCode
	}
}
