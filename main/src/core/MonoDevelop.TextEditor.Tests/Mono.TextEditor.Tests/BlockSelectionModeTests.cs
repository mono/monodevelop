// 
// BlockSelectionModeTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	class BlockSelectionModeTests : TextEditorTestBase
	{
		[Test]
		public void TestInsertAtCaret ()
		{
			var data = Create (
				@"1234567890
1234<-567890
1234567890
1234567890
1234->$567890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			data.InsertAtCaret ("hello");

			Check (data, @"1234567890
1234hello<-567890
1234hello567890
1234hello567890
1234hello->$567890
1234567890");

		}

		[Test]
		public void TestEditModeInput ()
		{
			var data = Create (
				@"1234567890
1234<-567890
1234567890
1234567890
1234->$567890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			data.CurrentMode = new SimpleEditMode ();
			data.CurrentMode.InternalHandleKeypress (null, data, Gdk.Key.a, (uint)'a', Gdk.ModifierType.None);
			Check (data, @"1234567890
1234a<-567890
1234a567890
1234a567890
1234a->$567890
1234567890");

		}

		[Test]
		public void TestBackspaceWithTabs ()
		{
			var data = Create (
				@"1234567890
" + '\t' + @"1234<-567890
....1234567890
....1234567890
....1234->$567890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			DeleteActions.Backspace (data);
			Check (data, @"1234567890
" + '\t' + @"123<-567890
....123567890
....123567890
....123->$567890
1234567890");
		}

		[Test]
		public void TestBackspace ()
		{
			var data = Create (
				@"1234567890
1234<-567890
1234567890
1234567890
1234->$567890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			DeleteActions.Backspace (data);
			Check (data, @"1234567890
123<-567890
123567890
123567890
123->$567890
1234567890");
		}

		[Test]
		public void TestDelete ()
		{
			var data = Create (
@"1234567890
1234<-567890
1234567890
1234567890
1234->$567890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			DeleteActions.Delete (data);
			Check (data, @"1234567890
1234<-67890
123467890
123467890
1234->$67890
1234567890");
		}

		/// <summary>
		/// Bug 5724 - Tab-indent loses selection in block selection
		/// </summary>
		[Test]
		public void TestBug5724 ()
		{
			var data = Create (
@"1234567890
1234<-567890
1234567890
1234567890
123456->$7890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			MiscActions.InsertTab (data);
			Check (data, 
@"1234567890
1234	<-7890
1234	7890
1234	7890
1234	->$7890
1234567890");
		}

		[Test]
		public void TestEditModeInputWithTabs ()
		{
			var data = Create (
				@"1234567890
" + '\t' + @"<-567890
1234567890
1234567890
1234->$567890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);
			data.CurrentMode = new SimpleEditMode ();
			data.CurrentMode.InternalHandleKeypress (null, data, Gdk.Key.a, (uint)'a', Gdk.ModifierType.None);
			Check (data, @"1234567890
" + '\t' + @"a<-567890
1234a567890
1234a567890
1234a->$567890
1234567890");

		}

		[Test]
		public void TestInsertAtCaretWithTabs ()
		{
			var data = Create (
				@"1234567890
" + '\t' + @"<-567890
1234567890
1234567890
123456->$7890
1234567890");
			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);

			data.InsertAtCaret ("text");
			Check (data, @"1234567890
" + '\t' + @"text<-7890
1234text7890
1234text7890
1234text->$7890
1234567890");

		}

		[Test]
		public void TestPasteBlockSelection ()
		{
			var data = Create (
				@"1234567890
1234<-567890
1234567890
1234567890
1234->$567890
1234567890");

			data.MainSelection = data.MainSelection.WithSelectionMode (SelectionMode.Block);

			var clipboard = Gtk.Clipboard.Get (Mono.TextEditor.ClipboardActions.CopyOperation.CLIPBOARD_ATOM);
			clipboard.Text = "hello";

			ClipboardActions.Paste (data);

			Check (data, @"1234567890
1234hello567890
1234hello567890
1234hello567890
1234hello$567890
1234567890");
		}

	}
}

