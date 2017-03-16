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
using MonoDevelop.Ide.Editor.Highlighting;
using System.Text;

namespace Mono.TextEditor.Tests
{
	[Ignore("Redo")]
	[TestFixture]
	class SyntaxHighlightingTests : TextEditorTestBase
	{
		//[Test]
		//public void ValidateSyntaxModes ()
		//{
		//	Assert.IsTrue (SyntaxModeService.ValidateAllSyntaxModes ());
		//}
		
		static void TestOutput (string input, string expectedMarkup)
		{
			TestOutput (input, expectedMarkup, "text/x-csharp");
		}
		
		public static string GetMarkup (string input, string syntaxMode)
		{
			var data = new TextEditorData (new TextDocument (input));
			//data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, syntaxMode);
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Light");
			return data.GetMarkup (0, data.Length, false);
		}

		static void TestOutput (string input, string expectedMarkup, string syntaxMode)
		{
			string markup = GetMarkup (input, syntaxMode);
			if (markup != expectedMarkup){
				Console.WriteLine ("----Expected:");
				Console.WriteLine (expectedMarkup);
				Console.WriteLine ("----Got:");
				Console.WriteLine (markup);
			}
			Assert.AreEqual (expectedMarkup, markup, "expected:" + expectedMarkup + Environment.NewLine + "But got:" + markup);
		}
		 
		[Test]
		public void TestSpans ()
		{
			TestOutput ("/* TestMe */",
			            "<span foreground=\"#888A85\">/* TestMe */</span>");
		}
		
		[Test]
		public void TestStringEscapes ()
		{
			TestOutput ("\"Escape:\\\" \"outtext",
			            "<span foreground=\"#DB7100\">\"Escape:</span><span foreground=\"#A53E00\">\\\"</span><span foreground=\"#DB7100\"> \"</span><span foreground=\"#222222\">outtext</span>");
		}
		
		[Test]
		public void TestVerbatimStringEscapes ()
		{
			TestOutput ("@\"Escape:\"\" \"outtext",
			            "<span foreground=\"#DB7100\">@\"Escape:</span><span foreground=\"#A53E00\">\"\"</span><span foreground=\"#DB7100\"> \"</span><span foreground=\"#222222\">outtext</span>");
		}

		[Test]
		public void TestDoubleVerbatimStringEscapes ()
		{
			TestOutput ("@\"Escape:\"\"\"\" \"outtext",
			            "<span foreground=\"#DB7100\">@\"Escape:</span><span foreground=\"#A53E00\">\"\"\"\"</span><span foreground=\"#DB7100\"> \"</span><span foreground=\"#222222\">outtext</span>");
		}

		[Test]
		[Ignore ("Test does not work under mono 2.10.x")]
		public void TestVerbatimStringEscapeLineBreak ()
		{
			TestOutput ("@\"Escape:\"\"\ntext\"",
			            "<span foreground=\"#DB7100\">@\"Escape:</span><span foreground=\"#A53E00\">\"\"</span>\n<span foreground=\"#DB7100\">text\"</span>");
		}

		[Test]
		public void TestHexDigit ()
		{
			TestOutput ("0x12345679AFFEuL",
			            "<span foreground=\"#DB7100\">0x12345679AFFEuL</span>");
		}
		
		[Test]
		public void TestDoubleDigit ()
		{
			TestOutput ("123.45678e-09d",
			            "<span foreground=\"#DB7100\">123.45678e-09d</span>");
		}
		
		[Test]
		public void TestCDATASection ()
		{
			TestOutput ("<![CDATA[ test]]>",
			            "<span foreground=\"#222222\">&lt;![CDATA[ test]]&gt;</span>",
			            "application/xml");
		}
		
		
		///<summary>
		/// Bug 603 - Last token in doc comment has wrong color 
		///</summary>
		[Test]
		public void TestBug603 ()
		{
			TestOutput ("///<summary>foo bar</summary>",
			            "<span foreground=\"#C8B97B\">///&lt;summary&gt;</span><span foreground=\"#97B488\">foo bar</span><span foreground=\"#C8B97B\">&lt;/summary&gt;</span>");
		}

		[Test]
		public void TestFSharpLineBug ()
		{
			TestOutput (
				"\n\n\nlet x = 2",
				"<span foreground=\"#009695\">let</span><span foreground=\"#222222\"> x = </span><span foreground=\"#DB7100\">2</span>",
				"text/x-fsharp");
		}

		[Test]
		public void TestChunkValidity()
		{
			const string text = @"System.Console.WriteLine();";
			var data = new TextEditorData (new TextDocument (@"namespace FooBar
{
    class Test
    {
   " + text + @"
    }
}"));
			//data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-csharp");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Light");
			var line = data.GetLine (5);
			var chunks = data.GetChunks (line, line.Offset + 3, line.Length - 3);
			StringBuilder sb = new StringBuilder ();
			foreach (var chunk in chunks)
				sb.Append (data.GetTextAt (chunk));
			Assert.AreEqual (text, sb.ToString ());
		}
	}
}
