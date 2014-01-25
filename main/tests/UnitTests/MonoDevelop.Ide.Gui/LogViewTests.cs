//
// LogViewTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using NUnit.Framework;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Components;


namespace MonoDevelop.Ide.Gui
{
	[TestFixture]
	public class LogViewTests
	{
		[Test]
		public void TestUnixLineExtraction ()
		{
			string text = "   at MonoDevelop.Ide.Gui.LogViewTests.TestUnixLineExtraction() in /Users/user/monodevelop/main/tests/UnitTests/MonoDevelop.Ide.Gui/LogViewTests.cs:line 45";
			string file;
			int line;
			Assert.IsTrue (LogView.LogTextView.TryExtractFileAndLine (text, out file, out line));
			Assert.AreEqual (file, "/Users/user/monodevelop/main/tests/UnitTests/MonoDevelop.Ide.Gui/LogViewTests.cs");
			Assert.AreEqual (line, 45); 
		}

		[Test]
		public void TestWindowsLineExtraction ()
		{
			string text = "bei MonoDevelop.Ide.IdeApp.Run() in c:\\Users\\user\\Documents\\GitHub\\monodevelop\\main\\src\\core\\MonoDevelop.Ide\\MonoDevelop.Ide\\Ide.cs:Zeile 385.";
			string file;
			int line;
			Assert.IsTrue (LogView.LogTextView.TryExtractFileAndLine (text, out file, out line));
			Assert.AreEqual (file, "c:\\Users\\user\\Documents\\GitHub\\monodevelop\\main\\src\\core\\MonoDevelop.Ide\\MonoDevelop.Ide\\Ide.cs");
			Assert.AreEqual (line, 385); 
		}

	}
}

