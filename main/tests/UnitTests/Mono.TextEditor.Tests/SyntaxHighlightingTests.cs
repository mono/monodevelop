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
	[Ignore("Highighting changed")]
	[TestFixture()]
	public class SyntaxHighlightingTests
	{
		[Test]
		public void ValidateSyntaxModes ()
		{
			Assert.IsTrue (SyntaxModeService.ValidateAllSyntaxModes ());
		}
		
		static void TestOutput (string input, string expectedMarkup)
		{
			TestOutput (input, expectedMarkup, "text/x-csharp");
		}
		
		public static string GetMarkup (string input, string syntaxMode)
		{
			var data = new TextEditorData ();
			data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, syntaxMode);
			data.Text = input;
			data.ColorStyle = SyntaxModeService.GetColorStyle ("TangoLight");
			return data.GetMarkup (0, data.Length, false);
		}

		static void TestOutput (string input, string expectedMarkup, string syntaxMode)
		{
			string markup = GetMarkup (input, syntaxMode);
			Assert.AreEqual (expectedMarkup, markup, "expected:" + expectedMarkup + Environment.NewLine + "But got:" + markup);
		}
		 
		[Test]
		public void TestSpans ()
		{
			TestOutput ("/* TestMe */",
		                "<span foreground=\"#4D9A06\">/* TestMe */</span>");
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
		
		[Test]
		public void TestCDATASection ()
		{
			string markup = GetMarkup ("<![CDATA[ test ]]>", "application/xml");
			if (markup != "<span foreground=\"#A40000\" weight=\"bold\">&lt;![CDATA[</span><span foreground=\"#4D9A06\"> test</span><span foreground=\"#A40000\" weight=\"bold\"> ]]&gt;</span>" && 
			    markup != "<span foreground=\"#A40000\" weight=\"bold\">&lt;![CDATA[</span><span foreground=\"#4D9A06\"> test </span><span foreground=\"#A40000\" weight=\"bold\">]]&gt;</span>") {
				Assert.Fail ("CDATA markup invalid:" + markup);
			}
			
			TestOutput ("<![CDATA[ test]]>",
			            "<span foreground=\"#A40000\" weight=\"bold\">&lt;![CDATA[</span><span foreground=\"#4D9A06\"> test</span><span foreground=\"#A40000\" weight=\"bold\">]]&gt;</span>",
			            "application/xml");
		}
		
		
		///<summary>
		/// Bug 603 - Last token in doc comment has wrong color 
		///</summary>
		[Test]
		public void TestBug603 ()
		{
			TestOutput ("///<summary>foo bar</summary>",
		                "<span foreground=\"#4D9A06\">///&lt;summary&gt;foo bar&lt;/summary&gt;</span>");
		}
	}
}
