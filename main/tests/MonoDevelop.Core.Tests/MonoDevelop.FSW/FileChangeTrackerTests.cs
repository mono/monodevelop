//
// FileChangeTrackerTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MonoDevelop.FSW
{
	[TestFixture]
	public class FileChangeTrackerTests
	{
		[Test]
		public void TestContructedProperly ()
		{
			var asm = typeof (FileChangeTrackerTests).Assembly.Location;

			using (var tracker = new FileChangeTracker (asm)) {
				Assert.AreEqual (asm, tracker.FilePath);
			}
		}

		[Test]
		public async Task AssertUpdateTriggered ()
		{
			var tmpDir = UnitTests.Util.CreateTmpDir ("filechangetracker");
			var tmpFile = Path.Combine (tmpDir, "file.dll");
			var tmpFileMoved = Path.Combine (tmpDir, "file2.dll");

			var tracker = new FileChangeTracker (tmpFile);
			using (tracker) {
				// Create the file
				Assert.AreEqual (true, await ListenToOnUpdate (tracker, () => File.WriteAllText (tmpFile, "test")));
				// Modify the file size.
				Assert.AreEqual (true, await ListenToOnUpdate (tracker, () => File.WriteAllText (tmpFile, "test2")));

				// Rename
				Assert.AreEqual (true, await ListenToOnUpdate (tracker, () => File.Move (tmpFile, tmpFileMoved)));
				File.Move (tmpFileMoved, tmpFile);

				// Delete the file
				Assert.AreEqual (true, await ListenToOnUpdate (tracker, () => File.Delete (tmpFile)));
			}

			// We should no longer be getting events after disposal.
			Assert.AreEqual (false, await ListenToOnUpdate (tracker, () => File.WriteAllText (tmpFile, "test")));
		}

		static Task<bool> ListenToOnUpdate (FileChangeTracker tracker, Action callback)
		{
			var tcs = new TaskCompletionSource<bool> ();
			tracker.UpdatedOnDisk += (o, e) => tcs.TrySetResult (true);

			var cts = new CancellationTokenSource ();
			cts.Token.Register (() => tcs.TrySetResult (false));
			cts.CancelAfter (5000);

			callback ();

			return tcs.Task;
		}
	}
}
