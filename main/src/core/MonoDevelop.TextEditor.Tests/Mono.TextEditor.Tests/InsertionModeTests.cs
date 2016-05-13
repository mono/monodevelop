// 
// InsertionModeTests.cs
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
using System.Collections.Generic;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	class InsertionModeTests : TextEditorTestBase
	{
		string CreateInsertionPoint (string input, string text, NewLineInsertion before, NewLineInsertion after)
		{
			int idx = input.IndexOf ('$');
			Assert.Greater (idx, -1);
			TextEditorData data = new TextEditorData (new TextDocument (input.Substring (0, idx) + input.Substring (idx + 1)));
			InsertionPoint point = new InsertionPoint (data.Document.OffsetToLocation (idx), before, after);
			point.Insert (data, text);
			return data.Document.Text;
		}
		
		[Test()]
		public void TestInsertionPointClassStart ()
		{
			string test = CreateInsertionPoint (@"class Test
{
$	void TestMethod ()
	{
	}
}", "\tint a;", NewLineInsertion.None, NewLineInsertion.BlankLine);
			Assert.AreEqual (@"class Test
{
	int a;

	void TestMethod ()
	{
	}
}", test);
		}
		
		[Test()]
		public void TestInsertionPointClassEnd ()
		{
			string test = CreateInsertionPoint (@"class Test
{
	void TestMethod ()
	{
	}
$}", "\tint a;", NewLineInsertion.Eol, NewLineInsertion.Eol);
			Assert.AreEqual (@"class Test
{
	void TestMethod ()
	{
	}

	int a;
}", test);
		}
		
		[Test()]
		public void TestInsertionPointClassMiddle ()
		{
			string test = CreateInsertionPoint (@"class Test
{
	void TestMethod1 ()
	{
	}
$	void TestMethod2 ()
	{
	}
}", "\tint a;", NewLineInsertion.Eol, NewLineInsertion.BlankLine);
			Assert.AreEqual (@"class Test
{
	void TestMethod1 ()
	{
	}

	int a;

	void TestMethod2 ()
	{
	}
}", test);
		}
		
		/// <summary>
		/// Bug 683011 - Implement interface may insert region in the wrong spot
		/// </summary>
		[Test()]
		public void TestBug683011 ()
		{
			string test = CreateInsertionPoint (@"class Test
{$}", "\tint a;", NewLineInsertion.Eol, NewLineInsertion.Eol);
			Assert.AreEqual (@"class Test
{
	int a;
}", test);
		}
		
	}
}

