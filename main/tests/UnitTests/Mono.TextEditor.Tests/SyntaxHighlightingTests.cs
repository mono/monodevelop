// 
// SyntaxHighlightingTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using NUnit.Framework;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class SyntaxHighlightingTests
	{
		[Test]
		public void ValidateSyntaxModes ()
		{
			Assert.IsTrue (SyntaxModeService.ValidateAllSyntaxModes ());
		}
		static void TestOutput (string input,
		                         string expectedMarkup)
		{
			Document doc = new Document ();
			doc.SyntaxMode = SyntaxModeService.GetSyntaxMode ("text/x-csharp");
			doc.Text = input;
			string markup = doc.SyntaxMode.GetMarkup (doc, TextEditorOptions.DefaultOptions, SyntaxModeService.GetColorStyle (null, "TangoLight"), 0, doc.Length, false);
			Assert.AreEqual (expectedMarkup, markup, "expected:" + expectedMarkup + Environment.NewLine + "But got:" + markup);
		}
		
		[Test]
		public void TestSpans ()
		{
			TestOutput ("/* TestMe */",
		                "<span foreground=\"#4E9A06\">/* TestMe */</span>");
		}
		
		[Test]
		public void TestStringEscapes ()
		{
			TestOutput ("\"Escape:\\\" \"outtext",
		                "<span foreground=\"#75507B\">\"Escape:\\\" \"</span><span foreground=\"#000000\">outtext</span>");
		}
		
		[Test]
		public void TestVerbatimStringEscapes ()
		{
			TestOutput ("@\"Escape:\"\" \"outtext",
		                "<span foreground=\"#75507B\">@\"Escape:\"\" \"</span><span foreground=\"#000000\">outtext</span>");
		}
		
		[Test]
		public void TestHexDigit ()
		{
			TestOutput ("0x12345679AFFEuL",
		                "<span foreground=\"#75507B\">0x12345679AFFEuL</span>");
		}
		
		[Test]
		public void TestDoubleDigit ()
		{
			TestOutput ("123.45678e-09d",
		                "<span foreground=\"#75507B\">123.45678e-09d</span>");
		}
	}
}
