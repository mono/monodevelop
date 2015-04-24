//
// VersionInfoCacheTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using NUnit.Framework;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class VersionInfoCacheTests
	{
		VersionInfoCache cache;

		static FilePath GetFilePath (string path, params string[] others)
		{
			return FilePath.Build (Environment.CurrentDirectory, path, FilePath.Build (others));
		}

		[SetUp]
		public void SetUpCache ()
		{
			cache = new VersionInfoCache (new UnknownRepository ());
		}

		[Test]
		public void CanSetAndGetStatusOfFile ()
		{
			VersionInfo cached;

			// Initial set
			var vi = new VersionInfo (GetFilePath ("path"), string.Empty, false, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null);
			cache.SetStatus (vi);

			// Verify integrity of set.
			cached = cache.GetStatus (GetFilePath ("path"));
			Assert.AreSame (vi, cached);

			// Check that status is replaced on same path.
			var vi2 = new VersionInfo (GetFilePath ("path"), string.Empty, false, VersionStatus.ScheduledAdd, null, VersionStatus.Unversioned, null);
			cache.SetStatus (vi2);
			Assert.AreSame (vi2, cache.GetStatus (GetFilePath ("path")));
		}

		[Test]
		public void CanSetStatusOfMultipleFiles ()
		{
			VersionInfo cached;
			var statuses = new[] {
				new VersionInfo (GetFilePath ("path", "file1"), string.Empty, false, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null),
				new VersionInfo (GetFilePath ("path", "file2"), string.Empty, false, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null),
			};

			cache.SetStatus (statuses);

			cached = cache.GetStatus (GetFilePath ("path", "file1"));
			Assert.AreSame (statuses[0], cached);

			cached = cache.GetStatus (GetFilePath ("path", "file2"));
			Assert.AreSame (statuses[1], cached);
		}

		[Test]
		public void CanSetAndUnsetRefresh ()
		{
			VersionInfo cached;
			// Initial set
			var vi = new VersionInfo (GetFilePath ("path"), string.Empty, false, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null);
			cache.SetStatus (vi);

			// Check that caching sets requires refresh.
			cached = cache.GetStatus (GetFilePath ("path"));
			cache.ClearCachedVersionInfo (GetFilePath ("path"));
			Assert.IsTrue (cached.RequiresRefresh);

			// If we try and set the same again, requires refresh goes to false.
			cache.SetStatus (cached);
			Assert.IsFalse (cached.RequiresRefresh);
		}

		[Test]
		public void CanGetAndSetStatusOfADirectory ()
		{
			DirectoryStatus status;
			var statuses = new[] {
				new VersionInfo (GetFilePath ("path", "file"), string.Empty, false, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null),
				new VersionInfo (GetFilePath ("path", "inner"), string.Empty, true, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null),
				new VersionInfo (GetFilePath ("path", "inner", "file"), string.Empty, false, VersionStatus.Unmodified, null, VersionStatus.Unversioned, null),
			};

			cache.SetDirectoryStatus (GetFilePath ("path"), statuses, false);

			status = cache.GetDirectoryStatus (GetFilePath ("path"));
			Assert.IsTrue (status.FileInfo.All (vi => vi.Status == VersionStatus.Unmodified));
			Assert.IsTrue (status.FileInfo.All (vi => !vi.RequiresRefresh));

			cache.ClearCachedVersionInfo (GetFilePath ("path", "inner"));

			for (int i = 0; i < statuses.Length; ++i)
				Assert.AreSame (statuses[i], status.FileInfo[i]);

			// TODO: Fixme?
			Assert.IsFalse (status.RequiresRefresh);
			Assert.IsTrue (statuses [1].RequiresRefresh);
			Assert.IsTrue (statuses [2].RequiresRefresh);

			cache.ClearCachedVersionInfo (GetFilePath ("path"));
			Assert.IsTrue (statuses.All (vi => vi.RequiresRefresh));
			Assert.IsTrue (status.FileInfo.All (vi => vi.RequiresRefresh));
			Assert.IsTrue (status.RequiresRefresh);

			cache.SetDirectoryStatus (GetFilePath ("path"), statuses, false);
			Assert.IsTrue (status.FileInfo.All (vi => !vi.RequiresRefresh));
		}
	}
}

