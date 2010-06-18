// 
// VirtualSpacesTests.cs
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
/*
 * */
namespace Mono.TextEditor.Tests
{
	
	[TestFixture()]
	public class VirtualSpacesTests : UnitTests.TestBase
	{
		/// <summary>
		/// Bug 615196 - Pasting a chunk of text into the virtual whitespace fracks everything up
		/// </summary>
		[Test()]
		public void TestBug615196 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = "\n\nHello World\n";
			data.Caret.Offset = 1; // 2nd.Line
			data.Caret.AllowCaretBehindLineEnd = true;
			data.Caret.Column = 4;
			Clipboard clipboard = Clipboard.Get (Mono.TextEditor.ClipboardActions.CopyOperation.CLIPBOARD_ATOM);
			clipboard.Text = "Test";
			
			ClipboardActions.Paste (data);
			
			Assert.AreEqual ("\n    Test\nHello World\n", data.Document.Text);
		}
	}
}

