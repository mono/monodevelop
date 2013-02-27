// 
// ViTests.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Reflection;
using Mono.TextEditor.Vi;
using System.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class ViTests : TextEditorTestBase
	{
		[Test]
		public void ColumnMotion ()
		{
			var mode = new TestViEditMode () { Text = "Test\nText" };
			Assert.AreEqual (1, mode.Col);
			mode.Input ('l');
			Assert.AreEqual (2, mode.Col);
			mode.Input ("ll");
			Assert.AreEqual (4, mode.Col);
			mode.Input ('l');
			Assert.AreEqual (4, mode.Col);
			mode.Input ("hh");
			Assert.AreEqual (2, mode.Col);
			mode.Input ('h');
			Assert.AreEqual (1, mode.Col);
			mode.Input ('h');
			Assert.AreEqual (1, mode.Col);
		}
		
		[Test]
		public void LineMotion ()
		{
			var mode = new TestViEditMode () { Text =
@"abc def
ghy jklmn op
qrstu
vwxyz",
			};
			mode.Caret.Offset = 0;
			Assert.AreEqual (1, mode.Line);
			mode.Input ('j');
			Assert.AreEqual (2, mode.Line);
			mode.Input ('k');
			Assert.AreEqual (1, mode.Line);
			mode.Input ("jjjj");
			Assert.AreEqual (4, mode.Line);
			mode.Caret.Line = 2;
			mode.Caret.Column = 12;
			mode.Input ('j');
			Assert.AreEqual (5, mode.Col);
			mode.Input ('k');
			Assert.AreEqual (12, mode.Col);
		}
		
		[Test]
		public void ChangeWord ()
		{
			var mode = new TestViEditMode () { Text =
@"abc def
ghy jklmn op
qrstu",
			};
			mode.Caret.Offset = 0;
			mode.Input ("jwcwhi");
			Assert.AreEqual ("ghy hi op", mode.GetLine ());
		}
		
		[Test]
		public void DeleteWord ()
		{
			var mode = new TestViEditMode () { Text =
@"abc def
ghy jklmn op
qrstu",
			};
			mode.Caret.Offset = 0;
			mode.Input ("jwlldw");
			Assert.AreEqual ("ghy jkop", mode.GetLine ());
		}
		
		[Test]
		public void DeleteLine ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa aaa
   bbb bbb
   ccc ccc",
			};
			mode.Input ("jwlldd");
			Assert.AreEqual (2, mode.Line);
			Assert.AreEqual (4, mode.Col);
			Assert.AreEqual (
@"   aaa aaa
   ccc ccc", mode.Text);
		}

		[Test]
		[TestCase(0, Result = " foo.bar[20]->baz_bay")]
		[TestCase(2, Result = "iffoo.bar[20]->baz_bay")]
		[TestCase(3, Result = "if .bar[20]->baz_bay")]
		[TestCase(5, Result = "if .bar[20]->baz_bay")]
		[TestCase(6, Result = "if foobar[20]->baz_bay")]
		[TestCase(16, Result = "if foo.bar[20]->")]
		public string ChangeInnerWord(int column) 
		{
			var mode = new TestViEditMode { Text = "if foo.bar[20]->baz_bay" };
			RepeatChar('l', column, mode);
			mode.Input ("ciw");
			return mode.Text;
		}

		[Test]
		public void ChangeLine ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa aaa
   bbb bbb
   ccc ccc",
			};
			mode.Input ("jwllcceeee");
			Assert.AreEqual (
@"   aaa aaa
   eeee
   ccc ccc", mode.Text);
		}

		[Test]
		[TestCase(0, Result = "if (foo(baz) == bar) ")]
		[TestCase(2, Result = "if (foo(baz) == bar) ")]
		[TestCase(3, Result = "if () ")]
		[TestCase(6, Result = "if () ")]
		[TestCase(7, Result = "if (foo() == bar) ", Ignore = true)] // IBracketMatcher is misbehaving for test mode
		[TestCase(9, Result = "if (foo() == bar) ")]
		[TestCase(10, Result = "if (foo() == bar) ")]
		[TestCase(11, Result = "if (foo() == bar) ")]
		[TestCase(12, Result = "if () ")]
		[TestCase(18, Result = "if () ")]
		[TestCase(19, Result = "if () ")]
		[TestCase(20, Result = "if (foo(baz) == bar) ")]
		public string ChangeInnerParen(int column) 
		{
			var mode = new TestViEditMode { Text = "if (foo(baz) == bar) " };
			RepeatChar('l', column, mode);
			mode.Input ("ci)");
			return mode.Text;
		}

		[Test]
		[TestCase(0, 2, Result = "do { \n\tfoo\n} forever;")]
		[TestCase(0, 3, Result = "do {\n} forever;")]
		[TestCase(0, 4, Result = "do {\n} forever;")]
		[TestCase(1, 0, Result = "do {\n} forever;")]
		[TestCase(2, 0, Result = "do {\n} forever;")]
		[TestCase(2, 1, Result = "do { \n\tfoo\n} forever;")]
		public string ChangeInnerBrace (int row, int column)
		{
			var mode = new TestViEditMode { Text = 
@"do { 
	foo
} forever;" };
			RepeatChar ('j', row, mode);
			RepeatChar ('l', column, mode);
			mode.Input ("ci}");
			return mode.Text;
		}

		[Test]
		[TestCase(0, 0, Result = "{\n}")]
		[TestCase(1, 0, Result = "{\n}")]
		[TestCase(2, 0, Result = "{\n}")]
		[TestCase(3, 0, Result = "{\n\tdo\n\t{\n\t}\n\tdo {\n\t\tbar\n\t}\n}")]
		[TestCase(4, 0, Result = "{\n\tdo\n\t{\n\t}\n\tdo {\n\t\tbar\n\t}\n}")]
		[TestCase(5, 0, Result = "{\n}")]
		[TestCase(6, 0, Result = "{\n\tdo\n\t{\n\t\tfoo\n\t}\n\tdo {\n\t}\n}")]
		[TestCase(7, 0, Result = "{\n\tdo\n\t{\n\t\tfoo\n\t}\n\tdo {\n\t}\n}")]
		[TestCase(8, 0, Result = "{\n}")]
		public string ChangeInnerBrace_WithNestedMatchingBraces(int row, int column)
		{
			var mode = new TestViEditMode { Text = 
@"{
	do
	{
		foo
	}
	do {
		bar
	}
}" };
			RepeatChar('j', row, mode);
			RepeatChar('l', column, mode);
			mode.Input ("ci}");
			return mode.Text;
		}

		[Test]
		[Ignore("IBracketMatcher is misbehaving for test mode, this really does work.")]
		[TestCase('(', ')', '"')]
		[TestCase('{', '}', '"')]
		[TestCase('[', ']', '"')]
		[TestCase('<', '>', '"')]
		[TestCase('(', ')', '\'')]
		[TestCase('{', '}', '\'')]
		[TestCase('[', ']', '\'')]
		[TestCase('<', '>', '\'')]
		public void ChangeInnerBrace_IsntConfusedByQuotes (char startBrace, char endBrace, char quote)
		{
			var text = string.Format ("if {0}{2}{1}{2} == bar{1} ", startBrace, endBrace, quote);
			var mode = new TestViEditMode { Text = text };
			RepeatChar ('l', 4, mode);

			// Validate that our setups work. Use a specific case (the rest proved correct via induction).
			if (startBrace == '(' && quote == '\'') {
				Assert.AreEqual ("if (')' == bar) ", text, 
				                 "Validating that our test input because it's a bit too generic to be easy to read.");

				var plusMinusOneCharContext = "" + mode.GetChar(mode.Caret.Offset-1) + mode.GetChar(mode.Caret.Offset) 
					+ mode.GetChar(mode.Caret.Offset+1);
				Assert.AreEqual ("(')", plusMinusOneCharContext, "The cursor is where we expect it to be.");

				Assert.NotNull(mode.Document.BracketMatcher);
				Assert.IsInstanceOf<DefaultBracketMatcher>(mode.Document.BracketMatcher);

				var matchingParen = mode.Document.GetMatchingBracketOffset (3);
				Assert.AreEqual (14, matchingParen, "Validate setup correctly enabled mode.Document functionality.");
			}

			mode.Input ("ci" + endBrace);
			var expectedText = string.Format ("if {0}{1} ", startBrace, endBrace, quote);
			Assert.AreEqual (expectedText, mode.Text);
		}

		[Test]
		public void VisualInnerParen() 
		{
			var mode = new TestViEditMode { Text = "if (foo(baz) == bar) " };
			RepeatChar('l', 3, mode);
			mode.Input ("vi)");
			mode.AssertSelection (1, 5, 1, 20);
		}

		[Test]
		public void DeleteInnerParen() 
		{
			var mode = new TestViEditMode { Text = "if (foo(baz) == bar) " };
			RepeatChar('l', 3, mode);
			mode.Input ("di)");
			Assert.AreEqual ("if () ", mode.Text);
		}

		[Test]
		public void YankInnerParen() 
		{
			var mode = new TestViEditMode { Text = "if (foo(baz) == bar) " };
			RepeatChar('l', 3, mode);
			mode.Input ("yi)");
			mode.Input ("$p"); // HACK: I can't figure out how to test the register, so just paste at the end
			Assert.AreEqual ("if (foo(baz) == bar) foo(baz) == bar", mode.Text);
		}

		[Test]
		[TestCase(0, '\'', Result = @"'' 'world\'s' 'worlder\\'s'")]
		[TestCase(1, '\'', Result = @"'' 'world\'s' 'worlder\\'s'")]
		[TestCase(5, '\'', Result = @"'' 'world\'s' 'worlder\\'s'")]
		[TestCase(6, '\'', Result = @"'' 'world\'s' 'worlder\\'s'")]
		[TestCase(7, '\'', Result = @"'hello''world\'s' 'worlder\\'s'")]
		[TestCase(8, '\'', Result = @"'hello' '' 'worlder\\'s'", IgnoreReason = "Advanced scenario")]
		[TestCase(9, '\'', Result = @"'hello' '' 'worlder\\'s'")]
		[TestCase(16, '\'', Result = @"'hello' '' 'worlder\\'s'")]
		[TestCase(17, '\'', Result = @"'hello' '' 'worlder\\'s'")]
		[TestCase(18, '\'', Result = @"'hello' 'world\'s''worlder\\'s'")]
		[TestCase(19, '\'', Result = @"'hello' 'world\'s' ''", IgnoreReason = "Advanced scenario")]
		[TestCase(29, '\'', Result = @"'hello' 'world\'s' ''")]
		[TestCase(30, '\'', Result = @"'hello' 'world\'s' ''")]
		[TestCase(31, '\'', Result = @"'hello' 'world\'s' ''")]
		[TestCase(1, '"', Result = @""""" ""world\""s"" ""worlder\\""s""")]
		[TestCase(31, '`', Result = @"`hello` `world\`s` ``")]
		public string ChangeInnerQuote (int col, char quote)
		{
			var mode = new TestViEditMode { Text = @"'hello' 'world\'s' 'worlder\\'s'".Replace ('\'', quote) };
			RepeatChar('l', col, mode);
			mode.Input ("ci" + quote);
			return mode.Text;
		}

		[Test]
		public void ChangeOuterQuote() 
		{
			var mode = new TestViEditMode { Text = "'hello'" };
			RepeatChar('l', 3, mode);
			mode.Input ("ca'");
			Assert.That (mode.Text, Is.Empty);
		}

		[Test]
		public void ChangeOuterParen() 
		{
			var mode = new TestViEditMode { Text = "((hello))" };
			RepeatChar('l', 3, mode);
			mode.Input ("ca)");
			Assert.That (mode.Text, Is.EqualTo ("()"));
		}

		[Test]
		public void ChangeOuterParen_Multiline() 
		{
			var mode = new TestViEditMode { Text = "((\n\thello\n))" };
			RepeatChar('j', 1, mode);
			RepeatChar('l', 4, mode);
			mode.Input ("ca)");
			Assert.That (mode.Text, Is.EqualTo ("()"));
		}

		static void RepeatChar(char c, int count, TestViEditMode mode) 
		{
			string input = "";
			for (int i = 0; i < count; i++)
				input += c;
			mode.Input(input);
		}
		
		[Ignore("FixMe")]
		[Test]
		public void LineDeletePaste ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa aaa
   bbb bbb
   ccc ccc
   eee eee
   fff fff
   ggg ggg
   hhh hhh",
			};
			//move down, enter visual line mode, move down twice -> lines 2/3/4 selected -> delete
			mode.Input ("jjVjjd");
			Assert.AreEqual (
@"   aaa aaa
   bbb bbb
   ggg ggg
   hhh hhh", mode.Text);
			//enter visual line mode, move down once -> lines 1/2 selected -> delete
   			mode.Input ("Vkd");
			Assert.AreEqual (
@"   aaa aaa
   hhh hhh", mode.Text);
			//paste last delete below current line (1)
   			mode.Input ("p");
			Assert.AreEqual (
@"   aaa aaa
   hhh hhh
   bbb bbb
   ggg ggg", mode.Text);
			//paste last delete above current line (3)
   			mode.Input ("P");
			Assert.AreEqual (
@"   aaa aaa
   hhh hhh
   bbb bbb
   bbb bbb
   ggg ggg
   ggg ggg", mode.Text);
			Assert.AreEqual (5, mode.Line);
			//movement to/across boundaries, check selection still okay by deleting lines 1/2/3
			mode.Input ("kVlllllhhhhhhhhhhjjjhhhhhjjjjkkkkkkkkkkkkjd");
			Assert.AreEqual (
@"   aaa aaa
   ggg ggg
   ggg ggg", mode.Text);
		}
		
		[Ignore("Got broken because 'RemoveTrailingWhitespaces' option was removed.")]
		[Test]
		public void DeleteToLineBoundary ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa bbb ccc ddd
   eee fff ggg hhh
   iii jjj kkk lll",
			};
			//move 3 words, delete to line end
			mode.Input ("wwwd$");
			Assert.AreEqual (
@"   aaa bbb 
   eee fff ggg hhh
   iii jjj kkk lll", mode.Text);
			//move down to 0 position, move 3 words, delete to home
			mode.Input ("j0wwwd^");
			Assert.AreEqual (
@"   aaa bbb
   ggg hhh
   iii jjj kkk lll", mode.Text);
			//move down to 0 position, move 3 words, delete to 0
			mode.Input ("j0wwwd0");
			Assert.AreEqual (
@"   aaa bbb
   ggg hhh
kkk lll", mode.Text);
		}
		
		[Test]
		public void VisualMotion ()
		{
			var mode = new TestViEditMode () { Text =
@"    aaa bbb ccc ddd
   eee fff ggg hhh
   iii jjj kkk lll
   mmm nnn ooo ppp
   qqq rrr sss ttt",
			};
			//move 2 lines down, 2 words in, enter visual mode
			mode.Input ("jjwwv");
			mode.AssertSelection (3, 8, 3, 9);
			//2 letters to right
			mode.Input ("ll");
			mode.AssertSelection (3, 8, 3, 11);
			//4 letters to left
			mode.Input ("hhhh");
			mode.AssertSelection (3, 9, 3, 6);
			//1 line up
			mode.Input ("k");
			mode.AssertSelection (3, 9, 2, 6);
			//1 line up
			mode.Input ("k");
			mode.AssertSelection (3, 9, 1, 6);
			//5 letters to right
			mode.Input ("lllll");
			mode.AssertSelection (3, 9, 1, 11);
			//3 lines down
			mode.Input ("jjj");
			mode.AssertSelection (3, 8, 4, 12);
		}
		
		[Test]
		public void KeyNotationRoundTrip ()
		{
			string command = "<C-m>av2f<Space>34<Esc><M-Space><S-C-M-Down>";
			var keys = ViKeyNotation.Parse (command);
			Assert.IsNotNull (keys);
			Assert.AreEqual (11, keys.Count);
			var s = ViKeyNotation.ToString (keys);
			Assert.AreEqual (command, s);
		}
	}
	
	class TestViEditMode : ViEditMode
	{
		public TestViEditMode () : this (new TextEditorData ())
		{
			Data.Options.WordFindStrategy = new Mono.TextEditor.Vi.ViWordFindStrategy ();
		}
		
		//used to prevent edit actions from the HandleKeypress causing Caret/SelectionPositionChanged
		bool inputting;
		
		public TestViEditMode (TextEditorData data)
		{
			data.CurrentMode = this;
			textEditorData = data;
			// Currently doesn't work on my mac (test doesn't terminate with mono 2.6.7).
		//	var f = typeof (EditMode).GetField ("textEditorData", BindingFlags.NonPublic | BindingFlags.Instance);
		//	f.SetValue (this, data);
			
			data.Caret.PositionChanged += delegate(object sender, DocumentLocationEventArgs e) {
				if (!inputting)
					this.CaretPositionChanged ();
			};
			data.SelectionChanged += delegate(object sender, EventArgs e) {
				if (!inputting)
					this.SelectionChanged ();
			};
		}
		
		public new TextEditorData Data {
			get { return base.Data; }
		}
		
		public new TextDocument Document {
			get { return base.Document; }
		}
		
		public new Caret Caret {
			get { return base.Caret; }
		}
		
		public int Col {
			get { return Caret.Column; }
		}
		
		public int Line {
			get { return Caret.Line; }
		}
		
		public string Text {
			get { return Data.Document.Text; }
			set { Data.Document.Text = value; }
		}
		
		public string GetLine ()
		{
			return GetLine (Line);
		}
		
		public string GetLine (int line)
		{
			var seg = Document.GetLine (line);
			return Document.GetTextAt (seg.Offset, seg.Length);
		}
		
		public char GetChar (int offset)
		{
			return Document.GetCharAt (offset);
		}
		
		public char GetChar ()
		{
			return Document.GetCharAt (Caret.Offset);
		}
		
		public void Input (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			inputting = true;
			HandleKeypress (key, unicodeKey, modifier);
			inputting = false;
		}
		
		public void Input (char unicodeKey)
		{
			Input ((Gdk.Key)0, unicodeKey, Gdk.ModifierType.None);
		}
		
		public void Input (string sequence)
		{
			foreach (char c in sequence)
				Input ((Gdk.Key)0, c, Gdk.ModifierType.None);
		}
		
		public void AssertSelection (int anchorLine, int anchorCol, int leadLine, int leadCol)
		{
			var sel = Data.MainSelection;
			Assert.IsNotNull (sel);
			Assert.AreEqual (anchorLine, sel.Anchor.Line);
			Assert.AreEqual (anchorCol, sel.Anchor.Column);
			Assert.AreEqual (leadLine, sel.Lead.Line);
			Assert.AreEqual (leadCol, sel.Lead.Column);
		}
	}
}

