//
// TextFileModelTests.cs
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
using MonoDevelop.Ide.Gui.Documents;
using NUnit.Framework;
using System.Text;

namespace MonoDevelop.Ide.Gui.DocumentModels
{
	public class TextFileModelTests: FileModelTests
	{
		public override DocumentModel CreateModel ()
		{
			return new TextFileModel ();
		}

		public TextFileModel CreateTextFileModel ()
		{
			return (TextFileModel)CreateModel ();
		}

		[Test]
		public void TextFileModelAccessingUnitialized ()
		{
			try {
				var file = CreateTextFileModel ();
				file.SetText ("Foo");
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }

			try {
				var file = CreateTextFileModel ();
				file.GetText ();
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }
		}

		[Test]
		public void TextFileModelAccessingUnitializedForLinked ()
		{
			try {
				var file = CreateTextFileModel ();
				file.LinkToFile ("/foo.txt");
				file.SetText ("Foo");
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }

			try {
				var file = CreateTextFileModel ();
				file.LinkToFile ("/foo.txt");
				file.GetText ();
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }
		}

		[Test]
		public async Task DetectAbsenceOfByteOrderMark ()
		{
			string fileName;
			fileName = Path.GetTempFileName ();
			try {
				var enc = Encoding.UTF8;
				var bytes = enc.GetBytes ("Test");
				Assert.AreEqual (4, bytes.Length);
				using (var f = File.OpenWrite (fileName))
					f.Write (bytes, 0, bytes.Length);

				var file = (TextFileModel) CreateFileModel ();
				await file.LinkToFile (fileName);
				await file.Load ();
				Assert.AreEqual ("Test", file.GetText ());

				await file.Save ();
				Assert.AreEqual (4, new FileInfo (fileName).Length);

				file.SetText ("Testa");
				await file.Save ();
				Assert.AreEqual (5, new FileInfo (fileName).Length);

			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public async Task DetectByteOrderMark ()
		{
			string fileName;
			fileName = Path.GetTempFileName ();
			try {
				var enc = Encoding.UTF8;
				var bytes = enc.GetBytes ("Test");
				var bom = enc.GetPreamble ();
				Assert.AreEqual (3, bom.Length);
				Assert.AreEqual (4, bytes.Length);
				using (var f = File.OpenWrite (fileName)) {
					f.Write (bom, 0, bom.Length);
					f.Write (bytes, 0, bytes.Length);
				}

				var file = (TextFileModel)CreateFileModel ();
				await file.LinkToFile (fileName);
				await file.Load ();
				Assert.AreEqual ("Test", file.GetText ());

				await file.Save ();
				Assert.AreEqual (7, new FileInfo (fileName).Length);

				file.SetText ("Testa");
				await file.Save ();
				Assert.AreEqual (8, new FileInfo (fileName).Length);

			} finally {
				File.Delete (fileName);
			}
		}
	}
}
