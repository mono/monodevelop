// 
// VirtualInputModeTests.cs
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
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using Gtk;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class VirtualIndentModeTests
	{
		TextEditorData CreateData (string content)
		{
			var data = new TextEditorData (new TextDocument (content));
			data.IndentationTracker = SmartIndentModeTests.IndentTracker;
			data.Options.IndentStyle = IndentStyle.Virtual;
			return data;
		}

		[Test()]
		public void TestIndentNewLine ()
		{
			var data = CreateData ("\n\n\n");
			data.Caret.Offset = data.Document.GetLine (2).Offset;
			
			MiscActions.InsertNewLine (data);
			
			Assert.AreEqual ("\n\n\n\n", data.Document.Text);
			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
		}

		[Test()]
		public void TestLineEndBehavior ()
		{
			var data = CreateData ("\n\n\n");
			data.Caret.Offset = data.Document.GetLine (2).Offset;

			CaretMoveActions.LineEnd (data);
			
			Assert.AreEqual ("\n\n\n", data.Document.Text);
			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
		}

		[Test()]
		public void TestDesiredColumnCaretDown ()
		{
			var data = CreateData ("12345\n\n12345\n");
			data.Caret.Column = 4;
			Assert.AreEqual (4, data.Caret.DesiredColumn);

			CaretMoveActions.Down (data);
			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
			CaretMoveActions.Down (data);
			
			Assert.AreEqual (4, data.Caret.Column);
			Assert.AreEqual (4, data.Caret.DesiredColumn);
		}


		[Test()]
		public void TestDesiredColumnCaretUp ()
		{
			var data = CreateData ("12345\n\n12345\n");
			data.Caret.Line = 3;
			data.Caret.Column = 4;
			Assert.AreEqual (4, data.Caret.DesiredColumn);

			CaretMoveActions.Up (data);
			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
			CaretMoveActions.Up (data);
			
			Assert.AreEqual (4, data.Caret.Column);
			Assert.AreEqual (4, data.Caret.DesiredColumn);
		}

		[Test()]
		public void TestVirtualColumnDesiredColumnCaretUp ()
		{
			var data = CreateData ("12345\n\n12345\n");
			CaretMoveActions.Down (data);
			CaretMoveActions.Right (data);
			CaretMoveActions.Left (data);
			Assert.AreEqual (DocumentLocation.MinColumn, data.Caret.Column);
			Assert.AreEqual (DocumentLocation.MinColumn, data.Caret.DesiredColumn);
			CaretMoveActions.Right (data);
			CaretMoveActions.Up (data);
			CaretMoveActions.Down (data);


			int indentColumn = data.IndentationTracker.GetVirtualIndentationColumn (2, 1);
			Assert.AreEqual (indentColumn, data.Caret.Column);
			Assert.AreEqual (data.LogicalToVisualLocation (2, indentColumn).Column, data.Caret.DesiredColumn);
		}

		[Test()]
		public void TestVirtualColumnDesiredColumnCaretDown ()
		{
			var data = CreateData ("12345\n\n12345\n");
			var segs = new List<FoldSegment> ();
			segs.Add (new FoldSegment (data.Document, "", 5, 5, FoldingType.Region));
			data.Document.UpdateFoldSegments (segs);
			CaretMoveActions.Down (data);
			CaretMoveActions.Right (data);
			CaretMoveActions.Left (data);
			Assert.AreEqual (DocumentLocation.MinColumn, data.Caret.Column);
			Assert.AreEqual (DocumentLocation.MinColumn, data.Caret.DesiredColumn);
			CaretMoveActions.Right (data);
			CaretMoveActions.Down (data);
			CaretMoveActions.Up (data);

			int indentColumn = data.IndentationTracker.GetVirtualIndentationColumn (2, 1);
			Assert.AreEqual (indentColumn, data.Caret.Column);
			Assert.AreEqual (data.LogicalToVisualLocation (2, indentColumn).Column, data.Caret.DesiredColumn);
		}

		[Test()]
		public void TestCaretRightBehavior ()
		{
			var data = CreateData ("\n\n\n");
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (1, data.IndentationTracker.GetVirtualIndentationColumn (2, 1)), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (2, 1), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (2, data.IndentationTracker.GetVirtualIndentationColumn (2, 1)), data.Caret.Location);
		}

		[Test()]
		public void TestCaretRightBehaviorInNonEmptyLines ()
		{
			var data = CreateData ("12\n\n\n");
			CaretMoveActions.Right (data);
			CaretMoveActions.Right (data);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (2, 1), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (2, data.IndentationTracker.GetVirtualIndentationColumn (2, 1)), data.Caret.Location);
		}

		[Test()]
		public void TestBackspaceRightBehavior ()
		{
			var data = CreateData ("test\n\n\n");
			data.Caret.Location = new DocumentLocation (2, data.IndentationTracker.GetVirtualIndentationColumn (2, 1));
			DeleteActions.Backspace (data);
			Assert.AreEqual (new DocumentLocation (2, data.IndentationTracker.GetVirtualIndentationColumn (2, 1) - 1), data.Caret.Location);
			Assert.AreEqual ("test\n\t\n\n", data.Document.Text);
		}

		[Test()]
		public void TestBackspaceSelectionBehavior ()
		{
			var data = CreateData ("\n\t\ttest\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			SelectionActions.MoveUp (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			Assert.IsTrue (data.IsSomethingSelected);
			DeleteActions.Backspace (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			Assert.AreEqual ("\t\ttest\n\n", data.Document.Text);
		}

		[Test()]
		public void TestDeleteSelectionBehavior ()
		{
			var data = CreateData ("\n\t\ttest\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			SelectionActions.MoveUp (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			Assert.IsTrue (data.IsSomethingSelected);
			DeleteActions.Delete (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			Assert.AreEqual ("\t\ttest\n\n", data.Document.Text);
		}

		[Test()]
		public void TestDeleteBehavior ()
		{
			var data = CreateData ("\n\t\ttest");
			data.Caret.Location = new DocumentLocation (2, 3);
			CaretMoveActions.Up (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			DeleteActions.Delete (data);
			Assert.AreEqual ("\t\t\t\ttest", data.Document.Text);
		}

		[Test()]
		public void TestAutoRemoveIndent ()
		{
			var data = CreateData ("\n\t\t\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			
			data.InsertAtCaret (" ");
			Assert.AreEqual ("\n\t\t \n\n", data.Document.Text);
			DeleteActions.Backspace (data);
			Assert.AreEqual ("\n\n\n", data.Document.Text);

			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
		}
		
		[Test()]
		public void TestAutoRemoveIndentOnReturn ()
		{
			var data = CreateData ("\n\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			MiscActions.InsertNewLine (data);
			MiscActions.InsertNewLine (data);
			Assert.AreEqual (3, data.Caret.Column);
			Assert.AreEqual ("\n\n\n\n\n", data.Document.Text);
		}
		
		[Test()]
		public void TestAutoRemoveExistingIndentOnReturn ()
		{
			var data = CreateData ("\n\t\t\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			MiscActions.InsertNewLine (data);
			MiscActions.InsertNewLine (data);
			Assert.AreEqual (3, data.Caret.Column);
			Assert.AreEqual ("\n\n\n\n\n", data.Document.Text);
		}

		[Test()]
		public void TestRemoveExistingIndentWithBackspace ()
		{
			var data = CreateData ("\n\t\t\t\n\n");
			data.Caret.Location = new DocumentLocation (2, 4);
			DeleteActions.Backspace (data);
			Assert.AreEqual (3, data.Caret.Column);
			Assert.AreEqual ("\n\n\n", data.Document.Text);
		}

		[Test()]
		public void TestRemoveLastTabInLine ()
		{
			var data = CreateData ("\n\t\n\n");
			data.Caret.Location = new DocumentLocation (2, 2);
			DeleteActions.Backspace (data);
			Assert.AreEqual ("\n\n\n", data.Document.Text);
			Assert.AreEqual (1, data.Caret.Column);
		}

		[Test()]
		public void TestAutoRemoveIndentNotRemovingOnCaretMove ()
		{
			var data = CreateData ("\n\t\t\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			CaretMoveActions.Down (data);
			Assert.AreEqual ("\n\t\t\n\n", data.Document.Text);
			CaretMoveActions.Up (data);
			Assert.AreEqual ("\n\t\t\n\n", data.Document.Text);
			Assert.AreEqual (new DocumentLocation (2, 3), data.Caret.Location);
		}

		
		[Test()]
		public void TestUndoRedo ()
		{
			var data = CreateData ("");
			data.Caret.Location = new DocumentLocation (1, 3);
			data.InsertAtCaret ("1");
			data.InsertAtCaret ("2");
			data.InsertAtCaret ("3");
			Assert.AreEqual ("\t\t123", data.Document.Text);

			Assert.IsTrue (data.Document.CanUndo);
			data.Document.Undo ();
			data.Document.Undo ();
			data.Document.Undo ();
			Assert.IsFalse (data.Document.CanUndo);

			Assert.AreEqual (data.Document.Text, "");
			data.Document.Redo ();
			data.Document.Redo ();
			data.Document.Redo ();
			Assert.AreEqual ("\t\t123", data.Document.Text);

		}
		
		[Test()]
		public void TestRemoveDoesntBreakCaretPosition ()
		{
			var data = CreateData ("Hello\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			
			data.Remove (0, "Hello".Length);

			Assert.AreEqual ("\n\n", data.Document.Text);
			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
		}
		
		[Test()]
		public void TestInsertDoesntBreakCaretPosition ()
		{
			var data = CreateData ("\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			
			data.Insert (0, "Hello");

			Assert.AreEqual ("Hello\n\n", data.Document.Text);
			Assert.AreEqual (data.IndentationTracker.GetVirtualIndentationColumn (2, 1), data.Caret.Column);
		}


		[Test()]
		public void TestReturnOnNonEmptyLine ()
		{
			var data = CreateData ("\n\t\tFoo\t\tBar\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			MiscActions.InsertNewLine (data);
			Assert.AreEqual (3, data.Caret.Column);
			Assert.AreEqual ("\n\n\t\tFoo\t\tBar\n\n", data.Document.Text);
		}

		[Test()]
		public void TestTabBehavior ()
		{
			var data = CreateData ("\n\n\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			MiscActions.InsertTab (data);
			Assert.AreEqual ("\n\t\t\t\n\n", data.Document.Text);
		}

		/// <summary>
		/// Bug 5067 - Selection does not respect virtual space
		/// </summary>
		[Test()]
		public void TestBug5067 ()
		{
			var data = CreateData ("\n\n\t\tFoo ();\n");
			data.Caret.Location = new DocumentLocation (2, 3);
			SelectionActions.MoveDown (data);
			DeleteActions.Delete (data);

			Assert.AreEqual ("\n\t\tFoo ();\n", data.Document.Text);
		}

		TextEditorData CreateDataWithSpaces (string content)
		{
			var data = new TextEditorData (new TextDocument (content));
			data.IndentationTracker = new SmartIndentModeTests.TestIndentTracker ("        ");
			data.Options = new TextEditorOptions () {
				TabsToSpaces = true,
				IndentStyle = IndentStyle.Virtual
			};
			return data;
		}

		[Test()]
		public void TestIndentWithSpaces ()
		{
			var data = CreateDataWithSpaces ("\n        \n\n");
			data.Caret.Location = new DocumentLocation (2, 9);
			MiscActions.InsertNewLine (data);
			Assert.AreEqual ("\n\n\n\n", data.Document.Text);
		}

		/// <summary>
		/// Bug 5402 - Backspace doesn't work with 1-tab virtual indent
		/// </summary>
		[Test()]
		public void TestBug5402 ()
		{
			var data = CreateData ("\t");
			data.IndentationTracker = new SmartIndentModeTests.TestIndentTracker ("\t");
			data.Caret.Location = new DocumentLocation (1, 2);
			DeleteActions.Backspace (data);
			Assert.AreEqual ("", data.Document.Text);
			Assert.AreEqual (new DocumentLocation (1, 1), data.Caret.Location);
		}

		/// <summary>
		/// Bug 5949 - Movement across empty line is not symmetric
		/// </summary>
		[Test()]
		public void TestBug5949 ()
		{
			var data = CreateData ("\t\tfoo\n\n\t\tbar");
			data.Caret.Location = new DocumentLocation (3, 1);
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (2, 3), data.Caret.Location);
		}

		/// <summary>
		/// Bug 5956 - Backspacing ignores virtual indents
		/// </summary>
		[Test()]
		public void TestBug5956 ()
		{
			var data = CreateData ("\t\tfoo\n\n\t\tbar");
			data.Caret.Location = new DocumentLocation (3, 1);
			DeleteActions.Backspace (data);
			Assert.AreEqual (new DocumentLocation (2, 3), data.Caret.Location);
			Assert.AreEqual ("\t\tfoo\n\t\t\t\tbar", data.Document.Text);
		}

		/// <summary>
		/// Bug 7012 - Paste does not replace entire selection made by the Shift key.
		/// </summary>
		[Ignore("Test broken.")]
		[Test()]
		public void TestBug7012 ()
		{
			var data = CreateData ("\n\t\tthrow new NotImplementedException();");
			var loc1 = new DocumentLocation (1, data.IndentationTracker.GetVirtualIndentationColumn (1, 1));
			var loc2 = new DocumentLocation (3, data.GetLine (2).Length);
			data.Caret.Location = loc2;
			data.SetSelection (loc1, loc2);
			var clipboard = Clipboard.Get (Mono.TextEditor.ClipboardActions.CopyOperation.CLIPBOARD_ATOM);
			clipboard.Text = "Console.WriteLine(\"Hello\");";
			
			ClipboardActions.Paste (data);
			
			Assert.AreEqual ("\t\tConsole.WriteLine(\"Hello\");", data.Document.Text);
		}
	}
}

