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
using System.Collections.Generic;
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
		internal class CachingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
				info.LastWriteTimeUtc = DateTime.UtcNow;
			}
		}

		static CompositionManager.Caching GetCaching (CompositionManager.ICachingFaultInjector faultInjector = null, Action<string> onCacheFileRequested = null, [CallerMemberName] string testName = null)
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
			}, faultInjector);
		}

		[Test]
		public void TestCacheFileNames ()
		{
			bool cacheFileRequested = false;
			bool cacheControlFileRequested = false;

			var caching = GetCaching (onCacheFileRequested: fileName => {
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
			Assert.AreEqual (true, caching.CanUse (), "MEF cache should be usable");
		}

		[Test]
		public async Task TestCacheIsSavedAndLoaded ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);
			Assert.IsNotNull (await CompositionManager.TryCreateRuntimeCompositionFromCache (caching));
		}

		[Test]
		public async Task TestCacheFileCorrupted ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);

			File.WriteAllText (caching.MefCacheFile, "corrupted");
			Assert.IsNull (await CompositionManager.TryCreateRuntimeCompositionFromCache (caching), "Cache was able to be constructed from corrupted cache");

			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestControlCacheFileCorrupted ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);

			File.WriteAllText (caching.MefCacheControlFile, "corrupted");

			Assert.AreEqual (false, caching.CanUse ());
			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestControlCacheFileStaleList ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);

			caching.Assemblies.Add (typeof (Console).Assembly);

			Assert.AreEqual (false, caching.CanUse ());
		}

		[Test]
		public async Task TestControlCacheFileStamps ()
		{
			var caching = GetCaching (faultInjector: new CachingFaultInjector ());
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);

			Assert.AreEqual (false, caching.CanUse ());
		}
	}
}
