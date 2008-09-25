//
// SelectionSelectAllTests.cs
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
	public class SelectionSelectAllTests
	{
		[Test()]
		public void TestSelectAll ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123456789
123456789
123456789
123456789
123456789
123456789";
			
			Assert.IsFalse (data.IsSomethingSelected);
			new SelectionSelectAll ().Run (data);
			Assert.IsTrue (data.IsSomethingSelected);
			
			Assert.AreEqual (data.SelectionRange.Offset, 0);
			Assert.AreEqual (data.SelectionRange.EndOffset, data.Document.Length);
			Assert.AreEqual (data.SelectionRange.EndOffset, data.Caret.Offset);
		}
		
		
		// Bug 362983 - Text selected with Select All can't be unselected
		// Open a file, press ctrl+a to select all text. Move the cursor or click anywhere
		// in the file. The selection is never erased.
		[Test()]
		public void TestSelectAllBug362983 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = "Test";
			Assert.IsFalse (data.IsSomethingSelected);
			new SelectionSelectAll ().Run (data);
			Assert.IsTrue (data.IsSomethingSelected);
			data.Caret.Offset = 0;
			Assert.IsFalse (data.IsSomethingSelected);
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
}
