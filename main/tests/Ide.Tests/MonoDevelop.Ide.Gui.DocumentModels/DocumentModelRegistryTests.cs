//
// DocumentModelTests.cs
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
using MonoDevelop.Ide.Gui.Documents;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Gui.DocumentModels
{
	public class DocumentModelRegistryTests : TestBase
	{
		DocumentModelRegistry registry;

		[SetUp]
		public void Setup ()
		{
			registry = new DocumentModelRegistry ();
		}

		[Test]
		public async Task RegisterNew ()
		{
			FilePath filePath = "test.txt";
			CustomFileModel.LiveBackends = 0;

			var file = new CustomFileModel ();
			file.CreateNew ();
			file.SetText ("Foo");
			await file.LinkToFile (filePath);
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			await registry.ShareModel (file);
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			var otherFile = await registry.GetSharedModel<CustomFileModel> (filePath);
			Assert.AreNotSame (file, otherFile);
			Assert.IsFalse (otherFile.IsLoaded);
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			await otherFile.Load ();
			Assert.AreEqual ("Foo", otherFile.GetText ());
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			// Backend is shared

			file.SetText ("Bar");
			Assert.AreEqual ("Bar", otherFile.GetText ());

			otherFile.SetText ("Bar2");
			Assert.AreEqual ("Bar2", file.GetText ());

			file.Dispose ();
			Assert.AreEqual (1, CustomFileModel.LiveBackends);
			otherFile.Dispose ();
			Assert.AreEqual (0, CustomFileModel.LiveBackends);
		}

		[Test]
		public async Task UpgradeInstance ()
		{
			FilePath filePath = "test.txt";
			var file = new FileModel ();
			file.CreateNew ();
			await file.SetContent (TestHelper.ToStream ("Foo"));
			await file.LinkToFile (filePath);
			await registry.ShareModel (file);

			var textFile = await registry.GetSharedModel<TextFileModel> (filePath);
			Assert.IsFalse (textFile.IsLoaded);
			await textFile.Load ();
			Assert.AreEqual ("Foo", textFile.GetText ());

			var textBufferFile = await registry.GetSharedModel<TextBufferFileModel> (filePath);
			Assert.IsFalse (textBufferFile.IsLoaded);
			await textBufferFile.Load ();
			Assert.AreEqual ("Foo", textBufferFile.GetText ());
		}

		[Test]
		public async Task DowngradeInstance ()
		{
			FilePath filePath = "test.txt";
			var textBufferFile = new TextBufferFileModel ();
			textBufferFile.CreateNew ();
			textBufferFile.SetText ("Foo");
			await textBufferFile.LinkToFile (filePath);
			await registry.ShareModel (textBufferFile);

			var textFile = await registry.GetSharedModel<TextFileModel> (filePath);
			Assert.IsFalse (textFile.IsLoaded);
			await textFile.Load ();
			Assert.AreEqual ("Foo", textFile.GetText ());

			var file = await registry.GetSharedModel<FileModel> (filePath);
			Assert.IsFalse (file.IsLoaded);
			await file.Load ();
			Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));
		}

		[Test]
		public async Task UnlinkSharedFile ()
		{
			FilePath filePath = "test.txt";
			var file = new CustomFileModel ();
			file.CreateNew ();
			file.SetText ("Foo");
			await file.LinkToFile (filePath);
			await registry.ShareModel (file);
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			var file2 = await registry.GetSharedModel<CustomFileModel> (filePath);
			await file2.Load ();
			Assert.AreEqual (1, CustomFileModel.LiveBackends);
			Assert.AreEqual ("Foo", file2.GetText ());

			await file.ConvertToUnsaved ();
			Assert.AreEqual (2, CustomFileModel.LiveBackends);
			Assert.IsNull (file.Id);
			file.SetText ("Bar");
			Assert.AreEqual ("Foo", file2.GetText ());

			file.Dispose ();
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			file2.Dispose ();
			Assert.AreEqual (0, CustomFileModel.LiveBackends);
		}

		[Test]
		public async Task UnlinkSharedFileKeepSharedData ()
		{
			// Two different models for same file. One is loaded and modified.
			// The other is not loaded. Unlinking the modified should assign
			// data to unloaded model.

			FilePath filePath = "test.txt";
			var file = new CustomFileModel ();
			file.CreateNew ();
			file.SetText ("Foo");
			await file.LinkToFile (filePath);
			await registry.ShareModel (file);
			Assert.AreEqual (1, CustomFileModel.LiveBackends);

			var file2 = await registry.GetSharedModel<TextFileModel> (filePath);
			Assert.IsFalse (file2.IsLoaded);

			await file.ConvertToUnsaved ();

			// Unsaved file content should have been migrated to second file
			await file2.Load ();
			Assert.AreEqual ("Foo", file2.GetText ());
		}

		[Test]
		public async Task GetSharedModel ()
		{
			string fileName = Path.GetTempFileName ();
			try {
				File.WriteAllText (fileName, "Foo");

				var file = await registry.GetSharedFileModel<FileModel> (fileName);
				Assert.AreEqual (fileName, file.FilePath.ToString ());
				Assert.IsFalse (file.IsNew);
				Assert.IsFalse (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				await file.Load ();
				Assert.IsTrue (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.AreEqual ("Foo", TestHelper.FromStream (file.GetContent ()));
			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public async Task RelinkSharedBeforeLoad ()
		{
			string fileName = Path.GetTempFileName ();
			try {
				File.WriteAllText (fileName, "Foo");
				File.WriteAllText (fileName + ".tmp", "Bar");

				var file = await registry.GetSharedFileModel<FileModel> (fileName);
				Assert.AreEqual (fileName, file.FilePath.ToString ());
				Assert.IsFalse (file.IsNew);
				Assert.IsFalse (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				await file.LinkToFile (fileName + ".tmp");
				await file.Load ();
				Assert.IsTrue (file.IsLoaded);
				Assert.IsFalse (file.HasUnsavedChanges);
				Assert.AreEqual ("Bar", TestHelper.FromStream (file.GetContent ()));
			} finally {
				File.Delete (fileName);
				File.Delete (fileName + ".tmp");
			}
		}
	}

	class CustomFileModel : TextFileModel
	{
		public static int LiveBackends = 0;

		protected internal override Type RepresentationType => typeof(CustomFileModelRepresentation);

		class CustomFileModelRepresentation : TextFileModelRepresentation
		{
			string text;

			public bool Disposed;

			public CustomFileModelRepresentation ()
			{
				LiveBackends++;
			}

			protected override string OnGetText ()
			{
				return text;
			}

			protected override void OnSetText (string text)
			{
				this.text = text;
			}

			protected override async Task OnLoad ()
			{
				var file = await TextFileUtility.ReadAllTextAsync (FilePath);
				text = file.Text;
				Encoding = file.Encoding;
				UseByteOrderMark = file.HasByteOrderMark;
			}

			protected override void OnCreateNew ()
			{
				text = "";
				Encoding = Encoding.UTF8;
			}

			protected override async Task OnSave ()
			{
				await TextFileUtility.WriteTextAsync (FilePath, text, Encoding, UseByteOrderMark);
			}

			protected internal override Task OnDispose ()
			{
				Disposed = true;
				LiveBackends--;
				return base.OnDispose ();
			}
		}
	}
}
