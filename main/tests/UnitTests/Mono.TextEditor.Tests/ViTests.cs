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
			var data = new TextEditorData  ();
			data.Document.Text = "Test";
			var mode = new TestViEditMode (data);
			data.CurrentMode = mode;
			
			Assert.AreEqual (0, data.Caret.Offset);
			mode.Input ('l');
			Assert.AreEqual (1, data.Caret.Offset);
			mode.Input ('l');
			Assert.AreEqual (2, data.Caret.Offset);
			mode.Input ('h');
			Assert.AreEqual (1, data.Caret.Offset);
			mode.Input ('h');
			Assert.AreEqual (0, data.Caret.Offset);
		}
	}
	
	//allows access to protected members
	class TestViEditMode : Mono.TextEditor.Vi.ViEditMode
	{
		public TestViEditMode (TextEditorData data)
		{
			var f = typeof (EditMode).GetField ("textEditorData", BindingFlags.NonPublic | BindingFlags.Instance);
			f.SetValue (this, data);
		}
		
		public void Input (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			HandleKeypress (key, unicodeKey, modifier);
		}
		
		public void Input (char unicodeKey)
		{
			HandleKeypress ((Gdk.Key)0, unicodeKey, Gdk.ModifierType.None);
		}
	}
}

