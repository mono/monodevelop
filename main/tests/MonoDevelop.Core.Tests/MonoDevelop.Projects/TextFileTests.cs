//
// TextFileTests.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace MonoDevelop.Projects
{
	public class TextFileTests
	{
		[Test]
		public void TestRead ()
		{
			var cwd = new FileInfo (Assembly.GetExecutingAssembly ().Location).DirectoryName;
			string tempFilename = Path.Combine (cwd, Path.GetRandomFileName());

			string testText = "hello world!";
			File.WriteAllText (tempFilename, testText);

			var filePath = new MonoDevelop.Core.FilePath (tempFilename);
			var tf = new Text.TextFile ();
			tf.Read (filePath, "UTF8");

			try {
				Assert.AreEqual (tf.Text, testText);
			} finally {
				File.Delete (tempFilename);
			}
		}

		[Test]
		public void TestWrite ()
		{
			var cwd = new FileInfo (Assembly.GetExecutingAssembly ().Location).DirectoryName;
			string tempFilename = Path.Combine (cwd, Path.GetRandomFileName());

			string testText = "hello world!";

			var filePath = new MonoDevelop.Core.FilePath (tempFilename);
			Text.TextFile.WriteFile (filePath, testText, "UTF8");

			string fileContents = File.ReadAllText (tempFilename);

			try {
				Assert.AreEqual (fileContents, testText);
			} finally {
				File.Delete (tempFilename);
			}
		}
	}
}

