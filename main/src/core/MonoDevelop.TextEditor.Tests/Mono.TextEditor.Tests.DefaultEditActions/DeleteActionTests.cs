// 
// DeleteActionTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class DeleteActionTests : TextEditorTestBase
	{
		[Test]
		public void TestBackspace ()
		{
			var data = Create (@"1234$567890");
			DeleteActions.Backspace (data);
			Check (data, @"123$567890");
		}
		
		[Test]
		public void TestBackspaceCase1 ()
		{
			var data = Create (@"$1234567890");
			DeleteActions.Backspace (data);
			Check (data, @"$1234567890");
		}
		
		[Test]
		public void TestDelete ()
		{
			var data = Create (@"1234$567890");
			DeleteActions.Delete (data);
			Check (data, @"1234$67890");
		}
		
		[Test]
		public void TestBackspaceDeleteCase1 ()
		{
			var data = Create (@"1234567890$");
			DeleteActions.Delete (data);
			Check (data, @"1234567890$");
		}
		
		[Test]
		public void TestDeleteCaretLine ()
		{
			var data = Create (@"1234567890
1234$67890
1234567890");
			DeleteActions.CaretLine (data);
			Assert.AreEqual (@"1234567890
1234567890", data.Document.Text);
		}

		[Test]
		public void TestDeleteCaretLineWithFoldings ()
		{
			var data = Create (@"1234567890
1234$678+[90
1234567890
123456789]0
1234567890");
			DeleteActions.CaretLine (data);
			Assert.AreEqual (@"1234567890
1234567890", data.Document.Text);
		}
		
		[Test]
		public void TestDeleteCaretLineWithFoldingsCase2 ()
		{
			var data = Create (@"1234567890
12+[3467890
12]34$567+[890
123456789]0
1234567890");
			DeleteActions.CaretLine (data);
			Assert.AreEqual (@"1234567890
1234567890", data.Document.Text);
		}

		[Test]
		public void TestDeleteCaretLineWithSelection ()
		{
			var data = Create (@"1234567890
1234$<-67890
1234567890
12345->67890
1234567890");
			DeleteActions.CaretLine (data);
			Assert.AreEqual (@"1234567890
1234567890", data.Document.Text);
		}

		[Test]
		public void TestDeleteCaretLineToStart ()
		{
			var data = Create (@"1234567890
1234$67890
1234567890");
			DeleteActions.CaretLineToStart (data);
			Assert.AreEqual (@"1234567890
67890
1234567890", data.Document.Text);
		}

		[Test]
		public void TestDeleteCaretLineToStartWithFoldings ()
		{
			var data = Create (@"1234567890
1234+[567890
1234567890
123]4$67890
1234567890");
			DeleteActions.CaretLineToStart (data);
			Assert.AreEqual (@"1234567890
67890
1234567890", data.Document.Text);
		}

		[Test]
		public void TestDeleteCaretLineToEnd ()
		{
			var data = Create (@"1234567890
1234$67890
1234567890");
			DeleteActions.CaretLineToEnd (data);
			Assert.AreEqual (@"1234567890
1234
1234567890", data.Document.Text);
		}

		[Test]
		public void TestDeleteCaretLineToEndWithFoldings ()
		{
			var data = Create (@"1234567890
1234$678+[90
1234567890
123456789]0
1234567890");
			DeleteActions.CaretLineToEnd (data);
			Assert.AreEqual (@"1234567890
1234
1234567890", data.Document.Text);
		}



		[Test]
		public void TestDeletePreviousWord ()
		{
			var data = Create (@"      word1 word2 word3$");
			DeleteActions.PreviousWord (data);
			Check (data, @"      word1 word2 $");
			DeleteActions.PreviousWord (data);
			Check (data, @"      word1 $");
			DeleteActions.PreviousWord (data);
			Check (data, @"      $");
		}
		
		[Test]
		public void TestDeletePreviousSubword ()
		{
			var data = Create (@"      SomeLongWord$");
			DeleteActions.PreviousSubword (data);
			Check (data, @"      SomeLong$");
			DeleteActions.PreviousSubword (data);
			Check (data, @"      Some$");
			DeleteActions.PreviousSubword (data);
			Check (data, @"      $");
		}

		[Test]
		public void TestDeleteNextWord ()
		{
			var data = Create (@"      $word1 word2 word3");
			DeleteActions.NextWord (data);
			Check (data, @"      $ word2 word3");
			DeleteActions.NextWord (data);
			Check (data, @"      $ word3");
			DeleteActions.NextWord (data);
			Check (data, @"      $");
		}

		[Test]
		public void TestDeleteNextSubword ()
		{
			var data = Create (@"      $SomeLongWord");
			DeleteActions.NextSubword (data);
			Check (data, @"      $LongWord");
			DeleteActions.NextSubword (data);
			Check (data, @"      $Word");
			DeleteActions.NextSubword (data);
			Check (data, @"      $");
		}
	}
}
