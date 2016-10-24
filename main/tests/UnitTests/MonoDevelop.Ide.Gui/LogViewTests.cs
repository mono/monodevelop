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
using System.Threading.Tasks;


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


		[Test]
		public void TestStackTraceExtraction ()
		{
			string text = "at Mono.TextEditor.TextArea+CenterToWrapper.Run (System.Object sender, System.EventArgs e) [0x00216] in /monodevelop/main/src/addins/MonoDevelop.SourceEditor2/Mono.TextEditor/Gui/TextArea.cs:1234 ";
			string file;
			int line;
			Assert.IsTrue (LogView.LogTextView.TryExtractFileAndLine (text, out file, out line));
			Assert.AreEqual (file, "/monodevelop/main/src/addins/MonoDevelop.SourceEditor2/Mono.TextEditor/Gui/TextArea.cs");
			Assert.AreEqual (line, 1234);
		}

		[Test]
		public void TestNewLineAtEnd ()
		{
			string text = "at X in /a:1\t\n";
			string file;
			int line;
			Assert.IsTrue (LogView.LogTextView.TryExtractFileAndLine (text, out file, out line));
			Assert.AreEqual (file, "/a");
			Assert.AreEqual (line, 1);
		}

		[Test]
		public async Task RunAnimation ()
		{
			Assert.Ignore ("This test is *far* too strict to reliably pass on different machines. A 20ms window is not enough");

			int n = 0;
			DateTime t = DateTime.MinValue;
			int t1 = 0, t2 = 0;
			DispatchService.RunAnimation (() => {
				n++;
				if (n == 1) {
					t = DateTime.Now;
					return 100;
				}
				else if (n == 2) {
					t1 = (int)(DateTime.Now - t).TotalMilliseconds;
					t = DateTime.Now;
					return 200;
				}
				else {
					t2 = (int)(DateTime.Now - t).TotalMilliseconds;
					return -1;
				}
			});
			await Task.Delay (1000);
			Assert.IsTrue (t1 >= 100 && t1 <= 120);
			Assert.IsTrue (t2 >= 200 && t1 <= 220);
		}
	}
}

