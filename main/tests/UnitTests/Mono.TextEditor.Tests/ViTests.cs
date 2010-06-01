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

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class ViTests
	{
		[Test]
		public void ColumnMotion ()
		{
			var mode = new TestViEditMode () { Text = "Test\nText" };
			Assert.AreEqual (0, mode.Col);
			mode.Input ('l');
			Assert.AreEqual (1, mode.Col);
			mode.Input ("ll");
			Assert.AreEqual (3, mode.Col);
			mode.Input ('l');
			Assert.AreEqual (3, mode.Col);
			mode.Input ("hh");
			Assert.AreEqual (1, mode.Col);
			mode.Input ('h');
			Assert.AreEqual (0, mode.Col);
			mode.Input ('h');
			Assert.AreEqual (0, mode.Col);
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
			Assert.AreEqual (0, mode.Line);
			mode.Input ('j');
			Assert.AreEqual (1, mode.Line);
			mode.Input ('k');
			Assert.AreEqual (0, mode.Line);
			mode.Input ("jjjj");
			Assert.AreEqual (3, mode.Line);
			mode.Caret.Line = 1;
			mode.Caret.Column = 11;
			mode.Input ('j');
			Assert.AreEqual (4, mode.Col);
			mode.Input ('k');
			Assert.AreEqual (11, mode.Col);
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
			Assert.AreEqual (1, mode.Line);
			Assert.AreEqual (3, mode.Col);
			Assert.AreEqual (
@"   aaa aaa
   ccc ccc", mode.Text);
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
		
		[TestFixtureSetUp] 
		public void SetUp()
		{
			Gtk.Application.Init ();
		}
		
		[TestFixtureTearDown] 
		public void Dispose()
		{
		}
	}
	
	class TestViEditMode : Mono.TextEditor.Vi.ViEditMode
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
			var f = typeof (EditMode).GetField ("textEditorData", BindingFlags.NonPublic | BindingFlags.Instance);
			f.SetValue (this, data);
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
		
		public new Document Document {
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
			return Document.GetTextAt (seg.Offset, seg.EditableLength);
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
	}
}

