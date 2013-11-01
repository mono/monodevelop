// 
// CSharpTextEditorIndentationTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Refactoring;
using Mono.TextEditor;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.CSharp.Formatting;
using UnitTests;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharpBinding
{

	[TestFixture]
	public class CSharpTextEditorIndentationTests : TestBase
	{
		const string eolMarker = "\n";

		public static TextEditorData Create (string content, ITextEditorOptions options = null)
		{
			var data = new TextEditorData ();
			data.Options.DefaultEolMarker = eolMarker;
			data.Options.IndentStyle = IndentStyle.Smart;
			if (options != null)
				data.Options = options;
			var sb = new StringBuilder ();
			int caretIndex = -1, selectionStart = -1, selectionEnd = -1;
			var foldSegments = new List<FoldSegment> ();
			var foldStack = new Stack<FoldSegment> ();

			for (int i = 0; i < content.Length; i++) {
				var ch = content [i];
				switch (ch) {
					case '$':
					caretIndex = sb.Length;
					break;
					case '<':
					if (i + 1 < content.Length) {
						if (content [i + 1] == '-') {
							selectionStart = sb.Length;
							i++;
							break;
						}
					}
					goto default;
					case '-':
					if (i + 1 < content.Length) {
						var next = content [i + 1];
						if (next == '>') {
							selectionEnd = sb.Length;
							i++;
							break;
						}
						if (next == '[') {
							var segment = new FoldSegment (data.Document, "...", sb.Length, 0, FoldingType.None);
							segment.IsFolded = false;
							foldStack.Push (segment);
							i++;
							break;
						}
					}
					goto default;
					case '+':
					if (i + 1 < content.Length) {
						var next = content [i + 1];
						if (next == '[') {
							var segment = new FoldSegment (data.Document, "...", sb.Length, 0, FoldingType.None);
							segment.IsFolded = true;
							foldStack.Push (segment);
							i++;
							break;
						}
					}
					goto default;
					case ']':
					if (foldStack.Count > 0) {
						FoldSegment segment = foldStack.Pop ();
						segment.Length = sb.Length - segment.Offset;
						foldSegments.Add (segment);
						break;
					}
					goto default;
					default:
					sb.Append (ch);
					break;
				}
			}
			
			data.Text = sb.ToString ();

			if (caretIndex >= 0)
				data.Caret.Offset = caretIndex;
			if (selectionStart >= 0) {
				if (caretIndex == selectionStart) {
					data.SetSelection (selectionEnd, selectionStart);
				} else {
					data.SetSelection (selectionStart, selectionEnd);
					if (caretIndex < 0)
						data.Caret.Offset = selectionEnd;
				}
			}
			if (foldSegments.Count > 0)
				data.Document.UpdateFoldSegments (foldSegments);
			return data;
		}

		IStateMachineIndentEngine CreateTracker (TextEditorData data)
		{
			var policy = PolicyService.InvariantPolicies.Get <CSharpFormattingPolicy> ("text/x-csharp").CreateOptions();
			var textStylePolicy = data.CreateNRefactoryTextEditorOptions();
			textStylePolicy.IndentBlankLines = true;
			var result = new CacheIndentEngine(new ICSharpCode.NRefactory.CSharp.CSharpIndentEngine(data.Document, textStylePolicy, policy));
			result.Update (data.Caret.Offset);
			return result;
		}

		void CheckOutput (TextEditorData data, string output, CSharpTextEditorIndentation engine = null)
		{
			if (engine == null)
				engine = new CSharpTextEditorIndentation ();
			engine.FixLineStart (data, CreateTracker (data), data.Caret.Line);
			int idx = output.IndexOf ('$');
			if (idx > 0)
				output = output.Substring (0, idx) + output.Substring (idx + 1);
			if (output != data.Text)
				Console.WriteLine (data.Text.Replace ("\t", "\\t").Replace (" ", "."));
			Assert.AreEqual (output, data.Text);
			Assert.AreEqual (idx, data.Caret.Offset, "Caret offset mismatch.");
		}

		[Test]
		public void TestXmlDocumentContinuation ()
		{
			var data = Create (
				"\t\t///" + eolMarker + 
					"\t\t/// Hello$" + eolMarker +
					"\t\tclass Foo {}"
			);

			MiscActions.InsertNewLine (data);

			CheckOutput (data,
				"\t\t///" + eolMarker +
				"\t\t/// Hello" + eolMarker +
				"\t\t/// $" + eolMarker +
				"\t\tclass Foo {}");
		}

		[Test]
		public void TestXmlDocumentContinuationCase2 ()
		{
			var data = Create ("\t\t///" + eolMarker +
"\t\t/// Hel$lo" + eolMarker +
"\t\tclass Foo {}");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\t\t///" + eolMarker +
"\t\t/// Hel" + eolMarker +
"\t\t/// $lo" + eolMarker +
				"\t\tclass Foo {}");
		}

		[Test]
		public void TestMultiLineCommentContinuation ()
		{
			var data = Create ("\t\t/*$" + eolMarker + "\t\tclass Foo {}");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\t\t/*" + eolMarker + "\t\t * $" + eolMarker + "\t\tclass Foo {}");
		}

		[Test]
		public void TestMultiLineCommentContinuationCase2 ()
		{
			var data = Create (
				"\t\t/*" + eolMarker +
				"\t\t * Hello$" + eolMarker +
				"\t\tclass Foo {}");
			MiscActions.InsertNewLine (data);
			CheckOutput (data, 
			             "\t\t/*" + eolMarker +
			             "\t\t * Hello" + eolMarker +
			             "\t\t * $" + eolMarker +
			             "\t\tclass Foo {}");
		}

		[Test]
		public void TestMultiLineCommentContinuationCase3 ()
		{
			var data = Create ("\t\t/*" + eolMarker +
			             "\t\t * Hel$lo" + eolMarker +
			             "class Foo {}");
			MiscActions.InsertNewLine (data);

			CheckOutput (data,
			             "\t\t/*" + eolMarker +
			             "\t\t * Hel" + eolMarker +
			             "\t\t * $lo" + eolMarker +
			             "class Foo {}");
		}

		[Test]
		public void TestStringContination ()
		{
			var data = Create ("\t\t\"Hello$ World\"");
			MiscActions.InsertNewLine (data);

			var engine = new CSharpTextEditorIndentation () {
				wasInStringLiteral = true
			};
			CheckOutput (data, "\t\t\"Hello\" +" + eolMarker + "\t\t\"$World\"", engine);
		}

		/// <summary>
		/// Bug 3214 - Unclosed String causes 'Enter' key to produce appended String line.
		/// </summary>
		[Test]
		public void TestBug3214 ()
		{
			var data = Create ("\"Hello\n\t$");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\"Hello\n\t" + eolMarker + "\t$");
		}

		void TestGuessSemicolonInsertionOffset (string fooBar, bool expected = true)
		{
			StringBuilder sb = new StringBuilder ();
			int semicolonOffset = 0;
			int guessedOffset = 0;
			for (int i = 0; i <fooBar.Length; i++) {
				char ch = fooBar [i];
				if (ch == '$') {
					semicolonOffset = sb.Length - 1;
				} else if (ch == '~') {
					guessedOffset = sb.Length - 1;
				} else {
					sb.Append (ch);
				}
			}
			var data = new TextEditorData ();
			data.Text = sb.ToString ();
			int guessed;
			Assert.AreEqual (expected, CSharpTextEditorIndentation.GuessSemicolonInsertionOffset (data, data.GetLineByOffset (semicolonOffset), semicolonOffset, out guessed));
			if (expected)
				Assert.AreEqual (guessedOffset, guessed);
		}

		[Test]
		public void TestSemicolonOffsetInParens ()
		{
			TestGuessSemicolonInsertionOffset ("FooBar($)~");
		}

		[Test]
		public void TestSemicolonAlreadyPlaced ()
		{
			TestGuessSemicolonInsertionOffset ("FooBar($~);", false);
		}
		
		/// <summary>
		/// Bug 6190 - Incorrect semicolon placement inside property declaration when "Smart semocolon placement" is active
		/// </summary>
		[Test]
		public void TestBug6190 ()
		{
			TestGuessSemicolonInsertionOffset ("public bool Property { get$~ private set; }", false);
			TestGuessSemicolonInsertionOffset ("public bool Property { get; private set$~ }", false);
		}
		/// <summary>
		/// Bug 5353 - semicolon placed in wrong place in single-line statement 
		/// </summary>
		[Test]
		public void TestBug5353 ()
		{
			TestGuessSemicolonInsertionOffset ("NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Refresh, delegate { ReloadSummaryWrapper()$~}); ", false);
		}

		[Test]
		public void TestInPropertySemicolon ()
		{
			TestGuessSemicolonInsertionOffset ("public bool Test { get { return false $~ } }", false);
		}

		[Test]
		public void TestInnerMethodCall ()
		{
			TestGuessSemicolonInsertionOffset ("Foo(Bar()$)~");
		}

		[Test]
		public void TestBug5353Case2 ()
		{
			TestGuessSemicolonInsertionOffset ("NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Refresh, delegate { ReloadSummaryWrapper();$})~");
		}

		/// <summary>
		/// Bug 6862 - Smart semicolon placement does not work on empty parameter call 
		/// </summary>
		[Test]
		public void TestBug6862 ()
		{
			TestGuessSemicolonInsertionOffset ("this.method($)~");
		}

		/// <summary>
		/// Bug 11966 - Code Completion Errors with /// Comments
		/// </summary>
		[Test]
		public void TestBug11966 ()
		{
			var data = Create ("///<summary>This is a long comment $ </summary>");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, @"///<summary>This is a long comment 
/// $ </summary>");
		}


		[Test]
		public void TestEnterSelectionBehavior ()
		{
			var data = Create ("\tfirst\n<-\tsecond\n->$\tthird");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\tfirst\n\t$third");
		}


		/// <summary>
		/// Bug 15335 - In a multiline comment, pressing Enter jumps way ahead
		/// </summary>
		[Test]
		public void TestBug15335 ()
		{
			var data = Create ("namespace Foo\n{\n\tpublic class Bar\n\t{\n\t\tvoid Test()\r\n\t\t{\r\n\t\t\t/* foo$\n\t\t}\n\t}\n}\n");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "namespace Foo\n{\n\tpublic class Bar\n\t{\n\t\tvoid Test()\r\n\t\t{\r\n\t\t\t/* foo\n\t\t\t * $\n\t\t}\n\t}\n}\n");
		}
	}
}
