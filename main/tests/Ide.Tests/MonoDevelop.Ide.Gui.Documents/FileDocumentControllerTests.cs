//
// FileDocumentControllerTests.cs
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
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Text;
using System.IO;

namespace MonoDevelop.Ide.Gui.Documents
{
	public class FileDocumentControllerTests : TestBase
	{
		[Test]
		public async Task CreateFileWithImplicitModel ()
		{
			FilePath filePath = "foo.txt";
			var file = new TestFileDocumentController ();
			var content = TestHelper.ToStream ("Test");
			await file.Initialize (new FileDescriptor (filePath, "text/plain", content, null));

			Assert.IsTrue (file.IsNewDocument);
			Assert.IsTrue (file.HasUnsavedChanges);
			Assert.AreEqual ("foo.txt", file.FilePath.FileName);
			Assert.AreEqual ("text/plain", file.MimeType);
			Assert.AreEqual (Encoding.UTF8, file.Encoding);
			Assert.IsInstanceOf<TextFileModel> (file.Model);
			Assert.AreEqual ("Test", TestHelper.FromStream (file.FileModel.GetContent ()));
			Assert.AreEqual ("foo.txt", file.DocumentTitle);

			FilePath tempFile = Path.GetTempFileName ();
			try {
				file.FilePath = tempFile;
				Assert.AreEqual (tempFile, file.FilePath);

				await file.Save ();

				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.IsTrue (file.FileModel.IsLinked);
				Assert.AreEqual (tempFile, file.FileModel.FilePath);

				// Verify that the model is now shared
				var registry = await Runtime.GetService<DocumentModelRegistry> ();
				var copy = await registry.GetSharedModel<TextFileModel> (tempFile);
				await copy.Load ();
				Assert.AreEqual ("Test", copy.GetText ());

				await file.FileModel.SetContent (TestHelper.ToStream ("Test Changed"));
				Assert.AreEqual ("Test Changed", copy.GetText ());
			} finally {
				File.Delete (tempFile);
			}
		}

		[Test]
		public async Task LoadFileWithImplicitModel ()
		{
			FilePath tempFile = Path.GetTempFileName ();

			try {
				File.Move (tempFile, tempFile + ".txt");
				tempFile = tempFile + ".txt";
				File.WriteAllText (tempFile, "Test");

				var file = new TestFileDocumentController ();
				await file.Initialize (new FileDescriptor (tempFile, null, null));

				Assert.IsFalse (file.IsNewDocument);
				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.AreEqual (tempFile, file.FilePath);
				Assert.AreEqual ("text/plain", file.MimeType);
				Assert.AreEqual (Encoding.UTF8, file.Encoding);
				Assert.IsInstanceOf<TextFileModel> (file.Model);
				Assert.IsTrue (file.Model.IsLinked);
				Assert.IsFalse (file.Model.IsLoaded);
				Assert.AreEqual (tempFile, file.FileModel.FilePath);
				Assert.AreEqual (tempFile.FileName, file.DocumentTitle);

				await file.Model.Load ();

				// Verify that the model is now shared
				var registry = await Runtime.GetService<DocumentModelRegistry> ();
				var copy = await registry.GetSharedModel<TextFileModel> (tempFile);
				await copy.Load ();
				Assert.AreEqual ("Test", copy.GetText ());

				await file.FileModel.SetContent (TestHelper.ToStream ("Test Changed"));
				Assert.IsTrue (file.HasUnsavedChanges);

				Assert.AreEqual ("Test Changed", copy.GetText ());

				await file.Save ();
				Assert.IsFalse (file.HasUnsavedChanges);

				Assert.AreEqual ("Test Changed", File.ReadAllText (tempFile));

			} finally {
				File.Delete (tempFile);
			}
		}

		[Test]
		public async Task CreateFileWithoutImplicitModel ()
		{
			FilePath filePath = "foo.txt";
			var file = new FileDocumentController ();
			var content = TestHelper.ToStream ("Test");
			await file.Initialize (new FileDescriptor (filePath, "text/plain", content, null));

			Assert.IsTrue (file.IsNewDocument);
			Assert.IsTrue (file.HasUnsavedChanges);
			Assert.AreEqual ("foo.txt", file.FilePath.FileName);
			Assert.AreEqual ("text/plain", file.MimeType);
			Assert.AreEqual (Encoding.UTF8, file.Encoding);
			Assert.IsNull (file.Model);
			Assert.AreEqual ("foo.txt", file.DocumentTitle);

			FilePath tempFile = Path.GetTempFileName ();
			try {
				file.FilePath = tempFile;
				Assert.AreEqual (tempFile, file.FilePath);
				await file.Save ();

				// The save implementation must reset HasUnsavedChanges
				Assert.IsTrue (file.HasUnsavedChanges);

				Assert.IsNull (file.Model);
			} finally {
				File.Delete (tempFile);
			}
		}

		[Test]
		public async Task InferMimeType ()
		{
			FilePath filePath = "foo.txt";
			var file = new TestFileDocumentController ();
			await file.Initialize (new FileDescriptor (filePath, null, null));

			Assert.AreEqual ("foo.txt", file.FilePath.FileName);
			Assert.AreEqual ("text/plain", file.MimeType);

			file.MimeType = "text/xml";
			Assert.AreEqual ("text/xml", file.MimeType);

			file.MimeType = null;
			Assert.AreEqual ("text/plain", file.MimeType);
		}
	}

	class TestFileDocumentController : FileDocumentController
	{
		protected override Type FileModelType => typeof(TextFileModel);
	}
}
