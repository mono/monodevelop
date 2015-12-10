//
// RazorCSharpParserContext.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using System;
using System.Collections.Generic;
using System.Web.Razor;
using Microsoft.CodeAnalysis;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.Razor
{
	class RazorCSharpParserContext
	{
		MonoDevelop.Ide.TypeSystem.ParseOptions parseOptions;
		OpenRazorDocument razorDocument;

		public RazorCSharpParserContext (MonoDevelop.Ide.TypeSystem.ParseOptions parseOptions, OpenRazorDocument razorDocument)
		{
			this.parseOptions = parseOptions;
			this.razorDocument = razorDocument;
		}

		public DotNetProject Project {
			get { return parseOptions.Project as DotNetProject; }
		}

		public AspNetAppProjectFlavor AspProject {
			get { return parseOptions.Project.As<AspNetAppProjectFlavor> (); }
		}

		public string FileName {
			get { return parseOptions.FileName; }
		}

		public ITextSource Content {
			get { return parseOptions.Content; }
		}

		public ITextDocument Document {
			get { return razorDocument.Document; }
		}

		public OpenRazorDocument RazorDocument {
			get { return razorDocument; }
		}

		public SyntaxTree ParsedSyntaxTree { get; set; }
		public string CSharpCode { get; set; }
		public Microsoft.CodeAnalysis.Document AnalysisDocument { get; set; }
		public XDocument HtmlParsedDocument { get; set; }
		public IList<Comment> Comments { get; set; }
		public MonoDevelop.Web.Razor.EditorParserFixed.RazorEditorParser EditorParser {
			get { return razorDocument.EditorParser; }
			set { razorDocument.EditorParser = value; }
		}

		public DocumentParseCompleteEventArgs CapturedArgs {
			get { return razorDocument.CapturedArgs; }
		}

		public ChangeInfo GetLastTextChange ()
		{
			return razorDocument.LastTextChange;
		}

		public void ClearLastTextChange ()
		{
			razorDocument.ClearLastTextChange ();
		}
	}
}

