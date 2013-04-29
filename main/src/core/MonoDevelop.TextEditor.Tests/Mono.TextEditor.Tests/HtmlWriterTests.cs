// 
// HtmlWriterTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor.Utils;
using NUnit.Framework;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor.Tests
{
	[Ignore]
	[TestFixture]
	public class HtmlWriterTests : TextEditorTestBase
	{
		[Test]
		public void TestSimpleCSharpHtml ()
		{
			var data = Create ("class Foo {}");
			data.ColorStyle = SyntaxModeService.GetColorStyle ("TangoLight");
			data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-csharp");
			string generatedHtml = HtmlWriter.GenerateHtml (data);
			Assert.AreEqual (
				@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">
<HTML>
<HEAD>
<META HTTP-EQUIV=""CONTENT-TYPE"" CONTENT=""text/html; charset=utf-8"">
<META NAME=""GENERATOR"" CONTENT=""Mono Text Editor"">
</HEAD>
<BODY>
<FONT face = 'Mono'>
<SPAN style = 'color:#009695;' >class</SPAN><SPAN style = 'color:#444444;' >&nbsp;Foo&nbsp;</SPAN><SPAN style = 'color:#444444;' >{}</SPAN></FONT>
</BODY></HTML>
", generatedHtml);
		}

		[Test]
		public void TestXml ()
		{
			var data = Create (
@"<foo
	attr1 = ""1""
	attr2 = ""2""
/>");
			data.ColorStyle = SyntaxModeService.GetColorStyle ("TangoLight");
			data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "application/xml");
			string generatedHtml = HtmlWriter.GenerateHtml (data);
			Assert.AreEqual (
				@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">
<HTML>
<HEAD>
<META HTTP-EQUIV=""CONTENT-TYPE"" CONTENT=""text/html; charset=utf-8"">
<META NAME=""GENERATOR"" CONTENT=""Mono Text Editor"">
</HEAD>
<BODY>
<FONT face = 'Mono'>
<SPAN style = 'color:#444444;' >&lt;</SPAN><SPAN style = 'color:#3364a4;' >foo</SPAN><BR>
<SPAN style = 'color:#3364a4;' >&nbsp;&nbsp;&nbsp;&nbsp;</SPAN><SPAN style = 'color:#444444;' >attr1</SPAN><SPAN style = 'color:#444444;' >&nbsp;=</SPAN><SPAN style = 'color:#3364a4;' >&nbsp;</SPAN><SPAN style = 'color:#f57d00;' >&quot;</SPAN><SPAN style = 'color:#f57d00;' >1</SPAN><SPAN style = 'color:#f57d00;' >&quot;</SPAN><BR>
<SPAN style = 'color:#3364a4;' >&nbsp;&nbsp;&nbsp;&nbsp;</SPAN><SPAN style = 'color:#444444;' >attr2</SPAN><SPAN style = 'color:#444444;' >&nbsp;=</SPAN><SPAN style = 'color:#3364a4;' >&nbsp;</SPAN><SPAN style = 'color:#f57d00;' >&quot;</SPAN><SPAN style = 'color:#f57d00;' >2</SPAN><SPAN style = 'color:#f57d00;' >&quot;</SPAN><BR>
<SPAN style = 'color:#444444;' >/</SPAN><SPAN style = 'color:#444444;' >&gt;</SPAN></FONT>
</BODY></HTML>
"
, generatedHtml);
		}



	}
}

