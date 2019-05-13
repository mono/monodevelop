//
// FindInFilesSessionTests.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class FindInFilesSessionTests : IdeTestBase
	{
		[Test]
		public async Task TestSearch ()
		{
			var model = new FindInFilesModel {
				RecurseSubdirectories = true,
				FindPattern = "foo"
			};

			await TestSession (model, async (scope, session, monitor) => {
				var result = session.FindAll (model, await scope.GetFilesAsync (model), monitor, default).ToList ();
				Assert.AreEqual (2, result.Count);
				Assert.IsTrue (result.Any (f => Path.GetFileName (f.FileName) == "a.cs"));
				Assert.IsTrue (result.Any (f => Path.GetFileName (f.FileName) == "b.txt"));
			});
		}

		private static async Task TestSession (FindInFilesModel model, Func<Scope, FindInFilesSession, ProgressMonitor, Task> callback)
		{
			if (model == null)
				throw new ArgumentNullException (nameof (model));

			var tmpPath = Path.GetTempPath ();

			try {
				tmpPath = Path.Combine (tmpPath, "project");
				Directory.CreateDirectory (tmpPath);
				File.WriteAllText (Path.Combine (tmpPath, "a.cs"), "foo");
				File.WriteAllText (Path.Combine (tmpPath, "b.txt"), "foo");
				var sub = Path.Combine (tmpPath, "sub");
				Directory.CreateDirectory (sub);
				File.WriteAllText (Path.Combine (sub, "c.cs"), "bar");

				model.SearchScope = SearchScope.Directories;
				model.FindInFilesPath = tmpPath;

				var scope = Scope.Create (model);

				var session = new FindInFilesSession ();
				var monitor = new ProgressMonitor ();
				await callback (scope, session, monitor);


			} finally {
				Directory.Delete (tmpPath, true);
			}
		}
	}
}
