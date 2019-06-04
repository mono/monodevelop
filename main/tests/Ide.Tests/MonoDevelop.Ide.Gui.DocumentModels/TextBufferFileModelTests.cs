//
// TextBufferFileModelTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;
using NUnit.Framework;

namespace MonoDevelop.Ide.Gui.DocumentModels
{
	public class TextBufferFileModelTests: TextFileModelTests
	{
		public override DocumentModel CreateModel ()
		{
			return new TextBufferFileModel ();
		}

		[Test]
		public void CreateNewTextBufferFile ()
		{
			using (var model = new TextBufferFileModel ()) {
				model.CreateNew ("foo.cs", null);
				Assert.AreEqual ("CSharp", model.TextBuffer.ContentType.TypeName);
			}
			using (var model = new TextBufferFileModel ()) {
				model.CreateNew (null, "text/x-csharp");
				Assert.AreEqual ("CSharp", model.TextBuffer.ContentType.TypeName);
			}
		}

		[Test]
		public async Task CreateNewTextBufferFileRenameWhenSaving ()
		{
			using (var model = new TextBufferFileModel ()) {
				model.CreateNew ("foo.cs", null);
				Assert.AreEqual ("CSharp", model.TextBuffer.ContentType.TypeName);

				var dir = UnitTests.Util.CreateTmpDir ("CreateNewFileRenameWhenSaving");
				var file = Path.Combine (dir, "bar.txt");
				await model.SaveAs (file);
				Assert.AreEqual ("text", model.TextBuffer.ContentType.TypeName);
			}
		}
	}
}
