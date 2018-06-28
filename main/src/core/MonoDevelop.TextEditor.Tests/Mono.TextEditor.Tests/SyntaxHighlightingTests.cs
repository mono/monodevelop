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
using System.Threading.Tasks;

namespace Mono.TextEditor.Tests
{
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
			data.Options.EditorThemeName = EditorTheme.DefaultDarkThemeName;
				
			data.Document.MimeType = syntaxMode;
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
			            "<span foreground=\"#789769\">/* TestMe */</span>");
		}
		
		[Test]
		public void TestStringEscapes ()
		{
			TestOutput ("\"Escape:\\\" \"outtext",
			            "<span foreground=\"#e5da73\">\"Escape:</span><span foreground=\"#8ae232\">\\\"</span><span foreground=\"#e5da73\"> \"</span><span foreground=\"#eeeeec\">outtext</span>");
		}
		
		[Test]
		public void TestVerbatimStringEscapes ()
		{
			TestOutput ("@\"Escape:\"\" \"outtext",
			            "<span foreground=\"#e5da73\">@\"Escape:</span><span foreground=\"#8ae232\">\"\"</span><span foreground=\"#e5da73\"> \"</span><span foreground=\"#eeeeec\">outtext</span>");
		}

		[Test]
		public void TestDoubleVerbatimStringEscapes ()
		{
			TestOutput ("@\"Escape:\"\"\"\" \"outtext",
			            "<span foreground=\"#e5da73\">@\"Escape:</span><span foreground=\"#8ae232\">\"\"\"\"</span><span foreground=\"#e5da73\"> \"</span><span foreground=\"#eeeeec\">outtext</span>");
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
			            "<span foreground=\"#8ae232\">0x12345679AFFEuL</span>");
		}
		
		[Test]
		public void TestDoubleDigit ()
		{
			TestOutput ("123.45678e-09d",
			            "<span foreground=\"#8ae232\">123.45678e-09d</span>");
		}
		
		[Test]
		public void TestCDATASection ()
		{
			TestOutput ("<![CDATA[ test]]>",
			            "<span foreground=\"#e5da73\">&lt;![CDATA[ test]]&gt;</span>",
			            "application/xml");
		}
		
		
		///<summary>
		/// Bug 603 - Last token in doc comment has wrong color 
		///</summary>
		[Test]
		public void TestBug603 ()
		{
			TestOutput ("///<summary>foo bar</summary>",
			            "<span foreground=\"#789769\">///</span><span foreground=\"#888a85\">&lt;</span><span foreground=\"#719dcf\">summary</span><span foreground=\"#888a85\">&gt;</span><span foreground=\"#789769\">foo bar</span><span foreground=\"#888a85\">&lt;/</span><span foreground=\"#719dcf\">summary</span><span foreground=\"#888a85\">&gt;</span>");
		}

		[Test]
		public void TestFSharpLineBug ()
		{
			TestOutput (
				"\n\n\nlet x = 2",
				"<span foreground=\"#719dcf\">let</span><span foreground=\"#eeeeec\"> x = </span><span foreground=\"#8ae232\">2</span>",
				"text/x-fsharp");
		}

		[Test]
		public async Task TestChunkValidity()
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
			var chunks = await data.GetChunks (line, line.Offset + 3, line.Length - 3);
			StringBuilder sb = new StringBuilder ();
			foreach (var chunk in chunks)
				sb.Append (data.GetTextAt (chunk));
			Assert.AreEqual (text, sb.ToString ());
		}

		/// <summary>
		/// Bug 55462 - Syntax highlighting breaks after using literal string with "$@" prefix
		/// </summary>
		[Test]
		public void Test55462 ()
		{
			TestOutput ("$@\"test\" // test",
			  			"<span foreground=\"#e5da73\">$@\"test\"</span><span foreground=\"#eeeeec\"> </span><span foreground=\"#789769\">// test</span>");
		}

		/// <summary>
		/// Bug 55670 - color scheme not working on floating point literals without decimal points
		/// </summary>
		[Test]
		public void Test55670 ()
		{
			TestOutput ("0.5f",
			  			"<span foreground=\"#8ae232\">0.5f</span>");
			TestOutput ("5f",
			  			"<span foreground=\"#8ae232\">5f</span>");
			TestOutput ("2e64",
			  			"<span foreground=\"#8ae232\">2e64</span>");
		}

		[Test]
		public void TestBinaryLiteral ()
		{
			TestOutput ("0b1111_0000",
			  			"<span foreground=\"#8ae232\">0b1111_0000</span>");
		}

		[Test]
		public void TestBug56747 ()
		{
			TestOutput ("$\"{{foo}}\"",
			  			"<span foreground=\"#e5da73\">$\"{{foo}}\"</span>");
		}

		[Test]
		public void TestBug57033 ()
		{
			TestOutput ("$\"{foo}\"",
			  			"<span foreground=\"#e5da73\">$\"{</span><span foreground=\"#eeeeec\">foo</span><span foreground=\"#e5da73\">}\"</span>");
		}

	}
}
