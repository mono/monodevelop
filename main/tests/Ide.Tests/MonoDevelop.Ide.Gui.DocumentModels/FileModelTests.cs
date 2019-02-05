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

namespace MonoDevelop.Ide.Gui.DocumentModels
{
	public class FileModelTests: DocumentModelTestsBase
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

		public static Stream ToStream (string text)
		{
			var mem = new MemoryStream ();
			var w = new StreamWriter (mem);
			w.Write (text);
			w.Flush ();
			mem.Position = 0;
			return mem;
		}

		public static string FromStream (Stream stream)
		{
			var r = new StreamReader (stream);
			return r.ReadToEnd ();
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
			Assert.IsFalse (file.CanWrite);
			await file.SetContent (ToStream ("Foo"));
			Assert.AreEqual ("Foo", FromStream (file.GetContent ()));

			// Not liked to a file, so it should not load, but also not fail
			await file.Load ();
			Assert.IsTrue (file.IsLoaded);
			Assert.IsTrue (file.IsNew);

			Assert.AreEqual ("Foo", FromStream (file.GetContent ()));

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

				Assert.AreEqual ("Foo", FromStream (file.GetContent ()));

				// File was new, so it won't load data from file
				await file.Load ();
				Assert.AreEqual ("Foo", FromStream (file.GetContent ()));
				Assert.AreEqual ("Empty", File.ReadAllText (fileName));

				await file.Save ();
				Assert.AreEqual ("Foo", File.ReadAllText (fileName));

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
				Assert.IsFalse (file.IsNew);
				Assert.IsFalse (file.IsLoaded);
				await file.Load ();
				Assert.IsTrue (file.IsLoaded);
				Assert.AreEqual ("Foo", FromStream (file.GetContent ()));
			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public async Task UnsharedFileModelChangeEvents ()
		{
			int changedCount = 0;

			var file = new TextFileModel ();

			Assert.AreEqual (0, changedCount);

			file.Changed += delegate {
				changedCount++;
			};

			file.CreateNew ();
			Assert.AreEqual (0, changedCount);

			file.GetContent ();
			Assert.AreEqual (0, changedCount);

			await file.SetContent (ToStream ("Foo"));
			Assert.AreEqual (1, changedCount);
		}
	}
}
