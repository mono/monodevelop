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
using MonoDevelop.Core;

namespace Mono.TextEditor.Tests
{
	[Ignore("Port to new engine")]
	[TestFixture]
	class HtmlWriterTests : TextEditorTestBase
	{
		[Test]
		public void TestSimpleCSharpHtml ()
		{
			if (Platform.IsWindows)
				Assert.Inconclusive ();
			var data = Create ("");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Tango");
			//data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-csharp");
			data.Text = "class Foo {}";
			//SyntaxModeService.WaitUpdate (data.Document);
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
<SPAN style='color:#3364a4;'>class</SPAN><SPAN style='color:#222222;'>&nbsp;Foo&nbsp;</SPAN><SPAN style='color:#222222;'>{}</SPAN></FONT>
</BODY></HTML>
", generatedHtml);
		}

		[Test]
		public void TestXml ()
		{
			if (Platform.IsWindows)
				Assert.Inconclusive ();
			var data = Create ("");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Tango");
			//data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "application/xml");
			data.Text = @"<foo
	attr1 = ""1""
	attr2 = ""2""
/>";
			
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
<SPAN style='color:#222222;'>&lt;</SPAN><SPAN style='color:#204987;'>foo</SPAN><BR>
<SPAN style='color:#204987;'>&nbsp;&nbsp;&nbsp;&nbsp;</SPAN><SPAN style='color:#f57800;'>attr1</SPAN><SPAN style='color:#222222;'>&nbsp;=</SPAN><SPAN style='color:#204987;'>&nbsp;</SPAN><SPAN style='color:#a40000;'>&quot;</SPAN><SPAN style='color:#a40000;'>1</SPAN><SPAN style='color:#a40000;'>&quot;</SPAN><BR>
<SPAN style='color:#204987;'>&nbsp;&nbsp;&nbsp;&nbsp;</SPAN><SPAN style='color:#f57800;'>attr2</SPAN><SPAN style='color:#222222;'>&nbsp;=</SPAN><SPAN style='color:#204987;'>&nbsp;</SPAN><SPAN style='color:#a40000;'>&quot;</SPAN><SPAN style='color:#a40000;'>2</SPAN><SPAN style='color:#a40000;'>&quot;</SPAN><BR>
<SPAN style='color:#222222;'>/</SPAN><SPAN style='color:#222222;'>&gt;</SPAN></FONT>
</BODY></HTML>
"
, generatedHtml);
		}



	}
}

