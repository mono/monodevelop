//
// DocumentModelTestsBase.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.DocumentModels
{
	public abstract class DocumentModelTestsBase: TestBase
	{
		public abstract DocumentModel CreateModel ();
		public abstract Task LinkModel (DocumentModel model);

		[Test]
		public void AccessingUnitialized ()
		{
			try {
				var file = CreateModel ();
				var r = file.IsNew;
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }
			try {
				var file = CreateModel ();
				var r = file.HasUnsavedChanges;
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }
		}

		[Test]
		public async Task InvalidInitializationOrder ()
		{
			try {
				var file = CreateModel ();
				await file.Load ();
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }

			try {
				var file = CreateModel ();
				await LinkModel (file);
				file.CreateNew ();
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) { }

			string fileName = Path.GetTempFileName ();
			try {
				var file = CreateModel ();
				await LinkModel (file);
				await file.Load ();
				file.CreateNew ();
				Assert.Fail ("Expected InvalidOperationException");
			} catch (InvalidOperationException) {
			} finally {
				File.Delete (fileName);
			}
		}
	}
}
