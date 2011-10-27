// 
// EditActionsTest.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace Mono.TextEditor.Tests
{
	
	[TestFixture()]
	public class EditActionsTest
	{
		/// <summary>
		/// Bug 615191 - When using multiline selection to indent/outdent, the indenter selects too much
		/// </summary>
		[Test()]
		public void TestBug615196_IndentCase ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = "\n\n\n\n\n";
			data.Caret.Offset = data.Document.GetLine (2).Offset; // 2nd.Line
			data.MainSelection = new Selection (2, 1, 4, 1);
			MiscActions.InsertTab (data);
			MiscActions.InsertTab (data);
			
			Assert.AreEqual ("\n\t\t\n\t\t\n\n\n", data.Document.Text);
		}
		
		[Test()]
		public void TestBug615196_UnIndentCase ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = "\n\t\t\n\t\t\n\t\t\n\n";
			data.Caret.Offset = data.Document.GetLine (2).Offset; // 2nd.Line
			data.MainSelection = new Selection (2, 1, 4, 1);
			MiscActions.RemoveTab (data);
			MiscActions.RemoveTab (data);
			
			Assert.AreEqual ("\n\n\n\t\t\n\n", data.Document.Text);
		}
	
		
		/// <summary>
		/// Bug 1700 - Tab in multi-line edit changes messes up the selection
		/// </summary>
		[Test()]
		public void TestBug1700 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Document.Text = "123\n123\n123";
			data.MainSelection = new Selection (1, 2, 3, 2, SelectionMode.Block);
			MiscActions.InsertTab (data);
			
			Assert.AreEqual ("1\t23\n1\t23\n1\t23", data.Document.Text);
		}
	}
}

