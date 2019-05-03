//
// ScopeTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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

using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class ScopeTests : IdeTestBase
	{
		[Test]
		public async Task TestIssue5949 ()
		{
			string fileName1 = Path.GetTempFileName () + ".cs";
			var documentManager = await Runtime.GetService<DocumentManager> ();

			try {
				File.WriteAllText (fileName1, "Foo");
				var doc1 = await documentManager.OpenDocument (new FileOpenInformation (fileName1));
				doc1.RunWhenContentAdded<ITextView> (delegate {
					try {
						Assert.IsInstanceOf<OpenFileProvider> (WholeProjectScope.GetFileProvider (documentManager, fileName1, null));
					} finally {
						File.Delete (fileName1);
					}
				});
			} finally {
				if (File.Exists (fileName1))
					File.Delete (fileName1);
			}

			string fileName2 = Path.GetTempFileName () + ".cs";
			try {
				File.WriteAllText (fileName2, "Foo");
				Assert.IsInstanceOf<FileProvider> (WholeProjectScope.GetFileProvider (documentManager, fileName2, null));
				File.Delete (fileName2);
			} finally {
				File.Delete (fileName1);
			}

		}
	}
}
