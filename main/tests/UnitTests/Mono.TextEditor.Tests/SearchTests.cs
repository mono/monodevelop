//
// SearchTests.cs
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
	public class SearchTests
	{
		[Test()]
		public void TestSearchForward ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = @"ababab";
			data.SearchEngine.SearchRequest.SearchPattern = "ab";
			SearchResult result = data.SearchForward (0);
			Assert.AreEqual (0, result.Offset);
			Assert.AreEqual (2, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			
			result = data.SearchForward (1);
			Assert.AreEqual (2, result.Offset);
			Assert.AreEqual (4, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			
			result = data.SearchForward (4);
			Assert.AreEqual (4, result.Offset);
			Assert.AreEqual (6, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			
			result = data.SearchForward (5);
			Assert.AreEqual (0, result.Offset);
			Assert.AreEqual (2, result.EndOffset);
			Assert.IsTrue (result.SearchWrapped);
		}
		
		[Test()]
		public void TestSearchBackward ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = @"ababab";
			data.SearchEngine.SearchRequest.SearchPattern = "ab";
			SearchResult result = data.SearchBackward (0);
			Assert.AreEqual (4, result.Offset);
			Assert.AreEqual (6, result.EndOffset);
			Assert.IsTrue (result.SearchWrapped);
			
			result = data.SearchBackward (4);
			Assert.AreEqual (2, result.Offset);
			Assert.AreEqual (4, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			
			result = data.SearchBackward (2);
			Assert.AreEqual (0, result.Offset);
			Assert.AreEqual (2, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
		}
		
		[Test()]
		public void TestFindNext ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = @"ababab";
			data.SearchEngine.SearchRequest.SearchPattern = "ab";
			
			data.Caret.Offset = 0;
			SearchResult result = data.FindNext ();
			Assert.AreEqual (0, result.Offset);
			Assert.AreEqual (2, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
			
			result = data.FindNext ();
			Assert.AreEqual (2, result.Offset);
			Assert.AreEqual (4, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
			
			result = data.FindNext ();
			Assert.AreEqual (4, result.Offset);
			Assert.AreEqual (6, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
			
			result = data.FindNext ();
			Assert.AreEqual (0, result.Offset);
			Assert.AreEqual (2, result.EndOffset);
			Assert.IsTrue (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
		}
		
		[Test()]
		public void TestFindPrev ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = @"ababab";
			data.SearchEngine.SearchRequest.SearchPattern = "ab";
			
			data.Caret.Offset = 0;
			SearchResult result = data.FindPrevious ();
			Assert.AreEqual (4, result.Offset);
			Assert.AreEqual (6, result.EndOffset);
			Assert.IsTrue (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
			
			result = data.FindPrevious ();
			Assert.AreEqual (2, result.Offset);
			Assert.AreEqual (4, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
			
			result = data.FindPrevious ();
			Assert.AreEqual (0, result.Offset);
			Assert.AreEqual (2, result.EndOffset);
			Assert.IsFalse (result.SearchWrapped);
			Assert.AreEqual (result.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (result.Length, data.SelectionRange.Length);
			Assert.AreEqual (result.EndOffset, data.Caret.Offset);
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
