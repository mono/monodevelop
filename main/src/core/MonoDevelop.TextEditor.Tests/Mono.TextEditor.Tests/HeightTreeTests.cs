// 
// HeightTreeTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	class HeightTreeTests 
	{
		public static TextEditorData Create (string content)
		{
			return new TextEditorData (new TextDocument(content));
		}

		[Test()]
		public void TestSimpleLineNumberToY ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual ((i - 1) * editor.LineHeight, heightTree.LineNumberToY (i));
			}
		}

		[Test()]
		public void TestSimpleYToLineNumber ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual (i, heightTree.YToLineNumber ((i - 1) * editor.LineHeight));
			}
		}
		
		[Test()]
		public void TestYToLineNumberWithFolds ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7\n8\n9\n0");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			heightTree.Fold (1, 2);
			heightTree.Fold (6, 3);
			heightTree.Fold (5, 5);
			
			Assert.AreEqual (1, heightTree.YToLineNumber (0 * editor.LineHeight));
			Assert.AreEqual (4, heightTree.YToLineNumber (1 * editor.LineHeight));
			Assert.AreEqual (5, heightTree.YToLineNumber (2 * editor.LineHeight));
		}
		
		[Test()]
		public void TestLineNumberToYWithFolds ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7\n8\n9\n0");
			
			//123
			//4
			//5[678]90
			
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			heightTree.Fold (1, 2);
			heightTree.Fold (6, 3);
			heightTree.Fold (5, 5);
			Assert.AreEqual (0 * editor.LineHeight, heightTree.LineNumberToY (1));
			Assert.AreEqual (0 * editor.LineHeight, heightTree.LineNumberToY (2));
			Assert.AreEqual (0 * editor.LineHeight, heightTree.LineNumberToY (3));
			Assert.AreEqual (1 * editor.LineHeight, heightTree.LineNumberToY (4));
			for (int i = 5; i <= 10; i++)
				Assert.AreEqual (2 * editor.LineHeight, heightTree.LineNumberToY (i));
		}
		
		[Test()]
		public void TestSetLineHeight ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			for (int i = 1; i <= editor.LineCount; i += 2) {
				heightTree.SetLineHeight (i, editor.LineHeight * 2);
			}
			
			double y = 0;
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual (y, heightTree.LineNumberToY (i));
				y += i % 2 == 0 ? editor.LineHeight : editor.LineHeight * 2;
			}
			
			y = 0;
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual (i, heightTree.YToLineNumber (y));
				y += i % 2 == 0 ? editor.LineHeight : editor.LineHeight * 2;
			}
			
		}
		
		[Test()]
		public void TestFoldLineNumberToYCase1 ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			
			heightTree.Fold (2, 2);
			
			for (int i = 1; i <= editor.LineCount; i++) {
				int j = i;
				if (j >= 2) {
					if (j <= 4) {
						j = 2;
					} else { 
						j -= 2;
					}
				}
				Assert.AreEqual ((j - 1) * editor.LineHeight, heightTree.LineNumberToY (i));
			}
		}
		
		[Test()]
		public void TestFoldYToLineNumber ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			
			heightTree.Fold (2, 2);
			
			for (int i = 1; i <= editor.LineCount; i++) {
				int j = i;
				if (j >= 2) {
					if (j <= 4) {
						j = 2;
					} else { 
						j -= 2;
					}
				}
				
				int k;
				if (i >= 2 && i <= 4) {
					k = 2;
				} else {
					k = i;
				}
				
				Assert.AreEqual (k, heightTree.YToLineNumber ((j - 1) * editor.LineHeight));
			}
		}
		
		[Test()]
		public void TestFoldLineNumberToY ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7\n8");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			
			heightTree.Fold (1, 3);
			Assert.AreEqual (0, heightTree.LineNumberToY (2));
			Assert.AreEqual (0, heightTree.LineNumberToY (4));
			Assert.AreEqual (1 * editor.LineHeight, heightTree.LineNumberToY (5));
			Assert.AreEqual (2 * editor.LineHeight, heightTree.LineNumberToY (6));
			Assert.AreEqual (3 * editor.LineHeight, heightTree.LineNumberToY (7));
		}
		
		[Test()]
		public void TestCoordinatesAfterFolding ()
		{
			var editor = new TextEditorData ();
			for (int i = 0; i < 100; i++)
				editor.Insert (0, "line\n");
			
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			
			heightTree.Fold (1, 2);
			
			heightTree.Fold (6, 4);
			heightTree.Fold (5, 10);
			
			var f = heightTree.Fold (20, 8);
			heightTree.Unfold (f, 20, 8);
			
			heightTree.Fold (20, 4);
			heightTree.Fold (25, 4);
			
			for (int i = 50; i <= editor.LineCount; i++) {
				var y = System.Math.Max (0, (i - 3 - 10 - 8) * editor.LineHeight);
				Assert.AreEqual (y, heightTree.LineNumberToY (i), "line:" + i + " --> y:" + y);
				Assert.AreEqual (i, heightTree.YToLineNumber (y), "y:" + y + " --> line:" + i);
			}
			for (int i = 50; i <= editor.LineCount; i++) {
				var y = System.Math.Max (0, (i - 3 - 10 - 8) * editor.LineHeight);
				Assert.AreEqual (y, heightTree.LineNumberToY (i), "line:" + i + " --> y:" + y);
				Assert.AreEqual (i, heightTree.YToLineNumber (y), "y:" + y + " --> line:" + i);
			}
		}
		
		[Test()]
		public void TestUnfold ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			var f = heightTree.Fold (2, 2);
			heightTree.Unfold (f, 2, 2);
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual ((i - 1) * editor.LineHeight, heightTree.LineNumberToY (i));
				Assert.AreEqual (i, heightTree.YToLineNumber ((i - 1) * editor.LineHeight));
			}
		}

		/// <summary>
		/// Bug 4839 - Hitting enter on last line of document makes editor scroll to top
		/// </summary>
		[Test()]
		public void TestBug4839 ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			editor.Caret.Offset = editor.Text.Length;
			var heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			MiscActions.InsertNewLine (editor);
			Assert.AreEqual ((editor.LineCount - 1) * editor.LineHeight, heightTree.LineNumberToY (editor.LineCount));
		}

		[Test()]
		public void TestBug4839MultipleNewLines ()
		{
			var editor = Create ("1\n2\n3\n4\n5\n6\n7");
			editor.Caret.Offset = editor.Text.Length;
			var heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			MiscActions.InsertNewLine (editor);
			MiscActions.InsertNewLine (editor);
			Assert.AreEqual ((editor.LineCount - 1) * editor.LineHeight, heightTree.LineNumberToY (editor.LineCount));
		}

	}
}

