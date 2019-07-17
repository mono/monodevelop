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

using System;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class ScopeTests : IdeTestBase
	{
		[Test]
		public void TestWholeWorkspace_Creation ()
		{
			var model = new FindInFilesModel {
				SearchScope = SearchScope.WholeWorkspace
			};

			var scope = Scope.Create (model);
			Assert.IsNotNull (scope);
		}

		[Test]
		public void TestAllOpenFilesScope_Creation ()
		{
			var model = new FindInFilesModel {
				SearchScope = SearchScope.AllOpenFiles
			};

			var scope = Scope.Create (model);
			Assert.IsNotNull (scope);
		}

		[Test]
		public void TestCurrentDocumentScope_Creation ()
		{
			var model = new FindInFilesModel {
				SearchScope = SearchScope.CurrentDocument
			};

			var scope = Scope.Create (model);
			Assert.IsNotNull (scope);
		}


		[Test]
		public async Task TestInDirectoryScope_InvalidDirectory ()
		{
			await RunDirectoryTestAsync (async path => {
				var model = new FindInFilesModel {
					FindInFilesPath = null,
					SearchScope = SearchScope.Directories,
					RecurseSubdirectories = false
				};

				var scope = Scope.Create (model);
				Assert.IsNull (scope);
			});
		}

		[Test]
		public async Task TestInDirectoryScope ()
		{
			await RunDirectoryTestAsync (async path => {
				var model = new FindInFilesModel {
					FindInFilesPath = path,
					SearchScope = SearchScope.Directories,
					RecurseSubdirectories = false
				};

				var scope = Scope.Create (model);
				Assert.IsNotNull (scope);
				var files = await scope.GetFilesAsync (model);

				Assert.AreEqual (2, files.Length);
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "a.cs"));
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "b.txt"));
			});
		}

		[Test]
		public async Task TestInDirectoryScope_Recursive ()
		{
			await RunDirectoryTestAsync (async path => {
				var model = new FindInFilesModel {
					FindInFilesPath = path,
					SearchScope = SearchScope.Directories,
					RecurseSubdirectories = true
				};

				var scope = Scope.Create (model);
				Assert.IsNotNull (scope);
				var files = await scope.GetFilesAsync (model);

				Assert.AreEqual (3, files.Length);
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "a.cs"));
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "b.txt"));
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "c.cs"));
			});
		}

		[Test]
		public async Task TestInDirectoryScope_FileMask ()
		{
			await RunDirectoryTestAsync (async path => {
				var model = new FindInFilesModel {
					FindInFilesPath = path,
					SearchScope = SearchScope.Directories,
					RecurseSubdirectories = true,
					FileMask  = "*.cs"
				};

				var scope = Scope.Create (model);
				Assert.IsNotNull (scope);
				var files = await scope.GetFilesAsync (model);

				Assert.AreEqual (2, files.Length);
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "a.cs"));
				Assert.IsTrue (files.Any (f => Path.GetFileName (f.FileName) == "c.cs"));
			});
		}

		static async Task RunDirectoryTestAsync (Func<string, Task> callback)
		{
			var tmpPath = Path.GetTempPath ();

			try {
				tmpPath = Path.Combine (tmpPath, "project");
				Directory.CreateDirectory (tmpPath);
				File.WriteAllText (Path.Combine (tmpPath, "a.cs"), "foo");
				File.WriteAllText (Path.Combine (tmpPath, "b.txt"), "foo");
				var sub = Path.Combine (tmpPath, "sub");
				Directory.CreateDirectory (sub);
				File.WriteAllText (Path.Combine (sub, "c.cs"), "bar");

				await callback (tmpPath);
			} finally {
				Directory.Delete (tmpPath, true);
			}
		}
	}
}