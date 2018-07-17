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
		internal class NewerStampCachingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
				info.LastWriteTimeUtc = info.LastWriteTimeUtc.Subtract (TimeSpan.FromSeconds (1));
			}
		}

		internal class OlderStampCachingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
				info.LastWriteTimeUtc = info.LastWriteTimeUtc.Add (TimeSpan.FromSeconds (1));
			}
		}

		internal class LocationCachingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
				// Change one of the assemblies' paths
				info.Location = typeof (LocationCachingFaultInjector).Assembly.Location;
			}
		}

		static CompositionManager.Caching GetCaching (CompositionManager.ICachingFaultInjector faultInjector = null, Action<string> onCacheFileRequested = null, [CallerMemberName] string testName = null)
		{
			var assemblies = CompositionManager.ReadAssembliesFromAddins ();
			var caching = new CompositionManager.Caching (assemblies, file => {
				onCacheFileRequested?.Invoke (file);

				var tmpDir = Path.Combine (Util.TmpDir, "mef", testName);
				if (Directory.Exists (tmpDir)) {
					Directory.Delete (tmpDir, true);
				}
				Directory.CreateDirectory (tmpDir);
				return Path.Combine (tmpDir, file);
			}, faultInjector);

			return caching;
		}

		static async Task<RuntimeComposition> CreateAndWrite (CompositionManager.Caching caching)
		{
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);
			return composition;
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

			Assert.IsFalse (caching.CanUse ());
			Assert.IsFalse (File.Exists (caching.MefCacheFile));
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile));
		}

		[Test]
		public async Task TestCacheIsSaved ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			Assert.IsTrue (File.Exists (caching.MefCacheFile), "MEF cache file was not written");
			Assert.IsTrue (File.Exists (caching.MefCacheControlFile), "MEF cache control file was not written");
			Assert.IsTrue (caching.CanUse (), "MEF cache should be usable");
		}

		[Test]
		public async Task TestCacheIsSavedAndLoaded ()
		{
			var caching = GetCaching ();
			var currentRuntimeComposition = await CreateAndWrite (caching);

			Assert.IsNotNull (currentRuntimeComposition);
			var cachedRuntimeComposition = await CompositionManager.TryCreateRuntimeCompositionFromCache (caching);

			Assert.AreEqual (currentRuntimeComposition, cachedRuntimeComposition);
		}

		[Test]
		public async Task TestCacheFileCorrupted ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.WriteAllText (caching.MefCacheFile, "corrupted");
			Assert.IsNull (await CompositionManager.TryCreateRuntimeCompositionFromCache (caching), "Cache was able to be constructed from corrupted cache");

			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestControlCacheFileCorrupted ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.WriteAllText (caching.MefCacheControlFile, "corrupted");

			Assert.IsFalse (caching.CanUse ());
			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestControlCacheFilePartial ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.WriteAllText (caching.MefCacheControlFile, @"{
   ""AssemblyInfos"":[
      {
         ""Location"":""/Applications/Visual Studio.app/Contents/Resources/lib/monodevelop/bin/Microsoft.VisualStudio.Text.Logic.dll"",
         ""LastWriteTimeUtc"":""2018-03-10T01:13:54Z""
      },");

			Assert.IsFalse (caching.CanUse ());
			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestControlCacheFileEmpty ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.WriteAllText (caching.MefCacheControlFile, "");

			Assert.IsFalse (caching.CanUse ());
			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestControlCacheFileEmptyObject ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.WriteAllText (caching.MefCacheControlFile, "{}");

			Assert.IsFalse (caching.CanUse ());
			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}

		[Test]
		public async Task TestCacheFileMissing ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.Delete (caching.MefCacheFile);

			Assert.IsFalse (caching.CanUse ());
		}

		[Test]
		public async Task TestControlCacheFileMissing ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			File.Delete (caching.MefCacheControlFile);

			Assert.IsFalse (caching.CanUse ());
		}

		[Test]
		public async Task TestControlCacheFileStaleList ()
		{
			var caching = GetCaching ();
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);

			caching.Assemblies.Add (typeof (Console).Assembly);

			Assert.IsFalse (caching.CanUse ());
		}

		[TestCase (typeof (OlderStampCachingFaultInjector))]
		[TestCase (typeof (NewerStampCachingFaultInjector))]
		[TestCase (typeof (LocationCachingFaultInjector))]
		public async Task TestControlCacheFaultInjection (Type injectorType)
		{
			var injector = (CompositionManager.ICachingFaultInjector)Activator.CreateInstance (injectorType);
			var caching = GetCaching (injector);
			var composition = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, cacheManager);

			Assert.IsFalse (caching.CanUse ());
		}
	}
}
