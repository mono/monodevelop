// 
// SmartInsertModeTests.cs
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class SmartIndentModeTests
	{
		internal readonly static IndentationTracker IndentTracker = new TestIndentTracker ();

		internal class TestIndentTracker : IndentationTracker
		{
			string indentString;

			public override IndentationTrackerFeatures SupportedFeatures {
				get {
					return IndentationTrackerFeatures.All;
				}
			}

			public TestIndentTracker (string indentString = "\t\t")
			{
				this.indentString = indentString;
			}

			public override string GetIndentationString (int lineNumber)
			{
				return indentString;
			}
		}

		TextEditorData CreateData (string content)
		{
			var data = new TextEditorData (new TextDocument (content));
			data.IndentationTracker = IndentTracker;
			data.Options.IndentStyle = IndentStyle.Smart;
			return data;
		}

		[Test()]
		public void TestIndentNewLine ()
		{
			var data = CreateData ("\n\n\n");
			data.Caret.Offset = data.Document.GetLine (2).Offset;
			
			MiscActions.InsertNewLine (data);
			
			Assert.AreEqual ("\n\n\t\t\n\n", data.Document.Text);
			Assert.AreEqual (data.Document.GetLine (3).Offset + 2, data.Caret.Offset);
		}

		[Test()]
		public void TestLineEndBehavior ()
		{
			var data = CreateData ("\n\n\n");
			data.Caret.Offset = data.Document.GetLine (2).Offset;

			CaretMoveActions.LineEnd (data);
			
			Assert.AreEqual ("\n\n\n", data.Document.Text);
			Assert.AreEqual (data.Document.GetLine (2).Offset, data.Caret.Offset);
		}

		[Test()]
		public void TestDesiredColumnCaretDown ()
		{
			var data = CreateData ("12345\n\n12345\n");
			data.Caret.Column = 4;
			Assert.AreEqual (4, data.Caret.DesiredColumn);

			CaretMoveActions.Down (data);
			Assert.AreEqual (1, data.Caret.Column);
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
			Assert.AreEqual (1, data.Caret.Column);
			CaretMoveActions.Up (data);
			
			Assert.AreEqual (4, data.Caret.Column);
			Assert.AreEqual (4, data.Caret.DesiredColumn);
		}

		[Test()]
		public void TestCaretRightBehavior ()
		{
			var data = CreateData ("\n\n\n");
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (2, 1), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (3, 1), data.Caret.Location);
		}


		/// <summary>
		/// Bug 53878 - Insert matching brace does not indent properly
		/// </summary>
		[Test]
		public void TestBug53878 ()
		{
			var data = CreateData ("    FooBar\n    Foo {}");
			data.Caret.Offset = data.Document.GetLine (2).EndOffset - 1;
			data.Options.IndentStyle = IndentStyle.Auto;
			MiscActions.InsertNewLine (data);

			Assert.AreEqual ("    FooBar\n    Foo {\n    }", data.Document.Text);
			Assert.AreEqual (data.Document.GetLine (3).EndOffset - 1, data.Caret.Offset);
		}

	}
}

