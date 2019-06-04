//
// FileModelTests.cs
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
using MonoDevelop.Ide.Gui.Documents;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using UnitTests;

namespace MonoDevelop.Ide.Gui.DocumentModels
{
	[RequireService(typeof(DesktopService))]
	public class FileModelTests : DocumentModelTestsBase
	{
		string tempFile;

		[TearDown]
		public void TearDownLocal ()
		{
			if (tempFile != null) {
				File.Delete (tempFile);
				tempFile = null;
			}
		}

		public override DocumentModel CreateModel ()
		{
			return new FileModel ();
		}

		public override Task LinkModel (DocumentModel model)
		{
			tempFile = Path.GetTempFileName ();
			return ((FileModel)model).LinkToFile (tempFile);
		}

		public FileModel CreateFileModel ()
		{
			return (FileModel)CreateModel ();
		}

		[Test]
		public void FileModelAccessingUnitialized ()
		{
			try {
				var file = CreateFileModel ();
				var r = file.CanWrite;
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }
		}

		[Test]
		public void FileModelAccessingUnitializedForLinked ()
		{
			try {
				var file = CreateFileModel ();
				file.LinkToFile ("/foo.txt");
				file.SetContent (new MemoryStream ());
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }

			try {
				var file = CreateFileModel ();
				file.LinkToFile ("/foo.txt");
				file.GetContent ();
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }
		}

		[Test]
		public async Task UnsharedFileModelCreation ()
		{
			var file = CreateFileModel ();

			Assert.AreEqual (FilePath.Null, file.FilePath);
			Assert.IsNull (file.Id);
			Assert.IsFalse (file.IsShared);
			Assert.IsFalse (file.IsLoaded);

			file.CreateNew ();
			Assert.IsTrue (file.IsLoaded);
			Assert.IsTrue (file.IsNew);
			Assert.IsTrue (file.HasUnsavedChanges);
			Assert.IsFalse (file.CanWrite);
			await file.SetContent (TestHelper.ToStream ("Foo"));
			Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));

			// Not liked to a file, so it should not load, but also not fail
			await file.Load ();
			Assert.IsTrue (file.IsLoaded);
			Assert.IsTrue (file.IsNew);
			Assert.IsTrue (file.HasUnsavedChanges);

			Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));

			string fileName = Path.GetTempFileName ();
			try {
				File.WriteAllText (fileName, "Empty");
				await file.LinkToFile (fileName);
				Assert.AreEqual (fileName, file.FilePath.ToString ());
				Assert.IsTrue (file.CanWrite);
				Assert.IsFalse (file.IsNew);
				Assert.IsNotNull (file.Id);
				Assert.IsFalse (file.IsShared);
				Assert.IsTrue (file.IsLoaded);
				Assert.IsTrue (file.HasUnsavedChanges);

				Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));

				// File was new, so it won't load data from file
				await file.Load ();
				Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));
				Assert.AreEqual ("Empty", File.ReadAllText (fileName));

				await file.Save ();
				Assert.AreEqual ("Foo", File.ReadAllText (fileName));
				Assert.IsFalse (file.IsNew);
				Assert.IsFalse (file.HasUnsavedChanges);

			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public async Task UnsharedFileModelLoad ()
		{
			string fileName;
			fileName = Path.GetTempFileName ();
			try {
				File.WriteAllText (fileName, "Foo");

				var file = CreateFileModel ();
				await file.LinkToFile (fileName);
				Assert.AreEqual (fileName, file.FilePath.ToString ());
				Assert.IsFalse (file.IsNew);
				Assert.IsFalse (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				await file.Load ();
				Assert.IsTrue (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));

				await file.SetContent (TestHelper.ToStream ("Bar"));
				Assert.IsTrue (file.HasUnsavedChanges);
			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public async Task UnsharedFileModelLoadSetsMimeType ()
		{
			var dir = Util.CreateTmpDir ("UnsharedFileModelLoadSetsMimeType");
			var fileName = Path.Combine (dir, "Foo.cs");
			File.WriteAllText (fileName, "Foo");

			var file = CreateFileModel ();
			await file.LinkToFile (fileName);
			Assert.AreEqual (null, file.MimeType);
			await file.Load ();
			Assert.AreEqual ("text/x-csharp", file.MimeType);
		}

		[Test]
		public async Task UnsharedFileModelChangeEvents ()
		{
			int changedCount = 0;

			var file = CreateFileModel ();

			Assert.AreEqual (0, changedCount);

			file.Changed += delegate {
				changedCount++;
			};

			file.CreateNew ();
			Assert.AreEqual (0, changedCount);

			file.GetContent ();
			Assert.AreEqual (0, changedCount);

			await file.SetContent (TestHelper.ToStream ("Foo"));
			Assert.AreEqual (1, changedCount);
		}

		[Test]
		public async Task UnsharedFileModelSaveAs ()
		{
			var file = CreateFileModel ();
			file.CreateNew ();
			await file.SetContent (TestHelper.ToStream ("Foo"));

			string fileName = Path.GetTempFileName ();
			try {
				File.WriteAllText (fileName, "Empty");

				Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));
				Assert.AreEqual ("Empty", File.ReadAllText (fileName));

				await file.SaveAs (fileName);
				Assert.AreEqual (fileName, file.FilePath.ToString ());
				Assert.IsTrue (file.CanWrite);
				Assert.IsFalse (file.IsNew);
				Assert.IsNotNull (file.Id);
				Assert.IsFalse (file.IsShared);
				Assert.IsTrue (file.IsLoaded);

				Assert.AreEqual ("Foo", File.ReadAllText (fileName));

			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public async Task Reload ()
		{
			string fileName;
			fileName = Path.GetTempFileName ();
			try {
				File.WriteAllText (fileName, "Foo");

				var file = CreateFileModel ();
				await file.LinkToFile (fileName);
				Assert.AreEqual (fileName, file.FilePath.ToString ());
				Assert.IsFalse (file.IsNew);
				Assert.IsFalse (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				await file.Load ();
				Assert.IsTrue (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));

				await file.SetContent (TestHelper.ToStream ("Bar"));
				Assert.IsTrue (file.HasUnsavedChanges);

				File.WriteAllText (fileName, "Modified");

				bool changed = false;
				file.Changed += (sender, e) => changed = true;

				await file.Reload ();

				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.AreEqual ("Modified", TestHelper.FromStream (file.GetContent ()));
				Assert.IsTrue (changed);

			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public void CreateNewFile ()
		{
			using (var model = CreateFileModel ()) {
				model.CreateNew ("foo.cs", null);
				Assert.AreEqual ("foo.cs", model.FilePath.ToString ());
				Assert.AreEqual ("text/x-csharp", model.MimeType);
			}
			using (var model = CreateFileModel ()) {
				model.CreateNew (null, "text/x-csharp");
				Assert.AreEqual (FilePath.Null, model.FilePath);
				Assert.AreEqual ("text/x-csharp", model.MimeType);
			}
		}

		[Test]
		public async Task CreateNewFileRenameWhenSaving ()
		{
			using (var model = CreateFileModel ()) {
				model.CreateNew ("foo.cs", null);

				var dir = UnitTests.Util.CreateTmpDir ("CreateNewFileRenameWhenSaving");
				var file = Path.Combine (dir, "bar.txt");
				await model.SaveAs (file);
				Assert.AreEqual ("text/plain", model.MimeType);
				Assert.AreEqual (file, model.FilePath.ToString ());
			}
		}
	}
}
