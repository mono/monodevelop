//
// UndoRedoTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class UndoRedoTests
	{
		static TextEditorData Create (string content)
		{
			return new TextEditorData (new TextDocument (content));
		}

		[Test()]
		public void TestSimpleUndo ()
		{
			TextEditorData data = Create ("Hello");
			Assert.IsFalse (data.Document.CanUndo);
			data.Caret.Offset = data.Document.Length;
			data.InsertAtCaret ("World");
			Assert.IsTrue (data.Document.CanUndo);
			data.Document.Undo ();
			Assert.IsFalse (data.Document.CanUndo);
			Assert.AreEqual (data.Document.Text, "Hello");
			Assert.AreEqual (data.Document.Length, data.Caret.Offset);
		}
		
		[Test()]
		public void TestSimpleRedo ()
		{
			TextEditorData data = Create ("Hello");
			Assert.IsFalse (data.Document.CanUndo);
			data.Caret.Offset = data.Document.Length;
			data.InsertAtCaret ("World");
			Assert.AreEqual (data.Caret.Offset, data.Document.Length);
			Assert.IsTrue (data.Document.CanUndo);
			data.Document.Undo ();
			Assert.IsFalse (data.Document.CanUndo);
			data.Document.Redo ();
			Assert.IsTrue (data.Document.CanUndo);
			
			Assert.AreEqual (data.Document.Text, "HelloWorld");
			Assert.AreEqual (data.Document.Length, data.Caret.Offset);
		}
	}
}
