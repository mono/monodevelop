//
// CompositionManager.CachingTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Composition
{
	[TestFixture]
	public class CompositionManagerCachingTests
	{
		static CompositionManager.Caching GetCaching (Action<string> onCacheFileRequested = null, [CallerMemberName] string testName = null)
		{
			var assemblies = CompositionManager.ReadAssembliesFromAddins ();
			return new CompositionManager.Caching (assemblies, file => {
				onCacheFileRequested?.Invoke (file);

				var tmpDir = Path.Combine (Util.TmpDir, "mef", testName);
				if (Directory.Exists (tmpDir)) {
					Directory.Delete (tmpDir, true);
				}
				Directory.CreateDirectory (tmpDir);
				return Path.Combine (tmpDir, file);
			});
		}

		[Test]
		public void TestCacheFileNames ()
		{
			bool cacheFileRequested = false;
			bool cacheControlFileRequested = false;

			var caching = GetCaching (fileName => {
				cacheFileRequested |= fileName == "mef-cache";
				cacheControlFileRequested |= fileName == "mef-cache-control";
			});

			Assert.AreEqual (true, cacheFileRequested, "Cache file was not requested");
			Assert.AreEqual (true, cacheControlFileRequested, "Cache control file was not requested");
		}

		[Test]
		public void TestNonExistentCache ()
		{
			var caching = GetCaching ();
			Assert.AreEqual (false, caching.CanUse (), "Cache should not be usable when it doesnt exist");
		}

		[Test]
		public async Task TestCacheIsSaved ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);
			Assert.AreEqual (true, File.Exists (caching.MefCacheFile), "MEF cache file was not written");
			Assert.AreEqual (true, File.Exists (caching.MefCacheControlFile), "MEF cache control file was not written");
		}

		[Test]
		public async Task TestSavedCacheCanBeUsed ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);
			Assert.AreEqual (true, caching.CanUse (), "MEF cache should be usable");
		}
	}
}
