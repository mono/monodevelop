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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using NUnit.Framework;
using UnitTests;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.CSharp;

namespace MonoDevelop.Ide.Composition
{
	[TestFixture]
	public class CompositionManagerCachingTests
	{
		internal class LocationCachingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
				// Change one of the assemblies' path
				info.Location = typeof (LocationCachingFaultInjector).Assembly.Location;
			}

			public void FaultWritingComposition ()
			{
			}
		}

		internal class ModuleVersionIdCachingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
				// Change one of the assemblies' mvid
				info.ModuleVersionId = Guid.NewGuid ();
			}

			public void FaultWritingComposition ()
			{
			}
		}

		internal class CacheWritingFaultInjector : CompositionManager.ICachingFaultInjector
		{
			public void FaultAssemblyInfo (CompositionManager.MefControlCacheAssemblyInfo info)
			{
			}

			public void FaultWritingComposition ()
			{
				throw new CompositionManager.InvalidRuntimeCompositionException ("test");
			}
		}

		static CompositionManager.Caching GetCaching (CompositionManager.ICachingFaultInjector faultInjector = null, Action<string> onCacheFileRequested = null, [CallerMemberName] string testName = null, CompositionManager.RuntimeCompositionExceptionHandler handler = null)
		{
			var mefAssemblies = CompositionManager.ReadAssembliesFromAddins ();
			var exceptionHandler = handler ?? new CompositionManager.RuntimeCompositionExceptionHandler ();
			var caching = new CompositionManager.Caching (mefAssemblies, handler, file => {
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
			var (composition, catalog) = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, catalog, cacheManager);
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
			await CreateAndWrite (caching);

			caching.MefAssemblies.Add (typeof (CompositionManagerCachingTests).Assembly);

			Assert.IsFalse (caching.CanUse ());
		}

		[Test]
		public async Task TestCacheControlDataIntegrity ()
		{
			var caching = GetCaching ();

			var (composition, catalog) = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);

			var inputAssemblies = catalog.GetInputAssemblies ().Select (x => x.ToString ()).ToArray ();
			Assert.That (caching.MefAssemblies, Contains.Item (typeof (CompositionManager).Assembly));
			Assert.That (inputAssemblies, Contains.Item (typeof (CompositionManager).Assembly.GetName ().ToString ()));

			Assert.That (caching.MefAssemblies, Is.Not.Contains (typeof (Console).Assembly));
			Assert.That (inputAssemblies, Contains.Item (typeof (Console).Assembly.GetName ().ToString ()));
		}

		[TestCase (typeof (LocationCachingFaultInjector))]
		[TestCase (typeof (ModuleVersionIdCachingFaultInjector))]
		public async Task TestControlCacheFaultInjection (Type injectorType)
		{
			var injector = (CompositionManager.ICachingFaultInjector)Activator.CreateInstance (injectorType);
			var caching = GetCaching (injector);
			await CreateAndWrite (caching);

			Assert.IsFalse (caching.CanUse ());
		}

		[Test]
		public void TestCacheWithDynamicAssembly ()
		{
			var asm = GenerateAssembly ();

			var caching = GetCaching ();

			// At some point, it will call asm.Location on each assembly, that would throw if we don't filter them.
			Assert.IsFalse (caching.CanUse (handleExceptions: false));
			Assert.IsFalse (caching.CanUse (handleExceptions: true));
		}

		static Assembly GenerateAssembly()
		{
			var codeProvider = new CSharpCodeProvider ();
			var parameters = new CompilerParameters {
				GenerateExecutable = false,
				GenerateInMemory = true
			};
			var results = codeProvider.CompileAssemblyFromSource (parameters, "public class GeneratedClass {}");
			return results.CompiledAssembly;
		}

		[Test]
		public async Task TestCacheDeletedOnTryingToWriteInvalid ()
		{
			var caching = GetCaching ();
			await CreateAndWrite (caching);

			Assert.True (caching.CanUse ());
			Assert.True (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.True (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");

			caching = GetCaching (new CacheWritingFaultInjector ());
			await CreateAndWrite (caching);

			Assert.IsFalse (File.Exists (caching.MefCacheFile), "Cache was not deleted on corruption");
			Assert.IsFalse (File.Exists (caching.MefCacheControlFile), "Cache control was not deleted on corruption");
		}
	}
}
