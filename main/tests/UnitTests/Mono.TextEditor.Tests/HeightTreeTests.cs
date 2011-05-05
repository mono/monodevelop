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
using UnitTests;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class HeightTreeTests  : TestBase
	{
		[Test()]
		public void TestSimpleLineNumberToY ()
		{
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7";
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual ((i - 1) * editor.LineHeight, heightTree.LineNumberToY (i));
			}
		}

		[Test()]
		public void TestSimpleYToLineNumber ()
		{
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7";
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual (i, heightTree.YToLineNumber ((i - 1) * editor.LineHeight));
			}
		}
		
		[Test()]
		public void TestSetLineHeight ()
		{
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7";
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
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7";
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
				Console.WriteLine ("i:" + i + "/" + j);
				Assert.AreEqual ((j - 1) * editor.LineHeight, heightTree.LineNumberToY (i));
			}
		}
		
		[Test()]
		public void TestFoldYToLineNumber ()
		{
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7";
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
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7\n8";
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			
			heightTree.Fold (1, 3);
			for (int i = 1; i <= editor.LineCount; i++) {
				Console.WriteLine (i + ":"+ heightTree.LineNumberToY (i));
			}
			Assert.AreEqual (0, heightTree.LineNumberToY (2));
			Assert.AreEqual (0, heightTree.LineNumberToY (4));
			Assert.AreEqual (1 * editor.LineHeight, heightTree.LineNumberToY (5));
			Assert.AreEqual (2 * editor.LineHeight, heightTree.LineNumberToY (6));
			Assert.AreEqual (3 * editor.LineHeight, heightTree.LineNumberToY (7));
		}
		
		[Test()]
		public void TestUnfold ()
		{
			var editor = new TextEditorData ();
			editor.Text = "1\n2\n3\n4\n5\n6\n7";
			HeightTree heightTree = new HeightTree (editor);
			heightTree.Rebuild ();
			heightTree.Fold (2, 2);
			heightTree.Unfold (2, 2);
			for (int i = 1; i <= editor.LineCount; i++) {
				Assert.AreEqual ((i - 1) * editor.LineHeight, heightTree.LineNumberToY (i));
				Assert.AreEqual (i, heightTree.YToLineNumber ((i - 1) * editor.LineHeight));
			}
		}
		
		
	}
}

